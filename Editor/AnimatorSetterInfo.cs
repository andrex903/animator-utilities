#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
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
            var infos = AnimatorUtility.GetAllStatesInfo(source);

            bindings.Clear();
            for (int i = 0; i < infos.Count; i++)
            {
                List<string> tags = new();

                if (infos[i].state.motion is BlendTree blendTree)
                {
                    foreach (var child in blendTree.children)
                    {
                        tags.Add(string.Empty);
                    }
                }
                else tags.Add(infos[i].state.name);

                bindings.Add(new AnimationBinding
                {
                    stateName = infos[i].state.name,
                    tags = tags
                });
            }

            bindings = bindings.OrderBy(x => x.stateName).ToList();
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