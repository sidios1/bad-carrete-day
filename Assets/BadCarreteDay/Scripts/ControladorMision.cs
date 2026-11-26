using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Lista de objetivos que se cumplen EN ORDEN (ej: "Habla con el viejo" -> "Compra el vino" -> ...).
// Otros scripts llaman a CompletarObjetivo(indice) por UnityEvent.
public class ControladorMision : MonoBehaviour
{
    [Header("Mision")]
    public string tituloMision = "Recupera tu telefono";
    public List<Objetivo> objetivos = new List<Objetivo>();

    // PROPIEDAD: indice del objetivo actual. Al cambiar avisa el nuevo texto (al HUD).
    [SerializeField]
    private int _objetivoActual = 0;
    public int objetivoActual
    {
        get { return _objetivoActual; }
        set
        {
            if(value != _objetivoActual)
            {
                _objetivoActual = value;
                eventos.alCambiarObjetivo.Invoke(TextoObjetivoActual());
            }
        }
    }

    [Header("Configuracion Eventos")]
    public Eventos eventos;

    void Start()
    {
        eventos.alCambiarObjetivo.Invoke(TextoObjetivoActual());
    }

    // METODOS

    // Marca un objetivo como cumplido. Si se cumple uno FUTURO (ej: juntar las lucas antes
    // de hablar con el viejo) queda guardado y se salta cuando le toque.
    public void CompletarObjetivo(int indice)
    {
        if(indice < 0 || indice >= objetivos.Count) return;
        if(objetivos[indice].completado == true) return;

        objetivos[indice].completado = true;
        Debug.Log("Objetivo completado: " + objetivos[indice].descripcion);
        objetivos[indice].alCompletar.Invoke();

        // Avanzamos mientras el objetivo actual ya este cumplido
        if(indice != objetivoActual) return;
        while(objetivoActual < objetivos.Count && objetivos[objetivoActual].completado == true)
            objetivoActual++;

        if(objetivoActual >= objetivos.Count)
        {
            Debug.Log("MISION COMPLETADA: " + tituloMision);
            eventos.alCompletarMision.Invoke();
        }
    }

    public void CompletarObjetivoActual()
    {
        CompletarObjetivo(objetivoActual);
    }

    public string TextoObjetivoActual()
    {
        if(objetivoActual >= objetivos.Count) return "¡Listo!";
        return objetivos[objetivoActual].descripcion;
    }

    // DEFINICION DE DATOS
    [System.Serializable]
    public class Objetivo
    {
        public string descripcion;
        public bool completado;
        public UnityEvent alCompletar = new UnityEvent();
    }

    [System.Serializable]
    public struct Eventos
    {
        public UnityEvent<string> alCambiarObjetivo;
        public UnityEvent alCompletarMision;
    }
}
