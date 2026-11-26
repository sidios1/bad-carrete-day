using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Herramienta de EDITOR — El mundo del Nivel 1 como un CERRO REAL.
// El jugador sube el cerro hacia +Z. Hacia el ESTE (+X) el cerro baja al plan y la bahia;
// hacia el OESTE (-X) el cerro sigue subiendo con casas.
//
// Reglas:
//  - Todo va sobre un Terrain continuo (no hay vacio en ninguna parte).
//  - Los edificios usan ESCALA UNIFORME (nunca se deforman) y tienen su propio collider.
//  - Lo que esta por debajo de un edificio llega hasta el terreno (cimientos o pilotes).
//  - Cada borde jugable tiene un limite que se ve (baranda, reja, muro, fachada) + un collider.
//
// Alturas del recorrido:  pasaje y plaza y=0 · calle alta y=4 · techos y=8 · cima y=16
// ConstructorF3 arma la jugabilidad (NPCs, mision, monedas) usando lo que se construye aqui.
public static class ConstructorNivel1
{
    const string arte = "Assets/BadCarreteDay/Arte/";
    const string kaykit = arte + "Entorno/KayKit_CityBuilder/";
    public const float escalaKayKit = 4.7f;   // un auto de KayKit mide 0,94 -> 4,4 m reales
    public const float escalaKenney = 7.4f;   // un contenedor de Kenney mide 0,82 -> 6,1 m reales

    static Terrain terreno;
    static int contadorEdificios;

    // Alturas utiles que la jugabilidad necesita (para poner la plata encima)
    public static float topeCapo;           // altura del capo del ultimo auto
    public static Vector3 posicionCapo;     // centro del capo del ultimo auto
    public static float topeCajonLote, topeAutoLote, topeContenedor, topeCajonPlaza, topeAutoPlaza, topeKiosko, topeBalcon;

    // ===================== ALTURA DEL RECORRIDO =====================

    // Altura del piso del recorrido segun z (la "columna vertebral" del nivel)
    public static float RutaY(float z)
    {
        if (z <= 55f) return 0f;                                       // pasaje y plaza
        if (z <= 63f) return Mathf.Lerp(0f, 4f, (z - 55f) / 8f);        // escalera de Toby
        if (z <= 110.7f) return 4f;                                     // calle alta
        if (z <= 115.1f) return Mathf.Lerp(4f, 16f, (z - 110.7f) / 4.4f); // cara del cerro (ascensor)
        return 16f;                                                     // cima
    }

    static float RutaMin(float z)
    {
        float m = 999f;
        for (float d = -6f; d <= 6f; d += 0.5f) m = Mathf.Min(m, RutaY(z + d));
        return m;
    }

    static float RutaSuave(float z)
    {
        float s = 0f; int n = 0;
        for (float d = -15f; d <= 15f; d += 1f) { s += RutaY(z + d); n++; }
        return s / n;
    }

    // Limites oeste/este de la franja del recorrido (afuera empieza la ladera)
    static float BordeOeste(float z) { return -16f; }
    static float BordeEste(float z)
    {
        if (z < 55f) return 12f;     // pasaje y plaza
        if (z < 115f) return 2f;     // calle alta: al este estan las casas del lado bajo
        return 16f;                  // cima
    }

    // Altura del terreno en (x, z): bajo el recorrido queda 1 m mas abajo (los pisos tienen su propio collider),
    // al oeste sube el cerro y al este baja hasta el puerto y el mar.
    public static float AlturaTerreno(float x, float z)
    {
        float oeste = BordeOeste(z), este = BordeEste(z);
        float h;
        if (x < oeste)
            h = RutaSuave(z) - 1f + Mathf.Min((oeste - x) * 0.6f, 75f);
        else if (x <= este)
            h = RutaMin(z) - 1f;
        else
        {
            h = RutaMin(z) - 1f - (x - este) * 0.8f;
            h = Mathf.Max(h, -36f);                       // explanada del puerto
            if (x > 82f) h = Mathf.Lerp(-36f, -46f, Mathf.Clamp01((x - 82f) / 6f)); // fondo del mar
        }
        if (x < este && z < -14f) h += (-14f - z) * 0.35f;  // detras del inicio el cerro tambien sube
        if (z > 172f) h += (z - 172f) * 0.5f;              // detras de la cancha sigue subiendo
        return h;
    }

    // ===================== TERRENO =====================

    public static Terrain Terreno()
    {
        string ruta = "Assets/BadCarreteDay/Escenas/Nivel1_Terreno.asset";
        AssetDatabase.DeleteAsset(ruta);

        TerrainData datos = new TerrainData();
        datos.heightmapResolution = 513;
        Vector3 origen = new Vector3(-240f, -50f, -200f);
        datos.size = new Vector3(640f, 180f, 640f);

        int n = datos.heightmapResolution;
        float[,] alturas = new float[n, n];
        for (int iz = 0; iz < n; iz++)
            for (int ix = 0; ix < n; ix++)
            {
                float x = origen.x + ix / (float)(n - 1) * datos.size.x;
                float z = origen.z + iz / (float)(n - 1) * datos.size.z;
                alturas[iz, ix] = Mathf.Clamp01((AlturaTerreno(x, z) - origen.y) / datos.size.y);
            }
        datos.SetHeights(0, 0, alturas);

        // Capas: pasto seco (plano), tierra (pendiente) y roca (muy empinado)
        TerrainLayer pasto = Capa("Terreno_Pasto", TexturaRuido(new Color(0.36f, 0.36f, 0.22f), new Color(0.27f, 0.29f, 0.17f), 11), 6f);
        TerrainLayer tierra = Capa("Terreno_Tierra", TexturaRuido(new Color(0.42f, 0.33f, 0.24f), new Color(0.33f, 0.25f, 0.18f), 23), 5f);
        TerrainLayer roca = Capa("Terreno_Roca", TexturaPiedra(), 4f);
        datos.terrainLayers = new TerrainLayer[] { pasto, tierra, roca };
        datos.alphamapResolution = 256;
        int a = datos.alphamapResolution;
        float[,,] mapa = new float[a, a, 3];
        for (int iz = 0; iz < a; iz++)
            for (int ix = 0; ix < a; ix++)
            {
                float pendiente = datos.GetSteepness(ix / (float)(a - 1), iz / (float)(a - 1));
                float wRoca = Mathf.InverseLerp(38f, 50f, pendiente);
                float wTierra = Mathf.InverseLerp(18f, 30f, pendiente) * (1f - wRoca);
                float wPasto = Mathf.Max(0f, 1f - wRoca - wTierra);
                mapa[iz, ix, 0] = wPasto; mapa[iz, ix, 1] = wTierra; mapa[iz, ix, 2] = wRoca;
            }
        datos.SetAlphamaps(0, 0, mapa);
        AssetDatabase.CreateAsset(datos, ruta);

        GameObject obj = Terrain.CreateTerrainGameObject(datos);
        obj.name = "Terreno_Cerro";
        obj.transform.position = origen;
        terreno = obj.GetComponent<Terrain>();
        Shader s = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (s != null)
        {
            string rutaMat = "Assets/BadCarreteDay/Materiales/Terreno_Cerro.mat";
            AssetDatabase.DeleteAsset(rutaMat);
            Material m = new Material(s);
            AssetDatabase.CreateAsset(m, rutaMat);
            terreno.materialTemplate = m;
        }
        terreno.drawInstanced = true;
        obj.isStatic = true;
        return terreno;
    }

    public static float AlturaSuelo(float x, float z)
    {
        if (terreno == null) return AlturaTerreno(x, z);
        return terreno.SampleHeight(new Vector3(x, 0f, z)) + terreno.transform.position.y;
    }

    static TerrainLayer Capa(string nombre, Texture2D tex, float metros)
    {
        string ruta = "Assets/BadCarreteDay/Materiales/" + nombre + ".terrainlayer";
        AssetDatabase.DeleteAsset(ruta);
        TerrainLayer c = new TerrainLayer();
        c.diffuseTexture = tex;
        c.tileSize = new Vector2(metros, metros);
        AssetDatabase.CreateAsset(c, ruta);
        return c;
    }

    // ===================== PIEZAS DEL MUNDO (con collider) =====================

    // Piso caminable: caja que va desde "topY" hacia abajo hasta enterrarse en el terreno
    public static GameObject Suelo(Transform padre, string nombre, float minX, float maxX, float minZ, float maxZ, float topY, Material mat)
    {
        float fondo = Mathf.Min(topY - 1f, MinimoTerreno(minX, maxX, minZ, maxZ) - 0.5f);
        GameObject s = ConstructorF2.Cubo(nombre, new Vector3((minX + maxX) / 2f, (topY + fondo) / 2f, (minZ + maxZ) / 2f),
                                          new Vector3(maxX - minX, topY - fondo, maxZ - minZ), mat);
        s.transform.SetParent(padre, true);
        s.isStatic = true;
        // El material se repite cada ~1,6 m sin estirarse (aunque la caja sea alta)
        return s;
    }

    // Caja solida con collider (muros, mesones, plataformas)
    public static GameObject Caja(Transform padre, string nombre, Vector3 centro, Vector3 tam, Material mat)
    {
        GameObject s = ConstructorF2.Cubo(nombre, centro, tam, mat);
        s.transform.SetParent(padre, true);
        s.isStatic = true;
        return s;
    }

    // Capa "Limite": muros invisibles y rejas con puas (no se pueden agarrar con AgarreBorde)
    public static int CapaLimite()
    {
        SerializedObject tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty capas = tm.FindProperty("layers");
        for (int i = 8; i < 32; i++)
            if (capas.GetArrayElementAtIndex(i).stringValue == "Limite") return i;
        for (int i = 8; i < 32; i++)
            if (string.IsNullOrEmpty(capas.GetArrayElementAtIndex(i).stringValue))
            {
                capas.GetArrayElementAtIndex(i).stringValue = "Limite";
                tm.ApplyModifiedProperties();
                return i;
            }
        return 0;
    }

    public const float altoPerimetro = 30f; // nadie salta (ni escala) 30 m

    // Collider invisible (respaldo de barandas, rejas y bordes del mapa)
    public static GameObject Limite(Transform padre, Vector3 a, Vector3 b, float alto = altoPerimetro)
    {
        GameObject l = new GameObject("Limite");
        l.layer = CapaLimite();
        l.transform.SetParent(padre, false);
        Vector3 d = b - a; d.y = 0f;
        l.transform.position = (a + b) / 2f + Vector3.up * alto / 2f;
        l.transform.rotation = Quaternion.LookRotation(d.normalized);
        BoxCollider c = l.AddComponent<BoxCollider>();
        c.size = new Vector3(0.3f, alto, d.magnitude);
        l.isStatic = true;
        return l;
    }

    // Escalera caminable: escalones de ~0,2 m (menos que el StepOffset del jugador: no hay que saltar)
    public static void Escalera(Transform padre, string nombre, float minX, float maxX, float z0, float z1, float y0, float y1, int escalones, Material mat)
    {
        float huella = (z1 - z0) / escalones;
        float contra = (y1 - y0) / escalones;
        for (int i = 0; i < escalones; i++)
        {
            float tope = y0 + contra * (i + 1);
            float fondo = Mathf.Min(y0 - 0.5f, MinimoTerreno(minX, maxX, z0 + huella * i, z0 + huella * (i + 1)) - 0.5f);
            Caja(padre, nombre + "_" + i, new Vector3((minX + maxX) / 2f, (tope + fondo) / 2f, z0 + huella * (i + 0.5f)),
                 new Vector3(maxX - minX, tope - fondo, huella + 0.02f), mat);
        }
    }

    // Baranda metalica (se ve) + collider invisible alto (no se puede saltar por encima)
    public static void Baranda(Transform padre, Vector3 a, Vector3 b, float alto = 1.05f)
    {
        Material metal = ConstructorF2.Material("Baranda_Metal", new Color(0.2f, 0.32f, 0.3f));
        Vector3 d = b - a;
        int postes = Mathf.Max(1, Mathf.CeilToInt(d.magnitude / 1.5f));
        for (int i = 0; i <= postes; i++)
        {
            Vector3 p = Vector3.Lerp(a, b, i / (float)postes);
            ConstructorF7.Pieza(padre, "Baranda_Poste", PrimitiveType.Cylinder, p + Vector3.up * alto / 2f, new Vector3(0.06f, alto / 2f, 0.06f), Vector3.zero, metal);
        }
        foreach (float h in new[] { alto, alto * 0.5f })
        {
            GameObject r = ConstructorF7.Pieza(padre, "Baranda_Tubo", PrimitiveType.Cylinder, (a + b) / 2f + Vector3.up * h, new Vector3(0.05f, d.magnitude / 2f, 0.05f), Vector3.zero, metal);
            r.transform.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
        }
        Limite(padre, a, b, altoPerimetro);
    }

    // Reja de malla. Sin puas: se puede escalar (colgarse del borde de arriba).
    // Con puas: alambre de puas arriba, hace daño y empuja (RejaPuas), y no se puede agarrar.
    public static void Reja(Transform padre, Vector3 a, Vector3 b, float alto, bool conPuas, bool esBordeDelMapa)
    {
        Material metal = ConstructorF2.Material("Reja_Cancha", new Color(0.35f, 0.38f, 0.4f));
        Vector3 d = b - a; d.y = 0f;
        Quaternion giro = Quaternion.LookRotation(Vector3.Cross(Vector3.up, d.normalized));
        int postes = Mathf.Max(1, Mathf.CeilToInt(d.magnitude / 2.6f));
        for (int k = 0; k <= postes; k++)
            ConstructorF7.Pieza(padre, "Reja_Poste", PrimitiveType.Cylinder, Vector3.Lerp(a, b, k / (float)postes) + Vector3.up * alto / 2f, new Vector3(0.08f, alto / 2f, 0.08f), Vector3.zero, metal);
        foreach (float h in new[] { 0.1f, alto / 2f, alto - 0.05f })
        {
            GameObject al = ConstructorF7.Pieza(padre, "Reja_Alambre", PrimitiveType.Cylinder, (a + b) / 2f + Vector3.up * h, new Vector3(0.05f, d.magnitude / 2f, 0.05f), Vector3.zero, metal);
            al.transform.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
        }
        GameObject malla = ConstructorF7.Pieza(padre, "Reja_Malla", PrimitiveType.Quad, (a + b) / 2f + Vector3.up * alto / 2f, new Vector3(d.magnitude, alto, 1f), Vector3.zero, ConstructorF3.MaterialMalla(d.magnitude));
        malla.transform.rotation = giro;
        malla.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Collider solido de la reja (delgado)
        GameObject solido = new GameObject(conPuas ? "Reja_Puas" : "Reja_Escalable");
        solido.transform.SetParent(padre, false);
        solido.transform.position = (a + b) / 2f + Vector3.up * alto / 2f;
        solido.transform.rotation = giro;
        solido.AddComponent<BoxCollider>().size = new Vector3(d.magnitude, alto, 0.12f);
        solido.isStatic = true;

        if (conPuas)
        {
            solido.layer = CapaLimite();
            // Alambre de puas en espiral arriba
            Material puas = ConstructorF2.Material("Alambre_Puas", new Color(0.55f, 0.55f, 0.58f));
            int vueltas = Mathf.CeilToInt(d.magnitude / 0.35f);
            for (int k = 0; k < vueltas; k++)
            {
                Vector3 p = Vector3.Lerp(a, b, (k + 0.5f) / vueltas) + Vector3.up * (alto + 0.2f);
                GameObject anillo = ConstructorF7.Pieza(padre, "Puas", PrimitiveType.Cylinder, p, new Vector3(0.42f, 0.008f, 0.42f), Vector3.zero, puas);
                anillo.transform.rotation = Quaternion.LookRotation(d.normalized) * Quaternion.Euler(90f, 0f, 20f);
                anillo.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // Trigger que pincha (un poco mas grande que la reja)
            GameObject pincha = new GameObject("Puas_Trigger");
            pincha.transform.SetParent(solido.transform, false);
            BoxCollider t = pincha.AddComponent<BoxCollider>();
            t.isTrigger = true;
            t.size = new Vector3(d.magnitude, alto + 0.6f, 0.9f);
            t.center = new Vector3(0f, 0.3f, 0f);
            ConstructorBadCarreteDay.Agregar<RejaPuas>(pincha);
        }
        if (esBordeDelMapa) Limite(padre, a, b, altoPerimetro);
    }

    // Muro de contencion de piedra (con collider)
    public static GameObject MuroPiedra(Transform padre, string nombre, Vector3 centro, Vector3 tam)
    {
        return Caja(padre, nombre, centro, tam, MaterialPiedra(tam));
    }

    static Material MaterialPiedra(Vector3 tam)
    {
        Material m = ConstructorF2.Material("Muro_Piedra", new Color(0.62f, 0.6f, 0.56f));
        m.SetTexture("_BaseMap", TexturaPiedraGuardada());
        m.SetTextureScale("_BaseMap", new Vector2(2f, 2f));
        return m;
    }

    // ===================== EDIFICIOS A ESCALA REAL =====================

    // Edificio KayKit con escala uniforme. "frente" = hacia donde mira la fachada (eje X o Z).
    // "puntoFrente" = centro de la fachada a nivel del suelo. Lleva collider y cimientos/pilotes.
    public static GameObject Edificio(Transform padre, string modelo, Vector3 puntoFrente, Vector3 frente, bool pilotes, bool conCimientos = true)
    {
        GameObject m = Modelo(padre, kaykit + modelo + "_withoutBase.fbx", escalaKayKit, Quaternion.LookRotation(frente));
        m.name = "Edificio_" + contadorEdificios++;
        Alinear(m, puntoFrente, frente);
        ColliderDeLimites(m);
        if (conCimientos) Cimientos(padre, m, pilotes);
        m.isStatic = true;
        return m;
    }

    // Ancho de un modelo a lo largo de la fila (para llenar filas sin rendijas)
    static Dictionary<string, float> anchos = new Dictionary<string, float>();
    static float AnchoModelo(string modelo)
    {
        if (anchos.ContainsKey(modelo)) return anchos[modelo];
        GameObject m = Modelo(null, kaykit + modelo + "_withoutBase.fbx", escalaKayKit, Quaternion.identity);
        float ancho = ConstructorF5.Limites(m).size.x;
        Object.DestroyImmediate(m);
        anchos[modelo] = ancho;
        return ancho;
    }

    // Fila de edificios pegados (sin rendijas) a lo largo de la linea a->b, todos mirando hacia "frente".
    // El espacio que sobra al final se cierra con una medianera (muro pintado).
    public static void FilaEdificios(Transform padre, Vector3 a, Vector3 b, Vector3 frente, bool pilotes)
    {
        string[] modelos = ConstructorF7.edificiosKayKit;
        Vector3 dir = (b - a);
        float largo = dir.magnitude;
        dir.Normalize();
        float recorrido = 0f;
        while (largo - recorrido > 0.05f)
        {
            float queda = largo - recorrido;
            List<string> caben = new List<string>();
            foreach (string mo in modelos) if (AnchoModelo(mo) <= queda + 0.01f) caben.Add(mo);
            if (caben.Count == 0)
            {
                Medianera(padre, a + dir * (recorrido + queda / 2f), dir, queda, frente);
                break;
            }
            string elegido = caben[Random.Range(0, caben.Count)];
            float ancho = AnchoModelo(elegido);
            Edificio(padre, elegido, a + dir * (recorrido + ancho / 2f), frente, pilotes);
            recorrido += ancho;
        }
    }

    static void Medianera(Transform padre, Vector3 centroBase, Vector3 dir, float ancho, Vector3 frente)
    {
        Color[] colores = { new Color(0.85f, 0.55f, 0.45f), new Color(0.55f, 0.7f, 0.75f), new Color(0.9f, 0.8f, 0.55f) };
        Material mat = ConstructorF2.Material("Medianera_" + (contadorEdificios % 3), colores[contadorEdificios % 3]);
        float alto = 8f, fondo = 6f;
        Vector3 centro = centroBase - frente * fondo / 2f + Vector3.up * alto / 2f;
        Vector3 tam = Mathf.Abs(dir.x) > 0.5f ? new Vector3(ancho + 0.02f, alto, fondo) : new Vector3(fondo, alto, ancho + 0.02f);
        GameObject m = Caja(padre, "Medianera", centro, tam, mat);
        Cimientos(padre, m, false);
    }

    // Instancia un modelo con escala uniforme (sin colliders propios)
    public static GameObject Modelo(Transform padre, string ruta, float escala, Quaternion rot)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (asset == null) { Debug.LogError("[Nivel1] Falta el modelo " + ruta); return new GameObject("FALTA_" + ruta); }
        GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        PrefabUtility.UnpackPrefabInstance(m, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        m.transform.SetParent(padre, false);
        m.transform.localScale = Vector3.one * escala;
        m.transform.rotation = rot;
        foreach (Collider c in m.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        return m;
    }

    // Mueve el modelo para que su fachada quede en "puntoFrente" y su base en puntoFrente.y
    static void Alinear(GameObject m, Vector3 puntoFrente, Vector3 frente)
    {
        Bounds b = ConstructorF5.Limites(m);
        Vector3 lado = Vector3.Cross(Vector3.up, frente);
        float caraActual = Vector3.Dot(b.center, frente) + Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(frente.x), 0f, Mathf.Abs(frente.z))));
        Vector3 mover = frente * (Vector3.Dot(puntoFrente, frente) - caraActual)
                      + lado * (Vector3.Dot(puntoFrente, lado) - Vector3.Dot(b.center, lado))
                      + Vector3.up * (puntoFrente.y - b.min.y);
        m.transform.position += mover;
    }

    // BoxCollider del tamaño REAL del modelo
    public static BoxCollider ColliderDeLimites(GameObject m)
    {
        Bounds mundo = ConstructorF5.Limites(m);
        BoxCollider col = m.AddComponent<BoxCollider>();
        Vector3 e = m.transform.lossyScale;
        Quaternion inv = Quaternion.Inverse(m.transform.rotation);
        Vector3 tamLocal = inv * mundo.size;
        col.size = new Vector3(Mathf.Abs(tamLocal.x) / e.x, Mathf.Abs(tamLocal.y) / e.y, Mathf.Abs(tamLocal.z) / e.z);
        Vector3 c = inv * (mundo.center - m.transform.position);
        col.center = new Vector3(c.x / e.x, c.y / e.y, c.z / e.z);
        return col;
    }

    // Debajo de cada edificio: cimiento hasta el terreno (si la caida es poca) o pilotes (ladera empinada)
    static void Cimientos(Transform padre, GameObject edificio, bool pilotes)
    {
        Bounds b = ConstructorF5.Limites(edificio);
        float suelo = MinimoTerreno(b.min.x, b.max.x, b.min.z, b.max.z);
        float caida = b.min.y - suelo;
        if (caida <= 0.05f) return;
        if (!pilotes || caida < 2.5f)
        {
            Material zocalo = ConstructorF2.Material("Zocalo", new Color(0.33f, 0.31f, 0.3f));
            float alto = caida + 0.6f;
            ConstructorF7.Pieza(padre, "Cimiento", PrimitiveType.Cube, new Vector3(b.center.x, b.min.y - alto / 2f + 0.02f, b.center.z),
                                new Vector3(b.size.x - 0.1f, alto, b.size.z - 0.1f), Vector3.zero, zocalo);
            return;
        }
        // Pilotes: postes de hormigon en grilla hasta el terreno + vigas
        Material hormigon = ConstructorF2.Material("Pilote", new Color(0.45f, 0.44f, 0.42f));
        int nx = Mathf.Max(2, Mathf.CeilToInt(b.size.x / 3f) + 1), nz = Mathf.Max(2, Mathf.CeilToInt(b.size.z / 3f) + 1);
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                float x = Mathf.Lerp(b.min.x + 0.3f, b.max.x - 0.3f, i / (float)(nx - 1));
                float z = Mathf.Lerp(b.min.z + 0.3f, b.max.z - 0.3f, j / (float)(nz - 1));
                float s = AlturaSuelo(x, z) - 0.4f;
                float alto = b.min.y - s;
                if (alto < 0.2f) continue;
                ConstructorF7.Pieza(padre, "Pilote", PrimitiveType.Cube, new Vector3(x, s + alto / 2f, z), new Vector3(0.4f, alto, 0.4f), Vector3.zero, hormigon);
            }
        ConstructorF7.Pieza(padre, "Viga", PrimitiveType.Cube, new Vector3(b.center.x, b.min.y - 0.2f, b.center.z), new Vector3(b.size.x, 0.4f, b.size.z), Vector3.zero, hormigon);
    }

    static float MinimoTerreno(float minX, float maxX, float minZ, float maxZ)
    {
        float m = 999f;
        for (int i = 0; i <= 3; i++)
            for (int j = 0; j <= 3; j++)
                m = Mathf.Min(m, AlturaSuelo(Mathf.Lerp(minX, maxX, i / 3f), Mathf.Lerp(minZ, maxZ, j / 3f)));
        return m;
    }

    static float MaximoTerreno(float minX, float maxX, float minZ, float maxZ)
    {
        float m = -999f;
        for (int i = 0; i <= 3; i++)
            for (int j = 0; j <= 3; j++)
                m = Mathf.Max(m, AlturaSuelo(Mathf.Lerp(minX, maxX, i / 3f), Mathf.Lerp(minZ, maxZ, j / 3f)));
        return m;
    }

    // ===================== OBJETOS REALES PARA SALTAR =====================

    // Auto estacionado (KayKit). Devuelve la altura del techo.
    public static float Auto(Transform padre, string modelo, Vector3 centroSuelo, float rotY)
    {
        GameObject m = Modelo(padre, kaykit + modelo + ".fbx", escalaKayKit, Quaternion.Euler(0f, rotY, 0f));
        m.name = "Auto_" + modelo;
        Bounds b = ConstructorF5.Limites(m);
        m.transform.position += new Vector3(centroSuelo.x - b.center.x, centroSuelo.y - b.min.y, centroSuelo.z - b.center.z);
        m.isStatic = true;
        b = ConstructorF5.Limites(m);
        // Fisica logica: carroceria (capo y maleta) baja + cabina alta al centro
        bool largoEnZ = b.size.z > b.size.x;
        float alto = b.size.y, capo = b.min.y + alto * 0.58f;
        Caja(m.transform, "Auto_Carroceria", new Vector3(b.center.x, (b.min.y + capo) / 2f, b.center.z), new Vector3(b.size.x, capo - b.min.y, b.size.z), null).GetComponent<Renderer>().enabled = false;
        Vector3 tamCabina = largoEnZ ? new Vector3(b.size.x * 0.92f, alto, b.size.z * 0.48f) : new Vector3(b.size.x * 0.48f, alto, b.size.z * 0.92f);
        Caja(m.transform, "Auto_Cabina", new Vector3(b.center.x, b.center.y, b.center.z), tamCabina, null).GetComponent<Renderer>().enabled = false;
        topeCapo = capo;
        // Centro del capo (adelante de la cabina)
        float adelante = (largoEnZ ? b.size.z : b.size.x) * 0.37f;
        Vector3 haciaAdelante = Quaternion.Euler(0f, rotY, 0f) * Vector3.forward;
        posicionCapo = new Vector3(b.center.x, capo, b.center.z) + haciaAdelante * adelante;
        return b.max.y;
    }

    // Contenedor-bodega (Kenney). Devuelve la altura del techo.
    public static float Contenedor(Transform padre, string modelo, Vector3 centroSuelo, float rotY)
    {
        GameObject m = Modelo(padre, arte + "Entorno/Kenney_Industrial/" + modelo + ".fbx", 1f, Quaternion.Euler(0f, rotY, 0f));
        m.name = "Contenedor";
        // Escala uniforme para que mida lo de un contenedor real de 20 pies (6,06 m de largo)
        Bounds b0 = ConstructorF5.Limites(m);
        m.transform.localScale = Vector3.one * (6.06f / Mathf.Max(b0.size.x, b0.size.z));
        Bounds b = ConstructorF5.Limites(m);
        m.transform.position += new Vector3(centroSuelo.x - b.center.x, centroSuelo.y - b.min.y, centroSuelo.z - b.center.z);
        ColliderDeLimites(m);
        m.isStatic = true;
        return ConstructorF5.Limites(m).max.y;
    }

    // Cajon de madera de 0,8 m (para subirse). Devuelve la altura de arriba.
    public static float Cajon(Transform padre, Vector3 centroSuelo)
    {
        GameObject m = ConstructorF5.Ajustar(arte + "Entorno/KayKit_Restaurant/crate.fbx",
            new Bounds(centroSuelo + Vector3.up * 0.4f, Vector3.one * 0.8f), Random.Range(-10f, 10f), padre, "Cajon");
        ColliderDeLimites(m);
        m.isStatic = true;
        return centroSuelo.y + 0.8f;
    }

    // Kiosko de lata (2,5 m) con techo, ventana, letrero. Devuelve la altura del techo.
    public static float Kiosko(Transform padre, Vector3 centroSuelo, float lado)
    {
        Transform k = ConstructorF7.Hijo(padre, "Kiosko");
        Material lata = ConstructorF2.Material("Kiosko_Lata", new Color(0.15f, 0.5f, 0.55f));
        Material techo = ConstructorF2.Material("Kiosko_Techo", new Color(0.6f, 0.6f, 0.62f));
        float alto = 2.4f;
        Caja(k, "Kiosko_Cuerpo", centroSuelo + Vector3.up * alto / 2f, new Vector3(lado, alto, lado), lata);
        Caja(k, "Kiosko_Techo", centroSuelo + Vector3.up * (alto + 0.06f), new Vector3(lado + 0.5f, 0.12f, lado + 0.5f), techo);
        // Franjas de lata acanalada
        for (int i = 0; i < 8; i++)
        {
            float t = -lado / 2f + lado * (i + 0.5f) / 8f;
            ConstructorF7.Pieza(k, "Canal", PrimitiveType.Cube, centroSuelo + new Vector3(t, alto * 0.45f, -lado / 2f - 0.02f), new Vector3(0.05f, alto * 0.85f, 0.03f), Vector3.zero, techo);
        }
        ConstructorF7.Pieza(k, "Kiosko_Ventana", PrimitiveType.Cube, centroSuelo + new Vector3(0f, 1.45f, -lado / 2f - 0.03f), new Vector3(lado * 0.7f, 0.7f, 0.03f), Vector3.zero,
                            ConstructorF7.MaterialEmisivo("Kiosko_Luz", new Color(1f, 0.85f, 0.55f), 1.2f));
        ConstructorF7.Texto3D(k, "KIOSKO LA CUMBRE", centroSuelo + new Vector3(0f, 2.15f, -lado / 2f - 0.05f), 180f, 0.24f, new Color(1f, 0.9f, 0.4f));
        ConstructorF7.Texto3D(k, "CIGARROS · BEBIDAS · CARGA BIP", centroSuelo + new Vector3(0f, 0.95f, -lado / 2f - 0.05f), 180f, 0.11f, Color.white);
        return centroSuelo.y + alto + 0.12f;
    }

    // ===================== CASAS DEL FONDO (sobre el terreno) =====================

    // Ladera oeste (sube) y ladera este (baja al plan) llenas de casas apoyadas en el terreno
    public static void CasasDeFondo(Transform padre)
    {
        Random.InitState(4321);
        string[] modelos = ConstructorF7.edificiosKayKit;
        for (float z = -60f; z <= 250f; z += 10f)
        {
            for (float x = -125f; x <= 78f; x += 9.5f)
            {
                if (Random.value < 0.18f) continue;
                float px = x + Random.Range(-1.5f, 1.5f), pz = z + Random.Range(-2f, 2f);
                if (!LibreParaFondo(px, pz)) continue;
                bool ladoEste = px > 0f;
                // Miran hacia el recorrido (el jugador las ve de frente)
                Vector3 frente = ladoEste ? Vector3.left : Vector3.right;
                string mo = modelos[Random.Range(0, modelos.Length)];
                GameObject m = Modelo(padre, kaykit + mo + "_withoutBase.fbx", escalaKayKit, Quaternion.LookRotation(frente) * Quaternion.Euler(0f, Random.Range(-6f, 6f), 0f));
                m.name = "Edificio_Fondo_" + contadorEdificios++;
                Bounds b = ConstructorF5.Limites(m);
                m.transform.position += new Vector3(px - b.center.x, 0f, pz - b.center.z);
                b = ConstructorF5.Limites(m);
                // Oeste: la casa se entierra atras en el cerro (base = punto mas bajo).
                // Este: la casa se apoya arriba y abajo lleva pilotes (base = punto mas alto).
                float baseY = ladoEste ? MaximoTerreno(b.min.x, b.max.x, b.min.z, b.max.z) : MinimoTerreno(b.min.x, b.max.x, b.min.z, b.max.z);
                m.transform.position += Vector3.up * (baseY - b.min.y);
                Cimientos(padre, m, ladoEste);
                m.isStatic = true;
            }
        }
    }

    // No poner casas de fondo encima del recorrido, de los edificios jugables ni del puerto
    static bool LibreParaFondo(float x, float z)
    {
        if (x > 60f) return false;                                // puerto y mar
        // Conos de vista despejados hacia el plan y la bahia (sitio eriazo, mirador y hueco de la cancha):
        // ahi la ladera baja libre y se ven las casas de mas abajo y el puerto
        if (x > 0f && x < 50f && ((z > 6f && z < 26f) || (z > 36f && z < 62f) || (z > 143f && z < 160f))) return false;
        if (x < 0f) return x < -22f;                              // oeste: despues de las filas jugables
        if (z < 58f) return x > 20f;                              // este del pasaje y la plaza
        if (z < 113f) return x > 13f;                             // este de la calle alta (casas del lado bajo)
        return x > 24f;                                           // este de la cima
    }

    // ===================== VESTIR (props y detalles; sin cambiar la jugabilidad) =====================

    public static void Vestir(Transform deco, ConstructorF2.Base b)
    {
        Random.InitState(777);
        Transform fondo = new GameObject("Fondo_Casas").transform; // fuera de "Nivel": no entra al NavMesh
        CasasDeFondo(fondo);
        ConstructorNivel1Detalles.Vestir(deco, b);
    }

    // ===================== TEXTURAS GENERADAS =====================

    static Texture2D TexturaRuido(Color a, Color c, int semilla)
    {
        Random.InitState(semilla);
        int n = 128;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGB24, true);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float v = Mathf.PerlinNoise(x * 0.08f + semilla, y * 0.08f) * 0.7f + Random.value * 0.3f;
                t.SetPixel(x, y, Color.Lerp(a, c, v));
            }
        t.Apply();
        return ConstructorF7.GuardarTextura("Terreno_" + semilla, t, true);
    }

    static Texture2D texturaPiedra;
    static Texture2D TexturaPiedraGuardada()
    {
        if (texturaPiedra == null) texturaPiedra = TexturaPiedra();
        return texturaPiedra;
    }

    // Piedras irregulares con junta (muros de contencion de Valparaiso)
    static Texture2D TexturaPiedra()
    {
        Random.InitState(99);
        int n = 128;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGB24, true);
        Color junta = new Color(0.25f, 0.24f, 0.22f);
        int filas = 6;
        for (int y = 0; y < n; y++)
        {
            int fila = y * filas / n;
            float yEnFila = (y * filas / (float)n) - fila;
            for (int x = 0; x < n; x++)
            {
                float desplaz = (fila % 2) * 0.5f;
                float u = (x / (float)n * 4f + desplaz) % 1f;
                bool esJunta = yEnFila < 0.08f || u < 0.04f;
                int idPiedra = fila * 10 + (int)((x / (float)n * 4f + desplaz));
                float tono = 0.5f + 0.25f * Mathf.Sin(idPiedra * 12.9898f);
                Color piedra = new Color(0.6f, 0.57f, 0.52f) * (0.8f + 0.3f * tono) * (0.9f + Random.value * 0.15f);
                t.SetPixel(x, y, esJunta ? junta : piedra);
            }
        }
        t.Apply();
        return ConstructorF7.GuardarTextura("Piedra_Muro", t, true);
    }
}
