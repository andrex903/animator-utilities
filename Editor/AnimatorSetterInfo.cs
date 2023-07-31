#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    [CreateAssetMenu(menuName = "Redeev/Animator Setter/Info")]
    public class AnimatorSetterInfo : ScriptableObject
    {
        public RuntimeAnimatorController source;
        public List<AnimationBinding> bindings = new();

        [Serializable]
        public class AnimationBinding
        {
            public string stateName = string.Empty;
            public List<string> tags = new();
            public string excluded = string.Empty;
        }

        public void LoadAnimations()
        {
            var states = AnimatorUtility.GetAllStates(source);

            bindings.Clear();
            for (int i = 0; i < states.Count; i++)
            {
                List<string> tags = new();

                if (states[i].state.motion is BlendTree blendTree)
                {
                    foreach (var child in blendTree.children)
                    {
                        tags.Add(string.Empty);
                    }
                }
                else tags.Add(states[i].state.name);

                bindings.Add(new AnimationBinding
                {
                    stateName = states[i].state.name,
                    tags = tags
                });
            }
        }

        public static bool Validate(string clipName, string tag, string excluded)
        {
            if (string.IsNullOrEmpty(clipName)) return false;

            clipName = clipName.ToLower();
            if (!clipName.Contains(tag.ToLower())) return false;
            return string.IsNullOrEmpty(excluded) || !clipName.Contains(excluded.ToLower());
        }

        public bool TryGetBinding(string name, out AnimationBinding binding)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].stateName == name)
                {
                    binding = bindings[i];
                    return true;
                }
            }
            binding = default;
            return false;
        }
    }
}
#endif