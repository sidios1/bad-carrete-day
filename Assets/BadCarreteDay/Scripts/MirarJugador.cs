using UnityEngine;

// Hace que un NPC mire al jugador:
//  - Cuando el jugador esta cerca, gira la CABEZA hacia el (IK de Animator, solo personajes humanoides).
//  - Cuando le hablas, gira el CUERPO hacia el jugador (si "girarCuerpoAlHablar" esta activo).
// DialogoNPC avisa con SendMessage("EmpezarConversacion") y SendMessage("TerminarConversacion").
// Para que funcione el giro de cabeza, la capa del Animator debe tener "IK Pass" activado.
public class MirarJugador : MonoBehaviour
{
    [Tooltip("Distancia a la que empieza a mirar al jugador")]
    public float distancia = 4.5f;
    [Tooltip("Gira la cabeza hacia el jugador (humanoides)")]
    public bool girarCabeza = true;
    [Tooltip("Al hablarle, el cuerpo se da vuelta hacia el jugador")]
    public bool girarCuerpoAlHablar = true;
    [Tooltip("Gira el cuerpo siempre que el jugador este cerca (ej: el perro)")]
    public bool girarCuerpoCerca = false;
    [Tooltip("Al terminar de hablar vuelve a mirar hacia donde estaba")]
    public bool volverAlTerminar = true;
    public float velocidadGiro = 4f;

    private Transform jugador;
    private Animator animator;
    private Quaternion rotacionOriginal;
    private bool hablando = false;
    private bool volviendo = false;
    private float pesoCabeza = 0f;

    void Start()
    {
        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if(j != null) jugador = j.transform;
        animator = GetComponent<Animator>();
        rotacionOriginal = transform.rotation;
    }

    public void EmpezarConversacion() { hablando = true; volviendo = false; }
    public void TerminarConversacion() { hablando = false; volviendo = volverAlTerminar; }
    // Deja de mirar (ej: Toby ya comio y se va a dormir)
    public void Dejar() { enabled = false; }

    void Update()
    {
        if(jugador == null) return;
        Vector3 haciaJugador = jugador.position - transform.position;
        haciaJugador.y = 0f;
        bool cerca = haciaJugador.magnitude < distancia;

        if((hablando && girarCuerpoAlHablar) || (girarCuerpoCerca && cerca))
        {
            if(haciaJugador.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(haciaJugador), velocidadGiro * Time.deltaTime);
        }
        else if(volviendo)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionOriginal, velocidadGiro * Time.deltaTime);
            if(Quaternion.Angle(transform.rotation, rotacionOriginal) < 1f) volviendo = false;
        }

        // La cabeza se suaviza (no salta de golpe)
        pesoCabeza = Mathf.MoveTowards(pesoCabeza, (cerca || hablando) ? 1f : 0f, Time.deltaTime * 2f);
    }

    void OnAnimatorIK(int capa)
    {
        if(!girarCabeza || animator == null || jugador == null) return;
        animator.SetLookAtWeight(pesoCabeza, 0.15f, 0.85f, 1f, 0.6f);
        animator.SetLookAtPosition(jugador.position + Vector3.up * 1.5f);
    }
}
