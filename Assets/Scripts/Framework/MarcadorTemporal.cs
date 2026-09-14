using UnityEngine;

/// <summary>
/// Marcador que se muestra un momento y se borra solo.
///
/// Para qué existe: un `/step` del servidor es un turno completo. Dentro
/// del mismo turno un bombero puede llegar a un POI, revelarlo y cargar a
/// la víctima. Unity solo recibe el estado del final del turno, así que
/// esa víctima nunca aparece en ningún estado y el espectador no llega a
/// verla nunca. El resultado sube a "rescatadas" sin que se haya visto a
/// nadie.
///
/// Este componente dibuja la víctima encontrada durante menos de un
/// segundo, en la celda donde la levantaron, subiendo y encogiendo hasta
/// desaparecer. Es puramente visual: no está en ningún registro de
/// BoardManager, no se sincroniza con nada y no retrasa a Python.
///
/// Se encoge en vez de desvanecerse porque el material es opaco y
/// bajarle el alfa exigiría instanciar un material por copia.
/// </summary>
public class MarcadorTemporal : MonoBehaviour
{
    [Tooltip("Segundos que dura visible antes de borrarse.")]
    public float duracion = 0.9f;

    [Tooltip("Cuánto sube durante ese tiempo, en unidades de mundo.")]
    public float subida = 0.55f;

    [Tooltip("Tamaño al aparecer, respecto al del prefab.")]
    public float escalaInicial = 0.7f;

    [Tooltip("Tamaño máximo antes de encogerse.")]
    public float escalaPico = 1.15f;

    private float tiempo;
    private Vector3 origen;
    private Vector3 escalaBase;

    void Start()
    {
        origen = transform.position;
        escalaBase = transform.localScale;
    }

    void Update()
    {
        tiempo += Time.deltaTime;

        float t = Mathf.Clamp01(tiempo / Mathf.Max(duracion, 0.01f));

        // Sube desacelerando: rápido al principio, casi quieto al final.
        transform.position = origen +
            new Vector3(0f, subida * (1f - Mathf.Pow(1f - t, 2.2f)), 0f);

        // Crece de golpe y después se cierra. El pico temprano es lo que
        // hace que se note aunque dure menos de un segundo.
        float e;

        if (t < 0.22f)
        {
            e = Mathf.Lerp(escalaInicial, escalaPico, t / 0.22f);
        }
        else
        {
            e = Mathf.Lerp(escalaPico, 0f, (t - 0.22f) / 0.78f);
        }

        transform.localScale = escalaBase * e;

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
