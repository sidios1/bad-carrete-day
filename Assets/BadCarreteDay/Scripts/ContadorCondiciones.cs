using UnityEngine;
using UnityEngine.Events;

// Cuenta condiciones cumplidas y avisa cuando estan todas
// (ej: la escalera se abre cuando Toby esta comiendo Y Benja se tomo el agua).
public class ContadorCondiciones : MonoBehaviour
{
    public int requeridas = 2;
    private int cumplidas = 0;

    public UnityEvent alCumplirTodas;

    public void Cumplir()
    {
        cumplidas++;
        if(cumplidas == requeridas) alCumplirTodas.Invoke();
    }
}
