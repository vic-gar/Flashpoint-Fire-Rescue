using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Cliente que conecta la escena de Unity con el servidor de Python.
///
/// Responsabilidad: hablar HTTP con Server/server.py y entregar cada
/// estado recibido a BoardManager.ApplyState(). No dibuja nada ni
/// conoce las reglas del juego; la simulación completa corre en Python.
/// Es el lado cliente del criterio 4 (cliente-servidor).
///
/// Integración cliente-servidor desarrollada con apoyo de Claude
/// siguiendo la plantilla WebClient.cs vista en TC2008B, con dos
/// correcciones necesarias:
///
/// 1. La plantilla usa EditorJsonUtility, que solo existe dentro del
///    editor y falla en una compilación del juego. Aquí se usa
///    JsonUtility, que funciona en ambos.
/// 2. La plantilla hace una sola petición en Start(). Aquí se pide un
///    turno cada cierto tiempo, que es lo que hace falta para ver la
///    simulación avanzar.
///
/// Flujo: al arrancar manda POST /reset con la estrategia y la semilla
/// del inspector (o GET /state si resetOnConnect está apagado) y luego,
/// en bucle, POST /step cada secondsBetweenSteps segundos. Cada
/// respuesta es el estado completo de la partida (SimulationState.cs).
///
/// COMPATIBILIDAD CON UNITY: el puerto, los nombres de los endpoints y
/// las llaves del JSON deben coincidir con server.py. Cómo usarlo: un
/// GameObject vacío con este componente y un BoardManager en la escena.
/// </summary>
public class SimulationClient : MonoBehaviour
{
    [Header("Servidor")]
    public string host = "http://localhost";

    // COMPATIBILIDAD CON UNITY: el valor que manda es el del inspector,
    // no este. Si la escena guardó otro puerto, hay que cambiarlo ahí.
    public int port = 3000;

    [Tooltip("aleatoria, mejorada o mejorada_sin_coordinacion. Se envía al servidor al reiniciar.")]
    public string strategy = "mejorada";

    [Tooltip("Semilla de la partida. Misma semilla, misma partida.")]
    public int seed = 42;

    [Header("Simulación")]
    [Tooltip("Segundos entre un turno y el siguiente.")]
    public float secondsBetweenSteps = 0.5f;

    [Tooltip("Arranca la simulación sola al entrar a Play.")]
    public bool autoStart = true;

    [Tooltip("Reinicia la partida en el servidor al conectarse.")]
    public bool resetOnConnect = true;

    [Header("Referencias")]
    [Tooltip("Si se deja vacío se busca el BoardManager de la escena.")]
    public BoardManager boardManager;

    [Header("Estado (solo lectura)")]
    public string ultimoResultado = "sin conectar";
    public string estrategiaActiva = "";
    public int turno;
    public int rescatadas;
    public int perdidas;
    public int danio;

    private bool running;

    private string BaseUrl => $"{host}:{port}";

    void Start()
    {
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
        }

        if (boardManager == null)
        {
            Debug.LogError(
                "SimulationClient: no hay BoardManager en la escena.",
                this
            );

            return;
        }

        if (autoStart)
        {
            StartSimulation();
        }
    }

    // =========================================================
    // Control
    // =========================================================

    public void StartSimulation()
    {
        if (running)
        {
            return;
        }

        running = true;
        StartCoroutine(RunSimulation());
    }

    public void StopSimulation()
    {
        running = false;
    }

    // =========================================================
    // Bucle principal
    // =========================================================

    // Corrutina (vista en clase): cada yield espera la respuesta HTTP o
    // el tiempo entre turnos sin congelar la escena.
    private IEnumerator RunSimulation()
    {
        if (resetOnConnect)
        {
            // El servidor acepta la estrategia y la semilla en el reinicio.
            // Se arma el JSON a mano porque JsonUtility no serializa
            // diccionarios y son solo dos campos.
            string body =
                "{\"estrategia\":\"" + strategy + "\"," +
                "\"semilla\":" + seed + "}";

            yield return Post("/reset", OnStateReceived, body);
        }
        else
        {
            yield return Get("/state", OnStateReceived);
        }

        while (running)
        {
            yield return new WaitForSeconds(secondsBetweenSteps);

            if (!running)
            {
                break;
            }

            yield return Post("/step", OnStateReceived);

            if (ultimoResultado != "en_curso")
            {
                Debug.Log(
                    $"SimulationClient: partida terminada ({ultimoResultado}) " +
                    $"en {turno} turnos. Rescatadas: {rescatadas}, " +
                    $"perdidas: {perdidas}, daño: {danio}.",
                    this
                );

                running = false;
            }
        }
    }

    // Convierte el JSON en SimulationState, copia los contadores al
    // inspector (solo para verlos) y le pasa el estado al BoardManager.
    private void OnStateReceived(string json)
    {
        SimulationState state = JsonUtility.FromJson<SimulationState>(json);

        if (state == null)
        {
            Debug.LogError(
                "SimulationClient: no se pudo interpretar la respuesta " +
                "del servidor.",
                this
            );

            return;
        }

        ultimoResultado = state.resultado;
        estrategiaActiva = state.estrategia;
        turno = state.turno;
        rescatadas = state.rescatadas;
        perdidas = state.perdidas;
        danio = state.danio;

        boardManager.ApplyState(state);
    }

    // =========================================================
    // Peticiones HTTP
    // =========================================================

    private IEnumerator Get(string path, System.Action<string> onSuccess)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(BaseUrl + path))
        {
            yield return request.SendWebRequest();

            if (!HandleResult(request, path))
            {
                yield break;
            }

            onSuccess(request.downloadHandler.text);
        }
    }

    private IEnumerator Post(
        string path,
        System.Action<string> onSuccess,
        string jsonBody = null
    )
    {
        using (UnityWebRequest request =
            new UnityWebRequest(BaseUrl + path, "POST"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();

            if (!string.IsNullOrEmpty(jsonBody))
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bytes);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            yield return request.SendWebRequest();

            if (!HandleResult(request, path))
            {
                yield break;
            }

            onSuccess(request.downloadHandler.text);
        }
    }

    private bool HandleResult(UnityWebRequest request, string path)
    {
        if (request.result == UnityWebRequest.Result.Success)
        {
            return true;
        }

        Debug.LogError(
            $"SimulationClient: falló la petición a {path}. " +
            $"{request.error}. Revisa que el servidor de Python esté " +
            $"corriendo en {BaseUrl}.",
            this
        );

        running = false;

        return false;
    }
}
