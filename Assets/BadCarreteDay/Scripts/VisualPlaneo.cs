using StarterAssets;
using UnityEngine;

// Muestra el polerón girando sobre la cabeza de Benja mientras planea
// (lee la variable Planeando que agregamos al ThirdPersonController).
public class VisualPlaneo : MonoBehaviour
{
    public ThirdPersonController controlador;
    [Tooltip("Objeto del polerón (hijo del hueso de la cabeza)")]
    public GameObject poleron;
    public float velocidadGiro = 900f;

    void Update()
    {
        bool planeando = controlador != null && controlador.Planeando;

        if(poleron.activeSelf != planeando)
            poleron.SetActive(planeando);

        if(planeando)
            poleron.transform.Rotate(Vector3.up, velocidadGiro * Time.deltaTime, Space.World);
    }
}
