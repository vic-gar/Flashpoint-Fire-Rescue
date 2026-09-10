using UnityEngine;

/// <summary>
/// Abre y cierra la hoja de la puerta.
///
/// Qué problema resuelve: hasta ahora la puerta era un cubo aplastado que
/// se dibujaba una vez al construir el tablero y no se volvía a tocar. El
/// JSON trae `puertas[i].abierta` y `puertas[i].destruida` desde siempre,
/// pero nadie los leía, así que una puerta abierta y una cerrada se veían
/// exactamente igual. En un juego donde abrir una puerta cuesta puntos de
/// acción, eso es información que el espectador necesita.
///
/// Cómo funciona: la hoja no gira sobre su centro sino sobre un objeto
/// vacío puesto en el canto de las bisagras. Girar ese objeto arrastra la
/// hoja como una puerta de verdad. La rotación se interpola en vez de
/// saltar, que es lo que hace que se lea el movimiento.
///
/// Solo presentación: BoardManager le dice el estado que ya venía en el
/// JSON. No decide nada ni le manda nada al servidor.
/// </summary>
public class PuertaVisual : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Objeto vacío en el canto de las bisagras. La hoja cuelga de él.")]
    public Transform bisagra;

    [Tooltip("La hoja. Se apaga cuando la puerta queda destruida.")]
    public GameObject hoja;

    [Header("Movimiento")]
    [Tooltip("Grados que gira al abrirse. Negativo abre hacia el otro lado.")]
    [Range(-140f, 140f)]
    public float anguloAbierta = -102f;

    [Tooltip("Grados por segundo. Muy alto se ve como salto.")]
    public float velocidad = 240f;

    [Header("Estado")]
    [Tooltip("Lo pone BoardManager con lo que manda el servidor.")]
    public bool abierta;

    [Tooltip("Pared derribada: la hoja desaparece y queda el marco.")]
    public bool destruida;

    void Reset()
    {
        // Para que al agregarlo a mano en el inspector agarre lo obvio.
        bisagra = transform.Find("Bisagra");

        if (bisagra != null && bisagra.childCount > 0)
        {
            hoja = bisagra.GetChild(0).gameObject;
        }
    }

    void Update()
    {
        if (bisagra == null)
        {
            return;
        }

        if (hoja != null && hoja.activeSelf == destruida)
        {
            hoja.SetActive(!destruida);
        }

        // Una puerta destruida ya no es puerta: el hueco queda libre, así
        // que la hoja se va del todo y no tiene sentido seguir girándola.
        if (destruida)
        {
            return;
        }

        Quaternion objetivo = Quaternion.Euler(
            0f,
            abierta ? anguloAbierta : 0f,
            0f
        );

        bisagra.localRotation = Quaternion.RotateTowards(
            bisagra.localRotation,
            objetivo,
            velocidad * Time.deltaTime
        );
    }
}
