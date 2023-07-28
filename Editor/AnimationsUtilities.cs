#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    public static class AnimationsUtilities
    {
        [MenuItem("Assets/Redeev/Setup Fast Animator Controller", validate = true)]
        private static bool SetupAnimatorControllerValidation()
        {
            if (Selection.objects.Length == 1)
            {
                return Selection.objects[0].GetType() == typeof(AnimatorController);
            }
            return false;
        }

        [MenuItem("Assets/Redeev/Setup Fast Animator Controller")]
        private static void SetupAnimatorController()
        {
            if (!EditorUtility.DisplayDialog("Warning", "This will delete all the current parameters and transitions", "Continue", "Cancel")) return;

            var controller = Selection.objects[0] as AnimatorController;

            controller.parameters = new AnimatorControllerParameter[0];
            controller.layers[0].stateMachine.anyStateTransitions = new AnimatorStateTransition[0];

            var childStates = controller.layers[0].stateMachine.states;
            controller.layers[0].stateMachine.entryPosition = new Vector3(400f, -(childStates.Length / 2 + 1) * 50f, 0f);
            for (int i = 0; i < childStates.Length; i++)
            {
                var state = childStates[i];
                state.position = new(400f, 50f * (i - childStates.Length / 2), 0f);
                childStates[i] = state;
            }
            controller.layers[0].stateMachine.states = childStates;

            AnimatorState idleState = null;

            foreach (var state in controller.layers[0].stateMachine.states)
            {
                if (state.state.name.Contains("Idle")) idleState = state.state;               
                controller.AddParameter(state.state.name, AnimatorControllerParameterType.Trigger);
                var transition = controller.layers[0].stateMachine.AddAnyStateTransition(state.state);
                transition.AddCondition(AnimatorConditionMode.If, 0, state.state.name);
                transition.canTransitionToSelf = false;
                transition.duration = 0f;
            }

            if (idleState != null)
            {
                foreach (var state in controller.layers[0].stateMachine.states)
                {
                    if (state.state == idleState || state.state.motion.isLooping) continue;
                    var transition = state.state.AddTransition(idleState);
                    transition.hasExitTime = true;
                    transition.exitTime = 1f;
                    transition.duration = 0f;
                }
            }
        }

        [MenuItem("Assets/Redeev/Setup Animation Clips", validate = true)]
        private static bool SetupAnimationValidation()
        {
            foreach (var item in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(item);
                if (Path.GetExtension(path) != ".fbx") return false;
            }
            return Selection.objects.Length > 0;
        }

        [MenuItem("Assets/Redeev/Setup Animation Clips")]
        private static void SetupAnimation()
        {
            foreach (var item in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(item);
                if (Path.GetExtension(path) == ".fbx")
                {
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    var importer = (ModelImporter)AssetImporter.GetAtPath(path);

                    SetupModelImporterNames(importer, fileName);
                    SetupModelImporter(importer, fileName);
                }
            }
        }

        private static void SetupModelImporterNames(ModelImporter modelImporter, string name)
        {
            ModelImporterClipAnimation[] clipAnimations = modelImporter.defaultClipAnimations;
            for (int i = 0; i < clipAnimations.Length; i++)
            {
                clipAnimations[i].name = name;
            }
            modelImporter.clipAnimations = clipAnimations;
            modelImporter.SaveAndReimport();
        }

        private static void SetupModelImporter(ModelImporter modelImporter, string name)
        {
            ModelImporterClipAnimation[] clipAnimations = modelImporter.clipAnimations;

            for (int i = 0; i < clipAnimations.Length; i++)
            {
                if (name.Contains("Walk") || name.Contains("Idle") || name.Contains("Run")) clipAnimations[i].loopTime = true;                
                clipAnimations[i].keepOriginalPositionY = true;
                clipAnimations[i].keepOriginalPositionXZ = true;
                clipAnimations[i].keepOriginalOrientation = true;
                clipAnimations[i].lockRootHeightY = true;
                clipAnimations[i].lockRootPositionXZ = true;
                clipAnimations[i].lockRootRotation = true;
            }
            modelImporter.clipAnimations = clipAnimations;
            modelImporter.SaveAndReimport();
        }
    }
}
#endif