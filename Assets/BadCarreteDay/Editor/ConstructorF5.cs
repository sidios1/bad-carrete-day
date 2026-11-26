using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Herramienta de EDITOR para la Fase 5 (arte y audio). "Viste" el greybox:
// - Las casas/techos (bloques) se quedan como COLLIDER invisible y encima se pone
//   un modelo KayKit escalado al mismo tamaño -> la jugabilidad no cambia.
// - Props (faroles con luz, basureros, postes, arboles), perro, productos del almacen.
// - Kit cuma (jockey, banano, cadena) en los enemigos.
// - Luz de madrugada, cielo, niebla, post-proceso, musica y sonidos.
// - Fuente Luckiest Guy (Apache 2.0) para el HUD.
public static class ConstructorF5
{
    const string arte = "Assets/BadCarreteDay/Arte/";
    const string rutaFuenteTTF = arte + "Fuentes/LuckiestGuy-Regular.ttf";
    const string rutaFuenteTMP = arte + "Fuentes/LuckiestGuy SDF.asset";
    const string rutaCielo = "Assets/BadCarreteDay/Materiales/Cielo_Madrugada.mat";
    const string rutaPerfil = "Assets/BadCarreteDay/Materiales/PostProceso_Madrugada.asset";

    // ===== ASSETS COMPARTIDOS (se crean una vez) =====

    [MenuItem("Bad Carrete Day/F5 - Preparar assets (fuente, cielo, audio)")]
    public static void PrepararAssets()
    {
        CrearFuente();
        ConfigurarAudio();
        CrearCielo();
        CrearPerfilPostProceso();
        ImportarAnimalesYProps();
    }

    public const string rutaPerro = arte + "Animales/Toby_Dog.fbx";
    public const string rutaEscoba = arte + "Props/Escoba.obj";

    // Toby (perro animado de Quaternius, CC0) y la escoba (Poly by Google, CC-BY): ver Arte/Props/CREDITOS.txt
    static void ImportarAnimalesYProps()
    {
        ModelImporter perro = (ModelImporter)AssetImporter.GetAtPath(rutaPerro);
        if (perro != null)
        {
            perro.animationType = ModelImporterAnimationType.Generic;
            perro.importAnimation = true;
            ModelImporterClipAnimation[] clips = perro.defaultClipAnimations;
            foreach (ModelImporterClipAnimation c in clips)
            {
                string n = c.name.ToLower();
                // Todo en loop salvo los "de una vez" (ataque, salto, muerte)
                c.loopTime = !(n.Contains("attack") || n.Contains("jump") || n.Contains("death"));
                Debug.Log("[Toby] clip: " + c.name + " loop=" + c.loopTime);
            }
            perro.clipAnimations = clips;
            perro.SaveAndReimport();
        }
        ModelImporter escoba = (ModelImporter)AssetImporter.GetAtPath(rutaEscoba);
        if (escoba != null)
        {
            escoba.importAnimation = false;
            escoba.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            escoba.SaveAndReimport();
        }
    }

    // Clip del perro cuyo nombre contiene alguna de las palabras (en orden de preferencia)
    public static AnimationClip ClipPerro(params string[] palabras)
    {
        Object[] todo = AssetDatabase.LoadAllAssetsAtPath(rutaPerro);
        foreach (string p in palabras)
            foreach (Object o in todo)
                if (o is AnimationClip && !o.name.StartsWith("__preview__") && o.name.ToLower().Contains(p.ToLower()))
                    return (AnimationClip)o;
        return null;
    }

    static void CrearFuente()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(rutaFuenteTMP) != null) return;
        Font ttf = AssetDatabase.LoadAssetAtPath<Font>(rutaFuenteTTF);
        TMP_FontAsset fuente = TMP_FontAsset.CreateFontAsset(ttf);
        fuente.name = "LuckiestGuy SDF";
        AssetDatabase.CreateAsset(fuente, rutaFuenteTMP);
        fuente.material.name = "LuckiestGuy SDF Material";
        AssetDatabase.AddObjectToAsset(fuente.material, fuente);
        foreach (Texture2D atlas in fuente.atlasTextures)
        {
            atlas.name = "LuckiestGuy SDF Atlas";
            AssetDatabase.AddObjectToAsset(atlas, fuente);
        }
        // Si falta algun caracter, se usa la fuente por defecto de TextMesh Pro
        if (TMP_Settings.defaultFontAsset != null)
            fuente.fallbackFontAssetTable = new List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
        EditorUtility.SetDirty(fuente);
        AssetDatabase.SaveAssets();
    }

    public static TMP_FontAsset Fuente()
    {
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(rutaFuenteTMP);
    }

    // Aplica Luckiest Guy a todos los textos bajo "raiz" (menos los de lectura larga)
    public static void AplicarFuente(GameObject raiz, params string[] excepto)
    {
        TMP_FontAsset fuente = Fuente();
        if (fuente == null) return;
        foreach (TMP_Text t in raiz.GetComponentsInChildren<TMP_Text>(true))
            if (System.Array.IndexOf(excepto, t.name) < 0)
                t.font = fuente;
    }

    // La musica pesa mucho en WAV: se comprime (Vorbis) y se reproduce en streaming
    static void ConfigurarAudio()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { arte + "Audio/Musica" }))
        {
            AudioImporter imp = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            AudioImporterSampleSettings conf = imp.defaultSampleSettings;
            conf.loadType = AudioClipLoadType.Streaming;
            conf.compressionFormat = AudioCompressionFormat.Vorbis;
            conf.quality = 0.6f;
            imp.defaultSampleSettings = conf;
            imp.SaveAndReimport();
        }
    }

    // Cielo de madrugada: textura degradé (violeta -> lila -> rosado/naranjo en el horizonte)
    // usada con el shader Skybox/Panoramic.
    static void CrearCielo()
    {
        string rutaTex = "Assets/BadCarreteDay/Materiales/Cielo_Degrade.png";
        // PRE-AMANECER (hora azul): casi noche arriba, horizonte celeste con un toque rosado
        Color[] paradas = {                       // de abajo (0) hacia arriba (1)
            new Color(0.03f, 0.04f, 0.09f),       // bajo el horizonte (mar/cerros oscuros)
            new Color(0.42f, 0.55f, 0.72f),       // horizonte: celeste
            new Color(0.5f, 0.45f, 0.62f),        // franja rosada muy tenue
            new Color(0.16f, 0.24f, 0.48f),       // azul profundo
            new Color(0.05f, 0.07f, 0.2f),        // azul marino
            new Color(0.02f, 0.03f, 0.1f),        // arriba: noche
        };
        float[] alturas = { 0f, 0.5f, 0.535f, 0.62f, 0.8f, 1f };
        int ancho = 1024, alto = 512;
        Texture2D tex = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
        Random.InitState(5047); // siempre las mismas estrellas
        for (int y = 0; y < alto; y++)
        {
            float v = y / (alto - 1f);
            int k = 0;
            while (k < alturas.Length - 2 && v > alturas[k + 1]) k++;
            float t = Mathf.InverseLerp(alturas[k], alturas[k + 1], v);
            Color c = Color.Lerp(paradas[k], paradas[k + 1], Mathf.SmoothStep(0f, 1f, t));
            for (int x = 0; x < ancho; x++)
            {
                Color px = c;
                // Estrellas: solo en la parte alta (se apagan cerca del horizonte)
                if (v > 0.66f && Random.value < 0.0018f * Mathf.InverseLerp(0.66f, 1f, v))
                    px = Color.Lerp(c, Color.white, Random.Range(0.4f, 0.9f));
                tex.SetPixel(x, y, px);
            }
        }
        System.IO.File.WriteAllBytes(rutaTex, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(rutaTex);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(rutaTex);
        ti.wrapModeU = TextureWrapMode.Repeat;
        ti.wrapModeV = TextureWrapMode.Clamp;
        ti.mipmapEnabled = false;
        ti.maxTextureSize = 1024;
        ti.SaveAndReimport();

        Material cielo = AssetDatabase.LoadAssetAtPath<Material>(rutaCielo);
        if (cielo != null && cielo.shader.name != "Skybox/Panoramic")
        {
            AssetDatabase.DeleteAsset(rutaCielo);
            cielo = null;
        }
        if (cielo == null)
        {
            cielo = new Material(Shader.Find("Skybox/Panoramic"));
            AssetDatabase.CreateAsset(cielo, rutaCielo);
        }
        cielo.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex));
        cielo.SetFloat("_Mapping", 1f);   // latitud-longitud
        cielo.SetFloat("_ImageType", 0f); // 360 grados
        cielo.SetFloat("_Exposure", 1f);
        EditorUtility.SetDirty(cielo);
    }

    static void CrearPerfilPostProceso()
    {
        AssetDatabase.DeleteAsset(rutaPerfil);
        VolumeProfile perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(perfil, rutaPerfil);

        Bloom bloom = perfil.Add<Bloom>(true);
        bloom.intensity.Override(1.1f);   // que brillen faroles y ventanas
        bloom.threshold.Override(0.85f);
        bloom.scatter.Override(0.75f);
        ColorAdjustments color = perfil.Add<ColorAdjustments>(true);
        color.saturation.Override(8f);
        color.postExposure.Override(0.25f);
        color.colorFilter.Override(new Color(0.92f, 0.95f, 1f)); // leve tono frio
        Vignette vineta = perfil.Add<Vignette>(true);
        vineta.intensity.Override(0.28f);
        vineta.color.Override(new Color(0.02f, 0.03f, 0.1f));
        Tonemapping tono = perfil.Add<Tonemapping>(true);
        tono.mode.Override(TonemappingMode.Neutral);

        foreach (VolumeComponent c in perfil.components)
            AssetDatabase.AddObjectToAsset(c, perfil);
        AssetDatabase.SaveAssets();
    }

    // ===== VESTIR EL NIVEL (se llama desde ConstructorF3 antes de hornear el NavMesh) =====

    public static void VestirNivel(ConstructorF2.Base b)
    {
        Transform deco = new GameObject("Decoracion").transform;
        deco.SetParent(GameObject.Find("Nivel").transform, false);

        ConstructorNivel1.Vestir(deco, b); // props, detalles de cada zona y casas de fondo
        VestirToby();
        // (los productos del almacen ya no se ven en el meson: se compran en la tienda)
        GirarMonedas();
        foreach (ControladorEnemigo e in Object.FindObjectsByType<ControladorEnemigo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (e.GetComponent<ConductaJulio>() == null) KitCuma(e.gameObject); // Julio no lleva jockey ni banano
        Iluminacion(b);
        Audio(b);
        ConstructorF7.VestirNivel(b); // F7: revision de arte (fondo, lugares con sentido, detalle)
    }


    // --- Fachadas: los muros lisos se esconden y detras se pone una fila de edificios ---





    public static void Prop(Transform deco, string ruta, Vector3 base_, Vector3 tamanio, float rotY, bool conCollider)
    {
        GameObject m = Ajustar(arte + ruta, new Bounds(base_ + Vector3.up * tamanio.y / 2f, tamanio), rotY, deco, "Prop_" + System.IO.Path.GetFileNameWithoutExtension(ruta));
        if (conCollider)
        {
            // Collider del tamaño REAL del modelo (convertido a coordenadas locales del contenedor)
            Bounds mundo = Limites(m);
            Vector3 e = m.transform.lossyScale;
            BoxCollider col = m.AddComponent<BoxCollider>();
            col.size = new Vector3(mundo.size.x / e.x, mundo.size.y / e.y, mundo.size.z / e.z);
            Vector3 centro = mundo.center - m.transform.position;
            col.center = new Vector3(centro.x / e.x, centro.y / e.y, centro.z / e.z);
        }
    }

    // --- Toby: quiltro bravo (perro animado de Quaternius, teñido oscuro y con ojos rojos) ---
    const string rutaAnimatorToby = "Assets/BadCarreteDay/Animaciones/AnimatorToby.controller";

    static void VestirToby()
    {
        GameObject toby = GameObject.Find("NPC_Toby");
        if (toby == null) return;
        Transform cuerpo = toby.transform.Find("Cuerpo");
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPerro);
        if (asset == null) { Debug.LogError("[BadCarreteDay] Falta el modelo de Toby: " + rutaPerro); return; }

        // Contenedor (lo mueve PerroGuardian al lanzarse) + modelo con escala pareja
        GameObject contenedor = new GameObject("Modelo_Toby");
        contenedor.transform.SetParent(toby.transform, false);
        GameObject perro = (GameObject)PrefabUtility.InstantiatePrefab(asset, contenedor.transform);
        perro.name = "Perro";
        foreach (Collider c in perro.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

        // El modelo de Quaternius mira hacia -X: se gira para que mire al frente (+Z del Toby)
        perro.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        // Grande: ~1,2 m hasta la cabeza (un perro que impone)
        Bounds b = Limites(perro);
        float escala = altoToby / Mathf.Max(b.size.y, 0.001f);
        perro.transform.localScale *= escala;
        b = Limites(perro);
        perro.transform.position += new Vector3(toby.transform.position.x - b.center.x, toby.transform.position.y - b.min.y, toby.transform.position.z - b.center.z);

        // Pelaje oscuro de quiltro bravo
        Tenir(perro, "Toby", new Color(0.32f, 0.24f, 0.2f));

        // Ojos rojos que brillan. Los huesos no tienen nombre ("Bone.003"): la punta de la cabeza es el
        // extremo de hueso ("_end") mas alto de la mitad delantera del perro
        Transform punta = null;
        Vector3 centro = Limites(perro).center;
        foreach (Transform t in perro.GetComponentsInChildren<Transform>())
            if (t.name.EndsWith("_end") && !t.name.StartsWith("IK") && Vector3.Dot(t.position - centro, toby.transform.forward) > 0.2f
                && (punta == null || t.position.y > punta.position.y)) punta = t;
        if (punta != null)
        {
            Transform cabeza = punta.parent;
            Material rojo = ConstructorF7.MaterialEmisivo("Toby_Ojos", new Color(1f, 0.02f, 0.01f), 1.6f);
            Vector3 baseOjos = Vector3.Lerp(cabeza.position, punta.position, 0.45f) + Vector3.up * 0.04f;
            foreach (float lado in new[] { -1f, 1f })
            {
                GameObject ojo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ojo.name = "Ojo";
                Object.DestroyImmediate(ojo.GetComponent<Collider>());
                ojo.GetComponent<Renderer>().sharedMaterial = rojo;
                ojo.transform.SetParent(cabeza, true);
                ojo.transform.position = baseOjos + toby.transform.right * lado * 0.065f + toby.transform.forward * 0.03f;
                ojo.transform.localScale = Vector3.one * (0.04f / Mathf.Max(cabeza.lossyScale.x, 0.0001f));
            }
        }
        else Debug.LogWarning("[BadCarreteDay] No encontre la cabeza de Toby para los ojos");

        // Animator del perro (quieto / caminando segun la velocidad del NavMeshAgent)
        Animator anim = perro.GetComponent<Animator>();
        if (anim == null) anim = perro.AddComponent<Animator>();
        anim.applyRootMotion = false;
        anim.runtimeAnimatorController = CrearAnimatorToby();
        MobController mob = toby.GetComponent<MobController>();
        Animator viejo = toby.GetComponent<Animator>();
        if (mob != null) mob.animatorMob = anim;
        if (viejo != null) Object.DestroyImmediate(viejo);
        cuerpo.GetComponent<Renderer>().enabled = false;
    }

    public const float altoToby = 1.2f;

    static RuntimeAnimatorController CrearAnimatorToby()
    {
        AssetDatabase.DeleteAsset(rutaAnimatorToby);
        UnityEditor.Animations.AnimatorController c = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(rutaAnimatorToby);
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        c.AddParameter("atacando", AnimatorControllerParameterType.Bool);
        UnityEditor.Animations.BlendTree arbol;
        c.CreateBlendTreeInController("Locomocion", out arbol, 0);
        arbol.blendParameter = "Speed";
        arbol.AddChild(ClipPerro("Idle"), 0f);
        arbol.AddChild(ClipPerro("Walk"), 1.2f);
        // Mas rapido = la misma caminata acelerada (trote)
        arbol.AddChild(ClipPerro("Gallop", "Run", "Walk"), 3f);
        UnityEditor.Animations.ChildMotion[] hijos = arbol.children;
        hijos[2].timeScale = 2f;
        arbol.children = hijos;
        AssetDatabase.SaveAssets();
        return c;
    }


    static void GirarMonedas()
    {
        // Que brillen: se ven de lejos aunque haya neblina
        foreach (string nombre in new[] { "Gamba_Plateada", "Quina_Dorada", "Luka_Verde" })
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>("Assets/BadCarreteDay/Materiales/" + nombre + ".mat");
            if (m == null) continue;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", m.GetColor("_BaseColor") * 0.45f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
        }

        foreach (Moneda m in Object.FindObjectsByType<Moneda>(FindObjectsSortMode.None))
        {
            Girar g = m.gameObject.AddComponent<Girar>();
            g.velocidad = 140f;
            g.eje = Vector3.up;
            g.ejeDelMundo = true;
        }
    }

    // --- KIT CUMA: jockey de visera plana, banano cruzado y cadena dorada ---
    static void KitCuma(GameObject enemigo)
    {
        Transform cabeza = Hueso(enemigo, "Head");
        Transform pecho = Hueso(enemigo, "Spine2");
        Transform cuello = Hueso(enemigo, "Neck");
        if (cabeza == null) return;

        Color[] coloresJockey = { new Color(0.9f, 0.15f, 0.15f), new Color(0.1f, 0.1f, 0.12f), new Color(0.15f, 0.35f, 0.9f), Color.white };
        Color cJockey = coloresJockey[Mathf.Abs(enemigo.name.GetHashCode()) % coloresJockey.Length];

        // El hueso de la cabeza de Mixamo esta en la base del craneo: la gorra va ~20 cm mas arriba
        Parte(cabeza, "Jockey_Copa", PrimitiveType.Sphere, new Vector3(0f, 0.2f, 0.01f), new Vector3(0.23f, 0.13f, 0.25f), Vector3.zero, cJockey);
        Parte(cabeza, "Jockey_Visera", PrimitiveType.Cube, new Vector3(0f, 0.17f, 0.15f), new Vector3(0.22f, 0.015f, 0.17f), new Vector3(-8f, 0f, 0f), cJockey);
        if (pecho != null)
            Parte(pecho, "Banano", PrimitiveType.Capsule, new Vector3(0f, 0.05f, 0.13f), new Vector3(0.09f, 0.16f, 0.07f), new Vector3(0f, 0f, 60f), new Color(0.08f, 0.08f, 0.1f));
        if (cuello != null)
            Parte(cuello, "Cadena", PrimitiveType.Cylinder, new Vector3(0f, -0.03f, 0.03f), new Vector3(0.2f, 0.008f, 0.2f), new Vector3(20f, 0f, 0f), new Color(1f, 0.8f, 0.2f));
    }

    static void Parte(Transform hueso, string nombre, PrimitiveType tipo, Vector3 posLocal, Vector3 tamanioMundo, Vector3 rot, Color color)
    {
        GameObject p = GameObject.CreatePrimitive(tipo);
        p.name = nombre;
        Object.DestroyImmediate(p.GetComponent<Collider>());
        p.transform.SetParent(hueso, false);
        // Los huesos de Mixamo pueden venir escalados: compensamos para que el tamaño sea en metros
        Vector3 escalaPadre = hueso.lossyScale;
        p.transform.localPosition = new Vector3(posLocal.x / escalaPadre.x, posLocal.y / escalaPadre.y, posLocal.z / escalaPadre.z);
        p.transform.localRotation = Quaternion.Euler(rot);
        p.transform.localScale = new Vector3(tamanioMundo.x / escalaPadre.x, tamanioMundo.y / escalaPadre.y, tamanioMundo.z / escalaPadre.z);
        p.GetComponent<Renderer>().sharedMaterial = ConstructorF2.Material("Kit_" + ColorUtility.ToHtmlStringRGB(color), color);
    }

    // --- Polerón-hélice para el planeo (se agrega al prefab del jugador) ---
    public static void AgregarPoleronHelice(GameObject jugador)
    {
        Transform cabeza = Hueso(jugador, "Head");
        if (cabeza == null) return;
        Transform viejo = cabeza.Find("Poleron_Helice");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);

        GameObject helice = new GameObject("Poleron_Helice");
        helice.transform.SetParent(cabeza, false);
        Vector3 e = cabeza.lossyScale;
        helice.transform.localPosition = new Vector3(0f, 0.38f / e.y, 0f);
        helice.transform.localScale = new Vector3(1f / e.x, 1f / e.y, 1f / e.z);
        Color gris = new Color(0.55f, 0.55f, 0.6f);
        Parte(helice.transform, "Manga_1", PrimitiveType.Cube, Vector3.zero, new Vector3(1.1f, 0.05f, 0.22f), Vector3.zero, gris);
        Parte(helice.transform, "Manga_2", PrimitiveType.Cube, Vector3.zero, new Vector3(0.22f, 0.05f, 1.1f), Vector3.zero, gris);
        Parte(helice.transform, "Capucha", PrimitiveType.Sphere, new Vector3(0f, 0.04f, 0f), new Vector3(0.3f, 0.18f, 0.3f), Vector3.zero, gris);
        helice.SetActive(false);

        VisualPlaneo visual = jugador.GetComponent<VisualPlaneo>();
        if (visual == null) visual = jugador.AddComponent<VisualPlaneo>();
        visual.controlador = jugador.GetComponent<StarterAssets.ThirdPersonController>();
        visual.poleron = helice;
    }

    // --- Luz de madrugada ---
    static void Iluminacion(ConstructorF2.Base b)
    {
        Light sol = null;
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sol = l; break; }
        if (sol != null)
        {
            // Pre-amanecer: el sol aun no sale; luz fria y debil que viene del horizonte
            sol.transform.rotation = Quaternion.Euler(10f, -35f, 0f);
            sol.color = new Color(0.62f, 0.72f, 1f);
            sol.intensity = 0.45f;
            sol.shadows = LightShadows.Soft;
            RenderSettings.sun = sol;
        }

        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(rutaCielo);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.22f, 0.28f, 0.48f);
        RenderSettings.ambientEquatorColor = new Color(0.2f, 0.24f, 0.38f);
        RenderSettings.ambientGroundColor = new Color(0.07f, 0.07f, 0.12f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.17f, 0.22f, 0.36f);
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance = 330f;

        GameObject vol = new GameObject("PostProceso_Global");
        Volume volumen = vol.AddComponent<Volume>();
        volumen.isGlobal = true;
        volumen.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(rutaPerfil);

        foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            UniversalAdditionalCameraData datos = cam.GetUniversalAdditionalCameraData();
            datos.renderPostProcessing = true;
            // La camara es una instancia de prefab: sin esto Unity descarta el cambio al guardar la escena
            PrefabUtility.RecordPrefabInstancePropertyModifications(datos);
            EditorUtility.SetDirty(datos);
        }
    }

    // --- Musica, jingles, ladrido y plata ---
    static void Audio(ConstructorF2.Base b)
    {
        GameObject sistemas = b.juego.gameObject;

        AudioSource musica = Fuente2D(sistemas, "Musica/stumble_around.wav", 0.3f, true, true);
        AudioSource victoria = Fuente2D(sistemas, "Jingles/jingles_PIZZI03.ogg", 0.8f, false, false);
        AudioSource derrota = Fuente2D(sistemas, "Jingles/jingles_SAX03.ogg", 0.8f, false, false);
        b.dinero.sonidoSumar = Fuente2D(sistemas, "Jingles/jingles_PIZZI00.ogg", 0.5f, false, false);

        UnityEventTools.AddPersistentListener(b.juego.eventos.alGanar, musica.Stop);
        UnityEventTools.AddPersistentListener(b.juego.eventos.alGanar, victoria.Play);
        UnityEventTools.AddPersistentListener(b.juego.eventos.alPerder, musica.Stop);
        UnityEventTools.AddPersistentListener(b.juego.eventos.alPerder, derrota.Play);

        // Ladrido del Toby (3D: se escucha cerca del perro)
        GameObject toby = GameObject.Find("NPC_Toby");
        if (toby != null)
        {
            AudioSource ladrido = toby.AddComponent<AudioSource>();
            ladrido.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(arte + "Audio/SFX/dog_barking_mono.wav");
            ladrido.playOnAwake = false;
            ladrido.spatialBlend = 1f;
            ladrido.minDistance = 3f;
            ladrido.maxDistance = 25f;
            ControladorColisionTrigger zona = toby.GetComponentInChildren<ControladorColisionTrigger>(true);
            if (zona != null) UnityEventTools.AddVoidPersistentListener(zona.onTriggerEnterEvent, ladrido.Play);
        }
    }

    static AudioSource Fuente2D(GameObject obj, string clip, float volumen, bool loop, bool alIniciar)
    {
        AudioSource a = obj.AddComponent<AudioSource>();
        a.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(arte + "Audio/" + clip);
        a.volume = volumen;
        a.loop = loop;
        a.playOnAwake = alIniciar;
        a.spatialBlend = 0f;
        return a;
    }

    // ===== UTILIDADES =====

    // Instancia un modelo y lo escala/ubica para llenar exactamente "objetivo" (caja en el mundo).
    // Se usa un contenedor sin rotacion para poder escalar en los ejes del mundo.
    public static GameObject Ajustar(string rutaModelo, Bounds objetivo, float rotY, Transform padre, string nombre)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo);
        if (asset == null) { Debug.LogError("[BadCarreteDay] No existe: " + rutaModelo); return new GameObject(nombre); }

        GameObject contenedor = new GameObject(nombre);
        contenedor.transform.SetParent(padre, true);
        GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(asset, contenedor.transform);
        modelo.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        foreach (Collider c in modelo.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

        Bounds actual = Limites(modelo);
        Vector3 escala = new Vector3(objetivo.size.x / Mathf.Max(actual.size.x, 0.001f),
                                     objetivo.size.y / Mathf.Max(actual.size.y, 0.001f),
                                     objetivo.size.z / Mathf.Max(actual.size.z, 0.001f));
        // Modelo centrado en X/Z y apoyado en el suelo del contenedor
        modelo.transform.localPosition = new Vector3(-actual.center.x, -actual.min.y, -actual.center.z);
        contenedor.transform.position = new Vector3(objetivo.center.x, objetivo.min.y, objetivo.center.z);
        // Si el padre esta escalado, compensamos para que el tamaño final sea el pedido
        Vector3 ep = padre != null ? padre.lossyScale : Vector3.one;
        contenedor.transform.localScale = new Vector3(escala.x / ep.x, escala.y / ep.y, escala.z / ep.z);
        return contenedor;
    }

    // Un cubo primitivo mide 1x1x1: sus limites son su posicion y su escala (funciona aunque este inactivo)
    public static Bounds LimitesCubo(Transform t)
    {
        return new Bounds(t.position, t.lossyScale);
    }

    public static Bounds Limites(GameObject obj)
    {
        Renderer[] rs = obj.GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds(obj.transform.position, Vector3.zero);
        bool primero = true;
        foreach (Renderer r in rs)
        {
            if (primero) { b = r.bounds; primero = false; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }

    public static void Tenir(GameObject obj, string nombre, Color tinte)
    {
        ConstructorBadCarreteDay.CrearCarpeta("Assets/BadCarreteDay/Materiales/Tintes");
        Dictionary<Material, Material> copias = new Dictionary<Material, Material>();
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                if (!copias.ContainsKey(mats[i]))
                {
                    string ruta = "Assets/BadCarreteDay/Materiales/Tintes/" + nombre + "_" + mats[i].name + ".mat";
                    Material copia = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                    if (copia == null)
                    {
                        copia = new Material(mats[i]);
                        if (copia.HasProperty("_BaseColor")) copia.SetColor("_BaseColor", copia.GetColor("_BaseColor") * tinte);
                        AssetDatabase.CreateAsset(copia, ruta);
                    }
                    copias[mats[i]] = copia;
                }
                mats[i] = copias[mats[i]];
            }
            r.sharedMaterials = mats;
        }
    }

    static Transform Hueso(GameObject raiz, string sufijo)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name.EndsWith(":" + sufijo) || t.name == sufijo) return t;
        return null;
    }

    static GameObject BuscarIncluyendoInactivos(string nombre)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == nombre) return t.gameObject;
        return null;
    }
}
