using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Tienda tipo Zelda/Pokemon: una lista de productos con precio y descripcion.
// W/S (o flechas, o el mouse) para elegir, E o clic para comprar, Esc para salir.
// Cada producto es un ControladorCompra (el mismo de antes): la tienda solo lo muestra y lo compra.
// Un producto apagado (SetActive false) no esta a la venta: sale en gris ("Comprado").
// Al abrir, el vendedor saluda en una burbuja y te mira (sin conversacion: el menu sale al tiro).
public class ControladorTienda : MonoBehaviour
{
    // true mientras la tienda esta abierta (el dialogo, la pausa y el ataque la respetan)
    public static bool abierta = false;
    // Frame en que se cerro (el mismo Esc que cierra la tienda no debe abrir la pausa)
    public static int frameCierre = -1;

    [Header("Productos (en orden)")]
    public ControladorCompra[] productos;
    [TextArea] public string[] descripciones;
    public Color[] coloresIcono;
    [Tooltip("Lo que dice el vendedor al comprar cada producto (vacio = nada)")]
    public string[] comentarios;

    [Header("Vendedor")]
    public Transform vendedor;
    [Tooltip("Lo que dice el vendedor al abrir la tienda")]
    public string saludo = "";
    public string nombreVendedor = "Doña Marta";
    public string fraseNoAlcanza = "No te alcanza, mijo. Yo no soy el Padre Hurtado.";

    [Header("UI")]
    public GameObject panel;
    public TMP_Text textoTitulo;
    public TMP_Text textoDinero;
    public TMP_Text textoDescripcion;
    [Tooltip("Una fila por producto (Button con Image de fondo)")]
    public Button[] filas;
    public Image[] iconos;
    public TMP_Text[] nombres;
    public TMP_Text[] precios;

    [Header("Referencias")]
    public ControladorDinero dinero;
    public ControladorJuego juego;
    public ControladorBurbujas burbujas;
    public InputActionReference inputComprar;
    public InputActionReference inputSalir;

    [Header("Sonidos")]
    public AudioSource fuente;
    public AudioClip sonidoMover;
    public AudioClip sonidoComprar;
    public AudioClip sonidoError;

    private int seleccion = 0;
    private int frameApertura = -1;
    private readonly Color colorFila = new Color(1f, 1f, 1f, 0.08f);
    private readonly Color colorElegida = new Color(1f, 0.85f, 0.2f, 0.35f);

    void Start()
    {
        abierta = false;
        if(panel != null) panel.SetActive(false);
        for(int i = 0; i < filas.Length; i++)
        {
            int indice = i; // copia para el clic
            filas[i].onClick.AddListener(() => { seleccion = indice; Comprar(); });
        }
    }

    void OnEnable()
    {
        if(inputComprar != null) inputComprar.action.started += AlComprar;
        if(inputSalir != null) inputSalir.action.started += AlSalir;
    }

    void OnDisable()
    {
        if(inputComprar != null) inputComprar.action.started -= AlComprar;
        if(inputSalir != null) inputSalir.action.started -= AlSalir;
        abierta = false;
    }

    // Se conecta al final de la conversacion con Doña Marta
    public void Abrir()
    {
        if(abierta == true) return;
        abierta = true;
        frameApertura = Time.frameCount;
        panel.SetActive(true);
        if(juego != null) juego.BloquearJugador(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        seleccion = PrimeraDisponible();
        Refrescar();
        if(vendedor != null)
        {
            vendedor.SendMessage("EmpezarConversacion", SendMessageOptions.DontRequireReceiver); // te mira
            if(burbujas != null && saludo != "") burbujas.Decir(vendedor, nombreVendedor, saludo);
        }
    }

    public void Cerrar()
    {
        if(abierta == false) return;
        abierta = false;
        frameCierre = Time.frameCount;
        panel.SetActive(false);
        if(juego != null) juego.BloquearJugador(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if(vendedor != null) vendedor.SendMessage("TerminarConversacion", SendMessageOptions.DontRequireReceiver);
    }

    void Update()
    {
        if(abierta == false || Keyboard.current == null) return;
        if(Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame) Mover(-1);
        if(Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame) Mover(1);
    }

    public void Mover(int direccion)
    {
        seleccion = (seleccion + direccion + productos.Length) % productos.Length;
        Sonar(sonidoMover);
        Refrescar();
    }

    private void AlComprar(InputAction.CallbackContext contexto)
    {
        if(abierta == false || Time.frameCount == frameApertura) return;
        Comprar();
    }

    private void AlSalir(InputAction.CallbackContext contexto)
    {
        if(abierta == true) Cerrar();
    }

    // Compra el producto elegido (si esta a la venta y alcanza la plata)
    public void Comprar()
    {
        if(abierta == false) return;
        ControladorCompra p = productos[seleccion];
        if(Disponible(seleccion) == false) { Sonar(sonidoError); return; }
        if(dinero.dinero < p.precio)
        {
            Sonar(sonidoError);
            if(burbujas != null && vendedor != null) burbujas.Decir(vendedor, nombreVendedor, fraseNoAlcanza);
            return;
        }
        p.Comprar();
        Sonar(sonidoComprar);
        if(burbujas != null && vendedor != null && seleccion < comentarios.Length && comentarios[seleccion] != "")
            burbujas.Decir(vendedor, nombreVendedor, comentarios[seleccion]);
        Refrescar();
    }

    // A la venta = el producto esta prendido (lo prende la mision) y no se ha comprado
    public bool Disponible(int i)
    {
        return productos[i] != null && productos[i].gameObject.activeSelf;
    }

    int PrimeraDisponible()
    {
        for(int i = 0; i < productos.Length; i++) if(Disponible(i)) return i;
        return 0;
    }

    void Refrescar()
    {
        if(textoDinero != null) textoDinero.text = "Tienes: $" + dinero.dinero.ToString("#,0").Replace(",", ".");
        for(int i = 0; i < filas.Length; i++)
        {
            bool aLaVenta = Disponible(i);
            string precio = "$" + productos[i].precio.ToString("#,0").Replace(",", ".");
            nombres[i].text = productos[i].nombreProducto;
            precios[i].text = aLaVenta ? precio : "Comprado";
            Color texto = aLaVenta ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            nombres[i].color = texto;
            precios[i].color = aLaVenta ? new Color(1f, 0.85f, 0.25f) : texto;
            if(i < iconos.Length && i < coloresIcono.Length)
                iconos[i].color = aLaVenta ? coloresIcono[i] : new Color(0.4f, 0.4f, 0.4f, 0.5f);
            filas[i].GetComponent<Image>().color = (i == seleccion) ? colorElegida : colorFila;
        }
        if(textoDescripcion != null)
        {
            string d = seleccion < descripciones.Length ? descripciones[seleccion] : "";
            if(Disponible(seleccion) == false && productos[seleccion].Comprado() == false) d = "Todavía no lo necesitas.";
            textoDescripcion.text = d;
        }
    }

    void Sonar(AudioClip clip)
    {
        if(fuente != null && clip != null) fuente.PlayOneShot(clip);
    }
}
