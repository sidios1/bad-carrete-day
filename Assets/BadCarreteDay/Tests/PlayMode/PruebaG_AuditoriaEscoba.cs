using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Auditoria de la escoba en la mano de Benja (herramienta: no corre sola).
// Pasa por cada animacion y mide, cuadro a cuadro:
//  - angulo entre la escoba y el eje del puño (base del meñique -> base del indice)
//  - distancia del mango al centro del puño
//  - si la escoba se mete en la cabeza, torso o piernas
//  - si las cerdas se entierran en el suelo
//  - si los dedos estan cerrados
// Deja una tabla "[AUDITORIA]" en el log y fotos en Capturas/Juego/Auditoria.
public class PruebaG_AuditoriaEscoba
{
    public static readonly string[] Estados = { "quieto", "caminar", "correr", "saltar", "pegar", "golpeado", "bostezo", "hablar", "colgado", "subir", "morir" };

    public class Resultado
    {
        public float angulo, distancia, suelo; public int choques, muestras, dedosAbiertos; public string partes = "";
        public bool enEspalda;
    }

    GameObject jugador; Animator anim; Component entradas;
    Dictionary<string, Resultado> tabla = new Dictionary<string, Resultado>();
    public List<string> desactivados = new List<string>();
    string carpeta; RenderTexture rt; Camera cam;

    [SetUp]
    public void MarcarPruebas()
    {
        if (GameObject.Find("PruebasAutomaticas") == null)
            Object.DontDestroyOnLoad(new GameObject("PruebasAutomaticas"));
    }

    [UnityTest, Explicit("Herramienta de auditoria")]
    public IEnumerator AuditarEscoba()
    {
        yield return Recorrer(true);
        string reporte = "\n[AUDITORIA] estado     | ang.puño | dist.puño | choques (partes)        | suelo | dedos abiertos";
        foreach (string e in Estados)
        {
            if (desactivados.Contains(e)) { reporte += "\n[AUDITORIA] " + e.PadRight(10) + " | desactivado con la escoba (no ocurre)"; continue; }
            if (!tabla.ContainsKey(e)) { reporte += "\n[AUDITORIA] " + e.PadRight(10) + " | (sin datos)"; continue; }
            Resultado r = tabla[e];
            reporte += "\n[AUDITORIA] " + e.PadRight(10) + " | " + r.angulo.ToString("F0").PadLeft(5) + "°  | " + (r.distancia * 100f).ToString("F1").PadLeft(6) + " cm | "
                       + (r.choques + "/" + r.muestras + " " + r.partes).PadRight(23) + " | " + (r.suelo * 100f).ToString("F0").PadLeft(3) + "cm | " + r.dedosAbiertos + "/" + r.muestras
                       + (r.enEspalda ? " · en la espalda" : "");
        }
        Debug.Log(reporte);
        Assert.Pass();
    }

    // Recorre todas las animaciones. Lo usa tambien PruebaG_Escoba (sin fotos).
    public IEnumerator Recorrer(bool fotos, Dictionary<string, Resultado> destino = null)
    {
        if (destino != null) tabla = destino;
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1f);
        jugador = GameObject.FindGameObjectWithTag("Player");
        anim = jugador.GetComponent<Animator>();
        // Sin fotos no se renderiza nada y el Animator no moveria los huesos (culling): que anime siempre
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        entradas = jugador.GetComponent("StarterAssetsInputs");
        jugador.SendMessage("TomarAgua");
        jugador.SendMessage("TomarEscoba");
        if (fotos)
        {
            carpeta = Path.Combine(Application.dataPath, "../Capturas/Juego/Auditoria");
            Directory.CreateDirectory(carpeta);
            cam = Camera.main;
            rt = new RenderTexture(960, 540, 24);
            cam.targetTexture = rt;
            GameObject.Find("HUD").GetComponent<Canvas>().enabled = false;
            ((Behaviour)cam.GetComponent("CinemachineBrain")).enabled = false;
        }
        Vector3 inicio = new Vector3(0f, 0.1f, 20f);

        yield return Ir(inicio, 0f);
        yield return Muestrear("quieto", 1.2f, fotos);

        Entrada("move", Vector2.up);
        yield return new WaitForSeconds(0.5f);
        yield return Muestrear("caminar", 1.2f, fotos);
        Entrada("sprint", true);
        yield return new WaitForSeconds(0.6f);
        yield return Muestrear("correr", 1.0f, fotos);
        Entrada("sprint", false);
        Entrada("move", Vector2.zero);

        yield return Ir(inicio, 0f);
        Entrada("jump", true);
        yield return Muestrear("saltar", 1.1f, fotos);

        yield return Ir(inicio, 0.4f);
        jugador.SendMessage("Atacar", new InputAction.CallbackContext());
        yield return Muestrear("pegar", 1.4f, fotos);

        yield return Ir(inicio, 0.4f);
        anim.SetTrigger("golpeado");
        yield return Muestrear("golpeado", 0.9f, fotos);

        // Bostezo (idle al azar): lo que haria ControladorRandomIdle
        // Con la escoba el bostezo esta desactivado (ControladorRandomIdle apagado): solo se audita si sigue activo
        Behaviour randomIdle = (Behaviour)jugador.GetComponent("ControladorRandomIdle");
        if (randomIdle != null && randomIdle.enabled)
        {
            yield return Ir(inicio, 0.4f);
            anim.SetInteger("randomIdle", 1);
            anim.SetBool("estaIdle", true);
            yield return Muestrear("bostezo", 2.5f, fotos);
            anim.SetBool("estaIdle", false);
            anim.SetInteger("randomIdle", 0);
        }
        else desactivados.Add("bostezo");

        // Hablando (con la señora Nelly, en la plaza)
        yield return Ir(new Vector3(-3f, 0.1f, 32f), 0.3f);
        GameObject.Find("NPC_SraNelly").SendMessage("IniciarDialogo");
        yield return Muestrear("hablar", 1.2f, fotos);
        GameObject.Find("Sistemas").SendMessage("TerminarDialogo");
        yield return new WaitForSeconds(0.5f);

        // Colgado del kiosko y subir
        yield return Ir(new Vector3(7.9f, 0.1f, 33.2f), 0.3f);
        jugador.transform.rotation = Quaternion.identity;
        Entrada("move", Palanca(Vector3.forward));
        yield return new WaitForSeconds(0.4f);
        Entrada("jump", true);
        for (float t = 0f; t < 1.5f && !anim.GetBool("Colgado"); t += Time.deltaTime) yield return null;
        Entrada("move", Vector2.zero);
        yield return new WaitForSeconds(0.4f);
        yield return Muestrear("colgado", 0.8f, fotos);
        Entrada("jump", true);
        yield return Muestrear("subir", 1.1f, fotos);
        yield return new WaitForSeconds(0.5f);

        // Morir
        yield return Ir(inicio, 0.4f);
        jugador.SendMessage("CalcularDanio", 999f);
        yield return Muestrear("morir", 1.8f, fotos);
        if (fotos) { cam.targetTexture = null; ((Behaviour)cam.GetComponent("CinemachineBrain")).enabled = true; }
    }

    void Entrada(string campo, object valor) { entradas.GetType().GetField(campo).SetValue(entradas, valor); }

    Vector2 Palanca(Vector3 direccion)
    {
        Vector3 f = Camera.main.transform.forward; f.y = 0f; f.Normalize();
        Vector3 r = Vector3.Cross(Vector3.up, f);
        return new Vector2(Vector3.Dot(direccion, r), Vector3.Dot(direccion, f)).normalized;
    }

    IEnumerator Ir(Vector3 pos, float espera)
    {
        CharacterController cc = jugador.GetComponent<CharacterController>();
        cc.enabled = false;
        jugador.transform.position = pos;
        jugador.transform.rotation = Quaternion.identity;
        cc.enabled = true;
        yield return new WaitForSeconds(espera + 0.3f);
    }

    IEnumerator Muestrear(string estado, float duracion, bool fotos)
    {
        Resultado r = new Resultado();
        int foto = 0;
        float proximaFoto = 0f;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            yield return null;
            Medir(r);
            if (fotos && t >= proximaFoto && foto < 4)
            {
                Foto(estado + "_" + foto + "_lado", jugador.transform.position + jugador.transform.right * 2.6f + Vector3.up * 1.3f + jugador.transform.forward * 0.4f, jugador.transform.position + Vector3.up * 1f);
                Transform mano = anim.GetBoneTransform(HumanBodyBones.RightHand);
                Foto(estado + "_" + foto + "_mano", mano.position + jugador.transform.right * 0.55f + jugador.transform.forward * 0.25f + Vector3.up * 0.15f, mano.position);
                foto++;
                proximaFoto += duracion / 4f;
            }
        }
        tabla[estado] = r;
    }

    // Mide un cuadro
    void Medir(Resultado r)
    {
        GameObject visual = GameObject.Find("Palo_Escoba_Visual");
        if (visual == null) return;
        r.muestras++;
        Transform escoba = visual.transform;
        Vector3 a = escoba.position, dir = escoba.forward; // extremo del mango -> cerdas
        float largo = 1.17f; // la escoba mide 1,25 m y el mango asoma 8 cm bajo el puño

        Transform pelvis = anim.GetBoneTransform(HumanBodyBones.Hips);
        Transform pecho = anim.GetBoneTransform(HumanBodyBones.UpperChest) ?? anim.GetBoneTransform(HumanBodyBones.Chest);
        r.enEspalda = escoba.parent != null && pecho != null && escoba.parent == pecho;

        // Eje y centro del puño
        Transform indice = anim.GetBoneTransform(HumanBodyBones.RightIndexProximal);
        Transform menique = anim.GetBoneTransform(HumanBodyBones.RightLittleProximal);
        Vector3 centro = Vector3.zero; int n = 0;
        foreach (HumanBodyBones h in new[] { HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightLittleIntermediate })
        {
            Transform t = anim.GetBoneTransform(h);
            if (t != null) { centro += t.position; n++; }
        }
        if (!r.enEspalda && indice != null && menique != null && n > 0)
        {
            centro /= n;
            Vector3 eje = (indice.position - menique.position).normalized;
            r.angulo = Mathf.Max(r.angulo, Vector3.Angle(eje, dir));
            Vector3 alCentro = centro - a;
            float dist = (alCentro - Vector3.Project(alCentro, dir)).magnitude;
            r.distancia = Mathf.Max(r.distancia, dist);
            // dedos: el indice doblado (angulo entre la palma y la falange) > 50°
            Transform medio = anim.GetBoneTransform(HumanBodyBones.RightIndexIntermediate);
            Transform mano = anim.GetBoneTransform(HumanBodyBones.RightHand);
            if (medio != null && mano != null && Vector3.Angle(indice.position - mano.position, medio.position - indice.position) < 40f) r.dedosAbiertos++;
        }

        // Choques con el cuerpo: puntos a lo largo de la escoba (sin los 25 cm que van en la mano)
        bool choco = false;
        for (float d = 0.25f; d <= largo; d += 0.05f)
        {
            Vector3 p = a + dir * d;
            string parte = Parte(p);
            if (parte != null)
            {
                choco = true;
                if (!r.partes.Contains(parte)) r.partes += (r.partes == "" ? "" : ",") + parte;
            }
        }
        if (choco) r.choques++;
        // Cerdas bajo el suelo
        // Suelo real bajo Benja (en el aire los pies no son el suelo)
        RaycastHit bajo;
        float pie = Physics.Raycast(jugador.transform.position + Vector3.up * 0.5f, Vector3.down, out bajo, 5f, ~0, QueryTriggerInteraction.Ignore) ? bajo.point.y : jugador.transform.position.y;
        float puntaY = (a + dir * largo).y;
        r.suelo = Mathf.Max(r.suelo, pie - puntaY);
    }

    string Parte(Vector3 p)
    {
        Transform cabeza = anim.GetBoneTransform(HumanBodyBones.Head);
        if (cabeza != null && Vector3.Distance(p, cabeza.position + Vector3.up * 0.1f) < 0.15f) return "cabeza";
        if (Capsula(p, HumanBodyBones.Hips, HumanBodyBones.Neck, 0.15f)) return "torso";
        if (Capsula(p, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, 0.08f) || Capsula(p, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, 0.06f)) return "pierna izq";
        if (Capsula(p, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, 0.08f) || Capsula(p, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, 0.06f)) return "pierna der";
        if (Capsula(p, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, 0.05f) || Capsula(p, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, 0.045f)) return "brazo izq";
        if (Capsula(p, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, 0.05f)) return "brazo der";
        return null;
    }

    bool Capsula(Vector3 p, HumanBodyBones h0, HumanBodyBones h1, float radio)
    {
        Transform t0 = anim.GetBoneTransform(h0), t1 = anim.GetBoneTransform(h1);
        if (t0 == null || t1 == null) return false;
        Vector3 ab = t1.position - t0.position;
        float k = Mathf.Clamp01(Vector3.Dot(p - t0.position, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
        return Vector3.Distance(p, t0.position + ab * k) < radio;
    }

    void Foto(string nombre, Vector3 desde, Vector3 hacia)
    {
        cam.transform.position = desde;
        cam.transform.LookAt(hacia);
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(carpeta, nombre + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null;
        Object.Destroy(tex);
    }
}
