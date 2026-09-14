using System;

/// <summary>
/// Clases que reflejan el JSON que envía el servidor de Python.
///
/// Son solo datos, sin lógica. SimulationClient.cs convierte el texto
/// JSON en estas clases con JsonUtility y BoardManager.ApplyState las
/// lee para dibujar.
///
/// Cada campo tiene que llamarse igual que la llave del JSON que arma
/// FlashPointModel.get_state(). JsonUtility no avisa si un nombre no
/// coincide: deja el campo en su valor por defecto y la escena se vería
/// mal sin ningún error. Por eso los nombres están en español, como en
/// el servidor, y hay una prueba en test_server.py que lee este archivo
/// y compara campo por campo. Renombrar aquí obliga a cambiar el
/// servidor y volver a correr esa prueba.
///
/// Siguen la plantilla cliente-servidor vista en TC2008B.
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
