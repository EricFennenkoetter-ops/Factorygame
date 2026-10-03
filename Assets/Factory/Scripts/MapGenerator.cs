using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

// Generiert prozedural eine Factory-Game Map:
// Terrain (Hoehe + Biom-Texturen), Wasser, Baeume und Ressourcen-Nodes.
// Aufruf ueber das Inspector-Kontextmenu ("Generate Map") oder per Code via GenerateMap().
[RequireComponent(typeof(Terrain))]
public class MapGenerator : MonoBehaviour
{
    [Header("Terrain")]
    public int heightmapResolution = 513; // 2^n + 1
    public Vector3 terrainSize = new Vector3(2000f, 300f, 2000f);
    public int seed = 12345;

    [Header("Hoehen-Rauschen (Berge/Taeler)")]
    public float heightNoiseScale = 650f; // groesser = sanftere, weitlaeufigere Huegel statt Kraterlandschaft
    public int heightOctaves = 4;
    [Range(0f, 1f)] public float heightPersistence = 0.42f;
    public float heightLacunarity = 2f;
    public float warpNoiseScale = 900f;
    public float warpStrength = 140f; // verzerrt die Sample-Koordinaten -> organischere, weniger gitterartige Formen
    public AnimationCurve heightCurve = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.15f, 0.08f), new Keyframe(0.55f, 0.4f),
        new Keyframe(0.75f, 0.62f), new Keyframe(1f, 1f)); // laengere, sanfte Ebenen im unteren/mittleren Bereich
    public int smoothPasses = 2; // Box-Blur auf der fertigen Hoehenkarte - entfernt scharfe Mini-Kanten/Spitzen, an denen man haengen bleibt

    [Header("Feuchtigkeits-Rauschen (Biom-Verteilung)")]
    public float moistureNoiseScale = 500f;
    public int moistureOctaves = 3;

    [Header("Ebene Flaechen (fuer Fabrikgebaeude)")]
    public float flatnessNoiseScale = 700f;
    [Range(0f, 1f)] public float flatnessThreshold = 0.55f;
    [Range(0f, 1f)] public float flattenStrength = 0.85f;

    [Header("Wasser")]
    [Range(0f, 1f)] public float waterLevel = 0.16f;
    [Range(0f, 0.2f)] public float shoreWidth = 0.02f;
    public Material waterMaterial;
    public int waterMeshResolution = 200; // Gitterzellen pro Achse fuer Seen/Fluss-Mesh

    [Header("Seen")]
    public float lakeNoiseScale = 350f;
    [Range(0f, 1f)] public float lakeThreshold = 0.66f;
    [Range(0f, 0.5f)] public float lakeBandWidth = 0.22f; // breiter = sanftere Uferboeschung statt steiler Klippe
    [Range(0f, 0.3f)] public float lakeDepth = 0.05f;

    [Header("Fluesse")]
    public float riverNoiseScale = 1500f; // gross = wenige, durchgehende Fluesse statt vielem Geflecht
    public float riverWarpScale = 500f;
    public float riverWarpStrength = 160f; // absolute Verzerrung in Units (nicht mehr an riverNoiseScale gekoppelt)
    [Range(0f, 1f)] public float riverThreshold = 0.86f; // etwas niedriger = breiteres, sanfteres Ufer-Band
    [Range(0f, 0.3f)] public float riverDepth = 0.03f;

    [Header("Biom-Schwellenwerte (normalisierte Hoehe 0-1)")]
    public float mountainStart = 0.7f; // mehr Grasland/Ebenen, Berge erst weiter aussen wie bei Satisfactory
    public float snowLine = 0.88f;
    public float steepSlopeDegrees = 32f;

    [Header("Felsbrocken")]
    public float rockClusterScale = 90f;
    [Range(0f, 1f)] public float rockClusterThreshold = 0.68f;
    public float rockGridSpacing = 22f; // groesser = weniger dicht, mehr Platz zum Laufen
    [Range(0f, 1f)] public float rockSpawnChance = 0.3f;

    [Header("Baeume")]
    public GameObject treePrefab; // optional - leer lassen fuer einfache prozedurale Baeume
    public float treeGridSpacing = 13f; // groesser = weniger dicht, mehr Platz zum Laufen zwischen Baeumen
    public float treeNoiseScale = 45f;
    [Range(0f, 1f)] public float treeClusterThreshold = 0.55f;
    [Range(0f, 1f)] public float treeSpawnChance = 0.45f;

    [Header("Ressourcen")]
    public GameObject resourceNodePrefab; // optional - leer lassen fuer einfache prozedurale Nodes
    public float resourceClusterScale = 130f;
    [Range(0f, 1f)] public float resourceClusterThreshold = 0.68f;
    public int resourceAttempts = 500;
    public float minResourceSpacing = 28f;

    private Terrain terrain;
    private TerrainData terrainData;
    private int offsetHX, offsetHZ, offsetMX, offsetMZ;
    private int offsetFX, offsetFZ, offsetLX, offsetLZ, offsetRX, offsetRZ;
    private Transform vegetationParent;
    private Transform resourceParent;
    private Transform waterParent;

    private readonly List<Vector3> placedResourcePositions = new List<Vector3>();

    private static readonly (ResourceType type, Color color)[] ResourceColors = new (ResourceType, Color)[]
    {
        (ResourceType.Iron, new Color(0.72f, 0.45f, 0.35f)),
        (ResourceType.Copper, new Color(0.85f, 0.5f, 0.2f)),
        (ResourceType.Coal, new Color(0.12f, 0.12f, 0.12f)),
        (ResourceType.Stone, new Color(0.6f, 0.6f, 0.6f)),
        (ResourceType.Oil, new Color(0.05f, 0.05f, 0.05f)),
        (ResourceType.Uranium, new Color(0.3f, 0.85f, 0.3f)),
    };

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("MapGenerator braucht ein Terrain-Component auf demselben GameObject.");
            return;
        }

        Random.InitState(seed);
        offsetHX = Random.Range(-100000, 100000);
        offsetHZ = Random.Range(-100000, 100000);
        offsetMX = Random.Range(-100000, 100000);
        offsetMZ = Random.Range(-100000, 100000);
        offsetFX = Random.Range(-100000, 100000);
        offsetFZ = Random.Range(-100000, 100000);
        offsetLX = Random.Range(-100000, 100000);
        offsetLZ = Random.Range(-100000, 100000);
        offsetRX = Random.Range(-100000, 100000);
        offsetRZ = Random.Range(-100000, 100000);

        SetupTerrainData();
        GenerateHeights();
        PaintTerrainLayers();
        ClearGenerated();
        SpawnWater();
        SpawnTrees();
        SpawnRocks();
        SpawnResources();

        Debug.Log("Map-Generierung abgeschlossen. Seed: " + seed);
    }

    private void SetupTerrainData()
    {
        terrainData = terrain.terrainData;
        if (terrainData == null)
        {
            terrainData = new TerrainData();
            terrain.terrainData = terrainData;
#if UNITY_EDITOR
            TerrainCollider collider = GetComponent<TerrainCollider>();
            if (collider != null) collider.terrainData = terrainData;
#endif
        }

        terrainData.heightmapResolution = heightmapResolution;
        terrainData.size = terrainSize;
    }

    // --- Rausch-Hilfsfunktionen -------------------------------------------------

    private float FractalNoise(float worldX, float worldZ, float scale, int octaves, float persistence, float lacunarity, int offX, int offZ)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float sum = 0f;
        float maxSum = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (worldX + offX) / scale * frequency;
            float sampleZ = (worldZ + offZ) / scale * frequency;
            sum += Mathf.PerlinNoise(sampleX, sampleZ) * amplitude;
            maxSum += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return sum / maxSum; // 0..1
    }

    private float GetNormalizedHeight(float worldX, float worldZ)
    {
        // Domain Warp: die Sample-Koordinaten leicht verzerren, bevor die eigentliche
        // Hoehe abgefragt wird -> vermeidet die typischen runden "Perlin-Krater" und
        // erzeugt stattdessen unregelmaessige, organisch wirkende Huegelketten.
        float warpX = worldX + (FractalNoise(worldX, worldZ, warpNoiseScale, 2, 0.5f, 2f, offsetHX + 9001, offsetHZ) - 0.5f) * warpStrength;
        float warpZ = worldZ + (FractalNoise(worldX, worldZ, warpNoiseScale, 2, 0.5f, 2f, offsetHX, offsetHZ + 9001) - 0.5f) * warpStrength;

        float raw = FractalNoise(warpX, warpZ, heightNoiseScale, heightOctaves, heightPersistence, heightLacunarity, offsetHX, offsetHZ);

        // Ebene Flaechen: an manchen Stellen (grossflaechige Maske) die Detail-Oktaven
        // durch eine flachere Version ersetzen -> natuerliche Plateaus/Ebenen fuer Gebaeude.
        float flatMask = FractalNoise(worldX, worldZ, flatnessNoiseScale, 2, 0.5f, 2f, offsetFX, offsetFZ);
        float flatAmount = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(flatnessThreshold, 1f, flatMask)) * flattenStrength;
        if (flatAmount > 0f)
        {
            float smooth = FractalNoise(worldX, worldZ, heightNoiseScale * 2.5f, 2, 0.5f, 2f, offsetHX, offsetHZ);
            raw = Mathf.Lerp(raw, smooth, flatAmount);
        }

        float h = heightCurve.Evaluate(raw);

        // Seen: grosse, weiche Blobs unter das Wasserniveau absenken -> echte Seebecken
        // statt einer einzigen, das ganze Tiefland flutenden Plane.
        float lakeMask = FractalNoise(worldX, worldZ, lakeNoiseScale, 2, 0.5f, 2f, offsetLX, offsetLZ);
        float lakeStrength = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(lakeThreshold, Mathf.Min(1f, lakeThreshold + lakeBandWidth), lakeMask));
        if (lakeStrength > 0f)
        {
            h = Mathf.Lerp(h, Mathf.Max(0f, waterLevel - lakeDepth), lakeStrength);
        }

        // Fluesse: wenige, breite, durchgehende Baender (grossskalige Ridge-Noise mit
        // moderater, absolut begrenzter Verzerrung), nur im Flach-/Huegelland.
        if (h < mountainStart + 0.05f)
        {
            float riverWarpX = worldX + (FractalNoise(worldX, worldZ, riverWarpScale, 2, 0.5f, 2f, offsetRX + 5000, offsetRZ) - 0.5f) * riverWarpStrength;
            float riverWarpZ = worldZ + (FractalNoise(worldX, worldZ, riverWarpScale, 2, 0.5f, 2f, offsetRX, offsetRZ + 5000) - 0.5f) * riverWarpStrength;
            float ridge = 1f - Mathf.Abs(2f * FractalNoise(riverWarpX, riverWarpZ, riverNoiseScale, 2, 0.5f, 2f, offsetRX, offsetRZ) - 1f);
            float riverStrength = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(riverThreshold, 1f, ridge));
            if (riverStrength > 0f)
            {
                h = Mathf.Lerp(h, Mathf.Max(0f, waterLevel - riverDepth), riverStrength);
            }
        }

        return h;
    }

    // True, wenn an dieser Stelle Wasser (See oder Fluss) sein soll - fuer die Wasser-Mesh.
    private bool IsWaterAt(float worldX, float worldZ)
    {
        return GetNormalizedHeight(worldX, worldZ) <= waterLevel + 0.001f;
    }

    private float GetMoisture(float worldX, float worldZ)
    {
        return FractalNoise(worldX, worldZ, moistureNoiseScale, moistureOctaves, 0.5f, 2f, offsetMX, offsetMZ);
    }

    private float GetClusterNoise(float worldX, float worldZ, float scale, int offX, int offZ)
    {
        return FractalNoise(worldX, worldZ, scale, 2, 0.5f, 2f, offX, offZ);
    }

    // --- Hoehenkarte --------------------------------------------------------

    private void GenerateHeights()
    {
        int res = terrainData.heightmapResolution;
        float[,] heights = new float[res, res];

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float worldX = (float)x / (res - 1) * terrainSize.x;
                float worldZ = (float)z / (res - 1) * terrainSize.z;
                heights[z, x] = GetNormalizedHeight(worldX, worldZ);
            }
        }

        heights = SmoothHeights(heights, res, smoothPasses);
        terrainData.SetHeights(0, 0, heights);
    }

    // Einfacher 3x3-Box-Blur ueber die Hoehenkarte. Die analytischen Rausch-Formeln
    // (Warp + Ebenen-Blend + See-/Fluss-Carving) koennen an manchen Stellen einzelne
    // scharfe Spruenge zwischen Nachbar-Texeln erzeugen - das reicht schon, damit ein
    // Rigidbody-Character daran haengen bleibt. Der Blur entfernt solche Mini-Kanten,
    // ohne die grosse Form der Landschaft sichtbar zu veraendern.
    private float[,] SmoothHeights(float[,] heights, int res, int passes)
    {
        for (int p = 0; p < passes; p++)
        {
            float[,] result = new float[res, res];
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    float sum = 0f;
                    int count = 0;
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int nz = z + dz;
                        if (nz < 0 || nz >= res) continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx;
                            if (nx < 0 || nx >= res) continue;
                            sum += heights[nz, nx];
                            count++;
                        }
                    }
                    result[z, x] = sum / count;
                }
            }
            heights = result;
        }
        return heights;
    }

    // --- Terrain-Texturen (Splatmap) ----------------------------------------

    private void PaintTerrainLayers()
    {
        TerrainLayer[] layers = BuildTerrainLayers();
        terrainData.terrainLayers = layers;

        int alphaRes = terrainData.alphamapResolution;
        float[,,] map = new float[alphaRes, alphaRes, layers.Length];

        for (int z = 0; z < alphaRes; z++)
        {
            for (int x = 0; x < alphaRes; x++)
            {
                float normX = (float)x / (alphaRes - 1);
                float normZ = (float)z / (alphaRes - 1);
                float worldX = normX * terrainSize.x;
                float worldZ = normZ * terrainSize.z;

                float h = GetNormalizedHeight(worldX, worldZ);
                float m = GetMoisture(worldX, worldZ);
                float slope = terrainData.GetSteepness(normX, normZ);

                float sandW = 0f, grassW = 0f, dirtW = 0f, rockW = 0f, snowW = 0f;

                if (h < waterLevel + shoreWidth)
                {
                    sandW = 1f;
                }
                else if (h > snowLine)
                {
                    snowW = 1f;
                }
                else if (slope > steepSlopeDegrees || h > mountainStart)
                {
                    rockW = 1f;
                }
                else
                {
                    grassW = Mathf.Lerp(0.55f, 1f, m);
                    dirtW = 1f - grassW;
                }

                float total = sandW + grassW + dirtW + rockW + snowW;
                if (total <= 0f) { grassW = 1f; total = 1f; }

                map[z, x, 0] = grassW / total;
                map[z, x, 1] = dirtW / total;
                map[z, x, 2] = rockW / total;
                map[z, x, 3] = sandW / total;
                map[z, x, 4] = snowW / total;
            }
        }

        terrainData.SetAlphamaps(0, 0, map);
    }

    private TerrainLayer[] BuildTerrainLayers()
    {
        // Kraeftige, bunte Satisfactory-Palette statt gedeckter Grautoene - plus
        // Akzentfarben (Bluemchen, Moos, Laub, Kies-Glitzer) fuer mehr Farbvielfalt.
        return new TerrainLayer[]
        {
            CreateBlendedLayer("Grass", new Color(0.22f, 0.60f, 0.14f), new Color(0.46f, 0.76f, 0.20f), 10f, 0.12f,
                accentColor: new Color(0.82f, 0.86f, 0.20f), accentChance: 0.05f),   // gelbe Wildblumen-Tupfer
            CreateBlendedLayer("ForestFloor", new Color(0.32f, 0.24f, 0.10f), new Color(0.48f, 0.38f, 0.16f), 8f, 0.10f,
                accentColor: new Color(0.62f, 0.30f, 0.08f), accentChance: 0.08f),   // rotbraunes Herbstlaub
            CreateBlendedLayer("Rock", new Color(0.38f, 0.37f, 0.40f), new Color(0.62f, 0.58f, 0.54f), 6f, 0.15f,
                accentColor: new Color(0.30f, 0.50f, 0.24f), accentChance: 0.06f),   // Moosflecken
            CreateBlendedLayer("Sand", new Color(0.84f, 0.70f, 0.40f), new Color(0.94f, 0.85f, 0.60f), 12f, 0.08f,
                accentColor: new Color(0.60f, 0.52f, 0.36f), accentChance: 0.05f),   // dunklere Kiesel
            CreateBlendedLayer("Snow", new Color(0.88f, 0.92f, 0.98f), new Color(1f, 1f, 1f), 14f, 0.05f,
                accentColor: new Color(0.55f, 0.75f, 0.95f), accentChance: 0.04f),   // bläuliche Schattenkanten
        };
    }

    // Erzeugt eine Textur, die zwischen zwei Farbtoenen ueber mehrschichtiges Rauschen
    // blendet (statt einer einzigen Flat-Color) und optional vereinzelte Akzentfarb-Tupfer
    // einstreut (Bluemchen/Moos/Laub) - wirkt aus der Naehe deutlich weniger wie ein
    // einfarbiger Kunststoff-Boden und liefert insgesamt mehr Farbvielfalt.
    private TerrainLayer CreateBlendedLayer(string layerName, Color colorA, Color colorB, float tileSize, float roughness,
        Color? accentColor = null, float accentChance = 0f)
    {
#if UNITY_EDITOR
        string folder = "Assets/Factory/TerrainLayers";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Factory")) AssetDatabase.CreateFolder("Assets", "Factory");
            AssetDatabase.CreateFolder("Assets/Factory", "TerrainLayers");
        }

        const int texSize = 128;
        string texPath = folder + "/" + layerName + "_Tex.png";
        Texture2D tex = new Texture2D(texSize, texSize);

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                // Grossflaechiges Blend-Muster (welche Farbe dominiert) ...
                float blend = Mathf.PerlinNoise(x * 0.035f, y * 0.035f);
                blend = Mathf.SmoothStep(0f, 1f, blend);
                Color baseColor = Color.Lerp(colorA, colorB, blend);

                // ... plus feines Speckle-Rauschen fuer Bodenstruktur/Kies-Look.
                float fine = Mathf.PerlinNoise(x * 0.22f, y * 0.22f) - 0.5f;
                float speck = Mathf.PerlinNoise(x * 0.9f + 100f, y * 0.9f + 100f) - 0.5f;
                float n = fine * roughness + speck * roughness * 0.5f;

                Color c = new Color(
                    Mathf.Clamp01(baseColor.r + n),
                    Mathf.Clamp01(baseColor.g + n),
                    Mathf.Clamp01(baseColor.b + n));

                if (accentColor.HasValue && accentChance > 0f)
                {
                    float accentNoise = Mathf.PerlinNoise(x * 1.6f + 500f, y * 1.6f + 500f);
                    if (accentNoise > 1f - accentChance)
                    {
                        float t = Mathf.InverseLerp(1f - accentChance, 1f, accentNoise);
                        c = Color.Lerp(c, accentColor.Value, t);
                    }
                }

                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        File.WriteAllBytes(texPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(texPath);
        Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        string layerPath = folder + "/" + layerName + ".terrainlayer";
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, layerPath);
        }
        layer.diffuseTexture = savedTex;
        layer.tileSize = new Vector2(tileSize, tileSize);
        EditorUtility.SetDirty(layer);
        AssetDatabase.SaveAssets();
        return layer;
#else
        // Zur Laufzeit (Build) koennen keine Assets erzeugt werden - Platzhalter-Layer.
        TerrainLayer layer = new TerrainLayer();
        return layer;
#endif
    }

    // --- Aufraeumen vorheriger Generierung -----------------------------------

    private void ClearGenerated()
    {
        Transform parent = transform.parent != null ? transform.parent : transform;

        Transform existingVeg = parent.Find("Vegetation");
        Transform existingRes = parent.Find("Resources");
        Transform existingWater = parent.Find("Water");

        if (existingVeg != null) DestroyImmediate(existingVeg.gameObject);
        if (existingRes != null) DestroyImmediate(existingRes.gameObject);
        if (existingWater != null) DestroyImmediate(existingWater.gameObject);

        vegetationParent = new GameObject("Vegetation").transform;
        vegetationParent.SetParent(parent);
        resourceParent = new GameObject("Resources").transform;
        resourceParent.SetParent(parent);
        waterParent = new GameObject("Water").transform;
        waterParent.SetParent(parent);

        placedResourcePositions.Clear();
    }

    // --- Wasser ---------------------------------------------------------------

    private void SpawnWater()
    {
        int res = Mathf.Max(8, waterMeshResolution);
        float cellX = terrainSize.x / res;
        float cellZ = terrainSize.z / res;

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int z = 0; z < res; z++)
        {
            float worldZ0 = z * cellZ;
            float worldZc = worldZ0 + cellZ * 0.5f;

            for (int x = 0; x < res; x++)
            {
                float worldX0 = x * cellX;
                float worldXc = worldX0 + cellX * 0.5f;

                if (!IsWaterAt(worldXc, worldZc)) continue;

                float x0 = worldX0, x1 = worldX0 + cellX;
                float z0 = worldZ0, z1 = worldZ0 + cellZ;

                int baseIndex = verts.Count;
                verts.Add(new Vector3(x0, 0f, z0));
                verts.Add(new Vector3(x1, 0f, z0));
                verts.Add(new Vector3(x1, 0f, z1));
                verts.Add(new Vector3(x0, 0f, z1));
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));

                // Winding fuer nach oben zeigende Normale (analog Unity-Terrain-Meshes).
                tris.Add(baseIndex + 0); tris.Add(baseIndex + 3); tris.Add(baseIndex + 1);
                tris.Add(baseIndex + 1); tris.Add(baseIndex + 3); tris.Add(baseIndex + 2);
            }
        }

        if (verts.Count == 0)
        {
            Debug.LogWarning("Keine Wasserflaeche gefunden - See-/Fluss-Schwellenwerte pruefen.");
            return;
        }

        GameObject waterGO = new GameObject("WaterMesh");
        waterGO.transform.SetParent(waterParent);
        float waterY = terrain.transform.position.y + waterLevel * terrainSize.y;
        waterGO.transform.position = new Vector3(terrain.transform.position.x, waterY, terrain.transform.position.z);

        Mesh mesh = new Mesh();
        mesh.name = "WaterMesh";
        if (verts.Count > 60000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter mf = waterGO.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = waterGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = GetOrCreateWaterMaterial();
    }

    private Material GetOrCreateWaterMaterial()
    {
        if (waterMaterial != null) return waterMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        Color waterColor = new Color(0.05f, 0.55f, 0.68f, 0.72f); // kraeftigeres, satteres Tuerkis-Blau
        mat.color = waterColor;

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f); // Transparent (URP Lit)
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_Smoothness", 0.9f);
        mat.renderQueue = 3000;
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");

        waterMaterial = mat;
        return mat;
    }

    // --- Baeume -----------------------------------------------------------

    private void SpawnTrees()
    {
        int treeOffX = offsetHX + 777;
        int treeOffZ = offsetHZ + 777;

        for (float worldZ = 0f; worldZ < terrainSize.z; worldZ += treeGridSpacing)
        {
            for (float worldX = 0f; worldX < terrainSize.x; worldX += treeGridSpacing)
            {
                float jitterX = worldX + Random.Range(-treeGridSpacing * 0.4f, treeGridSpacing * 0.4f);
                float jitterZ = worldZ + Random.Range(-treeGridSpacing * 0.4f, treeGridSpacing * 0.4f);
                jitterX = Mathf.Clamp(jitterX, 0f, terrainSize.x - 0.01f);
                jitterZ = Mathf.Clamp(jitterZ, 0f, terrainSize.z - 0.01f);

                float h = GetNormalizedHeight(jitterX, jitterZ);
                if (h < waterLevel + shoreWidth + 0.02f || h > mountainStart) continue; // nur Wald-/Ebenenband

                float moisture = GetMoisture(jitterX, jitterZ);
                float cluster = GetClusterNoise(jitterX, jitterZ, treeNoiseScale, treeOffX, treeOffZ);
                float density = cluster * Mathf.Lerp(0.5f, 1f, moisture);

                if (density < treeClusterThreshold) continue;
                if (Random.value > treeSpawnChance) continue;

                Vector3 worldPos = new Vector3(
                    terrain.transform.position.x + jitterX,
                    0f,
                    terrain.transform.position.z + jitterZ);
                worldPos.y = terrain.SampleHeight(worldPos) + terrain.transform.position.y;

                SpawnTreeAt(worldPos);
            }
        }
    }

    private void SpawnTreeAt(Vector3 pos)
    {
        GameObject tree;
        if (treePrefab != null)
        {
#if UNITY_EDITOR
            tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, vegetationParent);
            tree.transform.position = pos;
#else
            tree = Instantiate(treePrefab, pos, Quaternion.identity, vegetationParent);
#endif
        }
        else
        {
            tree = BuildProceduralTree();
            tree.transform.SetParent(vegetationParent);
            tree.transform.position = pos;
        }

        tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        float scale = Random.Range(0.8f, 1.3f);
        tree.transform.localScale *= scale;

        if (tree.GetComponent<HarvestableTree>() == null)
            tree.AddComponent<HarvestableTree>();
    }

    // Baut eine Nadelbaum-Silhouette aus einem verjuengten Stamm (zwei Zylinder-Segmente)
    // und drei nach oben kleiner werdenden, leicht abgeflachten Kronen-"Etagen" -
    // wirkt aus der Distanz deutlich mehr wie ein Fichten-/Tannen-Baum als eine einzelne Kugel.
    private GameObject BuildProceduralTree()
    {
        GameObject root = new GameObject("Tree");

        GameObject trunkLower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunkLower.name = "TrunkLower";
        trunkLower.transform.SetParent(root.transform);
        trunkLower.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        trunkLower.transform.localScale = new Vector3(0.34f, 0.6f, 0.34f);
        Color barkColor = Color.Lerp(new Color(0.32f, 0.21f, 0.12f), new Color(0.42f, 0.27f, 0.15f), Random.value);
        ApplyColor(trunkLower, barkColor);

        GameObject trunkUpper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunkUpper.name = "TrunkUpper";
        trunkUpper.transform.SetParent(root.transform);
        trunkUpper.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        trunkUpper.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
        ApplyColor(trunkUpper, barkColor);

        // Meistens saftiges Gruen, gelegentlich ein warmer Herbstbaum fuer Farbabwechslung.
        Color canopyColor;
        if (Random.value < 0.12f)
            canopyColor = Color.Lerp(new Color(0.75f, 0.55f, 0.08f), new Color(0.68f, 0.30f, 0.10f), Random.value);
        else
            canopyColor = Color.Lerp(new Color(0.12f, 0.45f, 0.15f), new Color(0.30f, 0.62f, 0.18f), Random.value);
        float[] tierY = { 1.9f, 2.9f, 3.7f };
        float[] tierRadius = { 1.7f, 1.3f, 0.85f };
        for (int i = 0; i < tierY.Length; i++)
        {
            GameObject tier = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tier.name = "CanopyTier" + i;
            tier.transform.SetParent(root.transform);
            tier.transform.localPosition = new Vector3(0f, tierY[i], 0f);
            tier.transform.localScale = new Vector3(tierRadius[i], tierRadius[i] * 0.8f, tierRadius[i]);
            ApplyColor(tier, Color.Lerp(canopyColor, canopyColor * 0.85f, (float)i / tierY.Length));
        }

        AddBoundingInteractionCollider(root);
        return root;
    }

    // --- Felsbrocken --------------------------------------------------------

    private void SpawnRocks()
    {
        int rockOffX = offsetHX + 4242;
        int rockOffZ = offsetHZ + 4242;

        for (float worldZ = 0f; worldZ < terrainSize.z; worldZ += rockGridSpacing)
        {
            for (float worldX = 0f; worldX < terrainSize.x; worldX += rockGridSpacing)
            {
                float jitterX = Mathf.Clamp(worldX + Random.Range(-rockGridSpacing * 0.4f, rockGridSpacing * 0.4f), 0f, terrainSize.x - 0.01f);
                float jitterZ = Mathf.Clamp(worldZ + Random.Range(-rockGridSpacing * 0.4f, rockGridSpacing * 0.4f), 0f, terrainSize.z - 0.01f);

                float h = GetNormalizedHeight(jitterX, jitterZ);
                if (h < waterLevel + shoreWidth) continue; // nicht im Wasser

                // Felsen bevorzugt in Bergen/Fels-Haengen, vereinzelt auch im Flachland.
                bool inRockyZone = h > mountainStart - 0.08f;
                float cluster = GetClusterNoise(jitterX, jitterZ, rockClusterScale, rockOffX, rockOffZ);
                if (cluster < rockClusterThreshold) continue;
                if (!inRockyZone && Random.value > 0.15f) continue;
                if (Random.value > rockSpawnChance) continue;

                Vector3 worldPos = new Vector3(
                    terrain.transform.position.x + jitterX,
                    0f,
                    terrain.transform.position.z + jitterZ);
                worldPos.y = terrain.SampleHeight(worldPos) + terrain.transform.position.y;

                SpawnRockAt(worldPos);
            }
        }
    }

    // Boulder aus 2-3 ueberlappenden, unregelmaessig skalierten/rotierten Wuerfeln -
    // wirkt kantiger/natuerlicher als eine einzelne perfekte Kugel.
    private void SpawnRockAt(Vector3 pos)
    {
        GameObject root = new GameObject("Rock");
        root.transform.SetParent(vegetationParent);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Color rockColor = Color.Lerp(new Color(0.38f, 0.37f, 0.36f), new Color(0.55f, 0.53f, 0.50f), Random.value);
        int chunks = Random.Range(2, 4);
        for (int i = 0; i < chunks; i++)
        {
            GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chunk.name = "Chunk" + i;
            chunk.transform.SetParent(root.transform);
            chunk.transform.localPosition = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0.1f, 0.5f), Random.Range(-0.4f, 0.4f));
            chunk.transform.localRotation = Quaternion.Euler(Random.Range(0f, 40f), Random.Range(0f, 360f), Random.Range(0f, 40f));
            chunk.transform.localScale = new Vector3(Random.Range(0.8f, 1.6f), Random.Range(0.6f, 1.2f), Random.Range(0.8f, 1.6f));
            ApplyColor(chunk, Color.Lerp(rockColor, rockColor * 0.9f, Random.value));
        }

        AddBoundingInteractionCollider(root);
        float scale = Random.Range(0.7f, 2.2f);
        root.transform.localScale = Vector3.one * scale;
    }

    // Baeume/Felsen/Erzvorkommen bestehen aus mehreren kleinen, einzeln versetzten Primitiven.
    // Zwischen denen gibt es Luecken in der Kollisionshuelle, durch die ein duenner
    // Fadenkreuz-Raycast (z.B. fuer Abbauen/Interaktion) einfach hindurchgehen kann, ohne
    // etwas zu treffen. Deshalb zusaetzlich EINEN grossen, unsichtbaren BoxCollider auf den
    // gesamten sichtbaren Bereich legen, der garantiert immer getroffen wird.
    // WICHTIG: muss aufgerufen werden, SOLANGE root.transform.localScale noch (1,1,1) ist
    // (also vor dem abschliessenden Skalieren), sonst stimmen die Bounds nicht.
    private void AddBoundingInteractionCollider(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            worldBounds.Encapsulate(renderers[i].bounds);

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = root.transform.InverseTransformPoint(worldBounds.center);
        box.size = worldBounds.size;
    }

    private void ApplyColor(GameObject go, Color color)
    {
        Renderer r = go.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = color;
        r.sharedMaterial = mat;
    }

    // --- Ressourcen -------------------------------------------------------

    private void SpawnResources()
    {
        for (int i = 0; i < resourceAttempts; i++)
        {
            float worldX = Random.Range(0f, terrainSize.x);
            float worldZ = Random.Range(0f, terrainSize.z);

            float h = GetNormalizedHeight(worldX, worldZ);
            if (h < waterLevel) continue; // nicht unter Wasser spawnen

            ResourceType? chosen = PickResourceType(worldX, worldZ, h);
            if (chosen == null) continue;

            Vector3 worldPos = new Vector3(
                terrain.transform.position.x + worldX,
                0f,
                terrain.transform.position.z + worldZ);
            worldPos.y = terrain.SampleHeight(worldPos) + terrain.transform.position.y;

            if (IsTooClose(worldPos)) continue;

            SpawnResourceAt(worldPos, chosen.Value);
            placedResourcePositions.Add(worldPos);
        }
    }

    private ResourceType? PickResourceType(float worldX, float worldZ, float normalizedHeight)
    {
        // Jeder Ressourcentyp hat einen eigenen Cluster-Noise-Offset,
        // dadurch entstehen natuerliche, unabhaengige Adern.
        foreach (var entry in ResourceColors)
        {
            int offX = offsetMX + (int)entry.type * 1000;
            int offZ = offsetMZ + (int)entry.type * 1000;
            float cluster = GetClusterNoise(worldX, worldZ, resourceClusterScale, offX, offZ);
            if (cluster < resourceClusterThreshold) continue;

            switch (entry.type)
            {
                case ResourceType.Uranium:
                    if (normalizedHeight < mountainStart + 0.05f) continue;
                    break;
                case ResourceType.Oil:
                    if (normalizedHeight > waterLevel + 0.08f) continue;
                    break;
                case ResourceType.Iron:
                case ResourceType.Copper:
                case ResourceType.Coal:
                case ResourceType.Stone:
                    if (normalizedHeight > mountainStart + 0.15f) continue;
                    break;
            }

            return entry.type;
        }

        return null;
    }

    private bool IsTooClose(Vector3 pos)
    {
        float sqrMin = minResourceSpacing * minResourceSpacing;
        foreach (Vector3 p in placedResourcePositions)
        {
            if ((p - pos).sqrMagnitude < sqrMin) return true;
        }
        return false;
    }

    private void SpawnResourceAt(Vector3 pos, ResourceType type)
    {
        GameObject node;
        if (resourceNodePrefab != null)
        {
#if UNITY_EDITOR
            node = (GameObject)PrefabUtility.InstantiatePrefab(resourceNodePrefab, resourceParent);
            node.transform.position = pos;
#else
            node = Instantiate(resourceNodePrefab, pos, Quaternion.identity, resourceParent);
#endif
        }
        else
        {
            node = BuildProceduralResourceNode(type, pos);
        }

        node.name = "Resource_" + type;

        ResourceNode comp = node.GetComponent<ResourceNode>();
        if (comp == null) comp = node.AddComponent<ResourceNode>();
        comp.type = type;
        comp.amount = Random.Range(300, 900);
    }

    // Baut ein Erzvorkommen als Felsbrocken mit sichtbaren, farbigen Kristall-/Erzstuecken
    // obendrauf - statt eines einzelnen schwebenden Farbwuerfels.
    private GameObject BuildProceduralResourceNode(ResourceType type, Vector3 pos)
    {
        GameObject root = new GameObject("ResourceNode");
        root.transform.SetParent(resourceParent);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Color oreColor = Color.magenta;
        foreach (var entry in ResourceColors)
        {
            if (entry.type == type) { oreColor = entry.color; break; }
        }

        if (type == ResourceType.Oil)
        {
            // Oel: flache, dunkle Gesteinsscheiben mit einer glaenzend-schwarzen "Lache" obenauf.
            for (int i = 0; i < 3; i++)
            {
                GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = "Slab" + i;
                slab.transform.SetParent(root.transform);
                slab.transform.localPosition = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.02f, 0.12f), Random.Range(-0.6f, 0.6f));
                slab.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                slab.transform.localScale = new Vector3(Random.Range(1.2f, 2f), 0.15f, Random.Range(1.2f, 2f));
                ApplyColor(slab, new Color(0.28f, 0.26f, 0.22f));
            }

            GameObject pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pool.name = "OilPool";
            pool.transform.SetParent(root.transform);
            pool.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            pool.transform.localScale = new Vector3(1.3f, 0.03f, 1.3f);
            ApplyColor(pool, oreColor);

            AddBoundingInteractionCollider(root);
            root.transform.localScale = Vector3.one * Random.Range(1.3f, 2.2f);
            return root;
        }

        // Feste Erze: grauer Felsbrocken-Sockel + mehrere farbige Kristall-/Erzklumpen obendrauf.
        Color rockColor = new Color(0.4f, 0.39f, 0.38f);
        int baseChunks = Random.Range(2, 4);
        for (int i = 0; i < baseChunks; i++)
        {
            GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chunk.name = "BaseChunk" + i;
            chunk.transform.SetParent(root.transform);
            chunk.transform.localPosition = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0.1f, 0.35f), Random.Range(-0.35f, 0.35f));
            chunk.transform.localRotation = Quaternion.Euler(Random.Range(0f, 30f), Random.Range(0f, 360f), Random.Range(0f, 30f));
            chunk.transform.localScale = new Vector3(Random.Range(0.9f, 1.5f), Random.Range(0.5f, 0.9f), Random.Range(0.9f, 1.5f));
            ApplyColor(chunk, Color.Lerp(rockColor, rockColor * 0.9f, Random.value));
        }

        int oreBits = Random.Range(3, 6);
        for (int i = 0; i < oreBits; i++)
        {
            GameObject bit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bit.name = "OreBit" + i;
            bit.transform.SetParent(root.transform);
            bit.transform.localPosition = new Vector3(Random.Range(-0.45f, 0.45f), Random.Range(0.4f, 0.75f), Random.Range(-0.45f, 0.45f));
            bit.transform.localRotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
            bit.transform.localScale = Vector3.one * Random.Range(0.22f, 0.4f);
            ApplyColor(bit, Color.Lerp(oreColor, Color.white, 0.1f));
        }

        AddBoundingInteractionCollider(root);
        root.transform.localScale = Vector3.one * Random.Range(1.4f, 2.4f);
        return root;
    }
}
