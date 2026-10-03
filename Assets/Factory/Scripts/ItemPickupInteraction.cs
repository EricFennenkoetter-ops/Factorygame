using UnityEngine;

public class ItemPickupInteraction : MonoBehaviour
{
    public KeyCode pickupKey = KeyCode.E;
    public float pickupRange = 4f;
    private Camera cam;
    private string promptText = "";
    void Start()
    {
        cam = ResolveCamera();
    }
    private Camera ResolveCamera()
    {
        if (Camera.main != null) return Camera.main;
        CameraRotation camRot = Object.FindFirstObjectByType<CameraRotation>();
        if (camRot != null)
        {
            Camera c = camRot.GetComponent<Camera>();
            if (c != null) return c;
        }
        return Object.FindFirstObjectByType<Camera>();
    }
    void Update()
    {
        if (cam == null) cam = ResolveCamera();
        if (cam == null) return;

        promptText = "";
        DroppedItemPickup nearest = FindNearestDrop();
        if (nearest == null) return;

        string label = nearest.sourceItem != null ? nearest.sourceItem.name : "Item";
        promptText = "[" + pickupKey + "] " + label + " aufheben";

        if (Input.GetKeyDown(pickupKey))
        {
            if (nearest.logicScript != null && nearest.sourceItem != null)
                nearest.logicScript.changeItemInHotbar(nearest.sourceItem, nearest.amount);

            Destroy(nearest.gameObject);
        }
    }
    private DroppedItemPickup FindNearestDrop()
    {
        Vector3 origin = cam.transform.position;
        Collider[] hits = Physics.OverlapSphere(origin, pickupRange);

        DroppedItemPickup closest = null;
        float closestSqrDist = float.MaxValue;

        foreach (Collider col in hits)
        {
            DroppedItemPickup drop = col.GetComponentInParent<DroppedItemPickup>();
            if (drop == null) continue;

            float sqrDist = (drop.transform.position - origin).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closest = drop;
            }
        }

        return closest;
    }
    void OnGUI()
    {
        if (string.IsNullOrEmpty(promptText)) return;
        GUI.color = Color.white;
        GUI.Label(new Rect(Screen.width * 0.5f - 100f, Screen.height * 0.5f + 44f, 200f, 24f), promptText);
    }
}
