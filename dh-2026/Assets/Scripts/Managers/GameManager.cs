using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager not found!");
            return;
        }
        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager not found!");
            return;
        }
        DialogueManager.Instance.LoadSituation("start");
    }
}
