using UnityEngine;

public class TeleportToFactoryMap : MonoBehaviour
{
    public KeyCode teleportKey = KeyCode.T;
    public Transform player;
    public Transform spawnPoint;
    void Update()
    {
        if (player == null || spawnPoint == null) return;
        if (!Input.GetKeyDown(teleportKey)) return;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        player.position = spawnPoint.position;
        Debug.Log("Zur Factory-Map teleportiert.");
    }
}
