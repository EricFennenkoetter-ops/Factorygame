using UnityEngine;

// Schiebt die Kamera minimal nach vorne (nur Blickrichtung-Yaw, kein Kippen) und
// optional etwas hoeher/tiefer - damit man aus dem Kopf statt aus dem Hals schaut
// und der eigene Kopf nicht ins Bild ragt.
// Gehoert auf dasselbe Objekt wie cameraMovementScript (Camera Holder). Laeuft in
// LateUpdate, also NACH cameraMovementScript.
[DefaultExecutionOrder(200)]
public class CameraFrontOffset : MonoBehaviour
{
    [Tooltip("Yaw-only Transform (das 'orientation' aus CameraRotation)")]
    public Transform orientation;

    [Tooltip("Wie weit vor den Kopf (Meter)")]
    public float forward = 0.25f;

    [Tooltip("Feinjustierung Hoehe (Meter, + = hoeher)")]
    public float up = 0f;

    void LateUpdate()
    {
        if (orientation != null)
            transform.position += orientation.forward * forward;
        if (up != 0f)
            transform.position += Vector3.up * up;
    }
}
