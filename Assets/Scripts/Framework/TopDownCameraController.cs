using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cámara superior desplazable para el tablero de Fire Rescue.
///
/// Guarda un punto de enfoque sobre el plano del tablero. WASD mueve ese
/// punto y el zoom cambia la altura; la posición de la cámara se calcula
/// a partir de los dos, así que siempre apunta al mismo sitio. Es una
/// capa visual: si se borra, la simulación sigue funcionando igual.
///
/// Usa el Input System nuevo (Keyboard.current, Mouse.current), que es
/// el que tiene activado el proyecto.
/// </summary>
[RequireComponent(typeof(Camera))]
public class TopDownCameraController : MonoBehaviour
{
    [Header("Desplazamiento")]
    [Tooltip("Unidades de mundo por segundo. Una celda del tablero mide 1 unidad.")]
    public float moveSpeed = 6f;

    [Tooltip("Suavizado del movimiento en segundos. 0 = respuesta inmediata.")]
    [Range(0f, 0.5f)]
    public float smoothTime = 0.12f;

    [Tooltip("El desplazamiento se acelera cuando la cámara está alta, para " +
             "que recorrer el tablero cueste lo mismo de lejos que de cerca.")]
    public bool scaleSpeedWithZoom = true;

    [Header("Zoom")]
    [Tooltip("Qué tanto se acerca por cada muesca de la rueda del mouse.")]
    public float zoomSpeed = 4f;

    [Tooltip("Zoom máximo de acercamiento. Altura en perspectiva, " +
             "orthographicSize si la cámara es ortográfica.")]
    public float minZoom = 3f;

    [Tooltip("Zoom máximo de alejamiento.")]
    public float maxZoom = 14f;

    [Tooltip("Zoom al arrancar. Se ajusta solo entre minZoom y maxZoom.")]
    public float startZoom = 9f;

    [Header("Ángulo")]
    [Tooltip("90 = cenital pura. Entre 55 y 70 se ven mejor los modelos 3D " +
             "sin perder la lectura de tablero.")]
    [Range(30f, 90f)]
    public float pitch = 62f;

    [Header("Límites del tablero")]
    [Tooltip("Lee filas, columnas y cellSize del BoardManager de la escena. " +
             "Si no lo encuentra usa los valores de abajo.")]
    public bool autoFitToBoard = true;

    [Tooltip("Esquina del tablero con menor X y menor Z, en coordenadas de mundo.")]
    public Vector2 boardMin = new Vector2(0f, -5f);

    [Tooltip("Esquina del tablero con mayor X y mayor Z, en coordenadas de mundo.")]
    public Vector2 boardMax = new Vector2(7f, 0f);

    [Tooltip("Cuánto se puede sacar el enfoque fuera del tablero, en celdas. " +
             "Sirve para ver los bordes cómodamente.")]
    public float margin = 2f;

    [Header("Teclas")]
    [Tooltip("Vuelve al centro del tablero. Si queda en None se usa F.")]
    public Key recenterKey = Key.F;

    // ---------------------------------------------------------
    // Estado interno
    // ---------------------------------------------------------

    private Camera cam;

    // Punto del plano del tablero al que apunta la cámara.
    private Vector3 focus;

    // Destino del enfoque y del zoom. El valor real los persigue con
    // suavizado, y por eso hay dos variables de cada uno.
    private Vector3 targetFocus;
    private float zoom;
    private float targetZoom;

    private Vector3 focusVelocity;
    private float zoomVelocity;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (autoFitToBoard)
        {
            FitBoundsToBoard();
        }

        targetZoom = Mathf.Clamp(startZoom, minZoom, maxZoom);
        zoom = targetZoom;

        targetFocus = BoardCenter();
        focus = targetFocus;

        ApplyToCamera();
    }

    // LateUpdate para que la cámara se coloque después de que el
    // BoardManager haya movido lo que tuviera que mover en ese frame.
    void LateUpdate()
    {
        ReadInput();

        focus = Vector3.SmoothDamp(focus, targetFocus, ref focusVelocity, smoothTime);
        zoom = Mathf.SmoothDamp(zoom, targetZoom, ref zoomVelocity, smoothTime);

        ApplyToCamera();
    }

    // ---------------------------------------------------------
    // Entrada
    // ---------------------------------------------------------

    private void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            float x = 0f;
            float z = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) z -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) z += 1f;

            Vector3 direction = new Vector3(x, 0f, z);

            // Normalizar evita que moverse en diagonal sea más rápido.
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            float speed = moveSpeed;

            if (scaleSpeedWithZoom)
            {
                speed *= zoom / Mathf.Max(startZoom, 0.001f);
            }

            targetFocus += direction * speed * Time.unscaledDeltaTime;

            // Si el campo quedó vacío en el Inspector se usa F, para que
            // recentrar siempre tenga una tecla asignada.
            Key key = recenterKey == Key.None ? Key.F : recenterKey;

            if (keyboard[key].wasPressedThisFrame)
            {
                targetFocus = BoardCenter();
            }
        }

        Mouse mouse = Mouse.current;

        if (mouse != null)
        {
            targetZoom -= ScrollNotches(mouse) * zoomSpeed;
        }

        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        targetFocus = ClampToBounds(targetFocus);
    }

    /// <summary>
    /// Rueda del mouse normalizada a más o menos 1 por muesca.
    ///
    /// Windows manda 120 por muesca y algunos trackpads mandan valores
    /// pequeños, así que sin normalizar el zoom se sentiría distinto en
    /// cada equipo.
    /// </summary>
    private float ScrollNotches(Mouse mouse)
    {
        float raw = mouse.scroll.ReadValue().y;

        if (Mathf.Abs(raw) > 1f)
        {
            raw /= 120f;
        }

        return Mathf.Clamp(raw, -1f, 1f);
    }

    // ---------------------------------------------------------
    // Colocación de la cámara
    // ---------------------------------------------------------

    private void ApplyToCamera()
    {
        if (cam.orthographic)
        {
            // En ortográfica el zoom es el tamaño de la vista, no la
            // altura. La cámara se pone alta y fija para no cortar nada.
            cam.orthographicSize = zoom;

            float height = Mathf.Max(maxZoom * 2f, 20f);
            transform.position = OffsetFromFocus(height);
        }
        else
        {
            // En perspectiva el zoom es la altura sobre el tablero.
            transform.position = OffsetFromFocus(zoom);
        }

        transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    /// <summary>
    /// Posición de la cámara para mirar el punto de enfoque desde cierta
    /// altura con la inclinación configurada.
    ///
    /// Con pitch de 90 grados la cámara queda justo encima. Con menos,
    /// se retrasa en Z lo necesario para que el enfoque siga quedando en
    /// el centro de la pantalla.
    /// </summary>
    private Vector3 OffsetFromFocus(float height)
    {
        float radians = Mathf.Deg2Rad * Mathf.Clamp(pitch, 30f, 90f);
        float back = height / Mathf.Tan(radians);

        return new Vector3(focus.x, height, focus.z - back);
    }

    // ---------------------------------------------------------
    // Límites
    // ---------------------------------------------------------

    private Vector3 ClampToBounds(Vector3 point)
    {
        point.x = Mathf.Clamp(point.x, boardMin.x - margin, boardMax.x + margin);
        point.y = 0f;
        point.z = Mathf.Clamp(point.z, boardMin.y - margin, boardMax.y + margin);

        return point;
    }

    private Vector3 BoardCenter()
    {
        return new Vector3(
            (boardMin.x + boardMax.x) * 0.5f,
            0f,
            (boardMin.y + boardMax.y) * 0.5f
        );
    }

    /// <summary>
    /// Calcula los límites leyendo el BoardManager de la escena.
    ///
    /// Solo lee sus campos públicos, no lo modifica. BoardManager coloca
    /// cada celda en (columna * cellSize, y, -fila * cellSize), así que
    /// el tablero ocupa X de 0 a (columnas-1)*cellSize y Z de
    /// -(filas-1)*cellSize a 0.
    ///
    /// Si no hay BoardManager en la escena se quedan los valores del
    /// Inspector y se avisa por consola, sin romper nada.
    /// </summary>
    private void FitBoundsToBoard()
    {
        BoardManager board = FindFirstObjectByType<BoardManager>();

        if (board == null)
        {
            Debug.LogWarning(
                "TopDownCameraController: no encontré un BoardManager en la " +
                "escena, uso los límites del Inspector.",
                this
            );

            return;
        }

        float width = (board.columns - 1) * board.cellSize;
        float depth = (board.rows - 1) * board.cellSize;

        boardMin = new Vector2(0f, -depth);
        boardMax = new Vector2(width, 0f);
    }

    // ---------------------------------------------------------
    // Ayuda visual en el editor
    // ---------------------------------------------------------

    /// <summary>
    /// Dibuja el área del tablero y el área que puede recorrer el
    /// enfoque, para poder verlas en la pestaña Scene sin darle Play.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Vector3 boardCenter = new Vector3(
            (boardMin.x + boardMax.x) * 0.5f,
            0f,
            (boardMin.y + boardMax.y) * 0.5f
        );

        Vector3 boardSize = new Vector3(
            boardMax.x - boardMin.x,
            0.05f,
            boardMax.y - boardMin.y
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(boardCenter, boardSize);

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.7f);
        Gizmos.DrawWireCube(
            boardCenter,
            boardSize + new Vector3(margin * 2f, 0f, margin * 2f)
        );
    }
}
