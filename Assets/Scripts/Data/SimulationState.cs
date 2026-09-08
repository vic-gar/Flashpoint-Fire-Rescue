using System;

/// <summary>
/// Clases que reflejan el JSON que envía el servidor de Python.
///
/// Responsabilidad: solo datos. SimulationClient.cs convierte el texto
/// JSON en estas clases con JsonUtility y BoardManager.ApplyState las
/// lee para dibujar. No tienen lógica a propósito.
///
/// COMPATIBILIDAD CON UNITY: cada campo debe llamarse exactamente igual
/// que la llave del JSON que arma FlashPointModel.get_state() en
/// Server/model/flashpoint_model.py. JsonUtility no avisa si un nombre
/// no coincide: deja el campo en su valor por defecto y la escena se
/// vería mal sin ningún error. Por eso los nombres están en español,
/// igual que en el servidor, y hay una prueba en Server/tests/
/// test_server.py que lee este archivo y compara campo por campo.
/// NO renombrar campos sin cambiar el servidor y correr esa prueba.
///
/// Desarrolladas con apoyo de Claude siguiendo la plantilla
/// cliente-servidor de TC2008B.
///
/// Nota: JsonUtility no sabe deserializar un arreglo suelto en la
/// raíz del JSON. Aquí no hace falta ningún truco porque el servidor
/// siempre responde con un objeto que contiene los arreglos dentro.
/// </summary>

[Serializable]
public class FirefighterState
{
    public int id;

    // Filas y columnas empiezan en 0, igual que en el servidor.
    public int fila;
    public int columna;
    public int ap;
    public bool cargando;
}

[Serializable]
public class CellPosition
{
    public int fila;
    public int columna;
}

[Serializable]
public class POIState
{
    public int fila;
    public int columna;
    public bool revelado;

    // "v" víctima, "f" falsa alarma, "?" mientras siga boca abajo
    public string tipo;
}

[Serializable]
public class DoorState
{
    public int fila1;
    public int columna1;
    public int fila2;
    public int columna2;
    public bool abierta;
    public bool destruida;
}

/// <summary>
/// Estado completo de la partida en un momento dado.
/// Corresponde a lo que devuelve GET /state y POST /step.
/// </summary>
[Serializable]
public class SimulationState
{
    // "en_curso", "victoria", "derrota_victimas" o "derrota_colapso".
    public string resultado;

    // Nombre de la estrategia con la que juega el servidor.
    public string estrategia;

    public int turno;
    public int bombero_actual;

    public int rescatadas;
    public int perdidas;
    public int danio;

    public FirefighterState[] bomberos;
    public CellPosition[] fuegos;
    public CellPosition[] humos;
    public POIState[] pois;
    public DoorState[] puertas;

    public bool EnCurso()
    {
        return resultado == "en_curso";
    }
}
