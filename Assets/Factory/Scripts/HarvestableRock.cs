using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HarvestableRock : MonoBehaviour
{
    public int stoneAmount = 30;
    public int stonePerHit = 5;
    public int stages = 3;
    public GameObject chunkPrefab;
    public int chunkCount = 6;
    public Material particleMaterial;
    public Color dustColor = new Color(0.58f, 0.57f, 0.55f);
    public bool IsDepleted { get; private set; }
    private Vector3 baseScale;
    private int startAmount;
    private Coroutine punchRoutine;
    void Awake()
    {
        baseScale = transform.localScale;
        startAmount = Mathf.Max(1, stoneAmount);
    }
    public int Hit(Vector3 hitPoint, Vector3 hitDirection)
    {
        if (IsDepleted) return 0;

        int given = Mathf.Min(stonePerHit, stoneAmount);
        stoneAmount -= given;
        HarvestFX.Burst(hitPoint, dustColor, 16, particleMaterial);

        if (punchRoutine != null) StopCoroutine(punchRoutine);
        if (stoneAmount <= 0)
            StartCoroutine(Crumble());
        else
            punchRoutine = StartCoroutine(Punch(StageScale()));

        return given;
    }
    private Vector3 StageScale()
    {
        float remaining = (float)stoneAmount / startAmount;
        float stage = Mathf.Ceil(remaining * stages) / stages;
        return baseScale * Mathf.Lerp(0.5f, 1f, stage);
    }
    private IEnumerator Punch(Vector3 targetScale)
    {
        Vector3 from = transform.localScale;
        Vector3 squash = new Vector3(from.x * 1.06f, from.y * 0.88f, from.z * 1.06f);
        for (float t = 0f; t < 0.08f; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(from, squash, t / 0.08f);
            yield return null;
        }
        for (float t = 0f; t < 0.2f; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(squash, targetScale, t / 0.2f);
            yield return null;
        }
        transform.localScale = targetScale;
        punchRoutine = null;
    }
    private IEnumerator Crumble()
    {
        IsDepleted = true;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        List<GameObject> chunks = new List<GameObject>();
        if (chunkPrefab != null)
        {
            Collider playerCollider = FindPlayerCollider();
            float size = transform.localScale.x;
            for (int i = 0; i < chunkCount; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.3f, 0.9f), Random.Range(-0.6f, 0.6f)) * size;
                GameObject chunk = Instantiate(chunkPrefab, transform.position + offset, Random.rotation);
                chunk.transform.localScale = Vector3.one * size * Random.Range(0.8f, 1.3f);
                Rigidbody rb = chunk.GetComponent<Rigidbody>();
                if (rb == null) rb = chunk.AddComponent<Rigidbody>();
                Vector3 outward = new Vector3(offset.x, 0f, offset.z).normalized;
                rb.AddForce((outward * Random.Range(2f, 4f) + Vector3.up * Random.Range(3f, 5f)) * rb.mass, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 2f * rb.mass, ForceMode.Impulse);
                if (playerCollider != null)
                {
                    foreach (Collider c in chunk.GetComponentsInChildren<Collider>())
                        Physics.IgnoreCollision(c, playerCollider, true);
                }
                chunks.Add(chunk);
            }
        }

        yield return new WaitForSeconds(2.5f);

        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            foreach (GameObject chunk in chunks)
            {
                if (chunk != null)
                    chunk.transform.localScale = Vector3.Lerp(chunk.transform.localScale, Vector3.zero, t / 0.6f);
            }
            yield return null;
        }
        foreach (GameObject chunk in chunks)
        {
            if (chunk != null) Destroy(chunk);
        }
        Destroy(gameObject);
    }
    private static Collider FindPlayerCollider()
    {
        PlayerMovementScript player = Object.FindFirstObjectByType<PlayerMovementScript>();
        return player != null ? player.GetComponent<Collider>() : null;
    }
}
