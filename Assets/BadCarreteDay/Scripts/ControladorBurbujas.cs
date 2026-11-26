using System.Collections;
using UnityEngine;

// Frases sueltas en burbujas sobre los personajes (gruñido de Toby, gritos de los cumas,
// comentarios de Doña Marta) y pensamientos de Benja (nube). Desaparecen solas.
// Hay UNO por escena (en "Sistemas"). Las conversaciones las maneja ControladorDialogo.
public class ControladorBurbujas : MonoBehaviour
{
    [Header("Burbujas (UI)")]
    [Tooltip("Burbujas para frases sueltas (se usan por turnos)")]
    public Burbuja[] burbujasSueltas;
    [Tooltip("Nube de pensamiento de Benja")]
    public Burbuja nubePensamiento;

    [Header("Jugador")]
    public Transform jugador;
    public float duracionPorLetra = 0.055f;
    public float duracionMinima = 2.2f;

    private int siguiente = 0;
    private Coroutine[] ocultar;
    private Coroutine ocultarNube;

    void Start()
    {
        ocultar = new Coroutine[burbujasSueltas.Length];
        foreach(Burbuja b in burbujasSueltas) b.gameObject.SetActive(false);
        if(nubePensamiento != null) nubePensamiento.gameObject.SetActive(false);
    }

    float Duracion(string texto)
    {
        return Mathf.Max(duracionMinima, texto.Length * duracionPorLetra);
    }

    // Un personaje dice algo en una burbuja (con un par de silabas de murmullo)
    public void Decir(Transform quien, string nombre, string texto)
    {
        if(quien == null || burbujasSueltas.Length == 0) return;
        // Si ese personaje ya tiene una burbuja, se reusa
        int i = -1;
        for(int k = 0; k < burbujasSueltas.Length; k++)
            if(burbujasSueltas[k].gameObject.activeSelf && burbujasSueltas[k].sigue == quien) i = k;
        if(i < 0) { i = siguiente; siguiente = (siguiente + 1) % burbujasSueltas.Length; }

        Burbuja b = burbujasSueltas[i];
        Voz voz = quien.GetComponent<Voz>();
        b.Seguir(quien, voz != null ? voz.alturaCabeza : 2f);
        b.texto.text = texto;
        if(b.nombre != null) b.nombre.text = nombre;
        if(b.indicadorAvanzar != null) b.indicadorAvanzar.SetActive(false); // las frases sueltas no se avanzan
        b.gameObject.SetActive(true);
        if(voz != null) { voz.Silaba(); voz.Invoke("Silaba", 0.12f); }

        if(ocultar[i] != null) StopCoroutine(ocultar[i]);
        ocultar[i] = StartCoroutine(_Ocultar(b.gameObject, Duracion(texto)));
    }

    // Pensamiento de Benja (nube sobre su cabeza)
    public void Pensar(string texto)
    {
        if(nubePensamiento == null || jugador == null) return;
        nubePensamiento.Seguir(jugador, 2.05f);
        nubePensamiento.texto.text = texto;
        nubePensamiento.gameObject.SetActive(true);
        if(ocultarNube != null) StopCoroutine(ocultarNube);
        ocultarNube = StartCoroutine(_Ocultar(nubePensamiento.gameObject, Duracion(texto) + 0.8f));
    }

    IEnumerator _Ocultar(GameObject obj, float segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);
        obj.SetActive(false);
    }
}
