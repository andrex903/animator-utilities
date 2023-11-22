#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RedeevEditor.Utilities
{
    public class AnimatorSetterEditorWindow : EditorWindow
    {
        private AnimatorSetterInfo sourceInfo;
        private List<RuntimeAnimatorController> destinations = new();
        private List<Object> animations = new();
        private Vector2 scrollPosition;

        [MenuItem("Tools/Animator Setter")]
        public static void ShowWindow()
        {
            GetWindow<AnimatorSetterEditorWindow>("Animator Setter");
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("Helpbox");

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            sourceInfo = (AnimatorSetterInfo)EditorGUILayout.ObjectField("Source", sourceInfo, typeof(AnimatorSetterInfo), false);
            DestinationsGUI();

            if (!sourceInfo || !sourceInfo.source || destinations.Count == 0)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            SyncGUI();

            AnimationsGUI();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DestinationsGUI()
        {
            Rect rect = EditorGUILayout.BeginVertical("Helpbox");

            EditorGUILayout.BeginHorizontal("Box");
            EditorGUILayout.LabelField("Destinations", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            if (destinations.Count == 0)
            {
                EditorGUILayout.LabelField("Drag controllers here", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                for (int i = 0; i < destinations.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    destinations[i] = (RuntimeAnimatorController)EditorGUILayout.ObjectField(destinations[i], typeof(RuntimeAnimatorController), false);
                    if (EditorUtilityGUI.IconButton("d_TreeEditor.Trash", 25f, 20f))
                    {
                        destinations.RemoveAt(i);
                        GUIUtility.ExitGUI();
                        return;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Clear"), GUILayout.Height(20f)))
                {
                    destinations.Clear();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorUtilityGUI.DropAreaGUI(rect, obj =>
            {
                if (obj is RuntimeAnimatorController controller) destinations.Add(controller);
            });

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
                foreach (var destination in destinations) SyncStates(destination);
            }

            if (GUILayout.Button(new GUIContent("Sync Parameters"), GUILayout.Height(20f)))
            {
                foreach (var destination in destinations) SyncParameters(destination);
            }

            if (GUILayout.Button(new GUIContent("Sync Transitions"), GUILayout.Height(20f)))
            {
                foreach (var destination in destinations) SyncTransitions(destination);
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
                if (GUILayout.Button(new GUIContent("Overwrite"), GUILayout.Height(20f)))
                {
                    foreach (var destination in destinations) Overwrite(destination);
                }
                if (GUILayout.Button(new GUIContent("Fill"), GUILayout.Height(20f)))
                {
                    foreach (var destination in destinations) Fill(destination);
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

        private void SyncTransitions(RuntimeAnimatorController destination)
        {
            var originalTransitionsInfo = AnimatorUtility.GetAllTransitions(sourceInfo.source);
            var destinationTransitionsInfo = AnimatorUtility.GetAllTransitions(destination);
            var destinationStatesInfo = AnimatorUtility.GetAllStatesInfo(destination);

            foreach (var transitionInfo in originalTransitionsInfo)
            {
                var destinationTransition = destinationTransitionsInfo.Find(x => x.Hash == transitionInfo.Hash);
                if (destinationTransition.transition)
                {
                    AnimatorUtility.CloneTransition(transitionInfo, destinationTransition);
                }
                else
                {
                    if (transitionInfo.type == AnimatorUtility.TransitionSourceType.NormalState)
                    {
                        var stateInfo = destinationStatesInfo.Find(x => x.state.name == transitionInfo.source.name);
                        if (stateInfo.state != null)
                        {
                            if (transitionInfo.StateTransition.destinationState)
                            {
                                var state = destinationStatesInfo.Find(x => x.state.name == transitionInfo.StateTransition.destinationState.name);
                                var transition = stateInfo.state.AddTransition(state.state);
                                AnimatorUtility.CloneTransition(transitionInfo, transition);
                            }
                        }
                    }
                    else if (transitionInfo.type == AnimatorUtility.TransitionSourceType.AnyState)
                    {
                        var machine = AnimatorUtility.GetStateMachine(destination, transitionInfo.sourceStateMachine.name);

                        if (transitionInfo.StateTransition.destinationState)
                        {
                            var state = destinationStatesInfo.Find(x => x.state.name == transitionInfo.StateTransition.destinationState.name);

                            var transition = machine.AddAnyStateTransition(state.state);

                            AnimatorUtility.CloneTransition(transitionInfo, transition);
                        }

                    }
                    else if (transitionInfo.type == AnimatorUtility.TransitionSourceType.StateMachine)
                    {

                    }
                    else if (transitionInfo.type == AnimatorUtility.TransitionSourceType.EntryState)
                    {

                    }
                }
            }

            EditorUtility.SetDirty(destination);
        }

        private void Overwrite(RuntimeAnimatorController destination)
        {
            var infos = AnimatorUtility.GetAllStatesInfo(destination);

            foreach (var info in infos)
            {
                if (sourceInfo.TryGetBinding(info.state.name, out var binding))
                {
                    if (info.state.motion is BlendTree tree)
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
                        info.state.motion = GetMotion(binding.tags[0], binding.excluded);
                    }
                }
            }
            EditorUtility.SetDirty(destination);
        }

        private void Fill(RuntimeAnimatorController destination)
        {
            var infos = AnimatorUtility.GetAllStatesInfo(destination);

            foreach (var info in infos)
            {
                if (sourceInfo.TryGetBinding(info.state.name, out var binding))
                {
                    if (info.state.motion is BlendTree tree)
                    {
                        if (tree.children.Length == 0)
                        {
                            ChildMotion[] motions = new ChildMotion[binding.tags.Count];

                            for (int i = 0; i < binding.tags.Count; i++)
                            {
                                motions[i].motion = GetMotion(binding.tags[i], binding.excluded);
                                motions[i].timeScale = 1f;
                            }
                            tree.children = motions;
                        }
                    }
                    else
                    {
                        if (info.state.motion == null) info.state.motion = GetMotion(binding.tags[0], binding.excluded);
                    }
                }
            }
            EditorUtility.SetDirty(destination);
        }

        private Motion GetMotion(string tag, string excluded)
        {
            var temp = animations.OrderBy(x => Random.value).ToList();
            for (int i = 0; i < temp.Count; i++)
            {
                var clips = AnimatorUtility.GetAnimationClipsFromImporter(AssetDatabase.GetAssetPath(temp[i]));
                foreach (var clip in clips)
                {
                    if (AnimatorSetterInfo.Validate(clip.name, tag, excluded)) return clip;
                }
            }
            return null;
        }

        private void SyncStates(RuntimeAnimatorController destination)
        {
            var originalStatesInfo = AnimatorUtility.GetAllStatesInfo(sourceInfo.source);
            var destinationStatesInfo = AnimatorUtility.GetAllStatesInfo(destination);
            var controller = destination as AnimatorController;

            foreach (var info in originalStatesInfo)
            {
                if (destinationStatesInfo.Find(x => x.state.name == info.state.name).state == null)
                {
                    controller.layers[info.layer].stateMachine.AddState(info.state.name, info.position);
                }
            }

            EditorUtility.SetDirty(destination);
        }

        private void SyncParameters(RuntimeAnimatorController destination)
        {
            var original = sourceInfo.source as AnimatorController;
            var controller = destination as AnimatorController;

            foreach (var parameter in original.parameters)
            {
                if (!HasParameter(parameter.name, controller)) controller.AddParameter(parameter);
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