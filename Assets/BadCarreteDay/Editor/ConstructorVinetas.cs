using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Herramienta de EDITOR: arma 3 mini escenas 3D para las viñetas de la intro y las fotografia.
//   1) Concepcion 18:00 — sala del certamen (Benja celebrando)
//   2) 19:30 — la previa en un depto (amigos, botellas, luces)
//   3) 01:12 — "¡VAMOS A VALPOOO!" (auto en la carretera, letrero a Valparaiso)
// Las imagenes quedan en Arte/Intro/vineta_1..3.png (Sprite) y la escena en Escenas/Vinetas.unity (fuera del build).
public static class ConstructorVinetas
{
    const string arte = "Assets/BadCarreteDay/Arte/";
    const string carpetaIntro = arte + "Intro/";


    [MenuItem("Bad Carrete Day/F7 - Crear viñetas 3D de la intro")]
    public static void CrearVinetas()
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Luz y post-proceso comunes
        Light sol = new GameObject("Sol").AddComponent<Light>();
        sol.type = LightType.Directional;
        sol.shadows = LightShadows.Soft;
        Volume vol = new GameObject("PostProceso").AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/BadCarreteDay/Materiales/PostProceso_Madrugada.asset");
        Camera cam = new GameObject("Camara").AddComponent<Camera>();
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        cam.fieldOfView = 45f;

        // ---- 1) SALA DEL CERTAMEN (atardecer naranjo por la ventana) ----
        Transform sala = new GameObject("Vineta1_Sala").transform;
        Caja(sala, new Vector3(0f, -0.05f, 0f), new Vector3(10f, 0.1f, 8f), new Color(0.55f, 0.4f, 0.28f));     // piso
        Caja(sala, new Vector3(0f, 1.5f, 4f), new Vector3(10f, 3f, 0.1f), new Color(0.85f, 0.82f, 0.72f));      // muro del pizarron
        Caja(sala, new Vector3(-5f, 1.5f, 0f), new Vector3(0.1f, 3f, 8f), new Color(0.8f, 0.78f, 0.68f));       // muro lateral
        Caja(sala, new Vector3(0f, 3.05f, 0f), new Vector3(10f, 0.1f, 8f), new Color(0.9f, 0.88f, 0.8f));       // cielo raso
        Caja(sala, new Vector3(5f, 1.5f, 0f), new Vector3(0.1f, 3f, 8f), new Color(0.8f, 0.78f, 0.68f));        // otro muro
        Caja(sala, new Vector3(0f, 2.98f, 0f), new Vector3(0.2f, 0.05f, 2.5f), new Color(1f, 1f, 0.95f), 3f);   // tubo de luz
        Caja(sala, new Vector3(0f, 1.6f, 3.94f), new Vector3(4.5f, 1.6f, 0.04f), new Color(0.1f, 0.3f, 0.2f));  // pizarron
        Texto(sala, "CÁLCULO II — CERTAMEN FINAL", new Vector3(0f, 2.15f, 3.9f), 180f, 2.2f, Color.white);
        Texto(sala, "f(x) = x² + 3x ... ¿¿y ahora??", new Vector3(0f, 1.55f, 3.9f), 180f, 1.6f, new Color(0.95f, 0.95f, 0.8f));
        Caja(sala, new Vector3(-4.94f, 1.8f, 0f), new Vector3(0.04f, 1.4f, 2.2f), new Color(1f, 0.6f, 0.3f), 2f); // ventana con sol de tarde
        for (int fila = 0; fila < 2; fila++)
            for (int col = 0; col < 3; col++)
            {
                Vector3 p = new Vector3(-2.6f + col * 2.6f, 0f, 0.8f - fila * 2.2f);
                Modelo(sala, "KayKit_Furniture/table_small.fbx", new Bounds(p + Vector3.up * 0.38f, new Vector3(1f, 0.76f, 0.7f)), 0f);
                Modelo(sala, "KayKit_Furniture/chair_A_wood.fbx", new Bounds(p + new Vector3(0f, 0.45f, -0.6f), new Vector3(0.5f, 0.9f, 0.5f)), 0f);
            }
        GameObject benja1 = Personaje(sala, "Aj", new Vector3(0f, 0f, -0.8f), 180f, "Victory", 1.1f);
        Texto(sala, "7,0", new Vector3(0.4f, 0.8f, -0.4f), 180f, 1.2f, new Color(1f, 0.2f, 0.2f)); // la nota que el CREE que se saco
        sol.transform.rotation = Quaternion.Euler(25f, 80f, 0f);
        sol.color = new Color(1f, 0.75f, 0.5f);
        sol.intensity = 1.3f;
        Ambiente(new Color(0.6f, 0.55f, 0.5f), new Color(0.35f, 0.3f, 0.3f));
        Foto(cam, new Vector3(1.6f, 1.6f, -4.2f), new Vector3(0f, 1.2f, 1f), "vineta_1");

        // ---- 2) LA PREVIA (living de depto con luces de fiesta) ----
        Transform previa = new GameObject("Vineta2_Previa").transform;
        previa.position = new Vector3(100f, 0f, 0f);
        Vector3 o = previa.position;
        Caja(previa, o + new Vector3(0f, -0.05f, 0f), new Vector3(9f, 0.1f, 7f), new Color(0.45f, 0.3f, 0.22f));
        Caja(previa, o + new Vector3(0f, 1.5f, 3.5f), new Vector3(9f, 3f, 0.1f), new Color(0.55f, 0.6f, 0.75f));
        Caja(previa, o + new Vector3(-4.5f, 1.5f, 0f), new Vector3(0.1f, 3f, 7f), new Color(0.5f, 0.55f, 0.7f));
        Caja(previa, o + new Vector3(0f, 3.05f, 0f), new Vector3(9f, 0.1f, 7f), new Color(0.5f, 0.5f, 0.6f));
        Caja(previa, o + new Vector3(4.5f, 1.5f, 0f), new Vector3(0.1f, 3f, 7f), new Color(0.5f, 0.55f, 0.7f));
        Modelo(previa, "KayKit_Furniture/couch_pillows.fbx", new Bounds(o + new Vector3(0f, 0.45f, 2.6f), new Vector3(2.6f, 0.9f, 1f)), 180f);
        Modelo(previa, "KayKit_Furniture/table_low.fbx", new Bounds(o + new Vector3(0f, 0.22f, 1.1f), new Vector3(1.6f, 0.44f, 0.8f)), 0f);
        Modelo(previa, "KayKit_Furniture/rug_rectangle_stripes_A.fbx", new Bounds(o + new Vector3(0f, 0.01f, 1f), new Vector3(3.5f, 0.02f, 2.4f)), 0f);
        Modelo(previa, "KayKit_Furniture/lamp_standing.fbx", new Bounds(o + new Vector3(-3.6f, 0.85f, 2.8f), new Vector3(0.5f, 1.7f, 0.5f)), 0f);
        Modelo(previa, "KayKit_Furniture/pictureframe_large_A.fbx", new Bounds(o + new Vector3(1.8f, 1.9f, 3.42f), new Vector3(1f, 0.8f, 0.06f)), 180f);
        for (int i = 0; i < 6; i++) // botellas sobre la mesa
            Cilindro(previa, o + new Vector3(-0.6f + i * 0.24f, 0.58f, 1.1f + (i % 2) * 0.15f), new Vector3(0.08f, 0.14f, 0.08f), i % 3 == 0 ? new Color(0.5f, 0.1f, 0.15f) : new Color(0.1f, 0.35f, 0.15f));
        Personaje(previa, "Aj", o + new Vector3(0.3f, 0f, -0.4f), 180f, "Victory", 2.0f);
        Personaje(previa, "James", o + new Vector3(-1.8f, 0f, 0.4f), 160f, "Taunt", 1.0f);
        Personaje(previa, "David", o + new Vector3(2.1f, 0f, 0.6f), 200f, "DrunkIdle", 2.0f);
        Personaje(previa, "Martha", o + new Vector3(-0.8f, 0f, 2.2f), 180f, "Yawn", 2.5f);
        Luz(previa, o + new Vector3(-2.5f, 2.6f, 0f), new Color(1f, 0.3f, 0.8f), 8f, 4f);   // luces de fiesta
        Luz(previa, o + new Vector3(2.5f, 2.6f, 0f), new Color(0.3f, 0.6f, 1f), 8f, 4f);
        Luz(previa, o + new Vector3(-3.6f, 1.6f, 2.8f), new Color(1f, 0.8f, 0.5f), 5f, 3f);
        Ambiente(new Color(0.3f, 0.25f, 0.4f), new Color(0.15f, 0.12f, 0.2f));
        sol.intensity = 0.2f;
        Foto(cam, o + new Vector3(0.6f, 1.7f, -4.6f), o + new Vector3(0f, 1f, 1f), "vineta_2");

        // ---- 3) ¡VAMOS A VALPOOO! (carretera de noche) ----
        Transform ruta = new GameObject("Vineta3_Carretera").transform;
        ruta.position = new Vector3(200f, 0f, 0f);
        Vector3 r = ruta.position;
        Caja(ruta, r + new Vector3(0f, -0.05f, 30f), new Vector3(8f, 0.1f, 120f), new Color(0.12f, 0.12f, 0.14f));   // asfalto
        Caja(ruta, r + new Vector3(0f, -0.1f, 30f), new Vector3(80f, 0.1f, 120f), new Color(0.1f, 0.14f, 0.1f));    // pasto
        for (int i = 0; i < 14; i++)
            Caja(ruta, r + new Vector3(0f, 0.01f, -10f + i * 6f), new Vector3(0.15f, 0.02f, 2.5f), new Color(0.9f, 0.85f, 0.4f)); // linea central
        Modelo(ruta, "KayKit_CityBuilder/car_sedan.fbx", new Bounds(r + new Vector3(1.6f, 0.65f, 2f), new Vector3(1.9f, 1.3f, 4.2f)), 0f);
        // Focos del auto
        foreach (float x in new[] { 0.95f, 2.25f })
        {
            Light f = Luz(ruta, r + new Vector3(x, 0.7f, 4.2f), new Color(1f, 0.95f, 0.8f), 30f, 6f);
            f.type = LightType.Spot;
            f.spotAngle = 50f;
            f.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
        }
        // Letrero verde de carretera
        Caja(ruta, r + new Vector3(5.2f, 2.5f, 14f), new Vector3(0.15f, 5f, 0.15f), new Color(0.5f, 0.5f, 0.52f));
        Caja(ruta, r + new Vector3(3.6f, 4.6f, 14f), new Vector3(3.8f, 1.6f, 0.1f), new Color(0.05f, 0.45f, 0.2f));
        Texto(ruta, "VALPARAÍSO  116 km", new Vector3(r.x + 3.6f, 4.85f, 13.93f), 180f, 3f, Color.white);
        Texto(ruta, "Viña del Mar  →", new Vector3(r.x + 3.6f, 4.25f, 13.93f), 180f, 2f, Color.white);
        Texto(ruta, "¡VAMOS A VALPOOO!", new Vector3(r.x + 1.6f, 2.1f, 2f), 180f, 2.2f, new Color(1f, 0.85f, 0.3f));
        Ambiente(new Color(0.12f, 0.14f, 0.28f), new Color(0.04f, 0.05f, 0.1f));
        sol.color = new Color(0.5f, 0.6f, 1f);
        sol.intensity = 0.25f;
        sol.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/BadCarreteDay/Materiales/Cielo_Madrugada.mat");
        Foto(cam, r + new Vector3(-2.5f, 2.2f, -5f), r + new Vector3(2.2f, 1.6f, 9f), "vineta_3");

        if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
        poses.Clear();
        Directory.CreateDirectory("Assets/BadCarreteDay/Escenas");
        EditorSceneManager.SaveScene(escena, "Assets/BadCarreteDay/Escenas/Vinetas.unity");
        AssetDatabase.Refresh();
        foreach (string n in new[] { "vineta_1", "vineta_2", "vineta_3" })
        {
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(carpetaIntro + n + ".png");
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }
        Debug.Log("[BadCarreteDay] Viñetas 3D listas");
    }

    // ===== Piezas =====

    static GameObject Caja(Transform padre, Vector3 pos, Vector3 tam, Color color, float emision = 0f)
    {
        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(c.GetComponent<Collider>());
        c.transform.SetParent(padre, true);
        c.transform.position = pos;
        c.transform.localScale = tam;
        Material m = ConstructorF2.Material("Vineta_" + ColorUtility.ToHtmlStringRGB(color) + (emision > 0 ? "_E" : ""), color);
        if (emision > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * emision); }
        c.GetComponent<Renderer>().sharedMaterial = m;
        return c;
    }

    static void Cilindro(Transform padre, Vector3 pos, Vector3 tam, Color color)
    {
        GameObject c = Caja(padre, pos, tam, color);
        Mesh cil = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
        c.GetComponent<MeshFilter>().sharedMesh = cil;
    }

    static void Modelo(Transform padre, string ruta, Bounds caja, float rotY)
    {
        ConstructorF5.Ajustar(arte + "Entorno/" + ruta, caja, rotY, padre, System.IO.Path.GetFileNameWithoutExtension(ruta));
    }

    static TMP_Text Texto(Transform padre, string texto, Vector3 pos, float rotY, float tamanio, Color color)
    {
        GameObject obj = new GameObject("Texto");
        obj.transform.SetParent(padre, true);
        obj.transform.position = pos;
        obj.transform.rotation = Quaternion.Euler(0f, rotY + 180f, 0f);
        TextMeshPro t = obj.AddComponent<TextMeshPro>();
        t.text = texto;
        t.fontSize = tamanio;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.enableWordWrapping = false;
        if (ConstructorF5.Fuente() != null) t.font = ConstructorF5.Fuente();
        t.rectTransform.sizeDelta = new Vector2(10f, 2f);
        return t;
    }

    static Light Luz(Transform padre, Vector3 pos, Color color, float alcance, float intensidad)
    {
        Light l = new GameObject("Luz").AddComponent<Light>();
        l.transform.SetParent(padre, true);
        l.transform.position = pos;
        l.type = LightType.Point;
        l.color = color;
        l.range = alcance;
        l.intensity = intensidad;
        return l;
    }

    static void Ambiente(Color cielo, Color suelo)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = cielo;
        RenderSettings.ambientEquatorColor = Color.Lerp(cielo, suelo, 0.5f);
        RenderSettings.ambientGroundColor = suelo;
        RenderSettings.fog = false;
    }

    // Personaje de Mixamo con una pose tomada de una animacion (para que no quede en "T")
    // Poses pendientes: se muestrean todas juntas antes de cada foto
    // (si se muestrean de a una, cada muestreo borra la pose anterior)
    class Pose { public GameObject personaje; public AnimationClip clip; public float segundo; }
    static System.Collections.Generic.List<Pose> poses = new System.Collections.Generic.List<Pose>();

    static GameObject Personaje(Transform padre, string modelo, Vector3 pos, float rotY, string clip, float segundo)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(arte + "Personajes/" + modelo + ".fbx");
        GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        PrefabUtility.UnpackPrefabInstance(p, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        p.transform.SetParent(padre, true);
        p.transform.position = pos;
        p.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        AnimationClip c = ConstructorBadCarreteDay.BuscarClip(clip);
        Animator anim = p.GetComponent<Animator>();
        Debug.Log("[Vinetas] " + modelo + " clip=" + (c != null ? c.name + " humano=" + c.isHumanMotion : "NULL")
            + " avatar=" + (anim != null && anim.avatar != null ? anim.avatar.name + " humano=" + anim.avatar.isHuman : "NULL"));
        if (c != null && anim != null)
        {
            // Pose: se "muestrea" el clip en el segundo indicado (modo animacion del editor).
            // El modo queda activo hasta terminar las fotos.
            anim.applyRootMotion = false;
            poses.Add(new Pose { personaje = p, clip = c, segundo = segundo });
            p.transform.position = pos;
            p.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        }
        return p;
    }

    static void Foto(Camera cam, Vector3 desde, Vector3 hacia, string nombre)
    {
        if (!AnimationMode.InAnimationMode()) AnimationMode.StartAnimationMode();
        AnimationMode.BeginSampling();
        foreach (Pose pose in poses)
            AnimationMode.SampleAnimationClip(pose.personaje, pose.clip, pose.segundo);
        AnimationMode.EndSampling();
        cam.transform.position = desde;
        cam.transform.LookAt(hacia);
        int w = 1920, h = 1080;
        RenderTexture rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;
        cam.Render();
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        Directory.CreateDirectory(carpetaIntro);
        File.WriteAllBytes(carpetaIntro + nombre + ".png", tex.EncodeToPNG());
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }
}
