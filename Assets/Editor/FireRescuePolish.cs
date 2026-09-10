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

    [MenuItem("Tools/Fire Rescue/Aplicar TODO el polish visual", false, 0)]
    public static void Todo()
    {
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
        CrearCarpetas();
        VestirHabitaciones();

        AssetDatabase.SaveAssets();
        Debug.Log("[Fire Rescue] Habitaciones vestidas.");
    }

    [MenuItem("Tools/Fire Rescue/5 - Construir HUD", false, 24)]
    public static void PasoHUD()
    {
        if (TMP_Settings.defaultFontAsset == null)
        {
            EditorUtility.DisplayDialog(
                "Falta TextMeshPro",
                "Abre Window > TextMeshPro > Import TMP Essential Resources, " +
                "acepta, y vuelve a ejecutar este paso.",
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
        Color color
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
            RutaTexturas + "/T_Puff.png"
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

    private static void ConstruirFuego()
    {
        Material matLlama = MaterialParticula(
            "PartFuego",
            "Mobile/Particles/Additive",
            Color.white
        );

        Material matChispa = MaterialParticula(
            "PartChispa",
            "Mobile/Particles/Additive",
            Color.white
        );

        GameObject raiz = new GameObject("FireVisual");

        // Núcleo sólido y emissive: le da cuerpo a la llama y se ve
        // desde lejos aunque las partículas sean sutiles.
        GameObject nucleo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nucleo.name = "Core";
        nucleo.transform.SetParent(raiz.transform, false);
        nucleo.transform.localScale = new Vector3(0.34f, 0.26f, 0.34f);
        nucleo.transform.localPosition = new Vector3(0f, -0.06f, 0f);
        Object.DestroyImmediate(nucleo.GetComponent<Collider>());
        nucleo.GetComponent<Renderer>().sharedMaterial = Cargar("NucleoFuego");

        PulseVisual latido = nucleo.AddComponent<PulseVisual>();
        latido.amplitud = 0.13f;
        latido.velocidad = 3.4f;
        latido.pulsarEmision = true;
        latido.emisionMinima = 1.2f;
        latido.emisionMaxima = 2.8f;

        // Llamas
        GameObject llamas = new GameObject("Flames");
        llamas.transform.SetParent(raiz.transform, false);

        ParticleSystem ps = llamas.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.55f, 1.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.20f, 0.42f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;
        main.startColor = new ParticleSystem.MinMaxGradient(
            Hex("FFD24A"),
            Hex("FF4A15")
        );

        var emision = ps.emission;
        emision.rateOverTime = 22f;

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 14f;
        forma.radius = 0.20f;
        forma.rotation = new Vector3(-90f, 0f, 0f);

        var colorVida = ps.colorOverLifetime;
        colorVida.enabled = true;
        colorVida.color = new ParticleSystem.MinMaxGradient(
            Degradado(
                new[] { Hex("FFE08A"), Hex("FF7A22"), Hex("C22A08") },
                new[] { 0f, 0.45f, 1f },
                new[] { 0f, 1f, 0.9f, 0f },
                new[] { 0f, 0.15f, 0.6f, 1f }
            )
        );

        var tamVida = ps.sizeOverLifetime;
        tamVida.enabled = true;
        tamVida.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.25f, 1f),
                new Keyframe(1f, 0.15f)
            )
        );

        // Ruido: es lo que hace que la llama tiemble en vez de subir recta.
        var ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.45f;
        ruido.frequency = 1.6f;
        ruido.scrollSpeed = 1.1f;

        var render = ps.GetComponent<ParticleSystemRenderer>();
        render.sharedMaterial = matLlama;
        render.renderMode = ParticleSystemRenderMode.Billboard;
        render.sortingFudge = -2f;

        // Chispas
        GameObject chispas = new GameObject("Sparks");
        chispas.transform.SetParent(raiz.transform, false);

        ParticleSystem sp = chispas.AddComponent<ParticleSystem>();
        var spMain = sp.main;
        spMain.duration = 1f;
        spMain.loop = true;
        spMain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        spMain.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.9f);
        spMain.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        spMain.simulationSpace = ParticleSystemSimulationSpace.World;
        spMain.maxParticles = 18;
        spMain.gravityModifier = -0.06f;
        spMain.startColor = new ParticleSystem.MinMaxGradient(Hex("FFD98A"));

        var spEm = sp.emission;
        spEm.rateOverTime = 7f;

        var spForma = sp.shape;
        spForma.shapeType = ParticleSystemShapeType.Cone;
        spForma.angle = 26f;
        spForma.radius = 0.16f;
        spForma.rotation = new Vector3(-90f, 0f, 0f);

        var spColor = sp.colorOverLifetime;
        spColor.enabled = true;
        spColor.color = new ParticleSystem.MinMaxGradient(
            Degradado(
                new[] { Hex("FFF0B0"), Hex("FF7A22") },
                new[] { 0f, 1f },
                new[] { 1f, 1f, 0f },
                new[] { 0f, 0.55f, 1f }
            )
        );

        var spRender = sp.GetComponent<ParticleSystemRenderer>();
        spRender.sharedMaterial = matChispa;
        spRender.renderMode = ParticleSystemRenderMode.Billboard;

        GuardarComoPrefab(raiz, RutaPrefabs + "/FireVisual.prefab");

        // Se mete dentro del prefab Fire que BoardManager ya instancia,
        // sin borrar su raíz ni sus componentes.
        InyectarVisual("Fire", RutaPrefabs + "/FireVisual.prefab", true);
    }

    // =====================================================
    // HUMO
    // =====================================================

    private static void ConstruirHumo()
    {
        Material matHumo = MaterialParticula(
            "PartHumo",
            "Mobile/Particles/Alpha Blended",
            Color.white
        );

        GameObject raiz = new GameObject("SmokeVisual");

        ParticleSystem ps = raiz.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 2f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.16f, 0.36f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 26;
        main.startColor = new ParticleSystem.MinMaxGradient(
            Hex("8B939B"),
            Hex("5E666E")
        );

        var emision = ps.emission;
        emision.rateOverTime = 6f;

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 20f;
        forma.radius = 0.26f;
        forma.rotation = new Vector3(-90f, 0f, 0f);

        var colorVida = ps.colorOverLifetime;
        colorVida.enabled = true;
        colorVida.color = new ParticleSystem.MinMaxGradient(
            Degradado(
                new[] { Hex("9AA2AA"), Hex("4E555C") },
                new[] { 0f, 1f },
                new[] { 0f, 0.42f, 0f },
                new[] { 0f, 0.3f, 1f }
            )
        );

        // El humo crece al subir: es lo que lo distingue del fuego, que
        // se encoge y desaparece rápido.
        var tamVida = ps.sizeOverLifetime;
        tamVida.enabled = true;
        tamVida.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(1f, 1.5f)
            )
        );

        var rotVida = ps.rotationOverLifetime;
        rotVida.enabled = true;
        rotVida.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

        var ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.22f;
        ruido.frequency = 0.5f;
        ruido.scrollSpeed = 0.25f;

        var render = ps.GetComponent<ParticleSystemRenderer>();
        render.sharedMaterial = matHumo;
        render.renderMode = ParticleSystemRenderMode.Billboard;
        render.sortingFudge = 2f;

        GuardarComoPrefab(raiz, RutaPrefabs + "/SmokeVisual.prefab");

        CrearPrefabHumoDeJuego();
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

        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(bm);

        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetActiveScene()
        );
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
        PanelRedondeado("T_Panel", 64, 14, 0f);
        PanelRedondeado("T_PanelBorde", 64, 14, 3f);
        Degradado("T_Brillo", 8, 64);

        Icono("T_IconRescate", DibujarPersona);
        Icono("T_IconPerdida", DibujarPersonaCaida);
        Icono("T_IconDanio", DibujarEdificio);
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

        CanvasScaler escala = hud.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escala.matchWidthOrHeight = 0.5f;

        HUDController ctrl = hud.AddComponent<HUDController>();
        ctrl.client = Object.FindFirstObjectByType<SimulationClient>();

        Color tinta = Hex("E8EDF2");
        Color tenue = Hex("8A96A3");
        Color panelFondo = new Color(0.055f, 0.075f, 0.10f, 0.88f);

        // ---------- Cabecera ----------
        GameObject header = Panel(hud.transform, "Header",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(32f, -32f), new Vector2(430f, 128f),
            panelFondo);

        Acento(header.transform, Hex("D23B32"));

        Texto(header.transform, "Titulo", "FIRE RESCUE",
            new Vector2(20f, -16f), new Vector2(300f, 34f),
            30f, tinta, FontStyles.Bold);

        Texto(header.transform, "Subtitulo", "OPERATION STATUS",
            new Vector2(21f, -50f), new Vector2(300f, 20f),
            13f, tenue, FontStyles.Normal);

        ctrl.estadoPunto = Punto(header.transform, "EstadoPunto",
            new Vector2(21f, -80f), 12f, Hex("5C9BD6"));

        ctrl.estadoTexto = Texto(header.transform, "Estado", "STANDBY",
            new Vector2(40f, -74f), new Vector2(260f, 22f),
            15f, tinta, FontStyles.Bold);

        Texto(header.transform, "TurnoEtiqueta", "TURN",
            new Vector2(-96f, -18f), new Vector2(80f, 18f),
            12f, tenue, FontStyles.Normal, TextAlignmentOptions.Right,
            new Vector2(1f, 1f));

        ctrl.turnoTexto = Texto(header.transform, "Turno", "000",
            new Vector2(-16f, -34f), new Vector2(120f, 48f),
            42f, tinta, FontStyles.Bold, TextAlignmentOptions.Right,
            new Vector2(1f, 1f));

        ctrl.estrategiaTexto = Texto(header.transform, "Estrategia", "...",
            new Vector2(-16f, -86f), new Vector2(240f, 20f),
            13f, Hex("5C9BD6"), FontStyles.Bold,
            TextAlignmentOptions.Right, new Vector2(1f, 1f));

        // ---------- Paneles de métricas ----------
        float y = -176f;

        GameObject pRescate = PanelMetrica(hud.transform, "RescuePanel",
            "VICTIMS RESCUED", y, panelFondo, Hex("35A06A"),
            "T_IconRescate",
            out ctrl.rescatadasTexto, out ctrl.rescatadasBarra,
            out ctrl.rescatadasFlash);

        ctrl.rescatadasPanel = pRescate.GetComponent<RectTransform>();

        y -= 104f;

        GameObject pPerdidas = PanelMetrica(hud.transform, "CasualtyPanel",
            "VICTIMS LOST", y, panelFondo, Hex("E8A33D"),
            "T_IconPerdida",
            out ctrl.perdidasTexto, out ctrl.perdidasBarra,
            out ctrl.perdidasFlash);

        ctrl.perdidasPanel = pPerdidas.GetComponent<RectTransform>();

        y -= 104f;

        GameObject pDanio = PanelMetrica(hud.transform, "DamagePanel",
            "STRUCTURAL INTEGRITY", y, panelFondo, Hex("D23B32"),
            "T_IconDanio",
            out ctrl.danioTexto, out ctrl.danioBarra,
            out ctrl.danioFlash);

        ctrl.danioPanel = pDanio.GetComponent<RectTransform>();

        ctrl.danioEtiqueta = Texto(pDanio.transform, "Estado", "STABLE",
            new Vector2(-18f, -14f), new Vector2(140f, 20f),
            13f, tinta, FontStyles.Bold,
            TextAlignmentOptions.Right, new Vector2(1f, 1f));

        // ---------- Overlay final ----------
        ConstruirOverlay(hud.transform, ctrl, tinta, tenue);

        EditorUtility.SetDirty(hud);
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

    private static GameObject PanelMetrica(
        Transform padre,
        string nombre,
        string titulo,
        float y,
        Color fondo,
        Color acento,
        string nombreIcono,
        out TMP_Text valor,
        out Image barra,
        out Image flash
    )
    {
        GameObject panel = Panel(padre, nombre,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(32f, y), new Vector2(430f, 92f), fondo);

        Acento(panel.transform, acento);

        flash = Capa(panel.transform, "Flash", acento);

        // Icono a la izquierda, con su propio recuadro tenue.
        GameObject caja = new GameObject("IconoFondo",
            typeof(RectTransform), typeof(Image));

        caja.transform.SetParent(panel.transform, false);

        RectTransform cajaRt = caja.GetComponent<RectTransform>();
        cajaRt.anchorMin = new Vector2(0f, 1f);
        cajaRt.anchorMax = new Vector2(0f, 1f);
        cajaRt.pivot = new Vector2(0f, 1f);
        cajaRt.anchoredPosition = new Vector2(20f, -16f);
        cajaRt.sizeDelta = new Vector2(46f, 46f);

        Image cajaImg = caja.GetComponent<Image>();
        cajaImg.color = new Color(acento.r, acento.g, acento.b, 0.14f);
        cajaImg.raycastTarget = false;

        Sprite redondo = Sprite("T_Panel");

        if (redondo != null)
        {
            cajaImg.sprite = redondo;
            cajaImg.type = Image.Type.Sliced;
            cajaImg.pixelsPerUnitMultiplier = 3.2f;
        }

        Sprite icono = Sprite(nombreIcono);

        if (icono != null)
        {
            GameObject ic = new GameObject("Icono",
                typeof(RectTransform), typeof(Image));

            ic.transform.SetParent(caja.transform, false);

            RectTransform icRt = ic.GetComponent<RectTransform>();
            icRt.anchorMin = new Vector2(0.5f, 0.5f);
            icRt.anchorMax = new Vector2(0.5f, 0.5f);
            icRt.pivot = new Vector2(0.5f, 0.5f);
            icRt.anchoredPosition = Vector2.zero;
            icRt.sizeDelta = new Vector2(26f, 26f);

            Image icImg = ic.GetComponent<Image>();
            icImg.sprite = icono;
            icImg.color = acento;
            icImg.raycastTarget = false;
            icImg.preserveAspect = true;
        }

        Texto(panel.transform, "Titulo", titulo,
            new Vector2(78f, -16f), new Vector2(280f, 20f),
            13f, Hex("8A96A3"), FontStyles.Bold);

        valor = Texto(panel.transform, "Valor", "0 / 0",
            new Vector2(78f, -34f), new Vector2(280f, 34f),
            28f, Hex("E8EDF2"), FontStyles.Bold);

        // Riel de la barra
        GameObject riel = new GameObject("BarraFondo",
            typeof(RectTransform), typeof(Image));

        riel.transform.SetParent(panel.transform, false);

        RectTransform rielRt = riel.GetComponent<RectTransform>();
        rielRt.anchorMin = new Vector2(0f, 1f);
        rielRt.anchorMax = new Vector2(0f, 1f);
        rielRt.pivot = new Vector2(0f, 1f);
        rielRt.anchoredPosition = new Vector2(20f, -74f);
        rielRt.sizeDelta = new Vector2(392f, 9f);

        Image rielImg = riel.GetComponent<Image>();
        rielImg.color = new Color(1f, 1f, 1f, 0.10f);

        Sprite capsula = Sprite("T_Panel");

        if (capsula != null)
        {
            rielImg.sprite = capsula;
            rielImg.type = Image.Type.Sliced;
            rielImg.pixelsPerUnitMultiplier = 9f;
        }

        GameObject relleno = new GameObject("BarraRelleno",
            typeof(RectTransform), typeof(Image));

        relleno.transform.SetParent(riel.transform, false);

        RectTransform relRt = relleno.GetComponent<RectTransform>();
        relRt.anchorMin = Vector2.zero;
        relRt.anchorMax = Vector2.one;
        relRt.offsetMin = Vector2.zero;
        relRt.offsetMax = Vector2.zero;

        barra = relleno.GetComponent<Image>();
        barra.color = acento;

        if (capsula != null)
        {
            barra.sprite = capsula;
        }

        barra.type = Image.Type.Filled;
        barra.fillMethod = Image.FillMethod.Horizontal;
        barra.fillOrigin = (int)Image.OriginHorizontal.Left;
        barra.fillAmount = 0f;

        return panel;
    }

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
        Vector2? ancla = null
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
        t.characterSpacing = 2f;

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
