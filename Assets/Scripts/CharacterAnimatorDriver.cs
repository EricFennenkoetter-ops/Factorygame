using UnityEngine;

// Treibt den Mixamo-Character-Animator aus der Bewegung des Player-Rigidbodies.
// Animator-Parameter:
//   "Speed"    (float) - 0 = stehen, 1 = laufen  (echte Bewegung, mit Deadzone)
//   "Crouch"   (bool)  - Crouch-Taste gehalten
//   "Sprint"   (bool)  - Sprint-Taste gehalten UND in Bewegung UND am Boden
//   "Grounded" (bool)  - Player steht auf dem Boden (aus PlayerMovementScript)
// Ausserdem: Koerper dreht in Blickrichtung (orientation).
[RequireComponent(typeof(Animator))]
public class CharacterAnimatorDriver : MonoBehaviour
{
    [Header("Referenzen")]
    public Rigidbody body;            // Rigidbody des Player-Objekts
    public Transform orientation;     // dasselbe "orientation"-Transform wie in PlayerMovementScript

    [Header("Einstellungen")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float speedDampTime = 0.12f;
    public float turnSpeed = 20f;      // wie schnell der Koerper der Blickrichtung folgt
    public float moveDeadzone = 0.4f;  // unter dieser echten Geschwindigkeit (m/s) = stehen

    [Header("Anim-Tempo (gegen Fuss-Schlittern)")]
    public float walkClipSpeed = 1.5f;   // ~ Meter/s die der Walk/Crouch-Clip "laeuft"
    public float runClipSpeed = 4.0f;    // ~ Meter/s die der Sprint-Clip "laeuft"

    Animator anim;
    PlayerMovementScript move;
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int CrouchHash = Animator.StringToHash("Crouch");
    static readonly int SprintHash = Animator.StringToHash("Sprint");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");
    static readonly int MoveMulHash = Animator.StringToHash("MoveMul");
    float moveMul = 1f;

    Vector3 lastFlatPos;
    bool hasLast;

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (body == null && transform.parent != null)
            body = transform.parent.GetComponentInParent<Rigidbody>();
        if (body != null) move = body.GetComponent<PlayerMovementScript>();
    }

    void Update()
    {
        // Echte horizontale Bewegung des Players messen - robust gegen Rigidbody-Restdrift.
        Transform src = body != null ? body.transform
                       : (transform.parent != null ? transform.parent : transform);
        Vector3 pos = src.position; pos.y = 0f;

        float instSpeed = 0f;
        if (hasLast && Time.deltaTime > 0f)
            instSpeed = Vector3.Distance(pos, lastFlatPos) / Time.deltaTime;
        lastFlatPos = pos;
        hasLast = true;

        bool moving = instSpeed > moveDeadzone;
        bool grounded = move != null ? move.grounded : true;
        bool sprinting = grounded && moving && Input.GetKey(sprintKey);

        anim.SetFloat(SpeedHash, moving ? 1f : 0f, speedDampTime, Time.deltaTime);
        anim.SetBool(CrouchHash, Input.GetKey(crouchKey) && grounded);
        anim.SetBool(SprintHash, sprinting);
        anim.SetBool(GroundedHash, grounded);

        // Abspieltempo an echte Geschwindigkeit koppeln (nur wenn wirklich in Bewegung)
        float refSpeed = sprinting ? runClipSpeed : walkClipSpeed;
        float targetMul = moving ? Mathf.Clamp(instSpeed / Mathf.Max(refSpeed, 0.1f), 0.6f, 2.2f) : 1f;
        moveMul = Mathf.Lerp(moveMul, targetMul, 10f * Time.deltaTime);
        anim.SetFloat(MoveMulHash, moveMul);

        if (orientation != null)
        {
            Quaternion rot = Quaternion.Euler(0f, orientation.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, turnSpeed * Time.deltaTime);
        }
    }
}
