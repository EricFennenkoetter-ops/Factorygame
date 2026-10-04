using UnityEngine;

public static class HarvestFX
{
    private static Material fallbackMaterial;
    private static Mesh chipMesh;
    public static void Burst(Vector3 position, Color color, int count, Material material)
    {
        GameObject go = new GameObject("HarvestBurst");
        go.SetActive(false);
        go.transform.position = position;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        Color dark = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(color, dark);
        main.gravityModifier = 1.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.maxParticles = count;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ParticleSystemRenderer psRenderer = go.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        psRenderer.mesh = GetChipMesh();
        psRenderer.material = material != null ? material : GetFallbackMaterial();

        go.SetActive(true);
    }
    private static Mesh GetChipMesh()
    {
        if (chipMesh == null)
        {
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chipMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(tmp);
        }
        return chipMesh;
    }
    private static Material GetFallbackMaterial()
    {
        if (fallbackMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            fallbackMaterial = new Material(shader);
        }
        return fallbackMaterial;
    }
}
