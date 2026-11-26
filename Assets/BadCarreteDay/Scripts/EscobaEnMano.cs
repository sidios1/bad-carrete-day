using UnityEngine;

// Pone la escoba DENTRO del puño de Benja en cada cuadro (despues de las animaciones):
//  - el mango pasa por el centro de los dedos cerrados
//  - va en la direccion del puño: de la base del meñique a la del indice (las cerdas salen por
//    el lado del pulgar, como la hoja de una espada; el extremo del mango asoma bajo el meñique)
// Asi queda derecha en cualquier animacion. Ademas:
//  - fuera del golpe, la muñeca deja la escoba en la linea del antebrazo, inclinada hacia adelante
//    (brazo abajo -> adelante y abajo, como una espada en reposo); y si asi chocara con el cuerpo,
//    prueba otras direcciones y usa la primera que no atraviese cabeza, torso, piernas ni el otro brazo
//  - las cerdas nunca se meten en el suelo
//  - colgado/subiendo un borde (manos en el borde) o conversando/comprando, va a la espalda
public class EscobaEnMano : MonoBehaviour
{
    public Animator animator;
    [Tooltip("La escoba (su origen es el punto de agarre y apunta hacia +Z)")]
    public Transform escoba;
    public AgarreBorde agarre;
    [Tooltip("Para saber si esta conversando o comprando (control bloqueado)")]
    public StarterAssets.ThirdPersonController movimiento;

    [Header("Agarre")]
    [Tooltip("0 = el centro queda en las falanges del medio; 1 = en los nudillos")]
    [Range(0f, 1f)] public float haciaLosNudillos = 0.35f;

    [Header("Muñeca (fuera del golpe)")]
    [Tooltip("Cuanto se inclina la escoba hacia adelante respecto del antebrazo (0 = en linea con el antebrazo)")]
    public float haciaAdelante = 0.9f;
    [Tooltip("Lo maximo que se dobla la muñeca")]
    public float maximoDoblez = 80f;
    public float largo = 1.25f;

    [Header("En la espalda (colgado / subiendo / conversando)")]
    public Vector3 posicionEspalda = new Vector3(0f, 0.05f, -0.16f);
    public Vector3 giroEspalda = new Vector3(-150f, 0f, 35f);

    private Transform mano, codo, pecho, indice, menique;
    private Transform cabeza, cadera, cuello, piernaI, rodillaI, pieI, piernaD, rodillaD, pieD, brazoI, antebrazoI, manoI;
    private Transform[] medias = new Transform[4], nudillos = new Transform[4];

    void Start()
    {
        mano = animator.GetBoneTransform(HumanBodyBones.RightHand);
        codo = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        cabeza = animator.GetBoneTransform(HumanBodyBones.Head);
        cadera = animator.GetBoneTransform(HumanBodyBones.Hips);
        cuello = animator.GetBoneTransform(HumanBodyBones.Neck);
        piernaI = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg); rodillaI = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg); pieI = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        piernaD = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg); rodillaD = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg); pieD = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        brazoI = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm); antebrazoI = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm); manoI = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        pecho = animator.GetBoneTransform(HumanBodyBones.UpperChest);
        if(pecho == null) pecho = animator.GetBoneTransform(HumanBodyBones.Chest);
        indice = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
        menique = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
        HumanBodyBones[] m = { HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightLittleIntermediate };
        HumanBodyBones[] p = { HumanBodyBones.RightIndexProximal, HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightRingProximal, HumanBodyBones.RightLittleProximal };
        for(int i = 0; i < 4; i++) { medias[i] = animator.GetBoneTransform(m[i]); nudillos[i] = animator.GetBoneTransform(p[i]); }
    }

    void LateUpdate()
    {
        if(escoba == null || escoba.gameObject.activeInHierarchy == false || mano == null || indice == null || menique == null) return;

        // Colgado o subiendo: a la espalda
        bool conversando = movimiento != null && movimiento.enabled == false && (agarre == null || agarre.colgado == false);
        if(((agarre != null && (agarre.colgado || agarre.subiendo)) || conversando) && pecho != null)
        {
            if(escoba.parent != pecho) escoba.SetParent(pecho, false);
            escoba.localPosition = new Vector3(posicionEspalda.x / pecho.lossyScale.x, posicionEspalda.y / pecho.lossyScale.y, posicionEspalda.z / pecho.lossyScale.z);
            escoba.localRotation = Quaternion.Euler(giroEspalda);
            return;
        }
        if(escoba.parent != mano) escoba.SetParent(mano, false);

        // Muñeca: fuera del golpe, en la linea del antebrazo e inclinada hacia adelante, sin chocar el cuerpo
        AnimatorStateInfo estado = animator.GetCurrentAnimatorStateInfo(0);
        bool golpe = estado.IsName("Ataque01") || (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("Ataque01"));
        if(golpe == false)
        {
            Vector3 antebrazo = (mano.position - codo.position).normalized;
            Vector3 adelante = animator.transform.forward, derecha = animator.transform.right;
            // Corriendo, el brazo va muy atras: la escoba se lleva hacia afuera y casi horizontal
            bool corriendo = animator.GetFloat("Speed") > 3f;
            Vector3[] opciones = {
                corriendo ? (adelante + derecha * 0.5f + Vector3.up * 0.1f).normalized : (antebrazo + adelante * haciaAdelante).normalized,
                (antebrazo + adelante * haciaAdelante + derecha * 0.6f).normalized,
                (adelante + derecha * 0.3f - Vector3.up * 0.5f).normalized,
                (adelante + derecha * 0.6f).normalized,
                (derecha + adelante * 0.3f).normalized,
            };
            Vector3 elegido = opciones[0];
            foreach(Vector3 o in opciones)
                if(ChocaCuerpo(mano.position, o) == false) { elegido = o; break; }
            Vector3 actual = (indice.position - menique.position).normalized;
            float angulo = Vector3.Angle(actual, elegido);
            Vector3 deseado = Vector3.RotateTowards(actual, elegido, maximoDoblez * Mathf.Deg2Rad, 0f);
            if(angulo > 0.1f) mano.rotation = Quaternion.FromToRotation(actual, deseado) * mano.rotation;
        }
        // Las cerdas no se meten en el suelo: se levanta la muñeca lo justo
        {
            Vector3 dir = (indice.position - menique.position).normalized;
            float suelo = animator.transform.position.y + 0.06f;
            float punta = mano.position.y + dir.y * largo;
            if(punta < suelo && dir.y < 0f)
            {
                float dirYMin = Mathf.Clamp((suelo - mano.position.y) / largo, -1f, 1f);
                Vector3 horizontal = new Vector3(dir.x, 0f, dir.z).normalized;
                Vector3 nueva = (horizontal * Mathf.Sqrt(1f - dirYMin * dirYMin) + Vector3.up * dirYMin).normalized;
                mano.rotation = Quaternion.FromToRotation(dir, nueva) * mano.rotation;
            }
        }

        Vector3 centroMedias = Vector3.zero, centroNudillos = Vector3.zero;
        for(int i = 0; i < 4; i++) { centroMedias += medias[i].position; centroNudillos += nudillos[i].position; }
        centroMedias /= 4f; centroNudillos /= 4f;
        Vector3 centro = Vector3.Lerp(centroMedias, centroNudillos, haciaLosNudillos);
        Vector3 eje = (indice.position - menique.position).normalized;
        Vector3 haciaDedos = centroMedias - mano.position; // da el "giro" de las cerdas
        escoba.position = centro;
        escoba.rotation = Quaternion.LookRotation(eje, haciaDedos);
    }

    // ¿La escoba (desde la mano, en esa direccion) atravesaria el cuerpo? (cabeza, torso, piernas, otro brazo)
    bool ChocaCuerpo(Vector3 desde, Vector3 dir)
    {
        for(float d = 0.25f; d <= largo; d += 0.08f)
        {
            Vector3 p = desde + dir * d;
            if(cabeza != null && Vector3.Distance(p, cabeza.position + Vector3.up * 0.1f) < 0.17f) return true;
            if(Capsula(p, cadera, cuello, 0.17f)) return true;
            if(Capsula(p, piernaI, rodillaI, 0.13f) || Capsula(p, rodillaI, pieI, 0.11f)) return true;
            if(Capsula(p, piernaD, rodillaD, 0.13f) || Capsula(p, rodillaD, pieD, 0.11f)) return true;
            if(Capsula(p, brazoI, antebrazoI, 0.07f) || Capsula(p, antebrazoI, manoI, 0.06f)) return true;
        }
        return false;
    }

    static bool Capsula(Vector3 p, Transform t0, Transform t1, float radio)
    {
        if(t0 == null || t1 == null) return false;
        Vector3 ab = t1.position - t0.position;
        float k = Mathf.Clamp01(Vector3.Dot(p - t0.position, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
        return Vector3.Distance(p, t0.position + ab * k) < radio;
    }
}
