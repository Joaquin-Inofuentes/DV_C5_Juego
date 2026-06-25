using UnityEditor;
using UnityEngine;
using System.IO;

namespace Redes.EditorTools
{
    /// <summary>
    /// Creates procedural audio clips for VFX/SFX that don't have external audio files.
    /// Called by the Corregir pipeline to ensure all sounds exist.
    /// </summary>
    public static class RedesProceduralAudio
    {
        private const string AudioFolder = "Assets/_Redes/Art/Audio";
        public const string OuchPath = AudioFolder + "/Ouch.wav";
        public const string ObstacleHitPath = AudioFolder + "/ObstacleHit.wav";

        /// <summary>
        /// Ensures both procedural audio clips exist. Safe to call multiple times.
        /// </summary>
        public static void EnsureAudioClips()
        {
            EnsureFolder("Assets/_Redes", "Art");
            EnsureFolder("Assets/_Redes/Art", "Audio");

            if (!File.Exists(OuchPath))
            {
                Debug.Log("[REDES][CORREGIR] Creando sonido 'Ouch' procedural...");
                CreateOuchClip();
            }

            if (!File.Exists(ObstacleHitPath))
            {
                Debug.Log("[REDES][CORREGIR] Creando sonido 'ObstacleHit' procedural...");
                CreateObstacleHitClip();
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Creates a short descending-pitch sine wave that mimics a human grunt/ouch.
        /// Duration: ~0.25 seconds, starts at 400Hz descends to 200Hz.
        /// </summary>
        private static void CreateOuchClip()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float normalizedT = (float)i / sampleCount;

                // Descending frequency from 400Hz to 200Hz
                float freq = Mathf.Lerp(400f, 200f, normalizedT);

                // Sine wave with envelope (attack + decay)
                float envelope = 1f;
                if (normalizedT < 0.05f)
                    envelope = normalizedT / 0.05f; // Quick attack
                else if (normalizedT > 0.6f)
                    envelope = (1f - normalizedT) / 0.4f; // Slow decay

                // Add slight harmonics for a more "human" quality
                float sample = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f;
                sample += Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.2f;
                sample += Mathf.Sin(2f * Mathf.PI * freq * 3f * t) * 0.1f;

                samples[i] = sample * envelope * 0.8f;
            }

            WriteWav(OuchPath, samples, sampleRate);
        }

        /// <summary>
        /// Creates a short metallic impact / white noise burst.
        /// Duration: ~0.15 seconds.
        /// </summary>
        private static void CreateObstacleHitClip()
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            System.Random rng = new System.Random(42); // Deterministic seed

            for (int i = 0; i < sampleCount; i++)
            {
                float normalizedT = (float)i / sampleCount;

                // Sharp attack, fast decay envelope
                float envelope = 1f;
                if (normalizedT < 0.02f)
                    envelope = normalizedT / 0.02f;
                else
                    envelope = Mathf.Pow(1f - normalizedT, 3f);

                // Mix of noise + high-frequency sine for metallic quality
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float tone = Mathf.Sin(2f * Mathf.PI * 2000f * ((float)i / sampleRate)) * 0.3f;

                samples[i] = (noise * 0.7f + tone) * envelope * 0.7f;
            }

            WriteWav(ObstacleHitPath, samples, sampleRate);
        }

        /// <summary>
        /// Writes a mono 16-bit PCM WAV file.
        /// </summary>
        private static void WriteWav(string path, float[] samples, int sampleRate)
        {
            string fullPath = Path.Combine(Application.dataPath, "..", path);
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                int channels = 1;
                int bitsPerSample = 16;
                int byteRate = sampleRate * channels * bitsPerSample / 8;
                int blockAlign = channels * bitsPerSample / 8;
                int dataSize = samples.Length * blockAlign;

                // RIFF header
                writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataSize);
                writer.Write(new char[] { 'W', 'A', 'V', 'E' });

                // fmt chunk
                writer.Write(new char[] { 'f', 'm', 't', ' ' });
                writer.Write(16); // chunk size
                writer.Write((short)1); // PCM
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write((short)blockAlign);
                writer.Write((short)bitsPerSample);

                // data chunk
                writer.Write(new char[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);

                for (int i = 0; i < samples.Length; i++)
                {
                    float clamped = Mathf.Clamp(samples[i], -1f, 1f);
                    short pcm = (short)(clamped * 32767f);
                    writer.Write(pcm);
                }
            }

            Debug.Log($"[REDES][CORREGIR] WAV creado: {path}");
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
