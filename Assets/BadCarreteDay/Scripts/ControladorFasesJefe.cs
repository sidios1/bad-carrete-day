using UnityEngine;
using UnityEngine.Events;

// Pelea con Julio "El Pastero": muestra su barra de salud y,
// cuando le queda la mitad de la vida, pasa a la FASE 2 (se enoja; los refuerzos son opcionales).
public class ControladorFasesJefe : MonoBehaviour
{
    [Header("Jefe")]
    public ControladorEnemigo jefe;
    public float saludMaxima = 150f;

    [Header("Barra de salud del jefe (HUD)")]
    public GameObject panelBarra;
    public ControladorBarraSalud barra;

    [Header("Fase 2")]
    [Range(0f, 1f)]
    public float porcentajeFase2 = 0.5f;
    [Tooltip("Multiplica la velocidad de Julio en la fase 2")]
    public float multiplicadorVelocidad = 1.4f;
    public ControladorEnemigo[] refuerzos;

    [Header("Configuracion Eventos")]
    public UnityEvent alEmpezarPelea;
    public UnityEvent alFase2;

    private bool enFase2 = false;

    void Start()
    {
        if(panelBarra != null) panelBarra.SetActive(false);
    }

    // Conectar al final del dialogo con Julio
    public void EmpezarPelea()
    {
        if(panelBarra != null) panelBarra.SetActive(true);
        jefe.Activar();
        alEmpezarPelea.Invoke();
    }

    // Conectar a DemoControladorEstadisticas.eventos.alCambiarSalud del jefe
    public void ActualizarSalud(float salud)
    {
        if(barra != null)
            barra.SetearSalud01(salud / saludMaxima * 100f);

        if(enFase2 == false && salud > 0f && salud <= saludMaxima * porcentajeFase2)
            EmpezarFase2();
    }

    private void EmpezarFase2()
    {
        enFase2 = true;
        jefe.mob.agente.speed *= multiplicadorVelocidad;

        if(refuerzos != null)
        foreach(ControladorEnemigo cabro in refuerzos)
        {
            cabro.gameObject.SetActive(true);
            cabro.Activar();
        }
        alFase2.Invoke();
    }

    // Conectar a ControladorEnemigo.alDerrotado del jefe
    public void TerminarPelea()
    {
        if(panelBarra != null) panelBarra.SetActive(false);
        if(refuerzos != null)
            foreach(ControladorEnemigo cabro in refuerzos)
                if(cabro != null && cabro.gameObject.activeInHierarchy) cabro.Rendirse();
    }
}
