using UnityEngine;

// Dreht Wirbelsaeule, Nacken und Kopf des Characters zur Blickrichtung der Kamera.
// Braucht KEIN Animation-Rigging-Package - arbeitet in LateUpdate direkt auf den Bones,
// nachdem der Animator die aktuelle Pose geschrieben hat.
//
// Setup: "aimSource" = die Kamera (das Transform, auf dem CameraRotation sitzt).
//        Die Bones per Drag&Drop aus dem Character-Skelett zuweisen
//        (mixamorig:Spine, mixamorig:Spine1, mixamorig:Spine2, mixamorig:Neck, mixamorig:Head).
public class HeadLook : MonoBehaviour
{
    [Header("Referenzen")]
    public Transform aimSource;          // Kamera
    public Transform head;               // mixamorig:Head
    public Transform[] spineChain;       // z.B. Spine1, Spine2, Neck (ohne Head)

    [Header("Einstellungen")]
    [Range(0f, 1f)] public float weight = 1f;
    public float maxYaw = 70f;           // max. horizontale Kopfdrehung (Grad)
    public float maxPitch = 55f;         // max. vertikale Kopfdrehung (Grad)
    public float smooth = 12f;

    [Header("First-Person")]
    // Kopf normal lassen (false). Nur einschalten, wenn beim Hochschauen der eigene
    // Kopf/Nase ins Bild ragt UND es dich stoert (schrumpft den Bone fuer ALLE Kameras,
    // d.h. auch der Schatten wird dann kopflos).
    public bool hideHead = false;
    public Transform[] extraHiddenBones;

    float curYaw, curPitch;

    void LateUpdate()
    {
        if (aimSource == null || head == null) return;

        // Blickrichtung der Kamera relativ zur Koerperausrichtung dieses Objekts
        Vector3 localDir = transform.InverseTransformDirection(aimSource.forward);
        if (localDir.sqrMagnitude < 1e-6f) return;
        localDir.Normalize();

        float targetYaw = Mathf.Clamp(Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg, -maxYaw, maxYaw);
        float targetPitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg, -maxPitch, maxPitch);

        float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        curYaw = Mathf.Lerp(curYaw, targetYaw, t);
        curPitch = Mathf.Lerp(curPitch, targetPitch, t);

        int bones = (spineChain != null ? spineChain.Length : 0) + 1;
        float wPer = weight / bones;

        if (spineChain != null)
        {
            foreach (var b in spineChain)
            {
                if (b == null) continue;
                b.rotation = Quaternion.AngleAxis(curYaw * wPer, transform.up)
                           * Quaternion.AngleAxis(curPitch * wPer, transform.right)
                           * b.rotation;
            }
        }

        head.rotation = Quaternion.AngleAxis(curYaw * wPer, transform.up)
                      * Quaternion.AngleAxis(curPitch * wPer, transform.right)
                      * head.rotation;

        if (hideHead)
        {
            head.localScale = new Vector3(1e-4f, 1e-4f, 1e-4f);
            if (extraHiddenBones != null)
                foreach (var b in extraHiddenBones)
                    if (b != null) b.localScale = new Vector3(1e-4f, 1e-4f, 1e-4f);
        }
    }
}
