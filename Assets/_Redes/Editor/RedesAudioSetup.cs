using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Reflection;

namespace Redes.EditorTools
{
    public static class RedesAudioSetup
    {
        public const string MixerPath = "Assets/_Redes/Art/Audio/GameMixer.mixer";

        // [MenuItem("Tools/Redes/Audio Setup", priority = 10)]
        public static void CreateAudioMixerAndSetup()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Redes/Art/Audio"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Redes/Art"))
                    AssetDatabase.CreateFolder("Assets/_Redes", "Art");
                AssetDatabase.CreateFolder("Assets/_Redes/Art", "Audio");
            }

            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null)
            {
                // Create AudioMixer using Reflection to call internal AudioMixerController
                Type controllerType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
                if (controllerType != null)
                {
                    var createMethod = controllerType.GetMethod("CreateAudioMixerControllerAtPath", BindingFlags.Public | BindingFlags.Static);
                    if (createMethod != null)
                    {
                        mixer = (AudioMixer)createMethod.Invoke(null, new object[] { MixerPath });
                        Debug.Log($"[AUDIO] GameMixer created at {MixerPath} via reflection.");
                    }
                }
            }

            if (mixer != null)
            {
                // Add Groups "SFX" and "Music" via Reflection
                Type controllerType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
                Type groupType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerGroupController");

                if (controllerType != null && groupType != null)
                {
                    // Find master group
                    var masterGroupProp = controllerType.GetProperty("masterGroup", BindingFlags.Public | BindingFlags.Instance);
                    var masterGroup = masterGroupProp.GetValue(mixer);

                    // AddChildToParent method
                    var addChildMethod = controllerType.GetMethod("AddChildToParent", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

                    if (masterGroup != null && addChildMethod != null)
                    {
                        EnsureGroupExists(mixer, controllerType, groupType, masterGroup, addChildMethod, "SFX");
                        EnsureGroupExists(mixer, controllerType, groupType, masterGroup, addChildMethod, "Music");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureGroupExists(AudioMixer mixer, Type controllerType, Type groupType, object masterGroup, MethodInfo addChildMethod, string name)
        {
            var groups = mixer.FindMatchingGroups(name);
            bool exists = false;
            foreach (var g in groups)
            {
                if (g.name == name) { exists = true; break; }
            }

            if (!exists)
            {
                // Instantiate AudioMixerGroupController(mixer)
                var constructor = groupType.GetConstructor(new Type[] { controllerType });
                if (constructor != null)
                {
                    var newGroup = constructor.Invoke(new object[] { mixer });
                    // set name
                    var nameProp = groupType.GetProperty("name", BindingFlags.Public | BindingFlags.Instance);
                    if (nameProp != null)
                    {
                        nameProp.SetValue(newGroup, name);
                    }
                    else
                    {
                        var nameField = groupType.GetField("m_Name", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (nameField != null) nameField.SetValue(newGroup, name);
                    }

                    // Add to master group
                    addChildMethod.Invoke(mixer, new object[] { masterGroup, newGroup });
                    Debug.Log($"[AUDIO] Group {name} added to GameMixer via reflection");
                }
            }
        }

        public static AudioMixerGroup GetGroup(string name)
        {
            AssetDatabase.Refresh();
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null) return null;
            var groups = mixer.FindMatchingGroups(name);
            foreach (var g in groups)
            {
                if (g.name == name) return g;
            }
            return groups.Length > 0 ? groups[0] : null;
        }
    }
}

