using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Herramienta de EDITOR para la Fase 6: Menu principal, Intro (viñetas), Fin (final),
// creditos con licencias, orden del Build y compilacion para Mac.
public static class ConstructorF6
{
    const string escenas = "Assets/BadCarreteDay/Escenas/";
    const string rutaMenu = escenas + "MenuInicio.unity";
    const string rutaIntro = escenas + "Intro.unity";
    const string rutaNivel = escenas + "Nivel1_Cerro.unity";
    const string rutaFin = escenas + "Fin.unity";
    const string arte = "Assets/BadCarreteDay/Arte/";

    // Texto de creditos (licencias verificadas en la F5)
    public const string creditos =
        "<b>BAD CARRETE DAY</b> — Nivel 1: \"El Pastero\"\n" +
        "Proyecto académico · Desarrollo de Videojuegos · UCSC 2026\n" +
        "Inspirado en la jugabilidad de Conker's Bad Fur Day (Rare). Personajes, mundo e historia originales.\n\n" +
        "<b>Código base</b>: repositorio de clase ClaseUCSC2026 · Unity Starter Assets – Third Person (Unity Companion License)\n" +
        "<b>Personajes y animaciones</b>: Mixamo (Adobe) — Aj, Peasant Man, Mremireh O Desbiens, James, David, Martha\n" +
        "<b>Entorno</b>: KayKit City Builder, Restaurant y Furniture Bits — Kay Lousberg (CC0)\n" +
        "Kenney City Kit Suburban, Roads, Industrial y Cube Pets — Kenney.nl (CC0)\n" +
        "<b>Música</b>: \"Stumble Around\" — Zane Little Music (CC0) · \"Comedy Funny Background Song\" — Almitory (CC0)\n" +
        "Jingles: Kenney Music Jingles (CC0)\n" +
        "<b>Sonidos</b>: \"Dog barking mono\" — Brandon Morris, OpenGameArt (CC0) · Kenney Impact Sounds (CC0)\n" +
        "<b>Fuente</b>: Luckiest Guy — Astigmatic (Apache License 2.0)\n\n" +
        "Hecho con Unity 6.";

    [MenuItem("Bad Carrete Day/F6 - Construir menu, intro y fin")]
    public static void ConstruirF6()
    {
        ConstructorVinetas.CrearVinetas(); // F7: viñetas 3D de la intro
        // Las imagenes del menu/intro deben importarse como Sprite unico
        foreach (string n in new[] { "portada", "vineta_despertar" })
        {
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(arte + "Intro/" + n + ".png");
            if (ti != null && (ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single))
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
        }
        OrdenarBuild(); // primero, para que los indices queden fijos
        CrearMenu();
        CrearIntro();
        CrearFin();
        OrdenarBuild();
        Debug.Log("[BadCarreteDay] F6 construida ✅");
    }

    // ===== BUILD SETTINGS =====
    // 0 Menu · 1 Intro · 2 Nivel 1 · 3 Fin · (escenas de prueba al final: las usan las pruebas automaticas)
    public static void OrdenarBuild()
    {
        string[] orden = { rutaMenu, rutaIntro, rutaNivel, rutaFin,
                           escenas + "Prueba_F2.unity", escenas + "Prueba_F1.unity", escenas + "Prueba_F0.unity" };
        List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>();
        foreach (string ruta in orden)
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta) != null)
                lista.Add(new EditorBuildSettingsScene(ruta, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }

    // ===== MENU PRINCIPAL =====

    [MenuItem("Bad Carrete Day/F6 - Crear menu")]
    public static void CrearMenu()
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        GameObject sistemas = new GameObject("Sistemas");
        ControladorCargaEscena carga = ConstructorBadCarreteDay.Agregar<ControladorCargaEscena>(sistemas);
        ControladorMenu menu = ConstructorBadCarreteDay.Agregar<ControladorMenu>(sistemas);
        menu.cargaEscena = carga;
        Musica(sistemas, "Musica/acoustic_comedy.mp3", 0.45f);

        GameObject canvas = ConstructorF2.CrearCanvas("CanvasMenu");
        Fondo(canvas.transform, "Portada", arte + "Intro/portada.png", Color.white);
        Pantalla(canvas.transform, "Oscurecer", new Color(0.12f, 0.05f, 0.22f, 0.35f));

        TMP_Text titulo = ConstructorF2.Texto(canvas.transform, "Titulo", "BAD CARRETE DAY", new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1700f, 220f), 150, TextAlignmentOptions.Center);
        titulo.color = new Color(1f, 0.85f, 0.3f);
        titulo.outlineWidth = 0.25f;
        titulo.outlineColor = new Color32(70, 20, 90, 255);
        TMP_Text sub = ConstructorF2.Texto(canvas.transform, "Subtitulo", "Nivel 1: El Pastero", new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(1200f, 80f), 56, TextAlignmentOptions.Center);
        sub.color = new Color(1f, 0.55f, 0.8f);

        ConstructorF2.Boton(canvas.transform, "BotonJugar", "Jugar", new Vector2(0f, -40f), menu.Jugar);
        GameObject panelCreditos = Creditos(canvas.transform);
        menu.panelCreditos = panelCreditos;
        GameObject panelConfig = PanelConfiguracion(canvas.transform);
        BotonBool(canvas.transform, "BotonConfiguracion", "Configuración", new Vector2(0f, -150f), panelConfig.SetActive, true);
        BotonBool(canvas.transform, "BotonCreditos", "Créditos", new Vector2(0f, -260f), menu.MostrarCreditos, true);
        ConstructorF2.Boton(canvas.transform, "BotonSalir", "Salir", new Vector2(0f, -370f), menu.Salir);

        ConstructorF2.Texto(canvas.transform, "Controles",
            "WASD mover · Shift correr · Espacio saltar (suelta y mantén en el aire: ¡planear!) · Clic atacar · E interactuar · Esc pausa",
            new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1800f, 60f), 28, TextAlignmentOptions.Center);

        // Los paneles van encima de todo (menos la pantalla de carga)
        panelConfig.transform.SetAsLastSibling();
        panelCreditos.transform.SetAsLastSibling();
        PantallaCarga(canvas.transform, carga);
        ConstructorF2.CrearEventSystem();
        ConstructorF5.AplicarFuente(canvas, "TextoCreditos", "Controles");
        EditorSceneManager.SaveScene(escena, rutaMenu);
    }

    // ===== INTRO (viñetas del guion) =====

    [MenuItem("Bad Carrete Day/F6 - Crear intro")]
    public static void CrearIntro()
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        GameObject sistemas = new GameObject("Sistemas");
        ControladorCargaEscena carga = ConstructorBadCarreteDay.Agregar<ControladorCargaEscena>(sistemas);
        SecuenciaIntro intro = ConstructorBadCarreteDay.Agregar<SecuenciaIntro>(sistemas);
        intro.cargaEscena = carga;
        intro.inputAvanzar = ConstructorBadCarreteDay.BuscarInputActionReference("Jump");
        intro.inputAvanzarAlt = ConstructorBadCarreteDay.BuscarInputActionReference("Interaccion");
        intro.inputSaltar = ConstructorBadCarreteDay.BuscarInputActionReference("Pausa");
        Musica(sistemas, "Musica/acoustic_comedy.mp3", 0.3f);

        GameObject canvas = ConstructorF2.CrearCanvas("CanvasIntro");
        intro.fondo = Pantalla(canvas.transform, "Fondo", Color.black).GetComponent<Image>();
        intro.imagen = Fondo(canvas.transform, "Imagen", arte + "Intro/vineta_despertar.png", Color.white).GetComponent<Image>();

        // Recuadro de historieta abajo
        GameObject caja = ConstructorF2.Panel(canvas.transform, "CajaTexto", new Color(1f, 0.97f, 0.88f, 0.95f), new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(1500f, 260f));
        intro.textoVineta = ConstructorF2.Texto(caja.transform, "TextoVineta", "...", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1420f, 230f), 46, TextAlignmentOptions.Center);
        intro.textoVineta.color = new Color(0.15f, 0.08f, 0.25f);

        GameObject cartel = ConstructorF2.Panel(canvas.transform, "Cartel", new Color(1f, 0.85f, 0.3f, 1f), new Vector2(0f, 1f), new Vector2(560f, -90f), new Vector2(1040f, 100f));
        intro.textoEncabezado = ConstructorF2.Texto(cartel.transform, "TextoEncabezado", "...", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1010f, 90f), 46, TextAlignmentOptions.Center);
        intro.textoEncabezado.color = new Color(0.3f, 0.08f, 0.35f);

        intro.textoContador = ConstructorF2.Texto(canvas.transform, "Contador", "1 / 4", new Vector2(1f, 1f), new Vector2(-120f, -70f), new Vector2(200f, 60f), 36, TextAlignmentOptions.Right);
        ConstructorF2.Texto(canvas.transform, "Ayuda", "Espacio / E / clic: seguir  ·  Esc: saltar intro", new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1200f, 40f), 26, TextAlignmentOptions.Center);
        ConstructorF2.Boton(canvas.transform, "BotonSiguiente", "Seguir >", new Vector2(760f, -130f), intro.Siguiente);

        Sprite despertar = AssetDatabase.LoadAssetAtPath<Sprite>(arte + "Intro/vineta_despertar.png");
        intro.vinetas = new SecuenciaIntro.Vineta[]
        {
            new SecuenciaIntro.Vineta { encabezado = "Concepción, 18:00", colorFondo = new Color(0.15f, 0.2f, 0.45f), imagen = AssetDatabase.LoadAssetAtPath<Sprite>(arte + "Intro/vineta_1.png"),
                texto = "Último certamen de Cálculo II.\nBenja: \"Fácil. Lo saqué. Esto se celebra.\"" },
            new SecuenciaIntro.Vineta { encabezado = "19:30", colorFondo = new Color(0.35f, 0.15f, 0.5f), imagen = AssetDatabase.LoadAssetAtPath<Sprite>(arte + "Intro/vineta_2.png"),
                texto = "\"Una previa tranquila\", dijeron." },
            new SecuenciaIntro.Vineta { encabezado = "01:12", colorFondo = new Color(0.75f, 0.2f, 0.5f), imagen = AssetDatabase.LoadAssetAtPath<Sprite>(arte + "Intro/vineta_3.png"),
                texto = "Alguien gritó: \"¡VAMOS A VALPOOO!\"" },
            new SecuenciaIntro.Vineta { encabezado = "05:47 · Algún cerro de Valparaíso", colorFondo = Color.black, imagen = despertar,
                texto = "Benja: \"¿Dónde... estoy? ¿Por qué huelo a meao? ...¿Y MI CELULAR?\"" },
        };

        PantallaCarga(canvas.transform, carga);
        ConstructorF2.CrearEventSystem();
        ConstructorF5.AplicarFuente(canvas, "Ayuda");
        EditorSceneManager.SaveScene(escena, rutaIntro);
    }

    // ===== FIN =====

    [MenuItem("Bad Carrete Day/F6 - Crear fin")]
    public static void CrearFin()
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        GameObject sistemas = new GameObject("Sistemas");
        ControladorCargaEscena carga = ConstructorBadCarreteDay.Agregar<ControladorCargaEscena>(sistemas);
        PantallaFin pantalla = ConstructorBadCarreteDay.Agregar<PantallaFin>(sistemas);
        pantalla.cargaEscena = carga;

        GameObject canvas = ConstructorF2.CrearCanvas("CanvasFin");
        Fondo(canvas.transform, "Fondo", arte + "Intro/portada.png", Color.white);
        Pantalla(canvas.transform, "Oscurecer", new Color(0.1f, 0.04f, 0.18f, 0.7f));

        // --- VICTORIA: el telefono ---
        GameObject victoria = Pantalla(canvas.transform, "PanelVictoria", new Color(0f, 0f, 0f, 0f));
        TMP_Text t1 = ConstructorF2.Texto(victoria.transform, "Titulo", "¡RECUPERASTE EL TELÉFONO!", new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(1700f, 130f), 86, TextAlignmentOptions.Center);
        t1.color = new Color(1f, 0.85f, 0.3f);

        GameObject telefono = ConstructorF2.Panel(victoria.transform, "Telefono", new Color(0.08f, 0.08f, 0.1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-460f, -10f), new Vector2(440f, 640f));
        GameObject pantallaTel = ConstructorF2.Panel(telefono.transform, "PantallaTelefono", new Color(0.55f, 0.45f, 0.8f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 580f));
        TMP_Text bateria = ConstructorF2.Texto(pantallaTel.transform, "Bateria", "05:58          1%", new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(400f, 40f), 26, TextAlignmentOptions.Center);
        bateria.color = new Color(1f, 0.35f, 0.35f);
        GameObject notif = ConstructorF2.Panel(pantallaTel.transform, "Notificacion", new Color(1f, 1f, 1f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(390f, 200f));
        TMP_Text tn = ConstructorF2.Texto(notif.transform, "TextoNotificacion",
            "<b>Portal UCSC</b>\nNota Certamen Final Cálculo II: <b><color=#D62828>2,1</color></b>\nExamen: jueves 08:00, Concepción.",
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(370f, 185f), 28, TextAlignmentOptions.Left);
        tn.color = new Color(0.1f, 0.08f, 0.15f);

        TMP_Text benja = ConstructorF2.Texto(victoria.transform, "ReaccionBenja", "Benja: \"¿¡DOS COMA UNO!? ...¿Y estoy en VALPARAÍSO?\"",
            new Vector2(0.5f, 0.5f), new Vector2(330f, 120f), new Vector2(820f, 220f), 52, TextAlignmentOptions.Center);
        TMP_Text continuara = ConstructorF2.Texto(victoria.transform, "Continuara", "CONTINUARÁ...\n<size=60%>Baja del cerro sin que te vea El Traficante.</size>",
            new Vector2(0.5f, 0.5f), new Vector2(330f, -120f), new Vector2(820f, 220f), 70, TextAlignmentOptions.Center);
        continuara.color = new Color(1f, 0.55f, 0.8f);
        pantalla.apareceEnOrden = new GameObject[] { notif, benja.gameObject, continuara.gameObject };
        pantalla.panelVictoria = victoria;

        // --- DERROTA ---
        GameObject derrota = Pantalla(canvas.transform, "PanelDerrota", new Color(0f, 0f, 0f, 0f));
        TMP_Text t2 = ConstructorF2.Texto(derrota.transform, "Titulo", "TE DEJARON PATO", new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1600f, 160f), 120, TextAlignmentOptions.Center);
        t2.color = new Color(1f, 0.35f, 0.35f);
        pantalla.textoFraseDerrota = ConstructorF2.Texto(derrota.transform, "FraseDerrota", "...", new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1500f, 80f), 44, TextAlignmentOptions.Center);
        pantalla.frasesDerrota = new string[] {
            "Ni Don Lucho se cae tanto.",
            "Julio lleva tres días sin dormir y aun así te ganó.",
            "Tu mamá te dijo que no fueras a ese carrete.",
        };
        ConstructorF2.Boton(derrota.transform, "BotonReintentar", "Reintentar", new Vector2(0f, -80f), pantalla.Reintentar);
        pantalla.panelDerrota = derrota;

        // --- Botones comunes y creditos ---
        ConstructorF2.Boton(canvas.transform, "BotonMenu", "Menú", new Vector2(130f, -430f), pantalla.IrAlMenu);
        BotonBool(canvas.transform, "BotonCreditos", "Créditos", new Vector2(530f, -430f), pantalla.MostrarCreditos, true);
        pantalla.panelCreditos = Creditos(canvas.transform);

        PantallaCarga(canvas.transform, carga);
        ConstructorF2.CrearEventSystem();
        ConstructorF5.AplicarFuente(canvas, "TextoCreditos", "TextoNotificacion", "Bateria");
        EditorSceneManager.SaveScene(escena, rutaFin);
    }

    // ===== PANEL DE CONFIGURACION (menu principal y pausa) =====

    public static GameObject PanelConfiguracion(Transform padre)
    {
        GameObject panel = Pantalla(padre, "PanelConfiguracion", new Color(0.1f, 0.05f, 0.18f, 1f));
        ControladorConfiguracion conf = ConstructorBadCarreteDay.Agregar<ControladorConfiguracion>(panel);
        TMP_Text titulo = ConstructorF2.Texto(panel.transform, "TituloConfiguracion", "CONFIGURACIÓN", new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1200f, 110f), 80, TextAlignmentOptions.Center);
        titulo.color = new Color(1f, 0.85f, 0.3f);

        conf.textoVolumen = ConstructorF2.Texto(panel.transform, "TextoVolumen", "Volumen", new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(900f, 60f), 40, TextAlignmentOptions.Center);
        conf.sliderVolumen = SliderConfig(panel.transform, "SliderVolumen", new Vector2(0f, 100f), 0f, 1f);
        UnityEventTools.AddPersistentListener(conf.sliderVolumen.onValueChanged, conf.CambiarVolumen);

        conf.textoSensibilidad = ConstructorF2.Texto(panel.transform, "TextoSensibilidad", "Sensibilidad", new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(900f, 60f), 40, TextAlignmentOptions.Center);
        conf.sliderSensibilidad = SliderConfig(panel.transform, "SliderSensibilidad", new Vector2(0f, -90f), 0.2f, 3f);
        UnityEventTools.AddPersistentListener(conf.sliderSensibilidad.onValueChanged, conf.CambiarSensibilidad);

        GameObject b = ConstructorF2.Panel(panel.transform, "BotonVolver", new Color(1f, 0.35f, 0.7f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(360f, 90f));
        Button btn = b.AddComponent<Button>();
        UnityEventTools.AddBoolPersistentListener(btn.onClick, panel.SetActive, false);
        ConstructorF2.Texto(b.transform, "Texto", "Volver", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 80f), 40, TextAlignmentOptions.Center);

        ConstructorF5.AplicarFuente(panel);
        panel.SetActive(false);
        return panel;
    }

    static Slider SliderConfig(Transform padre, string nombre, Vector2 pos, float min, float max)
    {
        GameObject obj = DefaultControls.CreateSlider(new DefaultControls.Resources());
        obj.name = nombre;
        obj.transform.SetParent(padre, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(800f, 40f);
        rt.anchoredPosition = pos;
        Slider s = obj.GetComponent<Slider>();
        s.minValue = min;
        s.maxValue = max;
        obj.transform.Find("Fill Area/Fill").GetComponent<Image>().color = new Color(1f, 0.35f, 0.7f);
        RectTransform manija = (RectTransform)obj.transform.Find("Handle Slide Area/Handle");
        manija.sizeDelta = new Vector2(40f, 0f);
        return s;
    }

    // ===== PIEZAS DE UI =====

    static GameObject Pantalla(Transform padre, string nombre, Color color)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform));
        obj.transform.SetParent(padre, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = color.a > 0.01f;
        return obj;
    }

    // Imagen a pantalla completa que cubre todo sin deformarse
    static GameObject Fondo(Transform padre, string nombre, string rutaSprite, Color color)
    {
        GameObject obj = Pantalla(padre, nombre, color);
        Image img = obj.GetComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(rutaSprite);
        img.raycastTarget = false;
        AspectRatioFitter ajuste = obj.AddComponent<AspectRatioFitter>();
        ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        ajuste.aspectRatio = 16f / 9f;
        return obj;
    }

    static GameObject Creditos(Transform padre)
    {
        GameObject panel = Pantalla(padre, "PanelCreditos", new Color(0.08f, 0.04f, 0.14f, 1f));
        TMP_Text t = ConstructorF2.Texto(panel.transform, "TituloCreditos", "CRÉDITOS Y LICENCIAS", new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1400f, 100f), 70, TextAlignmentOptions.Center);
        t.color = new Color(1f, 0.85f, 0.3f);
        ConstructorF2.Texto(panel.transform, "TextoCreditos", creditos, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1600f, 720f), 30, TextAlignmentOptions.Center);
        // Boton volver: apaga el panel
        GameObject b = ConstructorF2.Panel(panel.transform, "BotonVolver", new Color(1f, 0.35f, 0.7f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(360f, 90f));
        Button btn = b.AddComponent<Button>();
        UnityEventTools.AddBoolPersistentListener(btn.onClick, panel.SetActive, false);
        ConstructorF2.Texto(b.transform, "Texto", "Volver", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 80f), 40, TextAlignmentOptions.Center);
        panel.SetActive(false);
        return panel;
    }

    public static void BotonBool(Transform padre, string nombre, string texto, Vector2 pos, UnityEngine.Events.UnityAction<bool> accion, bool valor)
    {
        GameObject obj = ConstructorF2.Panel(padre, nombre, new Color(1f, 0.35f, 0.7f), new Vector2(0.5f, 0.5f), pos, new Vector2(360f, 90f));
        Button boton = obj.AddComponent<Button>();
        UnityEventTools.AddBoolPersistentListener(boton.onClick, accion, valor);
        ConstructorF2.Texto(obj.transform, "Texto", texto, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 80f), 40, TextAlignmentOptions.Center);
    }

    // Pantalla de carga que usa ControladorCargaEscena (clase): panel + barra + porcentaje
    static void PantallaCarga(Transform padre, ControladorCargaEscena carga)
    {
        GameObject panel = Pantalla(padre, "PantallaCarga", new Color(0.1f, 0.05f, 0.18f, 1f));
        ConstructorF2.Texto(panel.transform, "TextoCargando", "Cargando...", new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(800f, 100f), 64, TextAlignmentOptions.Center);
        GameObject slider = DefaultControls.CreateSlider(new DefaultControls.Resources());
        slider.name = "BarraProgreso";
        slider.transform.SetParent(panel.transform, false);
        RectTransform rt = (RectTransform)slider.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(900f, 40f);
        rt.anchoredPosition = new Vector2(0f, -20f);
        Slider s = slider.GetComponent<Slider>();
        s.interactable = false;
        Object.DestroyImmediate(slider.transform.Find("Handle Slide Area").gameObject);
        slider.transform.Find("Fill Area/Fill").GetComponent<Image>().color = new Color(1f, 0.85f, 0.3f);
        carga.pantallaCarga = panel;
        carga.barraProgreso = s;
        carga.textoPorcentaje = ConstructorF2.Texto(panel.transform, "TextoPorcentaje", "0%", new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(400f, 70f), 44, TextAlignmentOptions.Center);
        panel.SetActive(false);
    }

    static void Musica(GameObject obj, string clip, float volumen)
    {
        AudioSource a = obj.AddComponent<AudioSource>();
        a.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(arte + "Audio/" + clip);
        a.loop = true;
        a.playOnAwake = true;
        a.volume = volumen;
    }

    // ===== COMPILAR (Mac) =====

    // Build de PRUEBAS (Development): incluye los botones "IR A (PRUEBAS)" del menu de pausa
    [MenuItem("Bad Carrete Day/F6 - Compilar juego de PRUEBAS (Mac, con atajos)")]
    public static void CompilarMacPruebas() { CompilarMac("Builds/MacPruebas/BadCarreteDay_Pruebas.app", BuildOptions.Development); }

    [MenuItem("Bad Carrete Day/F6 - Compilar juego (Mac)")]
    public static void CompilarMac() { CompilarMac("Builds/Mac/BadCarreteDay.app", BuildOptions.None); }

    static void CompilarMac(string ruta, BuildOptions modo)
    {
        OrdenarBuild();
        List<string> rutas = new List<string>();
        foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
            if (e.enabled && !e.path.Contains("Prueba_")) rutas.Add(e.path); // el juego no incluye las escenas de prueba
        PlayerSettings.productName = "Bad Carrete Day";
        PlayerSettings.companyName = "UCSC Desarrollo de Videojuegos";
        BuildPlayerOptions opciones = new BuildPlayerOptions
        {
            scenes = rutas.ToArray(),
            locationPathName = ruta,
            target = BuildTarget.StandaloneOSX,
            options = modo,
        };
        BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        Debug.Log("[BadCarreteDay] Build: " + reporte.summary.result + " · " + (reporte.summary.totalSize / (1024f * 1024f)).ToString("F0") + " MB · errores=" + reporte.summary.totalErrors);
        if (reporte.summary.result != BuildResult.Succeeded && Application.isBatchMode) EditorApplication.Exit(1);
    }
}
