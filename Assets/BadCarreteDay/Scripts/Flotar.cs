using UnityEngine;

// Hace que un objeto se meza suavemente (barcos en la bahia).
public class Flotar : MonoBehaviour
{
    [Tooltip("Cuanto sube y baja (metros)")]
    public float altura = 0.25f;
    [Tooltip("Cuanto se inclina (grados)")]
    public float inclinacion = 2f;
    public float velocidad = 0.6f;

    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private float desfase;

    void Start()
    {
        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;
        desfase = Random.Range(0f, 10f); // cada barco se mueve distinto
    }

    void Update()
    {
        float t = Time.time * velocidad + desfase;
        transform.position = posicionInicial + Vector3.up * Mathf.Sin(t) * altura;
        transform.rotation = rotacionInicial * Quaternion.Euler(Mathf.Sin(t * 0.8f) * inclinacion, 0f, Mathf.Cos(t * 0.6f) * inclinacion);
    }
}
