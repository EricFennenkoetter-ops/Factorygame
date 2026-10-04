using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FactoryMapSetup
{
    private const string ScenePath = "Assets/Scenes/FactoryMap.unity";
    [MenuItem("Factory/Setup Build Mode Only (No Regenerate)")]
    public static void SetupBuildModeOnly()
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        GameObject buildGO = GameObject.Find("Factory Build Manager");
        if (buildGO == null)
        {
            buildGO = new GameObject("Factory Build Manager");
            Debug.Log("'Factory Build Manager' angelegt.");
        }

        BuildModeController controller = buildGO.GetComponent<BuildModeController>();
        if (controller == null)
        {
            controller = buildGO.AddComponent<BuildModeController>();
            Debug.Log("BuildModeController hinzugefuegt.");
        }
        else
        {
            Debug.Log("BuildModeController war schon vorhanden.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Baumodus eingerichtet - Taste 'B' sollte jetzt funktionieren. Scene gespeichert (" + scene.name + ").");
    }
    [MenuItem("Factory/Setup Crafting Menu Only (No Regenerate)")]
    public static void SetupCraftingOnly()
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        GameObject buildGO = GameObject.Find("Factory Build Manager");
        if (buildGO == null)
        {
            buildGO = new GameObject("Factory Build Manager");
            Debug.Log("'Factory Build Manager' angelegt.");
        }

        AddGameplaySystems(buildGO);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Crafting-System eingerichtet - Taste 'H' zum Abbauen, Taste 'C' oeffnet das Crafting-Menue. Scene gespeichert (" + scene.name + ").");
    }
    private static void AddGameplaySystems(GameObject managerGO)
    {
        if (managerGO.GetComponent<HarvestInteraction>() == null)
            managerGO.AddComponent<HarvestInteraction>();

        if (managerGO.GetComponent<CraftingMenuController>() == null)
            managerGO.AddComponent<CraftingMenuController>();

        if (managerGO.GetComponent<ItemPickupInteraction>() == null)
            managerGO.AddComponent<ItemPickupInteraction>();
    }
    [MenuItem("Factory/Generate New Map (New Scene)")]
    public static void GenerateNewMapScene()
    {
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Ungespeicherte Aenderungen",
                "Die aktuelle Scene hat ungespeicherte Aenderungen. Trotzdem eine neue Scene fuer die Factory-Map erstellen?",
                "Fortfahren", "Abbrechen");
            if (!proceed) return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        BuildTerrainAndGenerate(Vector3.zero);

        EnsureFolder("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("Factory-Map Scene gespeichert unter: " + ScenePath);
    }
    [MenuItem("Factory/Bake Map Into SampleScene (One-Time)")]
    public static void BakeMapIntoSampleScene()
    {
        const string samplePath = "Assets/Scenes/SampleScene.unity";
        if (!System.IO.File.Exists(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), samplePath)))
        {
            Debug.LogError("SampleScene.unity nicht gefunden unter " + samplePath);
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(samplePath, OpenSceneMode.Single);

        GameObject existingRoot = GameObject.Find("FactoryMap");
        if (existingRoot != null)
        {
            if (!Application.isBatchMode)
            {
                bool redo = EditorUtility.DisplayDialog(
                    "Factory-Map existiert bereits",
                    "In der SampleScene ist bereits eine 'FactoryMap' vorhanden. Neu generieren und die alte ersetzen?",
                    "Neu generieren", "Abbrechen");
                if (!redo) return;
            }
            Terrain oldTerrain = existingRoot.GetComponentInChildren<Terrain>();
            string oldTerrainPath = oldTerrain != null ? AssetDatabase.GetAssetPath(oldTerrain.terrainData) : null;
            Object.DestroyImmediate(existingRoot);
            if (!string.IsNullOrEmpty(oldTerrainPath)) AssetDatabase.DeleteAsset(oldTerrainPath);
        }

        Vector3 mapRootOffset = new Vector3(3000f, 0f, -1000f);
        GameObject root = BuildTerrainAndGenerate(mapRootOffset);

        Terrain terrain = root.GetComponentInChildren<Terrain>();
        MapGenerator generator = terrain != null ? terrain.GetComponent<MapGenerator>() : null;

        PlayerMovementScript move = Object.FindFirstObjectByType<PlayerMovementScript>();
        if (move != null && terrain != null && generator != null)
        {

            if (move.ground.value != 0)
            {
                for (int i = 0; i < 32; i++)
                {
                    if ((move.ground.value & (1 << i)) != 0)
                    {
                        terrain.gameObject.layer = i;
                        break;
                    }
                }
            }

            Vector3 spawn = FindSpawnPoint(terrain, generator);

            ClearAreaAroundSpawn(root, spawn, 4f, 15f, keepTrees: 4, keepRocks: 3);

            GameObject spawnMarker = new GameObject("FactoryMap Spawn Point");
            spawnMarker.transform.SetParent(root.transform);
            spawnMarker.transform.position = spawn;

            GameObject playerRoot = move.transform.root.gameObject;

            GameObject casinoMarker = GameObject.Find("Casino Spawn Point");
            if (casinoMarker == null)
            {
                casinoMarker = new GameObject("Casino Spawn Point");
                casinoMarker.transform.position = playerRoot.transform.position;
            }

            CameraRotation camRot = Object.FindFirstObjectByType<CameraRotation>();
            GameObject camRoot = camRot != null ? camRot.transform.root.gameObject : null;

            CapsuleCollider cap = move.GetComponent<CapsuleCollider>();
            float scaleY = Mathf.Abs(playerRoot.transform.lossyScale.y);
            float rootY = cap != null
                ? spawn.y - cap.center.y * scaleY + cap.height * 0.5f * scaleY
                : spawn.y + 1f;
            Vector3 startPos = new Vector3(spawn.x, rootY, spawn.z);

            playerRoot.transform.position = startPos;
            if (camRoot != null && camRoot != playerRoot)
                camRoot.transform.position = startPos;

            GameObject teleportGO = GameObject.Find("FactoryMap Teleport") ?? new GameObject("FactoryMap Teleport");
            TeleportToFactoryMap teleport = teleportGO.GetComponent<TeleportToFactoryMap>() ?? teleportGO.AddComponent<TeleportToFactoryMap>();
            teleport.player = playerRoot.transform;
            teleport.spawnPoint = casinoMarker.transform;

            GameObject buildGO = GameObject.Find("Factory Build Manager") ?? new GameObject("Factory Build Manager");
            if (buildGO.GetComponent<BuildModeController>() == null)
                buildGO.AddComponent<BuildModeController>();
            AddGameplaySystems(buildGO);

            Debug.Log("Spieler startet jetzt direkt auf der Factory-Map bei " + startPos + ". Taste '" + teleport.teleportKey +
                      "' bringt zurueck zum Casino. Taste 'B' oeffnet den Baumodus, 'H' baut ab, 'C' oeffnet das Crafting-Menue.");
        }
        else
        {
            Debug.LogWarning("Kein PlayerMovementScript in der SampleScene gefunden - Teleport-Hotkey wurde nicht eingerichtet.");
        }

        if (generator != null) Object.DestroyImmediate(generator);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Factory-Map wurde fest in SampleScene.unity eingebacken. Kein erneutes Generieren noetig.");
    }
    [MenuItem("Factory/Regenerate Map In Current Scene (New Seed)")]
    public static void RegenerateInCurrentScene()
    {
        MapGenerator generator = Object.FindFirstObjectByType<MapGenerator>();
        if (generator == null)
        {
            Debug.LogWarning("Kein MapGenerator in der aktuellen Scene gefunden. Nutze 'Factory/Generate New Map (New Scene)' zuerst.");
            return;
        }

        generator.seed = Random.Range(0, int.MaxValue);
        generator.GenerateMap();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
    [MenuItem("Factory/Add Player From SampleScene")]
    public static void AddPlayerFromSampleScene()
    {
        if (Object.FindFirstObjectByType<Terrain>() == null && System.IO.File.Exists(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ScenePath)))
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Scene factoryScene = EditorSceneManager.GetActiveScene();
        if (Object.FindFirstObjectByType<Terrain>() == null)
        {
            Debug.LogError("Keine Terrain-Map in der aktuellen Scene gefunden. Erst 'Factory/Generate New Map (New Scene)' ausfuehren.");
            return;
        }

        const string samplePath = "Assets/Scenes/SampleScene.unity";
        if (!System.IO.File.Exists(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), samplePath)))
        {
            Debug.LogError("SampleScene.unity nicht gefunden unter " + samplePath);
            return;
        }

        Scene sampleScene = EditorSceneManager.OpenScene(samplePath, OpenSceneMode.Additive);

        PlayerMovementScript move = Object.FindObjectsByType<PlayerMovementScript>(FindObjectsSortMode.None)
            .FirstOrDefault(m => m.gameObject.scene == sampleScene);

        if (move == null)
        {
            Debug.LogError("Kein PlayerMovementScript in SampleScene.unity gefunden.");
            EditorSceneManager.CloseScene(sampleScene, true);
            return;
        }

        GameObject playerRoot = move.transform.root.gameObject;

        CameraRotation camRot = Object.FindObjectsByType<CameraRotation>(FindObjectsSortMode.None)
            .FirstOrDefault(c => c.gameObject.scene == sampleScene);
        GameObject camRoot = camRot != null ? camRot.transform.root.gameObject : null;

        List<GameObject> toMove = new List<GameObject> { playerRoot };
        if (camRoot != null && camRoot != playerRoot) toMove.Add(camRoot);

        foreach (GameObject go in toMove)
            SceneManager.MoveGameObjectToScene(go, factoryScene);

        EditorSceneManager.CloseScene(sampleScene, true);

        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        MapGenerator generator = terrain.GetComponent<MapGenerator>();

        if (move.ground.value != 0)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((move.ground.value & (1 << i)) != 0)
                {
                    terrain.gameObject.layer = i;
                    break;
                }
            }
        }

        Vector3 spawn = FindSpawnPoint(terrain, generator);
        ClearAreaAroundSpawn(terrain.transform.parent != null ? terrain.transform.parent.gameObject : terrain.gameObject, spawn, 4f, 15f, keepTrees: 4, keepRocks: 3);

        CapsuleCollider cap = move.GetComponent<CapsuleCollider>();
        float scaleY = Mathf.Abs(playerRoot.transform.lossyScale.y);
        float rootY = cap != null
            ? spawn.y - cap.center.y * scaleY + cap.height * 0.5f * scaleY
            : spawn.y + 1f;

        playerRoot.transform.position = new Vector3(spawn.x, rootY, spawn.z);
        if (camRoot != null && camRoot != playerRoot)
            camRoot.transform.position = new Vector3(spawn.x, rootY, spawn.z);

        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Character/X Bot.fbx") != null)
        {
            PlaceCharacterInScene.Run();
        }

        EditorSceneManager.SaveScene(factoryScene);
        Debug.Log("Spieler wurde bei " + playerRoot.transform.position + " auf dem Factory-Terrain platziert.");
    }
    private static void ClearAreaAroundSpawn(GameObject mapRoot, Vector3 spawn, float innerSafeRadius, float outerRadius, int keepTrees, int keepRocks)
    {
        ClearChildrenNear(mapRoot.transform.Find("Resources"), spawn, outerRadius * outerRadius);
        ClearVegetationKeepingSome(mapRoot.transform.Find("Vegetation"), spawn, innerSafeRadius, outerRadius, keepTrees, keepRocks);
    }
    private static void ClearChildrenNear(Transform parent, Vector3 spawn, float sqrRadius)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            Vector3 flatDelta = child.position - spawn;
            flatDelta.y = 0f;
            if (flatDelta.sqrMagnitude < sqrRadius)
                Object.DestroyImmediate(child.gameObject);
        }
    }
    private static void ClearVegetationKeepingSome(Transform parent, Vector3 spawn, float innerSafeRadius, float outerRadius, int keepTrees, int keepRocks)
    {
        if (parent == null) return;

        float innerSqr = innerSafeRadius * innerSafeRadius;
        float outerSqr = outerRadius * outerRadius;
        List<Transform> treeCandidates = new List<Transform>();
        List<Transform> rockCandidates = new List<Transform>();

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            Vector3 flatDelta = child.position - spawn;
            flatDelta.y = 0f;
            float sqrDist = flatDelta.sqrMagnitude;

            if (sqrDist < innerSqr)
            {
                Object.DestroyImmediate(child.gameObject);
            }
            else if (sqrDist <= outerSqr)
            {
                bool isTree = child.GetComponentInChildren<HarvestableTree>() != null;
                (isTree ? treeCandidates : rockCandidates).Add(child);
            }
        }

        DestroyAllButRandom(treeCandidates, keepTrees);
        DestroyAllButRandom(rockCandidates, keepRocks);
    }
    private static void DestroyAllButRandom(List<Transform> candidates, int keepCount)
    {
        List<Transform> shuffled = candidates.OrderBy(_ => Random.value).ToList();
        for (int i = keepCount; i < shuffled.Count; i++)
            Object.DestroyImmediate(shuffled[i].gameObject);
    }
    private static Vector3 FindSpawnPoint(Terrain terrain, MapGenerator generator)
    {
        Vector3 origin = terrain.transform.position;
        Vector3 center = origin + new Vector3(generator.terrainSize.x * 0.5f, 0f, generator.terrainSize.z * 0.5f);

        for (int ring = 0; ring < 40; ring++)
        {
            for (int a = 0; a < 8; a++)
            {
                float angle = a * Mathf.PI * 2f / 8f;
                Vector3 candidate = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (ring * 40f);
                float worldY = terrain.SampleHeight(candidate) + origin.y;
                float normalized = (worldY - origin.y) / generator.terrainSize.y;

                if (normalized > generator.waterLevel + 0.05f && normalized < generator.mountainStart)
                {
                    candidate.y = worldY;
                    return candidate;
                }
            }
        }

        Vector3 fallback = center;
        fallback.y = terrain.SampleHeight(center) + origin.y;
        return fallback;
    }
    private static GameObject BuildTerrainAndGenerate(Vector3 rootPosition)
    {
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = 513;
        terrainData.size = new Vector3(2000f, 300f, 2000f);

        GameObject root = new GameObject("FactoryMap");
        root.transform.position = rootPosition;

        GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
        terrainGO.name = "FactoryTerrain";
        terrainGO.transform.SetParent(root.transform);
        terrainGO.transform.localPosition = new Vector3(-1000f, 0f, -1000f);

        MapGenerator generator = terrainGO.AddComponent<MapGenerator>();
        generator.seed = 12345;
        generator.terrainSize = terrainData.size;
        generator.heightmapResolution = 513;
        AssignHarvestablePrefabs(generator);

        generator.GenerateMap();

        EnsureFolder("Assets/Factory");
        string assetPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Factory/FactoryTerrainData.asset");
        AssetDatabase.CreateAsset(terrainData, assetPath);
        AssetDatabase.SaveAssets();

        return root;
    }
    private const string ModelsFolder = "Assets/Factory/Models";
    private const string PrefabFolder = "Assets/Factory/Prefabs";
    private const string MaterialFolder = "Assets/Factory/Materials";
    private static readonly string[] TreeModels = { "Tree_Pine_A", "Tree_Pine_B", "Tree_Pine_C", "Tree_Leaf_A", "Tree_Leaf_B" };
    private static readonly string[] AutumnModels = { "Tree_Leaf_A", "Tree_Leaf_B" };
    private static readonly string[] RockModels = { "Rock_A", "Rock_B", "Rock_C" };
    [MenuItem("Factory/Setup Harvestable Prefabs")]
    public static void SetupHarvestablePrefabs()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(MaterialFolder);

        Dictionary<string, Material> mats = new Dictionary<string, Material>
        {
            { "Bark", CreateLitMaterial("Bark", new Color(0.36f, 0.24f, 0.15f)) },
            { "WoodCut", CreateLitMaterial("WoodCut", new Color(0.82f, 0.65f, 0.43f)) },
            { "Needles", CreateLitMaterial("Needles", new Color(0.16f, 0.42f, 0.20f)) },
            { "Leaves", CreateLitMaterial("Leaves", new Color(0.33f, 0.60f, 0.21f)) },
            { "Rock", CreateLitMaterial("Rock", new Color(0.52f, 0.51f, 0.49f)) },
            { "RockDark", CreateLitMaterial("RockDark", new Color(0.38f, 0.37f, 0.36f)) },
            { "Moss", CreateLitMaterial("Moss", new Color(0.33f, 0.50f, 0.22f)) },
        };
        Material autumn = CreateLitMaterial("LeavesAutumn", new Color(0.85f, 0.52f, 0.12f));
        Material particles = CreateParticleMaterial();

        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { ModelsFolder }))
            ConfigureModelImporter(AssetDatabase.GUIDToAssetPath(guid), mats);

        GameObject stump = SavePrefab("Tree_Stump", "Tree_Stump", go => { });
        GameObject chunk = SavePrefab("Rock_Chunk", "Rock_Chunk", go =>
        {
            AddConvexMeshCollider(go);
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
        });
        foreach (string name in TreeModels)
            SavePrefab(name, name, go => AddTreeComponents(go, stump, particles));
        foreach (string name in AutumnModels)
        {
            SavePrefab(name + "_Autumn", name, go =>
            {
                AddTreeComponents(go, stump, particles);
                ReplaceMaterial(go, mats["Leaves"], autumn);
            });
        }
        foreach (string name in RockModels)
        {
            SavePrefab(name, name, go =>
            {
                AddConvexMeshCollider(go);
                HarvestableRock rock = go.AddComponent<HarvestableRock>();
                rock.chunkPrefab = chunk;
                rock.particleMaterial = particles;
            });
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Harvestable-Prefabs erstellt in " + PrefabFolder);
    }
    private static void AssignHarvestablePrefabs(MapGenerator generator)
    {
        bool modelsExist = AssetDatabase.LoadAssetAtPath<GameObject>(ModelsFolder + "/Tree_Pine_A.fbx") != null;
        bool prefabsExist = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/Tree_Pine_A.prefab") != null;
        if (modelsExist && !prefabsExist) SetupHarvestablePrefabs();

        generator.treePrefabs = LoadPrefabs(TreeModels, "");
        generator.autumnTreePrefabs = LoadPrefabs(AutumnModels, "_Autumn");
        generator.rockPrefabs = LoadPrefabs(RockModels, "");
        Debug.Log("Harvestable-Prefabs zugewiesen: " + generator.treePrefabs.Length + " Baeume, "
            + generator.autumnTreePrefabs.Length + " Herbstbaeume, " + generator.rockPrefabs.Length + " Felsen");
    }
    private static GameObject[] LoadPrefabs(string[] names, string suffix)
    {
        List<GameObject> result = new List<GameObject>();
        foreach (string name in names)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + name + suffix + ".prefab");
            if (prefab != null) result.Add(prefab);
        }
        return result.ToArray();
    }
    private static Material CreateLitMaterial(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }
    private static Material CreateParticleMaterial()
    {
        string path = MaterialFolder + "/HarvestParticles.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }
    private static void ConfigureModelImporter(string path, Dictionary<string, Material> mats)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.isReadable = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        foreach (KeyValuePair<string, Material> entry in mats)
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.Key), entry.Value);
        importer.SaveAndReimport();
    }
    private static GameObject SavePrefab(string prefabName, string modelName, System.Action<GameObject> configure)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelsFolder + "/" + modelName + ".fbx");
        if (model == null)
        {
            Debug.LogError("Modell fehlt: " + modelName);
            return null;
        }
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = prefabName;
        configure(instance);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabFolder + "/" + prefabName + ".prefab");
        Renderer r = instance.GetComponentInChildren<Renderer>();
        string materialNames = string.Join(", ", System.Array.ConvertAll(r.sharedMaterials, m => m != null ? m.name : "null"));
        Debug.Log("Prefab " + prefabName + ": Groesse " + r.bounds.size + ", Materialien [" + materialNames + "]");
        Object.DestroyImmediate(instance);
        return prefab;
    }
    private static void AddConvexMeshCollider(GameObject go)
    {
        MeshFilter mf = go.GetComponentInChildren<MeshFilter>();
        MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = mf.sharedMesh;
        mc.convex = true;
    }
    private static void AddTreeComponents(GameObject go, GameObject stump, Material particles)
    {
        Bounds b = go.GetComponentInChildren<Renderer>().bounds;
        CapsuleCollider trunk = go.AddComponent<CapsuleCollider>();
        trunk.radius = 0.55f;
        trunk.height = b.size.y * 0.5f;
        trunk.center = new Vector3(0f, trunk.height * 0.5f, 0f);
        BoxCollider canopy = go.AddComponent<BoxCollider>();
        canopy.isTrigger = true;
        canopy.center = new Vector3(b.center.x, b.center.y + b.size.y * 0.1f, b.center.z);
        canopy.size = new Vector3(b.size.x * 0.7f, b.size.y * 0.75f, b.size.z * 0.7f);
        HarvestableTree tree = go.AddComponent<HarvestableTree>();
        tree.stumpPrefab = stump;
        tree.particleMaterial = particles;
    }
    private static void ReplaceMaterial(GameObject go, Material from, Material to)
    {
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            Material[] shared = r.sharedMaterials;
            for (int i = 0; i < shared.Length; i++)
            {
                if (shared[i] == from) shared[i] = to;
            }
            r.sharedMaterials = shared;
        }
    }
    public static void BakeBatch()
    {
        SetupHarvestablePrefabs();
        BakeMapIntoSampleScene();
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        string leaf = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
