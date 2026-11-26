using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Plataforma que viaja entre dos puntos (el ascensor viejo del cerro).
// Se activa por evento (ej: la palanca / pegatina ¡AQUÍ!) con Activar().
// Tambien arrastra al jugador que va encima (el CharacterController no lo hace solo).
public class PlataformaMovil : MonoBehaviour
{
    [Header("Recorrido")]
    public Transform puntoInicio;
    public Transform puntoFin;
    [Tooltip("Metros por segundo")]
    public float velocidad = 2f;
    [Tooltip("Si es true, al llegar vuelve sola al inicio despues de la espera")]
    public bool idaYVuelta = false;
    public float esperaEnExtremos = 2f;

    [Header("Configuracion Eventos")]
    public UnityEvent alLlegarAlFin;

    // Personajes parados sobre la plataforma (los detecta un trigger hijo)
    private List<CharacterController> pasajeros = new List<CharacterController>();
    private bool moviendose = false;

    void Start()
    {
        if(puntoInicio != null)
            transform.position = puntoInicio.position;
    }

    // METODOS

    public void Activar()
    {
        if(moviendose == true) return;
        StartCoroutine(_Viajar());
    }

    IEnumerator _Viajar()
    {
        moviendose = true;

        yield return _MoverHasta(puntoFin.position);
        alLlegarAlFin.Invoke();

        if(idaYVuelta == true)
        {
            yield return new WaitForSeconds(esperaEnExtremos);
            yield return _MoverHasta(puntoInicio.position);
        }

        moviendose = false;
    }

    IEnumerator _MoverHasta(Vector3 destino)
    {
        while(Vector3.Distance(transform.position, destino) > 0.01f)
        {
            Vector3 anterior = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);
            Vector3 delta = transform.position - anterior;

            // Movemos a los pasajeros lo mismo que se movio la plataforma
            foreach(CharacterController pasajero in pasajeros)
                if(pasajero != null && pasajero.enabled)
                    pasajero.Move(delta);

            yield return null;
        }
        transform.position = destino;
    }

    // La plataforma tiene un segundo collider (trigger) un poco mas alto que detecta a quien va encima
    public void SubePasajero(Collider other)
    {
        CharacterController cc = other.GetComponent<CharacterController>();
        if(cc != null && !pasajeros.Contains(cc)) pasajeros.Add(cc);
    }

    public void BajaPasajero(Collider other)
    {
        CharacterController cc = other.GetComponent<CharacterController>();
        if(cc != null) pasajeros.Remove(cc);
    }

    void OnTriggerEnter(Collider other) { SubePasajero(other); }
    void OnTriggerExit(Collider other) { BajaPasajero(other); }
}
