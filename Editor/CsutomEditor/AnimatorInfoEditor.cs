#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    [CustomEditor(typeof(AnimatorInfo))]
    public class AnimatorInfoEditor : Editor
    {
        private AnimatorInfo m_AnimatorInfo;

        private void OnEnable()
        {
            m_AnimatorInfo = (AnimatorInfo)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AnimatorInfo.source)));

            EditorGUILayout.BeginVertical("HelpBox");

            EditorGUILayout.BeginHorizontal("Box");
            EditorGUILayout.LabelField("Bindings", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            foreach (var binding in m_AnimatorInfo.bindings)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(binding.stateName, GUILayout.Width(100f));
                for (int i = 0; i < binding.tags.Count; i++)
                {
                    binding.tags[i] = EditorGUILayout.TextField(binding.tags[i]);
                }
                binding.excluded = EditorGUILayout.TextField(binding.excluded, GUILayout.Width(150f));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Load States"))
            {
                if (m_AnimatorInfo.bindings.Count == 0 || EditorUtility.DisplayDialog("Warning", "Are you sure to overwrite?", "Continue", "Cancel"))
                {
                    m_AnimatorInfo.LoadAnimations();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif