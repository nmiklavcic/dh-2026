using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // ── Data structures ──────────────────────────────────────

    public class Option
    {
        public string Text;
        public Action OnChosen;
    }

    public class Situation
    {
        public string Description;
        public List<Option> Options;
    }

    // ── Situation database (hardcoded for now) ───────────────

    private Dictionary<string, Situation> _situations;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _situations = new Dictionary<string, Situation>
        {
            ["start"] = new Situation
            {
                Description = "You wake up on a cold floor. The air is still. You have no idea where you are.",
                Options = new List<Option>
                {
                    new Option { Text = "Trace your hand along the left wall.",  OnChosen = () => LoadSituation("trace_left")  },
                    new Option { Text = "Stand up and walk forward.",             OnChosen = () => LoadSituation("walk_forward") },
                    new Option { Text = "Trace your hand along the right wall.", OnChosen = () => LoadSituation("trace_right") },
                }
            },

            ["trace_left"] = new Situation
            {
                Description = "Your fingertips find the wall. It is cold and smooth — painted plaster. You move slowly along it.",
                Options = new List<Option>
                {
                    new Option { Text = "Keep tracing forward.", OnChosen = () => LoadSituation("start") },
                    new Option { Text = "Stop and listen.",      OnChosen = () => LoadSituation("start") },
                }
            },

            ["trace_right"] = new Situation
            {
                Description = "The right wall feels the same — cold, smooth. A faint draft touches your wrist.",
                Options = new List<Option>
                {
                    new Option { Text = "Keep tracing forward.", OnChosen = () => LoadSituation("start") },
                    new Option { Text = "Stop and listen.",      OnChosen = () => LoadSituation("start") },
                }
            },

            ["walk_forward"] = new Situation
            {
                Description = "You take three steps. Your shin connects with something solid. A dull thunk echoes in the room.",
                Options = new List<Option>
                {
                    new Option { Text = "Check what you hit.",      OnChosen = () => Debug.Log("TODO: trigger Feel Around") },
                    new Option { Text = "Go back.",                 OnChosen = () => LoadSituation("start")                 },
                    new Option { Text = "Continue past it.",        OnChosen = () => LoadSituation("start")                 },
                }
            },
        };
    }

    // ── Load a situation ─────────────────────────────────────

    public void LoadSituation(string id)
    {
        if (!_situations.TryGetValue(id, out var situation))
        {
            Debug.LogWarning($"Situation '{id}' not found.");
            return;
        }

        var ui = UIManager.Instance;

        Debug.Log("LoadSituation called: " + id);
        ui.SituationText.text = situation.Description;

        ui.OptionsContainer.Clear();

        foreach (var option in situation.Options)
        {
            var btn = new Button();
            btn.text = $"[ {option.Text} ]";
            btn.AddToClassList("option-button");

            var captured = option;
            btn.clicked += () => captured.OnChosen?.Invoke();

            ui.OptionsContainer.Add(btn);
        }

        ui.ShowGameplay();
    }
}
