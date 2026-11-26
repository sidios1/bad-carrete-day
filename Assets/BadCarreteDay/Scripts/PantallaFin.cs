using System.Collections;
using TMPro;
using UnityEngine;

// Pantalla de Fin.
//  - Victoria: el telefono prende y van apareciendo, una tras otra, la notificacion
//    de la nota, la reaccion de Benja y el "Continuara".
//  - Derrota: "TE DEJARON PATO" + una frase al azar.
public class PantallaFin : MonoBehaviour
{
    [Header("Referencias")]
    public ControladorCargaEscena cargaEscena;
    public GameObject panelVictoria;
    public GameObject panelDerrota;
    public GameObject panelCreditos;

    [Header("Victoria: elementos que aparecen en orden")]
    public GameObject[] apareceEnOrden;
    public float pausaEntreElementos = 1.6f;

    [Header("Derrota: frase al azar")]
    public TMP_Text textoFraseDerrota;
    [TextArea(1, 2)]
    public string[] frasesDerrota;

    void Start()
    {
        // Para poder hacer clic en los botones
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        bool gano = (ControladorJuego.ultimoResultado == ControladorJuego.Resultado.victoria);
        if(panelVictoria != null) panelVictoria.SetActive(gano);
        if(panelDerrota != null) panelDerrota.SetActive(!gano);
        if(panelCreditos != null) panelCreditos.SetActive(false);

        if(gano) StartCoroutine(_MostrarEnOrden());
        else if(textoFraseDerrota != null && frasesDerrota != null && frasesDerrota.Length > 0)
            textoFraseDerrota.text = frasesDerrota[Random.Range(0, frasesDerrota.Length)];
    }

    IEnumerator _MostrarEnOrden()
    {
        if(apareceEnOrden == null) yield break;
        foreach(GameObject obj in apareceEnOrden) obj.SetActive(false);
        foreach(GameObject obj in apareceEnOrden)
        {
            yield return new WaitForSeconds(pausaEntreElementos);
            obj.SetActive(true);
        }
    }

    public void Reintentar()
    {
        int nivel = ControladorJuego.indiceUltimoNivel;
        if(nivel < 0) nivel = 0;
        cargaEscena.CargaEscena(nivel);
    }

    public void IrAlMenu()
    {
        cargaEscena.CargaEscena(0);
    }

    public void MostrarCreditos(bool mostrar)
    {
        if(panelCreditos != null) panelCreditos.SetActive(mostrar);
    }
}
