using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Herramienta de EDITOR — Revisa que el Nivel 1 este bien conectado:
//  1) Sin huecos: rayos hacia abajo sobre todo lo caminable deben pegar en un piso.
//  2) Toda la plata alcanzable: cada moneda esta sobre una superficie, y a esa superficie se llega
//     desde el piso subiendo de a maximo 1 m (el salto es 1,2 m). Ademas alcanza la plata:
//     con caña (solo caminando) para el agua, y en las zonas 1 y 2 para agua + vino + completo.
//  3) Recorrido completo: el NavMesh conecta cada zona con sus lugares importantes.
//  4) Nada flotando: cada edificio tiene terreno, cimiento, pilotes u otra estructura debajo.
//  5) Ningun edificio de menos de 5 m de alto.
// Uso: menu "Bad Carrete Day/Verificar Nivel 1" o -executeMethod VerificadorNivel1.VerificarEscena
public static class VerificadorNivel1
{
    static List<string> errores = new List<string>();

    [MenuItem("Bad Carrete Day/Verificar Nivel 1")]
    public static void VerificarEscena()
    {
        EditorSceneManager.OpenScene("Assets/BadCarreteDay/Escenas/Nivel1_Cerro.unity");
        int n = Verificar();
        if (Application.isBatchMode) EditorApplication.Exit(n == 0 ? 0 : 1);
    }

    public static int Verificar()
    {
        errores.Clear();
        Physics.SyncTransforms();
        Huecos();
        Plata();
        Recorrido();
        Flotando();
        Alturas();
        foreach (string e in errores) Debug.LogError("[Verificador] " + e);
        Debug.Log("[Verificador] " + (errores.Count == 0 ? "Nivel 1 OK ✅ (sin huecos, plata alcanzable, recorrido completo)" : errores.Count + " problemas ❌"));
        return errores.Count;
    }

    // ---------- 1) Huecos ----------
    struct Zona { public string nombre; public float x0, x1, z0, z1; public Zona(string n, float a, float b, float c, float d) { nombre = n; x0 = a; x1 = b; z0 = c; z1 = d; } }

    static void Huecos()
    {
        Zona[] zonas = {
            new Zona("Pasaje", -4.1f, 4.1f, -6.3f, 31.4f),
            new Zona("Sitio eriazo", 4.3f, 12.2f, 12.1f, 19.9f),
            new Zona("Plaza", -7.9f, 13.7f, 31.1f, 55.1f),
            new Zona("Escalera de Toby", -1.9f, 1.9f, 55.3f, 62.9f),
            new Zona("Calle alta", -9.9f, 1.9f, 63f, 110.5f),
            new Zona("Patio", 2.1f, 8.5f, 90.1f, 92.8f),
            new Zona("Cima", -15.8f, 15.8f, 115.3f, 171.8f),
        };
        foreach (Zona z in zonas)
        {
            int huecos = 0; Vector3 primero = Vector3.zero;
            for (float x = z.x0; x <= z.x1; x += 0.4f)
                for (float zz = z.z0; zz <= z.z1; zz += 0.4f)
                {
                    float esperado = ConstructorNivel1.RutaY(zz);
                    if (z.nombre == "Patio") esperado = 4f;
                    if (z.nombre == "Escalera de Toby") esperado = Mathf.Floor(ConstructorNivel1.RutaY(zz) / 0.2f) * 0.2f;
                    RaycastHit hit;
                    bool ok = Physics.Raycast(new Vector3(x, esperado + 3f, zz), Vector3.down, out hit, 6f, ~0, QueryTriggerInteraction.Ignore)
                              && hit.point.y >= esperado - 0.3f;
                    if (!ok) { if (huecos == 0) primero = new Vector3(x, esperado, zz); huecos++; }
                }
            if (huecos > 0) errores.Add("Hueco en " + z.nombre + ": " + huecos + " puntos sin piso (primero en " + primero + ")");
        }
    }

    // ---------- 2) Plata alcanzable ----------
    static void Plata()
    {
        // Superficies: colliders solidos del nivel (no triggers)
        List<Collider> sup = new List<Collider>();
        foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            if (!c.isTrigger && c.enabled && c.gameObject.activeInHierarchy && !(c is TerrainCollider) && c.name != "Limite" && c.name != "Barrera_Toby") sup.Add(c);

        // Inicio: pisos del recorrido
        HashSet<Collider> alcanzables = new HashSet<Collider>();
        Queue<Collider> cola = new Queue<Collider>();
        foreach (Collider c in sup)
            if (c.name.StartsWith("Suelo_") || c.name == "Calle_Alta" || c.name == "Lote_Eriazo" || c.name == "Cerro_Alto" || c.name.StartsWith("Escalon_") || c.name == "Patio")
            { alcanzables.Add(c); cola.Enqueue(c); }
        while (cola.Count > 0)
        {
            Bounds a = cola.Dequeue().bounds;
            foreach (Collider c in sup)
            {
                if (alcanzables.Contains(c)) continue;
                Bounds bb = c.bounds;
                float subida = bb.max.y - a.max.y;
                // Se sube saltando (hasta 1 m) o colgandose de un borde (hasta 3 m, con caña).
                // Los muros invisibles y las rejas con puas (capa Limite) no se agarran.
                if (subida > 3.0f) continue;
                if (subida > 1.0f && c.gameObject.layer == ConstructorNivel1.CapaLimite()) continue;
                if (SeparacionXZ(a, bb) > 1.0f) continue;
                alcanzables.Add(c); cola.Enqueue(c);
            }
        }

        // Cuentas de la plata que se necesita ANTES de subir la escalera de Toby (z < 55):
        //  - con caña (sin saltar ni colgarse): basureros, la señora Nelly y monedas en el suelo  -> $1.000 (agua)
        //  - en total (zona 1 y 2): agua + vino + completo                                    -> $5.000
        int conCana = 0, abajo = 0;
        foreach (Basurero bas in Object.FindObjectsByType<Basurero>(FindObjectsSortMode.None))
            if (bas.transform.position.z < 55f) { conCana += bas.plata; abajo += bas.plata; }
        if (GameObject.Find("NPC_SraNelly") != null) { conCana += 500; abajo += 500; }
        foreach (Moneda m in Object.FindObjectsByType<Moneda>(FindObjectsSortMode.None))
        {
            if (m.transform.position.z >= 55f) continue;
            abajo += m.puntos;
            RaycastHit piso;
            if (Physics.Raycast(m.transform.position, Vector3.down, out piso, 1.0f, ~0, QueryTriggerInteraction.Ignore) && piso.collider.name.StartsWith("Suelo_"))
                conCana += m.puntos;
        }
        if (conCana < 1000) errores.Add("Con caña (solo caminando) se juntan $" + conCana + ": no alcanza para el agua ($1.000)");
        if (abajo < 5000) errores.Add("En las zonas 1 y 2 hay $" + abajo + ": no alcanza para agua + vino + completo ($5.000)");
        Debug.Log("[Verificador] Plata con caña: $" + conCana + " · plata zonas 1 y 2: $" + abajo);

        foreach (Moneda m in Object.FindObjectsByType<Moneda>(FindObjectsSortMode.None))
        {
            RaycastHit hit;
            Vector3 p = m.transform.position;
            if (!Physics.Raycast(p, Vector3.down, out hit, 3f, ~0, QueryTriggerInteraction.Ignore))
            { errores.Add("Moneda $" + m.puntos + " en " + p + " no tiene nada debajo"); continue; }
            if (p.y - hit.point.y > 1.0f) errores.Add("Moneda $" + m.puntos + " en " + p + " esta a " + (p.y - hit.point.y).ToString("F2") + " m de " + hit.collider.name + " (no se alcanza)");
            if (!alcanzables.Contains(hit.collider)) errores.Add("Moneda $" + m.puntos + " sobre " + hit.collider.name + " en " + p + ": no se puede subir hasta ahi");
            else Debug.Log("[Verificador] Moneda $" + m.puntos + " sobre " + hit.collider.name + " a " + (p.y - hit.point.y).ToString("F2") + " m ✓");
        }

    }

    static float SeparacionXZ(Bounds a, Bounds b)
    {
        float dx = Mathf.Max(0f, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));
        float dz = Mathf.Max(0f, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    // ---------- 3) Recorrido (NavMesh) ----------
    static void Recorrido()
    {
        Camino("inicio -> Don Lucho", new Vector3(0f, 0f, 1f), new Vector3(-2.4f, 0.12f, 10.5f));
        // (al sitio eriazo se entra escalando la reja: no hay camino caminando)
        Camino("inicio -> ventanita del almacen", new Vector3(0f, 0f, 1f), new Vector3(-6.6f, 0f, 41f));
        Camino("inicio -> plato de Toby", new Vector3(0f, 0f, 1f), new Vector3(5f, 0f, 52.5f));
        Camino("inicio -> mirador", new Vector3(0f, 0f, 1f), new Vector3(12.5f, 0f, 45f));
        Camino("escalera -> calle alta", new Vector3(0f, 2f, 59f), new Vector3(-6f, 4f, 95f));
        Camino("calle alta -> patio", new Vector3(-6f, 4f, 70f), new Vector3(5f, 4f, 91.4f));
        Camino("calle alta -> Sra. Gladys (escoba)", new Vector3(-6f, 4f, 95f), new Vector3(-5.5f, 4f, 107.6f));
        Camino("cima -> pasaje oscuro", new Vector3(6f, 16f, 116.5f), new Vector3(0f, 16f, 128f));
        // (entre el pasaje y la cancha esta la reja: se abre al vencer a los cumas)
        Camino("entrada -> cancha", new Vector3(0f, 16f, 138f), new Vector3(0f, 16f, 150f));
        Camino("cancha -> sillon de Julio", new Vector3(0f, 16f, 150f), new Vector3(6.5f, 16f, 160f));
    }

    static void Camino(string nombre, Vector3 a, Vector3 b)
    {
        NavMeshHit ha, hb;
        if (!NavMesh.SamplePosition(a, out ha, 1.5f, NavMesh.AllAreas)) { errores.Add("Sin NavMesh en el inicio de '" + nombre + "' " + a); return; }
        if (!NavMesh.SamplePosition(b, out hb, 1.5f, NavMesh.AllAreas)) { errores.Add("Sin NavMesh en el final de '" + nombre + "' " + b); return; }
        NavMeshPath camino = new NavMeshPath();
        NavMesh.CalculatePath(ha.position, hb.position, NavMesh.AllAreas, camino);
        if (camino.status != NavMeshPathStatus.PathComplete) errores.Add("No hay camino caminando: " + nombre);
    }

    // ---------- 4) Nada flotando ----------
    static void Flotando()
    {
        // Lo que puede sostener un edificio: terreno, cimientos, pilotes, vigas y colliders
        List<Bounds> soportes = new List<Bounds>();
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            if (r.name == "Cimiento" || r.name == "Viga" || r.name == "Pilote") soportes.Add(r.bounds);

        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (!t.name.StartsWith("Edificio_")) continue;
            Bounds b = ConstructorF5.Limites(t.gameObject);
            float baseY = b.min.y;
            bool apoyado = false;
            // Terreno tocando la base en alguna esquina (o la base enterrada)
            foreach (Vector3 e in new[] { new Vector3(b.min.x, 0, b.min.z), new Vector3(b.max.x, 0, b.min.z), new Vector3(b.min.x, 0, b.max.z), new Vector3(b.max.x, 0, b.max.z), b.center })
                if (ConstructorNivel1.AlturaSuelo(e.x, e.z) >= baseY - 0.3f) apoyado = true;
            // Cimiento/viga justo debajo
            if (!apoyado)
                foreach (Bounds s in soportes)
                    if (Mathf.Abs(s.max.y - baseY) < 0.35f && SeparacionXZ(s, b) < 0.01f && s.Intersects(new Bounds(b.center - Vector3.up * (b.extents.y + 0.1f), new Vector3(b.size.x, 0.4f, b.size.z))))
                    { apoyado = true; break; }
            // Otra estructura con collider justo debajo (ej: departamentos sobre el almacen)
            if (!apoyado)
            {
                RaycastHit hit;
                if (Physics.Raycast(new Vector3(b.center.x, baseY + 0.02f, b.center.z), Vector3.down, out hit, 0.6f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.transform != t) apoyado = true;
            }
            if (!apoyado) errores.Add("Edificio flotando: " + t.name + " base y=" + baseY.ToString("F1") + " en " + b.center);
        }
    }

    // ---------- 5) Alturas ----------
    static void Alturas()
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name.StartsWith("Edificio_"))
            {
                float alto = ConstructorF5.Limites(t.gameObject).size.y;
                if (alto < 5f) errores.Add("Edificio bajo: " + t.name + " mide " + alto.ToString("F1") + " m");
            }
    }
}
