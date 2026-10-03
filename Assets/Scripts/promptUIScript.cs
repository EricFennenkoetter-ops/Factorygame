using UnityEngine;

public class promptUIScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject promptUI; // "Press E" text
    public GameLogicScript LogicScript;
    public GameObject player;

    void Start()
    {
        promptUI.SetActive(false);
    }

    void Update()
    {
        float distance = Vector3.Distance(player.transform.position, transform.position);
        if (distance < 3f)
        {
            promptUI.SetActive(true);
            if (Input.GetKeyDown(KeyCode.E))
            {
                LogicScript.changeItemInHotbar(gameObject, 1);
                gameObject.SetActive(false);
            }
            
        }
    }

    
}
