using System.Collections;
using UnityEngine;

// Presentacion de una zona (como en Conker): la primera vez que llegas, la camara
// muestra el objetivo un par de segundos y Benja lo comenta. Despues vuelve el control.
// Se conecta a un ControladorLugarObjetivo (alLlegarJugador -> Presentar).
public class PresentacionZona : MonoBehaviour
{
    // Las pruebas automaticas las apagan para no esperar
    public static bool desactivadas = false;

    [Tooltip("Objeto con la CinemachineCamera de presentaciones (apagado normalmente)")]
    public GameObject camara;
    public Vector3 posicionCamara;
    public Vector3 mirarA;
    [TextArea] public string comentario;
    public float duracion = 2.4f;

    public ControladorJuego juego;
    public ControladorHUD hud;

    private bool hecha = false;

    public void Presentar()
    {
        if(hecha || desactivadas || camara == null) return;
        if(GameObject.Find("PruebasAutomaticas") != null) return; // las pruebas no esperan presentaciones
        hecha = true;
        StartCoroutine(_Presentar());
    }

    IEnumerator _Presentar()
    {
        if(juego != null) juego.BloquearJugador(true);
        camara.transform.position = posicionCamara;
        camara.transform.rotation = Quaternion.LookRotation(mirarA - posicionCamara);
        camara.SetActive(true);
        if(hud != null && comentario != "") hud.MostrarPensamiento(comentario);
        yield return new WaitForSeconds(duracion);
        camara.SetActive(false);
        yield return new WaitForSeconds(0.5f); // la camara vuelve sola (transicion de Cinemachine)
        if(juego != null) juego.BloquearJugador(false);
    }
}
