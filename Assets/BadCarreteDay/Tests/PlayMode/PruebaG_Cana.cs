using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// La caña: hasta tomar agua Benja solo camina y habla. No pega, Don Lucho no lo pesca
// (sigue meando, sin darse vuelta) y la vista esta borrosa. Con el agua todo vuelve.
// Basureros: se revisan una sola vez.
public class PruebaG_Cana
{
    GameObject jugador, sistemas;

    [SetUp]
    public void MarcarPruebas()
    {
        if (GameObject.Find("PruebasAutomaticas") == null)
            Object.DontDestroyOnLoad(new GameObject("PruebasAutomaticas"));
    }

    string Texto(string nombre) { return GameObject.Find(nombre).GetComponent<TMP_Text>().text; }

    bool IntentarAtacar()
    {
        Component ataque = jugador.GetComponent("AtaqueJugador");
        return (bool)ataque.GetType().GetMethod("IntentarAtacar").Invoke(ataque, null);
    }

    float PesoVista()
    {
        Component vol = GameObject.Find("PostProceso_Cana").GetComponent("Volume");
        return (float)vol.GetType().GetField("weight").GetValue(vol);
    }

    [UnityTest]
    public IEnumerator ConCana_SoloCaminaYHabla_ConAguaVuelveTodo()
    {
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1f);
        jugador = GameObject.FindGameObjectWithTag("Player");
        sistemas = GameObject.Find("Sistemas");

        // Con caña: no pega y ve borroso
        Assert.IsFalse(IntentarAtacar(), "Con caña no deberia poder pegar");
        Assert.Greater(PesoVista(), 0.6f, "Con caña deberia ver borroso");

        // Don Lucho: no se da vuelta y no se le entiende a Benja
        GameObject lucho = GameObject.Find("NPC_DonLucho");
        Quaternion giroLucho = lucho.transform.rotation;
        lucho.SendMessage("IniciarDialogo");
        yield return null;
        Assert.IsNotNull(GameObject.Find("PanelDialogo"), "Don Lucho no contesto");
        Assert.AreEqual("Benja", Texto("TextoNombre"), "La primera linea deberia ser Benja balbuceando");
        yield return new WaitForSeconds(1.2f);
        Assert.Less(Quaternion.Angle(giroLucho, lucho.transform.rotation), 5f, "Don Lucho se dio vuelta con caña");
        sistemas.SendMessage("TerminarDialogo");
        yield return null;
        StringAssert.Contains("agua", Texto("TextoMision"));

        // Un basurero da su plata una sola vez
        GameObject basurero = null;
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name == "Basurero_$300") basurero = t.gameObject;
        Assert.IsNotNull(basurero, "No esta el basurero de $300");
        basurero.SendMessage("Revisar");
        basurero.SendMessage("Revisar");
        yield return null;
        Assert.AreEqual("$300", Texto("TextoDinero"), "El basurero dio plata dos veces");

        // Con agua: pega, salta y se le aclara la vista
        jugador.SendMessage("TomarAgua");
        yield return new WaitForSeconds(2.5f);
        Assert.Less(PesoVista(), 0.01f, "La vista no se aclaro con el agua");
        float y0 = jugador.transform.position.y;
        Component entradas = jugador.GetComponent("StarterAssetsInputs");
        entradas.GetType().GetField("jump").SetValue(entradas, true);
        float yMax = y0;
        for (float t = 0f; t < 0.6f; t += Time.deltaTime) { yMax = Mathf.Max(yMax, jugador.transform.position.y); yield return null; }
        Assert.Greater(yMax - y0, 0.5f, "Con agua deberia poder saltar");
        yield return new WaitForSeconds(1f);
        Assert.IsTrue(IntentarAtacar(), "Con agua deberia poder pegar");
    }
}
