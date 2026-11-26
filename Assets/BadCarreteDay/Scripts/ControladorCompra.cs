using UnityEngine;
using UnityEngine.Events;

// Un producto que se puede comprar (ej: la caja de vino o el completo del almacen).
// Se conecta al evento alInteractuar de DemoControlInteraccion -> Comprar().
public class ControladorCompra : MonoBehaviour
{
    [Header("Producto")]
    public string nombreProducto = "Completo";
    public int precio = 100;
    [Tooltip("Si es true, solo se puede comprar una vez")]
    public bool unaSolaVez = true;
    private bool comprado = false;

    [Header("Referencias")]
    public ControladorDinero controladorDinero;

    [Header("Configuracion Eventos")]
    public UnityEvent alComprar;
    public UnityEvent alNoAlcanzar;

    void Start()
    {
        if(controladorDinero == null)
            controladorDinero = FindFirstObjectByType<ControladorDinero>();
    }

    public void Comprar()
    {
        if(unaSolaVez == true && comprado == true) return;

        if(controladorDinero.Gastar(precio) == true)
        {
            comprado = true;
            Debug.Log("Compraste: " + nombreProducto);
            alComprar.Invoke();
        }
        else
        {
            Debug.Log("No te alcanza para: " + nombreProducto);
            alNoAlcanzar.Invoke();
        }
    }

    // Para la tienda: ¿ya se compro?
    public bool Comprado()
    {
        return comprado;
    }

        // Texto para mostrar en el HUD (ej: "[E] Comprar Completo ($100)")
    public string TextoPrompt()
    {
        return "[E] Comprar " + nombreProducto + " ($" + precio + ")";
    }
}
