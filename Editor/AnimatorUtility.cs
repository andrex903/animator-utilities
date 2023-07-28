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
                stringBuilder.Append(source);
                stringBuilder.Append(sourceStateMachine);
                stringBuilder.Append(transition.destinationState);
                stringBuilder.Append(transition.destinationStateMachine);
                stringBuilder.Append(orderInSrcTransitions);

                using MD5 md5 = MD5.Create();
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
                stringBuilder.Clear();
                foreach (byte b in hashBytes) stringBuilder.Append(b.ToString("x2"));
                return stringBuilder.ToString();
            }
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
            destination.transition.conditions = conditions;

            if (source.StateTransition)
            {
                destination.StateTransition.canTransitionToSelf = source.StateTransition.canTransitionToSelf;
                destination.StateTransition.duration = source.StateTransition.duration;
                destination.StateTransition.exitTime = source.StateTransition.exitTime;
                destination.StateTransition.hasExitTime = source.StateTransition.hasExitTime;
                destination.StateTransition.hasFixedDuration = source.StateTransition.hasFixedDuration;
                destination.StateTransition.interruptionSource = source.StateTransition.interruptionSource;
                destination.StateTransition.offset = source.StateTransition.offset;
                destination.StateTransition.orderedInterruption = source.StateTransition.orderedInterruption;
            }     
        }

        public static List<ChildAnimatorState> GetAllStates(RuntimeAnimatorController controller)
        {
            List<ChildAnimatorState> states = new();
            foreach (var layer in (controller as AnimatorController).layers)
            {
                foreach (var state in layer.stateMachine.states)
                {
                    states.Add(state);
                }
                foreach (var subState in layer.stateMachine.stateMachines)
                {
                    foreach (var state in subState.stateMachine.states)
                    {
                        states.Add(state);
                    }
                }
            }
            return states;
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
    }
}
#endif