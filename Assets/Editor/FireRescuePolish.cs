using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Constructor del acabado visual de Fire Rescue.
///
/// Por qué es un script de editor y no código de runtime: todo lo que
/// genera queda como assets y GameObjects REALES en el proyecto. Se
/// ejecuta una vez desde el menú y después el equipo puede abrir cada
/// material, cada panel y cada sistema de partículas en el Inspector y
/// modificarlos sin volver a tocar código. Si esto se dibujara desde
/// C# en Play, nadie podría editarlo.
///
/// No toca el servidor de Python, no cambia el contrato JSON y no
/// reescribe la lógica. Solo presentación.
///
/// Menú: Tools > Fire Rescue.
/// Cada paso corre por separado a propósito: si uno falla, los demás
/// siguen sirviendo.
/// </summary>
public static class FireRescuePolish
{
    private const string RaizArte = "Assets/_Polish";
    private const string RutaMateriales = RaizArte + "/Materials";
    private const string RutaTexturas = RaizArte + "/Textures";
    private const string RutaPrefabs = RaizArte + "/Prefabs";
    private const string RutaPrefabsJuego = "Assets/Prefabs";
    private const string RutaModelos = "Assets/Models/FireRescue";

    // Paleta. Un solo lugar donde cambiarla.
    private static readonly Color PisoClaro   = Hex("C7C2B6");
    private static readonly Color PisoOscuro  = Hex("B4AE9F");
    private static readonly Color Pared       = Hex("4C5057");
    private static readonly Color Puerta      = Hex("8A5A32");
    private static readonly Color Salida      = Hex("2FA36B");
    private static readonly Color Fuego       = Hex("FF5A1F");
    private static readonly Color Humo        = Hex("7A8189");
    private static readonly Color VictimaCol  = Hex("EDEAE0");
    private static readonly Color FalsaCol    = Hex("5A6068");
    private static readonly Color PoiCol      = Hex("3E8FD0");

    private static readonly Color[] ColoresBombero =
    {
        Hex("D23B32"), // rojo
        Hex("3D7FB8"), // azul
        Hex("E8A33D"), // amarillo
        Hex("35A06A"), // verde
        Hex("8E5BC4"), // morado
        Hex("E2703A"), // naranja
    };

    // =====================================================
    // MENÚ
    // =====================================================

    /// <summary>
    /// Comprueba que la escena abierta sea la del juego.
    ///
    /// Por qué existe: al salir de Safe Mode, Unity puede abrir una
    /// escena vacía "Untitled" en vez de MainScene. Si el polish corre
    /// ahí, los muebles, la luz y el HUD se construyen en una escena que
    /// nadie va a guardar, y parece que el menú no hizo nada.
    /// </summary>
    private static bool EscenaCorrecta()
    {
        if (Object.FindFirstObjectByType<BoardManager>() != null)
        {
            return true;
        }

        string abierta = EditorSceneManager.GetActiveScene().name;

        if (string.IsNullOrEmpty(abierta))
        {
            abierta = "sin guardar";
        }

        EditorUtility.DisplayDialog(
            "Escena equivocada",
            "La escena abierta es \"" + abierta + "\" y no tiene " +
            "BoardManager.\n\n" +
            "Abre Assets/Scenes/MainScene.unity y vuelve a ejecutar " +
            "esto. Si te pregunta si guardas la escena actual, di que no.",
            "Entendido"
        );

        return false;
    }

    [MenuItem("Tools/Fire Rescue/Aplicar TODO el polish visual", false, 0)]
    public static void Todo()
    {
        if (!EscenaCorrecta())
        {
            return;
        }

        PasoMateriales();
        PasoVFX();
        PasoPersonajes();
        PasoProps();
        PasoHUD();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Fire Rescue",
            "Polish aplicado.\n\n" +
            "Guarda la escena con Ctrl+S y dale Play con el servidor " +
            "de Python corriendo.",
            "Listo"
        );
    }

    [MenuItem("Tools/Fire Rescue/1 - Materiales e iluminacion", false, 20)]
    public static void PasoMateriales()
    {
        CrearCarpetas();
        CrearMateriales();
        VestirPrefabs();
        ConfigurarLuz();

        AssetDatabase.SaveAssets();
        Debug.Log("[Fire Rescue] Materiales e iluminación aplicados.");
    }

    [MenuItem("Tools/Fire Rescue/2 - Fuego y humo", false, 21)]
    public static void PasoVFX()
    {
        CrearCarpetas();
        CrearTexturaParticula();
        ConstruirFuego();
        ConstruirHumo();
        ConstruirVapor();

        AssetDatabase.SaveAssets();
        Debug.Log("[Fire Rescue] Fuego y humo construidos.");
    }

    [MenuItem("Tools/Fire Rescue/3 - Personajes y marcadores", false, 22)]
    public static void PasoPersonajes()
    {
        CrearCarpetas();
        VestirBombero();
        VestirVictimaYPoi();
        ConectarPrefabs();

        AssetDatabase.SaveAssets();
        Debug.Log("[Fire Rescue] Personajes y marcadores listos.");
    }

    [MenuItem("Tools/Fire Rescue/4 - Vestir habitaciones", false, 23)]
    public static void PasoProps()
    {
        if (!EscenaCorrecta())
        {
            return;
        }

        CrearCarpetas();
        VestirHabitaciones();

        AssetDatabase.SaveAssets();
        Debug.Log("[Fire Rescue] Habitaciones vestidas.");
    }

    [MenuItem("Tools/Fire Rescue/5 - Construir HUD", false, 24)]
    public static void PasoHUD()
    {
        if (!EscenaCorrecta())
        {
            return;
        }

        // TMP_Settings carga su asset desde Resources la primera vez. Si
        // los recursos esenciales todavía no se han importado, esa lectura
        // puede lanzar excepción en vez de devolver null, así que se
        // pregunta dentro de un try.
        bool tmpListo;

        try
        {
            tmpListo = TMP_Settings.defaultFontAsset != null;
        }
        catch (System.Exception)
        {
            tmpListo = false;
        }

        if (!tmpListo)
        {
            EditorUtility.DisplayDialog(
                "Falta TextMeshPro",
                "Abre Window > TextMeshPro > Import TMP Essential Resources, " +
                "pulsa Import TMP Essentials, y vuelve a ejecutar este paso.",
                "Entendido"
            );

            return;
        }

        CrearCarpetas();
        CrearSpritesHUD();
        ConstruirHUD();

        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetActiveScene()
        );

        Debug.Log("[Fire Rescue] HUD construido en Canvases/GameHUD.");
    }

    // =====================================================
    // CARPETAS
    // =====================================================

    private static void CrearCarpetas()
    {
        CrearCarpeta("Assets", "_Polish");
        CrearCarpeta(RaizArte, "Materials");
        CrearCarpeta(RaizArte, "Textures");
        CrearCarpeta(RaizArte, "Fonts");
        CrearCarpeta(RaizArte, "Prefabs");
        CrearCarpeta("Assets", "Models");
        CrearCarpeta("Assets/Models", "FireRescue");
    }

    private static void CrearCarpeta(string padre, string nombre)
    {
        string ruta = padre + "/" + nombre;

        if (!AssetDatabase.IsValidFolder(ruta))
        {
            AssetDatabase.CreateFolder(padre, nombre);
        }
    }

    // =====================================================
    // MATERIALES
    // =====================================================

    private static void CrearMateriales()
    {
        Mate("Piso", PisoClaro, 0.08f);
        Mate("PisoAlterno", PisoOscuro, 0.08f);
        Mate("Pared", Pared, 0.12f);
        Mate("Puerta", Puerta, 0.20f);
        Mate("Victima", VictimaCol, 0.10f, VictimaCol * 0.18f);
        Mate("FalsaAlarma", FalsaCol, 0.05f);
        Mate("POI", PoiCol, 0.30f, PoiCol * 0.55f);
        Mate("Salida", Salida, 0.35f, Salida * 1.1f);
        Mate("NucleoFuego", Fuego, 0.0f, Fuego * 2.4f);
        Mate("Metal", Hex("2E3742"), 0.45f);
        Mate("Piel", Hex("D6A279"), 0.05f);
        Mate("Reflejante", Hex("F0F3A8"), 0.55f, Hex("F0F3A8") * 0.35f);
        Mate("Madera", Hex("7A5334"), 0.15f);
        Mate("Tela", Hex("53606E"), 0.08f);

        for (int i = 0; i < ColoresBombero.Length; i++)
        {
            Mate("Bombero_" + (i + 1), ColoresBombero[i], 0.18f);
        }

        // Piso y pared dejan de ser color plano y pasan a tener textura
        // con normal map. Es el cambio que quita la sensacion de bloques
        // pintados.
        TexturizarMateriales();
    }

    private static Material Mate(
        string nombre,
        Color color,
        float suavidad,
        Color? emision = null
    )
    {
        string ruta = RutaMateriales + "/M_" + nombre + ".mat";

        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);

        if (m == null)
        {
            m = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, ruta);
        }

        m.SetColor("_Color", color);
        m.SetFloat("_Glossiness", suavidad);
        m.SetFloat("_Metallic", 0f);

        if (emision.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emision.Value);
            m.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            m.DisableKeyword("_EMISSION");
            m.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        EditorUtility.SetDirty(m);

        return m;
    }

    private static Material Cargar(string nombre)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(
            RutaMateriales + "/M_" + nombre + ".mat"
        );
    }

    // =====================================================
    // PREFABS DEL TABLERO
    // =====================================================

    private static void VestirPrefabs()
    {
        PintarPrefab("Cell", "Piso");
        PintarPrefab("Wall", "Pared");
        PintarPrefab("Door", "Puerta");
        PintarPrefab("Exit", "Salida");

        // Sin esto las 48 celdas muestran el mismo dibujo de duela y el
        // piso queda con cara de mosaico repetido.
        VariarSuperficie("Cell", true, 0.11f);
        VariarSuperficie("Wall", false, 0.13f);
    }

    /// <summary>
    /// Agrega SuperficieVariada al prefab y lo configura.
    ///
    /// El componente se pone sobre el objeto que tiene el Renderer, que
    /// no siempre es la raíz del prefab.
    /// </summary>
    private static void VariarSuperficie(
        string prefab,
        bool soloEnU,
        float brillo
    )
    {
        string ruta = RutaPrefabsJuego + "/" + prefab + ".prefab";

        // LoadPrefabContents lanza excepción si la ruta no existe, así
        // que se comprueba antes igual que en PintarPrefab.
        if (!File.Exists(ruta))
        {
            Debug.LogWarning("[Fire Rescue] No encontré " + ruta);
            return;
        }

        GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);

        if (raiz == null)
        {
            return;
        }

        try
        {
            Renderer r = raiz.GetComponentInChildren<Renderer>(true);

            if (r == null)
            {
                return;
            }

            SuperficieVariada v =
                r.gameObject.GetComponent<SuperficieVariada>();

            if (v == null)
            {
                v = r.gameObject.AddComponent<SuperficieVariada>();
            }

            v.soloEnU = soloEnU;
            v.variacionBrillo = brillo;
            v.desplazamiento = 1f;

            PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    /// <summary>
    /// Abre el prefab de verdad, le cambia el material a todos sus
    /// renderers y lo guarda. Se hace sobre el asset, no sobre una
    /// copia en la escena, para que sobreviva a Clear Preview y a Play.
    /// </summary>
    private static void PintarPrefab(string prefab, string material)
    {
        string ruta = RutaPrefabsJuego + "/" + prefab + ".prefab";

        if (!File.Exists(ruta))
        {
            Debug.LogWarning("[Fire Rescue] No encontré " + ruta);
            return;
        }

        Material m = Cargar(material);

        if (m == null)
        {
            return;
        }

        GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);

        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = new Material[r.sharedMaterials.Length];

            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = m;
            }

            if (mats.Length == 0)
            {
                mats = new[] { m };
            }

            r.sharedMaterials = mats;
        }

        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        PrefabUtility.UnloadPrefabContents(raiz);
    }

    // =====================================================
    // ILUMINACIÓN
    // =====================================================

    private static void ConfigurarLuz()
    {
        Light sol = null;

        foreach (Light l in Object.FindObjectsByType<Light>(
            FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional)
            {
                sol = l;
                break;
            }
        }

        if (sol != null)
        {
            sol.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            sol.intensity = 1.15f;
            sol.color = Hex("FFF2E0");
            sol.shadows = LightShadows.Soft;
            sol.shadowStrength = 0.62f;

            EditorUtility.SetDirty(sol);
        }

        // Ambiente en gradiente: cielo frío arriba y rebote cálido del
        // piso abajo. Es lo que le quita el aspecto plano y blanco a la
        // escena sin oscurecerla.
        RenderSettings.ambientMode =
            UnityEngine.Rendering.AmbientMode.Trilight;

        RenderSettings.ambientSkyColor = Hex("5C6B7E");
        RenderSettings.ambientEquatorColor = Hex("47505C");
        RenderSettings.ambientGroundColor = Hex("3A342E");
        RenderSettings.ambientIntensity = 1f;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = Hex("2B3038");
        RenderSettings.fogStartDistance = 18f;
        RenderSettings.fogEndDistance = 46f;

        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetActiveScene()
        );
    }

    // =====================================================
    // TEXTURA DE PARTÍCULA
    // =====================================================

    /// <summary>
    /// Genera un punto suave en PNG.
    ///
    /// Sin textura, el Particle System dibuja cuadrados duros. Eso es
    /// exactamente lo que hace que el fuego actual parezca cubos de
    /// colores en vez de llamas.
    /// </summary>
    private static void CrearTexturaParticula()
    {
        string ruta = RutaTexturas + "/T_Puff.png";

        if (File.Exists(ruta))
        {
            return;
        }

        const int lado = 128;

        Texture2D tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);

        float centro = (lado - 1) * 0.5f;

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float dx = (x - centro) / centro;
                float dy = (y - centro) / centro;

                float d = Mathf.Sqrt(dx * dx + dy * dy);

                // Caída suave: opaco al centro, cero justo en el borde.
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();

        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(ruta);

        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;

        if (imp != null)
        {
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
    }

    private static Material MaterialParticula(
        string nombre,
        string shaderPreferido,
        Color color,
        string textura = "T_Puff"
    )
    {
        string ruta = RutaMateriales + "/M_" + nombre + ".mat";

        Shader s =
            Shader.Find(shaderPreferido) ??
            Shader.Find("Particles/Standard Unlit") ??
            Shader.Find("Sprites/Default");

        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);

        if (m == null)
        {
            m = new Material(s);
            AssetDatabase.CreateAsset(m, ruta);
        }
        else
        {
            m.shader = s;
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
            RutaTexturas + "/" + textura + ".png"
        );

        if (tex != null)
        {
            m.mainTexture = tex;
        }

        m.color = color;

        EditorUtility.SetDirty(m);

        return m;
    }

    // =====================================================
    // FUEGO
    // =====================================================

    /// <summary>
    /// Arma la llama por capas.
    ///
    /// Qué estaba mal antes: una esfera emisiva de 0.34 en el centro con
    /// partículas redondas alrededor. Una esfera se lee como bola de luz,
    /// y un sprite circular se lee como humo iluminado. Ninguna de las dos
    /// cosas se parece a una llama.
    ///
    /// Qué se hace ahora:
    ///
    /// - La esfera desaparece. La llama la forman solo partículas.
    /// - El sprite T_Llama tiene silueta de llama: base redondeada, panza
    ///   al 20% de la altura, punta afilada y núcleo caliente sobre el eje.
    /// - Tres capas encimadas: base ancha y roja pegada al suelo, cuerpo
    ///   naranja subiendo, y punta amarilla pequeña y rápida. Es lo que
    ///   pedía la referencia: base, cuerpo y punta.
    /// - Más chispas sueltas para romper la regularidad.
    /// - Todas usan VerticalBillboard, no Billboard. Con la cámara a 62
    ///   grados, un billboard normal acostaría la llama hacia atrás; el
    ///   vertical la mantiene de pie y solo gira sobre su eje Y.
    /// - Material aditivo: donde se encima una capa con otra, el color se
    ///   suma y aparece solo un núcleo blanco-amarillo, sin tener que
    ///   dibujarlo.
    ///
    /// No se agregan luces reales. Con hasta veinte fuegos en el tablero y
    /// forward rendering, una Light por fuego se come el presupuesto de
    /// luces por píxel y el rendimiento cuenta en la rúbrica.
    /// </summary>
    private static void ConstruirFuego()
    {
        Material matLlama = MaterialParticula(
            "PartFuego",
            "Mobile/Particles/Additive",
            Color.white,
            "T_Llama"
        );

        Material matChispa = MaterialParticula(
            "PartChispa",
            "Mobile/Particles/Additive",
            Color.white,
            "T_Chispa"
        );

        GameObject raiz = new GameObject("FireVisual");

        // Base: ancha, lenta, roja, pegada al suelo. Es la que le da
        // asiento a la llama para que no parezca flotar.
        CapaLlama(
            raiz.transform, "LlamaBase", matLlama,
            alturaLocal: -0.02f,
            emision: 9f,
            maximo: 16,
            vidaMin: 0.34f, vidaMax: 0.58f,
            velMin: 0.22f, velMax: 0.46f,
            tamMin: 0.34f, tamMax: 0.52f,
            radio: 0.19f, angulo: 9f,
            colorA: Hex("FF6A18"), colorB: Hex("D6300A"),
            colorMedio: Hex("FF4E10"), colorFinal: Hex("8E1C04"),
            crecer: 0.85f, encoger: 0.42f,
            ruidoFuerza: 0.18f, ruidoFrec: 1.1f,
            orden: -3f
        );

        // Cuerpo: la masa principal de la llama.
        CapaLlama(
            raiz.transform, "LlamaCuerpo", matLlama,
            alturaLocal: 0.06f,
            emision: 13f,
            maximo: 22,
            vidaMin: 0.40f, vidaMax: 0.72f,
            velMin: 0.60f, velMax: 1.05f,
            tamMin: 0.24f, tamMax: 0.40f,
            radio: 0.13f, angulo: 12f,
            colorA: Hex("FFA023"), colorB: Hex("FF5A10"),
            colorMedio: Hex("FF7A1A"), colorFinal: Hex("B32806"),
            crecer: 1f, encoger: 0.22f,
            ruidoFuerza: 0.40f, ruidoFrec: 1.7f,
            orden: -2f
        );

        // Punta: chica, rápida y clara. Sube más y se apaga antes.
        CapaLlama(
            raiz.transform, "LlamaPunta", matLlama,
            alturaLocal: 0.16f,
            emision: 10f,
            maximo: 16,
            vidaMin: 0.26f, vidaMax: 0.46f,
            velMin: 1.05f, velMax: 1.75f,
            tamMin: 0.12f, tamMax: 0.22f,
            radio: 0.07f, angulo: 17f,
            colorA: Hex("FFEEA8"), colorB: Hex("FFC24A"),
            colorMedio: Hex("FFCF63"), colorFinal: Hex("FF7A1A"),
            crecer: 1f, encoger: 0.10f,
            ruidoFuerza: 0.62f, ruidoFrec: 2.4f,
            orden: -1f
        );

        // Chispas
        GameObject chispas = new GameObject("Chispas");
        chispas.transform.SetParent(raiz.transform, false);

        ParticleSystem pc = chispas.AddComponent<ParticleSystem>();

        var mc = pc.main;
        mc.duration = 1f;
        mc.loop = true;
        mc.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        mc.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 2.1f);
        mc.startSize = new ParticleSystem.MinMaxCurve(0.030f, 0.075f);
        mc.simulationSpace = ParticleSystemSimulationSpace.World;
        mc.maxParticles = 18;
        mc.gravityModifier = -0.06f;
        mc.startColor = new ParticleSystem.MinMaxGradient(
            Hex("FFE9A0"), Hex("FF9A2E"));

        var ec = pc.emission;
        ec.rateOverTime = 5f;

        var sc = pc.shape;
        sc.shapeType = ParticleSystemShapeType.Cone;
        sc.angle = 26f;
        sc.radius = 0.14f;
        sc.rotation = new Vector3(-90f, 0f, 0f);

        var cvc = pc.colorOverLifetime;
        cvc.enabled = true;
        cvc.color = new ParticleSystem.MinMaxGradient(
            Degradado(
                new[] { Hex("FFF0BE"), Hex("FF7A22") },
                new[] { 0f, 1f },
                new[] { 0f, 1f, 0f },
                new[] { 0f, 0.2f, 1f }
            )
        );

        var nc = pc.noise;
        nc.enabled = true;
        nc.strength = 0.55f;
        nc.frequency = 2.6f;
        nc.scrollSpeed = 1.4f;

        var rc = pc.GetComponent<ParticleSystemRenderer>();
        rc.sharedMaterial = matChispa;
        rc.renderMode = ParticleSystemRenderMode.Billboard;
        rc.sortingFudge = -4f;

        GuardarComoPrefab(raiz, RutaPrefabs + "/FireVisual.prefab");
    }

    /// <summary>
    /// Una capa de llama. Las tres capas del fuego son iguales salvo por
    /// los números, así que se arman con la misma función.
    /// </summary>
    private static void CapaLlama(
        Transform padre,
        string nombre,
        Material material,
        float alturaLocal,
        float emision,
        int maximo,
        float vidaMin, float vidaMax,
        float velMin, float velMax,
        float tamMin, float tamMax,
        float radio, float angulo,
        Color colorA, Color colorB,
        Color colorMedio, Color colorFinal,
        float crecer, float encoger,
        float ruidoFuerza, float ruidoFrec,
        float orden
    )
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(0f, alturaLocal, 0f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(vidaMin, vidaMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(velMin, velMax);
        main.startSize = new ParticleSystem.MinMaxCurve(tamMin, tamMax);

        // Giro chico a propósito. Con giro libre las llamas apuntarían a
        // cualquier lado; con ocho grados solo se despeinan un poco.
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.14f, 0.14f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maximo;
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);

        var em = ps.emission;
        em.rateOverTime = emision;

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = angulo;
        forma.radius = radio;
        forma.rotation = new Vector3(-90f, 0f, 0f);

        var colorVida = ps.colorOverLifetime;
        colorVida.enabled = true;
        colorVida.color = new ParticleSystem.MinMaxGradient(
            Degradado(
                new[] { colorA, colorMedio, colorFinal },
                new[] { 0f, 0.45f, 1f },
                new[] { 0f, 1f, 0.85f, 0f },
                new[] { 0f, 0.12f, 0.55f, 1f }
            )
        );

        // La llama nace chica, se abre y se afila al subir.
        var tamVida = ps.sizeOverLifetime;
        tamVida.enabled = true;
        tamVida.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.55f),
                new Keyframe(0.22f, crecer),
                new Keyframe(1f, encoger)
            )
        );

        // Sin ruido la llama sube recta como una vela y se nota falsa.
        var ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = ruidoFuerza;
        ruido.frequency = ruidoFrec;
        ruido.scrollSpeed = 1.2f;

        var render = ps.GetComponent<ParticleSystemRenderer>();
        render.sharedMaterial = material;

        // VerticalBillboard y no Billboard: mantiene la llama de pie con
        // la cámara inclinada. Es el detalle que más se nota.
        render.renderMode = ParticleSystemRenderMode.VerticalBillboard;
        render.sortingFudge = orden;
    }

    // =====================================================
    // HUMO
    // =====================================================

    /// <summary>
    /// Humo. Tiene que distinguirse del fuego de un vistazo.
    ///
    /// Las cuatro diferencias que lo separan de la llama:
    /// crece en vez de afilarse, sube lento en vez de rápido, no tiene
    /// núcleo brillante porque el material es transparente y no aditivo,
    /// y gira sobre sí mismo.
    /// </summary>
    private static void ConstruirHumo()
    {
        Material matHumo = MaterialParticula(
            "PartHumo",
            "Mobile/Particles/Alpha Blended",
            Color.white,
            "T_Bocanada"
        );

        GameObject raiz = new GameObject("SmokeVisual");

        ParticleSystem ps = raiz.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 2f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.30f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.40f, 0.72f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;
        main.startColor = new ParticleSystem.MinMaxGradient(
            Hex("98A0A8"), Hex("646C74"));

        var emision = ps.emission;
        emision.rateOverTime = 5.5f;

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 22f;
        forma.radius = 0.24f;
        forma.rotation = new Vector3(-90f, 0f, 0f);

        var colorVida = ps.colorOverLifetime;
        colorVida.enabled = true;
        colorVida.color = new ParticleSystem.MinMaxGradient(
            Degradado(
                new[] { Hex("A6AEB6"), Hex("545B62") },
                new[] { 0f, 1f },
                new[] { 0f, 0.38f, 0f },
                new[] { 0f, 0.28f, 1f }
            )
        );

        // Crece al subir. Es lo contrario del fuego, que se afila.
        var tamVida = ps.sizeOverLifetime;
        tamVida.enabled = true;
        tamVida.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.45f),
                new Keyframe(1f, 1.75f)
            )
        );

        var rotVida = ps.rotationOverLifetime;
        rotVida.enabled = true;
        rotVida.z = new ParticleSystem.MinMaxCurve(-0.45f, 0.45f);

        var ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.26f;
        ruido.frequency = 0.45f;
        ruido.scrollSpeed = 0.22f;

        var render = ps.GetComponent<ParticleSystemRenderer>();
        render.sharedMaterial = matHumo;
        render.renderMode = ParticleSystemRenderMode.Billboard;
        render.sortingFudge = 3f;

        GuardarComoPrefab(raiz, RutaPrefabs + "/SmokeVisual.prefab");

        CrearPrefabHumoDeJuego();
    }

    /// <summary>
    /// Ráfaga de vapor de un solo disparo, para cuando se apaga fuego.
    ///
    /// Se destruye sola: loop apagado y Stop Action en Destroy. BoardManager
    /// la instancia y se olvida de ella, así que no hay que llevar registro
    /// de nada ni limpiar al terminar la partida.
    /// </summary>
    private static void ConstruirVapor()
    {
        Material matVapor = MaterialParticula(
            "PartVapor",
            "Mobile/Particles/Alpha Blended",
            Color.white,
            "T_Bocanada"
        );

        GameObject raiz = new GameObject("SteamVisual");

        ParticleSystem ps = raiz.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.55f, 1.30f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.32f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 20;
        main.gravityModifier = -0.10f;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.startColor = new ParticleSystem.MinMaxGradient(
            Hex("EAF0F4"), Hex("BCC6CE"));

        // Un solo estallido al nacer, no emisión continua.
        var emision = ps.emission;
        emision.rateOverTime = 0f;
        emision.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)12)
        });

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 38f;
        forma.radius = 0.14f;
        forma.rotation = new Vector3(-90f, 0f, 0f);

        var colorVida = ps.colorOverLifetime;
        colorVida.enabled = true;
        colorVida.color = new ParticleSystem.MinMaxGradient(
            Degradado(
                new[] { Hex("FFFFFF"), Hex("C4CED6") },
                new[] { 0f, 1f },
                new[] { 0f, 0.75f, 0f },
                new[] { 0f, 0.15f, 1f }
            )
        );

        var tamVida = ps.sizeOverLifetime;
        tamVida.enabled = true;
        tamVida.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.6f),
                new Keyframe(1f, 2.1f)
            )
        );

        var render = ps.GetComponent<ParticleSystemRenderer>();
        render.sharedMaterial = matVapor;
        render.renderMode = ParticleSystemRenderMode.Billboard;
        render.sortingFudge = -6f;

        GuardarComoPrefab(raiz, RutaPrefabsJuego + "/Steam.prefab");
    }

    /// <summary>
    /// El humo se dibujaba con el prefab del fuego a media escala, así
    /// que se confundían. Aquí se crea un Smoke.prefab propio, con la
    /// misma raíz que espera BoardManager pero con el visual del humo.
    /// </summary>
    private static void CrearPrefabHumoDeJuego()
    {
        string origen = RutaPrefabsJuego + "/Fire.prefab";
        string destino = RutaPrefabsJuego + "/Smoke.prefab";

        if (!File.Exists(origen))
        {
            return;
        }

        if (!File.Exists(destino))
        {
            AssetDatabase.CopyAsset(origen, destino);
            AssetDatabase.Refresh();
        }

        GameObject raiz = PrefabUtility.LoadPrefabContents(destino);

        // Fuera el visual del fuego que venía heredado.
        for (int i = raiz.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(raiz.transform.GetChild(i).gameObject);
        }

        foreach (Renderer r in raiz.GetComponents<Renderer>())
        {
            r.enabled = false;
        }

        GameObject visual = AssetDatabase.LoadAssetAtPath<GameObject>(
            RutaPrefabs + "/SmokeVisual.prefab"
        );

        if (visual != null)
        {
            GameObject copia =
                (GameObject)PrefabUtility.InstantiatePrefab(visual);

            copia.transform.SetParent(raiz.transform, false);
        }

        PrefabUtility.SaveAsPrefabAsset(raiz, destino);
        PrefabUtility.UnloadPrefabContents(raiz);

        ConectarPrefabs();
    }

    /// <summary>
    /// Deja asignados en el BoardManager de la escena los prefabs que
    /// antes estaban vacíos. Se usa SerializedProperty para no fallar
    /// si algún campo todavía no existe en esta versión del script.
    /// </summary>
    private static void ConectarPrefabs()
    {
        BoardManager bm = Object.FindFirstObjectByType<BoardManager>();

        if (bm == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(bm);

        Asignar(so, "smokePrefab", RutaPrefabsJuego + "/Smoke.prefab");
        Asignar(so, "victimPrefab", RutaPrefabsJuego + "/Victim.prefab");
        Asignar(so, "falseAlarmPrefab", RutaPrefabsJuego + "/FalseAlarm.prefab");
        Asignar(so, "steamPrefab", RutaPrefabsJuego + "/Steam.prefab");

        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(bm);

        AjustarRitmo();

        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetActiveScene()
        );
    }

    /// <summary>
    /// Baja el ritmo de la demo a 1.2 segundos por turno.
    ///
    /// Qué es y qué no es: secondsBetweenSteps es la espera de Unity antes
    /// de pedir el siguiente POST /step. No entra en ninguna decisión, no
    /// toca ningún generador y no cambia ningún resultado. El servidor
    /// calcula el turno igual de rápido y devuelve el mismo estado. Es el
    /// mismo patrón que FuncAnimation en los notebooks de la clase: la
    /// velocidad de despliegue va aparte del modelo.
    ///
    /// A 0.5 segundos una partida de 40 turnos dura 20 segundos y no se
    /// alcanza a ver nada. A 1.2 dura unos 48, que es la duración correcta
    /// para grabar. Se cambia desde el inspector cuando quieras.
    ///
    /// Solo se toca si sigue en el valor viejo, para no pisar un ajuste
    /// que hayas hecho a mano.
    /// </summary>
    private static void AjustarRitmo()
    {
        SimulationClient cliente =
            Object.FindAnyObjectByType<SimulationClient>();

        if (cliente == null)
        {
            return;
        }

        if (Mathf.Abs(cliente.secondsBetweenSteps - 0.5f) > 0.001f)
        {
            return;
        }

        SerializedObject so = new SerializedObject(cliente);

        SerializedProperty p = so.FindProperty("secondsBetweenSteps");

        if (p != null)
        {
            p.floatValue = 1.2f;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(cliente);

            Debug.Log(
                "[Fire Rescue] Ritmo de la demo: 0.5 -> 1.2 s por turno. " +
                "Es solo presentación, no cambia ningún resultado. " +
                "Se ajusta en Systems > Simulation Client."
            );
        }
    }

    private static void Asignar(
        SerializedObject so,
        string campo,
        string rutaPrefab
    )
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null)
        {
            Debug.LogWarning(
                "[Fire Rescue] BoardManager no tiene el campo " + campo +
                ". Actualiza BoardManager.cs."
            );

            return;
        }

        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);

        if (prefab != null)
        {
            prop.objectReferenceValue = prefab;
        }
    }

    // =====================================================
    // PERSONAJES Y MARCADORES
    // =====================================================

    private static void VestirBombero()
    {
        string ruta = RutaPrefabsJuego + "/Firefighter.prefab";

        if (!File.Exists(ruta))
        {
            return;
        }

        GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);

        // La raíz conserva FirefighterView y todo lo que ya tenía.
        // Solo se apaga su malla y se le cuelga un hijo "Visual".
        foreach (Renderer r in raiz.GetComponents<Renderer>())
        {
            r.enabled = false;
        }

        Transform viejo = raiz.transform.Find("Visual");

        if (viejo != null)
        {
            Object.DestroyImmediate(viejo.gameObject);
        }

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(raiz.transform, false);

        GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(
            RutaModelos + "/FireRescue_Bombero.obj"
        );

        if (modelo != null)
        {
            GameObject cuerpo = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            cuerpo.name = "CharacterModel";
            cuerpo.transform.SetParent(visual.transform, false);

            AsignarPorNombre(cuerpo, new Dictionary<string, string>
            {
                { "cuerpo", "Bombero_1" },
                { "casco", "Reflejante" },
                { "tanque", "Metal" },
                { "reflejante", "Reflejante" },
                { "piel", "Piel" },
            });
        }
        else
        {
            // Sin el modelo importado, una cápsula con casco sigue
            // leyéndose mejor que una cápsula sola.
            GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cuerpo.name = "CharacterModel";
            cuerpo.transform.SetParent(visual.transform, false);
            cuerpo.transform.localScale = new Vector3(0.34f, 0.30f, 0.34f);
            Object.DestroyImmediate(cuerpo.GetComponent<Collider>());
            cuerpo.GetComponent<Renderer>().sharedMaterial = Cargar("Bombero_1");

            GameObject casco = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            casco.name = "Helmet";
            casco.transform.SetParent(cuerpo.transform, false);
            casco.transform.localScale = new Vector3(1.05f, 1.05f, 1.05f);
            casco.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            Object.DestroyImmediate(casco.GetComponent<Collider>());
            casco.GetComponent<Renderer>().sharedMaterial = Cargar("Reflejante");
        }

        // Aro en el suelo: desde la cámara cenital es lo que dice dónde
        // está parado cada bombero aunque haya humo encima.
        GameObject aro = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        aro.name = "GroundIndicator";
        aro.transform.SetParent(visual.transform, false);
        aro.transform.localPosition = new Vector3(0f, -0.38f, 0f);
        aro.transform.localScale = new Vector3(0.62f, 0.008f, 0.62f);
        Object.DestroyImmediate(aro.GetComponent<Collider>());
        aro.GetComponent<Renderer>().sharedMaterial = Cargar("Bombero_1");

        FirefighterVisual fv = raiz.GetComponent<FirefighterVisual>();

        if (fv == null)
        {
            fv = raiz.AddComponent<FirefighterVisual>();
        }

        fv.visual = visual.transform;

        // Los seis colores. Sin esto los seis bomberos salen idénticos,
        // porque FirefighterView solo asigna el nombre, no el color.
        Material[] paleta = new Material[ColoresBombero.Length];

        for (int i = 0; i < paleta.Length; i++)
        {
            paleta[i] = Cargar("Bombero_" + (i + 1));
        }

        fv.coloresPorId = paleta;

        // La lista se deja vacía a propósito: FirefighterVisual busca en
        // todo el hijo Visual y cambia únicamente los slots cuyo material
        // empieza por M_Bombero, que son la casaca y el aro del suelo.
        // Casco, tanque, guantes y piel se quedan iguales en los seis.
        fv.partesDeColor = new Renderer[0];
        fv.prefijoRecoloreable = "M_Bombero";

        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        PrefabUtility.UnloadPrefabContents(raiz);
    }

    private static void VestirVictimaYPoi()
    {
        // Víctima: modelo humano acostado.
        VestirMarcador(
            "Victim",
            RutaModelos + "/FireRescue_Victima.obj",
            new Dictionary<string, string>
            {
                { "cuerpo", "Victima" },
                { "piel", "Piel" },
                { "pelo", "Metal" },
            },
            "Victima",
            0.6f,
            0f
        );

        // Falsa alarma: mismo modelo, gris apagado, para que se note
        // de inmediato que no hay a quién rescatar.
        VestirMarcador(
            "FalseAlarm",
            RutaModelos + "/FireRescue_Victima.obj",
            new Dictionary<string, string>
            {
                { "cuerpo", "FalsaAlarma" },
                { "piel", "FalsaAlarma" },
                { "pelo", "FalsaAlarma" },
            },
            "FalsaAlarma",
            0f,
            0f
        );

        // POI sin revelar: marcador de búsqueda que gira y late.
        ConstruirMarcadorPOI();
    }

    private static void VestirMarcador(
        string prefab,
        string rutaModelo,
        Dictionary<string, string> materiales,
        string materialCaida,
        float amplitudPulso,
        float rotacion
    )
    {
        string ruta = RutaPrefabsJuego + "/" + prefab + ".prefab";

        if (!File.Exists(ruta))
        {
            return;
        }

        GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);

        foreach (Renderer r in raiz.GetComponents<Renderer>())
        {
            r.enabled = false;
        }

        Transform viejo = raiz.transform.Find("Visual");

        if (viejo != null)
        {
            Object.DestroyImmediate(viejo.gameObject);
        }

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(raiz.transform, false);

        GameObject modelo =
            AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo);

        if (modelo != null)
        {
            GameObject cuerpo =
                (GameObject)PrefabUtility.InstantiatePrefab(modelo);

            cuerpo.name = "Model";
            cuerpo.transform.SetParent(visual.transform, false);

            AsignarPorNombre(cuerpo, materiales);
        }
        else
        {
            GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cuerpo.name = "Model";
            cuerpo.transform.SetParent(visual.transform, false);
            cuerpo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cuerpo.transform.localScale = new Vector3(0.24f, 0.28f, 0.24f);
            Object.DestroyImmediate(cuerpo.GetComponent<Collider>());
            cuerpo.GetComponent<Renderer>().sharedMaterial =
                Cargar(materialCaida);
        }

        if (amplitudPulso > 0f)
        {
            PulseVisual p = visual.AddComponent<PulseVisual>();
            p.amplitud = amplitudPulso * 0.12f;
            p.velocidad = 1.1f;
            p.gradosPorSegundo = rotacion;
        }

        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        PrefabUtility.UnloadPrefabContents(raiz);
    }

    private static void ConstruirMarcadorPOI()
    {
        string ruta = RutaPrefabsJuego + "/POI.prefab";

        if (!File.Exists(ruta))
        {
            return;
        }

        GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);

        foreach (Renderer r in raiz.GetComponents<Renderer>())
        {
            r.enabled = false;
        }

        Transform viejo = raiz.transform.Find("Visual");

        if (viejo != null)
        {
            Object.DestroyImmediate(viejo.gameObject);
        }

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(raiz.transform, false);

        // Base circular pegada al piso
        GameObject baseDisco =
            GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        baseDisco.name = "Base";
        baseDisco.transform.SetParent(visual.transform, false);
        baseDisco.transform.localPosition = new Vector3(0f, -0.12f, 0f);
        baseDisco.transform.localScale = new Vector3(0.52f, 0.012f, 0.52f);
        Object.DestroyImmediate(baseDisco.GetComponent<Collider>());
        baseDisco.GetComponent<Renderer>().sharedMaterial = Cargar("POI");

        // Rombo flotante: se distingue de una víctima acostada al instante
        GameObject rombo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rombo.name = "Marker";
        rombo.transform.SetParent(visual.transform, false);
        rombo.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        rombo.transform.localScale = new Vector3(0.20f, 0.20f, 0.20f);
        rombo.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
        Object.DestroyImmediate(rombo.GetComponent<Collider>());
        rombo.GetComponent<Renderer>().sharedMaterial = Cargar("POI");

        PulseVisual p = rombo.AddComponent<PulseVisual>();
        p.amplitud = 0.14f;
        p.velocidad = 1.3f;
        p.gradosPorSegundo = 75f;
        p.alturaFlotacion = 0.06f;
        p.pulsarEmision = true;

        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        PrefabUtility.UnloadPrefabContents(raiz);
    }

    /// <summary>
    /// Asigna materiales a los submateriales del OBJ por el nombre del
    /// grupo. El generador nombra los grupos cuerpo, casco, tanque,
    /// reflejante, piel, pelo, y aquí se traducen a los materiales del
    /// proyecto para que nada dependa del .mtl importado.
    /// </summary>
    private static void AsignarPorNombre(
        GameObject objeto,
        Dictionary<string, string> mapa
    )
    {
        foreach (Renderer r in objeto.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null)
                {
                    continue;
                }

                string nombre = mats[i].name.ToLowerInvariant();

                foreach (KeyValuePair<string, string> par in mapa)
                {
                    if (nombre.Contains(par.Key))
                    {
                        Material m = Cargar(par.Value);

                        if (m != null)
                        {
                            mats[i] = m;
                        }

                        break;
                    }
                }
            }

            r.sharedMaterials = mats;
        }
    }

    private static void InyectarVisual(
        string prefabJuego,
        string rutaVisual,
        bool apagarMalla
    )
    {
        string ruta = RutaPrefabsJuego + "/" + prefabJuego + ".prefab";

        if (!File.Exists(ruta))
        {
            return;
        }

        GameObject visual =
            AssetDatabase.LoadAssetAtPath<GameObject>(rutaVisual);

        if (visual == null)
        {
            return;
        }

        GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);

        if (apagarMalla)
        {
            foreach (Renderer r in raiz.GetComponents<Renderer>())
            {
                r.enabled = false;
            }
        }

        Transform anterior = raiz.transform.Find(visual.name);

        if (anterior != null)
        {
            Object.DestroyImmediate(anterior.gameObject);
        }

        GameObject copia = (GameObject)PrefabUtility.InstantiatePrefab(visual);
        copia.transform.SetParent(raiz.transform, false);

        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        PrefabUtility.UnloadPrefabContents(raiz);
    }

    private static void GuardarComoPrefab(GameObject objeto, string ruta)
    {
        PrefabUtility.SaveAsPrefabAsset(objeto, ruta);
        Object.DestroyImmediate(objeto);
    }

    // =====================================================
    // UTILIDADES
    // =====================================================

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    private static Gradient Degradado(
        Color[] colores,
        float[] tiemposColor,
        float[] alfas,
        float[] tiemposAlfa
    )
    {
        Gradient g = new Gradient();

        GradientColorKey[] ck = new GradientColorKey[colores.Length];

        for (int i = 0; i < colores.Length; i++)
        {
            ck[i] = new GradientColorKey(colores[i], tiemposColor[i]);
        }

        GradientAlphaKey[] ak = new GradientAlphaKey[alfas.Length];

        for (int i = 0; i < alfas.Length; i++)
        {
            ak[i] = new GradientAlphaKey(alfas[i], tiemposAlfa[i]);
        }

        g.SetKeys(ck, ak);

        return g;
    }

    // =====================================================
    // PROPS DE INTERIOR
    // =====================================================

    /// <summary>
    /// Dónde va cada mueble. Fila, columna, qué prop, y hacia dónde
    /// mira. Se edita aquí o, mejor, moviendo los objetos en la escena:
    /// quedan como GameObjects normales bajo Environment/Props.
    /// </summary>
    private struct Mueble
    {
        public int fila;
        public int columna;
        public string prop;
        public float giro;
        public int esquina;   // 0 NO, 1 NE, 2 SE, 3 SO

        public Mueble(int f, int c, string p, float g, int e)
        {
            fila = f; columna = c; prop = p; giro = g; esquina = e;
        }
    }

    private static readonly Mueble[] Distribucion =
    {
        new Mueble(1, 1, "Sofa",       0f,   0),
        new Mueble(1, 2, "Mesa",       0f,   3),
        new Mueble(2, 2, "Silla",      180f, 1),
        new Mueble(0, 5, "Cama",       0f,   1),
        new Mueble(0, 6, "Gabinete",   90f,  2),
        new Mueble(1, 6, "Planta",     0f,   0),
        new Mueble(2, 6, "Escritorio", 180f, 3),
        new Mueble(2, 7, "Silla",      0f,   0),
        new Mueble(3, 4, "Estante",    90f,  1),
        new Mueble(4, 1, "Gabinete",   0f,   2),
        new Mueble(4, 2, "Mesa",       0f,   0),
        new Mueble(4, 5, "Caja",       25f,  3),
        new Mueble(5, 3, "Caja",       -15f, 1),
        new Mueble(5, 6, "Planta",     0f,   2),
    };

    private static void VestirHabitaciones()
    {
        BoardManager bm = Object.FindFirstObjectByType<BoardManager>();

        float celda = bm != null ? bm.cellSize : 1f;

        Transform entorno = BuscarOCrear("Environment");

        Transform anterior = entorno.Find("Props");

        if (anterior != null)
        {
            Object.DestroyImmediate(anterior.gameObject);
        }

        GameObject contenedor = new GameObject("Props");
        contenedor.transform.SetParent(entorno, false);

        // Sin BoardPreviewMarker a propósito: los muebles no los genera
        // BoardManager, así que Clear Preview y Play no deben borrarlos.

        int puestos = 0;

        foreach (Mueble m in Distribucion)
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(
                RutaModelos + "/Prop_" + m.prop + ".obj"
            );

            if (modelo == null)
            {
                continue;
            }

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(modelo);

            go.name = "Prop_" + m.prop + "_" + (m.fila + 1) + "_" + (m.columna + 1);
            go.transform.SetParent(contenedor.transform, false);

            // Pegado a una esquina de la celda, pero solo 0.14: el
            // mueble más largo es la cama (0.70) y con más desfase su
            // borde pasaría de 0.5 y atravesaría la pared.
            float dx = (m.esquina == 1 || m.esquina == 2) ? 0.14f : -0.14f;
            float dz = (m.esquina == 0 || m.esquina == 1) ? 0.14f : -0.14f;

            go.transform.position = new Vector3(
                m.columna * celda + dx,
                0.02f,
                -m.fila * celda + dz
            );

            go.transform.rotation = Quaternion.Euler(0f, m.giro, 0f);

            AsignarPorNombre(go, new Dictionary<string, string>
            {
                { "madera", "Madera" },
                { "tela",   "Tela" },
                { "sabana", "Victima" },
                { "metal",  "Metal" },
                { "libros", "Puerta" },
                { "maceta", "Puerta" },
                { "hoja",   "Salida" },
                { "carton", "Madera" },
                { "cinta",  "Victima" },
            });

            puestos++;
        }

        if (puestos == 0)
        {
            Debug.LogWarning(
                "[Fire Rescue] No encontré los Prop_*.obj en " +
                RutaModelos + ". Los muebles quedan pendientes."
            );
        }

        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetActiveScene()
        );
    }

    // =====================================================
    // SPRITES DEL HUD
    // =====================================================

    /// <summary>
    /// Genera los sprites de la interfaz.
    ///
    /// Sin esto el HUD son rectángulos duros de Unity, que es
    /// exactamente lo que hace que una UI parezca de depuración. Un
    /// panel con esquinas redondeadas en 9-slice, un degradado de brillo
    /// y tres iconos cambian por completo la lectura, y se generan aquí
    /// para no depender de ningún pack externo.
    /// </summary>
    private static void CrearSpritesHUD()
    {
        // Los PNG buenos ya vienen en el proyecto: los iconos son de
        // Lucide (licencia ISC) y los paneles se dibujaron fuera de
        // Unity con antialias por supermuestreo. Aqui solo se dibuja lo
        // que falte, por si alguien borra un archivo.
        SiFalta("T_Panel", () => PanelRedondeado("T_Panel", 64, 14, 0f));
        SiFalta("T_PanelBorde",
                () => PanelRedondeado("T_PanelBorde", 64, 14, 3f));
        SiFalta("T_Brillo", () => Degradado("T_Brillo", 8, 64));

        SiFalta("T_IconRescate", () => Icono("T_IconRescate", DibujarPersona));
        SiFalta("T_IconPerdida",
                () => Icono("T_IconPerdida", DibujarPersonaCaida));
        SiFalta("T_IconDanio", () => Icono("T_IconDanio", DibujarEdificio));

        // Bordes de 9-slice. El borde tiene que ser algo mayor que el
        // radio de la esquina o Unity estira la curva y se deforma.
        Sliced("T_Panel", 22f);
        Sliced("T_PanelSuave", 30f);
        Sliced("T_Marco", 22f);
        Sliced("T_Capsula", 15f);
        Sliced("T_Vineta", 0f);
        Sliced("T_VinetaEsquina", 0f);
        Particula("T_Llama");
        Particula("T_Chispa");
        Particula("T_Bocanada");
        Particula("T_Puff");

        string[] iconos =
        {
            "T_IconRescate", "T_IconPerdida", "T_IconDanio",
            "T_IconTurno", "T_IconFuego", "T_IconEstrategia",
            "T_IconAlerta"
        };

        foreach (string ic in iconos)
        {
            Sliced(ic, 0f);
        }
    }

    private static bool ExisteTextura(string nombre)
    {
        return System.IO.File.Exists(RutaTexturas + "/" + nombre + ".png");
    }

    private static void SiFalta(string nombre, System.Action generar)
    {
        if (!ExisteTextura(nombre))
        {
            generar();
        }
    }

    /// <summary>
    /// Importa una textura de partícula.
    ///
    /// alphaIsTransparency es lo importante: sin eso, Unity deja basura de
    /// color en los píxeles totalmente transparentes y al mezclar aparece
    /// un halo oscuro alrededor de cada partícula.
    /// </summary>
    private static void Particula(string nombre)
    {
        string ruta = RutaTexturas + "/" + nombre + ".png";

        if (!System.IO.File.Exists(ruta))
        {
            return;
        }

        AssetDatabase.ImportAsset(ruta);

        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;

        if (imp == null)
        {
            return;
        }

        imp.textureType = TextureImporterType.Default;
        imp.alphaIsTransparency = true;
        imp.sRGBTexture = true;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.filterMode = FilterMode.Bilinear;
        imp.mipmapEnabled = true;
        imp.SaveAndReimport();
    }

    private static void Sliced(string nombre, float borde)
    {
        if (!ExisteTextura(nombre))
        {
            return;
        }

        ImportarSprite(
            RutaTexturas + "/" + nombre + ".png",
            new Vector4(borde, borde, borde, borde)
        );
    }

    // =====================================================
    // Texturas de superficie (piso, pared)
    // =====================================================

    /// <summary>
    /// Pone albedo y normal map en los materiales del tablero.
    ///
    /// El tiling es 1 a proposito: la celda mide 1 unidad de mundo y la
    /// textura esta hecha para cubrir exactamente 1 unidad. Con un
    /// valor mayor el patron se repetiria dentro de la misma celda y el
    /// piso se veria a escala de casa de munecas.
    ///
    /// Las texturas son seamless, asi que la celda de al lado continua
    /// el patron y no se ve una reja de costuras en el tablero.
    /// </summary>
    private static void TexturizarMateriales()
    {
        Superficie("Piso", "T_PisoDuela");
        Superficie("PisoAlterno", "T_PisoLoseta");
        Superficie("Pared", "T_ParedYeso");
    }

    private static void Superficie(string material, string textura)
    {
        string rutaA = RutaTexturas + "/" + textura + "_A.png";
        string rutaN = RutaTexturas + "/" + textura + "_N.png";

        if (!System.IO.File.Exists(rutaA))
        {
            return;
        }

        ConfigurarSuperficie(rutaA, false);
        ConfigurarSuperficie(rutaN, true);

        Material m = AssetDatabase.LoadAssetAtPath<Material>(
            RutaMateriales + "/M_" + material + ".mat"
        );

        if (m == null)
        {
            return;
        }

        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaA);

        if (albedo != null)
        {
            m.SetTexture("_MainTex", albedo);
        }

        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaN);

        if (normal != null)
        {
            m.SetTexture("_BumpMap", normal);

            // Sin la keyword el Standard shader ignora el normal map
            // aunque este asignado en el material.
            m.EnableKeyword("_NORMALMAP");
        }

        m.SetTextureScale("_MainTex", Vector2.one);

        // El color pasa a blanco porque el tono ya viene en el albedo.
        // Si se dejara el color plano anterior, la textura saldria
        // tenida y volveriamos a ver una superficie de color liso.
        m.color = Color.white;

        EditorUtility.SetDirty(m);
    }

    private static void ConfigurarSuperficie(string ruta, bool esNormal)
    {
        AssetDatabase.ImportAsset(ruta);

        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;

        if (imp == null)
        {
            return;
        }

        // Un normal map importado como textura de color se ve morado y
        // la luz rebota mal. El tipo tiene que ser NormalMap.
        imp.textureType = esNormal
            ? TextureImporterType.NormalMap
            : TextureImporterType.Default;

        imp.sRGBTexture = !esNormal;
        imp.wrapMode = TextureWrapMode.Repeat;
        imp.filterMode = FilterMode.Trilinear;
        imp.mipmapEnabled = true;
        imp.anisoLevel = 4;
        imp.SaveAndReimport();
    }

    // =====================================================
    // Tipografia
    // =====================================================

    /// <summary>
    /// Crea el TMP_FontAsset de Barlow Condensed a partir del TTF.
    ///
    /// Barlow Condensed es licencia SIL Open Font 1.1 (el OFL.txt viene
    /// en la misma carpeta). Se eligio condensada porque en un HUD las
    /// etiquetas son largas y el espacio horizontal es lo que escasea.
    ///
    /// Si algo falla se devuelve null y el HUD se queda con la
    /// tipografia por defecto de TextMeshPro. Prefiero un HUD feo a un
    /// HUD que no compila la noche antes de entregar.
    /// </summary>
    private static TMP_FontAsset CrearFuente()
    {
        string ttf = RaizArte + "/Fonts/BarlowCondensed-SemiBold.ttf";
        string destino = RaizArte + "/Fonts/F_Barlow.asset";

        // AssetDatabase.CreateAsset falla si la carpeta no esta dada de
        // alta, aunque exista en disco.
        CrearCarpeta(RaizArte, "Fonts");

        TMP_FontAsset ya =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(destino);

        if (ya != null)
        {
            return ya;
        }

        Font fuente = AssetDatabase.LoadAssetAtPath<Font>(ttf);

        if (fuente == null)
        {
            Debug.LogWarning(
                "FireRescuePolish: no encontre " + ttf +
                ". El HUD usa la tipografia por defecto."
            );

            return null;
        }

        try
        {
            TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(fuente);

            if (fa == null)
            {
                return null;
            }

            AssetDatabase.CreateAsset(fa, destino);

            // El atlas y el material son sub-assets. Sin esto, al
            // recargar el proyecto la fuente queda sin textura y todo
            // el HUD sale en blanco.
            if (fa.atlasTextures != null && fa.atlasTextures.Length > 0)
            {
                fa.atlasTextures[0].name = "Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            }

            if (fa.material != null)
            {
                fa.material.name = "Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }

            // Se hornean de una vez los caracteres que usa el HUD para
            // que tambien funcione en una build y no solo en el editor.
            fa.TryAddCharacters(
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                "abcdefghijklmnopqrstuvwxyz" +
                "0123456789 /:.,-()"
            );

            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();

            return fa;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(
                "FireRescuePolish: no pude crear el TMP_FontAsset (" +
                e.Message + "). El HUD usa la tipografia por defecto."
            );

            return null;
        }
    }

    private static void PanelRedondeado(
        string nombre,
        int lado,
        int radio,
        float grosorBorde
    )
    {
        string ruta = RutaTexturas + "/" + nombre + ".png";

        Texture2D tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float d = DistanciaARectanguloRedondeado(x, y, lado, radio);

                // d < 0 dentro, d > 0 fuera. El antialias sale de dejar
                // que el alfa caiga en un pixel de ancho.
                float dentro = Mathf.Clamp01(0.5f - d);

                float a = dentro;

                if (grosorBorde > 0f)
                {
                    float anillo = Mathf.Clamp01(
                        1f - Mathf.Abs(d + grosorBorde * 0.5f) /
                        (grosorBorde * 0.5f + 0.5f)
                    );

                    a = anillo * dentro;
                }

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        ImportarSprite(ruta, new Vector4(radio + 2, radio + 2, radio + 2, radio + 2));
    }

    private static float DistanciaARectanguloRedondeado(
        int x, int y, int lado, int radio
    )
    {
        float cx = Mathf.Abs(x + 0.5f - lado * 0.5f);
        float cy = Mathf.Abs(y + 0.5f - lado * 0.5f);

        float mitad = lado * 0.5f - 0.5f;

        float dx = cx - (mitad - radio);
        float dy = cy - (mitad - radio);

        if (dx <= 0f && dy <= 0f)
        {
            return Mathf.Max(dx, dy) - radio;
        }

        dx = Mathf.Max(dx, 0f);
        dy = Mathf.Max(dy, 0f);

        return Mathf.Sqrt(dx * dx + dy * dy) - radio;
    }

    private static void Degradado(string nombre, int ancho, int alto)
    {
        string ruta = RutaTexturas + "/" + nombre + ".png";

        Texture2D tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);

        for (int y = 0; y < alto; y++)
        {
            float t = (float)y / (alto - 1);
            float a = Mathf.Pow(t, 2.2f) * 0.5f;

            for (int x = 0; x < ancho; x++)
            {
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        ImportarSprite(ruta, Vector4.zero);
    }

    private delegate float FormaIcono(float x, float y);

    private static void Icono(string nombre, FormaIcono forma)
    {
        string ruta = RutaTexturas + "/" + nombre + ".png";

        const int lado = 64;

        Texture2D tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                // Coordenadas de -1 a 1 con Y hacia arriba.
                float u = (x + 0.5f) / lado * 2f - 1f;
                float v = (y + 0.5f) / lado * 2f - 1f;

                float a = Mathf.Clamp01(forma(u, v));

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        ImportarSprite(ruta, Vector4.zero);
    }

    private static float Circulo(float x, float y, float cx, float cy, float r)
    {
        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
        return Mathf.Clamp01((r - d) * 24f);
    }

    private static float Caja(
        float x, float y, float cx, float cy, float hx, float hy
    )
    {
        float dx = Mathf.Abs(x - cx) - hx;
        float dy = Mathf.Abs(y - cy) - hy;
        float d = Mathf.Max(dx, dy);
        return Mathf.Clamp01(-d * 24f);
    }

    private static float DibujarPersona(float x, float y)
    {
        float cabeza = Circulo(x, y, 0f, 0.58f, 0.23f);
        float cuerpo = Caja(x, y, 0f, -0.22f, 0.22f, 0.34f);
        float hombros = Circulo(x, y, 0f, 0.13f, 0.33f);
        return Mathf.Max(cabeza, Mathf.Max(cuerpo, hombros));
    }

    private static float DibujarPersonaCaida(float x, float y)
    {
        // La misma figura tumbada: se lee como víctima perdida sin
        // necesitar una calavera ni nada dramático.
        float cabeza = Circulo(x, y, -0.44f, -0.26f, 0.21f);
        float cuerpo = Caja(x, y, 0.06f, -0.28f, 0.44f, 0.17f);
        float aviso = Caja(x, y, 0.46f, 0.52f, 0.075f, 0.24f);
        float punto = Circulo(x, y, 0.46f, 0.16f, 0.095f);
        return Mathf.Max(Mathf.Max(cabeza, cuerpo), Mathf.Max(aviso, punto));
    }

    private static float DibujarEdificio(float x, float y)
    {
        float torre = Caja(x, y, -0.30f, -0.10f, 0.28f, 0.62f);
        float ala = Caja(x, y, 0.34f, -0.34f, 0.30f, 0.38f);

        // Ventanas en negativo: se restan del bloque.
        float ventanas = 0f;

        for (int i = 0; i < 3; i++)
        {
            float vy = 0.22f - i * 0.34f;
            ventanas = Mathf.Max(ventanas, Caja(x, y, -0.30f, vy, 0.10f, 0.09f));
        }

        ventanas = Mathf.Max(ventanas, Caja(x, y, 0.34f, -0.28f, 0.11f, 0.10f));

        return Mathf.Clamp01(Mathf.Max(torre, ala) - ventanas);
    }

    private static void ImportarSprite(string ruta, Vector4 borde)
    {
        AssetDatabase.ImportAsset(ruta);

        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;

        if (imp == null)
        {
            return;
        }

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spriteBorder = borde;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.filterMode = FilterMode.Bilinear;
        imp.SaveAndReimport();
    }

    private static Sprite Sprite(string nombre)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(
            RutaTexturas + "/" + nombre + ".png"
        );
    }

    // =====================================================
    // HUD
    // =====================================================

    // Medidas del bloque de objetivos. Están arriba y no dentro de las
    // funciones para poder ajustar el HUD entero cambiando dos números.
    private const float AnchoFila = 420f;
    private const float AltoFila = 64f;
    private const float AltoFilaDanio = 84f;
    private const float SeparacionFila = 28f;

    /// <summary>
    /// Construye el HUD de juego.
    ///
    /// Criterios de la composición, para poder repetirla si hay que mover
    /// algo:
    ///
    /// 1. Nada de tarjetas ni recuadros. El fondo son dos degradados
    ///    pegados a los bordes: uno arriba y una viñeta en la esquina de
    ///    abajo a la izquierda. Dan legibilidad sin dibujar cajas.
    ///
    /// 2. Los tres bloques van anclados a esquinas distintas y no comparten
    ///    espacio horizontal. Identidad arriba a la izquierda, turno arriba
    ///    a la derecha, objetivos abajo a la izquierda. En 1920 quedan más
    ///    de mil píxeles entre el bloque izquierdo y el derecho, así que un
    ///    texto largo no se puede encimar con otro.
    ///
    /// 3. Jerarquía por tamaño. Un número grande (el turno), etiquetas
    ///    chicas muy espaciadas y barras de 5 píxeles.
    ///
    /// 4. Un acento por bloque y nada más. Naranja en la identidad, verde
    ///    en rescatados, ámbar en perdidos, rojo en daño. El resto es gris.
    ///
    /// 5. Todo el texto en español. Las cadenas que decide HUDController
    ///    están allá; las fijas están aquí.
    ///
    /// Todo queda como GameObjects normales: se puede mover, recolorear o
    /// borrar desde la jerarquía sin tocar el código.
    /// </summary>
    private static void ConstruirHUD()
    {
        Transform padre = BuscarOCrear("Canvases");

        Transform anterior = padre.Find("GameHUD");

        if (anterior != null)
        {
            Object.DestroyImmediate(anterior.gameObject);
        }

        GameObject hud = new GameObject(
            "GameHUD",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        hud.transform.SetParent(padre, false);

        Canvas canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        // ScaleWithScreenSize con referencia 1920x1080 y match 0.5: el HUD
        // se escala con la media geométrica de ancho y alto, así que
        // conserva su proporción en cualquier ventana 16:9 y se degrada
        // razonablemente fuera de esa proporción.
        //
        // OJO: el control "Scale" de la barra del Game View es zoom del
        // editor, no resolución. Si el HUD se ve chico ahí es porque el
        // panel es chico, no porque el Canvas esté mal.
        CanvasScaler escala = hud.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escala.matchWidthOrHeight = 0.5f;

        HUDController ctrl = hud.AddComponent<HUDController>();
        ctrl.client = Object.FindAnyObjectByType<SimulationClient>();

        Color tinta = Hex("F0F3F6");
        Color tenue = Hex("97A3AF");
        Color naranja = Hex("E8672A");
        Color verde = Hex("3FAE72");
        Color ambar = Hex("E8A33D");
        Color rojo = Hex("D9453B");

        // ---------- Fondos ----------
        Vineta(hud.transform, "VinetaSuperior", 260f, 0.62f);
        VinetaEsquina(hud.transform, "VinetaEsquina", 760f, 460f, 0.66f);

        // ---------- Identidad, arriba a la izquierda ----------
        RectTransform ident = Grupo(hud.transform, "Identidad",
            new Vector2(0f, 1f), new Vector2(48f, -42f),
            new Vector2(760f, 130f));

        // En vez del filete rojo de antes va el icono de llama. Una insignia
        // se lee como juego; una barra de color se lee como panel de datos.
        IconoUI(ident, "Emblema", "T_IconFuego",
              new Vector2(0f, -2f), 34f, naranja);

        Texto(ident, "Titulo", "FLASH POINT",
            new Vector2(48f, -2f), new Vector2(620f, 40f),
            34f, tinta, FontStyles.Bold,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), 9f);

        Texto(ident, "EstrategiaEtiqueta", "ESTRATEGIA",
            new Vector2(49f, -46f), new Vector2(150f, 22f),
            16f, tenue, FontStyles.Bold,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), 11f);

        ctrl.estrategiaTexto = Texto(ident, "Estrategia", "...",
            new Vector2(178f, -46f), new Vector2(560f, 22f),
            16f, naranja, FontStyles.Bold,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), 11f);

        ctrl.estadoPunto = Punto(ident, "EstadoPunto",
            new Vector2(51f, -80f), 11f, tenue);

        ctrl.estadoTexto = Texto(ident, "Estado", "ESPERANDO SERVIDOR",
            new Vector2(72f, -82f), new Vector2(620f, 22f),
            16f, tenue, FontStyles.Bold,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), 8f);

        // ---------- Turno, arriba a la derecha ----------
        RectTransform reloj = Grupo(hud.transform, "Turno",
            new Vector2(1f, 1f), new Vector2(-48f, -42f),
            new Vector2(300f, 120f));

        Texto(reloj, "TurnoEtiqueta", "TURNO",
            new Vector2(0f, 0f), new Vector2(290f, 20f),
            15f, tenue, FontStyles.Bold,
            TextAlignmentOptions.TopRight, new Vector2(1f, 1f), 13f);

        ctrl.turnoTexto = Texto(reloj, "Turno", "000",
            new Vector2(3f, -20f), new Vector2(290f, 76f),
            66f, tinta, FontStyles.Bold,
            TextAlignmentOptions.TopRight, new Vector2(1f, 1f), 1f);

        // ---------- Objetivos, abajo a la izquierda ----------
        // Se apilan de abajo hacia arriba: daño abajo (es el que late
        // cuando el edificio está por caerse y conviene tenerlo cerca del
        // borde), después perdidos, arriba rescatados.
        float yDanio = 0f;
        float yPerdidas = AltoFilaDanio + SeparacionFila;
        float yRescate = yPerdidas + AltoFila + SeparacionFila;

        RectTransform objetivos = Grupo(hud.transform, "Objetivos",
            new Vector2(0f, 0f), new Vector2(48f, 48f),
            new Vector2(AnchoFila, yRescate + AltoFila));

        GameObject fRescate = Fila(objetivos, "RescuePanel", yRescate,
            AltoFila, "CIVILES RESCATADOS", "T_IconRescate",
            verde, tinta, tenue,
            out ctrl.rescatadasTexto, out ctrl.rescatadasBarra,
            out ctrl.rescatadasFlash);

        ctrl.rescatadasPanel = fRescate.GetComponent<RectTransform>();

        GameObject fPerdidas = Fila(objetivos, "CasualtyPanel", yPerdidas,
            AltoFila, "CIVILES PERDIDOS", "T_IconPerdida",
            ambar, tinta, tenue,
            out ctrl.perdidasTexto, out ctrl.perdidasBarra,
            out ctrl.perdidasFlash);

        ctrl.perdidasPanel = fPerdidas.GetComponent<RectTransform>();

        GameObject fDanio = Fila(objetivos, "DamagePanel", yDanio,
            AltoFilaDanio, "DAÑO ESTRUCTURAL", "T_IconDanio",
            rojo, tinta, tenue,
            out ctrl.danioTexto, out ctrl.danioBarra,
            out ctrl.danioFlash);

        ctrl.danioPanel = fDanio.GetComponent<RectTransform>();

        // ESTABLE / RIESGO / CRÍTICO / COLAPSO, debajo del número.
        ctrl.danioEtiqueta = Texto(fDanio.transform, "Estado", "ESTABLE",
            new Vector2(0f, -44f), new Vector2(220f, 20f),
            14f, tenue, FontStyles.Bold,
            TextAlignmentOptions.TopRight, new Vector2(1f, 1f), 10f);

        // ---------- Overlay final ----------
        ConstruirOverlay(hud.transform, ctrl, tinta, tenue);

        // ---------- Tipografía ----------
        // Al final y de una pasada, para que alcance también al overlay.
        TMP_FontAsset fuente = CrearFuente();

        if (fuente != null)
        {
            foreach (TMP_Text t in hud.GetComponentsInChildren<TMP_Text>(true))
            {
                t.font = fuente;
            }
        }

        EditorUtility.SetDirty(hud);
    }

    /// <summary>
    /// Contenedor vacío anclado a una esquina. Sirve para mover un bloque
    /// entero del HUD arrastrando un solo objeto.
    /// </summary>
    private static RectTransform Grupo(
        Transform padre,
        string nombre,
        Vector2 ancla,
        Vector2 posicion,
        Vector2 tamano
    )
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));

        go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = ancla;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        return rt;
    }

    /// <summary>Icono tintado, anclado arriba a la izquierda del padre.</summary>
    private static Image IconoUI(
        Transform padre,
        string nombre,
        string sprite,
        Vector2 posicion,
        float lado,
        Color color
    )
    {
        GameObject go = new GameObject(nombre,
            typeof(RectTransform), typeof(Image));

        go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = new Vector2(lado, lado);

        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        img.preserveAspect = true;

        Sprite s = Sprite(sprite);

        if (s != null)
        {
            img.sprite = s;
        }

        return img;
    }

    /// <summary>
    /// Degradado pegado al borde superior. Sustituye a los paneles: oscurece
    /// lo justo para que el texto se lea, sin dibujar un recuadro.
    /// </summary>
    private static void Vineta(
        Transform padre,
        string nombre,
        float alto,
        float opacidad
    )
    {
        GameObject go = new GameObject(nombre,
            typeof(RectTransform), typeof(Image));

        go.transform.SetParent(padre, false);
        go.transform.SetAsFirstSibling();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, alto);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.02f, 0.03f, 0.045f, opacidad);
        img.raycastTarget = false;

        Sprite s = Sprite("T_Vineta");

        if (s != null)
        {
            img.sprite = s;
        }
    }

    /// <summary>
    /// Viñeta de esquina para el bloque de objetivos. Se oscurece hacia la
    /// esquina inferior izquierda y se desvanece en las dos direcciones, así
    /// que no tiene ningún borde recto visible.
    /// </summary>
    private static void VinetaEsquina(
        Transform padre,
        string nombre,
        float ancho,
        float alto,
        float opacidad
    )
    {
        GameObject go = new GameObject(nombre,
            typeof(RectTransform), typeof(Image));

        go.transform.SetParent(padre, false);
        go.transform.SetAsFirstSibling();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(ancho, alto);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.02f, 0.03f, 0.045f, opacidad);
        img.raycastTarget = false;

        Sprite s = Sprite("T_VinetaEsquina");

        if (s != null)
        {
            img.sprite = s;
        }
    }

    /// <summary>
    /// Una fila de objetivo: icono, etiqueta, número grande y barra fina.
    ///
    /// El pivote va a media altura para que el golpe de animación de
    /// HUDController crezca desde el centro de la fila. Con el pivote en una
    /// esquina, la fila se estiraría hacia un lado al recibir el golpe.
    /// </summary>
    private static GameObject Fila(
        Transform padre,
        string nombre,
        float y,
        float alto,
        string titulo,
        string nombreIcono,
        Color acento,
        Color tinta,
        Color tenue,
        out TMP_Text valor,
        out Image barra,
        out Image flash
    )
    {
        GameObject fila = new GameObject(nombre, typeof(RectTransform));

        fila.transform.SetParent(padre, false);

        RectTransform rt = fila.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y + alto * 0.5f);
        rt.sizeDelta = new Vector2(AnchoFila, alto);

        flash = Capa(fila.transform, "Flash", acento);

        IconoUI(fila.transform, "Icono", nombreIcono,
              new Vector2(0f, -1f), 26f, acento);

        Texto(fila.transform, "Titulo", titulo,
            new Vector2(38f, -3f), new Vector2(270f, 22f),
            15f, tenue, FontStyles.Bold,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), 10f);

        valor = Texto(fila.transform, "Valor", "0 / 0",
            new Vector2(0f, -2f), new Vector2(200f, 38f),
            30f, tinta, FontStyles.Bold,
            TextAlignmentOptions.TopRight, new Vector2(1f, 1f), 1f);

        // Riel de 5 píxeles pegado al borde inferior. Una barra delgada se
        // lee como HUD; una gruesa se lee como gráfica de reporte.
        GameObject riel = new GameObject("BarraFondo",
            typeof(RectTransform), typeof(Image));

        riel.transform.SetParent(fila.transform, false);

        RectTransform rrt = riel.GetComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0f, 0f);
        rrt.anchorMax = new Vector2(1f, 0f);
        rrt.pivot = new Vector2(0.5f, 0f);
        rrt.anchoredPosition = new Vector2(0f, 4f);
        rrt.sizeDelta = new Vector2(0f, 5f);

        Image rimg = riel.GetComponent<Image>();
        rimg.color = new Color(1f, 1f, 1f, 0.10f);
        rimg.raycastTarget = false;

        Sprite capsula = Sprite("T_Capsula");

        if (capsula != null)
        {
            rimg.sprite = capsula;
            rimg.type = Image.Type.Sliced;
            rimg.pixelsPerUnitMultiplier = 7f;
        }

        GameObject relleno = new GameObject("BarraRelleno",
            typeof(RectTransform), typeof(Image));

        relleno.transform.SetParent(riel.transform, false);

        RectTransform frt = relleno.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        // El relleno va sin sprite: con Image.Type.Filled el 9-slice no
        // aplica, y a 5 píxeles de alto la punta cuadrada no se distingue.
        barra = relleno.GetComponent<Image>();
        barra.color = acento;
        barra.raycastTarget = false;
        barra.type = Image.Type.Filled;
        barra.fillMethod = Image.FillMethod.Horizontal;
        barra.fillOrigin = (int)Image.OriginHorizontal.Left;
        barra.fillAmount = 0f;

        return fila;
    }

    private static void ConstruirOverlay(
        Transform padre,
        HUDController ctrl,
        Color tinta,
        Color tenue
    )
    {
        GameObject overlay = new GameObject(
            "EndGameOverlay",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image)
        );

        overlay.transform.SetParent(padre, false);

        RectTransform rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        ctrl.overlay = overlay.GetComponent<CanvasGroup>();

        GameObject caja = Panel(overlay.transform, "Caja",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(680f, 400f),
            new Color(0.055f, 0.075f, 0.10f, 0.97f));

        RectTransform cajaRt = caja.GetComponent<RectTransform>();
        cajaRt.pivot = new Vector2(0.5f, 0.5f);
        cajaRt.anchoredPosition = Vector2.zero;

        ctrl.overlayCaja = cajaRt;

        ctrl.overlayAcento = Acento(caja.transform, Hex("D23B32"), 6f);

        ctrl.overlayTitulo = Texto(caja.transform, "Titulo", "MISSION",
            new Vector2(40f, -44f), new Vector2(600f, 56f),
            46f, tinta, FontStyles.Bold);

        ctrl.overlaySubtitulo = Texto(caja.transform, "Subtitulo", "",
            new Vector2(41f, -102f), new Vector2(600f, 26f),
            17f, tenue, FontStyles.Normal);

        Linea(caja.transform, new Vector2(40f, -140f), 600f);

        ctrl.overlayEstadisticas = Texto(caja.transform, "Estadisticas", "",
            new Vector2(41f, -160f), new Vector2(600f, 200f),
            19f, tinta, FontStyles.Normal,
            TextAlignmentOptions.TopLeft);

        overlay.SetActive(false);
    }

    // ---------- Piezas de UI ----------

    private static GameObject Panel(
        Transform padre,
        string nombre,
        Vector2 anclaMin,
        Vector2 anclaMax,
        Vector2 posicion,
        Vector2 tamano,
        Color color
    )
    {
        GameObject go = new GameObject(nombre,
            typeof(RectTransform), typeof(Image));

        go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.pivot = new Vector2(anclaMin.x, anclaMax.y);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        Image fondo = go.GetComponent<Image>();
        fondo.color = color;

        // Esquinas redondeadas en 9-slice: el sprite se estira por el
        // centro y las esquinas se quedan intactas, así que un mismo
        // archivo de 64px sirve para paneles de cualquier tamaño.
        Sprite redondo = Sprite("T_Panel");

        if (redondo != null)
        {
            fondo.sprite = redondo;
            fondo.type = Image.Type.Sliced;
            fondo.pixelsPerUnitMultiplier = 1.6f;
        }

        // Brillo de arriba: media luz que da la sensación de superficie
        // en vez de recorte de color plano.
        Sprite brillo = Sprite("T_Brillo");

        if (brillo != null)
        {
            GameObject luz = new GameObject("Brillo",
                typeof(RectTransform), typeof(Image));

            luz.transform.SetParent(go.transform, false);

            RectTransform luzRt = luz.GetComponent<RectTransform>();
            luzRt.anchorMin = new Vector2(0f, 1f);
            luzRt.anchorMax = new Vector2(1f, 1f);
            luzRt.pivot = new Vector2(0.5f, 1f);
            luzRt.anchoredPosition = Vector2.zero;
            luzRt.sizeDelta = new Vector2(0f, Mathf.Min(tamano.y, 46f));

            Image luzImg = luz.GetComponent<Image>();
            luzImg.sprite = brillo;
            luzImg.color = new Color(1f, 1f, 1f, 0.06f);
            luzImg.raycastTarget = false;
        }

        // Contorno tenue: separa el panel del tablero de fondo.
        Sprite borde = Sprite("T_PanelBorde");

        if (borde != null)
        {
            GameObject linea = new GameObject("Contorno",
                typeof(RectTransform), typeof(Image));

            linea.transform.SetParent(go.transform, false);

            RectTransform lRt = linea.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = Vector2.zero;

            Image lImg = linea.GetComponent<Image>();
            lImg.sprite = borde;
            lImg.type = Image.Type.Sliced;
            lImg.pixelsPerUnitMultiplier = 1.6f;
            lImg.color = new Color(1f, 1f, 1f, 0.09f);
            lImg.raycastTarget = false;
        }

        return go;
    }

    private static Image Acento(Transform padre, Color color, float ancho = 4f)
    {
        GameObject go = new GameObject("Acento",
            typeof(RectTransform), typeof(Image));

        go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(ancho, 0f);

        Image img = go.GetComponent<Image>();
        img.color = color;

        Sprite redondo = Sprite("T_Panel");

        if (redondo != null)
        {
            img.sprite = redondo;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 4f;
        }

        return img;
    }

    private static Image Capa(Transform padre, string nombre, Color color)
    {
        GameObject go = new GameObject(nombre,
            typeof(RectTransform), typeof(Image));

        go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, 0f);
        img.raycastTarget = false;

        return img;
    }

    private static void Linea(Transform padre, Vector2 posicion, float ancho)
    {
        GameObject go = new GameObject("Linea",
            typeof(RectTransform), typeof(Image));

        go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = new Vector2(ancho, 1f);

        go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
    }

    private static Image Punto(
        Transform padre,
        string nombre,
        Vector2 posicion,
        float tamano,
        Color color
    )
    {
        GameObject go = new GameObject(nombre,
            typeof(RectTransform), typeof(Image));

        go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = new Vector2(tamano, tamano);

        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        // Sin sprite, un Image es un cuadrado. La capsula lo redondea.
        Sprite redondo = Sprite("T_Capsula");

        if (redondo != null)
        {
            img.sprite = redondo;
        }

        return img;
    }

    private static TMP_Text Texto(
        Transform padre,
        string nombre,
        string contenido,
        Vector2 posicion,
        Vector2 tamano,
        float tam,
        Color color,
        FontStyles estilo,
        TextAlignmentOptions alineacion = TextAlignmentOptions.TopLeft,
        Vector2? ancla = null,
        float tracking = 2f
    )
    {
        GameObject go = new GameObject(nombre,
            typeof(RectTransform), typeof(TextMeshProUGUI));

        go.transform.SetParent(padre, false);

        Vector2 a = ancla ?? new Vector2(0f, 1f);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = a;
        rt.anchorMax = a;
        rt.pivot = a;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = contenido;
        t.fontSize = tam;
        t.color = color;
        t.fontStyle = estilo;
        t.alignment = alineacion;
        t.raycastTarget = false;
        // El espaciado entre letras se pasa por parametro: los
        // titulos van muy abiertos y los numeros grandes casi cerrados.
        // Es lo que hace que un HUD se lea intencional y no por defecto.
        t.characterSpacing = tracking;

        return t;
    }

    private static Transform BuscarOCrear(string nombre)
    {
        GameObject go = GameObject.Find(nombre);

        if (go == null)
        {
            go = new GameObject(nombre);
        }

        return go.transform;
    }
}
