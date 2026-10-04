using System.Collections;
using UnityEngine;

public class HarvestableTree : MonoBehaviour
{
    public int woodAmount = 50;
    public int woodPerHit = 10;
    public GameObject stumpPrefab;
    public Material particleMaterial;
    public Color chipColor = new Color(0.62f, 0.45f, 0.28f);
    public bool IsFelled { get; private set; }
    private Quaternion baseRotation;
    private Coroutine shakeRoutine;
    void Awake()
    {
        baseRotation = transform.rotation;
    }
    public int Hit(Vector3 hitPoint, Vector3 hitDirection)
    {
        if (IsFelled) return 0;

        int given = Mathf.Min(woodPerHit, woodAmount);
        woodAmount -= given;
        HarvestFX.Burst(hitPoint, chipColor, 14, particleMaterial);

        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        if (woodAmount <= 0)
            StartCoroutine(Fall(hitDirection));
        else
            shakeRoutine = StartCoroutine(Shake(hitDirection));

        return given;
    }
    private Vector3 TiltAxis(Vector3 direction)
    {
        Vector3 flat = new Vector3(direction.x, 0f, direction.z);
        if (flat.sqrMagnitude < 0.0001f) flat = transform.forward;
        return Vector3.Cross(Vector3.up, flat.normalized);
    }
    private IEnumerator Shake(Vector3 direction)
    {
        Vector3 axis = TiltAxis(direction);
        float duration = 0.35f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float angle = Mathf.Sin(t * 45f) * 3.5f * (1f - t / duration);
            transform.rotation = Quaternion.AngleAxis(angle, axis) * baseRotation;
            yield return null;
        }
        transform.rotation = baseRotation;
        shakeRoutine = null;
    }
    private IEnumerator Fall(Vector3 direction)
    {
        IsFelled = true;
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        if (stumpPrefab != null)
        {
            GameObject stump = Instantiate(stumpPrefab, transform.position, baseRotation, transform.parent);
            stump.transform.localScale = Vector3.Scale(stump.transform.localScale, transform.localScale);
        }

        Vector3 axis = TiltAxis(direction);
        float fallTime = 1.6f;
        for (float t = 0f; t < fallTime; t += Time.deltaTime)
        {
            float k = t / fallTime;
            transform.rotation = Quaternion.AngleAxis(88f * k * k, axis) * baseRotation;
            yield return null;
        }
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            float bounce = Mathf.Sin(t / 0.4f * Mathf.PI) * 4f;
            transform.rotation = Quaternion.AngleAxis(88f - bounce, axis) * baseRotation;
            yield return null;
        }
        transform.rotation = Quaternion.AngleAxis(88f, axis) * baseRotation;

        yield return new WaitForSeconds(1.2f);

        Vector3 startScale = transform.localScale;
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / 0.8f);
            yield return null;
        }
        Destroy(gameObject);
    }
}
