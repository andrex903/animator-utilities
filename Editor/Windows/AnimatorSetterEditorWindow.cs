#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RedeevEditor.Utilities
{
    public class AnimatorSetterEditorWindow : EditorWindow
    {
        private AnimatorSetterInfo sourceInfo;
        private RuntimeAnimatorController destination;
        private List<Object> animations = new();

        [MenuItem("Tools/Animator Setter")]
        public static void ShowWindow()
        {
            GetWindow<AnimatorSetterEditorWindow>("Animator Setter");
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("Helpbox");

            sourceInfo = (AnimatorSetterInfo)EditorGUILayout.ObjectField("Source", sourceInfo, typeof(AnimatorSetterInfo), false);
            destination = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Destination", destination, typeof(RuntimeAnimatorController), false);

            if (!sourceInfo || !sourceInfo.source || !destination)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            SyncGUI();

            AnimationsGUI();

            EditorGUILayout.EndVertical();
        }

        private void SyncGUI()
        {
            EditorGUILayout.BeginVertical("Helpbox");

            EditorGUILayout.BeginHorizontal("Box");
            EditorGUILayout.LabelField("Sync", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal("Box");
            if (GUILayout.Button(new GUIContent("Sync States"), GUILayout.Height(20f)))
            {
                SyncStates();
            }

            if (GUILayout.Button(new GUIContent("Sync Parameters"), GUILayout.Height(20f)))
            {
                SyncParameters();
            }

            if (GUILayout.Button(new GUIContent("Sync Transitions"), GUILayout.Height(20f)))
            {
                SyncTransitions();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void AnimationsGUI()
        {
            Rect rect = EditorGUILayout.BeginVertical("Helpbox");

            EditorGUILayout.BeginHorizontal("Box");
            EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            if (animations.Count == 0)
            {
                EditorGUILayout.LabelField("Drag animations here", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                for (int i = 0; i < animations.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    animations[i] = (Object)EditorGUILayout.ObjectField(animations[i], typeof(Object), false);
                    if (EditorUtilityGUI.IconButton("d_TreeEditor.Trash", 25f, 20f))
                    {
                        animations.RemoveAt(i);
                        GUIUtility.ExitGUI();
                        return;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Fill"), GUILayout.Height(20f)))
                {
                    Fill();
                }
                if (GUILayout.Button(new GUIContent("Clear"), GUILayout.Height(20f)))
                {
                    animations.Clear();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorUtilityGUI.DropAreaGUI(rect, obj =>
            {
                animations.Add(obj);
            });

            EditorGUILayout.EndVertical();
        }

        private void SyncTransitions()
        {
            var originalTransitionsInfo = AnimatorUtility.GetAllTransitions(sourceInfo.source);
            var destinationTransitionsInfo = AnimatorUtility.GetAllTransitions(destination);

            foreach (var transitionInfo in destinationTransitionsInfo)
            {
                var originalTransition = originalTransitionsInfo.Find(x => x.Hash == transitionInfo.Hash);
                if (originalTransition.transition)
                {
                    AnimatorUtility.CloneTransition(originalTransition, transitionInfo);
                }
            }

            EditorUtility.SetDirty(destination);
        }

        private void Fill()
        {
            var states = AnimatorUtility.GetAllStates(destination);

            foreach (var state in states)
            {
                if (sourceInfo.TryGetBinding(state.state.name, out var binding))
                {
                    if (state.state.motion is BlendTree tree)
                    {
                        ChildMotion[] motions = new ChildMotion[binding.tags.Count];
                        for (int i = 0; i < binding.tags.Count; i++)
                        {
                            motions[i].motion = GetMotion(binding.tags[i], binding.excluded);
                            motions[i].timeScale = 1f;
                        }
                        tree.children = motions;
                    }
                    else
                    {
                        state.state.motion = GetMotion(binding.tags[0], binding.excluded);
                    }
                }
            }
            EditorUtility.SetDirty(destination);
        }

        private Motion GetMotion(string tag, string excluded)
        {
            for (int i = 0; i < animations.Count; i++)
            {
                var clips = AnimatorUtility.GetAnimationClipsFromImporter(AssetDatabase.GetAssetPath(animations[i]));
                foreach (var clip in clips)
                {
                    if (AnimatorSetterInfo.Validate(clip.name, tag, excluded)) return clip;
                }
            }
            return null;
        }

        private void SyncStates()
        {
            var originalStates = AnimatorUtility.GetAllStates(sourceInfo.source);
            var destinationStates = AnimatorUtility.GetAllStates(destination);

            foreach (var state in originalStates)
            {
                if (destinationStates.Find(x => x.state.name == state.state.name).state == null)
                {

                }
            }
        }

        private void SyncParameters()
        {
            var original = sourceInfo.source as AnimatorController;
            var destination = this.destination as AnimatorController;

            foreach (var parameter in original.parameters)
            {
                if (!HasParameter(parameter.name, destination)) destination.AddParameter(parameter);
            }
        }

        private bool HasParameter(string name, AnimatorController controller)
        {
            foreach (var parameter in controller.parameters)
            {
                if (name == parameter.name) return true;
            }
            return false;
        }
    }
}
#endif