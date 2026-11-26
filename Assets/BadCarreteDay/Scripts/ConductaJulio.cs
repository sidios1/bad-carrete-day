using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Julio "El Pastero": un TANQUE trasnochado (tres dias sin dormir). Se ve vivo pero con algo
// de zombie: camina lento y encorvado, con tirones de cabeza y respirando agitado.
//  - De frente no siente el dolor (se rie); en la espalda tiene un cuchillo clavado: ahi duele
//    (ControladorEnemigo multiplica el daño) y queda aturdido un momento.
//  - Su golpe es lento y pesado y empuja a Benja.
//  - Fase 2 (enojado): se vuelve erratico, con embestidas rapidas; despues de cada una queda
//    agotado (la oportunidad de pegarle en la espalda).
[DefaultExecutionOrder(9000)] // al final: su tope de giro manda sobre los demas scripts
public class ConductaJulio : MonoBehaviour
{
    public ControladorEnemigo enemigo;
    public Animator animator;
    public NavMeshAgent agente;
    public Transform jugador;
    public ControladorHUD hud;

    [Header("Sonidos")]
    public AudioSource voz;
    public AudioClip rugido, quejido;

    [Header("Tanque")]
    public float velocidad = 1.6f;
    [Tooltip("Velocidad en la fase 2 (fuera de las embestidas)")]
    public float velocidadEnojado = 2.0f;
    [Tooltip("Su golpe empuja a Benja hacia atras")]
    public float empuje = 6f;

    [Header("Giro (tanque: se le puede rodear)")]
    [Tooltip("Grados por segundo que puede girar como maximo (Benja corriendo en circulo lo supera)")]
    public float giroMaximo = 55f;
    [Tooltip("Mientras se rie o ruge no gira; pegando gira mas lento: la oportunidad de rodearlo")]
    public bool sinGiroAlPegar = true;
    [Range(0f, 1f)] public float giroAlPegar = 0.4f;

    [Header("Fase 2: embestidas")]
    public float velocidadEmbestida = 6.5f;
    public float duracionEmbestida = 1.3f;
    public float danioEmbestida = 20f;
    public float minEntreEmbestidas = 4f, maxEntreEmbestidas = 7f;
    [Tooltip("Segundos que queda agotado despues de embestir")]
    public float agotado = 1.6f;
    [Tooltip("Segundos que queda aturdido cuando le pegan en la espalda")]
    public float aturdidoEspalda = 0.6f;

    [Header("Tics (se ve trasnochado)")]
    [Tooltip("Grados que se encorva")]
    public float encorvado = 14f;
    public string[] frasesEmbestida = { "¡AAAAAAH!", "¡TE VOY A REVENTAR!", "¡¿Qué me mirai?!" };
    public string[] frasesAgotado = { "*jadeo* ...tres días... sin dormir...", "*jadeo* ...espérate... espérate..." };

    public bool peleando { get; private set; }
    public bool enojado { get; private set; }
    public bool embistiendo { get; private set; }
    public int embestidas { get; private set; }

    private float proximaEmbestida;
    private bool aturdidoAhora = false;
    private Transform cabeza, columna;
    private float proximoTic, ticHasta;
    private Quaternion giroTic = Quaternion.identity;
    private Quaternion rotacionCuadroAnterior;
    private bool hayRotacionAnterior = false;
    private float giroPermitido = 0f; // grados/s permitidos este cuadro (lo decide Girar)


    void Start()
    {
        if(animator != null && animator.isHuman)
        {
            cabeza = animator.GetBoneTransform(HumanBodyBones.Head);
            columna = animator.GetBoneTransform(HumanBodyBones.Chest);
        }
        if(agente != null) { agente.speed = velocidad; agente.updateRotation = false; } // el giro lo hace Girar()
        proximoTic = Time.time + Random.Range(2f, 5f);
    }

    // Conectar a ControladorFasesJefe.alEmpezarPelea
    public void EmpezarPelea()
    {
        peleando = true;
        // Desde aqui solo ConductaJulio lo gira (MirarJugador lo giraria rapido hacia Benja)
        MirarJugador mirar = GetComponent<MirarJugador>();
        if(mirar != null) { mirar.girarCuerpoAlHablar = false; mirar.volverAlTerminar = false; }
        Rugir();
    }

    // Conectar a ControladorFasesJefe.alFase2: se enoja
    public void EmpezarFase2()
    {
        enojado = true;
        velocidad = velocidadEnojado;
        if(agente != null) agente.speed = velocidad;
        proximaEmbestida = Time.time + 1.5f;
        Rugir();
        if(hud != null) hud.MostrarTitulo("¡JULIO SE ENOJÓ!");
    }

    // Conectar a ControladorEnemigo.alDerrotado
    public void Terminar()
    {
        peleando = false;
        StopAllCoroutines();
        embistiendo = false;
        enemigo.aturdido = false;
        if(voz != null) voz.Stop();
    }

    void Rugir()
    {
        if(animator != null) animator.SetTrigger("rugir");
        if(voz != null && rugido != null) voz.PlayOneShot(rugido);
    }

    // Conectar a ControladorEnemigo.alGolpeFrente: no siente el dolor, se rie
    public void GolpeDeFrente()
    {
        if(embistiendo || aturdidoAhora || enemigo.mob.estado == MobController.Estado.atacando) return;
        if(animator != null) animator.SetTrigger("reir");
    }

    // Conectar a ControladorEnemigo.alGolpeEspalda: el cuchillo duele, queda aturdido
    public void GolpeEnLaEspalda()
    {
        if(voz != null && quejido != null) voz.PlayOneShot(quejido);
        if(aturdidoAhora == false && embistiendo == false) StartCoroutine(_Aturdido(aturdidoEspalda));
    }

    // Julio gira SOLO aqui, lento (giroMaximo °/s): hacia donde camina o, si esta quieto cerca, hacia Benja.
    // Mientras se rie, ruge o esta agotado no gira, y pegando gira mas lento: la oportunidad de rodearlo.
    void Girar()
    {
        giroPermitido = giroMaximo;
        if(peleando == false || jugador == null || agente == null) return;
        float velocidadGiro = embistiendo ? 720f : giroMaximo;
        if(aturdidoAhora) velocidadGiro = 0f;
        if(animator != null && sinGiroAlPegar && embistiendo == false)
        {
            AnimatorStateInfo e = animator.GetCurrentAnimatorStateInfo(0);
            if(e.IsName("Reir") || e.IsName("Rugir")) velocidadGiro = 0f;
            else if(e.IsName("Atacando")) velocidadGiro = giroMaximo * giroAlPegar; // pegando gira aun mas lento
        }
        Vector3 dir = agente.velocity.sqrMagnitude > 0.04f ? agente.velocity : jugador.position - transform.position;
        dir.y = 0f;
        giroPermitido = velocidadGiro;
        if(dir.sqrMagnitude < 0.001f || velocidadGiro <= 0f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), velocidadGiro * Time.deltaTime);
    }

    void Update()
    {
        Girar();
        if(peleando == false || enojado == false || embistiendo || aturdidoAhora || jugador == null) return;
        if(Time.time < proximaEmbestida || enemigo.mob.estado == MobController.Estado.atacando) return;
        float distancia = Vector3.Distance(jugador.position, transform.position);
        if(distancia > 3f && distancia < 14f) StartCoroutine(_Embestir());
        else proximaEmbestida = Time.time + 0.5f;
    }

    // Embestida: corre rapido hacia Benja; si lo alcanza lo golpea y lo empuja. Despues queda agotado.
    IEnumerator _Embestir()
    {
        embistiendo = true;
        embestidas++;
        Decir(frasesEmbestida);
        if(voz != null && rugido != null) voz.PlayOneShot(rugido, 0.7f);
        // Se compromete con una direccion: corre en linea recta hacia donde estaba Benja (y se pasa).
        // Si Benja se hace a un lado, Julio sigue de largo y queda de espaldas.
        enemigo.mob.Detener();
        enemigo.mob.ResetTarget();
        Vector3 linea = jugador.position - transform.position;
        linea.y = 0f;
        Vector3 destino = transform.position + linea.normalized * (linea.magnitude + 3f);
        NavMeshHit punto;
        if(NavMesh.SamplePosition(destino, out punto, 2f, NavMesh.AllAreas)) destino = punto.position;
        agente.isStopped = false;
        agente.speed = velocidadEmbestida;
        agente.acceleration = 30f;
        agente.SetDestination(destino);
        for(float t = 0f; t < duracionEmbestida; t += Time.deltaTime)
        {
            if(t > 0.2f && agente.pathPending == false && agente.remainingDistance < 0.4f) break;
            Vector3 hacia = jugador.position - transform.position;
            hacia.y = 0f;
            if(hacia.magnitude < 1.5f)
            {
                jugador.SendMessage("CalcularDanio", danioEmbestida, SendMessageOptions.DontRequireReceiver);
                StartCoroutine(_Empujar(hacia.normalized, empuje * 1.3f));
                break;
            }
            yield return null;
        }
        agente.speed = velocidad;
        agente.acceleration = 8f;
        embistiendo = false;
        Decir(frasesAgotado);
        yield return _Aturdido(agotado);
        enemigo.mob.SetTarget(jugador); // vuelve a perseguir y pegar
        proximaEmbestida = Time.time + Random.Range(minEntreEmbestidas, maxEntreEmbestidas);
    }

    // Aturdido / agotado: no avanza ni pega (la oportunidad de pegarle en la espalda)
    IEnumerator _Aturdido(float segundos)
    {
        aturdidoAhora = true;
        enemigo.aturdido = true;
        if(agente.isOnNavMesh) agente.isStopped = true;
        yield return new WaitForSeconds(segundos);
        if(agente.isActiveAndEnabled && agente.isOnNavMesh) agente.isStopped = false;
        enemigo.aturdido = false;
        aturdidoAhora = false;
    }

    // Animation Event del golpe (lo recibe tambien ControladorEnemigo): ademas del daño, empuja
    public void ActivaArma()
    {
        if(aturdidoAhora || jugador == null) return;
        Vector3 hacia = jugador.position - transform.position;
        hacia.y = 0f;
        if(hacia.magnitude < 2.2f && Vector3.Angle(transform.forward, hacia) < 70f)
            StartCoroutine(_Empujar(hacia.normalized, empuje));
    }

    IEnumerator _Empujar(Vector3 direccion, float fuerza)
    {
        CharacterController cc = jugador.GetComponent<CharacterController>();
        for(float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            if(cc != null && cc.enabled) cc.Move(direccion * fuerza * (1f - t / 0.25f) * Time.deltaTime);
            yield return null;
        }
    }

    // Se ve trasnochado: encorvado y con tirones de cabeza (mas seguidos cuando esta enojado)
    void LateUpdate()
    {
        // Tope final: pase lo que pase en el cuadro (otros scripts, el agente), Julio no gira mas de lo permitido
        if(peleando && hayRotacionAnterior && embistiendo == false)
            transform.rotation = Quaternion.RotateTowards(rotacionCuadroAnterior, transform.rotation, giroPermitido * Time.deltaTime);
        rotacionCuadroAnterior = transform.rotation;
        hayRotacionAnterior = true;


        if(columna != null) columna.rotation = Quaternion.AngleAxis(encorvado, transform.right) * columna.rotation;
        if(cabeza == null) return;
        if(Time.time > proximoTic)
        {
            giroTic = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(-28f, 28f), Random.Range(-18f, 18f));
            ticHasta = Time.time + Random.Range(0.12f, 0.25f);
            proximoTic = Time.time + Random.Range(2f, 5f) * (enojado ? 0.45f : 1f);
        }
        if(Time.time < ticHasta) cabeza.rotation = cabeza.rotation * giroTic;
    }

    void Decir(string[] frases)
    {
        if(frases == null || frases.Length == 0 || hud == null || hud.burbujas == null) return;
        hud.burbujas.Decir(transform, "Julio", frases[Random.Range(0, frases.Length)]);
    }
}
