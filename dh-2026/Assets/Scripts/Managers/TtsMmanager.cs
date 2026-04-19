using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class TextToSpeechManager : MonoBehaviour
{
    public static TextToSpeechManager Instance 
    { 
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<TextToSpeechManager>();
                if (instance == null)
                {
                    GameObject ttsManagerObj = new GameObject("TextToSpeechManager");
                    instance = ttsManagerObj.AddComponent<TextToSpeechManager>();
                }
            }
            return instance;
        }
    }
    
    private static TextToSpeechManager instance;
    
    private string apiKey = "sk_9125d4ef775060d348ddf0b4460ad45833d376d3a86b7206";
    private string voiceId = "j9jfwdrw7BRfcR43Qohk"; // Frederick

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SpeakText(string text, System.Action onComplete = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("Text is empty");
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(GenerateAndPlaySpeech(text, onComplete));
    }

    private IEnumerator GenerateAndPlaySpeech(string text, System.Action onComplete)
    {
        string url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}?output_format=pcm_16000";

        // Escape quotes in text for JSON
        string escapedText = text.Replace("\"", "\\\"").Replace("\n", " ");
        string jsonPayload = $"{{\"text\":\"{escapedText}\",\"model_id\":\"eleven_monolingual_v1\",\"voice_settings\":{{\"stability\":0.5,\"similarity_boost\":0.75}}}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonPayload));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("xi-api-key", apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    AudioClip audioClip = PcmUtility.ToPcmAudioClip(request.downloadHandler.data, sampleRate: 16000, channels: 1);
                    SoundManager.Instance.PlayAudioClip(audioClip);
                    Debug.Log("TTS playback started");
                    onComplete?.Invoke();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error processing audio: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"ElevenLabs Error: {request.error} - {request.downloadHandler.text}");
            }
        }
    }
}