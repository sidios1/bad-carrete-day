using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Basurero que se puede revisar UNA vez con [E]: puede tener plata y/o algo que Benja comenta.
// Se conecta: DemoControlInteraccion.alInteractuar -> Revisar()
public class Basurero : MonoBehaviour
{
    [Header("Que hay adentro")]
    public int plata = 0;
    [TextArea(1, 3)]
    public string pensamiento = "Puro cachureo.";

    [Header("Referencias")]
    public ControladorDinero dinero;
    public ControladorHUD hud;
    [Tooltip("Zona de interaccion (se apaga despues de revisar)")]
    public GameObject zonaInteraccion;
    [Tooltip("Modelo del basurero (se sacude al revisarlo)")]
    public Transform modelo;
    public AudioClip sonido;

    public UnityEvent alRevisar;

    private bool revisado = false;

    public void Revisar()
    {
        if(revisado == true) return;
        revisado = true;
        if(plata > 0 && dinero != null) dinero.Sumar(plata);
        if(hud != null)
        {
            hud.OcultarPrompt();
            string texto = pensamiento;
            if(plata > 0) texto = "(+$" + plata + ") " + pensamiento;
            hud.MostrarPensamiento(texto);
        }
        if(sonido != null) AudioSource.PlayClipAtPoint(sonido, transform.position, 0.8f);
        if(zonaInteraccion != null) zonaInteraccion.SetActive(false);
        if(modelo != null) StartCoroutine(_Sacudir());
        alRevisar.Invoke();
    }

    // Se sacude un poco (como si Benja hurgara adentro)
    IEnumerator _Sacudir()
    {
        Quaternion inicial = modelo.localRotation;
        for(float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            modelo.localRotation = inicial * Quaternion.Euler(Mathf.Sin(t * 50f) * 6f * (1f - t / 0.5f), 0f, 0f);
            yield return null;
        }
        modelo.localRotation = inicial;
    }

    public bool YaRevisado() { return revisado; }
}
