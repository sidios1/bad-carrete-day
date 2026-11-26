using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DemoControlInteraccion : MonoBehaviour
{
    public InputActionReference inputInteraccion;
    // [Bad Carrete Day] solo se interactua si el que entro al trigger tiene este tag
    public string tagEsperado = "Player";
    // [Bad Carrete Day] true mientras el jugador esta dentro de la zona de deteccion
    public bool jugadorDentro = false;
    public Eventos eventos;

    void OnEnable()
    {
        inputInteraccion.action.started += Interactuar;
    }

    void OnDisable()
    {
        inputInteraccion.action.started -= Interactuar;
    }

    private void Interactuar(InputAction.CallbackContext context)
    {
        // [Bad Carrete Day] si el jugador no esta cerca, no hacemos nada
        if(jugadorDentro == false) return;
        // [Bad Carrete Day] en pausa no se interactua
        if(Time.timeScale == 0f) return;

        Debug.Log("INTERACCION");
        eventos.alInteractuar.Invoke();
    }

    void OnTriggerEnter(Collider other)
    {
        // [Bad Carrete Day] antes reaccionaba a cualquier objeto; ahora solo al tag esperado
        if(other.gameObject.tag == tagEsperado)
        {
            jugadorDentro = true;
            eventos.alEntrarDeteccion.Invoke();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if(other.gameObject.tag == tagEsperado)
        {
            jugadorDentro = false;
            eventos.alSalirDeteccion.Invoke();
        }
    }

    [System.Serializable]
    public struct Eventos
    {
        public UnityEvent alEntrarDeteccion;
        public UnityEvent alSalirDeteccion;
        public UnityEvent alInteractuar;
    }
}
