using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

// Herramienta de EDITOR — Revision de arte (F7): que cada escenario se vea lleno y con sentido.
// Se llama al final de ConstructorF5.VestirNivel (antes de hornear el NavMesh).
// REGLA: nada de esto cambia la jugabilidad. Los modelos nuevos van SIN collider
// (salvo que se diga lo contrario) y no se mueven zonas de interaccion ni triggers.
public static class ConstructorF7
{
    const string arte = "Assets/BadCarreteDay/Arte/";
    const string materiales = "Assets/BadCarreteDay/Materiales/";

    static Transform deco;               // padre de toda la decoracion F7
    static ConstructorF2.Base b;

    public static void VestirNivel(ConstructorF2.Base baseNivel)
    {
        b = baseNivel;
        deco = new GameObject("Decoracion_F7").transform;
        deco.SetParent(GameObject.Find("Nivel").transform, false);
        Random.InitState(1234); // decoracion "al azar" pero siempre igual

        // El mundo (terreno, edificios, zonas) lo arma ConstructorNivel1.
        // Aqui queda lo comun: bahia al pie del cerro y ventanas encendidas.
        Bahia();
        VentanasEncendidas();
        SonidosDeGolpe();
    }

    // ===================== V3: LUGARES CON SENTIDO =====================






    public static void Caseta(Transform padre, Vector3 basePos, string letrero, Material color)
    {
        Pieza(padre, "Caseta", PrimitiveType.Cube, basePos + Vector3.up * 1.1f, new Vector3(1.6f, 2.2f, 1.6f), Vector3.zero, color);
        Pieza(padre, "Caseta_Techo", PrimitiveType.Cube, basePos + Vector3.up * 2.3f, new Vector3(2f, 0.2f, 2f), Vector3.zero, ConstructorF2.Material("Almacen_Techo", new Color(0.35f, 0.18f, 0.15f)));
        Pieza(padre, "Caseta_Ventana", PrimitiveType.Cube, basePos + new Vector3(0f, 1.4f, -0.81f), new Vector3(1f, 0.6f, 0.02f), Vector3.zero, MaterialEmisivo("Ventana_Caseta", new Color(1f, 0.8f, 0.5f), 1.5f));
        Texto3D(padre, letrero, basePos + new Vector3(0f, 2.65f, -0.82f), 180f, 0.22f, new Color(1f, 0.9f, 0.4f));
    }




    // ===================== V4: DETALLE =====================






    // Sonido de golpe (Kenney Impact) al recibir un golpe: enemigos y jugador
    static void SonidosDeGolpe()
    {
        AudioClip golpe = AssetDatabase.LoadAssetAtPath<AudioClip>(arte + "Audio/SFX/Kenney_Impact/impactPunch_medium_001.ogg");
        if (golpe == null) return;
        foreach (DemoObjetivo o in Object.FindObjectsByType<DemoObjetivo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            AudioSource a = o.gameObject.AddComponent<AudioSource>();
            a.clip = golpe;
            a.playOnAwake = false;
            a.spatialBlend = 0.8f;
            a.volume = 0.8f;
            UnityEventTools.AddVoidPersistentListener(o.alSerGolpeado, a.Play);
        }
    }

    // ===================== PIEZAS DE DETALLE =====================

    public static void Botella(Transform padre, Vector3 pos, bool tirada)
    {
        Material vidrio = ConstructorF2.Material("Botella_Verde", new Color(0.1f, 0.35f, 0.15f));
        vidrio.SetFloat("_Smoothness", 0.9f);
        Vector3 rot = tirada ? new Vector3(90f, Random.Range(0f, 180f), 0f) : Vector3.zero;
        GameObject b = Pieza(padre, "Botella", PrimitiveType.Cylinder, pos, new Vector3(0.09f, 0.13f, 0.09f), rot, vidrio);
        if (!tirada) b.transform.position = pos + Vector3.up * 0.0f;
    }

    public static void Neumatico(Transform padre, Vector3 pos)
    {
        Pieza(padre, "Neumatico", PrimitiveType.Cylinder, pos, new Vector3(0.65f, 0.11f, 0.65f), new Vector3(0f, Random.Range(0f, 90f), 0f), ConstructorF2.Material("Goma", new Color(0.05f, 0.05f, 0.05f)));
    }

    // Cable con caida (catenaria aproximada) hecho de tramos de cilindro
    public static void Cable(Transform padre, Vector3 a, Vector3 b, Material mat)
    {
        int tramos = 10;
        float caida = Vector3.Distance(a, b) * 0.06f;
        Vector3 anterior = a;
        for (int i = 1; i <= tramos; i++)
        {
            float t = i / (float)tramos;
            Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * caida * 4f * t * (1f - t);
            Vector3 medio = (anterior + p) / 2f;
            Vector3 dir = p - anterior;
            GameObject tramo = Pieza(padre, "Cable", PrimitiveType.Cylinder, medio, new Vector3(0.025f, dir.magnitude / 2f, 0.025f), Vector3.zero, mat);
            tramo.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            anterior = p;
        }
    }

    // Cuerda con ropa colgada (paños de colores)
    public static void Tendedero(Transform padre, Vector3 a, Vector3 b)
    {
        Cable(padre, a, b, ConstructorF2.Material("Cuerda", new Color(0.8f, 0.8f, 0.75f)));
        Color[] colores = { new Color(0.9f, 0.3f, 0.35f), new Color(0.3f, 0.6f, 0.9f), new Color(0.95f, 0.85f, 0.3f), Color.white, new Color(0.4f, 0.8f, 0.45f) };
        int n = Mathf.Max(3, Mathf.RoundToInt(Vector3.Distance(a, b) / 1.2f));
        Vector3 dir = (b - a).normalized;
        float rotY = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + 90f;
        for (int i = 1; i < n; i++)
        {
            if (Random.value < 0.25f) continue;
            float t = i / (float)n;
            float caida = Vector3.Distance(a, b) * 0.06f * 4f * t * (1f - t);
            Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * (caida + Random.Range(0.25f, 0.4f));
            Pieza(padre, "Ropa", PrimitiveType.Cube, p, new Vector3(Random.Range(0.4f, 0.7f), Random.Range(0.45f, 0.75f), 0.02f), new Vector3(0f, rotY, 0f),
                  ConstructorF2.Material("Ropa_" + (i % colores.Length), colores[i % colores.Length]));
        }
    }

    public static void Grafiti(Transform padre, string texto, Vector3 pos, float rotY, Color color)
    {
        TMP_Text t = Texto3D(padre, texto, pos, rotY, 0.45f, color);
        t.transform.rotation = Quaternion.Euler(0f, rotY + 180f, Random.Range(-8f, 8f));
        t.fontStyle = FontStyles.Bold | FontStyles.Italic;
        t.outlineWidth = 0.2f;
        t.outlineColor = new Color32(10, 10, 10, 255);
    }

    public static void Mural(Transform padre, string nombre, Texture2D tex, Vector3 pos, float rotY, Vector2 tam, string texto, Color colorTexto)
    {
        Material m = ConstructorF2.Material(nombre, Color.white);
        m.SetTexture("_BaseMap", tex);
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = nombre;
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.SetParent(padre, false);
        q.transform.position = pos;
        // El Quad mira hacia -Z: lo giramos para que mire hacia el camino
        q.transform.rotation = Quaternion.Euler(0f, rotY + 180f, 0f);
        q.transform.localScale = new Vector3(tam.x, tam.y, 1f);
        q.GetComponent<Renderer>().sharedMaterial = m;
        TMP_Text t = Texto3D(padre, texto, pos + Quaternion.Euler(0f, rotY, 0f) * Vector3.forward * 0.03f + Vector3.down * tam.y * 0.32f, rotY, 0.5f, colorTexto);
        t.outlineWidth = 0.25f;
        t.outlineColor = new Color32(20, 10, 40, 255);
    }

    // Texto 3D (TextMeshPro) mirando hacia "rotY" (0 = mira hacia +Z)
    public static TMP_Text Texto3D(Transform padre, string texto, Vector3 pos, float rotY, float tamanio, Color color)
    {
        GameObject obj = new GameObject("Texto_" + texto);
        obj.transform.SetParent(padre, false);
        obj.transform.position = pos;
        // TMP se lee mirando hacia -Z de su transform: giramos 180 para que se lea desde rotY
        obj.transform.rotation = Quaternion.Euler(0f, rotY + 180f, 0f);
        TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
        tmp.text = texto;
        tmp.fontSize = tamanio * 10f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.enableWordWrapping = false;
        if (ConstructorF5.Fuente() != null) tmp.font = ConstructorF5.Fuente();
        tmp.rectTransform.sizeDelta = new Vector2(20f, 3f);
        return tmp;
    }

    public static Light LuzPunto(Transform padre, Vector3 pos, Color color, float alcance, float intensidad)
    {
        GameObject obj = new GameObject("Luz");
        obj.transform.SetParent(padre, true);
        obj.transform.position = pos;
        Light l = obj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = alcance;
        l.intensity = intensidad;
        return l;
    }

    // Pieza hija de un objeto ESCALADO, con tamaño y posicion en metros del mundo
    public static GameObject ParteMundo(Transform padre, string nombre, PrimitiveType tipo, Vector3 offsetMundo, Vector3 tamanioMundo, Material mat)
    {
        GameObject p = GameObject.CreatePrimitive(tipo);
        p.name = nombre;
        Object.DestroyImmediate(p.GetComponent<Collider>());
        p.transform.SetParent(padre, false);
        Vector3 e = padre.lossyScale;
        p.transform.localPosition = new Vector3(offsetMundo.x / e.x, offsetMundo.y / e.y, offsetMundo.z / e.z);
        p.transform.localScale = new Vector3(tamanioMundo.x / e.x, tamanioMundo.y / e.y, tamanioMundo.z / e.z);
        p.GetComponent<Renderer>().sharedMaterial = mat;
        return p;
    }

    public static void AplicarTextura(string nombreObjeto, string nombreMaterial, Texture2D tex, Color tinte, float metrosPorRepeticion)
    {
        GameObject obj = GameObject.Find(nombreObjeto);
        if (obj == null) return;
        Material m = ConstructorF2.Material(nombreMaterial, tinte);
        m.SetTexture("_BaseMap", tex);
        Vector3 e = obj.transform.lossyScale;
        m.SetTextureScale("_BaseMap", new Vector2(e.x / metrosPorRepeticion, e.z / metrosPorRepeticion));
        obj.GetComponent<Renderer>().sharedMaterial = m;
    }

    // ===================== TEXTURAS GENERADAS (originales) =====================

    public static Texture2D GuardarTextura(string nombre, Texture2D tex, bool repetir)
    {
        string ruta = materiales + "Texturas/" + nombre + ".png";
        ConstructorBadCarreteDay.CrearCarpeta(materiales + "Texturas");
        System.IO.File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(ruta);
        ti.wrapMode = repetir ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    // Adoquines: filas desplazadas de piedras con junta oscura y leve variacion de color
    public static Texture2D TexturaAdoquin()
    {
        int n = 256;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGB24, true);
        int altoFila = 32, anchoPiedra = 64;
        for (int y = 0; y < n; y++)
        {
            int fila = y / altoFila;
            int desfase = (fila % 2) * anchoPiedra / 2;
            for (int x = 0; x < n; x++)
            {
                int xx = (x + desfase) % n;
                int piedra = xx / anchoPiedra;
                int lx = xx % anchoPiedra, ly = y % altoFila;
                bool junta = lx < 3 || ly < 3;
                float variacion = Mathf.PerlinNoise(piedra * 3.1f + fila * 7.3f, fila * 1.7f) * 0.25f;
                float detalle = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.08f;
                float v = junta ? 0.25f : 0.62f + variacion + detalle;
                tex.SetPixel(x, y, new Color(v, v * 0.97f, v * 0.94f));
            }
        }
        tex.Apply();
        return GuardarTextura("Adoquin", tex, true);
    }

    // Concreto con manchas
    public static Texture2D TexturaConcreto()
    {
        int n = 256;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGB24, true);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float manchas = Mathf.PerlinNoise(x * 0.02f, y * 0.02f) * 0.25f;
                float grano = Mathf.PerlinNoise(x * 0.4f, y * 0.4f) * 0.08f;
                float v = 0.62f + manchas + grano;
                tex.SetPixel(x, y, new Color(v, v, v));
            }
        tex.Apply();
        return GuardarTextura("Concreto", tex, true);
    }

    // Mural de olas y sol (bahia de Valpo, estilo plano)
    public static Texture2D TexturaMuralOlas()
    {
        int w = 512, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = y / (float)h;
                Color c = Color.Lerp(new Color(1f, 0.55f, 0.3f), new Color(0.95f, 0.3f, 0.5f), v); // cielo
                float sol = Vector2.Distance(new Vector2(x, y), new Vector2(w * 0.7f, h * 0.62f));
                if (sol < 50f) c = new Color(1f, 0.85f, 0.25f);
                for (int k = 0; k < 4; k++) // franjas de olas
                {
                    float ola = h * (0.42f - k * 0.1f) + Mathf.Sin(x * 0.05f + k * 1.7f) * 10f;
                    if (y < ola) c = Color.Lerp(new Color(0.1f, 0.45f, 0.75f), new Color(0.05f, 0.2f, 0.45f), k / 3f);
                }
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return GuardarTextura("Mural_Olas", tex, false);
    }

    // Mural del ojo gigante
    public static Texture2D TexturaMuralOjo()
    {
        int w = 384, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        Vector2 centro = new Vector2(w / 2f, h * 0.58f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = ((x / 24 + y / 24) % 2 == 0) ? new Color(0.15f, 0.65f, 0.6f) : new Color(0.12f, 0.55f, 0.52f);
                Vector2 p = new Vector2(x, y);
                // forma de ojo: interseccion de dos circulos
                bool enOjo = Vector2.Distance(p, centro + Vector2.up * 90f) < 140f && Vector2.Distance(p, centro + Vector2.down * 90f) < 140f;
                if (enOjo) c = Color.white;
                float d = Vector2.Distance(p, centro);
                if (enOjo && d < 48f) c = new Color(0.55f, 0.25f, 0.75f);
                if (enOjo && d < 22f) c = new Color(0.05f, 0.05f, 0.08f);
                if (enOjo && Vector2.Distance(p, centro + new Vector2(10f, 10f)) < 7f) c = Color.white;
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return GuardarTextura("Mural_Ojo", tex, false);
    }

    // Mural geometrico de colores (triangulos)
    public static Texture2D TexturaMuralGeometrico()
    {
        int w = 448, h = 192;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        Color[] paleta = { new Color(0.95f, 0.3f, 0.45f), new Color(1f, 0.8f, 0.2f), new Color(0.2f, 0.6f, 0.9f), new Color(0.3f, 0.85f, 0.5f), new Color(0.6f, 0.35f, 0.85f) };
        int lado = 48;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int cx = x / lado, cy = y / lado;
                int lx = x % lado, ly = y % lado;
                bool mitad = (cx + cy) % 2 == 0 ? lx > ly : lx + ly < lado;
                tex.SetPixel(x, y, paleta[(cx * 3 + cy * 2 + (mitad ? 1 : 0)) % paleta.Length]);
            }
        tex.Apply();
        return GuardarTextura("Mural_Geometrico", tex, false);
    }

    // ===================== V2: FONDO — CERROS Y BAHIA =====================

    public static readonly string[] edificiosKayKit = { "building_A", "building_B", "building_C", "building_D", "building_E", "building_F", "building_G", "building_H" };




    // Tacho de basura con tapa (+ bolsa al lado). Sin collider.
    public static void Tacho(Transform padre, Vector3 basePos)
    {
        Material cuerpo = ConstructorF2.Material("Tacho_Verde", new Color(0.15f, 0.4f, 0.25f));
        Material tapa = ConstructorF2.Material("Tacho_Tapa", new Color(0.1f, 0.28f, 0.17f));
        Pieza(padre, "Tacho", PrimitiveType.Cylinder, basePos + Vector3.up * 0.45f, new Vector3(0.55f, 0.45f, 0.55f), Vector3.zero, cuerpo);
        Pieza(padre, "Tacho_Tapa", PrimitiveType.Cylinder, basePos + Vector3.up * 0.93f, new Vector3(0.62f, 0.04f, 0.62f), new Vector3(8f, 0f, 0f), tapa);
        Pieza(padre, "Bolsa", PrimitiveType.Sphere, basePos + new Vector3(0.45f, 0.2f, 0.2f), new Vector3(0.45f, 0.4f, 0.4f), Vector3.zero, ConstructorF2.Material("Bolsa_Negra", new Color(0.04f, 0.04f, 0.05f)));
    }

    // Bahia al este: mar, muelle con contenedores, bodegas, gruas, luces y barcos
    static void Bahia()
    {
        Transform bahia = Hijo(deco, "Fondo_Bahia");
        float nivelMar = -38f;

        // Mar
        GameObject mar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mar.name = "Mar";
        Object.DestroyImmediate(mar.GetComponent<Collider>());
        mar.transform.SetParent(bahia, false);
        mar.transform.position = new Vector3(330f, nivelMar - 0.5f, 80f);
        mar.transform.localScale = new Vector3(500f, 1f, 900f);
        Material agua = ConstructorF2.Material("Mar_Noche", new Color(0.03f, 0.07f, 0.14f));
        agua.SetFloat("_Smoothness", 0.92f);
        mar.GetComponent<Renderer>().sharedMaterial = agua;

        // Muelle (explanada del puerto)
        GameObject muelle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        muelle.name = "Muelle";
        Object.DestroyImmediate(muelle.GetComponent<Collider>());
        muelle.transform.SetParent(bahia, false);
        muelle.transform.position = new Vector3(85f, nivelMar + 1f, 80f);
        muelle.transform.localScale = new Vector3(40f, 3f, 260f);
        muelle.GetComponent<Renderer>().sharedMaterial = ConstructorF2.Material("Muelle_Cemento", new Color(0.42f, 0.42f, 0.44f));
        float piso = nivelMar + 2.5f;

        // Borde del muelle pintado amarillo, para que se lea donde termina el cemento y empieza el mar
        Material bordeAmarillo = MaterialEmisivo("Borde_Muelle", new Color(0.95f, 0.75f, 0.15f), 0.6f);
        Pieza(bahia, "Borde_Muelle", PrimitiveType.Cube, new Vector3(104.6f, piso + 0.15f, 80f), new Vector3(0.8f, 0.3f, 260f), Vector3.zero, bordeAmarillo);

        // Bodegas del puerto (Kenney Industrial)
        string[] bodegas = { "building-a", "building-c", "building-e", "building-g", "building-i", "building-k" };
        for (int i = 0; i < 6; i++)
            ConstructorF5.Ajustar(arte + "Entorno/Kenney_Industrial/" + bodegas[i] + ".fbx",
                new Bounds(new Vector3(74f, piso + 4f, -10f + i * 30f), new Vector3(12f, 8f, 20f)), -90f, bahia, "Bodega_" + i);

        // Contenedores apilados
        string[] contenedores = { "shipping-container-a", "shipping-container-b", "shipping-container-c" };
        for (int fila = 0; fila < 4; fila++)
            for (int i = 0; i < 9; i++)
            {
                int pisos = Random.Range(1, 4);
                for (int p = 0; p < pisos; p++)
                    ConstructorF5.Ajustar(arte + "Entorno/Kenney_Industrial/" + contenedores[Random.Range(0, 3)] + ".fbx",
                        new Bounds(new Vector3(86f + fila * 3.2f, piso + 1.3f + p * 2.6f, 10f + i * 7f), new Vector3(2.5f, 2.6f, 6.1f)), 0f, bahia, "Contenedor");
            }

        // Gruas portuarias (piezas simples): patas, viga y pluma hacia el mar
        for (int g = 0; g < 3; g++)
            Grua(bahia, new Vector3(101f, piso, 20f + g * 45f));

        // Luces del puerto (postes con cabeza luminosa; pocas luces reales)
        Material luzPuerto = MaterialEmisivo("Luz_Puerto", new Color(1f, 0.78f, 0.45f), 3f);
        for (int i = 0; i < 10; i++)
        {
            Vector3 p = new Vector3(95f, piso, -20f + i * 24f);
            Pieza(bahia, "Poste_Puerto", PrimitiveType.Cylinder, p + Vector3.up * 6f, new Vector3(0.3f, 6f, 0.3f), Vector3.zero, ConstructorF2.Material("Metal_Oscuro", new Color(0.15f, 0.15f, 0.17f)));
            Pieza(bahia, "Foco_Puerto", PrimitiveType.Cube, p + Vector3.up * 12.2f, new Vector3(1.2f, 0.4f, 0.8f), Vector3.zero, luzPuerto);
            if (i % 3 == 0)
            {
                Light l = new GameObject("Luz_Puerto").AddComponent<Light>();
                l.transform.SetParent(bahia, false);
                l.transform.position = p + Vector3.up * 11f;
                l.type = LightType.Point;
                l.color = new Color(1f, 0.78f, 0.45f);
                l.range = 40f;
                l.intensity = 6f;
            }
        }

        // Barcos en la bahia (con luces y meciendose)
        Barco(bahia, new Vector3(150f, nivelMar, 40f), 0f, 1f);
        Barco(bahia, new Vector3(210f, nivelMar, 130f), 35f, 1.4f);
        Barco(bahia, new Vector3(260f, nivelMar, -30f), -20f, 0.8f);
    }

    static void Grua(Transform padre, Vector3 basePos)
    {
        Material rojo = ConstructorF2.Material("Grua_Roja", new Color(0.75f, 0.18f, 0.15f));
        Material blanco = ConstructorF2.Material("Grua_Blanca", new Color(0.85f, 0.85f, 0.85f));
        for (int i = 0; i < 4; i++)
        {
            float dx = (i % 2 == 0) ? -3f : 3f;
            float dz = (i < 2) ? -3f : 3f;
            Pieza(padre, "Grua_Pata", PrimitiveType.Cube, basePos + new Vector3(dx, 9f, dz), new Vector3(0.7f, 18f, 0.7f), Vector3.zero, rojo);
        }
        Pieza(padre, "Grua_Cabina", PrimitiveType.Cube, basePos + new Vector3(0f, 18.5f, 0f), new Vector3(8f, 2f, 8f), Vector3.zero, blanco);
        Pieza(padre, "Grua_Pluma", PrimitiveType.Cube, basePos + new Vector3(16f, 20f, 0f), new Vector3(36f, 1.2f, 1.6f), Vector3.zero, rojo);
        Pieza(padre, "Grua_LuzPunta", PrimitiveType.Sphere, basePos + new Vector3(33.5f, 20.9f, 0f), Vector3.one * 0.7f, Vector3.zero, MaterialEmisivo("Luz_Roja_Grua", new Color(1f, 0.1f, 0.1f), 4f));
    }

    static void Barco(Transform padre, Vector3 pos, float rotY, float escala)
    {
        GameObject barco = new GameObject("Barco");
        barco.transform.SetParent(padre, false);
        barco.transform.position = pos;
        barco.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        barco.transform.localScale = Vector3.one * escala;
        Material casco = ConstructorF2.Material("Barco_Casco", new Color(0.12f, 0.14f, 0.2f));
        Material cubierta = ConstructorF2.Material("Barco_Cubierta", new Color(0.7f, 0.7f, 0.72f));
        PiezaLocal(barco.transform, "Casco", PrimitiveType.Cube, new Vector3(0f, 1.5f, 0f), new Vector3(9f, 4f, 40f), casco);
        PiezaLocal(barco.transform, "Proa", PrimitiveType.Cube, new Vector3(0f, 1.5f, 21f), new Vector3(6f, 4f, 6f), casco).transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        PiezaLocal(barco.transform, "Puente", PrimitiveType.Cube, new Vector3(0f, 6f, -14f), new Vector3(7f, 5f, 6f), cubierta);
        PiezaLocal(barco.transform, "Ventanas_Puente", PrimitiveType.Cube, new Vector3(0f, 7f, -10.95f), new Vector3(6f, 1f, 0.1f), MaterialEmisivo("Ventana_Barco", new Color(1f, 0.85f, 0.55f), 2.5f));
        // Contenedores sobre la cubierta
        for (int i = 0; i < 4; i++)
            PiezaLocal(barco.transform, "Carga", PrimitiveType.Cube, new Vector3(0f, 4.6f, -4f + i * 6.5f), new Vector3(7f, 2.4f, 6f),
                       ConstructorF2.Material("Carga_" + (i % 3), new[] { new Color(0.7f, 0.2f, 0.15f), new Color(0.15f, 0.35f, 0.6f), new Color(0.85f, 0.6f, 0.15f) }[i % 3]));
        PiezaLocal(barco.transform, "Luz_Mastil", PrimitiveType.Sphere, new Vector3(0f, 10f, -14f), Vector3.one * 0.6f, MaterialEmisivo("Luz_Blanca", Color.white, 4f));
        Flotar f = barco.AddComponent<Flotar>();
        f.altura = 0.3f;
        f.inclinacion = 1.2f;
        f.velocidad = 0.5f;
    }

    // ===================== UTILIDADES =====================

    public static Transform Hijo(Transform padre, string nombre)
    {
        Transform t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        return t;
    }

    // Pieza primitiva SIN collider (solo decoracion), en coordenadas del mundo
    public static GameObject Pieza(Transform padre, string nombre, PrimitiveType tipo, Vector3 pos, Vector3 escala, Vector3 rot, Material mat)
    {
        GameObject p = GameObject.CreatePrimitive(tipo);
        p.name = nombre;
        Object.DestroyImmediate(p.GetComponent<Collider>());
        p.transform.SetParent(padre, false);
        p.transform.position = pos;
        p.transform.rotation = Quaternion.Euler(rot);
        p.transform.localScale = escala;
        p.GetComponent<Renderer>().sharedMaterial = mat;
        return p;
    }

    // Pieza primitiva SIN collider en coordenadas locales del padre
    public static GameObject PiezaLocal(Transform padre, string nombre, PrimitiveType tipo, Vector3 posLocal, Vector3 escala, Material mat)
    {
        GameObject p = GameObject.CreatePrimitive(tipo);
        p.name = nombre;
        Object.DestroyImmediate(p.GetComponent<Collider>());
        p.transform.SetParent(padre, false);
        p.transform.localPosition = posLocal;
        p.transform.localScale = escala;
        p.GetComponent<Renderer>().sharedMaterial = mat;
        return p;
    }

    public static Material MaterialEmisivo(string nombre, Color color, float intensidad)
    {
        Material m = ConstructorF2.Material(nombre, color);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", color * intensidad);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(m);
        return m;
    }

    // ===================== V1: VENTANAS ENCENDIDAS =====================

    // En el atlas de KayKit las ventanas son el cuadro de abajo a la izquierda (0-25% x, 0-25% y).
    // Creamos una mascara de emision que solo ilumina ese cuadro -> ventanas amarillas.
    static Material materialVentanasEncendidas;

    public static Material MaterialVentanas()
    {
        if (materialVentanasEncendidas != null) return materialVentanasEncendidas;

        string rutaMascara = materiales + "Mascara_Ventanas_KayKit.png";
        Texture2D mascara = new Texture2D(64, 64, TextureFormat.RGB24, false);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                mascara.SetPixel(x, y, (x < 16 && y < 16) ? Color.white : Color.black);
        System.IO.File.WriteAllBytes(rutaMascara, mascara.EncodeToPNG());
        Object.DestroyImmediate(mascara);
        AssetDatabase.ImportAsset(rutaMascara);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(rutaMascara);
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.SaveAndReimport();

        Material original = AssetDatabase.LoadAssetAtPath<Material>(arte + "Entorno/KayKit_CityBuilder/citybits_texture.mat");
        if (original == null)
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(arte + "Entorno/KayKit_CityBuilder/building_A_withoutBase.fbx"))
                if (o is Material) { original = (Material)o; break; }

        string ruta = materiales + "KayKit_VentanasEncendidas.mat";
        AssetDatabase.DeleteAsset(ruta);
        Material m = new Material(original);
        m.EnableKeyword("_EMISSION");
        m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(rutaMascara));
        m.SetColor("_EmissionColor", new Color(1f, 0.62f, 0.22f) * 0.85f); // amarillo calido, sin quemarse a blanco
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        AssetDatabase.CreateAsset(m, ruta);
        materialVentanasEncendidas = m;
        return m;
    }

    // Enciende las ventanas de ~55% de los edificios (casas, fachadas y casas de fondo)
    static void VentanasEncendidas()
    {
        Material encendidas = MaterialVentanas();
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.InstanceID))
        {
            string n = t.name;
            bool esEdificio = n.StartsWith("Edificio_") || n.StartsWith("Modelo_Casa") || n.StartsWith("Fachada_") || n.StartsWith("Modelo_Techo") || n.StartsWith("CasaFondo_");
            if (!esEdificio) continue;
            if (Random.value > 0.55f) continue;
            EncenderVentanas(t.gameObject, encendidas);
        }
    }

    public static void EncenderVentanas(GameObject edificio, Material encendidas)
    {
        foreach (Renderer r in edificio.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null && mats[i].name.StartsWith("citybits_texture")) mats[i] = encendidas;
            r.sharedMaterials = mats;
        }
    }
}
