using UnityEditor;
using UnityEngine;

// Herramienta de EDITOR: imprime en consola un reporte para verificar la Fase 1.
public static class VerificadorF1
{
    [MenuItem("Bad Carrete Day/F1 - Verificar")]
    public static void Verificar()
    {
        // Avatares de personajes
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/BadCarreteDay/Arte/Personajes" }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(ruta))
            {
                Avatar a = obj as Avatar;
                if (a != null) Debug.Log("[Verif] Avatar " + ruta + " valido=" + a.isValid + " humano=" + a.isHuman);
            }
        }

        // Clips de animacion
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/BadCarreteDay/Arte/Animaciones" }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(ruta))
            {
                AnimationClip c = obj as AnimationClip;
                if (c != null && !c.name.StartsWith("__preview__"))
                    Debug.Log("[Verif] Clip " + c.name + " humano=" + c.humanMotion + " largo=" + c.length.ToString("F2") + "s loop=" + c.isLooping);
            }
        }

        // Prefab del jugador: altura y materiales
        GameObject jugador = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BadCarreteDay/Prefabs/JugadorAj.prefab");
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(jugador);
        Bounds b = new Bounds();
        bool primero = true;
        foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
        {
            if (primero) { b = r.bounds; primero = false; } else b.Encapsulate(r.bounds);
            foreach (Material m in r.sharedMaterials)
                Debug.Log("[Verif] Renderer " + r.name + " material=" + (m ? m.name : "NULL") + " shader=" + (m ? m.shader.name : "-"));
        }
        Animator anim = inst.GetComponentInChildren<Animator>();
        Debug.Log("[Verif] Altura Aj=" + b.size.y.ToString("F2") + "m  avatar=" + (anim.avatar ? anim.avatar.name : "NULL") + " controller=" + anim.runtimeAnimatorController.name);
        foreach (AnimatorControllerParameter p in ((UnityEditor.Animations.AnimatorController)anim.runtimeAnimatorController).parameters)
            Debug.Log("[Verif] Param " + p.name + " " + p.type);
        Object.DestroyImmediate(inst);
    }
}
