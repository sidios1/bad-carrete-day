using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Pruebas automaticas de la escena Prueba_F2 (Window > General > Test Runner > PlayMode).
// Recorren la escena "como un jugador" y revisan que los UnityEvents esten bien conectados.
// (Este ensamblado de pruebas no puede ver los scripts del juego directamente,
//  por eso se usa SendMessage y se leen los textos del HUD.)
public class PruebaF2
{
    GameObject jugador;
    GameObject sistemas;

    IEnumerator CargarEscena()
    {
        SceneManager.LoadScene("Prueba_F2");
        yield return new WaitForSeconds(0.5f);
        jugador = GameObject.FindGameObjectWithTag("Player");
        sistemas = GameObject.Find("Sistemas");
        Assert.IsNotNull(jugador, "No encontre al jugador");
        Assert.IsNotNull(sistemas, "No encontre Sistemas");
    }

    string Texto(string nombre)
    {
        return GameObject.Find(nombre).GetComponent<TMP_Text>().text;
    }

    // Mueve al jugador (hay que apagar el CharacterController para moverlo a mano)
    IEnumerator Teletransportar(Vector3 posicion)
    {
        CharacterController cc = jugador.GetComponent<CharacterController>();
        cc.enabled = false;
        jugador.transform.position = posicion;
        cc.enabled = true;
        yield return new WaitForSeconds(0.3f);
    }

    [UnityTest]
    public IEnumerator HUD_Dialogo_Lucas_Pegatina_y_Victoria()
    {
        yield return CargarEscena();

        // HUD inicial
        Assert.AreEqual("$0", Texto("TextoDinero"));
        Assert.AreEqual("100", Texto("TextoSalud"));
        StringAssert.Contains("Habla", Texto("TextoMision"));

        // Dialogo con el viejo
        GameObject.Find("NPC_ViejoCurado").SendMessage("IniciarDialogo");
        yield return new WaitForSeconds(0.2f);
        Assert.IsTrue(GameObject.Find("PanelDialogo") != null, "No se abrio el dialogo");
        Assert.AreEqual("Viejo curado", Texto("TextoNombre"));
        sistemas.SendMessage("TerminarDialogo");
        yield return null;
        StringAssert.Contains("lucas", Texto("TextoMision"));

        // Juntar las 3 lucas pasando por encima
        Vector3[] monedas = { new Vector3(-3f, 0.2f, 0f), new Vector3(-5f, 0.2f, 4f), new Vector3(6f, 3.2f, 1f) };
        foreach (Vector3 m in monedas)
            yield return Teletransportar(m);
        Assert.AreEqual("$300", Texto("TextoDinero"));
        StringAssert.Contains("pegatina", Texto("TextoMision"));

        // Pararse en la pegatina muestra el prompt
        yield return Teletransportar(new Vector3(-8f, 0.1f, -2f));
        Assert.IsTrue(GameObject.Find("PanelPrompt") != null, "No aparecio el prompt");
        StringAssert.Contains("palanca", Texto("TextoPrompt"));

        // Interactuar (equivale a apretar E estando encima)
        GameObject.Find("Pegatina_AQUI").SendMessage("Interactuar", new UnityEngine.InputSystem.InputAction.CallbackContext());
        yield return null;
        Assert.IsTrue(GameObject.Find("Plataforma_Aparece") != null, "No aparecio la plataforma");

        // Mision completa -> Victoria -> escena Fin con el panel de victoria
        yield return new WaitForSeconds(4.5f);
        Assert.AreEqual("Fin", SceneManager.GetActiveScene().name);
        Assert.IsNotNull(GameObject.Find("PanelVictoria"));
        Assert.IsNull(GameObject.Find("PanelDerrota"));
    }

    [UnityTest]
    public IEnumerator Caida_Trampa_Curacion_y_GameOver()
    {
        yield return CargarEscena();

        // Caer al vacio: -25 de salud y vuelve al checkpoint
        yield return Teletransportar(new Vector3(20f, -8f, 20f));
        yield return new WaitForSeconds(1.5f);
        Assert.AreEqual("75", Texto("TextoSalud"));
        Assert.Less(Vector3.Distance(jugador.transform.position, new Vector3(0f, 0.1f, -4f)), 2f, "No volvio al checkpoint");

        // Trampa: -34
        yield return Teletransportar(new Vector3(3f, 0.2f, -8f));
        Assert.AreEqual("41", Texto("TextoSalud"));

        // Agua mineral: +25
        yield return Teletransportar(new Vector3(0f, 0.2f, -10f));
        Assert.AreEqual("66", Texto("TextoSalud"));

        // Daño mortal -> Game Over -> escena Fin con el panel de derrota
        jugador.SendMessage("CalcularDanio", 500f);
        yield return new WaitForSeconds(4.5f);
        Assert.AreEqual("Fin", SceneManager.GetActiveScene().name);
        Assert.IsNotNull(GameObject.Find("PanelDerrota"));
        Assert.IsNull(GameObject.Find("PanelVictoria"));
    }
}
