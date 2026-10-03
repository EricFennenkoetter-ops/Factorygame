using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CraftingMenuController : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.C;
    public List<CraftingRecipe> recipes;
    public float panelWidth = 320f;
    public float rowHeight = 46f;
    public float maxRecipeListHeight = 260f;
    public bool MenuOpen { get; private set; }
    private int hoveredIndex = -1;
    private Vector2 recipeScroll;
    void Start()
    {
        if (recipes == null || recipes.Count == 0)
            recipes = CraftingRecipe.BuildDefaultSet();
    }
    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) SetMenuOpen(!MenuOpen);
    }
    private void SetMenuOpen(bool open)
    {
        MenuOpen = open;

        if (open)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            CameraRotation.LookEnabled = false;
            HotbarScript.InputBlocked = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            CameraRotation.LookEnabled = true;
            HotbarScript.InputBlocked = false;
        }
    }
    void OnGUI()
    {
        if (!MenuOpen || recipes == null) return;

        Dictionary<string, int> inventory = FactoryItemBridge.GetSnapshot();

        List<string> invKeys = new List<string>(inventory.Keys);
        invKeys.Sort();

        float x = Screen.width - panelWidth - 12f;
        float y = 12f;

        float inventoryLineCount = Mathf.Max(1, invKeys.Count);
        float inventorySectionHeight = 24f + inventoryLineCount * 18f + 10f;

        float fullRecipesHeight = recipes.Count * rowHeight;
        float recipeListViewHeight = Mathf.Min(fullRecipesHeight, maxRecipeListHeight);
        float recipesSectionHeight = 24f + recipeListViewHeight;

        float panelHeight = Mathf.Min(Screen.height - 24f, inventorySectionHeight + recipesSectionHeight + 16f);

        GUI.color = Color.white;
        GUI.Box(new Rect(x, y, panelWidth, panelHeight), "CRAFTING (C zum Schliessen)");

        float curY = y + 26f;

        GUI.Label(new Rect(x + 10f, curY, panelWidth - 20f, 18f), "Inventar:");
        curY += 20f;

        if (invKeys.Count == 0)
        {
            GUI.Label(new Rect(x + 16f, curY, panelWidth - 26f, 18f), "(Hotbar leer - mit 'H' an Baeumen/Erz abbauen)");
            curY += 18f;
        }
        else
        {
            foreach (string key in invKeys)
            {
                GUI.Label(new Rect(x + 16f, curY, panelWidth - 26f, 18f), key + ": " + inventory[key]);
                curY += 18f;
            }
        }

        curY += 10f;
        GUI.Label(new Rect(x + 10f, curY, panelWidth - 20f, 18f), "Herstellen:");
        curY += 20f;

        hoveredIndex = -1;
        Event e = Event.current;

        Rect viewRect = new Rect(x + 4f, curY, panelWidth - 8f, recipeListViewHeight);
        Rect contentRect = new Rect(0f, 0f, panelWidth - 24f, fullRecipesHeight);
        recipeScroll = GUI.BeginScrollView(viewRect, recipeScroll, contentRect);

        float rowY = 0f;
        for (int i = 0; i < recipes.Count; i++)
        {
            CraftingRecipe recipe = recipes[i];
            Rect rowRect = new Rect(4f, rowY, contentRect.width - 8f, rowHeight - 4f);

            bool canCraft = FactoryItemBridge.HasEnough(recipe.costs);

            if (rowRect.Contains(e.mousePosition)) hoveredIndex = i;

            GUI.color = canCraft ? Color.white : new Color(1f, 0.55f, 0.55f);
            if (GUI.Button(rowRect, recipe.itemName + "  x" + recipe.outputAmount))
            {
                if (canCraft)
                {
                    FactoryItemBridge.Consume(recipe.costs);
                    FactoryItemBridge.Add(recipe.itemName, recipe.outputAmount);
                }
            }

            rowY += rowHeight;
        }

        GUI.color = Color.white;
        GUI.EndScrollView();

        if (hoveredIndex >= 0)
        {
            DrawTooltip(recipes[hoveredIndex], e.mousePosition);
        }
    }
    private void DrawTooltip(CraftingRecipe recipe, Vector2 mousePos)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Benoetigt:");
        foreach (CraftCost c in recipe.costs)
        {
            int have = FactoryItemBridge.GetAmount(c.itemName);
            sb.AppendLine("- " + c.itemName + ": " + have + " / " + c.amount);
        }

        string text = sb.ToString();
        float width = 220f;
        float height = 20f + recipe.costs.Count * 18f;
        float tx = mousePos.x - width - 10f;
        if (tx < 0f) tx = mousePos.x + 10f;

        GUI.Box(new Rect(tx, mousePos.y, width, height), text);
    }
}
