using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ControladorJugador : MonoBehaviour
{
    // public float saludBase = 100f;
    // [SerializeField]
    // private float _saludActual;
    // public float saludActual
    // {
    //     get { return _saludActual; }
    //     set
    //     {
    //         if(value != _saludActual)
    //         {
    //             _saludActual = value;
    //             alActualizarSaludActual.Invoke(_saludActual);
    //         }
    //     }
    // }

    public InputActionReference inputAtaque;
    [SerializeField]
    private DemoArma _armaActual;
    public DemoArma armaActual
    {
        get{return _armaActual;}
        set
        {
            if(value != _armaActual)
            {
                _armaActual = value;
            }
        }
    }

    public UnityEvent<float> alActualizarSaludActual;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //saludActual += Time.deltaTime;
    }

    void OnEnable()
    {
        inputAtaque.action.started += Atacar;
    }

    void OnDisable()
    {
        inputAtaque.action.started -= Atacar;
    }

    public void ActivaArma()
    {
        if(armaActual == null) return;

        armaActual.SetDanioActivo(true);
    }

    public void DesactivaArma()
    {
        if(armaActual == null) return;

        armaActual.SetDanioActivo(false);
    }

    private void Atacar(InputAction.CallbackContext context)
    {
        // [Bad Carrete Day] en pausa (o con la tienda abierta) no se ataca
        if(Time.timeScale == 0f || ControladorTienda.abierta == true) return;
        // [Bad Carrete Day] AtaqueJugador decide si se puede pegar (en el suelo, sin dialogo,
        // sin estar colgado, sin otro golpe en curso) y apunta al enemigo mas cercano
        AtaqueJugador reglas = GetComponent<AtaqueJugador>();
        if(reglas != null && reglas.IntentarAtacar() == false) return;
        this.gameObject.GetComponent<Animator>().SetTrigger("ataque");
    }

    public void MensajeSalud()
    {
        //Debug.Log("Mi salud actual es: " + saludActual);
    }

    public void Metodo(Collider col)
    {
        // Calculando daño desde col
    }
}
