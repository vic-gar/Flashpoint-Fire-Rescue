using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interfaz de mando de Fire Rescue.
///
/// Responsabilidad: leer el estado que YA recibe SimulationClient y
/// pintarlo. No abre ninguna conexión propia, no habla con Python y no
/// conoce las reglas. Si se borra este componente la simulación sigue
/// funcionando exactamente igual.
///
/// Todas las referencias se asignan desde el Inspector. El HUD son
/// GameObjects reales bajo Canvases/GameHUD: se pueden mover, recolorear
/// y reescribir sin tocar este archivo. Aquí solo vive el comportamiento
/// (números, barras, colores y microanimaciones).
///
/// Construido con el menú Tools > Fire Rescue > Construir HUD, que crea
/// la jerarquía y deja este componente ya cableado.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Fuente de datos")]
    [Tooltip("Si se deja vacío se busca el SimulationClient de la escena.")]
    public SimulationClient client;

    [Header("Cabecera")]
    public TMP_Text turnoTexto;
    public TMP_Text estrategiaTexto;
    public TMP_Text estadoTexto;
    public Image estadoPunto;

    [Header("Rescatadas")]
    public TMP_Text rescatadasTexto;
    public Image rescatadasBarra;
    public RectTransform rescatadasPanel;
    public Image rescatadasFlash;

    [Header("Perdidas")]
    public TMP_Text perdidasTexto;
    public Image perdidasBarra;
    public RectTransform perdidasPanel;
    public Image perdidasFlash;

    [Header("Integridad estructural")]
    public TMP_Text danioTexto;
    public TMP_Text danioEtiqueta;
    public Image danioBarra;
    public RectTransform danioPanel;
    public Image danioFlash;

    [Header("Pantalla final")]
    public CanvasGroup overlay;
    public RectTransform overlayCaja;
    public TMP_Text overlayTitulo;
    public TMP_Text overlaySubtitulo;
    public TMP_Text overlayEstadisticas;
    public Image overlayAcento;

    [Header("Límites de la partida")]
    [Tooltip("Víctimas que hay que rescatar para ganar.")]
    public int objetivoRescate = 7;

    [Tooltip("Víctimas perdidas que provocan la derrota.")]
    public int limitePerdidas = 4;

    [Tooltip("Marcadores de daño que derrumban el edificio.")]
    public int limiteDanio = 24;

    [Header("Paleta")]
    public Color colorSeguro = new Color(0.35f, 0.83f, 0.58f);
    public Color colorAviso = new Color(0.96f, 0.72f, 0.25f);
    public Color colorCritico = new Color(0.95f, 0.35f, 0.25f);
    public Color colorInfo = new Color(0.36f, 0.72f, 0.94f);

    // ---------------------------------------------------------
    // Estado interno
    // ---------------------------------------------------------

    /// <summary>
    /// Si la partida sigue corriendo.
    ///
    /// OJO: SimulationState tiene su propio EnCurso(), pero aquí no se
    /// guarda el estado completo, solo los contadores que SimulationClient
    /// copia en cada respuesta. Por eso se compara contra la cadena que
    /// manda el servidor ("en_curso", "victoria", "derrota_victimas" o
    /// "derrota_colapso"), que es la misma que usa SimulationState.
    /// </summary>
    private bool EnCurso()
    {
        return client != null && client.ultimoResultado == "en_curso";
    }

    /// <summary>
    /// Si la partida YA TERMINÓ de verdad.
    ///
    /// No es lo mismo que "no está en curso". Antes de la primera respuesta
    /// del servidor, SimulationClient.ultimoResultado vale "sin conectar",
    /// que tampoco es "en_curso"; con la condición anterior la pantalla
    /// final salía nada más darle Play, con todos los contadores en cero.
    ///
    /// Aquí se comparan solo los tres estados terminales que manda el
    /// servidor. Son los mismos que usa flashpoint_model.py, así que si
    /// alguna vez cambian allá hay que cambiarlos aquí.
    /// </summary>
    private bool Termino()
    {
        if (client == null)
        {
            return false;
        }

        return client.ultimoResultado == "victoria"
            || client.ultimoResultado == "derrota_colapso"
            || client.ultimoResultado == "derrota_victimas";
    }

    private int rescatadasPrevias = -1;
    private int perdidasPrevias = -1;
    private int danioPrevio = -1;

    private float rescatadasSuave;
    private float perdidasSuave;
    private float danioSuave;

    private bool overlayMostrado;
    private float pulso;

    void Awake()
    {
        if (client == null)
        {
            client = FindAnyObjectByType<SimulationClient>();
        }

        if (overlay != null)
        {
            overlay.alpha = 0f;
            overlay.gameObject.SetActive(false);
        }

        ApagarFlashes();
    }

    void Update()
    {
        if (client == null)
        {
            return;
        }

        DetectarCambios();
        InterpolarBarras();
        PintarCabecera();
        LatirSiCritico();
    }

    // ---------------------------------------------------------
    // Cambios y feedback
    // ---------------------------------------------------------

    private void DetectarCambios()
    {
        // La primera lectura no dispara feedback: si no, al conectarse
        // parpadearía todo de golpe sin que haya pasado nada.
        bool primeraVez =
            rescatadasPrevias < 0 ||
            perdidasPrevias < 0 ||
            danioPrevio < 0;

        if (primeraVez)
        {
            rescatadasPrevias = client.rescatadas;
            perdidasPrevias = client.perdidas;
            danioPrevio = client.danio;

            rescatadasSuave = client.rescatadas;
            perdidasSuave = client.perdidas;
            danioSuave = client.danio;

            return;
        }

        if (client.rescatadas > rescatadasPrevias)
        {
            Golpe(rescatadasPanel);
            Destello(rescatadasFlash, colorSeguro);
        }

        if (client.perdidas > perdidasPrevias)
        {
            Golpe(perdidasPanel);
            Destello(perdidasFlash, colorCritico);
        }

        if (client.danio > danioPrevio)
        {
            Golpe(danioPanel, 1.03f);
            Destello(danioFlash, colorCritico, 0.35f);
        }

        rescatadasPrevias = client.rescatadas;
        perdidasPrevias = client.perdidas;
        danioPrevio = client.danio;

        if (Termino() && !overlayMostrado)
        {
            MostrarOverlay();
        }
    }

    // ---------------------------------------------------------
    // Barras y textos
    // ---------------------------------------------------------

    private void InterpolarBarras()
    {
        float v = 6f * Time.unscaledDeltaTime;

        rescatadasSuave = Mathf.Lerp(rescatadasSuave, client.rescatadas, v);
        perdidasSuave = Mathf.Lerp(perdidasSuave, client.perdidas, v);
        danioSuave = Mathf.Lerp(danioSuave, client.danio, v);

        Rellenar(rescatadasBarra, rescatadasSuave, objetivoRescate);
        Rellenar(perdidasBarra, perdidasSuave, limitePerdidas);
        Rellenar(danioBarra, danioSuave, limiteDanio);

        Escribir(rescatadasTexto, $"{client.rescatadas} / {objetivoRescate}");
        Escribir(perdidasTexto, $"{client.perdidas} / {limitePerdidas}");
        Escribir(danioTexto, $"{client.danio} / {limiteDanio}");

        if (rescatadasBarra != null)
        {
            rescatadasBarra.color = colorSeguro;
        }

        if (perdidasBarra != null)
        {
            perdidasBarra.color = Color.Lerp(
                colorAviso,
                colorCritico,
                Mathf.Clamp01((float)client.perdidas / limitePerdidas)
            );
        }

        PintarDanio();
    }

    /// <summary>
    /// El daño es el indicador que más dice de la partida, así que
    /// cambia de color y de etiqueta por tramos en vez de ser una barra
    /// que solo crece.
    /// </summary>
    private void PintarDanio()
    {
        int d = client.danio;

        string etiqueta;
        Color color;

        if (d >= limiteDanio)
        {
            etiqueta = "COLAPSO";
            color = colorCritico;
        }
        else if (d >= limiteDanio - 7)
        {
            etiqueta = "CRÍTICO";
            color = colorCritico;
        }
        else if (d >= limiteDanio / 2 - 3)
        {
            etiqueta = "RIESGO";
            color = colorAviso;
        }
        else
        {
            etiqueta = "ESTABLE";
            color = colorSeguro;
        }

        Escribir(danioEtiqueta, etiqueta);

        if (danioBarra != null)
        {
            danioBarra.color = color;
        }
    }

    private void PintarCabecera()
    {
        Escribir(turnoTexto, client.turno.ToString("D3"));
        Escribir(estrategiaTexto, NombreEstrategia(client.estrategiaActiva));

        string estado;
        Color color;

        switch (client.ultimoResultado)
        {
            case "victoria":
                estado = "MISIÓN CUMPLIDA";
                color = colorSeguro;
                break;

            case "derrota_colapso":
                estado = "COLAPSO ESTRUCTURAL";
                color = colorCritico;
                break;

            case "derrota_victimas":
                estado = "DEMASIADAS VÍCTIMAS";
                color = colorCritico;
                break;

            case "en_curso":
                estado = "OPERACIÓN EN CURSO";
                color = colorInfo;
                break;

            default:
                // Mientras no llega la primera respuesta del servidor.
                estado = "ESPERANDO SERVIDOR";
                color = new Color(0.62f, 0.66f, 0.72f);
                break;
        }

        Escribir(estadoTexto, estado);

        if (estadoPunto != null)
        {
            estadoPunto.color = color;
        }
    }

    private string NombreEstrategia(string valor)
    {
        switch (valor)
        {
            case "aleatoria":
                return "ALEATORIA";

            case "mejorada":
                return "MEJORADA";

            case "mejorada_sin_coordinacion":
                return "MEJORADA SIN COORDINACIÓN";

            default:
                return string.IsNullOrEmpty(valor) ? "..." : valor.ToUpper();
        }
    }

    // ---------------------------------------------------------
    // Latido cuando el edificio está por caerse
    // ---------------------------------------------------------

    private void LatirSiCritico()
    {
        if (danioPanel == null)
        {
            return;
        }

        bool critico =
            EnCurso() &&
            client.danio >= limiteDanio - 7;

        if (!critico)
        {
            pulso = Mathf.Lerp(pulso, 0f, 8f * Time.unscaledDeltaTime);
        }
        else
        {
            pulso = (Mathf.Sin(Time.unscaledTime * 5f) + 1f) * 0.5f;
        }

        float escala = 1f + pulso * 0.012f;

        danioPanel.localScale = new Vector3(escala, escala, 1f);

        if (danioFlash != null && critico)
        {
            Color c = colorCritico;
            c.a = pulso * 0.10f;
            danioFlash.color = c;
        }
    }

    // ---------------------------------------------------------
    // Pantalla final
    // ---------------------------------------------------------

    private void MostrarOverlay()
    {
        overlayMostrado = true;

        if (overlay == null)
        {
            return;
        }

        string titulo;
        string subtitulo;
        Color acento;

        switch (client.ultimoResultado)
        {
            case "victoria":
                titulo = "MISIÓN CUMPLIDA";
                subtitulo = "Civiles rescatados. Estructura estable.";
                acento = colorSeguro;
                break;

            case "derrota_colapso":
                titulo = "COLAPSO ESTRUCTURAL";
                subtitulo = "El edificio cedió. Misión fallida.";
                acento = colorCritico;
                break;

            case "derrota_victimas":
                titulo = "DEMASIADAS VÍCTIMAS";
                subtitulo = "Se perdieron cuatro civiles. Misión fallida.";
                acento = colorCritico;
                break;

            default:
                // No debería llegar aquí: Termino() filtra los estados que
                // no son de fin de partida.
                titulo = "PARTIDA TERMINADA";
                subtitulo = "";
                acento = colorInfo;
                break;
        }

        Escribir(overlayTitulo, titulo);
        Escribir(overlaySubtitulo, subtitulo);

        // Dos columnas dentro de un solo objeto de texto. La etiqueta
        // <pos=> de TextMeshPro corre el cursor a una posición fija en
        // píxeles, así que las cifras quedan alineadas entre sí sin tener
        // que crear cinco objetos separados.
        //
        // El objeto de texto está en gris y solo el dato va en blanco: es
        // lo que hace que se lea la cifra y no la etiqueta.
        string a = "<pos=420><color=#F0F3F6><b>";
        string b = "</b></color>";

        Escribir(
            overlayEstadisticas,
            $"TURNOS{a}{client.turno}{b}\n" +
            $"CIVILES RESCATADOS{a}{client.rescatadas} / {objetivoRescate}{b}\n" +
            $"CIVILES PERDIDOS{a}{client.perdidas} / {limitePerdidas}{b}\n" +
            $"DAÑO ESTRUCTURAL{a}{client.danio} / {limiteDanio}{b}\n" +
            $"ESTRATEGIA{a}{NombreEstrategia(client.estrategiaActiva)}{b}"
        );

        if (overlayTitulo != null)
        {
            overlayTitulo.color = acento;
        }

        if (overlayAcento != null)
        {
            overlayAcento.color = acento;
        }

        overlay.gameObject.SetActive(true);

        StartCoroutine(EntrarOverlay());
    }

    private IEnumerator EntrarOverlay()
    {
        float t = 0f;

        while (t < 1f)
        {
            // unscaledDeltaTime para que la animación funcione aunque
            // alguien pause el tiempo desde el editor.
            t += Time.unscaledDeltaTime * 2.2f;

            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

            overlay.alpha = k;

            if (overlayCaja != null)
            {
                float s = Mathf.Lerp(0.92f, 1f, k);
                overlayCaja.localScale = new Vector3(s, s, 1f);
            }

            yield return null;
        }

        overlay.alpha = 1f;

        if (overlayCaja != null)
        {
            overlayCaja.localScale = Vector3.one;
        }
    }

    // ---------------------------------------------------------
    // Microanimaciones
    // ---------------------------------------------------------

    private void Golpe(RectTransform objetivo, float fuerza = 1.06f)
    {
        if (objetivo == null)
        {
            return;
        }

        StopCoroutine(nameof(RutinaGolpe));
        StartCoroutine(RutinaGolpe(objetivo, fuerza));
    }

    private IEnumerator RutinaGolpe(RectTransform objetivo, float fuerza)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 5f;

            // Sube rápido y regresa: el clásico "scale punch".
            float k = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            float s = Mathf.Lerp(1f, fuerza, k);

            objetivo.localScale = new Vector3(s, s, 1f);

            yield return null;
        }

        objetivo.localScale = Vector3.one;
    }

    private void Destello(Image capa, Color color, float alfa = 0.55f)
    {
        if (capa == null)
        {
            return;
        }

        StartCoroutine(RutinaDestello(capa, color, alfa));
    }

    private IEnumerator RutinaDestello(Image capa, Color color, float alfa)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 2.4f;

            Color c = color;
            c.a = Mathf.Lerp(alfa, 0f, Mathf.Clamp01(t));

            capa.color = c;

            yield return null;
        }

        Color fin = color;
        fin.a = 0f;
        capa.color = fin;
    }

    private void ApagarFlashes()
    {
        foreach (Image capa in new[]
        {
            rescatadasFlash,
            perdidasFlash,
            danioFlash
        })
        {
            if (capa == null)
            {
                continue;
            }

            Color c = capa.color;
            c.a = 0f;
            capa.color = c;
        }
    }

    // ---------------------------------------------------------
    // Utilidades
    // ---------------------------------------------------------

    private void Escribir(TMP_Text campo, string valor)
    {
        if (campo != null && campo.text != valor)
        {
            campo.text = valor;
        }
    }

    private void Rellenar(Image barra, float valor, int maximo)
    {
        if (barra == null || maximo <= 0)
        {
            return;
        }

        barra.fillAmount = Mathf.Clamp01(valor / maximo);
    }
}
