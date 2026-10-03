using UnityEngine;

public class cameraMovementScript : MonoBehaviour
{
    public KeyCode crouchKey = KeyCode.LeftControl;

    public Transform refferenceObject;

    public float headheight;
    bool crouching;

    void Update()
    {
        if(Input.GetKey(crouchKey)){
            crouching = true;
        }
        else if(!Physics.Raycast(transform.position, Vector3.up, headheight * 0.5f)){
            crouching = false;
        }
        Vector3 newV = new Vector3(refferenceObject.position.x, refferenceObject.position.y + (crouching ? headheight * 0.75f : headheight), refferenceObject.position.z);
        transform.position = newV;
    }
}
