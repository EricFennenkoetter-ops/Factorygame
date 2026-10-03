#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Menue:  Tools > Casino Sim > Place Character In Scene
//
// Instanziiert X Bot unter dem Player, weist Animator + CasinoCharacter.controller zu,
// haengt CharacterAnimatorDriver + HeadLook an und verdrahtet alle Referenzen
// (Rigidbody, Orientation, Kamera, Bones) automatisch.
public static class PlaceCharacterInScene
{
    const string Dir = "Assets/Character";
    const string CharacterFbx = Dir + "/X Bot.fbx";
    const string ControllerPath = Dir + "/CasinoCharacter.controller";

    [MenuItem("Tools/Casino Sim/Place Character In Scene")]
    public static void Run()
    {
        var move = Object.FindFirstObjectByType<PlayerMovementScript>();
        if (move == null)
        {
            EditorUtility.DisplayDialog("Place Character",
                "Kein PlayerMovementScript in der Szene gefunden. Bitte die Spieler-Szene oeffnen.", "OK");
            return;
        }

        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterFbx);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var avatar = AssetDatabase.LoadAllAssetsAtPath(CharacterFbx).OfType<Avatar>().FirstOrDefault();
        if (fbx == null || controller == null)
        {
            EditorUtility.DisplayDialog("Place Character",
                "X Bot.fbx oder CasinoCharacter.controller fehlt. Erst 'Setup Walking Character' ausfuehren.", "OK");
            return;
        }

        // vorhandene Instanz entfernen
        var existing = move.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "X Bot" || t.name == "X Bot(Clone)");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        inst.name = "X Bot";
        Undo.RegisterCreatedObjectUndo(inst, "Place Character");
        inst.transform.SetParent(move.transform, false);
        // Character erbt die Player-Skalierung (Player-Kapsel ist hier 2x) -> passt zur Spielergroesse
        inst.transform.localScale = Vector3.one;
        inst.transform.localPosition = new Vector3(0f, GuessFeetOffset(move), 0f);
        inst.transform.localRotation = Quaternion.identity;

        var anim = inst.GetComponent<Animator>();
        if (anim == null) anim = inst.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        if (avatar != null) anim.avatar = avatar;
        anim.applyRootMotion = false;
        // sonst wird der Char faelschlich weg-gecullt und friert ein (SkinnedMesh-Bounds-Bug)
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.updateWhenOffscreen = true;

        // Orientation-Transform aus dem PlayerMovementScript ziehen
        var so = new SerializedObject(move);
        var orientation = so.FindProperty("orientation")?.objectReferenceValue as Transform;

        var driver = inst.GetComponent<CharacterAnimatorDriver>() ?? inst.AddComponent<CharacterAnimatorDriver>();
        driver.body = move.GetComponent<Rigidbody>();
        driver.orientation = orientation;

        var look = inst.GetComponent<HeadLook>() ?? inst.AddComponent<HeadLook>();
        var camRot = Object.FindFirstObjectByType<CameraRotation>();
        var cam = camRot != null ? camRot.GetComponent<Camera>() : Camera.main;
        look.aimSource = camRot != null ? camRot.transform : (cam != null ? cam.transform : null);
        look.hideHead = false;

        // Damit man beim Runterschauen den eigenen Koerper sieht: Near-Clip kleiner
        if (cam != null && cam.nearClipPlane > 0.12f)
        {
            Undo.RecordObject(cam, "Near Clip");
            cam.nearClipPlane = 0.08f;
        }
        look.head = FindBone(inst.transform, "Head");
        look.spineChain = new[]
        {
            FindBone(inst.transform, "Spine1"),
            FindBone(inst.transform, "Spine2"),
            FindBone(inst.transform, "Neck"),
        }.Where(b => b != null).ToArray();

        Selection.activeGameObject = inst;
        EditorUtility.SetDirty(inst);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(inst.scene);

        Debug.Log($"[PlaceCharacterInScene] parent={move.name} rb={(driver.body ? "ok" : "FEHLT")} " +
                  $"orientation={(orientation ? orientation.name : "FEHLT")} " +
                  $"aim={(look.aimSource ? look.aimSource.name : "FEHLT")} " +
                  $"head={(look.head ? look.head.name : "FEHLT")} spine={look.spineChain.Length}");

        EditorUtility.DisplayDialog("Place Character",
            "X Bot ist in der Szene unter '" + move.name + "'.\n\n" +
            "Pruefen:\n- Y-Position des X Bot (Fuesse auf Bodenhoehe der Kapsel)\n" +
            "- ggf. Kamera fuer den Test in 3rd Person schieben\n\n" +
            "Dann Play druecken.", "OK");
    }

    static float GuessFeetOffset(PlayerMovementScript move)
    {
        var cap = move.GetComponent<CapsuleCollider>();
        if (cap != null) return -(cap.height * 0.5f - cap.center.y);
        return -1f;
    }

    static Transform FindBone(Transform root, string contains)
    {
        return root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name.EndsWith(contains) || t.name.EndsWith(contains, System.StringComparison.OrdinalIgnoreCase));
    }
}
#endif
