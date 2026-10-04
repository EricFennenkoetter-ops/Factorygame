using System.Collections.Generic;
using UnityEngine;

public class BuildModeController : MonoBehaviour
{
    [Header("Steuerung")]
    public KeyCode toggleKey = KeyCode.B;
    public KeyCode placeKey = KeyCode.Mouse0;
    public KeyCode removeKey = KeyCode.Mouse1;
    public KeyCode rotateKey = KeyCode.R;
    [Header("Raster")]
    public float baseGridUnit = 2f;
    public float gridExtent = 28f;
    public float raycastDistance = 60f;
    public LayerMask placementMask = ~0;
    public Color gridColor = new Color(0.25f, 0.9f, 1f, 0.55f);
    public Color validPreviewColor = new Color(0.3f, 1f, 0.3f, 0.55f);
    public Color invalidPreviewColor = new Color(1f, 0.25f, 0.25f, 0.55f);
    [Header("Maschinen")]
    public MachineDefinition[] machines;
    public bool BuildModeActive { get; private set; }
    private Camera cam;
    private GameObject gridGO;
    private Mesh gridMesh;
    private GameObject previewGO;
    private Renderer previewRenderer;
    private Transform placedMachinesParent;
    private int selectedIndex;
    private float currentRotationY;
    private Vector3 lastGridCenter = new Vector3(float.NaN, 0f, 0f);
    private bool currentValidPlacement;
    private Vector3 currentPlacementPoint;
    private readonly List<GameObject> placedMachines = new List<GameObject>();
    void Start()
    {
        cam = ResolveCamera();
        if (machines == null || machines.Length == 0)
            machines = MachineDefinition.BuildDefaultSet();

        placedMachinesParent = new GameObject("PlacedMachines").transform;
        placedMachinesParent.SetParent(transform);
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

        if (Input.GetKeyDown(toggleKey)) SetBuildMode(!BuildModeActive);
        if (!BuildModeActive) return;

        if (cam == null) cam = ResolveCamera();
        if (cam == null) return;

        HandleSelection();
        UpdatePreviewAndGrid();

        if (Input.GetKeyDown(placeKey)) TryPlace();
        if (Input.GetKeyDown(removeKey)) TryRemove();
    }
    private void SetBuildMode(bool active)
    {
        BuildModeActive = active;

        if (active)
        {
            if (gridGO == null) BuildGridVisual();
            if (previewGO == null) BuildPreview();
            gridGO.SetActive(true);
            previewGO.SetActive(true);
            lastGridCenter = new Vector3(float.NaN, 0f, 0f);
        }
        else
        {
            if (gridGO != null) gridGO.SetActive(false);
            if (previewGO != null) previewGO.SetActive(false);
        }
    }
    private void HandleSelection()
    {
        for (int i = 0; i < machines.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) selectedIndex = i;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            int dir = scroll > 0f ? 1 : -1;
            selectedIndex = (selectedIndex + dir + machines.Length) % machines.Length;
        }

        if (Input.GetKeyDown(rotateKey)) currentRotationY = (currentRotationY + 90f) % 360f;
    }
    private void UpdatePreviewAndGrid()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, placementMask, QueryTriggerInteraction.Ignore))
        {
            previewGO.SetActive(false);
            currentValidPlacement = false;
            return;
        }

        previewGO.SetActive(true);
        MachineDefinition def = machines[selectedIndex];
        Vector2 footprint = GetEffectiveFootprint(def);

        Vector3 snapped = SnapToGrid(hit.point, footprint);
        currentPlacementPoint = snapped;

        previewGO.transform.position = new Vector3(snapped.x, snapped.y + def.height * 0.5f, snapped.z);
        previewGO.transform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);
        Vector2 visual = def.GetVisualSize();
        previewGO.transform.localScale = new Vector3(visual.x, def.height, visual.y);

        currentValidPlacement = !IsOverlapping(snapped, footprint);
        previewRenderer.sharedMaterial.color = currentValidPlacement ? validPreviewColor : invalidPreviewColor;

        Vector3 gridCenterCandidate = new Vector3(snapped.x, hit.point.y + 0.05f, snapped.z);
        if (float.IsNaN(lastGridCenter.x) || (gridCenterCandidate - lastGridCenter).sqrMagnitude > baseGridUnit * baseGridUnit)
        {
            RebuildGridMesh(gridCenterCandidate);
            lastGridCenter = gridCenterCandidate;
        }
    }
    private Vector2 GetEffectiveFootprint(MachineDefinition def)
    {
        bool rotated90 = Mathf.RoundToInt(currentRotationY / 90f) % 2 != 0;
        float x = Mathf.Max(baseGridUnit, rotated90 ? def.footprint.y : def.footprint.x);
        float z = Mathf.Max(baseGridUnit, rotated90 ? def.footprint.x : def.footprint.y);
        return new Vector2(x, z);
    }
    private Vector3 SnapToGrid(Vector3 p, Vector2 footprint)
    {
        float snappedY = Mathf.Round(p.y / baseGridUnit) * baseGridUnit;
        return new Vector3(SnapAxis(p.x, footprint.x), snappedY, SnapAxis(p.z, footprint.y));
    }
    private float SnapAxis(float p, float size)
    {
        int cellsSpan = Mathf.Max(1, Mathf.RoundToInt(size / baseGridUnit));
        if (cellsSpan % 2 != 0)
            return (Mathf.Floor(p / baseGridUnit) + 0.5f) * baseGridUnit;
        return Mathf.Round(p / baseGridUnit) * baseGridUnit;
    }
    private void BuildGridVisual()
    {
        gridGO = new GameObject("BuildGrid");
        gridGO.transform.SetParent(transform);

        MeshFilter mf = gridGO.AddComponent<MeshFilter>();
        MeshRenderer mr = gridGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = CreateUnlitTransparentMaterial(gridColor);

        gridMesh = new Mesh();
        gridMesh.name = "BuildGridMesh";
        gridMesh.MarkDynamic();
        mf.sharedMesh = gridMesh;
    }
    private void RebuildGridMesh(Vector3 worldCenter)
    {
        int cells = Mathf.Max(1, Mathf.RoundToInt(gridExtent / baseGridUnit));
        float centerX = Mathf.Round(worldCenter.x / baseGridUnit) * baseGridUnit;
        float centerZ = Mathf.Round(worldCenter.z / baseGridUnit) * baseGridUnit;
        float half = cells * baseGridUnit;

        List<Vector3> verts = new List<Vector3>();
        List<int> indices = new List<int>();

        for (int i = -cells; i <= cells; i++)
        {
            float x = centerX + i * baseGridUnit;
            int a = verts.Count; verts.Add(new Vector3(x, 0f, centerZ - half));
            int b = verts.Count; verts.Add(new Vector3(x, 0f, centerZ + half));
            indices.Add(a); indices.Add(b);

            float z = centerZ + i * baseGridUnit;
            int c = verts.Count; verts.Add(new Vector3(centerX - half, 0f, z));
            int d = verts.Count; verts.Add(new Vector3(centerX + half, 0f, z));
            indices.Add(c); indices.Add(d);
        }

        gridMesh.Clear();
        gridMesh.SetVertices(verts);
        gridMesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
        gridMesh.RecalculateBounds();

        gridGO.transform.position = new Vector3(0f, worldCenter.y, 0f);
    }
    private void BuildPreview()
    {
        previewGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        previewGO.name = "BuildPreview";
        Object.Destroy(previewGO.GetComponent<Collider>());
        previewGO.transform.SetParent(transform);
        previewRenderer = previewGO.GetComponent<Renderer>();
        previewRenderer.material = CreateUnlitTransparentMaterial(validPreviewColor);
    }
    private Material CreateUnlitTransparentMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        Material mat = new Material(shader);
        mat.color = color;

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        mat.renderQueue = 3000;
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        return mat;
    }
    private void TryPlace()
    {
        if (!currentValidPlacement) return;

        MachineDefinition def = machines[selectedIndex];
        GameObject go;
        if (def.prefab != null)
        {
            go = Instantiate(def.prefab, currentPlacementPoint, Quaternion.Euler(0f, currentRotationY, 0f), placedMachinesParent);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(placedMachinesParent);
            go.transform.position = new Vector3(currentPlacementPoint.x, currentPlacementPoint.y + def.height * 0.5f, currentPlacementPoint.z);
            go.transform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);
            Vector2 visual = def.GetVisualSize();
            go.transform.localScale = new Vector3(visual.x, def.height, visual.y);

            Renderer r = go.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = def.color;
            r.sharedMaterial = mat;
        }
        go.name = "Machine_" + def.machineName;

        MachineNode node = go.AddComponent<MachineNode>();
        node.machineType = def.machineName;
        node.footprint = GetEffectiveFootprint(def);

        placedMachines.Add(go);
    }
    private void TryRemove()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance)) return;

        MachineNode node = hit.collider.GetComponentInParent<MachineNode>();
        if (node == null) return;

        placedMachines.Remove(node.gameObject);
        Destroy(node.gameObject);
    }
    private bool IsOverlapping(Vector3 pos, Vector2 footprint)
    {
        Rect newRect = new Rect(pos.x - footprint.x * 0.5f, pos.z - footprint.y * 0.5f, footprint.x, footprint.y);

        foreach (GameObject m in placedMachines)
        {
            if (m == null) continue;
            MachineNode node = m.GetComponent<MachineNode>();
            Vector2 fp = node != null ? node.footprint : new Vector2(baseGridUnit, baseGridUnit);
            Rect existingRect = new Rect(m.transform.position.x - fp.x * 0.5f, m.transform.position.z - fp.y * 0.5f, fp.x, fp.y);
            if (newRect.Overlaps(existingRect)) return true;
        }

        return false;
    }
    void OnGUI()
    {
        if (!BuildModeActive || machines == null || machines.Length == 0) return;

        string label = "BAUMODUS - Ausgewaehlt: " + machines[selectedIndex].machineName + "\n" +
                        "1-" + machines.Length + ": Maschine waehlen | Mausrad: wechseln | R: drehen\n" +
                        "Linksklick: bauen | Rechtsklick: entfernen | B: Baumodus verlassen";

        GUI.color = Color.white;
        GUI.Label(new Rect(10, 10, 500, 60), label);
    }
}
