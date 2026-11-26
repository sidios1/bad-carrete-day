using StarterAssets;
using UnityEngine;
using UnityEngine.Rendering;

// La caña de Benja: hasta que se toma un agua SOLO puede caminar (lento) y hablar.
// No corre, no salta (ni se cuelga), no pega, y lo ve todo borroso.
// Con el agua vuelve a ser agil y se le aclara la vista.
// Va antes que el ThirdPersonController (DefaultExecutionOrder) para borrar el salto a tiempo.
[DefaultExecutionOrder(-50)]
public class Resaca : MonoBehaviour
{
    public ThirdPersonController movimiento;
    public StarterAssetsInputs entradas;
    public AgarreBorde agarre;
    public Animator animator;
    public ControladorHUD hud;

    [Header("Con caña")]
    [Tooltip("Debe calzar con la animacion de caminar curado (el Animator la acelera para 1 m/s)")]
    public float velocidadConCana = 1.0f;
    public string[] quejas = { "Mi cabeza...", "Nunca más tomo... hasta el viernes.", "Necesito un agua. Urgente." };
    public float segundosEntreQuejas = 35f;
    public string fraseNoSalta = "Ni cagando salto así... necesito un agua.";

    [Header("Vista borrosa")]
    [Tooltip("Volume con desenfoque, viñeta y colores apagados")]
    public Volume vistaCana;
    [Tooltip("Cuanto pulsa la vista (0 = nada)")]
    public float pulso = 0.25f;
    public float segundosAclarar = 2f;
    [Tooltip("Que tan borroso se ve mientras habla o compra (0..1)")]
    public float pesoHablando = 0.3f;

    public bool conCana = true;

    private float velocidadNormal, carreraNormal;
    private bool yaDijoNoSalta = false;
    private float aclarando = -1f;

    void Start()
    {
        velocidadNormal = movimiento.MoveSpeed;
        carreraNormal = movimiento.SprintSpeed;
        if(conCana == true)
        {
            movimiento.MoveSpeed = velocidadConCana;
            movimiento.SprintSpeed = velocidadConCana; // no puede correr
            if(agarre != null) agarre.enabled = false; // no se cuelga de nada
            InvokeRepeating("Quejarse", 18f, segundosEntreQuejas);
        }
        if(animator != null) animator.SetBool("Curado", conCana);
        if(vistaCana != null) vistaCana.weight = conCana ? 1f : 0f;
    }

    void Update()
    {
        // Si el Animator se reinicia (ej: al reaparecer despues de morir) se vuelve a marcar la caña
        if(animator != null && animator.GetBool("Curado") != conCana)
            animator.SetBool("Curado", conCana);

        if(conCana)
        {
            // Saltar no se puede: se borra el boton antes de que lo lea el movimiento
            if(entradas != null && entradas.jump)
            {
                entradas.jump = false;
                if(yaDijoNoSalta == false && hud != null) { yaDijoNoSalta = true; hud.MostrarPensamiento(fraseNoSalta); }
            }
            // La vista "late" con la cabeza. Hablando o comprando (control bloqueado) se aclara harto,
            // para leer y ver a quien te habla
            if(vistaCana != null)
            {
                float objetivo = 1f - pulso * 0.5f * (1f + Mathf.Sin(Time.time * 1.7f));
                if(movimiento.enabled == false) objetivo = pesoHablando;
                vistaCana.weight = Mathf.MoveTowards(vistaCana.weight, objetivo, Time.deltaTime * 2f);
            }
        }
        else if(aclarando >= 0f && vistaCana != null)
        {
            aclarando += Time.deltaTime;
            vistaCana.weight = Mathf.Lerp(vistaCana.weight, 0f, aclarando / segundosAclarar);
            if(aclarando >= segundosAclarar) { vistaCana.weight = 0f; aclarando = -1f; }
        }
    }

    void Quejarse()
    {
        if(hud != null && quejas.Length > 0) hud.MostrarPensamiento(quejas[Random.Range(0, quejas.Length)]);
    }

    // Se conecta a la compra del agua
    public void TomarAgua()
    {
        if(conCana == false) return;
        conCana = false;
        CancelInvoke("Quejarse");
        movimiento.MoveSpeed = velocidadNormal;
        movimiento.SprintSpeed = carreraNormal;
        if(agarre != null) agarre.enabled = true;
        if(animator != null) animator.SetBool("Curado", false);
        aclarando = 0f;
        if(hud != null) hud.MostrarPensamiento("Ahora sí... ¡a correr! (mantén Shift)");
    }
}
