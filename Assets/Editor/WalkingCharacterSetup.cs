#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Menue:  Tools > Casino Sim > Setup Walking Character
//
// - stellt alle FBX unter Assets/Character auf Rig = Humanoid
//   (X Bot -> eigener Avatar, alle "@"-Clips -> CopyFromOther X Bot)
// - setzt Loop fuer Walk / Idle / Crouch-Clips
// - baut/aktualisiert Assets/Character/CasinoCharacter.controller
//   mit Parametern  Speed (float)  und  Crouch (bool)
//
// Danach nur noch: X Bot in die Szene ziehen, Controller zuweisen,
// CharacterAnimatorDriver + HeadLook befuellen. (Details im Chat.)
public static class WalkingCharacterSetup
{
    const string Dir = "Assets/Character";
    const string ControllerPath = Dir + "/CasinoCharacter.controller";
    const string CharacterFbx = Dir + "/X Bot.fbx";

    [MenuItem("Tools/Casino Sim/Setup Walking Character")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(Dir))
        {
            EditorUtility.DisplayDialog("Setup", "Ordner Assets/Character fehlt.", "OK");
            return;
        }

        // 1) X Bot als Humanoid
        var charImp = AssetImporter.GetAtPath(CharacterFbx) as ModelImporter;
        if (charImp == null)
        {
            EditorUtility.DisplayDialog("Setup", "X Bot.fbx nicht gefunden unter Assets/Character.", "OK");
            return;
        }
        charImp.animationType = ModelImporterAnimationType.Human;
        charImp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        charImp.SaveAndReimport();

        var charAvatar = AssetDatabase.LoadAllAssetsAtPath(CharacterFbx)
            .OfType<Avatar>().FirstOrDefault();
        if (charAvatar == null)
        {
            EditorUtility.DisplayDialog("Setup", "Konnte keinen Avatar aus X Bot.fbx erzeugen. " +
                "Bitte im Import-Tab unter Rig > Configure die Bones pruefen.", "OK");
            return;
        }

        // 2) alle Animations-FBX ("@") auf denselben Avatar
        var animFbx = Directory.GetFiles(Dir, "*.fbx")
            .Select(p => p.Replace('\\', '/'))
            .Where(p => Path.GetFileName(p).Contains("@"))
            .ToArray();

        foreach (var path in animFbx)
        {
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) continue;
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            imp.sourceAvatar = charAvatar;

            imp.useFileScale = true;
            imp.globalScale = 1f;

            var clips = imp.defaultClipAnimations;
            string clean = Path.GetFileNameWithoutExtension(path);
            int at = clean.IndexOf('@');
            if (at >= 0) clean = clean.Substring(at + 1);
            bool isJump = clean.ToLower().Contains("jump");
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips.Length == 1) clips[i].name = clean;
                clips[i].loopTime = !isJump;               // Jump = einmalig, Rest loopt
                clips[i].lockRootRotation = true;
                clips[i].lockRootPositionXZ = true;
                // Jump: Y NICHT into pose backen -> die Sprunghoehe kommt NUR vom Rigidbody,
                // sonst hebt der Charakter optisch hoeher ab als die Kamera.
                clips[i].lockRootHeightY = !isJump;
            }
            imp.clipAnimations = clips;
            imp.SaveAndReimport();
        }

        // 3) Clips einsammeln
        AnimationClip walk = FindClipNot("walk", "crouch", "sprint") ?? FindClip("walk");
        AnimationClip idle = FindClipNot("idle", "crouch") ?? FindClip("breathing");
        AnimationClip crouchIdle = FindClip("crouch idle") ?? FindClip("crouching idle") ?? FindClip("crouch_idle");
        AnimationClip crouchWalk = FindClipMulti("crouch", "walk");
        AnimationClip sprint = FindClipNot("sprint", "crouch") ?? FindClipNot("run", "crouch")
                             ?? FindClipNot("jog", "crouch");
        AnimationClip jump = FindClip("jump");

        // Transition-Clip "Crouched To Standing": Anfang = Hocke, Ende = Stand.
        // Damit koennen wir OHNE echte Idle-Clips beide Standposen einfrieren.
        AnimationClip transClip = FindClip("crouched to standing") ?? FindClip("crouch to stand")
                                ?? FindClip("standing to crouch");

        bool crouchIdleFrozen = false;
        // Crouch-Idle: "Crouched To Standing" auf Frame 0 einfrieren = Hock-Pose.
        if (crouchIdle == null && transClip != null) { crouchIdle = transClip; crouchIdleFrozen = true; }

        if (walk == null)
        {
            EditorUtility.DisplayDialog("Setup",
                "Keine Walking-Animation gefunden. Lege 'X Bot@Walking.fbx' (Mixamo, In Place, Without Skin) in Assets/Character.",
                "OK");
            return;
        }

        // 4) Controller bauen
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        // Parameter (idempotent)
        EnsureParam(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParam(controller, "Crouch", AnimatorControllerParameterType.Bool);
        EnsureParam(controller, "Sprint", AnimatorControllerParameterType.Bool);
        EnsureParam(controller, "Grounded", AnimatorControllerParameterType.Bool);
        EnsureParam(controller, "MoveMul", AnimatorControllerParameterType.Float);   // Anim-Tempo an echte Speed anpassen
        // Defaults: Grounded = true (sonst 1 Frame Jump-State), MoveMul = 1 (sonst Anim eingefroren)
        {
            var arr = controller.parameters;
            foreach (var p in arr)
            {
                if (p.name == "Grounded") p.defaultBool = true;
                if (p.name == "MoveMul") p.defaultFloat = 1f;
            }
            controller.parameters = arr;
        }

        var sm = controller.layers[0].stateMachine;
        foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);
        foreach (var t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);
        foreach (var bt in AssetDatabase.LoadAllAssetsAtPath(ControllerPath).OfType<BlendTree>().ToArray())
            Object.DestroyImmediate(bt, true);

        // Idle: echter Clip -> normal abspielen. Sonst Walk auf der Durchschwing-Pose
        // (cycleOffset 0.25 = Fuesse zusammen, aufrecht) einfrieren.
        var idleSt = sm.AddState("Idle");
        idleSt.motion = idle ?? walk;
        if (idle == null) { idleSt.speed = 0f; idleSt.cycleOffset = 0.25f; }

        var walkSt = sm.AddState("Walk");
        walkSt.motion = walk;

        // CrouchIdle: "Crouched To Standing" am ANFANG einfrieren (= Hock-Pose).
        var cIdleSt = sm.AddState("CrouchIdle");
        cIdleSt.motion = crouchIdle ?? idle ?? walk;
        if (crouchIdleFrozen) { cIdleSt.speed = 0f; cIdleSt.cycleOffset = 0f; }
        else if (crouchIdle == null && idle == null) cIdleSt.speed = 0f;

        var cWalkSt = sm.AddState("CrouchWalk");
        cWalkSt.motion = crouchWalk ?? walk;

        AnimatorState runSt = null;
        if (sprint != null)
        {
            runSt = sm.AddState("Run");
            runSt.motion = sprint;
        }

        AnimatorState jumpSt = null;
        if (jump != null)
        {
            jumpSt = sm.AddState("Jump");
            jumpSt.motion = jump;
        }

        var all = new[] { idleSt, walkSt, cIdleSt, cWalkSt, runSt, jumpSt }.Where(s => s != null).ToArray();
        foreach (var st in all) st.writeDefaultValues = true;

        // Lauf-States: Abspieltempo an die echte Geschwindigkeit koppeln -> weniger Fuss-Schlittern
        foreach (var st in new[] { walkSt, cWalkSt, runSt }.Where(s => s != null))
        {
            st.speedParameterActive = true;
            st.speedParameter = "MoveMul";
        }

        sm.defaultState = idleSt;

        const float T = 0.3f;
        AddT(idleSt,  walkSt,  AnimatorConditionMode.Greater, T, "Speed");
        AddT(walkSt,  idleSt,  AnimatorConditionMode.Less,    T, "Speed");
        AddT(cIdleSt, cWalkSt, AnimatorConditionMode.Greater, T, "Speed");
        AddT(cWalkSt, cIdleSt, AnimatorConditionMode.Less,    T, "Speed");
        AddT(idleSt,  cIdleSt, AnimatorConditionMode.If,     0f, "Crouch");
        AddT(walkSt,  cWalkSt, AnimatorConditionMode.If,     0f, "Crouch");
        AddT(cIdleSt, idleSt,  AnimatorConditionMode.IfNot,  0f, "Crouch");
        AddT(cWalkSt, walkSt,  AnimatorConditionMode.IfNot,  0f, "Crouch");

        if (runSt != null)
        {
            AddT(walkSt, runSt,  AnimatorConditionMode.If,    0f, "Sprint");
            AddT(runSt,  walkSt, AnimatorConditionMode.IfNot, 0f, "Sprint");
            AddT(runSt,  idleSt, AnimatorConditionMode.Less,  T,  "Speed");
        }

        if (jumpSt != null)
        {
            var toJump = sm.AddAnyStateTransition(jumpSt);
            toJump.hasExitTime = false;
            toJump.duration = 0.1f;
            toJump.canTransitionToSelf = false;
            toJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");

            AddT(jumpSt, idleSt, AnimatorConditionMode.If, 0f, "Grounded");
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WalkingCharacterSetup] fertig. walk={walk?.name} idle={idle?.name} " +
                  $"crouchIdle={crouchIdle?.name} crouchWalk={crouchWalk?.name} " +
                  $"sprint={sprint?.name} jump={jump?.name}\nController: {ControllerPath}");
        EditorUtility.DisplayDialog("Setup",
            $"Fertig.\n\nStates: Idle, Walk, CrouchIdle, CrouchWalk" +
            (sprint != null ? ", Run" : "") + (jump != null ? ", Jump" : "") + "\n\n" +
            "walk=" + (walk ? walk.name : "-") + "  idle=" + (idle ? idle.name : "(Walk eingefroren)") + "\n" +
            "sprint=" + (sprint ? sprint.name : "-") + "  jump=" + (jump ? jump.name : "-") + "\n\n" +
            "Falls X Bot schon in der Szene ist: nur Play druecken. Sonst 'Place Character In Scene'.", "OK");
    }

    static void AddT(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold, string param)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0.12f;
        t.AddCondition(mode, threshold, param);
    }

    static void EnsureParam(AnimatorController c, string n, AnimatorControllerParameterType t)
    {
        if (c.parameters.Any(p => p.name == n)) return;
        c.AddParameter(n, t);
    }

    static AnimationClip FindClip(string keyword)
    {
        keyword = keyword.ToLower();
        return AssetDatabase.FindAssets("t:AnimationClip", new[] { Dir })
            .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(cl => cl != null && !cl.name.StartsWith("__preview"))
            .FirstOrDefault(cl => cl.name.ToLower().Contains(keyword));
    }

    static AnimationClip FindClipNot(string keyword, params string[] exclude)
    {
        keyword = keyword.ToLower();
        return AssetDatabase.FindAssets("t:AnimationClip", new[] { Dir })
            .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(cl => cl != null && !cl.name.StartsWith("__preview"))
            .Where(cl => cl.name.ToLower().Contains(keyword))
            .FirstOrDefault(cl => !exclude.Any(e => cl.name.ToLower().Contains(e.ToLower())));
    }

    static AnimationClip FindClipMulti(params string[] keywords)
    {
        return AssetDatabase.FindAssets("t:AnimationClip", new[] { Dir })
            .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(cl => cl != null && !cl.name.StartsWith("__preview"))
            .FirstOrDefault(cl => keywords.All(k => cl.name.ToLower().Contains(k.ToLower())));
    }
}
#endif
