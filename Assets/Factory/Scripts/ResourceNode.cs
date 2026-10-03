using UnityEngine;

public enum ResourceType
{
    Iron,
    Copper,
    Coal,
    Stone,
    Oil,
    Uranium
}

public class ResourceNode : MonoBehaviour
{
    public ResourceType type;
    public int amount = 500;
    public int amountPerHit = 10;
    public int Mine(int requestedAmount)
    {
        int mined = Mathf.Min(requestedAmount, amount);
        amount -= mined;

        if (amount <= 0)
        {
            Debug.Log(name + " ist erschoepft.");
            Destroy(gameObject);
        }

        return mined;
    }
}
