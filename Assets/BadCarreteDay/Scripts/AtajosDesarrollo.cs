using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// SOLO PARA PROBAR (editor y builds de desarrollo): botones "IR A (PRUEBAS)" en el menu de pausa
// (y las teclas F1..F7) saltan a un punto de la mision.
// Recarga el nivel y reproduce los MISMOS eventos del juego hasta ese punto (comprar, hablar,
// darle el completo al Toby...), asi el mundo queda igual que si lo hubieras jugado.
//   F1 inicio · F2 con agua · F3 Don Lucho ya tiene su vino (completo comprado, frente al Toby)
//   F4 Toby comiendo (calle alta) · F5 techo A · F6 cima (antes de los cumas, con escoba) · F7 cancha (Julio)
public class AtajosDesarrollo : MonoBehaviour
{
    public ControladorDinero dinero;
    public ControladorMision mision;
    public ControladorHUD hud;
    public Transform jugador;
    public ControladorCompra compraAgua, compraVino, compraCompleto;
    public DialogoNPC dialogoLucho;
    [Tooltip("Pegatina del plato del Toby (dejar el completo)")]
    public DemoControlInteraccion dejarCompleto;
    public AtaqueJugador ataque;
    [Tooltip("Columna de botones en el menu de pausa (se oculta en builds normales)")]
    public GameObject panelAtajos;
    public int objetivoTechoA = 6;
    public int objetivoAscensor = 7;

    // El atajo pedido sobrevive a la recarga de la escena
    private static int pendiente = 0;

    void Start()
    {
        if(Debug.isDebugBuild == false)
        {
            if(panelAtajos != null) panelAtajos.SetActive(false);
            enabled = false;
            return;
        }
        if(pendiente > 0)
        {
            int punto = pendiente;
            pendiente = 0;
            StartCoroutine(_Aplicar(punto));
        }
    }

    void Update()
    {
        Keyboard k = Keyboard.current;
        if(k == null) return;
        Key[] teclas = { Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7 };
        for(int i = 0; i < teclas.Length; i++)
            if(k[teclas[i]].wasPressedThisFrame) IrA(i + 1);
    }

    public void IrA(int punto)
    {
        pendiente = punto;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator _Aplicar(int punto)
    {
        yield return null; // que todos hagan su Start
        if(punto >= 2) // agua
        {
            dinero.Sumar(compraAgua.precio);
            compraAgua.Comprar();
            Teletransportar(new Vector3(-6f, 0.1f, 41f));
        }
        if(punto >= 3) // vino a Don Lucho + completo comprado
        {
            dinero.Sumar(compraVino.precio + compraCompleto.precio);
            compraVino.Comprar();
            dialogoLucho.CambiarConversacion(5);
            dialogoLucho.conversaciones[5].alTerminar.Invoke();
            compraCompleto.Comprar();
            Teletransportar(new Vector3(1.5f, 0.1f, 48f));
        }
        if(punto >= 4) // Toby comiendo
        {
            dejarCompleto.eventos.alInteractuar.Invoke();
            yield return null;
            Teletransportar(new Vector3(0f, 4.2f, 66f));
        }
        if(punto >= 5)
        {
            mision.CompletarObjetivo(objetivoTechoA);
            Teletransportar(Lugar("Llegada_TechoA", new Vector3(5.4f, 8.1f, 84f)));
        }
        if(punto >= 6) // ascensor arriba, con escoba
        {
            yield return null;
            mision.CompletarObjetivo(objetivoAscensor);
            if(ataque != null) ataque.TomarEscoba();
            Teletransportar(Lugar("Checkpoint_Cima", new Vector3(6f, 16.1f, 116.5f)));
        }
        if(punto >= 7) // los 3 cumas derrotados
        {
            yield return null;
            foreach(string cuma in new[] { "Cuma_James", "Cuma_David", "Cuma_James_Rojo" })
            {
                GameObject c = GameObject.Find(cuma);
                if(c != null) c.SendMessage("CalcularDanio", 999f);
            }
            yield return null;
            Teletransportar(Lugar("Checkpoint_Cancha", new Vector3(0f, 16.1f, 136f)));
        }
        if(hud != null) hud.MostrarPensamiento("[Atajo F" + punto + "] " + mision.TextoObjetivoActual());
    }

    Vector3 Lugar(string nombre, Vector3 siNoEsta)
    {
        GameObject g = GameObject.Find(nombre);
        return g != null ? g.transform.position + Vector3.up * 0.1f : siNoEsta;
    }

    void Teletransportar(Vector3 pos)
    {
        CharacterController cc = jugador.GetComponent<CharacterController>();
        if(cc != null) cc.enabled = false;
        jugador.position = pos;
        if(cc != null) cc.enabled = true;
    }
}
