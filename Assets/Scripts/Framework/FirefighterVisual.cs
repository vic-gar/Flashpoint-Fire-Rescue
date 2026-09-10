using UnityEngine;

/// <summary>
/// Capa visual del bombero: suaviza el movimiento sin tocar la lógica.
///
/// El problema que resuelve: BoardManager coloca al bombero con
/// transform.position = ..., así que el objeto salta de una celda a
/// otra de golpe. La posición lógica tiene que seguir siendo esa, sin
/// retraso, porque de ella dependen las reglas.
///
/// La solución: el objeto raíz sigue teletransportándose (lógica
/// correcta e inmediata) y este componente hace que el hijo "Visual"
/// persiga esa posición con suavizado, gire hacia donde va y rebote un
/// poco al caminar. Lo que se ve es fluido; lo que se calcula no cambia.
///
/// Va en el prefab Firefighter, sobre el objeto raíz. Si se borra, el
/// bombero vuelve a saltar pero la simulación sigue igual de correcta.
/// </summary>
public class FirefighterVisual : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Hijo que contiene el modelo. Si se deja vacío se usa el primer hijo.")]
    public Transform visual;

    [Header("Movimiento")]
    [Tooltip("Segundos que tarda el modelo en alcanzar la celda nueva.")]
    [Range(0.05f, 0.6f)]
    public float suavizado = 0.18f;

    [Tooltip("Grados por segundo al girar hacia la dirección de avance.")]
    public float velocidadGiro = 720f;

    [Header("Caminata")]
    [Tooltip("Altura del rebote al desplazarse, en unidades de mundo.")]
    public float alturaRebote = 0.045f;

    [Tooltip("Pasos por segundo del rebote.")]
    public float frecuenciaRebote = 7.5f;

    [Tooltip("Inclinación hacia adelante mientras avanza, en grados.")]
    public float inclinacion = 7f;

    [Tooltip("Segundos de retraso por bombero. El bombero 1 arranca de " +
             "inmediato, el 2 un poco después, y así. Con 0 salen todos a " +
             "la vez.")]
    [Range(0f, 0.25f)]
    public float escalonPorId = 0.07f;

    [Header("Identidad del bombero")]
    [Tooltip("Un material por bombero. El id del bombero elige cuál. " +
             "Los llena el menú Tools > Fire Rescue.")]
    public Material[] coloresPorId;

    [Tooltip("Dónde buscar. Vacío = todo el hijo Visual. Dentro de cada " +
             "renderer solo cambian los slots con el prefijo de abajo.")]
    public Renderer[] partesDeColor;

    [Tooltip("Forzar un id concreto. -1 lo deduce del FirefighterView.")]
    public int idForzado = -1;

    [Tooltip("Solo se recolorean los materiales cuyo nombre empiece así.")]
    public string prefijoRecoloreable = "M_Bombero";

    // ---------------------------------------------------------

    private Vector3 posicionSuave;
    private Vector3 velocidad;

    // Destino retrasado. El servidor manda un turno completo de golpe, así
    // que los seis bomberos cambian de celda en el mismo frame y el ojo no
    // alcanza a seguir a ninguno. Retrasar unas centésimas el arranque de
    // cada uno los desfasa lo justo para poder mirarlos uno por uno.
    //
    // Solo afecta al hijo Visual. La posición lógica del objeto raíz
    // cambia igual de inmediato que antes, así que no se altera ninguna
    // regla ni ningún resultado.
    private Vector3 destinoRetrasado;
    private Vector3 destinoPendiente;
    private float tiempoDeSalida;
    private int miId = 1;
    private float faseRebote;
    private Quaternion giroObjetivo = Quaternion.identity;

    void Awake()
    {
        if (visual == null && transform.childCount > 0)
        {
            visual = transform.GetChild(0);
        }

        posicionSuave = transform.position;
        destinoRetrasado = transform.position;
        destinoPendiente = transform.position;
        giroObjetivo = transform.rotation;
    }

    // Start y no Awake: BoardManager llama a FirefighterView.Initialize
    // justo después de instanciar, y ahí es donde el objeto recibe su
    // nombre con el id. En Awake todavía no lo tiene.
    void Start()
    {
        miId = idForzado >= 0 ? idForzado : DeducirId();
        AplicarColor(miId);
    }

    /// <summary>
    /// Pinta la casaca y el aro con el color que le toca a este bombero.
    /// Sin esto los seis salen del mismo color y no se distinguen entre
    /// sí, que es justo lo que hay que poder ver desde la cámara.
    /// </summary>
    public void AplicarColor(int id)
    {
        if (coloresPorId == null || coloresPorId.Length == 0)
        {
            return;
        }

        // Los ids de bombero del proyecto van de 1 a 6, no de 0 a 5.
        // Lo fija BoardFileReader.cs (new FirefighterData(1..6)) y lo
        // confirma el servidor en flashpoint_model.py
        // (firefighter_id = i + 1). Por eso se resta 1 antes de indexar:
        // con un módulo directo, el bombero 1 se llevaría el color 2 y
        // el 6 se llevaría el del 1.
        int indice = Mathf.Max(0, id - 1) % coloresPorId.Length;

        Material material = coloresPorId[indice];

        if (material == null)
        {
            return;
        }

        Renderer[] objetivos = partesDeColor;

        if (objetivos == null || objetivos.Length == 0)
        {
            objetivos = visual != null
                ? visual.GetComponentsInChildren<Renderer>(true)
                : GetComponentsInChildren<Renderer>(true);
        }

        // Se cambia SLOT por SLOT, no el renderer entero. El modelo del
        // bombero trae casaca, casco, tanque, guantes y piel en un solo
        // renderer con varios materiales: reemplazarlos todos lo dejaría
        // como una figura de un color plano.
        foreach (Renderer r in objetivos)
        {
            if (r == null)
            {
                continue;
            }

            Material[] mats = r.sharedMaterials;
            bool cambio = false;

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null &&
                    mats[i].name.StartsWith(prefijoRecoloreable))
                {
                    mats[i] = material;
                    cambio = true;
                }
            }

            if (cambio)
            {
                r.sharedMaterials = mats;
            }
        }
    }

    /// <summary>
    /// El id sale del nombre que le pone FirefighterView
    /// ("Firefighter_3"). Se lee del nombre y no del componente para no
    /// depender de que sus datos internos ya estén asignados.
    /// </summary>
    private int DeducirId()
    {
        string nombre = gameObject.name;

        int guion = nombre.LastIndexOf('_');

        if (guion >= 0 && guion < nombre.Length - 1)
        {
            if (int.TryParse(nombre.Substring(guion + 1), out int id))
            {
                return id;
            }
        }

        // Sin nombre útil, el orden dentro del contenedor sirve igual.
        // Se suma 1 para respetar la misma convención de 1 a N.
        return transform.GetSiblingIndex() + 1;
    }

    void OnEnable()
    {
        // Al reaparecer no debe venir arrastrando la posición vieja.
        posicionSuave = transform.position;
        destinoRetrasado = transform.position;
        destinoPendiente = transform.position;
        velocidad = Vector3.zero;
    }

    // LateUpdate para correr después de que BoardManager ya movió la
    // raíz en este frame. Si fuera Update, el visual perseguiría la
    // posición del frame anterior y se vería un tirón.
    void LateUpdate()
    {
        if (visual == null)
        {
            return;
        }

        // El destino real se guarda y se suelta unas centésimas después,
        // distintas para cada bombero.
        if (transform.position != destinoPendiente)
        {
            destinoPendiente = transform.position;
            tiempoDeSalida =
                Time.time + escalonPorId * Mathf.Max(0, miId - 1);
        }

        if (Time.time >= tiempoDeSalida)
        {
            destinoRetrasado = destinoPendiente;
        }

        Vector3 destino = destinoRetrasado;
        Vector3 anterior = posicionSuave;

        posicionSuave = Vector3.SmoothDamp(
            posicionSuave,
            destino,
            ref velocidad,
            suavizado
        );

        Vector3 avance = posicionSuave - anterior;
        avance.y = 0f;

        float rapidez = avance.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        bool caminando = rapidez > 0.05f;

        // Rebote: solo avanza la fase mientras se mueve, así se detiene
        // en el suelo en vez de quedarse flotando a media zancada.
        if (caminando)
        {
            faseRebote += Time.deltaTime * frecuenciaRebote;
        }
        else
        {
            faseRebote = Mathf.Lerp(faseRebote, 0f, 10f * Time.deltaTime);
        }

        float rebote = Mathf.Abs(Mathf.Sin(faseRebote)) * alturaRebote;

        if (!caminando)
        {
            rebote = 0f;
        }

        visual.position = posicionSuave + new Vector3(0f, rebote, 0f);

        // Giro hacia la dirección de avance
        if (caminando && avance.sqrMagnitude > 1e-8f)
        {
            giroObjetivo = Quaternion.LookRotation(avance.normalized, Vector3.up);
        }

        Quaternion conInclinacion = giroObjetivo;

        if (caminando)
        {
            conInclinacion = giroObjetivo * Quaternion.Euler(inclinacion, 0f, 0f);
        }

        visual.rotation = Quaternion.RotateTowards(
            visual.rotation,
            conInclinacion,
            velocidadGiro * Time.deltaTime
        );
    }
}
