using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class HotbarScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int HotbarElement=1;
    public GameLogicScript LogicScript;
    public GameObject cam;

    public static bool InputBlocked = false;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!InputBlocked)
        {
        // Liest vertikales Scrollen (Mausrad)
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            for (int a = 0; a < 10; a++)
                transform.GetChild(a).GetComponent<Image>().color = new Color(0f, 0f, 0f, 1f);

            transform.GetChild(9).GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            HotbarElement = 10;
        }
        else
        {
            for (int i = 1; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    for (int a = 0; a < 10; a++)
                        transform.GetChild(a).GetComponent<Image>().color = new Color(0f, 0f, 0f, 1f);

                    HotbarElement = i;
                    transform.GetChild(i - 1).GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
                }
            }
        }
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            if (HotbarElement <= 1)
            {
                HotbarElement = 10;
                transform.GetChild(0).GetComponent<Image>().color = new Color(0f, 0f, 0f, 1f);
                transform.GetChild(9).GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            }
            else
            {
                transform.GetChild(HotbarElement - 1).GetComponent<Image>().color = new Color(0f, 0f, 0f, 1f);
                HotbarElement--;
                transform.GetChild(HotbarElement - 1).GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            }
        }
        else if (scroll < 0f)
        {
            if (HotbarElement >= 10)
            {
                HotbarElement = 1;
                transform.GetChild(9).GetComponent<Image>().color = new Color(0f, 0f, 0f, 1f);
                transform.GetChild(0).GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            }
            else
            {
                transform.GetChild(HotbarElement -1).GetComponent<Image>().color = new Color(0f, 0f, 0f, 1f);
                HotbarElement++;
                transform.GetChild(HotbarElement - 1).GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            }
        }
        if (Input.GetKeyDown(KeyCode.Q) && HotbarElement >= 1 && HotbarElement <= LogicScript.Hotbar.Length)
        {
            var currItem = LogicScript.Hotbar[HotbarElement - 1];
            if (currItem!= null&&currItem.item != null){
                string name = currItem.item.name;
                var droppedItem = Instantiate(currItem.item, cam.transform.position, Quaternion.identity);
                droppedItem.name = name;
                droppedItem.SetActive(true);

                Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
                if (rb == null) rb = droppedItem.AddComponent<Rigidbody>();
                rb.AddForce(Vector3.forward * 100f);

                DroppedItemPickup pickup = droppedItem.AddComponent<DroppedItemPickup>();
                pickup.sourceItem = currItem.item;
                pickup.amount = 1;
                pickup.logicScript = LogicScript;

                PlayerMovementScript playerMove = FindFirstObjectByType<PlayerMovementScript>();
                if (playerMove != null)
                {
                    Collider playerCollider = playerMove.GetComponent<Collider>();
                    if (playerCollider != null)
                    {
                        foreach (Collider itemCollider in droppedItem.GetComponentsInChildren<Collider>())
                            Physics.IgnoreCollision(itemCollider, playerCollider, true);
                    }
                }

                //LogicScript.changeItemInHotbar(currItem.item, -currItem.amount);
                LogicScript.changeItemInHotbar(currItem.item, -1);
            }
        }
        }

        for (int i = 0; i < LogicScript.Hotbar.Length; i++)
        {
            var slot = transform.GetChild(i);
            var itemText = slot.Find("ItemText").GetComponent<TMP_Text>();
            var itemAmount = slot.Find("ItemAmount").GetComponent<TMP_Text>();
            if (LogicScript.Hotbar[i] != null && LogicScript.Hotbar[i].item != null)
            {
                itemText.text = LogicScript.Hotbar[i].item.name;
                itemAmount.text = LogicScript.Hotbar[i].amount.ToString();
            }
            else
            {
                itemText.text = "";
                itemAmount.text = "";
            }
        }
    }
}
