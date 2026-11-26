using UnityEngine;

// Va en el estado de ataque del Animator (de Benja y de los cumas).
// Al salir del estado apaga el arma SIEMPRE, aunque el golpe haya sido cortado
// (si no, el arma quedaba "prendida" y el siguiente golpe no pegaba).
public class ApagarArmaAlSalir : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SendMessage("DesactivaArma", SendMessageOptions.DontRequireReceiver);
    }
}
