using UnityEngine;
using TMPro;

public class ViewCoinsScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TMP_Text Text;
    public GameLogicScript LogicScript;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Text.text = LogicScript.coins.ToString();
    }
}
