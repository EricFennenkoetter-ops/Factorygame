using UnityEngine;

public class HarvestInteraction : MonoBehaviour
{
    public KeyCode harvestKey = KeyCode.H;
    public float harvestDistance = 5f;
    public int harvestAmountPerPress = 10;
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

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, harvestDistance)) return;

        ResourceNode node = hit.collider.GetComponentInParent<ResourceNode>();
        if (node != null)
        {
            promptText = "[" + harvestKey + "] " + node.type + " abbauen";
            if (Input.GetKeyDown(harvestKey))
            {
                int mined = node.Mine(harvestAmountPerPress);
                if (mined > 0)
                    FactoryItemBridge.Add(node.type.ToString(), mined);
            }
            return;
        }

        HarvestableTree tree = hit.collider.GetComponentInParent<HarvestableTree>();
        if (tree != null)
        {
            promptText = "[" + harvestKey + "] Holz sammeln";
            if (Input.GetKeyDown(harvestKey))
            {
                int wood = tree.Chop(harvestAmountPerPress);
                if (wood > 0)
                    FactoryItemBridge.Add("Wood", wood);
            }
        }
    }
    void OnGUI()
    {
        if (string.IsNullOrEmpty(promptText)) return;
        GUI.color = Color.white;
        GUI.Label(new Rect(Screen.width * 0.5f - 100f, Screen.height * 0.5f + 20f, 200f, 24f), promptText);
    }
}
