using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.Playables;

// Colgarse de un borde y subir (como en los plataformas 3D).
// Si Benja esta en el aire avanzando contra un muro cuyo borde le queda a la altura de las
// manos, se agarra. Con Saltar sube; con Atras (S) se suelta.
// Los colliders invisibles del borde del mapa van en la capa "Limite" y no se pueden agarrar.
//
// Las medidas (altura de las manos al colgar, cuanto avanza el cuerpo al subir) se MIDEN
// en las animaciones al construir el nivel: asi las manos quedan justo en el borde y,
// al terminar de subir, Benja queda parado donde termino el cuerpo (sin saltos raros).
public class AgarreBorde : MonoBehaviour
{
    public ThirdPersonController movimiento;
    public StarterAssetsInputs entradas;
    public Animator animator;

    [Header("Deteccion")]
    [Tooltip("Capas que se pueden agarrar (sin \"Limite\")")]
    public LayerMask capasAgarrables = ~0;
    [Tooltip("El borde debe quedar entre estas alturas sobre los pies")]
    public float alturaMinima = 0.8f;
    public float alturaMaxima = 2.2f;
    public float distanciaPared = 0.7f;

    [Header("Animaciones (se miden al empezar el juego)")]
    public AnimationClip clipColgado;
    public AnimationClip clipSubir;

    [Header("Medidas de las animaciones (se calculan solas en Start)")]
    [Tooltip("Altura de las manos sobre los pies cuando cuelga")]
    public float alturaManos = 2.0f;
    [Tooltip("Distancia del muro a la que queda Benja colgado (para que el cuerpo no se meta en el muro)")]
    public float separacionMuro = 0.2f;
    [Tooltip("Duracion de la animacion de subir (segundos)")]
    public float duracionSubida = 0.9f;
    [Tooltip("Segundos que tarda en acomodarse al agarrarse (sin teletransportarse)")]
    public float duracionAgarre = 0.12f;

    [Header("Eventos")]
    [Tooltip("La primera vez que se agarra (para el tutorial)")]
    public UnityEvent alAgarrarPrimeraVez;

    public bool colgado { get; private set; }
    public bool subiendo { get; private set; }
    private bool acomodando = false;
    private bool yaSeAgarro = false;
    private float sinAgarreHasta = 0f;
    private float alturaBorde;
    private Vector3 puntoPared;
    private Vector3 haciaPared;
    private CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Start()
    {
        MedirAnimaciones();
    }

    // Pone por un instante (antes del primer cuadro) las animaciones de colgarse y de subir en el
    // esqueleto de Benja y mide donde quedan sus manos y pies. Asi las manos quedan justo en el
    // borde y la subida termina donde termino el cuerpo, sea cual sea la animacion.
    void MedirAnimaciones()
    {
        if(animator == null || clipColgado == null || clipSubir == null || animator.isHuman == false) return;
        PlayableGraph grafo = PlayableGraph.Create("Medir");
        AnimationPlayableOutput salida = AnimationPlayableOutput.Create(grafo, "Medir", animator);
        AnimationClipPlayable pose = AnimationClipPlayable.Create(grafo, clipColgado);
        pose.SetApplyFootIK(false);
        salida.SetSourcePlayable(pose);

        pose.SetTime(clipColgado.length * 0.5f);
        grafo.Evaluate();
        Vector3 manos = transform.InverseTransformPoint((animator.GetBoneTransform(HumanBodyBones.LeftHand).position
                                                       + animator.GetBoneTransform(HumanBodyBones.RightHand).position) / 2f);
        alturaManos = manos.y + 0.06f; // los dedos quedan un poco sobre la muñeca

        // La parte del cuerpo que queda mas hacia el muro (pies, rodillas, pecho, cabeza...):
        // Benja se separa del muro lo justo para que ninguna se meta en el
        float frente = 0f;
        foreach(HumanBodyBones h in new[] { HumanBodyBones.Hips, HumanBodyBones.Chest, HumanBodyBones.UpperChest, HumanBodyBones.Head,
                                            HumanBodyBones.LeftToes, HumanBodyBones.RightToes, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot,
                                            HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg })
        {
            Transform hueso = animator.GetBoneTransform(h);
            if(hueso != null) frente = Mathf.Max(frente, transform.InverseTransformPoint(hueso.position).z);
        }
        separacionMuro = Mathf.Max(0.2f, frente + 0.1f);

        duracionSubida = clipSubir.length;
        grafo.Destroy();
        Debug.Log("[AgarreBorde] manos colgado: " + alturaManos.ToString("F2") + " m alto · separacion del muro " + separacionMuro.ToString("F2") + " m");
    }

    void Update()
    {
        if(Time.timeScale == 0f || subiendo || acomodando) return;

        if(colgado)
        {
            if(entradas.jump) { entradas.jump = false; StartCoroutine(_Subir()); }
            else if(entradas.move.y < -0.5f) Soltar();
            return;
        }

        // Solo en el aire, con el control activo y empujando hacia adelante
        if(movimiento.enabled == false || movimiento.Grounded || Time.time < sinAgarreHasta) return;
        if(entradas.move.sqrMagnitude < 0.1f) return;
        BuscarBorde();
    }

    void BuscarBorde()
    {
        Vector3 adelante = transform.forward;
        RaycastHit pared;
        // Dos rayos (pecho y cintura): asi detecta muros y tambien rejas bajas
        bool hayPared = Physics.Raycast(transform.position + Vector3.up * 1.2f, adelante, out pared, distanciaPared, capasAgarrables, QueryTriggerInteraction.Ignore)
                     || Physics.Raycast(transform.position + Vector3.up * 0.7f, adelante, out pared, distanciaPared, capasAgarrables, QueryTriggerInteraction.Ignore);
        if(!hayPared) return;
        if(Mathf.Abs(pared.normal.y) > 0.3f) return; // no es un muro

        // Desde arriba, apenas hacia adentro del borde (6 cm: asi tambien sirve en rejas delgadas)
        Vector3 adentro = pared.point - new Vector3(pared.normal.x, 0f, pared.normal.z).normalized * 0.06f;
        Vector3 desde = new Vector3(adentro.x, transform.position.y + alturaMaxima + 0.3f, adentro.z);
        RaycastHit arriba;
        if(!Physics.Raycast(desde, Vector3.down, out arriba, alturaMaxima - alturaMinima + 0.3f, capasAgarrables, QueryTriggerInteraction.Ignore)) return;
        if(arriba.normal.y < 0.7f) return;
        float altura = arriba.point.y - transform.position.y;
        if(altura < alturaMinima || altura > alturaMaxima) return;
        // Tiene que haber espacio para pararse arriba
        if(Physics.CheckCapsule(arriba.point + Vector3.up * 0.4f, arriba.point + Vector3.up * 1.5f, 0.25f, capasAgarrables, QueryTriggerInteraction.Ignore)) return;

        StartCoroutine(_Agarrar(pared, arriba.point.y));
    }

    IEnumerator _Agarrar(RaycastHit pared, float borde)
    {
        colgado = true;
        acomodando = true;
        alturaBorde = borde;
        haciaPared = -new Vector3(pared.normal.x, 0f, pared.normal.z).normalized;
        puntoPared = pared.point;
        movimiento.enabled = false;
        movimiento.ReiniciarEstado(false);
        entradas.jump = false;
        if(animator != null) animator.SetBool("Colgado", true);

        // Se acomoda (en vez de teletransportarse): manos en el borde, mirando al muro
        cc.enabled = false;
        Vector3 inicio = transform.position;
        Quaternion giroInicio = transform.rotation;
        Vector3 destino = new Vector3(puntoPared.x, alturaBorde - alturaManos, puntoPared.z) - haciaPared * separacionMuro;
        Quaternion giroDestino = Quaternion.LookRotation(haciaPared);
        for(float t = 0f; t < duracionAgarre; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duracionAgarre);
            transform.position = Vector3.Lerp(inicio, destino, k);
            transform.rotation = Quaternion.Slerp(giroInicio, giroDestino, k);
            yield return null;
        }
        transform.position = destino;
        transform.rotation = giroDestino;
        cc.enabled = true;
        acomodando = false;
        entradas.jump = false; // un Espacio apretado al agarrarse no cuenta como "subir"

        if(yaSeAgarro == false) { yaSeAgarro = true; alAgarrarPrimeraVez.Invoke(); }
    }

    // Subir: la ANIMACION mueve a Benja (movimiento de raiz): cuerpo y personaje suben juntos.
    // Al terminar, se le deja parado sobre el borde (un ajuste de centimetros).
    IEnumerator _Subir()
    {
        subiendo = true;
        if(animator != null) { animator.SetBool("Colgado", false); animator.SetTrigger("Subir"); }
        cc.enabled = false;
        if(animator != null) animator.applyRootMotion = true;
        yield return new WaitForSeconds(duracionSubida);
        if(animator != null) animator.applyRootMotion = false;

        // Parado sobre el borde: a la altura del borde y al menos 0,5 m pasado el muro
        // (si no, en una reja delgada quedaria parado encima de ella)
        Vector3 posicion = transform.position;
        float pasado = Vector3.Dot(new Vector3(posicion.x - puntoPared.x, 0f, posicion.z - puntoPared.z), haciaPared);
        if(pasado < 0.5f) posicion += haciaPared * (0.5f - pasado);
        posicion.y = alturaBorde + 0.02f;
        transform.position = posicion;
        cc.enabled = true;
        colgado = false;
        subiendo = false;
        entradas.jump = false; // un Espacio apretado durante la subida no salta solo
        movimiento.enabled = true;
        movimiento.ReiniciarEstado(true);
        sinAgarreHasta = Time.time + 0.3f;
    }

    public void Soltar()
    {
        if(colgado == false || subiendo) return;
        StopAllCoroutines();
        acomodando = false;
        cc.enabled = true;
        colgado = false;
        if(animator != null) animator.SetBool("Colgado", false);
        // Cae desde quieto (sin la velocidad que traia al agarrarse)
        movimiento.enabled = true;
        movimiento.ReiniciarEstado(false);
        sinAgarreHasta = Time.time + 0.6f;
    }
}
