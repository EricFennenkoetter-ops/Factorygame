#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

// Menue: Tools > Casino Sim > Camera To Eye Height
// - setzt cameraMovementScript.headheight auf Kopf-/Augenhoehe des X-Bot
// - haengt CameraFrontOffset ans Camera-Holder-Objekt und schiebt die Kamera
//   minimal vor den Kopf (damit man aus dem Kopf statt Hals schaut)
public static class CameraToEyeHeight
{
    [MenuItem("Tools/Casino Sim/Camera To Eye Height")]
    public static void Run()
    {
        var camMove = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .FirstOrDefault(m => m != null && m.GetType().Name == "cameraMovementScript");
        if (camMove == null)
        {
            EditorUtility.DisplayDialog("Eye Height", "cameraMovementScript nicht in der Szene gefunden.", "OK");
            return;
        }

        var soMove = new SerializedObject(camMove);
        var refObj = soMove.FindProperty("refferenceObject")?.objectReferenceValue as Transform;   // cameraPos
        if (refObj == null)
        {
            EditorUtility.DisplayDialog("Eye Height", "cameraMovementScript.refferenceObject ist leer.", "OK");
            return;
        }

        var xbot = GameObject.Find("X Bot");
        Transform head = xbot != null
            ? xbot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.EndsWith("Head"))
            : null;
        if (head == null)
        {
            EditorUtility.DisplayDialog("Eye Height", "Kopf-Bone (mixamorig:Head) nicht gefunden.", "OK");
            return;
        }

        // Kamera ziel: Mitte Kopf-Bone (nicht Hals) -> voller Bone-Abstand, kein Abzug
        float newHeadHeight = head.position.y - refObj.position.y;

        Undo.RecordObject(camMove, "Camera Eye Height");
        soMove.Update();
        soMove.FindProperty("headheight").floatValue = newHeadHeight;
        soMove.ApplyModifiedProperties();
        EditorUtility.SetDirty(camMove);

        // orientation aus CameraRotation holen
        var camRot = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .FirstOrDefault(m => m != null && m.GetType().Name == "CameraRotation");
        Transform orientation = camRot != null
            ? new SerializedObject(camRot).FindProperty("orientation")?.objectReferenceValue as Transform
            : null;

        // CameraFrontOffset ans Camera-Holder-Objekt
        var holder = camMove.gameObject;
        var offset = holder.GetComponent<CameraFrontOffset>();
        if (offset == null) offset = Undo.AddComponent<CameraFrontOffset>(holder);
        offset.orientation = orientation;
        offset.forward = 0.45f;
        offset.up = 0f;
        EditorUtility.SetDirty(offset);

        // Kopf ausblenden, damit er nicht ins Bild ragt (schrumpft den Kopf-Bone zur Laufzeit)
        var look = xbot.GetComponentsInChildren<MonoBehaviour>(true)
            .FirstOrDefault(m => m != null && m.GetType().Name == "HeadLook");
        if (look != null)
        {
            var soLook = new SerializedObject(look);
            var p = soLook.FindProperty("hideHead");
            if (p != null) { p.boolValue = true; soLook.ApplyModifiedProperties(); EditorUtility.SetDirty(look); }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(holder.scene);

        Debug.Log($"[CameraToEyeHeight] headheight={newHeadHeight:0.00}  frontOffset=0.25  " +
                  $"orientation={(orientation ? orientation.name : "FEHLT")}");
        EditorUtility.DisplayDialog("Eye Height",
            $"headheight = {newHeadHeight:0.00}\nCameraFrontOffset (0.25 m vor den Kopf) am '{holder.name}'.\n\n" +
            "Play druecken. Feintuning:\n" +
            "- zu tief/hoch -> CameraFrontOffset.up (+/-)\n" +
            "- Kopf ragt noch ins Bild -> forward etwas groesser", "OK");
    }
}
#endif
