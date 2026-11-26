using UnityEngine;

// Camara de conversacion: cuando empieza un dialogo, Benja se gira hacia el personaje
// y una segunda camara (Cinemachine) encuadra a los dos por sobre el hombro de Benja.
// Al prender este objeto, Cinemachine hace la transicion sola (tiene mas prioridad).
public class CamaraDialogo : MonoBehaviour
{
    [Tooltip("Objeto con la CinemachineCamera de dialogo (apagado normalmente)")]
    public GameObject camara;
    public Transform jugador;

    [Header("Encuadre")]
    public float distanciaAtras = 1.6f;
    public float alHombro = 0.9f;
    public float altura = 1.75f;

    // Lo llama ControladorDialogo al empezar una conversacion
    public void Enfocar(Transform personaje)
    {
        if(camara == null || jugador == null || personaje == null) return;

        Vector3 haciaPersonaje = personaje.position - jugador.position;
        haciaPersonaje.y = 0f;
        if(haciaPersonaje.sqrMagnitude < 0.01f) haciaPersonaje = jugador.forward;
        haciaPersonaje.Normalize();

        // Benja mira al personaje
        jugador.rotation = Quaternion.LookRotation(haciaPersonaje);

        // Camara detras y al lado de Benja, mirando la cara del personaje
        Vector3 lado = Vector3.Cross(Vector3.up, haciaPersonaje);
        Vector3 posicion = jugador.position - haciaPersonaje * distanciaAtras + lado * alHombro + Vector3.up * altura;
        Vector3 mirar = Vector3.Lerp(personaje.position, jugador.position, 0.25f) + Vector3.up * 1.45f;
        camara.transform.position = posicion;
        camara.transform.rotation = Quaternion.LookRotation(mirar - posicion);
        camara.SetActive(true);
    }

    public void Soltar()
    {
        if(camara != null) camara.SetActive(false);
    }
}
