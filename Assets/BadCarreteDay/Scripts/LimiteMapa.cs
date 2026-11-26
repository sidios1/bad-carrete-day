using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Red de seguridad del mapa: si el jugador logra salir de las zonas jugables
// (por algun hueco que se nos paso), la pantalla se pone negra y vuelve al ultimo
// checkpoint SIN perder vida.
public class LimiteMapa : MonoBehaviour
{
    public Transform jugador;
    public ControladorJuego juego;
    public ControladorHUD hud;
    [Tooltip("Zonas jugables (cajas en coordenadas del mundo)")]
    public Bounds[] zonas;
    [Tooltip("Imagen negra a pantalla completa (para el fundido)")]
    public Image fundido;
    public string frase = "Mejor no me meto por ahí.";
    [Tooltip("Segundos fuera antes de devolverlo")]
    public float tolerancia = 0.3f;

    private float fueraDesde = -1f;
    private bool volviendo = false;

    void Start()
    {
        if(fundido != null) fundido.gameObject.SetActive(false);
    }

    public bool Dentro(Vector3 posicion)
    {
        foreach(Bounds b in zonas)
            if(b.Contains(posicion)) return true;
        return false;
    }

    void Update()
    {
        if(volviendo || jugador == null) return;
        if(Dentro(jugador.position)) { fueraDesde = -1f; return; }
        if(fueraDesde < 0f) fueraDesde = Time.time;
        else if(Time.time - fueraDesde > tolerancia) StartCoroutine(_Volver());
    }

    IEnumerator _Volver()
    {
        volviendo = true;
        yield return Fundir(0f, 1f);
        juego.ReaparecerEnCheckpoint();
        if(hud != null) hud.MostrarPensamiento(frase);
        yield return new WaitForSeconds(0.2f);
        yield return Fundir(1f, 0f);
        fueraDesde = -1f;
        volviendo = false;
    }

    IEnumerator Fundir(float desde, float hasta)
    {
        if(fundido == null) yield break;
        fundido.gameObject.SetActive(true);
        for(float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            fundido.color = new Color(0f, 0f, 0f, Mathf.Lerp(desde, hasta, t / 0.25f));
            yield return null;
        }
        fundido.color = new Color(0f, 0f, 0f, hasta);
        if(hasta == 0f) fundido.gameObject.SetActive(false);
    }
}
