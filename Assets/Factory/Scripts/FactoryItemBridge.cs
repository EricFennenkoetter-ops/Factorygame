using System.Collections.Generic;
using UnityEngine;

public static class FactoryItemBridge
{
    private static GameLogicScript logicScript;
    private static Transform templateContainer;
    private static readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
    private static readonly Dictionary<string, Color> ItemColors = new Dictionary<string, Color>
    {
        { "Iron", new Color(0.72f, 0.45f, 0.35f) },
        { "Copper", new Color(0.85f, 0.5f, 0.2f) },
        { "Coal", new Color(0.12f, 0.12f, 0.12f) },
        { "Stone", new Color(0.6f, 0.6f, 0.6f) },
        { "Oil", new Color(0.05f, 0.05f, 0.05f) },
        { "Uranium", new Color(0.3f, 0.85f, 0.3f) },
        { "Wood", new Color(0.45f, 0.3f, 0.15f) },
        { "Iron Plate", new Color(0.8f, 0.8f, 0.85f) },
        { "Copper Wire", new Color(0.9f, 0.55f, 0.25f) },
        { "Wood Plank", new Color(0.55f, 0.38f, 0.2f) },
        { "Concrete", new Color(0.75f, 0.75f, 0.72f) },
        { "Gear", new Color(0.55f, 0.55f, 0.6f) },
        { "Steel Beam", new Color(0.4f, 0.42f, 0.46f) },
    };
    private static GameLogicScript GetLogicScript()
    {
        if (logicScript == null)
            logicScript = Object.FindFirstObjectByType<GameLogicScript>();
        return logicScript;
    }
    private static GameObject GetOrCreateTemplate(string itemName)
    {
        if (templates.TryGetValue(itemName, out GameObject existing) && existing != null)
            return existing;

        if (templateContainer == null)
        {
            GameObject containerGO = new GameObject("Factory Item Templates");
            containerGO.SetActive(false);
            templateContainer = containerGO.transform;
        }

        GameObject template = GameObject.CreatePrimitive(PrimitiveType.Cube);
        template.name = itemName;
        template.transform.SetParent(templateContainer);
        template.transform.localScale = Vector3.one * 0.3f;

        Color color = ItemColors.TryGetValue(itemName, out Color c) ? c : Color.white;
        Renderer r = template.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = color;
        r.sharedMaterial = mat;

        template.AddComponent<Rigidbody>();

        templates[itemName] = template;
        return template;
    }
    public static void Add(string itemName, int amount)
    {
        if (string.IsNullOrEmpty(itemName) || amount == 0) return;
        GameLogicScript script = GetLogicScript();
        if (script == null)
        {
            Debug.LogWarning("FactoryItemBridge: kein GameLogicScript in der Scene gefunden - Item '" + itemName + "' konnte nicht in die Hotbar gelegt werden.");
            return;
        }
        script.changeItemInHotbar(GetOrCreateTemplate(itemName), amount);
    }
    public static int GetAmount(string itemName)
    {
        GameLogicScript script = GetLogicScript();
        if (script == null || script.Hotbar == null) return 0;

        foreach (GameLogicScript.HotbarItem slot in script.Hotbar)
        {
            if (slot != null && slot.item != null && slot.item.name == itemName)
                return slot.amount;
        }
        return 0;
    }
    public static bool HasEnough(List<CraftCost> costs)
    {
        foreach (CraftCost c in costs)
        {
            if (GetAmount(c.itemName) < c.amount) return false;
        }
        return true;
    }
    public static void Consume(List<CraftCost> costs)
    {
        foreach (CraftCost c in costs)
            Add(c.itemName, -c.amount);
    }
    public static Dictionary<string, int> GetSnapshot()
    {
        Dictionary<string, int> result = new Dictionary<string, int>();
        GameLogicScript script = GetLogicScript();
        if (script == null || script.Hotbar == null) return result;

        foreach (GameLogicScript.HotbarItem slot in script.Hotbar)
        {
            if (slot != null && slot.item != null)
                result[slot.item.name] = slot.amount;
        }
        return result;
    }
}
