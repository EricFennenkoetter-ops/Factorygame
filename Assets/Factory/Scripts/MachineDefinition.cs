using UnityEngine;

[System.Serializable]
public class MachineDefinition
{
    public string machineName = "Machine";
    public Color color = Color.gray;
    public Vector2 footprint = new Vector2(2f, 2f);
    public float height = 1.5f;
    public Vector2 visualSize = Vector2.zero;
    public Vector2 GetVisualSize()
    {
        return visualSize.x > 0f && visualSize.y > 0f ? visualSize : footprint;
    }
    public static MachineDefinition[] BuildDefaultSet()
    {
        return new MachineDefinition[]
        {
            new MachineDefinition { machineName = "Miner", color = new Color(0.75f, 0.5f, 0.15f), footprint = new Vector2(2f, 2f), height = 1.8f },
            new MachineDefinition { machineName = "Smelter", color = new Color(0.65f, 0.2f, 0.15f), footprint = new Vector2(2f, 2f), height = 2.2f },
            new MachineDefinition { machineName = "Constructor", color = new Color(0.2f, 0.45f, 0.8f), footprint = new Vector2(2f, 2f), height = 1.5f },
            new MachineDefinition { machineName = "Assembler", color = new Color(0.55f, 0.25f, 0.65f), footprint = new Vector2(4f, 4f), height = 2f },
            new MachineDefinition { machineName = "Conveyor", color = new Color(0.5f, 0.5f, 0.5f), footprint = new Vector2(2f, 2f), visualSize = new Vector2(2f, 0.7f), height = 0.4f },
            new MachineDefinition { machineName = "Storage", color = new Color(0.35f, 0.35f, 0.15f), footprint = new Vector2(2f, 2f), height = 2f },
        };
    }
}
