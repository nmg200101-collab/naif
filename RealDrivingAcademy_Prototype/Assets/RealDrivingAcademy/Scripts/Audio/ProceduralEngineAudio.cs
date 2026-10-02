using UnityEngine;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class ProceduralEngineAudio : MonoBehaviour
    {
        public RealisticCarController car;
        [Range(0f,1f)] public float volume = 0.22f;
        public float baseFrequency = 45f;
        public float rpmFrequencyScale = 0.035f;
        double phase;
        int sampleRate;
        AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 0.65f;
            source.volume = volume;
            sampleRate = AudioSettings.outputSampleRate;
            var clip = AudioClip.Create("ProceduralEngine", sampleRate * 2, 1, sampleRate, true, OnAudioRead);
            source.clip = clip;
            source.Play();
        }

        void OnAudioRead(float[] data)
        {
            float rpm = car != null ? car.engineRpm : 0f;
            float freq = baseFrequency + rpm * rpmFrequencyScale;
            float amp = car != null && car.EngineRunning ? volume : 0f;
            for (int i = 0; i < data.Length; i++)
            {
                phase += 2.0 * System.Math.PI * freq / sampleRate;
                if (phase > System.Math.PI * 2.0) phase -= System.Math.PI * 2.0;
                // Fundamental + subtle second harmonic, deliberately simple for prototype.
                data[i] = (float)((System.Math.Sin(phase) * 0.72 + System.Math.Sin(phase * 2.0) * 0.28) * amp);
            }
        }
    }
}
