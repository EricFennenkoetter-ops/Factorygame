using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CraftCost
{
    public string itemName;
    public int amount;
}

[System.Serializable]
public class CraftingRecipe
{
    public string itemName;
    public int outputAmount = 1;
    public List<CraftCost> costs = new List<CraftCost>();
    public static List<CraftingRecipe> BuildDefaultSet()
    {
        return new List<CraftingRecipe>
        {
            new CraftingRecipe
            {
                itemName = "Iron Plate", outputAmount = 1,
                costs = new List<CraftCost> { new CraftCost { itemName = "Iron", amount = 2 } }
            },
            new CraftingRecipe
            {
                itemName = "Copper Wire", outputAmount = 2,
                costs = new List<CraftCost> { new CraftCost { itemName = "Copper", amount = 1 } }
            },
            new CraftingRecipe
            {
                itemName = "Wood Plank", outputAmount = 2,
                costs = new List<CraftCost> { new CraftCost { itemName = "Wood", amount = 1 } }
            },
            new CraftingRecipe
            {
                itemName = "Concrete", outputAmount = 1,
                costs = new List<CraftCost> { new CraftCost { itemName = "Stone", amount = 3 } }
            },
            new CraftingRecipe
            {
                itemName = "Gear", outputAmount = 1,
                costs = new List<CraftCost> { new CraftCost { itemName = "Iron Plate", amount = 2 } }
            },
            new CraftingRecipe
            {
                itemName = "Steel Beam", outputAmount = 1,
                costs = new List<CraftCost>
                {
                    new CraftCost { itemName = "Iron Plate", amount = 3 },
                    new CraftCost { itemName = "Coal", amount = 2 }
                }
            },
        };
    }
}
