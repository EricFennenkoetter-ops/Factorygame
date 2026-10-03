using UnityEngine;

public class HarvestableTree : MonoBehaviour
{
    public int woodAmount = 50;
    public int Chop(int requestedAmount)
    {
        int given = Mathf.Min(requestedAmount, woodAmount);
        woodAmount -= given;

        if (woodAmount <= 0)
        {
            Destroy(gameObject);
        }

        return given;
    }
}
