#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;

// Menue: Tools > Casino Sim > Diagnose  (funktioniert auch im Play-Mode)
public static class CharacterDiag
{
    [MenuItem("Tools/Casino Sim/Diagnose")]
    public static void Run()
    {
        var move = Object.FindFirstObjectByType<PlayerMovementScript>();
        var camRot = Object.FindFirstObjectByType<CameraRotation>();
        var xbot = GameObject.Find("X Bot");
        var cam = camRot ? camRot.GetComponent<Camera>() : Camera.main;

        string s = "=== Character Diagnose ===\n";
        if (move)
        {
            var rb = move.GetComponent<Rigidbody>();
            s += $"player      world={move.transform.position}  scale={move.transform.lossyScale}\n";
            if (rb) s += $"player      velocity={rb.linearVelocity}  |horiz|={new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude:0.00}\n";
        }
        if (cam)  s += $"camera      world={cam.transform.position}  near={cam.nearClipPlane}\n";
        if (xbot)
        {
            s += $"X Bot       world={xbot.transform.position}  local={xbot.transform.localPosition}\n";
            s += $"X Bot       lossyScale={xbot.transform.lossyScale}  parent={xbot.transform.parent?.name}\n";
            var smrs = xbot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var r in smrs)
                s += $"  renderer {r.name}: enabled={r.enabled} isVisible={r.isVisible} bounds.center={r.bounds.center} size={r.bounds.size}\n";
            var anim = xbot.GetComponent<Animator>();
            if (anim)
            {
                s += $"  animator: culling={anim.cullingMode} rootMotion={anim.applyRootMotion} avatarValid={anim.avatar && anim.avatar.isValid}\n";
                if (Application.isPlaying)
                {
                    float sp = anim.GetFloat("Speed");
                    bool cr = anim.GetBool("Crouch");
                    string state = anim.GetCurrentAnimatorClipInfoCount(0) > 0
                        ? anim.GetCurrentAnimatorClipInfo(0)[0].clip.name : "(leer/Idle)";
                    s += "  animator: Speed=" + sp.ToString("0.00") + "  Crouch=" + cr + "  clip=" + state + "\n";
                }
            }
        }
        else s += "X Bot NICHT gefunden (GameObject.Find)\n";

        if (cam && xbot)
            s += $"Distanz Kamera<->X Bot = {Vector3.Distance(cam.transform.position, xbot.transform.position):0.00} m\n";

        // CanStandUp-Raycast nachstellen
        if (move)
        {
            var cc = move.GetComponent<CapsuleCollider>();
            s += $"\nplayer.crouching = {move.crouching}   player.layer = {LayerMask.LayerToName(move.gameObject.layer)} ({move.gameObject.layer})\n";
            if (cc)
            {
                float ph = 2f; // playerHeight ist private - Standardwert
                var pf = typeof(PlayerMovementScript).GetField("playerHeight",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (pf != null) ph = (float)pf.GetValue(move);
                float need = (ph - cc.height) * Mathf.Abs(move.transform.lossyScale.y) + 0.05f;
                Vector3 top = cc.bounds.center + Vector3.up * cc.bounds.extents.y;
                float rr = cc.radius * Mathf.Abs(move.transform.lossyScale.x) * 0.95f;
                int mask = ~((1 << move.gameObject.layer) | (1 << 2));
                bool blocked = Physics.SphereCast(top - Vector3.up * rr, rr, Vector3.up, out var hit, need + rr, mask, QueryTriggerInteraction.Ignore);
                s += blocked
                    ? $"CanStandUp: BLOCKIERT von '{hit.collider.name}' @ {hit.distance:0.00}m (need {need:0.00})\n"
                    : $"CanStandUp: FREI (need {need:0.00}m Platz, cc.height {cc.height:0.00})\n";
            }
        }

        Debug.Log(s);
        EditorUtility.DisplayDialog("Diagnose", s, "OK");
    }
}
#endif
