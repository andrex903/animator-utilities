#if UNITY_EDITOR
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    public static class AnimatorUtility
    {
        public enum TransitionSourceType
        {
            NormalState = 0,
            AnyState = 1,
            EntryState = 2,
            StateMachine = 3
        }

        public struct AnimatorTransitionInfo
        {
            public TransitionSourceType type;
            public AnimatorState source;
            public AnimatorStateMachine sourceStateMachine;
            public AnimatorTransitionBase transition;
            public int orderInSrcTransitions;
            public AnimatorStateTransition StateTransition => (type != TransitionSourceType.EntryState) ? transition as AnimatorStateTransition : null;
            private string cachedHash;
            public string Hash => cachedHash ??= GenerateUniqueHash();

            private string GenerateUniqueHash()
            {
                StringBuilder stringBuilder = new();
                stringBuilder.Append(type);
                if (source) stringBuilder.Append(source.name);
                if (sourceStateMachine) stringBuilder.Append(sourceStateMachine.name);
                if (transition.destinationState) stringBuilder.Append(transition.destinationState.name);
                if (transition.destinationStateMachine) stringBuilder.Append(transition.destinationStateMachine.name);
                stringBuilder.Append(orderInSrcTransitions);

                using MD5 md5 = MD5.Create();
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
                stringBuilder.Clear();
                foreach (byte b in hashBytes) stringBuilder.Append(b.ToString("x2"));
                return stringBuilder.ToString();
            }
        }

        public struct AnimatorStateInfo
        {
            public AnimatorState state;
            public Vector3 position;
            public int layer;           
        }

        public static List<AnimatorTransitionInfo> GetAllTransitions(RuntimeAnimatorController controller)
        {
            List<AnimatorTransitionInfo> transitions = new();
            foreach (var layer in (controller as AnimatorController).layers)
            {
                transitions.AddRange(GetAllTransitions(layer.stateMachine));
            }
            return transitions;
        }

        public static List<AnimatorTransitionInfo> GetAllTransitions(AnimatorStateMachine stateMachine)
        {
            List<AnimatorTransitionInfo> transitions = new();

            for (int i = 0; i < stateMachine.anyStateTransitions.Length; i++)
            {
                AnimatorTransitionInfo info = new()
                {
                    type = TransitionSourceType.AnyState,
                    sourceStateMachine = stateMachine,
                    transition = stateMachine.anyStateTransitions[i],
                    orderInSrcTransitions = i
                };
                transitions.Add(info);
            }

            for (int i = 0; i < stateMachine.entryTransitions.Length; i++)
            {
                AnimatorTransitionInfo info = new()
                {
                    type = TransitionSourceType.EntryState,
                    transition = stateMachine.entryTransitions[i],
                    sourceStateMachine = stateMachine,
                    orderInSrcTransitions = i
                };
                transitions.Add(info);
            }

            foreach (var state in stateMachine.states)
            {
                for (int i = 0; i < state.state.transitions.Length; i++)
                {
                    AnimatorTransitionInfo info = new()
                    {
                        type = TransitionSourceType.NormalState,
                        source = state.state,
                        transition = state.state.transitions[i],
                        orderInSrcTransitions = i
                    };
                    transitions.Add(info);
                }
            }

            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                var stateMachineTransitions = stateMachine.GetStateMachineTransitions(subStateMachine.stateMachine);
                for (int i = 0; i < stateMachineTransitions.Length; i++)
                {
                    AnimatorTransitionInfo info = new()
                    {
                        type = TransitionSourceType.StateMachine,
                        sourceStateMachine = subStateMachine.stateMachine,
                        transition = stateMachineTransitions[i],
                        orderInSrcTransitions = i
                    };
                    transitions.Add(info);
                }

                transitions.AddRange(GetAllTransitions(subStateMachine.stateMachine));
            }
            return transitions;
        }

        public static void CloneTransition(AnimatorTransitionInfo source, AnimatorTransitionInfo destination)
        {
            if (source.type != destination.type) return;

            CloneTransition(source, destination.transition);
        }

        public static void CloneTransition(AnimatorTransitionInfo source, AnimatorTransitionBase destination)
        {
            var conditions = new AnimatorCondition[source.transition.conditions.Length];
            for (int i = 0; i < conditions.Length; i++)
            {
                conditions[i] = new()
                {
                    mode = source.transition.conditions[i].mode,
                    parameter = source.transition.conditions[i].parameter,
                    threshold = source.transition.conditions[i].threshold
                };
            }
            destination.conditions = conditions;

            if (source.StateTransition && destination is AnimatorStateTransition stateTransition)
            {
                stateTransition.canTransitionToSelf = source.StateTransition.canTransitionToSelf;
                stateTransition.duration = source.StateTransition.duration;
                stateTransition.exitTime = source.StateTransition.exitTime;
                stateTransition.hasExitTime = source.StateTransition.hasExitTime;
                stateTransition.hasFixedDuration = source.StateTransition.hasFixedDuration;
                stateTransition.interruptionSource = source.StateTransition.interruptionSource;
                stateTransition.offset = source.StateTransition.offset;
                stateTransition.orderedInterruption = source.StateTransition.orderedInterruption;
            }
        }

        public static List<AnimatorStateInfo> GetAllStatesInfo(RuntimeAnimatorController controller)
        {
            var animatorController = controller as AnimatorController;

            List<AnimatorStateInfo> states = new();
            for (int i = 0; i < animatorController.layers.Length; i++)
            {
                foreach (var state in animatorController.layers[i].stateMachine.states)
                {
                    states.Add(new() { state = state.state, position = state.position, layer = i });
                }
                foreach (var subState in animatorController.layers[i].stateMachine.stateMachines)
                {
                    foreach (var state in subState.stateMachine.states)
                    {
                        states.Add(new() { state = state.state, position = state.position, layer = i });
                    }
                }
            }
            return states;
        }

        public static AnimatorStateMachine GetStateMachine(RuntimeAnimatorController controller, string name)
        {
            var animatorController = controller as AnimatorController;

            foreach (var layer in animatorController.layers)
            {
                if (layer.stateMachine.name == name) return layer.stateMachine;
                foreach (var subStateMachine in layer.stateMachine.stateMachines)
                {
                    if (subStateMachine.stateMachine.name == name) return subStateMachine.stateMachine;
                }
            }

            return null;
        }

        public static AnimationClip[] GetAnimationClipsFromImporter(string modelImporterPath)
        {
            var animationClips = new List<AnimationClip>();
            var modelImporterAssets = AssetDatabase.LoadAllAssetsAtPath(modelImporterPath);
            foreach (var asset in modelImporterAssets)
            {
                if (asset is AnimationClip clip)
                {
                    animationClips.Add(clip);
                }
            }
            return animationClips.ToArray();
        }

        [MenuItem("Assets/Redeev/Fast Animator Setup", validate = true)]
        private static bool FastAnimatorSetupValidation()
        {
            if (Selection.objects.Length == 1)
            {
                return Selection.objects[0].GetType() == typeof(AnimatorController);
            }
            return false;
        }

        [MenuItem("Assets/Redeev/Fast Animator Setup")]
        private static void FastAnimatorSetup()
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
    }
}
#endif