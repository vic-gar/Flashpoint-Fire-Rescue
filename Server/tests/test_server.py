"""Pruebas del servidor HTTP que consume Unity.

Levantan el servidor de verdad en un puerto libre y le hacen
peticiones reales, para verificar no solo que responde sino que el
JSON tiene exactamente la forma que espera el cliente de Unity.

Eso último importa mucho: JsonUtility de Unity no avisa cuando un
campo no coincide, simplemente lo deja en su valor por defecto. Un
cambio de nombre en el servidor rompería la visualización en silencio,
así que aquí se verifican los nombres de los campos uno por uno.

Ejecutar desde la carpeta Server:

    python -m unittest discover -s tests -v
"""

import json
import os
import socket
import sys
import threading
import unittest
import urllib.error
import urllib.request

from http.server import HTTPServer

sys.path.insert(
    0,
    os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
)

import server as server_module

from server import Simulation, SimulationServer, DEFAULT_PORT, DEFAULT_STRATEGY


# Rutas de los scripts de Unity que dependen del servidor. Si no
# existen (por ejemplo al correr Server suelto) esas pruebas se saltan.
REPO_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
UNITY_CLIENT = os.path.join(REPO_DIR, "Assets", "Scripts", "Framework", "SimulationClient.cs")
UNITY_STATE = os.path.join(REPO_DIR, "Assets", "Scripts", "Data", "SimulationState.cs")


BOARD_FILE = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    "data",
    "final.txt"
)


def free_port():
    """Pide al sistema un puerto libre para no chocar con nada."""
    with socket.socket() as s:
        s.bind(("", 0))
        return s.getsockname()[1]


class ServerTestCase(unittest.TestCase):
    """Base que levanta y apaga el servidor una vez por clase."""

    @classmethod
    def setUpClass(cls):
        cls.port = free_port()

        server_module.simulation = Simulation(
            board_file=BOARD_FILE,
            seed=42
        )

        cls.httpd = HTTPServer(("localhost", cls.port), SimulationServer)

        cls.thread = threading.Thread(
            target=cls.httpd.serve_forever,
            daemon=True
        )

        cls.thread.start()

    @classmethod
    def tearDownClass(cls):
        cls.httpd.shutdown()
        cls.httpd.server_close()

    def url(self, path):
        return f"http://localhost:{self.port}{path}"

    def get(self, path):
        with urllib.request.urlopen(self.url(path), timeout=5) as response:
            return response.status, json.loads(response.read().decode("utf-8"))

    def post(self, path, payload=None):
        data = None

        if payload is not None:
            data = json.dumps(payload).encode("utf-8")

        request = urllib.request.Request(
            self.url(path),
            data=data if data else b"",
            method="POST"
        )

        with urllib.request.urlopen(request, timeout=5) as response:
            return response.status, json.loads(response.read().decode("utf-8"))

    def reset(self, seed=42, strategy=None):
        body = {"semilla": seed}

        if strategy is not None:
            body["estrategia"] = strategy

        return self.post("/reset", body)


# =========================================================
# Endpoints
# =========================================================

class TestEndpoints(ServerTestCase):

    def setUp(self):
        self.reset(42)

    def test_raiz_lista_los_endpoints(self):
        status, data = self.get("/")

        self.assertEqual(status, 200)
        self.assertIn("endpoints", data)
        self.assertIn("GET /state", data["endpoints"])

    def test_board_devuelve_la_geometria_completa(self):
        status, data = self.get("/board")

        self.assertEqual(status, 200)
        self.assertEqual(data["filas"], 6)
        self.assertEqual(data["columnas"], 8)
        self.assertEqual(len(data["celdas"]), 48)
        self.assertEqual(len(data["puertas"]), 8)
        self.assertEqual(len(data["salidas"]), 4)

    def test_state_devuelve_la_partida_inicial(self):
        status, data = self.get("/state")

        self.assertEqual(status, 200)
        self.assertEqual(data["resultado"], "en_curso")
        self.assertEqual(data["turno"], 0)
        self.assertEqual(len(data["bomberos"]), 6)
        self.assertEqual(len(data["fuegos"]), 10)
        self.assertEqual(len(data["humos"]), 0)
        self.assertEqual(len(data["pois"]), 3)

    def test_step_avanza_un_turno(self):
        _, antes = self.get("/state")
        status, despues = self.post("/step")

        self.assertEqual(status, 200)
        self.assertEqual(despues["turno"], antes["turno"] + 1)

    def test_varios_steps_avanzan_la_partida(self):
        for esperado in range(1, 6):
            _, data = self.post("/step")
            self.assertEqual(data["turno"], esperado)

    def test_reset_vuelve_al_inicio(self):
        for _ in range(5):
            self.post("/step")

        _, data = self.reset(42)

        self.assertEqual(data["turno"], 0)
        self.assertEqual(len(data["fuegos"]), 10)
        self.assertEqual(data["danio"], 0)

    def test_reset_con_semilla_distinta_da_partida_distinta(self):
        _, a = self.reset(1)
        for _ in range(8):
            _, a = self.post("/step")

        _, b = self.reset(2)
        for _ in range(8):
            _, b = self.post("/step")

        # Con semillas distintas es muy improbable que el tablero
        # quede exactamente igual tras ocho turnos.
        iguales = (
            a["danio"] == b["danio"]
            and len(a["fuegos"]) == len(b["fuegos"])
            and [f["fila"] for f in a["fuegos"]] ==
                [f["fila"] for f in b["fuegos"]]
        )

        self.assertFalse(iguales)

    def test_misma_semilla_reproduce_la_partida(self):
        _, a = self.reset(99)
        for _ in range(10):
            _, a = self.post("/step")

        _, b = self.reset(99)
        for _ in range(10):
            _, b = self.post("/step")

        self.assertEqual(a["turno"], b["turno"])
        self.assertEqual(a["danio"], b["danio"])
        self.assertEqual(a["rescatadas"], b["rescatadas"])
        self.assertEqual(len(a["fuegos"]), len(b["fuegos"]))

    def test_ruta_desconocida_devuelve_404(self):
        with self.assertRaises(urllib.error.HTTPError) as contexto:
            self.get("/no-existe")

        self.assertEqual(contexto.exception.code, 404)

    def test_content_type_es_json(self):
        with urllib.request.urlopen(self.url("/state"), timeout=5) as response:
            tipo = response.headers.get("Content-Type", "")

        self.assertIn("application/json", tipo)


# =========================================================
# Configuración por defecto y selección de estrategia
# =========================================================

class TestConfiguracion(ServerTestCase):

    def setUp(self):
        self.reset(42, "mejorada")

    def test_puerto_por_defecto_es_3000(self):
        self.assertEqual(DEFAULT_PORT, 3000)

    def test_estrategia_por_defecto_es_la_mejorada(self):
        self.assertEqual(DEFAULT_STRATEGY, "mejorada")

    def test_raiz_lista_las_estrategias_disponibles(self):
        _, data = self.get("/")

        for name in ["aleatoria", "mejorada", "mejorada_sin_coordinacion"]:
            self.assertIn(name, data["estrategias_disponibles"])

    def test_el_estado_dice_que_estrategia_corre(self):
        _, data = self.get("/state")

        self.assertEqual(data["estrategia"], "mejorada")

    def test_reset_cambia_de_estrategia(self):
        _, data = self.reset(1, "aleatoria")
        self.assertEqual(data["estrategia"], "aleatoria")

        _, data = self.post("/step")
        self.assertEqual(data["estrategia"], "aleatoria")

        _, data = self.reset(1, "mejorada")
        self.assertEqual(data["estrategia"], "mejorada")

    def test_reset_sin_estrategia_conserva_la_actual(self):
        self.reset(1, "aleatoria")

        _, data = self.post("/reset", {"semilla": 2})

        self.assertEqual(data["estrategia"], "aleatoria")

    def test_estrategia_desconocida_devuelve_400(self):
        with self.assertRaises(urllib.error.HTTPError) as contexto:
            self.reset(1, "no_existe")

        self.assertEqual(contexto.exception.code, 400)

    def test_misma_semilla_y_estrategia_reproducen_la_partida(self):
        _, a = self.reset(5, "mejorada")
        for _ in range(12):
            _, a = self.post("/step")

        _, b = self.reset(5, "mejorada")
        for _ in range(12):
            _, b = self.post("/step")

        self.assertEqual(a["rescatadas"], b["rescatadas"])
        self.assertEqual(a["danio"], b["danio"])
        self.assertEqual(len(a["fuegos"]), len(b["fuegos"]))

    def test_cliente_de_unity_usa_el_puerto_3000(self):
        if not os.path.exists(UNITY_CLIENT):
            self.skipTest("SimulationClient.cs no está en esta copia")

        with open(UNITY_CLIENT, encoding="utf-8") as file:
            source = file.read()

        self.assertIn("public int port = 3000;", source)
        self.assertNotIn("8585", source)


# =========================================================
# Contrato con el cliente de Unity
# =========================================================

class TestContratoConUnity(ServerTestCase):
    """Verifica que el JSON coincide con las clases de C#.

    Si alguno de estos nombres cambia, JsonUtility de Unity dejaría el
    campo en su valor por defecto sin lanzar ningún error, y la
    visualización fallaría en silencio.
    """

    def setUp(self):
        self.reset(42)

    def test_campos_del_estado(self):
        _, data = self.get("/state")

        for campo in [
            "resultado",
            "estrategia",
            "turno",
            "bombero_actual",
            "rescatadas",
            "perdidas",
            "danio",
            "bomberos",
            "fuegos",
            "humos",
            "pois",
            "puertas",
        ]:
            self.assertIn(campo, data, f"Falta el campo {campo}")

    def test_campos_de_bombero(self):
        _, data = self.get("/state")

        bombero = data["bomberos"][0]

        for campo in ["id", "fila", "columna", "ap", "cargando"]:
            self.assertIn(campo, bombero)

        self.assertIsInstance(bombero["id"], int)
        self.assertIsInstance(bombero["fila"], int)
        self.assertIsInstance(bombero["columna"], int)
        self.assertIsInstance(bombero["cargando"], bool)

    def test_campos_de_poi(self):
        _, data = self.get("/state")

        poi = data["pois"][0]

        for campo in ["fila", "columna", "revelado", "tipo"]:
            self.assertIn(campo, poi)

        # Mientras no se revele, el tipo no debe filtrarse al cliente.
        if not poi["revelado"]:
            self.assertEqual(poi["tipo"], "?")

    def test_campos_de_puerta(self):
        _, data = self.get("/state")

        puerta = data["puertas"][0]

        for campo in [
            "fila1",
            "columna1",
            "fila2",
            "columna2",
            "abierta",
            "destruida",
        ]:
            self.assertIn(campo, puerta)

    def test_campos_de_celda_del_tablero(self):
        _, data = self.get("/board")

        celda = data["celdas"][0]

        for campo in [
            "fila",
            "columna",
            "arriba",
            "izquierda",
            "abajo",
            "derecha",
        ]:
            self.assertIn(campo, celda)

    def test_coordenadas_dentro_del_tablero(self):
        _, data = self.get("/state")

        for grupo in ["fuegos", "humos", "pois"]:
            for item in data[grupo]:
                self.assertGreaterEqual(item["fila"], 0)
                self.assertLess(item["fila"], 6)
                self.assertGreaterEqual(item["columna"], 0)
                self.assertLess(item["columna"], 8)

    def test_campos_del_json_coinciden_con_las_clases_de_csharp(self):
        """Lee SimulationState.cs y compara sus campos con el JSON real.

        JsonUtility ignora en silencio los campos que no coinciden, así
        que esta es la única forma de enterarse de un desajuste.
        """
        if not os.path.exists(UNITY_STATE):
            self.skipTest("SimulationState.cs no está en esta copia")

        import re

        with open(UNITY_STATE, encoding="utf-8") as file:
            source = file.read()

        def fields_of(class_name):
            match = re.search(
                r"class " + class_name + r"\b.*?\{(.*?)\n\}",
                source,
                re.S
            )
            self.assertIsNotNone(match, f"No se encontró la clase {class_name}")
            return set(re.findall(r"public\s+[\w\[\]]+\s+(\w+)\s*;", match.group(1)))

        _, state = self.get("/state")

        pairs = [
            ("SimulationState", state),
            ("FirefighterState", state["bomberos"][0]),
            ("CellPosition", state["fuegos"][0]),
            ("POIState", state["pois"][0]),
            ("DoorState", state["puertas"][0]),
        ]

        for class_name, sample in pairs:
            cs_fields = fields_of(class_name)
            json_fields = set(sample.keys())

            self.assertEqual(
                cs_fields,
                json_fields,
                f"{class_name}: C# {sorted(cs_fields)} vs JSON {sorted(json_fields)}"
            )

    def test_el_estado_es_json_serializable_sin_perdidas(self):
        """El JSON debe sobrevivir un viaje de ida y vuelta."""
        _, data = self.get("/state")

        texto = json.dumps(data)

        self.assertEqual(json.loads(texto), data)


# =========================================================
# Partida completa a través del servidor
# =========================================================

class TestPartidaCompleta(ServerTestCase):

    def test_la_partida_termina_por_http(self):
        self.reset(0)

        resultado = "en_curso"
        turnos = 0

        while resultado == "en_curso" and turnos < 400:
            _, data = self.post("/step")
            resultado = data["resultado"]
            turnos += 1

        self.assertIn(
            resultado,
            ["victoria", "derrota_victimas", "derrota_colapso"]
        )

    def test_step_sobre_partida_terminada_no_falla(self):
        self.reset(0)

        resultado = "en_curso"
        turnos = 0

        while resultado == "en_curso" and turnos < 400:
            _, data = self.post("/step")
            resultado = data["resultado"]
            turnos += 1

        # Pedir más turnos después del final debe seguir respondiendo
        # el estado final en vez de romperse.
        status, data = self.post("/step")

        self.assertEqual(status, 200)
        self.assertEqual(data["resultado"], resultado)


if __name__ == "__main__":
    unittest.main(verbosity=2)
