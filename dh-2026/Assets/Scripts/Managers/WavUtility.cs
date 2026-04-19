using UnityEngine;
using System;
using System.Text;

public class WavUtility
{
    public static AudioClip ToAudioClip(byte[] wavData)
    {
        if (wavData == null || wavData.Length < 44)
        {
            Debug.LogError($"Invalid WAV data: length={wavData?.Length ?? 0}");
            return null;
        }

        // Log first 100 bytes as hex and ASCII for debugging
        string hex = BitConverter.ToString(wavData, 0, Mathf.Min(100, wavData.Length));
        Debug.Log($"WAV Data (first 100 bytes): {hex}");

        // Check RIFF header
        string riffHeader = Encoding.ASCII.GetString(wavData, 0, 4);
        Debug.Log($"RIFF Header: {riffHeader}");

        if (riffHeader != "RIFF")
        {
            Debug.LogError("Not a valid RIFF file");
            return null;
        }

        // Parse WAV header
        int channels = BitConverter.ToInt16(wavData, 8);
        int sampleRate = BitConverter.ToInt32(wavData, 24);
        short bitsPerSample = BitConverter.ToInt16(wavData, 34);

        Debug.Log($"WAV Header: channels={channels}, sampleRate={sampleRate}, bitsPerSample={bitsPerSample}");

        // Find data chunk - search through the file
        int dataOffset = -1;
        int dataSize = 0;

        for (int i = 12; i < wavData.Length - 8; i++)
        {
            if (wavData[i] == 'd' && wavData[i + 1] == 'a' &&
                wavData[i + 2] == 't' && wavData[i + 3] == 'a')
            {
                dataOffset = i + 8;
                dataSize = BitConverter.ToInt32(wavData, i + 4);
                Debug.Log($"Found 'data' chunk at offset {i}: dataOffset={dataOffset}, dataSize={dataSize}");
                break;
            }
        }

        if (dataOffset == -1)
        {
            Debug.LogError("Could not find 'data' chunk marker");
            // Try alternative: assume data starts at offset 44
            dataOffset = 44;
            dataSize = wavData.Length - 44;
            Debug.Log($"Using fallback: dataOffset={dataOffset}, dataSize={dataSize}");
        }

        if (dataSize <= 0)
        {
            Debug.LogError($"Invalid dataSize: {dataSize}");
            return null;
        }

        if (dataOffset + dataSize > wavData.Length)
        {
            Debug.LogWarning($"Data chunk exceeds file size: offset={dataOffset}, size={dataSize}, fileLength={wavData.Length}");
            dataSize = wavData.Length - dataOffset;
        }

        // Convert byte array to float array
        int sampleCount = dataSize / (bitsPerSample / 8);
        float[] audioData = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            if (bitsPerSample == 16)
            {
                short sample = BitConverter.ToInt16(wavData, dataOffset + i * 2);
                audioData[i] = sample / 32768f;
            }
            else if (bitsPerSample == 8)
            {
                audioData[i] = (wavData[dataOffset + i] - 128) / 128f;
            }
        }

        int clipSampleCount = sampleCount / channels;
        if (clipSampleCount <= 0)
        {
            Debug.LogError($"Invalid clip sample count: {clipSampleCount}");
            return null;
        }

        // Create AudioClip
        AudioClip audioClip = AudioClip.Create("TTS_Audio", clipSampleCount, channels, sampleRate, false);
        audioClip.SetData(audioData, 0);

        Debug.Log($"AudioClip created: {clipSampleCount} samples, {channels} channels, {sampleRate}Hz");
        return audioClip;
    }
}
