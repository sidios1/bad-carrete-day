using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// La escoba se ve derecha y logica en TODAS las animaciones (usa el recorrido de la auditoria):
// dentro del puño, alineada con el, sin atravesar el cuerpo ni el suelo, con los dedos cerrados,
// y en la espalda colgado / subiendo / conversando.
public class PruebaG_Escoba
{
    [SetUp]
    public void MarcarPruebas()
    {
        if (GameObject.Find("PruebasAutomaticas") == null)
            Object.DontDestroyOnLoad(new GameObject("PruebasAutomaticas"));
    }

    [UnityTest]
    public IEnumerator Escoba_DerechaYSinAtravesarNadaEnTodasLasAnimaciones()
    {
        PruebaG_AuditoriaEscoba auditoria = new PruebaG_AuditoriaEscoba();
        Dictionary<string, PruebaG_AuditoriaEscoba.Resultado> tabla = new Dictionary<string, PruebaG_AuditoriaEscoba.Resultado>();
        yield return auditoria.Recorrer(false, tabla);
        string fallas = "";
        foreach (string e in PruebaG_AuditoriaEscoba.Estados)
        {
            if (auditoria.desactivados.Contains(e)) continue;
            if (!tabla.ContainsKey(e)) { fallas += "\n - " + e + ": sin datos"; continue; }
            PruebaG_AuditoriaEscoba.Resultado r = tabla[e];
            Debug.Log("[ESCOBA] " + e + ": " + r.angulo.ToString("F0") + "° · " + (r.distancia * 100f).ToString("F1") + " cm · choques " + r.choques + "/" + r.muestras + " · suelo " + (r.suelo * 100f).ToString("F0") + " cm" + (r.enEspalda ? " · espalda" : ""));
            if (r.angulo > 10f) fallas += "\n - " + e + ": torcida " + r.angulo.ToString("F0") + "° respecto del puño";
            if (r.distancia > 0.03f) fallas += "\n - " + e + ": el mango pasa a " + (r.distancia * 100f).ToString("F1") + " cm del puño";
            if (r.choques > 0) fallas += "\n - " + e + ": atraviesa " + r.partes + " (" + r.choques + " cuadros)";
            if (r.suelo > 0.03f) fallas += "\n - " + e + ": se entierra " + (r.suelo * 100f).ToString("F0") + " cm";
            if (r.dedosAbiertos > 0 && !r.enEspalda) fallas += "\n - " + e + ": dedos abiertos (" + r.dedosAbiertos + " cuadros)";
            if ((e == "colgado" || e == "subir" || e == "hablar") && !r.enEspalda) fallas += "\n - " + e + ": deberia ir en la espalda";
        }
        Assert.IsEmpty(fallas, "Problemas con la escoba:" + fallas);
    }
}
