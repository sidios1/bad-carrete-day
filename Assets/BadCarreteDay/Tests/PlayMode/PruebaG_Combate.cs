using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Golpes con el palo de escoba:
//  - Un golpe le pega a TODOS los cumas que esten al frente
//  - Si cortan el golpe, el arma no queda prendida (el siguiente golpe pega)
//  - Apunta solo a un cuma que esta al lado
//  - No se puede pegar con el control bloqueado (dialogo)
public class PruebaG_Combate
{
    GameObject jugador;

    [SetUp]
    public void MarcarPruebas()
    {
        if (GameObject.Find("PruebasAutomaticas") == null)
            Object.DontDestroyOnLoad(new GameObject("PruebasAutomaticas"));
    }

    IEnumerator Preparar()
    {
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1f);
        jugador = GameObject.FindGameObjectWithTag("Player");
        jugador.SendMessage("TomarAgua");
        // La señora Gladys le pasa el palo de escoba
        yield return Ir(new Vector3(0f, 16.1f, 117f));
        GameObject.Find("NPC_SraGladys").SendMessage("IniciarDialogo");
        yield return null;
        GameObject.Find("Sistemas").SendMessage("TerminarDialogo");
        yield return null;
        // Benja en el pasaje oscuro mirando hacia +z (los cumas quedan quietos: no se activan)
        yield return Ir(new Vector3(0f, 16.1f, 118.8f));
        jugador.transform.rotation = Quaternion.identity;
    }

    IEnumerator Ir(Vector3 pos)
    {
        CharacterController cc = jugador.GetComponent<CharacterController>();
        cc.enabled = false;
        jugador.transform.position = pos;
        cc.enabled = true;
        yield return new WaitForSeconds(0.3f);
    }

    void Poner(string nombre, Vector3 pos)
    {
        GameObject c = GameObject.Find(nombre);
        UnityEngine.AI.NavMeshAgent ag = c.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (ag != null) ag.Warp(pos); else c.transform.position = pos;
    }

    float Salud(string nombre)
    {
        Component stats = GameObject.Find(nombre).GetComponent("DemoControladorEstadisticas");
        object actuales = stats.GetType().GetField("estadisticasActuales").GetValue(stats);
        return (float)actuales.GetType().GetField("salud").GetValue(actuales);
    }

    bool ArmaActiva()
    {
        Component arma = GameObject.Find("Palo_Escoba_Arma").GetComponent("DemoArma");
        return (bool)arma.GetType().GetField("danioActivo").GetValue(arma);
    }

    void Atacar() { jugador.SendMessage("Atacar", new InputAction.CallbackContext()); }

    // Sin la escoba (es un secreto) igual se pelea: a puño limpio pega menos
    [UnityTest]
    public IEnumerator SinEscoba_PegaAPunoLimpio()
    {
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1f);
        jugador = GameObject.FindGameObjectWithTag("Player");
        jugador.SendMessage("TomarAgua");
        yield return Ir(new Vector3(0f, 16.1f, 118.8f));
        jugador.transform.rotation = Quaternion.identity;
        Assert.IsNull(GameObject.Find("Palo_Escoba_Visual"), "Sin hablar con Gladys no deberia tener la escoba");
        GameObject.Find("Cuma_David").SetActive(false);
        GameObject.Find("Cuma_James_Rojo").SetActive(false);
        Poner("Cuma_James", new Vector3(0f, 16f, 120f));
        yield return new WaitForSeconds(0.2f);
        float antes = Salud("Cuma_James");
        Atacar();
        yield return new WaitForSeconds(1.5f);
        Debug.Log("[COMBATE] a puño: James " + antes + "->" + Salud("Cuma_James"));
        Assert.AreEqual(antes - 10f, Salud("Cuma_James"), 0.01f, "A puño limpio deberia quitar 10");
    }

    [UnityTest]
    public IEnumerator UnGolpe_LePegaADosCumas()
    {
        yield return Preparar();
        GameObject.Find("Cuma_James_Rojo").SetActive(false);
        Poner("Cuma_James", new Vector3(-0.45f, 16f, 119.9f));
        Poner("Cuma_David", new Vector3(0.45f, 16f, 120.0f));
        yield return new WaitForSeconds(0.2f);
        float a0 = Salud("Cuma_James"), b0 = Salud("Cuma_David");
        Atacar();
        yield return new WaitForSeconds(1.5f);
        Debug.Log("[COMBATE] James " + a0 + "->" + Salud("Cuma_James") + " · David " + b0 + "->" + Salud("Cuma_David"));
        Assert.Less(Salud("Cuma_James"), a0, "El golpe no le pego a James");
        Assert.Less(Salud("Cuma_David"), b0, "El golpe no le pego a David (solo pegaba a uno)");
    }

    [UnityTest]
    public IEnumerator GolpeCortado_NoDejaElArmaPrendida()
    {
        yield return Preparar();
        GameObject.Find("Cuma_David").SetActive(false);
        GameObject.Find("Cuma_James_Rojo").SetActive(false);
        Poner("Cuma_James", new Vector3(0f, 16f, 120f));
        yield return new WaitForSeconds(0.2f);
        Animator anim = jugador.GetComponent<Animator>();

        // Golpe 1, cortado justo cuando el arma esta prendida (como si lo golpearan con otra animacion)
        Atacar();
        yield return new WaitForSeconds(0.6f);
        anim.SetTrigger("golpeado");
        yield return new WaitForSeconds(0.6f);
        Assert.IsFalse(ArmaActiva(), "Despues de cortar el golpe, el arma quedo prendida");

        // Golpe 2: pega
        yield return new WaitForSeconds(0.8f);
        float antes = Salud("Cuma_James");
        Atacar();
        yield return new WaitForSeconds(1.5f);
        Assert.Less(Salud("Cuma_James"), antes, "El golpe siguiente no pego");
    }

    [UnityTest]
    public IEnumerator ApuntaSolo_AUnCumaAlCostado()
    {
        yield return Preparar();
        GameObject.Find("Cuma_David").SetActive(false);
        GameObject.Find("Cuma_James_Rojo").SetActive(false);
        Poner("Cuma_James", new Vector3(1.3f, 16f, 118.8f)); // a la derecha (90 grados)
        yield return new WaitForSeconds(0.2f);
        float antes = Salud("Cuma_James");
        Atacar();
        yield return new WaitForSeconds(1.5f);
        Assert.Less(Salud("Cuma_James"), antes, "No le pego al cuma que estaba al lado");
    }

    [UnityTest]
    public IEnumerator ConElControlBloqueado_NoSePega()
    {
        yield return Preparar();
        GameObject.Find("Sistemas").SendMessage("BloquearJugador", true);
        yield return null;
        Atacar();
        yield return new WaitForSeconds(0.4f);
        Assert.IsFalse(jugador.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Ataque01"), "Pego con el control bloqueado (dialogo)");
        GameObject.Find("Sistemas").SendMessage("BloquearJugador", false);
    }
}
