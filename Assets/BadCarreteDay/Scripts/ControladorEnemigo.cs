using UnityEngine;
using UnityEngine.Events;

// "Cerebro" de un enemigo (cumas y Julio). Junta los scripts de clase:
//   MobController (perseguir/atacar), DemoControladorEstadisticas (salud),
//   DemoObjetivo (recibir golpes) y DemoArma (sus puños).
// Tambien recibe los Animation Events ActivaArma / DesactivaArma de la animacion de golpe.
public class ControladorEnemigo : MonoBehaviour
{
    [Header("Referencias (scripts de clase)")]
    public MobController mob;
    public DemoControladorEstadisticas estadisticas;
    public Animator animator;
    public DemoArma arma;

    [Header("Objetivo")]
    public Transform jugador;

    [Header("Frases (se muestran como pensamiento en el HUD)")]
    public string nombre = "Cuma";
    public string[] frasesAlDetectar;
    public string[] frasesAlRecibirGolpe;
    public ControladorHUD hud;

    [Header("Jefe (Julio): punto debil en la espalda y sin dolor de frente")]
    [Tooltip("Si le pegan por la espalda (donde tiene el cuchillo) el daño se multiplica")]
    public bool puntoDebilEspalda = false;
    public float multiplicadorEspalda = 3f;
    [Tooltip("Que tan atras tiene que estar el jugador (grados desde la espalda)")]
    public float anguloEspalda = 60f;
    public string[] frasesEspalda;
    [Tooltip("false = de frente no se interrumpe al recibir golpes (no siente el dolor)")]
    public bool sienteDolor = true;
    [Tooltip("Que tan rapido gira hacia el jugador mientras pega")]
    public float giroAlPegar = 10f;
    [Tooltip("Aturdido (lo maneja ConductaJulio): no pega ni avanza")]
    public bool aturdido = false;
    public UnityEvent alGolpeEspalda;
    public UnityEvent alGolpeFrente;

    [Header("Al morir")]
    public bool destruirAlMorir = true;
    public float esperaDestruccion = 3f;

    [Header("Configuracion Eventos")]
    public UnityEvent alDerrotado;

    private bool activo = false;
    private bool muerto = false;

    void Update()
    {
        // MobController no gira al enemigo mientras pega: lo hacemos mirar al jugador
        if(aturdido && muerto == false) animator.SetBool("atacando", false);
        if(muerto == false && aturdido == false && mob.estado == MobController.Estado.atacando && jugador != null)
        {
            Vector3 dir = jugador.position - transform.position;
            dir.y = 0f;
            if(dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), giroAlPegar * Time.deltaTime);
        }
    }

    // METODOS

    // Empieza a perseguir y atacar al jugador (se puede llamar varias veces sin problema)
    public void Activar()
    {
        if(muerto == true) return;
        if(gameObject.activeInHierarchy == false) return; // ej: refuerzos que todavia no llegan

        if(activo == false)
        {
            activo = true;
            Decir(frasesAlDetectar);
        }
        if(mob.agente.isOnNavMesh) mob.agente.isStopped = false;
        mob.SetTarget(jugador);
    }

    // Conectar a DemoObjetivo.alSerGolpeado
    public void RecibirGolpe(float danio)
    {
        if(muerto == true) return;

        // ¿Le pegaron por la espalda? (el jugador esta detras)
        bool espalda = false;
        if(puntoDebilEspalda && jugador != null)
        {
            Vector3 haciaJugador = jugador.position - transform.position;
            haciaJugador.y = 0f;
            espalda = Vector3.Angle(-transform.forward, haciaJugador) < anguloEspalda;
        }
        if(espalda) danio *= multiplicadorEspalda;

        estadisticas.CalcularDanio(danio);
        if(estadisticas.estaVivo == true)
        {
            if(espalda)
            {
                animator.SetTrigger("golpeado");
                Decir(frasesEspalda);
                alGolpeEspalda.Invoke();
            }
            else if(sienteDolor)
            {
                animator.SetTrigger("golpeado");
                Decir(frasesAlRecibirGolpe);
            }
            else
            {
                Decir(frasesAlRecibirGolpe); // no se interrumpe: se rie
                alGolpeFrente.Invoke();
            }
            Activar(); // si le pegan, se enoja aunque no te haya visto
        }
    }

    // Conectar a DemoControladorEstadisticas.eventos.alMorir
    public void Morir()
    {
        if(muerto == true) return;
        muerto = true;

        mob.Detener();
        if(arma != null) arma.SetDanioActivo(false);
        animator.SetTrigger("muerte");

        // Que no estorbe ni lo sigan golpeando
        foreach(Collider col in GetComponents<Collider>()) col.enabled = false;
        mob.agente.enabled = false;

        alDerrotado.Invoke();

        if(destruirAlMorir == true)
            estadisticas.DestruirConDelay(esperaDestruccion);
    }

    // Deja de pelear (ej: los refuerzos cuando cae Julio)
    public void Rendirse()
    {
        if(muerto == true) return;
        mob.Detener();
        if(arma != null) arma.SetDanioActivo(false);
    }

    // Animation Events que traen los clips de caminar/correr de Starter Assets.
    // (Sin estos metodos Unity tira error. En la F5 aqui se ponen los sonidos de pasos.)
    public void OnFootstep(AnimationEvent evento) { }
    public void OnLand(AnimationEvent evento) { }

    // Animation Events de la animacion "Punching"
    public void ActivaArma() { if(arma != null && muerto == false && aturdido == false) arma.SetDanioActivo(true); }
    public void DesactivaArma() { if(arma != null) arma.SetDanioActivo(false); }

    private void Decir(string[] frases)
    {
        if(frases == null || frases.Length == 0) return;
        string frase = frases[Random.Range(0, frases.Length)];
        // La frase sale en una burbuja sobre ESTE personaje (no sobre Benja)
        if(hud != null && hud.burbujas != null) hud.burbujas.Decir(transform, nombre, frase);
        else if(hud != null) hud.MostrarPensamiento(nombre + ": " + frase);
    }
}
