using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Recorre el flujo de pantallas: Menu -> Intro (viñetas) -> Nivel 1, y Fin -> Menu.
public class PruebaF6
{
    string Texto(string nombre) { return GameObject.Find(nombre).GetComponent<TMP_Text>().text; }

    [UnityTest]
    public IEnumerator Menu_Intro_Nivel()
    {
        SceneManager.LoadScene("MenuInicio");
        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual("BAD CARRETE DAY", Texto("Titulo"));
        Assert.IsNull(GameObject.Find("PanelCreditos"), "Los creditos deberian partir ocultos");

        // Jugar -> pantalla de carga -> Intro
        GameObject.Find("Sistemas").SendMessage("Jugar");
        yield return new WaitForSeconds(0.1f);
        Assert.IsNotNull(GameObject.Find("PantallaCarga"), "No aparecio la pantalla de carga");
        yield return new WaitForSeconds(3f);
        Assert.AreEqual("Intro", SceneManager.GetActiveScene().name);

        // 4 viñetas del guion
        GameObject sistemas = GameObject.Find("Sistemas");
        StringAssert.Contains("Concepción", Texto("TextoEncabezado"));
        for (int i = 0; i < 3; i++) { sistemas.SendMessage("Siguiente"); yield return null; }
        StringAssert.Contains("Valparaíso", Texto("TextoEncabezado"));
        StringAssert.Contains("CELULAR", Texto("TextoVineta"));
        Assert.IsNotNull(GameObject.Find("Imagen"), "La ultima viñeta deberia tener imagen");

        // Al terminar -> Nivel 1
        sistemas.SendMessage("Siguiente");
        yield return new WaitForSeconds(4f);
        Assert.AreEqual("Nivel1_Cerro", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator Configuracion_VolumenYSensibilidad()
    {
        SceneManager.LoadScene("MenuInicio");
        yield return new WaitForSeconds(0.5f);
        Assert.IsNull(GameObject.Find("PanelConfiguracion"), "La configuracion deberia partir oculta");
        GameObject.Find("BotonConfiguracion").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        yield return null;
        Assert.IsNotNull(GameObject.Find("PanelConfiguracion"), "No se abrio la configuracion");

        GameObject.Find("SliderVolumen").GetComponent<UnityEngine.UI.Slider>().value = 0.3f;
        GameObject.Find("SliderSensibilidad").GetComponent<UnityEngine.UI.Slider>().value = 2f;
        yield return null;
        Assert.AreEqual(0.3f, AudioListener.volume, 0.01f);
        StringAssert.Contains("30%", Texto("TextoVolumen"));
        StringAssert.Contains("2.0", Texto("TextoSensibilidad").Replace(",", "."));
        // Quedan guardados
        Assert.AreEqual(0.3f, PlayerPrefs.GetFloat("volumen"), 0.01f);

        // Dejar los valores normales para no afectar otras pruebas ni al jugador
        GameObject.Find("SliderVolumen").GetComponent<UnityEngine.UI.Slider>().value = 0.8f;
        GameObject.Find("SliderSensibilidad").GetComponent<UnityEngine.UI.Slider>().value = 1f;
        yield return null;
    }

    [UnityTest]
    public IEnumerator Fin_Derrota_VuelveAlMenu()
    {
        // Prueba_F2 tiene 1 vida: morir -> Fin (derrota)
        SceneManager.LoadScene("Prueba_F2");
        yield return new WaitForSeconds(0.5f);
        GameObject.FindGameObjectWithTag("Player").SendMessage("CalcularDanio", 500f);
        yield return new WaitForSeconds(5f);
        Assert.AreEqual("Fin", SceneManager.GetActiveScene().name);
        Assert.IsNotNull(GameObject.Find("PanelDerrota"));
        Assert.AreNotEqual("...", Texto("FraseDerrota"), "Falta la frase al azar");

        GameObject.Find("Sistemas").SendMessage("IrAlMenu");
        yield return new WaitForSeconds(4f);
        Assert.AreEqual("MenuInicio", SceneManager.GetActiveScene().name);
    }
}
