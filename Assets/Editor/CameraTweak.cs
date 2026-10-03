#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

// Menue: Tools > Casino Sim > Camera Weiter Vor + Kopf Weg
public static class CameraTweak
{
    [MenuItem("Tools/Casino Sim/Camera Weiter Vor + Kopf Weg")]
    public static void Run()
    {
        var holder = Object.FindObjectsByType<CameraFrontOffset>(FindObjectsSortMode.None).FirstOrDefault();
        if (holder != null)
        {
            Undo.RecordObject(holder, "Camera forward");
            holder.forward = 0.5f;
            EditorUtility.SetDirty(holder);
        }

        var xbot = GameObject.Find("X Bot");
        var look = xbot != null
            ? xbot.GetComponentsInChildren<MonoBehaviour>(true)
                  .FirstOrDefault(m => m != null && m.GetType().Name == "HeadLook")
            : null;
        if (look != null)
        {
            var so = new SerializedObject(look);
            var p = so.FindProperty("hideHead");
            if (p != null) { p.boolValue = true; so.ApplyModifiedProperties(); EditorUtility.SetDirty(look); }
        }

        if (holder != null)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(holder.gameObject.scene);

        Debug.Log($"[CameraTweak] forward={(holder ? holder.forward.ToString() : "?")}  " +
                  $"hideHead={(look != null ? "true" : "HeadLook FEHLT")}");
        EditorUtility.DisplayDialog("Camera Tweak",
            "Kamera 0.5 m vor den Kopf + Kopf-Bone ausgeblendet.\nPlay druecken.\n\n" +
            "Feintuning: CameraFrontOffset.forward am 'Camera Holder'.", "OK");
    }
}
#endif
