using UnityEngine;
using System;

public class PcmUtility
{
    public static AudioClip ToPcmAudioClip(byte[] pcmData, int sampleRate = 16000, int channels = 1)
    {
        if (pcmData == null || pcmData.Length == 0)
        {
            Debug.LogError("Empty PCM data");
            return null;
        }

        // PCM 16-bit: 2 bytes per sample
        int sampleCount = pcmData.Length / 2;
        float[] audioData = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            short sample = BitConverter.ToInt16(pcmData, i * 2);
            audioData[i] = sample / 32768f;
        }

        int clipSampleCount = sampleCount / channels;
        AudioClip audioClip = AudioClip.Create("TTS_Audio", clipSampleCount, channels, sampleRate, false);
        audioClip.SetData(audioData, 0);

        Debug.Log($"PCM AudioClip created: {clipSampleCount} samples, {channels} channels, {sampleRate}Hz");
        return audioClip;
    }
}
