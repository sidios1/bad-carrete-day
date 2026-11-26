using UnityEngine;
using UnityEngine.Events;

// Lleva la cuenta de las "lucas" (plata) del jugador.
// Las monedas llaman a Sumar() y las tiendas a Gastar().
public class ControladorDinero : MonoBehaviour
{
    // PROPIEDAD: cada vez que cambia el dinero, avisa por evento (al HUD, por ejemplo)
    [SerializeField]
    private int _dinero = 0;
    public int dinero
    {
        get { return _dinero; }
        set
        {
            if(value != _dinero)
            {
                _dinero = value;
                eventos.alCambiarDinero.Invoke(_dinero);

                // Si llegamos al monto objetivo (y hay uno configurado), avisamos una sola vez
                if(montoObjetivo > 0 && _dinero >= montoObjetivo && objetivoAlcanzado == false)
                {
                    objetivoAlcanzado = true;
                    eventos.alAlcanzarMontoObjetivo.Invoke();
                }
            }
        }
    }

    [Header("Objetivo opcional (ej: juntar 300 para comprar algo)")]
    public int montoObjetivo = 0;
    private bool objetivoAlcanzado = false;

    [Header("Sonido al recibir plata (opcional)")]
    public AudioSource sonidoSumar;

    [Header("Configuracion Eventos")]
    public Eventos eventos;

    void Start()
    {
        // Avisamos el valor inicial para que el HUD muestre algo desde el principio
        eventos.alCambiarDinero.Invoke(_dinero);
    }

    // METODOS
    public void Sumar(int cantidad)
    {
        if(cantidad <= 0) return;
        dinero += cantidad;
        if(sonidoSumar != null) sonidoSumar.Play();
    }

    // Devuelve true si alcanzo la plata (y la descuenta), false si no alcanza
    public bool Gastar(int cantidad)
    {
        if(cantidad > dinero) return false;
        dinero -= cantidad;
        return true;
    }

    // DEFINICION DE DATOS
    [System.Serializable]
    public struct Eventos
    {
        public UnityEvent<int> alCambiarDinero;
        public UnityEvent alAlcanzarMontoObjetivo;
    }
}
