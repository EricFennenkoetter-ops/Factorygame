using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class GameLogicScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int coins = 0;
    public HotbarItem[] Hotbar;
    public GameObject TestCube;
    public GameObject testSphere;
    public HotbarScript  HotbarScript;
    public class HotbarItem
    {
        public GameObject item;
        public int amount;
        public HotbarItem(GameObject item, int amount)
        {
            this.item = item;
            this.amount = amount;
        }
    }

    void Start()
    {
        Hotbar = new HotbarItem[10];
        changeItemInHotbar(testSphere, 1);
        changeItemInHotbar(TestCube, 1);
        Debug.Log("Item at pos 0: "+Hotbar[0].item.name+",Amount:"+ Hotbar[0].amount);
        Debug.Log("Item at pos 1: " + Hotbar[1].item.name + ",Amount:" + Hotbar[1].amount);
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.F))
        {
            sellItem();
        }
    }
    public void changeCoins(int amount)
    {
        coins += amount;
    }
    public bool ItemExists(GameObject itemToCheck)
    {
        for (int i = 0; i < Hotbar.Length; i++)
        {
            if (Hotbar[i] != null && Hotbar[i].item.name == itemToCheck.name)
            {
                Debug.Log(itemToCheck.name + " already existant");
                return true;
            }
        }
        Debug.Log(itemToCheck.name + " not existant yet");
        return false; // not found

    }
    public int FindItemIndex(GameObject itemToCheck)
    {
        for (int i = 0; i < Hotbar.Length; i++)
        {
            if (Hotbar[i] != null && Hotbar[i].item.name == itemToCheck.name)
                return i; // return index where the item is
        }
        return -1; // not found
    }
    public int FindEmptyIndex()
    {
        for (int i = 0; i < Hotbar.Length; i++)
        {
            if (Hotbar[i]==null)
                return i; // return index where the item is
        }
        return -1; // not found
    }
    public void changeItemInHotbar(GameObject O,int pAmount)
    {
        if (ItemExists(O))
        {
            int index = FindItemIndex(O);
            if (Hotbar[index].amount + pAmount == 0)
            {
                Debug.Log("x");
                Hotbar[index] = null;
               // Hotbar[index].item = null;
               // Hotbar[index].amount = 0;

            }
            else Hotbar[index].amount += pAmount;
        }
        else
        {
            int index = FindEmptyIndex();
            if (index >= 0)
            {
                Hotbar[index] = new(O, pAmount);
                Debug.Log(O.name + " was added to the Hotbar at index:"+index);
            }else Debug.Log(O.name + " could not be added, Hotbar is already Full");
        }
    }
    public void sellItem()
    {
        int index = HotbarScript.HotbarElement - 1;
        if (index < 0 || index >= Hotbar.Length || Hotbar[index] == null) return;

        if (Hotbar[HotbarScript.HotbarElement-1].item != null)
        {
            var Element = Hotbar[HotbarScript.HotbarElement-1];
            if (Element.item.TryGetComponent<Variables>(out var variables))
            {
                if (Hotbar[HotbarScript.HotbarElement - 1].amount > 0)
                {
                    Hotbar[HotbarScript.HotbarElement - 1].amount--;
                    coins += Element.item.GetComponent<Variables>().declarations.Get<int>("Value");
                    if (Hotbar[HotbarScript.HotbarElement - 1].amount == 0) Hotbar[HotbarScript.HotbarElement - 1] = null;
                }
            }
        }
    }

}
