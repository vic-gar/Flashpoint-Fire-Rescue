"""Servidor HTTP que expone la simulación de Flash Point a Unity.

Responsabilidad: mantener UNA partida viva en memoria y dejar que Unity
la consulte y la avance por HTTP. Es el lado servidor del criterio 4
(cliente-servidor); el cliente es Assets/Scripts/Framework/
SimulationClient.cs. Todo lo que se responde sale de
FlashPointModel.get_state() y get_board_config().

Integración HTTP desarrollada con apoyo de Claude siguiendo la
plantilla cliente-servidor vista en TC2008B
(tc2008b/template-project/tc2008B_server.py, de Sergio Ruiz-Loza),
con tres correcciones necesarias para que funcione con datos reales:

1. La plantilla arma la respuesta con str(diccionario), que produce
   el repr de Python con comillas simples y no es JSON válido. Aquí
   se usa json.dumps.
2. La plantilla responde con Content-Type text/html. Aquí se responde
   application/json, que es lo que corresponde.
3. La plantilla solo tiene un endpoint de prueba con una posición
   fija. Aquí cada endpoint sirve el estado real del modelo Mesa.

Endpoints:

    GET  /         información del servidor y lista de endpoints
    GET  /board    geometría fija del tablero (paredes, puertas, salidas)
    GET  /state    estado actual de la partida
    POST /step     avanza un turno y devuelve el estado resultante
    POST /reset    reinicia la partida; acepta un JSON opcional con
                   {"semilla": n, "estrategia": "<nombre de STRATEGIES>"}
                   Nombres válidos hoy: aleatoria, mejorada,
                   mejorada_sin_coordinacion (ver strategies/__init__.py).

Uso:

    python server.py                   puerto 3000, estrategia mejorada
    python server.py 8080              puerto 8080
    python server.py 3000 aleatoria    puerto 3000, estrategia aleatoria

COMPATIBILIDAD CON UNITY: SimulationClient.cs tiene el puerto 3000 por
defecto. Si se cambia aquí hay que cambiarlo también en el inspector.
Los nombres de los endpoints y las llaves del JSON son el contrato con
Unity; no renombrar sin cambiar también el lado de C#.

Flujo por turno: Unity manda POST /step, el servidor llama
model.step() (acciones del bombero, fuego, POI) y responde el estado
completo; Unity lo dibuja y espera N segundos antes del siguiente.
"""

import json
import logging

from http.server import BaseHTTPRequestHandler, HTTPServer

from model.flashpoint_model import FlashPointModel
from strategies import STRATEGIES, get_strategy


# NO MODIFICAR SIN REVISAR: el puerto también está en el inspector de
# SimulationClient.cs y hay una prueba que verifica que sea 3000.
DEFAULT_PORT = 3000
DEFAULT_SEED = 42
DEFAULT_STRATEGY = "mejorada"


class Simulation:
    """Guarda la partida en curso entre una petición y la siguiente.

    El servidor HTTP crea un objeto manejador nuevo por cada petición,
    así que el modelo no puede vivir dentro del manejador o se
    reiniciaría en cada llamada.
    """

    def __init__(
        self,
        board_file="data/final.txt",
        seed=DEFAULT_SEED,
        strategy_name=DEFAULT_STRATEGY
    ):
        self.board_file = board_file
        self.seed = seed
        self.strategy_name = strategy_name
        self.model = None

        self.reset(seed, strategy_name)

    def reset(self, seed=None, strategy_name=None):
        if seed is not None:
            self.seed = seed

        if strategy_name is not None:
            self.strategy_name = strategy_name

        self.model = FlashPointModel(
            board_file=self.board_file,
            seed=self.seed,
            strategy=get_strategy(self.strategy_name)
        )

        return self.model

    def step(self):
        if self.model.running:
            self.model.step()

        return self.model


simulation = None


class SimulationServer(BaseHTTPRequestHandler):
    """Atiende una petición HTTP. http.server crea una instancia nueva
    por petición, por eso la partida vive en la variable global
    simulation y no aquí."""

    # =====================================================
    # Utilidades de respuesta
    # =====================================================

    def _send_json(self, payload, status=200):
        body = json.dumps(payload, ensure_ascii=False).encode("utf-8")

        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()

        self.wfile.write(body)

    def _send_error_json(self, message, status=404):
        self._send_json({"error": message}, status)

    def _read_body(self):
        """Lee el cuerpo de un POST y lo interpreta como JSON.

        Devuelve un diccionario vacío si no viene cuerpo, que es el
        caso normal de /step.
        """
        length = int(self.headers.get("Content-Length", 0))

        if length <= 0:
            return {}

        raw = self.rfile.read(length)

        try:
            return json.loads(raw.decode("utf-8"))
        except (ValueError, UnicodeDecodeError):
            return {}

    def log_message(self, format, *args):
        """Log de una línea por petición, más legible que el default."""
        logging.info("%s - %s", self.address_string(), format % args)

    # =====================================================
    # GET
    # =====================================================

    def do_GET(self):
        path = self.path.split("?")[0].rstrip("/")

        if path == "":
            self._send_json({
                "servidor": "Flash Point: Fire Rescue",
                "estrategia": simulation.model.strategy_name,
                "estrategias_disponibles": list(STRATEGIES),
                "semilla": simulation.seed,
                "endpoints": [
                    "GET /board",
                    "GET /state",
                    "POST /step",
                    "POST /reset",
                ],
            })
            return

        if path == "/board":
            self._send_json(simulation.model.get_board_config())
            return

        if path == "/state":
            self._send_json(simulation.model.get_state())
            return

        self._send_error_json(f"Ruta no encontrada: {self.path}")

    # =====================================================
    # POST
    # =====================================================

    def do_POST(self):
        path = self.path.split("?")[0].rstrip("/")

        if path == "/step":
            simulation.step()
            self._send_json(simulation.model.get_state())
            return

        if path == "/reset":
            body = self._read_body()
            seed = body.get("semilla", simulation.seed)
            strategy_name = body.get("estrategia", simulation.strategy_name)

            if strategy_name not in STRATEGIES:
                self._send_error_json(
                    f"Estrategia desconocida: {strategy_name}. "
                    f"Opciones: {', '.join(STRATEGIES)}",
                    status=400
                )
                return

            simulation.reset(seed, strategy_name)

            self._send_json(simulation.model.get_state())
            return

        self._send_error_json(f"Ruta no encontrada: {self.path}")


def run(
    port=DEFAULT_PORT,
    board_file="data/final.txt",
    seed=DEFAULT_SEED,
    strategy_name=DEFAULT_STRATEGY
):
    global simulation

    logging.basicConfig(level=logging.INFO, format="%(message)s")

    simulation = Simulation(
        board_file=board_file,
        seed=seed,
        strategy_name=strategy_name
    )

    httpd = HTTPServer(("", port), SimulationServer)

    print("=" * 55)
    print("SERVIDOR FLASH POINT: FIRE RESCUE")
    print("=" * 55)
    print(f"Escuchando en:  http://localhost:{port}")
    print(f"Semilla:        {seed}")
    print(f"Estrategia:     {simulation.model.strategy_name}")
    print(f"Bomberos:       {len(simulation.model.firefighters)}")
    print()
    print("Endpoints:")
    print(f"  GET  http://localhost:{port}/board")
    print(f"  GET  http://localhost:{port}/state")
    print(f"  POST http://localhost:{port}/step")
    print(f"  POST http://localhost:{port}/reset")
    print()
    print("Ctrl+C para detener.")
    print("=" * 55)

    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        pass

    httpd.server_close()
    print("\nServidor detenido.")


if __name__ == "__main__":
    from sys import argv

    port = DEFAULT_PORT
    strategy_name = DEFAULT_STRATEGY

    # Los argumentos se reconocen por forma: un número es el puerto y
    # un nombre es la estrategia, en cualquier orden.
    for arg in argv[1:]:
        if arg.isdigit():
            port = int(arg)
        else:
            strategy_name = arg

    if strategy_name not in STRATEGIES:
        print(f"Estrategia desconocida: {strategy_name}")
        print(f"Opciones: {', '.join(STRATEGIES)}")
        raise SystemExit(1)

    run(port=port, strategy_name=strategy_name)
