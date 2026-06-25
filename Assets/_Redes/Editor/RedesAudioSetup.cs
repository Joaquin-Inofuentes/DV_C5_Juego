using UnityEditor;
using UnityEditor.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace Redes.EditorTools
{
    public static class RedesAudioSetup
    {
        public const string MixerPath = "Assets/_Redes/Art/Audio/GameMixer.mixer";

        [MenuItem("Tools/Redes/Audio Setup", priority = 10)]
        public static void CreateAudioMixerAndSetup()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Redes/Art/Audio"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Redes/Art"))
                    AssetDatabase.CreateFolder("Assets/_Redes", "Art");
                AssetDatabase.CreateFolder("Assets/_Redes/Art", "Audio");
            }

            AudioMixerController mixer = AssetDatabase.LoadAssetAtPath<AudioMixerController>(MixerPath);
            if (mixer == null)
            {
                mixer = AudioMixerController.CreateAudioMixerControllerAtPath(MixerPath);
                Debug.Log($"[AUDIO] GameMixer created at {MixerPath}");
            }

            // Create groups if they don't exist
            var masterGroup = mixer.masterGroup;
            
            var sfxGroup = FindGroup(mixer, "SFX");
            if (sfxGroup == null)
            {
                mixer.AddChildToParent(mixer.masterGroup, new AudioMixerGroupController(mixer) { name = "SFX" });
                sfxGroup = FindGroup(mixer, "SFX");
                Debug.Log("[AUDIO] SFX Group added to GameMixer");
            }

            var musicGroup = FindGroup(mixer, "Music");
            if (musicGroup == null)
            {
                mixer.AddChildToParent(mixer.masterGroup, new AudioMixerGroupController(mixer) { name = "Music" });
                musicGroup = FindGroup(mixer, "Music");
                Debug.Log("[AUDIO] Music Group added to GameMixer");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static AudioMixerGroup GetGroup(string name)
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null) return null;
            var groups = mixer.FindMatchingGroups(name);
            return groups.Length > 0 ? groups[0] : null;
        }

        private static AudioMixerGroupController FindGroup(AudioMixerController mixer, string name)
        {
            foreach (var group in mixer.FindMatchingGroups(name))
            {
                if (group.name == name) return group as AudioMixerGroupController;
            }
            return null;
        }
    }
}
