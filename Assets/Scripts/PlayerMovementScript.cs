using System.Diagnostics;
using UnityEngine;

public class PlayerMovementScript : MonoBehaviour
{
    public float moveSpeed;

    public float groundDrag;

    public float jumpforce;
    public float jumpCooldown;
    public float airMultiplier;
    public float sprintSpeed;
    float airSprint;
    float sprintMultiplier;
    bool readyToJump;

    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    public float playerHeight;
    public LayerMask ground;
    public bool grounded;
    public bool crouching;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;
    CapsuleCollider capsuleCollider;
    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        capsuleCollider = GetComponent<CapsuleCollider>();
        airSprint = 1f;
        readyToJump = true;
        crouching = false;
    }

    void Update()
    {
        grounded = Physics.Raycast(capsuleCollider.bounds.center, Vector3.down, capsuleCollider.bounds.extents.y + 0.2f, ground);
        
        MyInput();
        SpeedControl();

        if(grounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0;
    }

    private void FixedUpdate(){
        MovePlayer();
        ApplyCustomGravity();
    }

    private void MyInput(){
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        bool wasCrouching = crouching;
        crouching = Input.GetKey(crouchKey) || (crouching && !CanStandUp());

        if(crouching != wasCrouching){
            capsuleCollider.height = crouching ? playerHeight * 0.75f : playerHeight;
            capsuleCollider.center = new Vector3(0, capsuleCollider.height * 0.5f - playerHeight * 0.5f, 0);
        }

        if(Input.GetKey(sprintKey) && grounded && !crouching)
            sprintMultiplier = sprintSpeed;
        else if (grounded) 
            sprintMultiplier = 1f;

        if(Input.GetKeyDown(jumpKey) && readyToJump && grounded){
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer(){
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        if(grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * sprintMultiplier * 10f, ForceMode.Force);
        else rb.AddForce(moveDirection.normalized * moveSpeed * sprintMultiplier * 10f * airMultiplier, ForceMode.Force);
    }

    private void SpeedControl(){
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if(flatVel.magnitude > moveSpeed*sprintMultiplier){
            Vector3 limitedVel;
            limitedVel = flatVel.normalized * moveSpeed * sprintMultiplier;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    private void Jump(){
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * jumpforce, ForceMode.Impulse);
    }

    private void ResetJump(){
        readyToJump = true;
    }

    private bool CanStandUp(){
        // true = genug Platz nach oben, um voll aufzustehen.
        // (Alte Version war invertiert UND haette den eigenen Player/Character mitgetroffen.)
        float need = (playerHeight - capsuleCollider.height) * Mathf.Abs(transform.lossyScale.y) + 0.05f;
        if (need <= 0f) return true;
        Vector3 top = capsuleCollider.bounds.center + Vector3.up * capsuleCollider.bounds.extents.y;
        float r = capsuleCollider.radius * Mathf.Abs(transform.lossyScale.x) * 0.95f;
        int mask = ~((1 << gameObject.layer) | (1 << 2));   // eigener Layer + Ignore Raycast raus
        return !Physics.SphereCast(top - Vector3.up * r, r, Vector3.up, out _, need + r, mask, QueryTriggerInteraction.Ignore);
    }

    private void ApplyCustomGravity(){
        if(rb.linearVelocity.y < 0)
            rb.AddForce(Vector3.up * Physics.gravity.y * (fallMultiplier - 1), ForceMode.Acceleration);
        else if(rb.linearVelocity.y > 0 && !Input.GetKey(jumpKey))
            rb.AddForce(Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1), ForceMode.Acceleration);
    }
}
