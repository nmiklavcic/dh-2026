using System;
using System.Collections;
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
        public int Row = 1; // 1 = top row, 2 = bottom row
    }

    public class Situation
    {
        public string Description;
        public List<Option> Options;
    }

    // Characters per second — tweak this to change typing speed
    private const float TypeSpeed = 35f;

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
                    // the starting options you are met with when you wake up in the cabinet
                    // One option to get user familiar with the gameplay
                    new Option { Text = "Get up.",  OnChosen = () => LoadSituation("go_cabinet_start"),  Row = 1 }
                }
            },

            ["go_cabinet_start"] = new Situation
            {
                Description = "You get up wondering how you got here in the first place IDK spremen pol.",
                Options = new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = "Trace your hand along the left wall.",  OnChosen = () => LoadSituation("trace_left_cabinet"),  Row = 1 },
                    new Option { Text = "Trace your hand along the right wall.", OnChosen = () => LoadSituation("trace_right_cabinet"), Row = 1 },
                    new Option { Text = "Stand up and walk forward.",            OnChosen = () => LoadSituation("walk_forward_cabinet"), Row = 2 },
                }
            },

            
            ["cabinet_start"] = new Situation
            {
                Description = "",
                Options = new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = "Trace your hand along the left wall.",  OnChosen = () => LoadSituation("trace_left_cabinet"),  Row = 1 },
                    new Option { Text = "Trace your hand along the right wall.", OnChosen = () => LoadSituation("trace_right_cabinet"), Row = 1 },
                    new Option { Text = "Walk forward.",            OnChosen = () => LoadSituation("walk_forward_cabinet"), Row = 2 },
                }
            },

            ["trace_left_cabinet"] = new Situation
            {
                // Options for when you 
                Description = "Your fingertips find the wall. It is cold and smooth — painted plaster. You move slowly along it letting your left hand guide you.\n{INSERT TEXT ZA OKNO}",
                Options = new List<Option>
                {
                    new Option { Text = "Turn back.", OnChosen = () => LoadSituation("cabinet_start"), Row = 1 },
                    new Option { Text = "Keep going.", OnChosem = () => LoadSituation("cabinet_door"), Row = 1},
                    new Option { Text = "Jump through the open window.",      OnChosen = () => LoadSituation("cabinet_window"), Row = 2},
                    // option to go through the middle if we decide you have that option
                    //new Option { Text = "",      OnChosen = () => LoadSituation("carpet-"), Row = 2}
                }
            },

            ["cabinet_window"] = new Situation
            {
                Description = "As you feel the cold air on your skin, it reminds you of freedom. Not wanting to be bound to an unknown place you jump. ",
                // Load game over screen and return to menu
                
            },

            ["cabinet_door"] = new Situation
            {
                Description = "",
                Options = new List<Option>
                {
                    new Option { Text = "", OnChosen= () => LoadSituation(""), Row = 1},
                }
            },
            
            ["carpet_1"] = new Situation
            {
                Description = "You wake up on a cold floor. The air is still. You have no idea where you are.",
                Options = new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = "Trace your hand along the left wall.",  OnChosen = () => LoadSituation("trace_left_cabinet"),  Row = 1 },
                    new Option { Text = "Trace your hand along the right wall.", OnChosen = () => LoadSituation("trace_right_cabinet"), Row = 1 },
                    new Option { Text = "Stand up and walk forward.",            OnChosen = () => LoadSituation("walk_forward_cabinet"), Row = 2 },
                }
            },

            ["carpet_2"] = new Situation
            {
                Description = "You wake up on a cold floor. The air is still. You have no idea where you are.",
                Options = new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = "Trace your hand along the left wall.",  OnChosen = () => LoadSituation("trace_left_cabinet"),  Row = 1 },
                    new Option { Text = "Trace your hand along the right wall.", OnChosen = () => LoadSituation("trace_right_cabinet"), Row = 1 },
                    new Option { Text = "Stand up and walk forward.",            OnChosen = () => LoadSituation("walk_forward_cabinet"), Row = 2 },
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
        StopAllCoroutines();
        StartCoroutine(TypeText(situation.Description, ui));

        ui.OptionsContainerR1.Clear();
        ui.OptionsContainerR2.Clear();

        foreach (var option in situation.Options)
        {
            var btn = new Button();
            btn.text = $"[ {option.Text} ]";
            btn.AddToClassList("option-button");

            var captured = option;
            btn.clicked += () => captured.OnChosen?.Invoke();

            if (option.Row == 2)
                ui.OptionsContainerR2.Add(btn);
            else
                ui.OptionsContainerR1.Add(btn);
        }

        ui.ShowGameplay();
    }

    private IEnumerator TypeText(string fullText, UIManager ui)
    {
        ui.SituationText.text = "";
        ui.OptionsContainerR1.AddToClassList("hidden");
        ui.OptionsContainerR2.AddToClassList("hidden");

        float delay = 1f / TypeSpeed;
        foreach (char c in fullText)
        {
            ui.SituationText.text += c;
            yield return new WaitForSeconds(delay);
        }

        ui.OptionsContainerR1.RemoveFromClassList("hidden");
        ui.OptionsContainerR2.RemoveFromClassList("hidden");
    }
}
