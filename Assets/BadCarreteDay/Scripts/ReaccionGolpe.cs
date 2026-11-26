using UnityEngine;

// Animacion de "golpeado" de Benja. Si lo golpean mientras el pega (o colgado de un borde),
// recibe el daño igual pero NO se corta su animacion: asi su golpe se termina.
// Se conecta a DemoObjetivo.alSerGolpeado (junto con el daño).
public class ReaccionGolpe : MonoBehaviour
{
    public Animator animator;
    public AgarreBorde agarre;

    public void Golpeado(float danio)
    {
        if(animator == null) return;
        if(agarre != null && agarre.colgado) return;
        AnimatorStateInfo estado = animator.GetCurrentAnimatorStateInfo(0);
        if(estado.IsName("Ataque01") || estado.IsName("SubirBorde") || estado.IsName("Dying")) return;
        if(animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("Ataque01")) return;
        animator.SetTrigger("golpeado");
    }
}
