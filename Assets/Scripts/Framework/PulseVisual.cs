using UnityEngine;

/// <summary>
/// Latido visual barato para marcadores del tablero.
///
/// Sirve para que un POI sin revelar, una salida o una víctima llamen
/// la atención desde la cámara cenital sin costar nada: no usa luces
/// en tiempo real, solo escala y, si el material lo permite, sube y
/// baja la emisión.
///
/// Va en el prefab del marcador. Se puede quitar sin romper nada.
/// </summary>
[DisallowMultipleComponent]
public class PulseVisual : MonoBehaviour
{
    [Header("Escala")]
    [Tooltip("Cuánto crece y encoge, en fracción de su tamaño.")]
    [Range(0f, 0.5f)]
    public float amplitud = 0.09f;

    [Tooltip("Latidos por segundo.")]
    public float velocidad = 1.6f;

    [Tooltip("Rota sobre su eje. Útil para marcadores de búsqueda.")]
    public float gradosPorSegundo = 0f;

    [Header("Flotación")]
    [Tooltip("Sube y baja. 0 lo deja quieto.")]
    public float alturaFlotacion = 0f;

    [Header("Emisión")]
    [Tooltip("Hace latir la emisión del material. Requiere Standard con Emission activada.")]
    public bool pulsarEmision = false;

    [Range(0f, 4f)]
    public float emisionMinima = 0.6f;

    [Range(0f, 6f)]
    public float emisionMaxima = 2.0f;

    // ---------------------------------------------------------

    private Vector3 escalaBase;
    private Vector3 posicionBase;
    private float desfase;

    private Renderer render;
    private MaterialPropertyBlock bloque;
    private Color colorEmision = Color.white;
    private bool tieneEmision;

    private static readonly int IdEmision =
        Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        escalaBase = transform.localScale;
        posicionBase = transform.localPosition;

        // Cada marcador arranca en un punto distinto del ciclo: si todos
        // latieran a la vez el tablero parecería parpadear entero.
        desfase = Random.Range(0f, Mathf.PI * 2f);

        render = GetComponentInChildren<Renderer>();

        if (render != null && pulsarEmision)
        {
            bloque = new MaterialPropertyBlock();

            Material material = render.sharedMaterial;

            if (material != null && material.HasProperty(IdEmision))
            {
                colorEmision = material.GetColor(IdEmision);

                if (colorEmision.maxColorComponent <= 0.001f)
                {
                    colorEmision = Color.white;
                }

                tieneEmision = true;
            }
        }
    }

    void OnEnable()
    {
        transform.localScale = escalaBase;
        transform.localPosition = posicionBase;
    }

    void Update()
    {
        float t = Time.time * velocidad * Mathf.PI * 2f + desfase;
        float onda = (Mathf.Sin(t) + 1f) * 0.5f;

        transform.localScale = escalaBase * (1f + (onda - 0.5f) * 2f * amplitud);

        if (alturaFlotacion > 0f)
        {
            transform.localPosition =
                posicionBase + new Vector3(0f, onda * alturaFlotacion, 0f);
        }

        if (gradosPorSegundo != 0f)
        {
            transform.Rotate(Vector3.up, gradosPorSegundo * Time.deltaTime, Space.Self);
        }

        if (tieneEmision && render != null)
        {
            // MaterialPropertyBlock en vez de tocar el material: así no
            // se crea una instancia de material por objeto ni se
            // modifica el asset compartido.
            float fuerza = Mathf.Lerp(emisionMinima, emisionMaxima, onda);

            render.GetPropertyBlock(bloque);
            bloque.SetColor(IdEmision, colorEmision * fuerza);
            render.SetPropertyBlock(bloque);
        }
    }
}
