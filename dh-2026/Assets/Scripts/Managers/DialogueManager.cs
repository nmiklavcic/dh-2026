using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // ── Data structures ──────────────────────────────────────

    private HashSet<string> _flags = new HashSet<string>();

    public void setFlag(string flag) => _flags.Add(flag);
    public void removeFlag(string flag) => _flags.Remove(flag);
    public bool hasFlag(string flag) => _flags.Contains(flag);

    // how to set flag:
    // new Option { Text = () => "...", OnChosen = () => { setFlag("..."); LoadSituation("..."); }, Row = X }

    // how to branch on a flag:
    // new Option { Text = () => "...", OnChosen = () => LoadSituation(hasFlag("flag") ? "a" : "b"), Row = 2 }

    // how to add conditional options:
    //   Options = () => {
    //       var list = new List<Option> { new Option { ... } };
    //       if (hasFlag("flag")) list.Add(new Option { ... });
    //       return list;
    //   },

    public class Option
    {
        public Func<string> Text;
        public Action OnChosen;
        public int Row = 1; // 1 = top row, 2 = bottom row, 3 = sidebar
    }

    public class Situation
    {
        public Func<string> Description;
        public Func<List<Option>> Options;
    }

    // ── Inventory ────────────────────────────────────────────

    public class InventoryItem
    {
        public string Name;
        public Action OnUse;
    }

    private List<InventoryItem> _inventory = new List<InventoryItem>();

    public void AddItem(string name, Action onUse)
    {
        _inventory.Add(new InventoryItem { Name = name, OnUse = onUse });
        UIManager.Instance.RefreshInventory(_inventory);
    }

    public void RemoveItem(string name)
    {
        _inventory.RemoveAll(i => i.Name == name);
        UIManager.Instance.RefreshInventory(_inventory);
    }

    // Characters per second
    private const float TypeSpeed = 35f;

    // ── Situation database ───────────────────────────────────

    private Dictionary<string, Situation> _situations;
    private string _currentSituation = "";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _situations = new Dictionary<string, Situation>
        {
            ["start"] = new Situation
            {
                Description = () => "You wake up on a cold floor. The air is still. You have no idea where you are.",
                Options = () => new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // One option to get user familiar with the gameplay
                    new Option { Text = () => "Get up.",  OnChosen = () => {
                        LoadSituation("cabinet_start");
                        try
                        {
                            SoundManager.Instance.PlaySound("door_open");
                        } catch (Exception e) { 
                            Debug.Log("Sound not found. " + e.Message); 
                        }
                        
                    },  Row = 1 }
                }
            },

            // ── Cabinet ──────────────────────────────────────────────────────────────────

            // ── Cabinet ──────────────────────────────────────────────────────────────────

            ["cabinet_door"] = new Situation
            {
                Description = () => "{cabinet_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("cabinet_window"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation("cabinet_table"), Row = 1 },
                    new Option { Text = () => "Go straight through the door.", OnChosen = () => LoadSituation("hallway_o_Cabinet_door"), Row = 2 },
                }
            },

            ["cabinet_start"] = new Situation
            {
                Description = () => "{cabinet_start}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("cabinet_window"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation("cabinet_table"), Row = 1 },
                    new Option { Text = () => "Go east in to the unknown.", OnChosen = () => LoadSituation("cabinet_carpet"), Row = 2 },
                }
            },

            ["cabinet_carpet"] = new Situation
            {
                Description = () => "{cabinet_carpet}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east.", OnChosen = () => LoadSituation("cabinet_door"), Row = 1 },
                    new Option { Text = () => "Go west.", OnChosen = () => LoadSituation("cabinet_start"), Row = 1 },
                    //new Option { Text = () => "Check what you tripped over.", OnChosen = () => LoadSituation("cabinet_carpet_check"), Row = 2 },
                }
            },

            ["cabinet_table"] = new Situation
            {
                Description = () => "{cabinet_table}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("cabinet_door"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("cabinet_start"), Row = 1 },
                    //new Option { Text = () => "Check what you hit.", OnChosen = () => LoadSituation("cabinet_table_check"), Row = 2 },
                }
            },

            ["cabinet_window"] = new Situation
            {
                Description = () => "{cabinet_table}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("cabinet_door"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("cabinet_start"), Row = 1 },
                    //new Option { Text = () => "Jump through the open window", OnChosen = () => LoadSituation("cabinet_window_check"), Row = 2 },
                }
            },

            // ── Hallway ───────────────────────────────────────────────────────────────────

            ["hallway_cabinet_door"] = new Situation
            {
                Description = () => "{hallway_cabinet_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("livingroom_gramophone"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation("hallway_closet_door"), Row = 1 },
                    new Option { Text = () => "Eneter cabinet", OnChosen = () => LoadSituation("cabinet_door"), Row = 2 },
                }
            },

            ["hallway_o_cabinet_door"] = new Situation
            {
                Description = () => "{hallway_o_cabinet_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("livingroom_casete_table"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation("hallway_bathroom_door"), Row = 1 },
                    new Option { Text = () => "Go east, back to the cabinet.", OnChosen = () => LoadSituation("hallway_cabinet_door"), Row = 2 },
                }
            },

            ["hallway_closet_door"] = new Situation
            {
                Description = () => "{hallway_closet_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("hallway_cabinet_door"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation("hallway_study_door"), Row = 1 },
                    //new Option { Text = () => "Check the closet", OnChosen = () => LoadSituation("closet_check"), Row = 2 },
                }
            },

            ["hallway_o_closet_door"] = new Situation
            {
                Description = () => "{hallway_o_closet_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("livingroom_casete_table"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation("hallway_bathroom_door"), Row = 1 },
                    new Option { Text = () => "Go east, back to the closet.", OnChosen = () => LoadSituation("hallway_closet_door"), Row = 2 },
                }
            },

            ["hallway_study_door"] = new Situation
            {
                Description = () => "{hallway_study_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("hallway_closet_door"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation("hallway_creaking_board"), Row = 1 },
                    new Option { Text = () => "Enter study.", OnChosen = () => LoadSituation("study_door"), Row = 2 },
                }
            },

            ["hallway_o_study_door"] = new Situation
            {
                Description = () => "{hallway_o_study_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("livingroom_casete_table"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation("hallway_bathroom_door"), Row = 1 },
                    new Option { Text = () => "Go east, back to the study.", OnChosen = () => LoadSituation("hallway_study_door"), Row = 2 },
                }
            },

            ["hallway_bathroom_door"] = new Situation
            {
                Description = () => "{hallway_bathroom_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("hallway_creaking_board_n"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("livingroom_casete_table"), Row = 1 },
                    new Option { Text = () => "Enter bathroom.", OnChosen = () => LoadSituation("bathroom_door"), Row = 2 },
                }
            },

            ["hallway_o_bathroom_door"] = new Situation
            {
                Description = () => "{hallway_o_bathroom_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("hallway_creaking_board_s"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("hallway_study_door"), Row = 1 },
                    new Option { Text = () => "Go north, back to the bathroom.", OnChosen = () => LoadSituation("hallway_bathroom_Door"), Row = 2 },
                }
            },

            ["hallway_creaking_board_n"] = new Situation
            {
                Description = () => "{hallway_creaking_board_n}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("hallway_kitchen_door"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("hallway_bathroom_door"), Row = 1 },
                    // new Option { Text = () => "Check under the board.", OnChosen = () => LoadSituation("creaking_board_check"), Row = 2 },
                }
            },

            ["hallway_creaking_board_s"] = new Situation
            {
                Description = () => "{hallway_creaking_board_s}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("hallway_bedroom_door"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("hallway_study_door"), Row = 1 },
                     //new Option { Text = () => "Check under the board.", OnChosen = () => LoadSituation("creaking_board_check"), Row = 2 },
                }
            },

            ["hallway_bedroom_door"] = new Situation
            {
                Description = () => "{hallway_bedroom_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("exit"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("creaking_board_s"), Row = 1 },
                    new Option { Text = () => "Enter bedroom.", OnChosen = () => LoadSituation("bedroom_door"), Row = 2 },
                }
            },

            ["hallway_o_bedroom_door"] = new Situation
            {
                Description = () => "{hallway_o_bedroom_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("hallway_bedroom_door"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("hallway_creaking_board_s"), Row = 1 },
                    new Option { Text = () => "Go north, back to the bedroom.", OnChosen = () => LoadSituation("hallway_bedroom_door"), Row = 2 },
                }
            },

            ["hallway_kitchen_door"] = new Situation
            {
                Description = () => "{hallway_kitchen_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("exit"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("hallway_creaking_board_n"), Row = 1 },
                    new Option { Text = () => "Enter kitchen.", OnChosen = () => LoadSituation("kitchen_door"), Row = 2 },
                }
            },

            ["hallway_o_kitchen_door"] = new Situation
            {
                Description = () => "{hallway_o_kitchen_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("exit"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("hallway_bedroom_door"), Row = 1 },
                    new Option { Text = () => "Go north, back to the kitchen.", OnChosen = () => LoadSituation("hallway_kitchen_door"), Row = 2 },
                }
            },

            // ── Study ─────────────────────────────────────────────────────────────────────

            ["study_door"] = new Situation
            {
                Description = () => "{study_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["study_skeleton"] = new Situation
            {
                Description = () => "{study_skeleton}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["study_table"] = new Situation
            {
                Description = () => "{study_table}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            // ── Bathroom ──────────────────────────────────────────────────────────────────

            ["bathroom_door"] = new Situation
            {
                Description = () => "{bathroom_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["bathroom_bath"] = new Situation
            {
                Description = () => "{bathroom_bath}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["bathroom_toilet"] = new Situation
            {
                Description = () => "{bathroom_toilet}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["bathroom_sink"] = new Situation
            {
                Description = () => "{bathroom_sink}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            // ── Bedroom ───────────────────────────────────────────────────────────────────

            ["bedroom_door"] = new Situation
            {
                Description = () => "{bedroom_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["bedroom_closet"] = new Situation
            {
                Description = () => "{bedroom_closet}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["bedroom_nightstand_1"] = new Situation
            {
                Description = () => "{bedroom_nightstand_1}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["bedroom_nightstand_2"] = new Situation
            {
                Description = () => "{bedroom_nightstand_2}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["bedroom_bed"] = new Situation
            {
                Description = () => "{bedroom_bed}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            // ── Living Room ───────────────────────────────────────────────────────────────

            ["livingroom_gramophone"] = new Situation
            {
                Description = () => "{livingroom_gramophone}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["livingroom_casete_table"] = new Situation
            {
                Description = () => "{livingroom_casete_table}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["livingroom_painting_safe"] = new Situation
            {
                Description = () => "{livingroom_painting_safe}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["livingroom_fireplace"] = new Situation
            {
                Description = () => "{livingroom_fireplace}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["livingroom_glass_table"] = new Situation
            {
                Description = () => "{livingroom_glass_table}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            // ── Kitchen ───────────────────────────────────────────────────────────────────

            ["kitchen_door"] = new Situation
            {
                Description = () => "{kitchen_door}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["kitchen_drawer"] = new Situation
            {
                Description = () => "{kitchen_drawer}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["kitchen_counter"] = new Situation
            {
                Description = () => "{kitchen_counter}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["kitchen_fridge"] = new Situation
            {
                Description = () => "{kitchen_fridge}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            // ── Exit ──────────────────────────────────────────────────────────────────────

            ["exit"] = new Situation
            {
                Description = () => "{exit}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            



        };
    }

    private void StartDotGame(System.Action onComplete = null)
    {
        if (DotConnectingMinigame.Instance == null)
        {
            GameObject minigameGO = new GameObject("DotConnectingMinigame");
            minigameGO.AddComponent<DotConnectingMinigame>();
        }

        DotConnectingMinigame.Instance.StartGame(onComplete ?? (() => OnDotGameComplete()));
    }

    public void OnDotGameComplete()
    {
        Debug.Log("Dot game completed!");
        UIManager.Instance.ShowGameplay();
        LoadSituation("dot_game_complete");
    }

    private void StartPuzzleGame()
    {
        if (JigsawPuzzleMinigame.Instance == null)
        {
            GameObject minigameGO = new GameObject("JigsawPuzzleMinigame");
            minigameGO.AddComponent<JigsawPuzzleMinigame>();
        }
        
        JigsawPuzzleMinigame.Instance.StartGame(() => OnPuzzleGameComplete());
    }

    public void OnPuzzleGameComplete()
    {
        Debug.Log("Puzzle game completed!");
        UIManager.Instance.ShowGameplay();

        LoadSituation("cabinet_start");
    }

    // ── Load a situation ─────────────────────────────────────

    public void LoadSituation(string id)
    {
        if (!_situations.TryGetValue(id, out var situation))
        {
            Debug.LogWarning($"Situation '{id}' not found.");
            return;
        }

        // Start loop if entering "start" situation
        if (id == "start" && _currentSituation != "start")
        {
            SoundManager.Instance.PlaySoundLoop("fireplace-1", 0.5f);
        }

        // Stop loop if leaving "start" situation
        if (_currentSituation == "start" && id != "start")
        {
            SoundManager.Instance.StopLoop();
        }

        _currentSituation = id;

        var ui = UIManager.Instance;

        Debug.Log("LoadSituation called: " + id);
        StopAllCoroutines();
        StartCoroutine(TypeSituation(situation, ui));

        ui.ShowGameplay();
    }

    private IEnumerator TypeSituation(Situation situation, UIManager ui)
    {
        float delay = 1f / TypeSpeed;

        // Clear containers and hide them
        ui.OptionsContainerR1.Clear();
        ui.OptionsContainerR2.Clear();
        ui.OptionsContainerR1.AddToClassList("hidden");
        ui.OptionsContainerR2.AddToClassList("hidden");

        // Type out the description
        ui.SituationText.text = "";
        foreach (char c in situation.Description())
        {
            ui.SituationText.text += c;
            yield return new WaitForSeconds(delay);
        }

        // Small pause before options appear
        yield return new WaitForSeconds(0.4f);

        // Show containers
        ui.OptionsContainerR1.RemoveFromClassList("hidden");
        ui.OptionsContainerR2.RemoveFromClassList("hidden");

        // Rebuild sidebar: inventory items first, then Row=3 situational options
        var allOptions = situation.Options();
        var sidebarOptions = new List<Option>();
        foreach (var opt in allOptions)
            if (opt.Row == 3) sidebarOptions.Add(opt);

        ui.RefreshInventory(_inventory);

        if (ui.OptionsContainerR3 != null)
        {
            foreach (var sideOpt in sidebarOptions)
            {
                var sideBtn = new Button();
                sideBtn.text = $"[ {sideOpt.Text()} ]";
                sideBtn.AddToClassList("inventory-button");
                var capturedSide = sideOpt;
                sideBtn.clicked += () => capturedSide.OnChosen?.Invoke();
                ui.OptionsContainerR3.Add(sideBtn);
            }

            if (sidebarOptions.Count > 0)
                ui.OptionsContainerR3.RemoveFromClassList("hidden");
        }
        else
        {
            Debug.LogError("OptionsContainerR3 is null — sidebar options will not appear. Check UXML and reimport.");
        }

        // Type out Row 1 and Row 2 options one by one
        foreach (var option in allOptions)
        {
            if (option.Row == 3) continue;

            var btn = new Button();
            btn.text = "";
            btn.AddToClassList("option-button");

            var captured = option;
            btn.clicked += () => captured.OnChosen?.Invoke();

            if (option.Row == 2)
                ui.OptionsContainerR2.Add(btn);
            else
                ui.OptionsContainerR1.Add(btn);

            string fullLabel = $"[ {option.Text()} ]";
            foreach (char c in fullLabel)
            {
                btn.text += c;
                yield return new WaitForSeconds(delay);
            }

            yield return new WaitForSeconds(0.15f);
        }
    }
}
