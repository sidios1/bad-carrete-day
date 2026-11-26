using UnityEngine;

// Hace girar un objeto sobre si mismo (monedas, lukas, el polerón-hélice...).
public class Girar : MonoBehaviour
{
    [Tooltip("Grados por segundo")]
    public float velocidad = 120f;
    public Vector3 eje = Vector3.up;
    [Tooltip("true = gira respecto al mundo (las monedas); false = respecto a si mismo")]
    public bool ejeDelMundo = true;

    void Update()
    {
        transform.Rotate(eje, velocidad * Time.deltaTime, ejeDelMundo ? Space.World : Space.Self);
    }
}
