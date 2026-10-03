using UnityEngine;

public static class FactorySystemsBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureSystems()
    {
        GameObject managerGO = GameObject.Find("Factory Build Manager");
        if (managerGO == null)
            managerGO = new GameObject("Factory Build Manager");

        if (managerGO.GetComponent<BuildModeController>() == null)
            managerGO.AddComponent<BuildModeController>();

        if (managerGO.GetComponent<HarvestInteraction>() == null)
            managerGO.AddComponent<HarvestInteraction>();

        if (managerGO.GetComponent<CraftingMenuController>() == null)
            managerGO.AddComponent<CraftingMenuController>();

        if (managerGO.GetComponent<ItemPickupInteraction>() == null)
            managerGO.AddComponent<ItemPickupInteraction>();
    }
}
