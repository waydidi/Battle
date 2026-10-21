using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace KingdomsEightBanners.EditorTools
{
    /// <summary>
    /// Import settings for the Blender-generated Storm model (art/blender/build_storm_warrior.py)
    /// and a builder for its Animator Controller:
    ///   Locomotion blend tree on Speed: Idle (0) -> Walk (2.2 m/s) -> Run (5 m/s and up)
    ///   Sitting / Sleeping bools, Attack / Dodge triggers.
    /// Parameter names match what ThirdPersonController drives.
    /// </summary>
    public sealed class StormModelPostprocessor : AssetPostprocessor
    {
        public const string ModelPath = "Assets/Art/Characters/Storm/Storm.fbx";

        private static readonly string[] LoopingClips = { "Idle", "Walk", "Run", "Sit", "Sleep" };

        private void OnPreprocessModel()
        {
            if (assetPath != ModelPath) return;
            var importer = (ModelImporter)assetImporter;
            // Generic keeps the clips bound to this exact skeleton; switch to Humanoid later to share
            // animations with other characters.
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }

        private void OnPreprocessAnimation()
        {
            if (assetPath != ModelPath) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                // Blender names clips "Armature|Action"; keep only the action name.
                clip.name = ShortName(clip.name);
                clip.loopTime = LoopingClips.Contains(clip.name);
            }

            importer.clipAnimations = clips;
        }

        public static string ShortName(string clipName)
        {
            int bar = clipName.LastIndexOf('|');
            return bar >= 0 ? clipName.Substring(bar + 1) : clipName;
        }
    }

    public static class StormCharacterSetup
    {
        public const string ControllerPath = "Assets/Art/Characters/Storm/Storm.controller";

        [MenuItem("Kingdoms/Create Storm Animator Controller")]
        public static void CreateControllerMenu()
        {
            var controller = CreateOrUpdateController();
            if (controller != null) Selection.activeObject = controller;
        }

        /// <summary>Builds (or rebuilds) Storm.controller from the clips inside Storm.fbx. Null if the model is missing.</summary>
        public static AnimatorController CreateOrUpdateController()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(StormModelPostprocessor.ModelPath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .GroupBy(c => StormModelPostprocessor.ShortName(c.name))
                .ToDictionary(g => g.Key, g => g.First());
            if (clips.Count == 0)
            {
                Debug.LogWarning("No animation clips found in " + StormModelPostprocessor.ModelPath +
                                 ". Export the model with art/blender/build_storm_warrior.py first.");
                return null;
            }

            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Sitting", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Sleeping", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dodge", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

            var machine = controller.layers[0].stateMachine;

            var locomotion = controller.CreateBlendTreeInController("Locomotion", out var tree);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            AddMotion(tree, clips, "Idle", 0f);
            AddMotion(tree, clips, "Walk", 2.2f);
            AddMotion(tree, clips, "Run", 5f);
            machine.defaultState = locomotion;

            var sit = AddState(machine, clips, "Sit");
            var sleep = AddState(machine, clips, "Sleep");
            var attack = AddState(machine, clips, "Attack");
            var dodge = AddState(machine, clips, "Dodge");

            // Rest states: enter on bool true, leave on bool false.
            Toggle(locomotion, sit, "Sitting");
            Toggle(locomotion, sleep, "Sleeping");

            // One-shot actions from anywhere, then back to locomotion when the clip ends.
            OneShot(machine, attack, locomotion, "Attack");
            OneShot(machine, dodge, locomotion, "Dodge");

            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void AddMotion(BlendTree tree, System.Collections.Generic.Dictionary<string, AnimationClip> clips,
            string name, float threshold)
        {
            if (clips.TryGetValue(name, out var clip)) tree.AddChild(clip, threshold);
        }

        private static AnimatorState AddState(AnimatorStateMachine machine,
            System.Collections.Generic.Dictionary<string, AnimationClip> clips, string name)
        {
            var state = machine.AddState(name);
            if (clips.TryGetValue(name, out var clip)) state.motion = clip;
            return state;
        }

        private static void Toggle(AnimatorState from, AnimatorState to, string parameter)
        {
            var enter = from.AddTransition(to);
            enter.hasExitTime = false;
            enter.duration = 0.3f;
            enter.AddCondition(AnimatorConditionMode.If, 0f, parameter);

            var leave = to.AddTransition(from);
            leave.hasExitTime = false;
            leave.duration = 0.3f;
            leave.AddCondition(AnimatorConditionMode.IfNot, 0f, parameter);
        }

        private static void OneShot(AnimatorStateMachine machine, AnimatorState state, AnimatorState back, string trigger)
        {
            var enter = machine.AddAnyStateTransition(state);
            enter.hasExitTime = false;
            enter.duration = 0.05f;
            enter.canTransitionToSelf = false;
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);

            var exit = state.AddTransition(back);
            exit.hasExitTime = true;
            exit.exitTime = 0.9f;
            exit.duration = 0.1f;
        }
    }
}
