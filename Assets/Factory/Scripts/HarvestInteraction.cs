using UnityEngine;

public class HarvestInteraction : MonoBehaviour
{
    public KeyCode harvestKey = KeyCode.H;
    public float harvestDistance = 6f;
    public float hitInterval = 0.6f;
    public int oreAmountPerHit = 10;
    private Camera cam;
    private string promptText = "";
    private float nextHitTime;
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

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, harvestDistance)) return;

        bool doHit = Input.GetKey(harvestKey) && Time.time >= nextHitTime;
        string holdHint = "[" + harvestKey + " halten] ";

        HarvestableTree tree = hit.collider.GetComponentInParent<HarvestableTree>();
        if (tree != null && !tree.IsFelled)
        {
            promptText = holdHint + "Baum faellen (" + tree.woodAmount + " Holz)";
            if (doHit)
            {
                int wood = tree.Hit(hit.point, ray.direction);
                if (wood > 0) FactoryItemBridge.Add("Wood", wood);
                nextHitTime = Time.time + hitInterval;
            }
            return;
        }

        HarvestableRock rock = hit.collider.GetComponentInParent<HarvestableRock>();
        if (rock != null && !rock.IsDepleted)
        {
            promptText = holdHint + rock.displayName + " abbauen (" + rock.amount + ")";
            if (doHit)
            {
                int mined = rock.Hit(hit.point, ray.direction);
                if (mined > 0) FactoryItemBridge.Add(rock.itemName, mined);
                nextHitTime = Time.time + hitInterval;
            }
            return;
        }

        ResourceNode node = hit.collider.GetComponentInParent<ResourceNode>();
        if (node != null)
        {
            promptText = holdHint + node.type + " abbauen (" + node.amount + ")";
            if (doHit)
            {
                int mined = node.Mine(oreAmountPerHit);
                if (mined > 0) FactoryItemBridge.Add(node.type.ToString(), mined);
                nextHitTime = Time.time + hitInterval;
            }
        }
    }
    void OnGUI()
    {
        if (string.IsNullOrEmpty(promptText)) return;
        GUI.color = Color.white;
        GUI.Label(new Rect(Screen.width * 0.5f - 150f, Screen.height * 0.5f + 20f, 300f, 24f), promptText);
    }
}
