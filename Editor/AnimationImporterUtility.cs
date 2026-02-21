#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedeevEditor.Utilities
{
    public static class AnimationImporterUtility
    {
        [MenuItem("Assets/Redeev/Animation Importer/Set Names", validate = true)]
        private static bool SetNamesValidation() => IsSelectionModelImporter();

        [MenuItem("Assets/Redeev/Animation Importer/Set Position and Rotation", validate = true)]
        private static bool SetPositionAndRotationValidation() => IsSelectionModelImporter();

        [MenuItem("Assets/Redeev/Animation Importer/Set Loops", validate = true)]
        private static bool SetLoopsValidation() => IsSelectionModelImporter();

        [MenuItem("Assets/Redeev/Animation Importer/Remove Materials", validate = true)]
        private static bool RemoveMaterialsValidation() => IsSelectionModelImporter();

        public static bool IsSelectionModelImporter()
        {
            if (Selection.objects.Length == 0) return false;

            foreach (var obj in Selection.objects)
            {
                if (!IsModelImporter(obj)) return false;
            }

            return true;
        }

        public static bool IsModelImporter(UnityEngine.Object obj)
        {
            if (obj == null) return false;

            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) return false;
            
            return AssetImporter.GetAtPath(path) is ModelImporter;
        }

        public static void ForEachModelSelected(Action<ModelImporter> callback)
        {
            foreach (var obj in Selection.objects)
            {
                if (IsModelImporter(obj)) callback?.Invoke((ModelImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj)));
            }
        }

        public static void AddEvent(ModelImporter modelImporter, string eventName, float time, float floatValue = 0f, int intValue = 0)
        {
            ForEachClips(modelImporter, clip =>
            {
                AnimationEvent existingEvent = clip.events.ToList().Find(x => x.functionName == eventName);
                if (existingEvent == null)
                {
                    clip.events = new List<AnimationEvent>(clip.events)
                {
                    new() { functionName = eventName, time = time, floatParameter = floatValue, intParameter = intValue }
                }.ToArray();
                }
            });
        }

        public static void AddEventToSelection(string eventName, float time, float floatValue = 0f, int intValue = 0)
        {
            ForEachModelSelected(modelImporter =>
            {
                AddEvent(modelImporter, eventName, time, floatValue, intValue);
            });
        }

        public static void ForEachClips(ModelImporter modelImporter, Action<ModelImporterClipAnimation> callback)
        {
            ModelImporterClipAnimation[] clipAnimations = modelImporter.clipAnimations;
            if (clipAnimations.Length == 0) clipAnimations = modelImporter.defaultClipAnimations;
            for (int i = 0; i < clipAnimations.Length; i++)
            {
                callback?.Invoke(clipAnimations[i]);
            }
            modelImporter.clipAnimations = clipAnimations;
            modelImporter.SaveAndReimport();
        }

        [MenuItem("Assets/Redeev/Animation Importer/Set Names")]
        private static void SetNames()
        {
            ForEachModelSelected(modelImporter =>
            {
                string fileName = Path.GetFileNameWithoutExtension(modelImporter.assetPath);
                ForEachClips(modelImporter, clip =>
                {
                    clip.name = fileName;
                });
            });
        }

        [MenuItem("Assets/Redeev/Animation Importer/Set Position and Rotation")]
        private static void SetPositionAndRotation()
        {
            ForEachModelSelected(modelImporter =>
            {
                ForEachClips(modelImporter, clip =>
                {
                    clip.keepOriginalPositionY = false;
                    clip.heightFromFeet = true;
                    clip.keepOriginalPositionXZ = true;
                    clip.keepOriginalOrientation = true;
                    clip.lockRootHeightY = true;
                    clip.lockRootPositionXZ = true;
                    clip.lockRootRotation = true;
                });
            });
        }

        [MenuItem("Assets/Redeev/Animation Importer/Set Loops")]
        private static void SetLoops()
        {
            ForEachModelSelected(modelImporter =>
            {
                ForEachClips(modelImporter, clip =>
                {
                    if (clip.name.Contains("Walk") || clip.name.Contains("Idle") || clip.name.Contains("Run"))
                    {
                        clip.loopTime = true;
                    }
                });
            });
        }

        [MenuItem("Assets/Redeev/Animation Importer/Remove Materials")]
        private static void RemoveMaterials()
        {
            ForEachModelSelected(modelImporter =>
            {
                modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            });
        }
    }
}
#endif