using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Animaciones y transiciones de Benja (se le escribe directo a sus entradas, como un control):
//  - Con caña camina a la misma velocidad que su animacion (no patina) y NO puede saltar
//  - Al colgarse, las manos quedan en el borde; al subir no hace "saltito" ni salta solo despues
public class PruebaG_Animaciones
{
    GameObject jugador;
    Component entradas;
    Animator anim;

    [SetUp]
    public void MarcarPruebas()
    {
        if (GameObject.Find("PruebasAutomaticas") == null)
            Object.DontDestroyOnLoad(new GameObject("PruebasAutomaticas"));
    }

    IEnumerator Cargar()
    {
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1f);
        jugador = GameObject.FindGameObjectWithTag("Player");
        entradas = jugador.GetComponent("StarterAssetsInputs");
        anim = jugador.GetComponent<Animator>();
    }

    void Entrada(string campo, object valor) { entradas.GetType().GetField(campo).SetValue(entradas, valor); }

    // Palanca hacia una direccion del mundo (segun hacia donde mira la camara)
    Vector2 Palanca(Vector3 direccion)
    {
        Vector3 f = Camera.main.transform.forward; f.y = 0f; f.Normalize();
        Vector3 r = Vector3.Cross(Vector3.up, f);
        return new Vector2(Vector3.Dot(direccion, r), Vector3.Dot(direccion, f)).normalized;
    }

    IEnumerator Ir(Vector3 pos)
    {
        CharacterController cc = jugador.GetComponent<CharacterController>();
        cc.enabled = false;
        jugador.transform.position = pos;
        cc.enabled = true;
        yield return new WaitForSeconds(0.5f);
    }

    string Estado()
    {
        foreach (string n in new[] { "Curado", "Idle Walk Run Blend", "JumpStart", "InAir", "JumpLand", "Colgado", "SubirBorde", "Ataque01", "HitReaction" })
            if (anim.GetCurrentAnimatorStateInfo(0).IsName(n)) return n;
        return "?";
    }

    [UnityTest]
    public IEnumerator ConCana_CaminaComoSuAnimacion_YNoSalta()
    {
        yield return Cargar();
        yield return Ir(new Vector3(0f, 0.1f, 4f));
        Entrada("move", Palanca(Vector3.forward));
        yield return new WaitForSeconds(0.8f); // acelera
        Vector3 p0 = jugador.transform.position;
        yield return new WaitForSeconds(1.5f);
        float velocidad = Vector3.Distance(p0, jugador.transform.position) / 1.5f;
        Assert.AreEqual("Curado", Estado(), "Con caña deberia usar la animacion de curado");
        Debug.Log("[ANIM] con caña avanza " + velocidad.ToString("F2") + " m/s");
        Assert.AreEqual(1.0f, velocidad, 0.15f, "Con caña deberia avanzar a 1 m/s (lo que avanza su animacion)");

        // Con caña NO se salta: el boton se borra y Benja sigue caminando curado
        float y0 = jugador.transform.position.y;
        Entrada("jump", true);
        bool salto = false;
        float yMax = y0;
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            string e = Estado();
            if (e == "JumpStart" || e == "InAir") salto = true;
            yMax = Mathf.Max(yMax, jugador.transform.position.y);
            yield return null;
        }
        Entrada("move", Vector2.zero);
        Debug.Log("[ANIM] con caña al apretar saltar: subio " + (yMax - y0).ToString("F2") + " m");
        Assert.IsFalse(salto, "Con caña no deberia saltar");
        Assert.Less(yMax - y0, 0.05f, "Con caña se despego del suelo");
        Assert.AreEqual(false, entradas.GetType().GetField("jump").GetValue(entradas), "El boton de saltar quedo guardado");
    }

    [UnityTest]
    public IEnumerator Colgarse_Y_Subir_SinSaltito()
    {
        yield return Cargar();
        jugador.SendMessage("TomarAgua");
        // Frente al kiosko de la plaza (techo a ~2,5 m), mirando hacia el (+z)
        GameObject techoKiosko = GameObject.Find("Kiosko_Techo");
        float borde = techoKiosko.GetComponent<Collider>().bounds.max.y;
        yield return Ir(new Vector3(7.9f, 0.1f, 33.2f));
        Entrada("move", Palanca(Vector3.forward));
        yield return new WaitForSeconds(0.4f);
        Entrada("jump", true);
        bool colgo = false;
        for (float t = 0f; t < 1.5f && !colgo; t += Time.deltaTime) { colgo = anim.GetBool("Colgado"); yield return null; }
        Entrada("move", Vector2.zero);
        Assert.IsTrue(colgo, "No se colgo del techo del kiosko");
        yield return new WaitForSeconds(0.6f);

        // Las manos quedan en el borde
        float manos = (anim.GetBoneTransform(HumanBodyBones.LeftHand).position.y + anim.GetBoneTransform(HumanBodyBones.RightHand).position.y) / 2f;
        Debug.Log("[ANIM] colgado: manos a " + manos.ToString("F2") + " (borde " + borde.ToString("F2") + "), estado " + Estado());
        Assert.AreEqual("Colgado", Estado());
        Assert.AreEqual(borde, manos, 0.25f, "Las manos no quedan en el borde");

        // Subir: el cuerpo y el personaje suben juntos (sin quedarse abajo y saltar al final)
        float yColgado = jugador.transform.position.y;
        Entrada("jump", true);
        yield return new WaitForSeconds(0.55f);
        float yMitad = jugador.transform.position.y;
        Entrada("jump", true); // Espacio a mitad de la subida: no debe saltar despues
        yield return new WaitForSeconds(0.9f);
        Debug.Log("[ANIM] subiendo: colgado y=" + yColgado.ToString("F2") + " · a la mitad y=" + yMitad.ToString("F2") + " · final y=" + jugador.transform.position.y.ToString("F2"));
        Assert.Greater(yMitad, yColgado + 0.3f, "A mitad de la subida el personaje no habia subido (salta al final)");
        Assert.AreEqual(borde, jugador.transform.position.y, 0.15f, "No quedo parado arriba del kiosko");

        // Despues de subir: parado, sin saltito ni aterrizaje, y sin salto guardado
        float yMin = 99f, yMax = -99f;
        bool aire = false;
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            float y = jugador.transform.position.y;
            yMin = Mathf.Min(yMin, y); yMax = Mathf.Max(yMax, y);
            string e = Estado();
            if (e == "JumpStart" || e == "InAir" || e == "JumpLand") aire = true;
            yield return null;
        }
        Debug.Log("[ANIM] despues de subir: y entre " + yMin.ToString("F2") + " y " + yMax.ToString("F2") + ", estado " + Estado());
        Assert.IsFalse(aire, "Despues de subir paso por salto/caida/aterrizaje");
        Assert.Less(yMax - yMin, 0.1f, "Despues de subir dio un saltito");
    }
}
