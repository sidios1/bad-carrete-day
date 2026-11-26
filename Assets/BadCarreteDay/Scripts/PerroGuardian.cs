using System.Collections;
using UnityEngine;

// Toby cuida la escalera de verdad: ladra apenas te ve, si te acercas gruñe,
// y si te acercas mas se te lanza y te empuja hacia atras. No deja pasar a nadie.
// Se calma (Calmarse) cuando le dejan un completo en el plato.
public class PerroGuardian : MonoBehaviour
{
    public Transform jugador;
    [Tooltip("Modelo del perro (se mueve hacia adelante al lanzarse)")]
    public Transform modelo;

    [Header("Distancias")]
    [Tooltip("Desde aqui te ladra (seguido)")]
    public float radioLadrido = 10f;
    public float radioGrunido = 4.5f;
    public float radioAtaque = 2.4f;

    [Header("Empujon")]
    public float fuerzaEmpuje = 7f;
    public float duracionEmpuje = 0.3f;
    public float tiempoEntreAtaques = 1.2f;

    [Header("Sonidos y frases")]
    public AudioSource fuente;
    public AudioClip ladrido;
    public AudioClip grunido;
    public ControladorBurbujas burbujas;
    public string[] frasesGrunido = { "Grrrrr...", "GRRRR... ¡GUAU!" };
    public string fraseAtaque = "¡GUAU! ¡GUAU! ¡GUAU!";

    private float proximoAtaque = 0f;
    private float proximoGrunido = 0f;
    private float proximoLadrido = 0f;

    void Start()
    {
        if(modelo == null) modelo = transform.Find("Modelo_Toby");
        if(jugador == null)
        {
            GameObject j = GameObject.FindGameObjectWithTag("Player");
            if(j != null) jugador = j.transform;
        }
    }

    void Update()
    {
        if(jugador == null) return;
        Vector3 hacia = jugador.position - transform.position;
        hacia.y = 0f;
        float distancia = hacia.magnitude;
        if(Mathf.Abs(jugador.position.y - transform.position.y) > 2.5f) return;

        if(distancia < radioAtaque && Time.time > proximoAtaque)
            StartCoroutine(_Lanzarse(hacia.normalized));
        else if(distancia < radioGrunido && Time.time > proximoGrunido)
        {
            proximoGrunido = Time.time + 3f;
            if(fuente != null && grunido != null) fuente.PlayOneShot(grunido);
            if(burbujas != null) burbujas.Decir(transform, "Toby", frasesGrunido[Random.Range(0, frasesGrunido.Length)]);
        }

        // Ladra seguido mientras estas cerca (cada ladrido con un tono un poco distinto)
        if(distancia < radioLadrido && Time.time > proximoLadrido)
        {
            proximoLadrido = Time.time + Random.Range(1.2f, 2f);
            Ladrar();
        }
    }

    void Ladrar()
    {
        if(fuente == null || ladrido == null) return;
        fuente.pitch = Random.Range(0.85f, 1.05f);
        fuente.PlayOneShot(ladrido, 1f);
    }

    IEnumerator _Lanzarse(Vector3 direccion)
    {
        proximoAtaque = Time.time + tiempoEntreAtaques;
        proximoGrunido = Time.time + 2f;
        proximoLadrido = Time.time + 1f;
        Ladrar();
        if(burbujas != null) burbujas.Decir(transform, "Toby", fraseAtaque);

        // El perro salta hacia adelante y te empuja
        Vector3 inicio = modelo != null ? modelo.localPosition : Vector3.zero;
        CharacterController cc = jugador.GetComponent<CharacterController>();
        for(float t = 0f; t < duracionEmpuje; t += Time.deltaTime)
        {
            if(modelo != null) modelo.localPosition = inicio + Vector3.forward * 0.6f * Mathf.Sin(t / duracionEmpuje * Mathf.PI);
            if(cc != null && cc.enabled) cc.Move(direccion * fuerzaEmpuje * Time.deltaTime);
            yield return null;
        }
        if(modelo != null) modelo.localPosition = inicio;
    }

    // Le dejaron un completo: deja de cuidar la escalera
    public void Calmarse()
    {
        StopAllCoroutines();
        enabled = false;
    }
}
