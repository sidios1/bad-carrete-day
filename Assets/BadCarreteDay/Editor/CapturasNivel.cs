using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Herramienta de EDITOR: saca fotos del nivel desde varios puntos (para revisar el arte).
// Uso: Unity -batchmode -projectPath ... -executeMethod CapturasNivel.Capturar -carpeta <ruta> -quit
public static class CapturasNivel
{
    [MenuItem("Bad Carrete Day/Sacar capturas del Nivel 1")]
    public static void Capturar()
    {
        string carpeta = Path.Combine(Application.dataPath, "../Capturas");
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-carpeta") carpeta = args[i + 1];
        Directory.CreateDirectory(carpeta);

        EditorSceneManager.OpenScene("Assets/BadCarreteDay/Escenas/Nivel1_Cerro.unity");

        GameObject obj = new GameObject("CamaraCapturas");
        Camera cam = obj.AddComponent<Camera>();
        cam.fieldOfView = 55f;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        // Altura de ojos del jugador (~1,7 m sobre el piso) en cada zona, y vistas hacia los bordes
        Foto(cam, carpeta, "01_pasaje", new Vector3(0f, 1.8f, 0f), new Vector3(0f, 1.5f, 14f));
        Foto(cam, carpeta, "02_don_lucho", new Vector3(-1f, 1.6f, 7.8f), new Vector3(-3.6f, 1f, 10.5f));
        Foto(cam, carpeta, "02b_sitio_eriazo", new Vector3(1.5f, 1.8f, 10f), new Vector3(10f, 2f, 17f));
        Foto(cam, carpeta, "02c_vista_al_plan", new Vector3(11f, 1.8f, 16f), new Vector3(60f, -25f, 20f));
        Foto(cam, carpeta, "03_plaza_almacen", new Vector3(4f, 1.8f, 33f), new Vector3(-8f, 1.5f, 42f));
        Foto(cam, carpeta, "03b_kiosko_balcon", new Vector3(0f, 1.8f, 41f), new Vector3(8f, 2.5f, 37f));
        Foto(cam, carpeta, "03c_mirador_bahia", new Vector3(11f, 1.8f, 48f), new Vector3(80f, -30f, 60f));
        Foto(cam, carpeta, "04_toby_escalera", new Vector3(1.5f, 1.7f, 46f), new Vector3(0f, 1.5f, 57f));
        Foto(cam, carpeta, "04b_subiendo_escalera", new Vector3(0f, 3.6f, 58f), new Vector3(0f, 4.8f, 66f));
        Foto(cam, carpeta, "05_calle_alta", new Vector3(-4f, 5.8f, 64f), new Vector3(-3f, 5f, 90f));
        Foto(cam, carpeta, "05b_escalera_rota", new Vector3(-5f, 5.8f, 71f), new Vector3(1f, 6f, 80f));
        Foto(cam, carpeta, "05c_techos", new Vector3(5.4f, 9.8f, 82f), new Vector3(5.4f, 8f, 100f));
        Foto(cam, carpeta, "05d_patio", new Vector3(-6f, 5.8f, 92f), new Vector3(8f, 6f, 92f));
        Foto(cam, carpeta, "05e_ascensor", new Vector3(5f, 9.8f, 102f), new Vector3(6f, 12f, 112f));
        Foto(cam, carpeta, "06_cima_estacion", new Vector3(6f, 17.8f, 116.5f), new Vector3(0f, 17f, 125f));
        Foto(cam, carpeta, "06b_pasaje_oscuro", new Vector3(0f, 17.8f, 118.5f), new Vector3(0f, 17f, 130f));
        Foto(cam, carpeta, "07_cuma_kit", new Vector3(-0.2f, 17.4f, 123f), new Vector3(-1f, 17.4f, 125f));
        Foto(cam, carpeta, "08_cancha_julio", new Vector3(0f, 17.8f, 139f), new Vector3(0f, 17f, 152f));
        Foto(cam, carpeta, "08b_cancha_hacia_bahia", new Vector3(-8f, 17.8f, 150f), new Vector3(60f, -10f, 150f));
        Foto(cam, carpeta, "10_bahia_desde_cima", new Vector3(0f, 30f, 160f), new Vector3(80f, -30f, 100f));
        Foto(cam, carpeta, "11_cerro_oeste", new Vector3(-5f, 6f, 92f), new Vector3(-60f, 30f, 100f));
        Foto(cam, carpeta, "12_vista_general", new Vector3(60f, 40f, -40f), new Vector3(0f, 8f, 70f));
        Foto(cam, carpeta, "13_aerea", new Vector3(40f, 90f, 20f), new Vector3(0f, 5f, 80f));
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        Foto(cam, carpeta, "09_benja", jugador.transform.position + new Vector3(0.8f, 1.6f, 2.6f), jugador.transform.position + Vector3.up * 1f);

        Object.DestroyImmediate(obj);
        Debug.Log("[Capturas] listas en " + carpeta);
    }

    // Imagenes para el menu (portada) y la intro (viñeta del despertar), sin personajes ni HUD
    [MenuItem("Bad Carrete Day/F6 - Capturar arte de menu e intro")]
    public static void CapturarArteMenu()
    {
        string carpeta = Path.Combine(Application.dataPath, "BadCarreteDay/Arte/Intro");
        Directory.CreateDirectory(carpeta);
        EditorSceneManager.OpenScene("Assets/BadCarreteDay/Escenas/Nivel1_Cerro.unity");
        foreach (Animator a in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
            if (a.GetComponentInChildren<SkinnedMeshRenderer>() != null) a.gameObject.SetActive(false);
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
        foreach (Moneda m in Object.FindObjectsByType<Moneda>(FindObjectsSortMode.None)) m.gameObject.SetActive(false);

        GameObject obj = new GameObject("CamaraCapturas");
        Camera cam = obj.AddComponent<Camera>();
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        cam.fieldOfView = 50f;
        Foto(cam, carpeta, "portada", new Vector3(1.5f, 7.5f, -3.5f), new Vector3(-1f, 6f, 50f), 1920, 1080);
        cam.fieldOfView = 70f;
        Foto(cam, carpeta, "vineta_despertar", new Vector3(0.3f, 0.35f, 2f), new Vector3(1.2f, 2.2f, 14f), 1920, 1080);
        Object.DestroyImmediate(obj);
        AssetDatabase.Refresh();
        foreach (string n in new[] { "portada", "vineta_despertar" })
        {
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath("Assets/BadCarreteDay/Arte/Intro/" + n + ".png");
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single; // una sola imagen (no "Multiple")
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }
        Debug.Log("[Capturas] arte de menu listo");
    }

    // Capturas de las pantallas de UI (menu, intro, fin) para revisarlas
    public static void CapturarPantallas()
    {
        string carpeta = Path.Combine(Application.dataPath, "../Capturas");
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-carpeta") carpeta = args[i + 1];
        Directory.CreateDirectory(carpeta);
        Pantalla(carpeta, "MenuInicio", "10_menu", null);
        Pantalla(carpeta, "Intro", "11_intro", null);
        Pantalla(carpeta, "Fin", "12_fin_victoria", "PanelDerrota");
        Pantalla(carpeta, "Fin", "13_fin_derrota", "PanelVictoria");
        Pantalla(carpeta, "MenuInicio", "14_configuracion", null, "PanelConfiguracion");
        Pantalla(carpeta, "MenuInicio", "15_creditos", null, "PanelCreditos");
    }

    static void Pantalla(string carpeta, string escena, string nombre, string ocultar, string mostrar = null)
    {
        EditorSceneManager.OpenScene("Assets/BadCarreteDay/Escenas/" + escena + ".unity");
        if (mostrar != null)
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == mostrar) t.gameObject.SetActive(true);
        if (ocultar != null) GameObject.Find(ocultar).SetActive(false);
        if (escena == "Intro") { SecuenciaIntro si = Object.FindFirstObjectByType<SecuenciaIntro>(); si.SendMessage("Mostrar", 3); }
        Camera cam = Camera.main;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = 1f;
        }
        Canvas.ForceUpdateCanvases();
        Foto(cam, carpeta, nombre, cam.transform.position, cam.transform.position + cam.transform.forward, 1920, 1080);
    }

    static void Foto(Camera cam, string carpeta, string nombre, Vector3 desde, Vector3 hacia)
    {
        Foto(cam, carpeta, nombre, desde, hacia, 1280, 720);
    }

    static void Foto(Camera cam, string carpeta, string nombre, Vector3 desde, Vector3 hacia, int ancho, int alto)
    {
        cam.transform.position = desde;
        cam.transform.LookAt(hacia);
        RenderTexture rt = new RenderTexture(ancho, alto, 24);
        cam.targetTexture = rt;
        cam.Render(); // calentamiento: el primer render puede salir sin sombras/luces listas
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(carpeta, nombre + ".png"), tex.EncodeToPNG());
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }
}
