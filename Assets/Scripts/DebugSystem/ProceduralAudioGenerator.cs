using UnityEngine;
using System.Collections.Generic;

namespace DebugSystem
{
    public static class ProceduralAudioGenerator
    {
        private static Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        public static AudioClip GetShootSound()
        {
            if (cache.ContainsKey("Shoot")) return cache["Shoot"];

            int sampleRate = 44100;
            float duration = 0.15f;
            int sampleLength = (int)(sampleRate * duration);
            float[] samples = new float[sampleLength];

            for (int i = 0; i < sampleLength; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 25f); // Quick decay
                float freq = Mathf.Lerp(800f, 200f, t / duration); // Pitch drop
                
                // Square wave approx + noise
                float wave = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t)) * 0.5f;
                float noise = Random.Range(-1f, 1f) * 0.3f;

                samples[i] = (wave + noise) * env * 0.3f;
            }

            AudioClip clip = AudioClip.Create("ProceduralShoot", sampleLength, 1, sampleRate, false);
            clip.SetData(samples, 0);
            cache["Shoot"] = clip;
            return clip;
        }

        public static AudioClip GetImpactSound()
        {
            if (cache.ContainsKey("Impact")) return cache["Impact"];

            int sampleRate = 44100;
            float duration = 0.3f;
            int sampleLength = (int)(sampleRate * duration);
            float[] samples = new float[sampleLength];

            for (int i = 0; i < sampleLength; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 15f); // Decay
                
                // Heavy noise with low pass flavor
                float noise1 = Random.Range(-1f, 1f);
                float noise2 = Random.Range(-1f, 1f) * 0.5f;
                
                samples[i] = ((noise1 + noise2) / 2f) * env * 0.4f;
            }

            AudioClip clip = AudioClip.Create("ProceduralImpact", sampleLength, 1, sampleRate, false);
            clip.SetData(samples, 0);
            cache["Impact"] = clip;
            return clip;
        }

        public static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            GameObject audioObj = new GameObject("TempAudio_" + clip.name);
            audioObj.transform.position = position;
            AudioSource src = audioObj.AddComponent<AudioSource>();
            src.clip = clip;
            src.spatialBlend = 0f; // 2D sound for clarity
            src.volume = volume;
            src.Play();
            Object.Destroy(audioObj, clip.length + 0.1f);
        }
    }
}
