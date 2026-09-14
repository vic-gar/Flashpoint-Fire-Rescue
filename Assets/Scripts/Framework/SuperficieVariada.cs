using UnityEngine;

/// <summary>
/// Rompe la repetición de la textura entre celdas.
///
/// El problema que resuelve: las 48 celdas del tablero comparten un
/// solo material y cada una muestra la textura completa, así que se ve
/// exactamente el mismo dibujo 48 veces. Con color plano no se notaba
/// porque no había dibujo; con textura sí, y el piso queda con cara de
/// mosaico repetido.
///
/// La solución: cada celda desliza su textura y cambia un poco de
/// brillo. Se hace con un MaterialPropertyBlock y no creando materiales
/// nuevos, así que sigue habiendo un solo material en memoria y no se
/// rompe el batching.
///
/// Por qué solo en U por defecto: las tablas de la duela corren en X,
/// que es la dirección de U. Deslizar en U mueve las testas (los cortes
/// entre tablas) y deja las juntas largas alineadas con la celda
/// vecina. Si se desplazara también en V, cada borde de celda tendría
/// un escalón en la duela y se vería peor que la repetición.
///
/// Va en los prefabs Cell y Wall. Si se borra, todo sigue funcionando:
/// se pierde la variación y nada más.
/// </summary>
[ExecuteAlways]
public class SuperficieVariada : MonoBehaviour
{
    [Tooltip("Cuánto puede deslizarse la textura, en fracción de textura.")]
    [Range(0f, 1f)]
    public float desplazamiento = 1f;

    [Tooltip("Deslizar solo a lo largo de la tabla. Apagar en paredes.")]
    public bool soloEnU = true;

    [Tooltip("Variación de brillo entre celdas. 0 = todas iguales.")]
    [Range(0f, 0.4f)]
    public float variacionBrillo = 0.10f;

    void Start()
    {
        Aplicar();
    }

    void OnValidate()
    {
        // Para poder ajustar los valores desde el Inspector y ver el
        // efecto sin darle Play.
        if (isActiveAndEnabled)
        {
            Aplicar();
        }
    }

    public void Aplicar()
    {
        Renderer r = GetComponent<Renderer>();

        if (r == null || r.sharedMaterial == null)
        {
            return;
        }

        // La semilla sale de la posición en el tablero, no de Random.
        // Así la misma celda se ve igual en cada ejecución y el video de
        // la demo no cambia entre tomas.
        Vector3 p = transform.position;

        int semilla = Mathf.RoundToInt(p.x * 71f) * 92837
                    + Mathf.RoundToInt(p.z * 71f) * 689287
                    + 7919;

        System.Random rng = new System.Random(semilla);

        float u = (float)rng.NextDouble() * desplazamiento;
        float v = soloEnU ? 0f : (float)rng.NextDouble() * desplazamiento;

        float brillo = 1f
            + ((float)rng.NextDouble() - 0.5f) * 2f * variacionBrillo;

        MaterialPropertyBlock bloque = new MaterialPropertyBlock();
        r.GetPropertyBlock(bloque);

        // _MainTex_ST empaqueta escala en xy y desplazamiento en zw.
        // Se lee la escala del material para no pisarla con un 1 fijo.
        Vector2 escala = r.sharedMaterial.mainTextureScale;

        bloque.SetVector("_MainTex_ST",
            new Vector4(escala.x, escala.y, u, v));

        Color c = r.sharedMaterial.color;

        bloque.SetColor("_Color", new Color(
            c.r * brillo,
            c.g * brillo,
            c.b * brillo,
            c.a
        ));

        r.SetPropertyBlock(bloque);
    }
}
