using UnityEditor;
using UnityEngine;

// Herramienta de EDITOR — Detalles de cada zona del Nivel 1 (sin collider, salvo que se diga).
// Todo va pegado a superficies REALES: fachadas, muros, pisos y techos que existen en el nivel.
public static class ConstructorNivel1Detalles
{
    const string arte = "Assets/BadCarreteDay/Arte/";
    static Transform deco;

    public static void Vestir(Transform padre, ConstructorF2.Base b)
    {
        deco = padre;
        Suelos();
        Faroles();
        Props();
        Almacen();
        Toby();
        DonLucho();
        EscaleraRota();
        Ascensor();
        Techos();
        PasajeOscuro();
        Cancha();
        Cables();
        Murales();
    }

    static Material Mat(string nombre, float r, float g, float bl) { return ConstructorF2.Material(nombre, new Color(r, g, bl)); }
    static GameObject Pieza(Transform p, string n, PrimitiveType t, Vector3 pos, Vector3 esc, Vector3 rot, Material m) { return ConstructorF7.Pieza(p, n, t, pos, esc, rot, m); }

    // ---------- Pisos con textura (sin estirarse) ----------
    static void Suelos()
    {
        Texture2D adoquin = ConstructorF7.TexturaAdoquin();
        Texture2D concreto = ConstructorF7.TexturaConcreto();
        Textura(new[] { "Suelo_Pasaje", "Suelo_Plaza" }, "Adoquin", adoquin, new Color(0.8f, 0.76f, 0.72f), 1.6f);
        Textura(new[] { "Calle_Alta", "Patio", "Descanso", "Anden_Abajo", "Vereda", "Techo_A", "Techo_B", "Cerro_Alto" }, "Concreto", concreto, new Color(0.78f, 0.78f, 0.8f), 3f);
        Textura(new[] { "Escalon_", "EscalonRoto_" }, "Concreto_Escalon", concreto, new Color(0.72f, 0.72f, 0.74f), 3f);
        Textura(new[] { "Lote_Eriazo" }, "Tierra_Lote", concreto, new Color(0.62f, 0.45f, 0.3f), 3f);
        Textura(new[] { "Cancha_Tierra" }, "Tierra_Cancha", concreto, new Color(0.8f, 0.5f, 0.28f), 3f);
    }

    // Aplica la textura a todos los objetos cuyo nombre empieza con alguno de los prefijos.
    // Cada objeto recibe su propio material con la repeticion segun su tamaño (en metros).
    static void Textura(string[] prefijos, string nombreMat, Texture2D tex, Color tinte, float metros)
    {
        int n = 0;
        foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.InstanceID))
        {
            bool coincide = false;
            foreach (string p in prefijos) if (r.name == p || (p.EndsWith("_") && r.name.StartsWith(p))) coincide = true;
            if (!coincide) continue;
            Vector3 e = r.transform.lossyScale;
            Material m = ConstructorF2.Material(nombreMat + "_" + Mathf.RoundToInt(e.x) + "x" + Mathf.RoundToInt(e.z), tinte);
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", new Vector2(Mathf.Max(1f, e.x / metros), Mathf.Max(1f, e.z / metros)));
            r.sharedMaterial = m;
            n++;
        }
    }

    // ---------- Faroles (la luz principal de la madrugada) ----------
    static readonly Vector3[] faroles = {
        new Vector3(-3.6f, 0.12f, 3f), new Vector3(3.6f, 0.12f, 9f), new Vector3(-3.6f, 0.12f, 19f), new Vector3(3.6f, 0.12f, 27f), // pasaje
        new Vector3(-6.8f, 0f, 33.5f), new Vector3(12.9f, 0f, 44f), new Vector3(12.9f, 0f, 54f), new Vector3(-6.8f, 0f, 53.5f),     // plaza
        new Vector3(1.4f, 4f, 64.5f), new Vector3(-9.2f, 4f, 72f), new Vector3(-9.2f, 4f, 88f), new Vector3(-9.2f, 4f, 104f),       // calle alta
        new Vector3(8.4f, 10.5f, 108.8f), new Vector3(10.5f, 16f, 118f),                                                              // ascensor
        new Vector3(-2.4f, 16f, 124f), new Vector3(2.4f, 16f, 131f),                                                                  // pasaje oscuro
        new Vector3(-11.8f, 16f, 138f), new Vector3(11.8f, 16f, 138f), new Vector3(-11.8f, 16f, 162.5f), new Vector3(11.8f, 16f, 162.5f), // cancha
    };

    static void Faroles()
    {
        Transform t = ConstructorF7.Hijo(deco, "Faroles");
        foreach (Vector3 p in faroles)
        {
            GameObject farol = ConstructorF5.Ajustar(arte + "Entorno/KayKit_CityBuilder/streetlight.fbx", new Bounds(p + Vector3.up * 2.25f, new Vector3(0.6f, 4.5f, 0.6f)), 0f, t, "Farol");
            Light l = ConstructorF7.LuzPunto(farol.transform, p + Vector3.up * 4.1f, new Color(1f, 0.66f, 0.32f), 13f, 9f);
            l.name = "Luz_Farol";
        }
    }

    // ---------- Props de calle ----------
    static void Props()
    {
        Transform t = ConstructorF7.Hijo(deco, "Props");
        // Pasaje
        ConstructorF7.Tacho(t, new Vector3(3.5f, 0.12f, 24f));
        ConstructorF7.Tacho(t, new Vector3(-3.5f, 0.12f, 15f));
        ConstructorF5.Ajustar(arte + "Entorno/Kenney_Roads/electricity-pole.fbx", new Bounds(new Vector3(3.6f, 3.62f, 6f), new Vector3(0.6f, 7f, 0.6f)), 0f, t, "Poste");
        ConstructorF5.Ajustar(arte + "Entorno/Kenney_Roads/electricity-pole.fbx", new Bounds(new Vector3(-3.6f, 3.62f, 27f), new Vector3(0.6f, 7f, 0.6f)), 0f, t, "Poste");
        ConstructorF5.Ajustar(arte + "Entorno/KayKit_CityBuilder/firehydrant.fbx", new Bounds(new Vector3(-3.4f, 0.47f, 6.5f), new Vector3(0.35f, 0.7f, 0.35f)), 0f, t, "Grifo");
        // Lote eriazo: maleza, neumaticos y escombros
        for (int i = 0; i < 6; i++)
            ConstructorF5.Ajustar(arte + "Entorno/KayKit_CityBuilder/bush.fbx", new Bounds(new Vector3(Random.Range(4.8f, 12f), 0.35f, Random.Range(12.3f, 13.3f) + (i % 2) * 6.3f), new Vector3(0.6f, 0.7f, 0.6f)), Random.Range(0f, 360f), t, "Maleza");
        ConstructorF7.Neumatico(t, new Vector3(11.6f, 0.12f, 13f));
        ConstructorF7.Neumatico(t, new Vector3(11.7f, 0.32f, 13.1f));
        // Plaza
        // Dumpster en la esquina del mirador (se revisa; detras, contra la baranda, hay una quina)
        ConstructorF5.Prop(t, "Entorno/KayKit_CityBuilder/dumpster.fbx", new Vector3(12.3f, 0f, 50.8f), new Vector3(1.4f, 1.4f, 2.4f), 90f, true);
        ConstructorF5.Prop(t, "Entorno/KayKit_CityBuilder/bench.fbx", new Vector3(12.6f, 0f, 47.5f), new Vector3(1.9f, 0.47f, 0.7f), 90f, true);   // mirando la bahia
        ConstructorF5.Prop(t, "Entorno/KayKit_CityBuilder/bench.fbx", new Vector3(-6.3f, 0f, 49.5f), new Vector3(1.9f, 0.47f, 0.7f), 90f, true);
        ConstructorF5.Prop(t, "Entorno/Kenney_Suburban/tree-large.fbx", new Vector3(11f, 0f, 51.5f), new Vector3(1.4f, 5f, 1.55f), 0f, false);
        ConstructorF5.Prop(t, "Entorno/Kenney_Suburban/tree-small.fbx", new Vector3(-6f, 0f, 34.8f), new Vector3(1.1f, 3f, 1.25f), 0f, false);
        ConstructorF5.Prop(t, "Entorno/Kenney_Suburban/planter.fbx", new Vector3(12.8f, 0f, 41.8f), new Vector3(1.6f, 0.72f, 1.2f), 90f, false);
        // Calle alta
        ConstructorF7.Tacho(t, new Vector3(-9.3f, 4f, 95f));
        ConstructorF5.Ajustar(arte + "Entorno/Kenney_Roads/electricity-pole.fbx", new Bounds(new Vector3(-9.4f, 7.5f, 80f), new Vector3(0.6f, 7f, 0.6f)), 0f, t, "Poste");
    }

    // ---------- Almacen de Doña Marta (local x -14..-8, z 37..47; frente en x -8) ----------
    // Fila de mercaderia sobre una repisa (centro en z, a lo largo de "largo"): latas, botellas, paquetes o frascos
    static void Mercaderia(Transform padre, Vector3 centro, float largo, int tipo)
    {
        Color[] colores = { new Color(0.85f, 0.15f, 0.15f), new Color(0.95f, 0.8f, 0.2f), new Color(0.2f, 0.5f, 0.85f),
                            new Color(0.25f, 0.65f, 0.3f), new Color(0.95f, 0.55f, 0.15f), new Color(0.9f, 0.9f, 0.9f) };
        float paso = tipo == 2 ? 0.26f : 0.16f;
        for (float z = -largo / 2f + paso / 2f; z < largo / 2f; z += paso)
        {
            Color c = colores[Random.Range(0, colores.Length)];
            Material m = Mat("Mercaderia_" + Mathf.RoundToInt(c.r * 10) + Mathf.RoundToInt(c.g * 10) + Mathf.RoundToInt(c.b * 10), c.r, c.g, c.b);
            Vector3 p = centro + new Vector3(0f, 0f, z);
            if (tipo == 0) Pieza(padre, "Lata", PrimitiveType.Cylinder, p + Vector3.up * 0.07f, new Vector3(0.09f, 0.07f, 0.09f), Vector3.zero, m);
            else if (tipo == 1)
            {
                Pieza(padre, "Botella", PrimitiveType.Cylinder, p + Vector3.up * 0.13f, new Vector3(0.08f, 0.13f, 0.08f), Vector3.zero, m);
                Pieza(padre, "Tapa", PrimitiveType.Cylinder, p + Vector3.up * 0.3f, new Vector3(0.035f, 0.04f, 0.035f), Vector3.zero, m);
            }
            else if (tipo == 2) Pieza(padre, "Paquete", PrimitiveType.Cube, p + Vector3.up * 0.15f, new Vector3(0.12f, 0.3f, 0.22f), Vector3.zero, m);
            else Pieza(padre, "Frasco", PrimitiveType.Cylinder, p + Vector3.up * 0.09f, new Vector3(0.11f, 0.09f, 0.11f), Vector3.zero, m);
        }
    }

    // Modelo con escala PAREJA (sin estirarlo), apoyado en "basePos" y con el alto pedido
    static GameObject Colocar(Transform padre, string ruta, Vector3 basePos, float alto, float rotY)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(arte + ruta);
        if (asset == null) { Debug.LogError("[BadCarreteDay] No existe: " + ruta); return null; }
        GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(asset, padre);
        m.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        foreach (Collider c in m.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        Bounds b = ConstructorF5.Limites(m);
        m.transform.localScale *= alto / Mathf.Max(b.size.y, 0.001f);
        b = ConstructorF5.Limites(m);
        m.transform.position += new Vector3(basePos.x - b.center.x, basePos.y - b.min.y, basePos.z - b.center.z);
        return m;
    }

    static void Almacen()
    {
        Transform a = ConstructorF7.Hijo(deco, "Almacen_Detalles");
        Material fierro = Mat("Fierro_Negro", 0.08f, 0.08f, 0.09f);
        // Reja de la ventanita (barrotes arriba del meson)
        for (float z = 37.6f; z <= 44.4f; z += 0.35f)
            Pieza(a, "Reja", PrimitiveType.Cube, new Vector3(-7.96f, 2.1f, z), new Vector3(0.04f, 1.3f, 0.04f), Vector3.zero, fierro);
        Pieza(a, "Reja_Marco", PrimitiveType.Cube, new Vector3(-7.96f, 2.72f, 41f), new Vector3(0.06f, 0.06f, 7f), Vector3.zero, fierro);
        // Puerta cerrada y timbre
        Pieza(a, "Puerta", PrimitiveType.Cube, new Vector3(-7.97f, 1.1f, 46f), new Vector3(0.06f, 2.2f, 1.1f), Vector3.zero, Mat("Puerta_Verde", 0.15f, 0.4f, 0.3f));
        // Placa del timbre junto a la puerta, con su boton
        Letrero(a, "TOQUE EL TIMBRE", new Vector3(-7.95f, 1.55f, 46.8f), 90f, 0.36f, 0.16f, new Color(0.85f, 0.75f, 0.45f), new Color(0.15f, 0.1f, 0.05f), 0.07f);
        Pieza(a, "Timbre", PrimitiveType.Cylinder, new Vector3(-7.93f, 1.38f, 46.8f), new Vector3(0.07f, 0.015f, 0.07f), new Vector3(0f, 0f, 90f), Mat("Timbre_Rojo", 0.8f, 0.1f, 0.1f));
        // Tabla colgada de la reja de la ventanita
        Letrero(a, "HOY NO SE FÍA\nMAÑANA SÍ", new Vector3(-7.9f, 2.25f, 43.5f), 90f, 0.9f, 0.5f, new Color(0.95f, 0.93f, 0.85f), new Color(0.6f, 0.08f, 0.08f), 0.13f);
        // Toldo a rayas sobre la ventana
        for (int i = 0; i < 11; i++)
            Pieza(a, "Toldo", PrimitiveType.Cube, new Vector3(-7.45f, 2.95f, 37.75f + i * 0.62f), new Vector3(1.1f, 0.05f, 0.62f), new Vector3(0f, 0f, -18f),
                  i % 2 == 0 ? Mat("Toldo_Rojo", 0.85f, 0.15f, 0.2f) : Mat("Toldo_Blanco", 0.95f, 0.95f, 0.95f));
        // Letrero
        Pieza(a, "Letrero", PrimitiveType.Cube, new Vector3(-7.95f, 3.7f, 41f), new Vector3(0.1f, 0.8f, 6.4f), Vector3.zero, Mat("Letrero_Azul", 0.1f, 0.2f, 0.5f));
        ConstructorF7.Texto3D(a, "ALMACÉN DOÑA MARTA", new Vector3(-7.88f, 3.7f, 41f), 90f, 0.55f, new Color(1f, 0.9f, 0.4f));
        // Interior: repisas de almacen de barrio con mercaderia (latas, botellas, paquetes, frascos),
        // un refri de bebidas, cajas de verdura y la caja registradora en el meson
        Material madera = Mat("Repisa_Madera", 0.45f, 0.3f, 0.18f);
        Random.InitState(7);
        for (int u = 0; u < 3; u++)
        {
            float zc = 38.6f + u * 2.4f;
            Pieza(a, "Repisa_Fondo", PrimitiveType.Cube, new Vector3(-13.75f, 1.2f, zc), new Vector3(0.05f, 2.4f, 2.3f), Vector3.zero, madera);
            Pieza(a, "Repisa_Lado", PrimitiveType.Cube, new Vector3(-13.5f, 1.2f, zc - 1.15f), new Vector3(0.5f, 2.4f, 0.05f), Vector3.zero, madera);
            Pieza(a, "Repisa_Lado", PrimitiveType.Cube, new Vector3(-13.5f, 1.2f, zc + 1.15f), new Vector3(0.5f, 2.4f, 0.05f), Vector3.zero, madera);
            for (int n = 0; n < 4; n++)
            {
                float y = 0.35f + n * 0.55f;
                Pieza(a, "Repisa", PrimitiveType.Cube, new Vector3(-13.5f, y, zc), new Vector3(0.5f, 0.04f, 2.3f), Vector3.zero, madera);
                Mercaderia(a, new Vector3(-13.45f, y + 0.02f, zc), 2.1f, (u * 4 + n) % 4);
            }
        }
        // Refri de bebidas (puerta de vidrio iluminada)
        Pieza(a, "Refri", PrimitiveType.Cube, new Vector3(-12.9f, 1f, 46.25f), new Vector3(1.1f, 2f, 0.8f), Vector3.zero, Mat("Refri_Rojo", 0.75f, 0.1f, 0.12f));
        Pieza(a, "Refri_Vidrio", PrimitiveType.Cube, new Vector3(-12.9f, 1.05f, 45.84f), new Vector3(0.9f, 1.6f, 0.02f), Vector3.zero, ConstructorF7.MaterialEmisivo("Refri_Luz", new Color(0.75f, 0.9f, 1f), 1.6f));
        ConstructorF7.Texto3D(a, "BEBIDAS HELADAS", new Vector3(-12.9f, 1.92f, 45.83f), 180f, 0.12f, Color.white);
        // Cajas de verdura y pan
        Colocar(a, "Entorno/KayKit_Restaurant/crate_tomatoes.fbx", new Vector3(-10.6f, 0f, 45.9f), 0.5f, 0f);
        Colocar(a, "Entorno/KayKit_Restaurant/crate_potatoes.fbx", new Vector3(-11.4f, 0f, 45.9f), 0.5f, 0f);
        Colocar(a, "Entorno/KayKit_Restaurant/crate_buns.fbx", new Vector3(-11.0f, 0.5f, 45.9f), 0.5f, 0f);
        // Caja registradora y frasco de dulces en el meson
        Pieza(a, "Caja_Registradora", PrimitiveType.Cube, new Vector3(-7.85f, 1.12f, 43.3f), new Vector3(0.4f, 0.24f, 0.45f), new Vector3(0f, 0f, 0f), Mat("Registradora", 0.2f, 0.2f, 0.22f));
        Colocar(a, "Entorno/KayKit_Restaurant/jar_A_large.fbx", new Vector3(-7.85f, 1f, 38.7f), 0.3f, 0f);
        // Calendario y almanaque en el muro
        Letrero(a, "ALMANAQUE\nNOVIEMBRE", new Vector3(-11f, 2.6f, 37.22f), 0f, 0.6f, 0.5f, new Color(0.95f, 0.93f, 0.85f), new Color(0.15f, 0.15f, 0.4f), 0.09f);
        Pieza(a, "Tubo_Fluorescente", PrimitiveType.Cube, new Vector3(-10.5f, 4.3f, 41f), new Vector3(0.12f, 0.06f, 3f), Vector3.zero, ConstructorF7.MaterialEmisivo("Fluorescente", new Color(0.95f, 1f, 0.9f), 3f));
        ConstructorF7.LuzPunto(a, new Vector3(-9.5f, 3.6f, 41f), new Color(1f, 0.92f, 0.75f), 7f, 4f);
    }

    // ---------- Toby: cucha, plato y letrero ----------
    static void Toby()
    {
        Transform t = ConstructorF7.Hijo(deco, "Toby_Rincon");
        GameObject plato = GameObject.Find("Plato_Toby");
        Vector3 p = plato != null ? plato.transform.position : new Vector3(5f, 0f, 52.5f);
        ConstructorF5.Ajustar(arte + "Entorno/KayKit_Restaurant/bowl.fbx", new Bounds(p + Vector3.up * 0.08f, new Vector3(0.45f, 0.16f, 0.45f)), 0f, t, "Plato");
        Material maderaRoja = Mat("Cucha_Roja", 0.6f, 0.2f, 0.15f);
        Material techo = Mat("Almacen_Techo", 0.35f, 0.18f, 0.15f);
        Vector3 c = new Vector3(-4.8f, 0f, 53.8f);
        Pieza(t, "Cucha_Cuerpo", PrimitiveType.Cube, c + Vector3.up * 0.5f, new Vector3(1.2f, 1f, 1.1f), Vector3.zero, maderaRoja);
        Pieza(t, "Cucha_Techo1", PrimitiveType.Cube, c + new Vector3(0f, 1.15f, -0.3f), new Vector3(1.35f, 0.08f, 0.75f), new Vector3(-38f, 0f, 0f), techo);
        Pieza(t, "Cucha_Techo2", PrimitiveType.Cube, c + new Vector3(0f, 1.15f, 0.3f), new Vector3(1.35f, 0.08f, 0.75f), new Vector3(38f, 0f, 0f), techo);
        Pieza(t, "Cucha_Entrada", PrimitiveType.Cube, c + new Vector3(0f, 0.35f, -0.56f), new Vector3(0.5f, 0.6f, 0.02f), Vector3.zero, Mat("Negro", 0.02f, 0.02f, 0.02f));
        ConstructorF7.Texto3D(t, "TOBY", c + new Vector3(0f, 0.85f, -0.57f), 180f, 0.25f, Color.white);
        // Letrero en la fachada al lado de la escalera
        Letrero(t, "¡CUIDADO CON EL PERRO!", new Vector3(-4.5f, 2.2f, 55.14f), 180f, 1.8f, 0.42f, new Color(0.95f, 0.8f, 0.15f), new Color(0.1f, 0.08f, 0.05f), 0.2f);
        // Pasamanos en las paredes de la escalera
        Material metal = Mat("Baranda_Metal", 0.2f, 0.32f, 0.3f);
        foreach (float x in new[] { -1.93f, 1.93f })
        {
            GameObject pm = Pieza(t, "Pasamanos", PrimitiveType.Cylinder, new Vector3(x, 2.95f, 59.1f), new Vector3(0.05f, 4.4f, 0.05f), Vector3.zero, metal);
            pm.transform.rotation = Quaternion.FromToRotation(Vector3.up, new Vector3(0f, 4f, 7.8f).normalized);
        }
    }

    // ---------- Don Lucho: charco, mancha en la fachada y botellas ----------
    static void DonLucho()
    {
        Transform t = ConstructorF7.Hijo(deco, "DonLucho_Detalles");
        GameObject lucho = GameObject.Find("NPC_DonLucho");
        Vector3 p = lucho != null ? lucho.transform.position : new Vector3(-3.35f, 0.12f, 10.5f);
        Material mojado = Mat("Mojado", 0.3f, 0.26f, 0.1f);
        mojado.SetFloat("_Smoothness", 0.95f);
        Pieza(t, "Charco", PrimitiveType.Cylinder, new Vector3(p.x - 0.35f, 0.125f, p.z), new Vector3(0.7f, 0.004f, 0.9f), Vector3.zero, mojado);
        Pieza(t, "Mancha_Muro", PrimitiveType.Cube, new Vector3(-4.19f, 0.6f, p.z), new Vector3(0.02f, 0.9f, 0.55f), Vector3.zero, Mat("Mancha", 0.18f, 0.15f, 0.08f));
        ConstructorF7.Botella(t, new Vector3(p.x + 0.5f, 0.18f, p.z + 0.8f), true);
        ConstructorF7.Botella(t, new Vector3(p.x + 0.2f, 0.25f, p.z + 1.2f), false);
    }

    // ---------- Escalera rota: escombros, baranda doblada, letrero ----------
    static void EscaleraRota()
    {
        Transform t = ConstructorF7.Hijo(deco, "EscaleraRota_Detalles");
        Material cemento = Mat("Escombro", 0.55f, 0.53f, 0.5f);
        Vector3[] trozos = { new Vector3(-2.2f, 4.11f, 76.2f), new Vector3(1.9f, 4.11f, 77.4f), new Vector3(-2.6f, 4.11f, 78.6f), new Vector3(-1.2f, 4.11f, 73.8f) };
        foreach (Vector3 tp in trozos)
            Pieza(t, "Escombro", PrimitiveType.Cube, tp, new Vector3(Random.Range(0.3f, 0.6f), 0.22f, Random.Range(0.3f, 0.6f)), new Vector3(Random.Range(-15f, 15f), Random.Range(0f, 90f), Random.Range(-10f, 10f)), cemento);
        Material fierro = Mat("Fierro_Oxidado", 0.4f, 0.22f, 0.12f);
        for (int i = 0; i < 4; i++)
            Pieza(t, "Baranda_Poste", PrimitiveType.Cylinder, new Vector3(-1.6f, 4.5f + i * 0.5f + 0.45f, 75f + i * 1.1f), new Vector3(0.06f, 0.45f, 0.06f), Vector3.zero, fierro);
        Pieza(t, "Baranda_Doblada", PrimitiveType.Cylinder, new Vector3(-1.75f, 6.1f, 77.7f), new Vector3(0.05f, 0.8f, 0.05f), new Vector3(35f, 0f, 50f), fierro);
        // Letrero en un poste al pie de la escalera
        Pieza(t, "Letrero_Poste", PrimitiveType.Cylinder, new Vector3(-2.4f, 5f, 74f), new Vector3(0.06f, 1f, 0.06f), Vector3.zero, fierro);
        Pieza(t, "Letrero_Tabla", PrimitiveType.Cube, new Vector3(-2.4f, 6.1f, 73.97f), new Vector3(2.6f, 0.5f, 0.04f), Vector3.zero, Mat("Letrero_Amarillo", 0.9f, 0.75f, 0.15f));
        ConstructorF7.Texto3D(t, "PELIGRO: ESCALERA EN REPARACIÓN (DESDE 2009)", new Vector3(-2.4f, 6.1f, 73.94f), 180f, 0.11f, new Color(0.1f, 0.1f, 0.1f));
        ConstructorF5.Prop(t, "Entorno/Kenney_Roads/construction-cone.fbx", new Vector3(-2.5f, 4f, 75.2f), new Vector3(0.45f, 0.65f, 0.45f), 0f, false);
    }

    // ---------- Ascensor (funicular): rieles en la cara del cerro, cabina y estaciones ----------
    static void Ascensor()
    {
        Transform t = ConstructorF7.Hijo(deco, "Ascensor_Estructura");
        Material madera = Mat("Madera_Vieja", 0.45f, 0.3f, 0.2f);
        Material rojo = Mat("Ascensor_Rojo", 0.7f, 0.15f, 0.12f);
        float angulo = Mathf.Atan2(4.4f, 12f) * Mathf.Rad2Deg;
        // Rieles a lo largo de la cara del cerro (del anden y 10,5 a la cima y 16)
        foreach (float x in new[] { 4.3f, 7.7f })
        {
            Vector3 a = new Vector3(x, 10f, 110.7f + 6f * 0.3667f), c = new Vector3(x, 16.2f, 110.7f + 12.2f * 0.3667f);
            GameObject riel = Pieza(t, "Riel", PrimitiveType.Cube, (a + c) / 2f + new Vector3(0f, 0f, -0.15f), new Vector3(0.25f, Vector3.Distance(a, c), 0.25f), Vector3.zero, madera);
            riel.transform.rotation = Quaternion.Euler(angulo, 0f, 0f);
        }
        // Cabina: barandas a los lados, postes y techo (se mueven con el ascensor)
        GameObject plataforma = GameObject.Find("Ascensor");
        if (plataforma != null)
        {
            Transform pt = plataforma.transform;
            foreach (float x in new[] { -1.45f, 1.45f })
                ConstructorF7.ParteMundo(pt, "Baranda", PrimitiveType.Cube, new Vector3(x, 0.75f, 0f), new Vector3(0.08f, 1.1f, 3f), rojo);
            foreach (Vector3 poste in new[] { new Vector3(-1.45f, 1.4f, -1.45f), new Vector3(1.45f, 1.4f, -1.45f), new Vector3(-1.45f, 1.4f, 1.45f), new Vector3(1.45f, 1.4f, 1.45f) })
                ConstructorF7.ParteMundo(pt, "Poste", PrimitiveType.Cube, poste, new Vector3(0.1f, 2.4f, 0.1f), rojo);
            ConstructorF7.ParteMundo(pt, "Techo", PrimitiveType.Cube, new Vector3(0f, 2.65f, 0f), new Vector3(3.2f, 0.12f, 3.2f), Mat("Almacen_Techo", 0.35f, 0.18f, 0.15f));
        }
        ConstructorF7.Caseta(t, new Vector3(3.2f, 10.5f, 107.2f), "ASCENSOR CERRO ALEGRÍA*", rojo);
        // Papel pegado bajo el letrero
        Letrero(t, "*no tiene nada de alegre", new Vector3(3.2f, 12.15f, 106.37f), 180f, 1.0f, 0.18f, new Color(0.97f, 0.97f, 0.92f), new Color(0.1f, 0.1f, 0.1f), 0.07f);
        ConstructorF7.Caseta(t, new Vector3(10.2f, 16f, 116.4f), "ESTACIÓN ARRIBA", rojo);
        ConstructorNivel1.Baranda(t, new Vector3(2f, 10.5f, 102.4f), new Vector3(2f, 10.5f, 110.08f));
    }

    // ---------- Techos: tanques, antenas y tendederos lejos de la linea de planeo ----------
    static void Techos()
    {
        Transform t = ConstructorF7.Hijo(deco, "Techos_Detalles");
        GameObject a = GameObject.Find("Techo_A"), bb = GameObject.Find("Techo_B");
        if (a == null || bb == null) return;
        Bounds ta = a.GetComponent<Renderer>().bounds, tb = bb.GetComponent<Renderer>().bounds;
        float ya = ta.max.y, yb = tb.max.y;
        // Tanques y antenas en las esquinas (lejos de la linea de salto, que va por el centro)
        ConstructorF5.Ajustar(arte + "Entorno/Kenney_Industrial/detail-tank.fbx", new Bounds(new Vector3(ta.max.x - 0.9f, ya + 0.9f, ta.min.z + 1.5f), new Vector3(1.4f, 1.8f, 1.4f)), 0f, t, "Tanque");
        ConstructorF5.Ajustar(arte + "Entorno/Kenney_Industrial/detail-tank.fbx", new Bounds(new Vector3(tb.max.x - 0.9f, yb + 0.9f, tb.max.z - 1.5f), new Vector3(1.4f, 1.8f, 1.4f)), 0f, t, "Tanque");
        Material metal = Mat("Metal_Oscuro", 0.15f, 0.15f, 0.17f);
        foreach (Vector3 p in new[] { new Vector3(ta.min.x + 0.6f, ya, ta.max.z - 1.3f), new Vector3(tb.max.x - 0.6f, yb, tb.min.z + 3.6f) })
        {
            Pieza(t, "Antena", PrimitiveType.Cylinder, p + Vector3.up * 1.2f, new Vector3(0.05f, 1.2f, 0.05f), Vector3.zero, metal);
            Pieza(t, "Antena_Brazo", PrimitiveType.Cube, p + Vector3.up * 2.1f, new Vector3(0.9f, 0.04f, 0.04f), Vector3.zero, metal);
        }
        Vector3 p0 = new Vector3(tb.max.x - 0.5f, yb, tb.min.z + 5.2f), p1 = new Vector3(tb.max.x - 0.5f, yb, tb.max.z - 3.2f);
        Pieza(t, "Poste_Tendedero", PrimitiveType.Cylinder, p0 + Vector3.up * 0.8f, new Vector3(0.06f, 0.8f, 0.06f), Vector3.zero, metal);
        Pieza(t, "Poste_Tendedero", PrimitiveType.Cylinder, p1 + Vector3.up * 0.8f, new Vector3(0.06f, 0.8f, 0.06f), Vector3.zero, metal);
        ConstructorF7.Tendedero(t, p0 + Vector3.up * 1.6f, p1 + Vector3.up * 1.6f);
    }

    // ---------- Pasaje oscuro: basura, colchon, neumaticos, grafitis y farol que parpadea ----------
    static void PasajeOscuro()
    {
        Transform t = ConstructorF7.Hijo(deco, "PasajeOscuro_Detalles");
        ConstructorF7.Tacho(t, new Vector3(-2.4f, 16f, 121.5f));
        ConstructorF7.Tacho(t, new Vector3(2.4f, 16f, 126.5f));
        Pieza(t, "Colchon", PrimitiveType.Cube, new Vector3(2.55f, 16.95f, 133f), new Vector3(0.25f, 1.8f, 0.9f), new Vector3(0f, 0f, -12f), Mat("Colchon", 0.65f, 0.6f, 0.45f));
        ConstructorF7.Neumatico(t, new Vector3(-2.4f, 16.12f, 129f));
        ConstructorF7.Neumatico(t, new Vector3(-2.3f, 16.32f, 129.1f));
        ConstructorF7.Botella(t, new Vector3(1.8f, 16.06f, 123f), true);
        ConstructorF7.Botella(t, new Vector3(-1.9f, 16.13f, 133.5f), false);
        ConstructorF7.Grafiti(t, "CUMAS 4EVER", new Vector3(-2.97f, 17.6f, 123.5f), 90f, new Color(0.95f, 0.25f, 0.6f));
        ConstructorF7.Grafiti(t, "EL PASTERO MANDA", new Vector3(2.97f, 17.9f, 128.5f), -90f, new Color(0.3f, 0.9f, 0.4f));
        ConstructorF7.Grafiti(t, "NO MEAR", new Vector3(-2.97f, 17.2f, 132.5f), 90f, new Color(1f, 0.85f, 0.2f));
        // Los edificios del pasaje son mas oscuros (tinte)
        foreach (Transform e in Object.FindObjectsByType<Transform>(FindObjectsSortMode.InstanceID))
            if (e.name.StartsWith("Edificio_") && !e.name.StartsWith("Edificio_Fondo") && e.position.y > 15f && e.position.z > 117f && e.position.z < 136f)
                ConstructorF5.Tenir(e.gameObject, "Oscuro", new Color(0.55f, 0.5f, 0.7f));
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.name == "Luz_Farol" && Vector3.Distance(l.transform.position, new Vector3(2.4f, 20.1f, 131f)) < 2f)
                l.gameObject.AddComponent<ParpadeoLuz>().luz = l;
    }

    // ---------- Cancha: lineas, segundo arco, galeria, focos y la "base" de Julio ----------
    static void Cancha()
    {
        Transform t = ConstructorF7.Hijo(deco, "Cancha_Detalles");
        float y = 16.05f;
        Material blanco = Mat("Linea_Blanca", 0.85f, 0.85f, 0.85f);
        Pieza(t, "Linea", PrimitiveType.Cube, new Vector3(-11.4f, y, 150f), new Vector3(0.12f, 0.01f, 25f), Vector3.zero, blanco);
        Pieza(t, "Linea", PrimitiveType.Cube, new Vector3(11.4f, y, 150f), new Vector3(0.12f, 0.01f, 25f), Vector3.zero, blanco);
        Pieza(t, "Linea", PrimitiveType.Cube, new Vector3(0f, y, 137.6f), new Vector3(22.8f, 0.01f, 0.12f), Vector3.zero, blanco);
        Pieza(t, "Linea", PrimitiveType.Cube, new Vector3(0f, y, 162.4f), new Vector3(22.8f, 0.01f, 0.12f), Vector3.zero, blanco);
        Pieza(t, "Linea", PrimitiveType.Cube, new Vector3(0f, y, 150f), new Vector3(22.8f, 0.01f, 0.12f), Vector3.zero, blanco);
        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.PI * 2f / 24f;
            Pieza(t, "Circulo", PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 3f, y, 150f + Mathf.Sin(a) * 3f), new Vector3(0.12f, 0.01f, 0.8f), new Vector3(0f, -a * Mathf.Rad2Deg, 0f), blanco);
        }
        Material arco = Mat("Blanco", 0.95f, 0.95f, 0.95f);
        Pieza(t, "Arco2_Poste", PrimitiveType.Cube, new Vector3(-2.5f, 17f, 138f), new Vector3(0.2f, 2f, 0.2f), Vector3.zero, arco);
        Pieza(t, "Arco2_Poste", PrimitiveType.Cube, new Vector3(2.5f, 17f, 138f), new Vector3(0.2f, 2f, 0.2f), Vector3.zero, arco);
        Pieza(t, "Arco2_Travesano", PrimitiveType.Cube, new Vector3(0f, 18f, 138f), new Vector3(5.2f, 0.2f, 0.2f), Vector3.zero, arco);
        // Galeria de tablones (solida) dentro de la reja, al oeste
        Material tablon = Mat("Madera_Vieja", 0.45f, 0.3f, 0.2f);
        for (int i = 0; i < 3; i++)
        {
            float alto = 0.5f + i * 0.4f;
            GameObject g = ConstructorNivel1.Caja(t, "Galeria", new Vector3(-10.4f - i * 0.6f, 16f + alto / 2f, 143f), new Vector3(0.6f, alto, 6f), tablon);
            g.isStatic = true;
        }
        // Focos (apagados) en las esquinas, entre la reja y las casas
        Material reja = Mat("Reja_Cancha", 0.35f, 0.38f, 0.4f);
        foreach (Vector3 c in new[] { new Vector3(-12.85f, 16f, 137.3f), new Vector3(12.85f, 16f, 137.3f), new Vector3(-12.85f, 16f, 163.2f), new Vector3(12.85f, 16f, 163.2f) })
        {
            Pieza(t, "Foco_Poste", PrimitiveType.Cylinder, c + Vector3.up * 4f, new Vector3(0.15f, 4f, 0.15f), Vector3.zero, reja);
            Pieza(t, "Foco", PrimitiveType.Cube, c + Vector3.up * 8.1f, new Vector3(1f, 0.6f, 0.3f), new Vector3(0f, c.x < 0 ? 60f : -60f, 0f), Mat("Foco_Apagado", 0.3f, 0.3f, 0.32f));
        }
        ConstructorF5.Ajustar(arte + "Entorno/KayKit_Furniture/couch.fbx", new Bounds(new Vector3(6.5f, 16.45f, 161.6f), new Vector3(2.2f, 0.9f, 0.9f)), 180f, t, "Sillon_Julio");
        ConstructorF7.Neumatico(t, new Vector3(4.5f, 16.12f, 161.8f));
        ConstructorF7.Neumatico(t, new Vector3(-6f, 16.12f, 159f));
        ConstructorF7.Botella(t, new Vector3(5.6f, 16.06f, 160.4f), true);
        ConstructorF7.Botella(t, new Vector3(7.8f, 16.13f, 160.6f), false);
    }

    // ---------- Cables y ropa colgada: siempre de fachada a fachada ----------
    static void Cables()
    {
        Transform t = ConstructorF7.Hijo(deco, "Cables");
        Material cable = Mat("Cable", 0.05f, 0.05f, 0.06f);
        // Pasaje (fachadas en x = -4.2 y 4.2; los edificios miden 7 m o mas)
        ConstructorF7.Cable(t, new Vector3(-4.15f, 6.4f, 2f), new Vector3(4.15f, 6.7f, 5f), cable);
        ConstructorF7.Cable(t, new Vector3(3.6f, 7.0f, 6f), new Vector3(-4.15f, 6.2f, 10f), cable);
        ConstructorF7.Cable(t, new Vector3(-4.15f, 6.6f, 22f), new Vector3(4.15f, 6.4f, 25f), cable);
        ConstructorF7.Cable(t, new Vector3(-3.6f, 7.0f, 27f), new Vector3(4.15f, 6.5f, 29f), cable);
        ConstructorF7.Tendedero(t, new Vector3(-4.15f, 4.6f, 7.5f), new Vector3(4.15f, 4.4f, 7.5f));
        ConstructorF7.Tendedero(t, new Vector3(-4.15f, 4.2f, 26f), new Vector3(4.15f, 4.4f, 26f));
        // Plaza: del edificio oeste (x -8) al edificio este (x 9.4)
        ConstructorF7.Cable(t, new Vector3(-7.95f, 6.5f, 33f), new Vector3(9.35f, 6.5f, 35f), cable);
        ConstructorF7.Cable(t, new Vector3(-7.95f, 6.8f, 50f), new Vector3(1.95f, 6.6f, 55.15f), cable);
        // Calle alta: de la fila oeste (x -10) a las casas del lado bajo (x 2)
        ConstructorF7.Cable(t, new Vector3(-9.4f, 10.4f, 80f), new Vector3(1.95f, 9.6f, 70f), cable);
        ConstructorF7.Tendedero(t, new Vector3(-9.95f, 8.2f, 66f), new Vector3(1.95f, 8.4f, 67f));
        // Pasaje oscuro
        ConstructorF7.Cable(t, new Vector3(-2.95f, 21.5f, 121f), new Vector3(2.95f, 21.2f, 127f), cable);
        ConstructorF7.Cable(t, new Vector3(2.95f, 21.5f, 129f), new Vector3(-2.95f, 21f, 134f), cable);
        ConstructorF7.Tendedero(t, new Vector3(-2.95f, 19.6f, 125.5f), new Vector3(2.95f, 19.4f, 125.5f));
    }

    // Cartel real: tabla con marco y el texto encima (nada de letras flotando en el aire).
    // rotY = hacia donde mira el cartel (0 = hacia +Z).
    public static void Letrero(Transform padre, string texto, Vector3 centro, float rotY, float ancho, float alto, Color fondo, Color colorTexto, float tamanio)
    {
        Quaternion giro = Quaternion.Euler(0f, rotY, 0f);
        Vector3 frente = giro * Vector3.forward;
        GameObject marco = Pieza(padre, "Letrero_Marco", PrimitiveType.Cube, centro - frente * 0.01f, new Vector3(ancho + 0.06f, alto + 0.06f, 0.03f), new Vector3(0f, rotY, 0f), Mat("Letrero_Marco", 0.2f, 0.15f, 0.1f));
        GameObject tabla = Pieza(padre, "Letrero_Tabla", PrimitiveType.Cube, centro + frente * 0.01f, new Vector3(ancho, alto, 0.03f), new Vector3(0f, rotY, 0f),
                                 ConstructorF2.Material("Letrero_" + ColorUtility.ToHtmlStringRGB(fondo), fondo));
        TMPro.TMP_Text t = ConstructorF7.Texto3D(padre, texto, centro + frente * 0.03f, rotY, tamanio, colorTexto);
        t.rectTransform.sizeDelta = new Vector2(ancho * 0.95f / 1f, alto);
        t.enableWordWrapping = false;
    }

    // ---------- Murales originales sobre muros reales ----------
    static void Murales()
    {
        Transform t = ConstructorF7.Hijo(deco, "Murales");
        // 1) En la medianera/costado del edificio que mira al sitio eriazo (cara z = 12, hacia +z)
        ConstructorF7.Mural(t, "Mural_Ola", ConstructorF7.TexturaMuralOlas(), new Vector3(7.4f, 3.6f, 12.03f), 0f, new Vector2(5.6f, 3.4f), "VALPO NUNCA DUERME", new Color(1f, 0.95f, 0.85f));
        // 2) En el muro de piedra del descanso (cara oeste, x = -2), visto desde la calle alta
        ConstructorF7.Mural(t, "Mural_Geometrico", ConstructorF7.TexturaMuralGeometrico(), new Vector3(-2.03f, 5f, 82.7f), -90f, new Vector2(5.8f, 1.8f), "ARRIBA EL ÁNIMO", Color.white);
        // 3) En la cara de piedra del cerro, sobre la calle alta (inclinado como la cara)
        float angulo = Mathf.Atan2(4.4f, 12f) * Mathf.Rad2Deg;
        Vector3 normal = Quaternion.Euler(angulo, 0f, 0f) * Vector3.back;
        Vector3 centro = new Vector3(-7f, 10f, 110.7f + 6f * 0.3667f) + normal * 0.05f;
        ConstructorF7.Mural(t, "Mural_Ojo", ConstructorF7.TexturaMuralOjo(), centro, 180f, new Vector2(8f, 5f), "EL CERRO TE MIRA", new Color(1f, 0.9f, 0.4f));
        Quaternion inclinacion = Quaternion.Euler(angulo, 0f, 0f);
        foreach (Transform hijo in t)
        {
            if (hijo.name == "Mural_Ojo")
                hijo.rotation = Quaternion.Euler(angulo, hijo.rotation.eulerAngles.y, 0f);
            if (hijo.name == "Texto_EL CERRO TE MIRA")
            {
                hijo.rotation = Quaternion.Euler(angulo, hijo.rotation.eulerAngles.y, 0f);
                hijo.position = centro + inclinacion * Vector3.down * 1.6f + normal * 0.03f;
            }
        }
    }
}
