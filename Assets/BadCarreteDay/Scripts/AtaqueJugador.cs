using System.Collections;
using StarterAssets;
using UnityEngine;

// Reglas del golpe de Benja (ControladorJugador de clase le pregunta antes de pegar):
//  - Solo se pega en el suelo, con el control activo (sin dialogo, tienda ni pausa),
//    sin estar colgado, vivo, sin caña (con caña solo camina y habla)
//    y sin otro golpe en curso (el clic no se "guarda").
//  - Apunta solo: gira hacia el enemigo vivo mas cercano que este cerca y adelante/al lado.
//  - Mientras pega camina lento (no patina).
public class AtaqueJugador : MonoBehaviour
{
    public ThirdPersonController movimiento;
    public Animator animator;
    public AgarreBorde agarre;
    public DemoControladorEstadisticas estadisticas;

    [Header("Apuntar")]
    public float radioApuntar = 2.5f;
    [Tooltip("Angulo maximo (desde el frente) para girar hacia un enemigo")]
    public float anguloApuntar = 110f;

    [Header("Arma (la zona de golpe frente a Benja)")]
    public DemoArma arma;
    [Tooltip("La escoba que se ve en la mano (se prende al recibirla)")]
    public GameObject escobaVisual;
    public float danioPunio = 10f;
    public float danioEscoba = 25f;

    [Header("Durante el golpe")]
    public float velocidadAlPegar = 0.3f;

    [Tooltip("Que tan rapido cierra/abre el puño")]
    public float velocidadGuardia = 6f;

    private bool pegando = false;
    private bool conEscoba = false;
    private int capaEscoba = -1;
    private Resaca resaca;

    void Start()
    {
        resaca = GetComponent<Resaca>(); // solo existe en los niveles con caña
        if(animator != null) capaEscoba = animator.GetLayerIndex("Escoba");
    }

    // Con la escoba, el puño va SIEMPRE cerrado (capa "Escoba" del Animator: solo los dedos),
    // tambien al pegar y al morir. Solo se abre colgado o subiendo un borde (las manos van en el borde
    // y la escoba en la espalda). La escoba la ubica EscobaEnMano (dentro del puño).
    void Update()
    {
        if(capaEscoba < 0) return;
        bool punio = conEscoba && (agarre == null || (agarre.colgado == false && agarre.subiendo == false));
        float peso = Mathf.MoveTowards(animator.GetLayerWeight(capaEscoba), punio ? 1f : 0f, Time.deltaTime * velocidadGuardia);
        animator.SetLayerWeight(capaEscoba, peso);
    }

    // La señora Gladys le pasa la escoba: se ve en la mano y pega mas fuerte
    public void TomarEscoba()
    {
        conEscoba = true;
        if(escobaVisual != null) escobaVisual.SetActive(true);
        if(arma != null) { arma.danio = danioEscoba; arma.nombre = "Escoba"; }
        // Con la escoba no bosteza (llevaria la escoba a la cara)
        ControladorRandomIdle bostezo = GetComponent<ControladorRandomIdle>();
        if(bostezo != null) bostezo.enabled = false;
        if(animator != null) { animator.SetBool("estaIdle", false); animator.SetInteger("randomIdle", 0); }
    }

    public bool IntentarAtacar()
    {
        if(movimiento == null || animator == null) return true;
        bool sePuede = movimiento.enabled && movimiento.Grounded
                       && (agarre == null || agarre.colgado == false)
                       && (estadisticas == null || estadisticas.estaVivo)
                       && (resaca == null || resaca.conCana == false)
                       && pegando == false
                       && animator.GetCurrentAnimatorStateInfo(0).IsName("Ataque01") == false;
        if(sePuede == false)
        {
            animator.ResetTrigger("ataque");
            return false;
        }
        Apuntar();
        StartCoroutine(_Pegando());
        return true;
    }

    // Gira hacia el enemigo vivo mas cercano (si hay uno cerca)
    void Apuntar()
    {
        ControladorEnemigo mejor = null;
        float mejorDistancia = radioApuntar;
        foreach(ControladorEnemigo e in FindObjectsByType<ControladorEnemigo>(FindObjectsSortMode.None))
        {
            if(e.estadisticas == null || e.estadisticas.estaVivo == false) continue;
            Vector3 hacia = e.transform.position - transform.position;
            hacia.y = 0f;
            float d = hacia.magnitude;
            if(d > mejorDistancia || d < 0.01f) continue;
            if(Vector3.Angle(transform.forward, hacia) > anguloApuntar) continue;
            mejor = e;
            mejorDistancia = d;
        }
        if(mejor == null) return;
        Vector3 direccion = mejor.transform.position - transform.position;
        direccion.y = 0f;
        transform.rotation = Quaternion.LookRotation(direccion.normalized);
    }

    // Mientras dura el golpe, camina lento
    IEnumerator _Pegando()
    {
        pegando = true;
        movimiento.MultiplicadorVelocidad = velocidadAlPegar;
        // espera a que entre al estado de ataque y despues a que salga (maximo 1,6 s)
        float t = 0f;
        yield return new WaitForSeconds(0.15f);
        while(t < 1.45f && animator.GetCurrentAnimatorStateInfo(0).IsName("Ataque01"))
        {
            t += Time.deltaTime;
            yield return null;
        }
        movimiento.MultiplicadorVelocidad = 1f;
        pegando = false;
    }
}
