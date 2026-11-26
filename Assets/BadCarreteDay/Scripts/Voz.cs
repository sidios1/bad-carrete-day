using System.Collections.Generic;
using UnityEngine;

// La "voz" de un personaje: un murmullo de silabas (como en Conker) en vez de voz real.
// Cada personaje tiene su tono. Tambien sabe donde esta su cabeza (para poner la burbuja).
public class Voz : MonoBehaviour
{
    // Todas las voces de la escena (para encontrar al que habla por su nombre)
    public static List<Voz> todas = new List<Voz>();

    [Tooltip("Nombre del personaje (igual al que aparece en los dialogos, ej: \"Doña Marta\")")]
    public string nombre = "Benja";
    [Tooltip("Silabas de murmullo (se generan por codigo en el editor)")]
    public AudioClip[] silabas;
    [Tooltip("1 = tono normal; mas bajo = mas grave")]
    public float tono = 1f;
    [Tooltip("Variacion al azar del tono en cada silaba")]
    public float variacion = 0.08f;
    [Tooltip("Cada cuantas letras suena una silaba")]
    public int letrasPorSilaba = 3;
    [Tooltip("Altura de la cabeza sobre los pies (para la burbuja)")]
    public float alturaCabeza = 2.05f;

    private AudioSource fuente;

    void Awake()
    {
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f; // el dialogo se escucha siempre igual
        fuente.volume = 0.7f;
    }

    void OnEnable() { todas.Add(this); }
    void OnDisable() { todas.Remove(this); }

    public Vector3 Cabeza()
    {
        return transform.position + Vector3.up * alturaCabeza;
    }

    // Una silaba del murmullo
    public void Silaba()
    {
        if(silabas == null || silabas.Length == 0) return;
        fuente.pitch = tono + Random.Range(-variacion, variacion);
        fuente.PlayOneShot(silabas[Random.Range(0, silabas.Length)]);
    }

    // Un sonido cualquiera (ej: el pitido de censura) desde este personaje
    public void Sonar(AudioClip clip)
    {
        if(clip == null) return;
        fuente.pitch = 1f;
        fuente.PlayOneShot(clip);
    }

    public static Voz Buscar(string nombrePersonaje)
    {
        foreach(Voz v in todas)
            if(v.nombre == nombrePersonaje) return v;
        return null;
    }
}
