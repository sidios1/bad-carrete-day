using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Nada esta bloqueado: si Benja llega a un evento con lo que le piden, el evento se lo salta.
// Ej: compra el vino antes de hablar con Don Lucho -> una sola conversacion cumple los tres pasos.
public class PruebaG_Atajos
{
    GameObject jugador, sistemas;

    [SetUp]
    public void MarcarPruebas()
    {
        if (GameObject.Find("PruebasAutomaticas") == null)
            Object.DontDestroyOnLoad(new GameObject("PruebasAutomaticas"));
    }

    string Texto(string nombre) { return GameObject.Find(nombre).GetComponent<TMP_Text>().text; }

    IEnumerator Hablar(string npc)
    {
        GameObject.Find(npc).SendMessage("IniciarDialogo");
        for (float t = 0f; t < 5f && GameObject.Find("PanelDialogo") == null; t += Time.deltaTime)
            yield return null;
        Assert.IsNotNull(GameObject.Find("PanelDialogo"), "No empezo la conversacion con " + npc);
        sistemas.SendMessage("TerminarDialogo");
        yield return null;
    }

    [UnityTest]
    public IEnumerator VinoAntesDeHablar_DonLuchoSeLoSalta()
    {
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1f);
        jugador = GameObject.FindGameObjectWithTag("Player");
        sistemas = GameObject.Find("Sistemas");

        // Todo a la venta desde el inicio (vino y completo incluidos)
        sistemas.SendMessage("Sumar", 5000);
        GameObject.Find("Producto_Vino").SendMessage("Comprar");
        GameObject.Find("Producto_Completo").SendMessage("Comprar");
        GameObject.Find("Producto_Agua").SendMessage("Comprar");
        yield return null;
        Assert.AreEqual("$0", Texto("TextoDinero"), "No se pudo comprar todo de una");
        Assert.IsNotNull(GameObject.Find("Pegatina_Completo"), "Con el completo comprado deberia poder dejarlo en el plato de Toby");

        // Don Lucho: una conversacion y quedan cumplidos hablar + vino + llevar vino
        yield return Hablar("NPC_DonLucho");
        Debug.Log("[ATAJOS] mision despues de Don Lucho: " + Texto("TextoMision"));
        StringAssert.Contains("Toby", Texto("TextoMision"), "Deberia quedar el siguiente paso pendiente: darle el completo al Toby");
    }

    // F2..F7: cada atajo deja la mision y el mundo en su punto
    [UnityTest]
    public IEnumerator AtajosDePrueba_DejanCadaPuntoListo()
    {
        string[] esperado = { "", "", "meando", "Toby", "escalera rota", "ascensor", "cumas", "Julio" };
        string fallas = "";
        for (int punto = 2; punto <= 7; punto++)
        {
            SceneManager.LoadScene("Nivel1_Cerro");
            yield return new WaitForSeconds(1f);
            GameObject.Find("Sistemas").SendMessage("IrA", punto);
            yield return new WaitForSeconds(2.5f);
            jugador = GameObject.FindGameObjectWithTag("Player");
            string mision = Texto("TextoMision");
            Debug.Log("[ATAJOS] F" + punto + ": " + mision + " · Benja en " + jugador.transform.position.ToString("F1"));
            if (!mision.Contains(esperado[punto])) fallas += "\n - F" + punto + ": mision '" + mision + "' (esperaba '" + esperado[punto] + "')";
            if (punto >= 4 && GameObject.Find("Barrera_Toby") != null) fallas += "\n - F" + punto + ": la escalera del Toby sigue cerrada";
            if (punto >= 6 && GameObject.Find("Palo_Escoba_Visual") == null) fallas += "\n - F" + punto + ": sin escoba";
            if (punto == 7 && GameObject.Find("Reja_Pasaje") != null) fallas += "\n - F7: la reja del pasaje sigue cerrada";
        }
        Assert.IsEmpty(fallas, "Atajos con problemas:" + fallas);
    }

    // En el menu de pausa: boton "4. Toby comiendo" -> despausa y deja la escalera abierta
    [UnityTest]
    public IEnumerator BotonDelMenuDePausa_LlevaAlPunto()
    {
        SceneManager.LoadScene("Nivel1_Cerro");
        yield return new WaitForSeconds(1f);
        GameObject.Find("Sistemas").SendMessage("Pausar");
        yield return null;
        Assert.AreEqual(0f, Time.timeScale, "No se pauso");
        GameObject boton = GameObject.Find("BotonAtajo_4");
        Assert.IsNotNull(boton, "No esta el boton de atajos en el menu de pausa");
        Component b = boton.GetComponent("Button");
        object alClic = b.GetType().GetProperty("onClick").GetValue(b);
        alClic.GetType().GetMethod("Invoke").Invoke(alClic, null);
        yield return new WaitForSeconds(2.5f);
        Assert.AreEqual(1f, Time.timeScale, "Quedo en pausa");
        Assert.IsNull(GameObject.Find("Barrera_Toby"), "La escalera del Toby sigue cerrada");
        StringAssert.Contains("escalera rota", Texto("TextoMision"));
    }
}
