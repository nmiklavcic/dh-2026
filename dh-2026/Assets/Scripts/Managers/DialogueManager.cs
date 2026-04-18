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
    
    public bool hasFlag(string flag) => _flags.Contains(flag);

    // how to set flag
    // Change option to :
    // new Option { Text = () => "...", OnChosen = () => 
    // {
    //     SetFlag("...");
    //     LoadSituation("...");
    // }, Row = X}
    //

    // how to check flag and choose between two options :
    // Change option that has differing situations
    // new Option { Text = () => "...", OnChosen = () => LoadSituation(HasFlag("carpet_seen") ? "carpet_2" : "carpet_1"), Row = 2 }



    public class Option
    {
        public Func<string> Text;
        public Action OnChosen;
        public int Row = 1; // 1 = top row, 2 = bottom row
    }

    public class Situation
    {
        public Func<string> Description;
        public Func<List<Option>> Options; 
    }

    // how to write situations with flags
    //   Options = () =>
    //   {
    //       var list = new List<Option>
    //       {
    //           new Option { Text = () => "Feel around.", OnChosen = () => LoadSituation("..."), Row = 1 },
    //       };
    //       if (hasFlag("cabinet_door_knob")) list.Add(new Option { Text = () => "Open the door.", OnChosen = () => LoadSituation("..."), Row = 2 });
    //       return list;
    //   },

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
            // ── Top  half ─────────────────────────────────────

            ["start"] = new Situation
            {
                Description = () => "You wake up on a cold floor. The air is still. You have no idea where you are.",
                Options = () => new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // One option to get user familiar with the gameplay
                    new Option { Text = () => "Get up.",  OnChosen = () => LoadSituation("cabinet_start"),  Row = 1 }
                }
            },

            ["go_cabinet_start"] = new Situation
            {
                Description = () => "You get up wondering how you got here in the first place IDK spremen pol.",
                Options = () => new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = () => "Feel around the room.",  OnChosen = () => StartDotGame(),  Row = 1 },
                    new Option { Text = () => "Trace your hand along the right wall.", OnChosen = () => LoadSituation("trace_right_cabinet"), Row = 1 },
                    new Option { Text = () => "Stand up and walk forward.",            OnChosen = () => LoadSituation("walk_forward_cabinet"), Row = 2 },
                }
            },

            
            ["cabinet_start"] = new Situation
            {
                Description = () => hasFlag("cabinet_start") ? "You are back where you started.\nSquare one.\nYou stop and wonder if you will ever escape this god awfull place." : "You get up wondering how you got here in the first place.\nNot having many other options you decide to explore your new environment.",
                Options = () => new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = () => "Trace your hand along the left wall.",  OnChosen = () => 
                    {
                        setFlag("cabinet_start");
                        LoadSituation("cabinet_window");
                    }, Row = 1},
                    new Option { Text = () => "Trace your hand along the right wall.", OnChosen = () => 
                    {
                        setFlag("cabinet_start");
                        LoadSituation("cabinet_table");
                    }, Row = 1 },
                    new Option { Text = () => "Walk forward, into the unknown.", OnChosen = () => 
                    {
                        setFlag("cabinet_start");
                        LoadSituation("cabinet_carpet"); 
                    }, Row = 2 },
                }
            },

            // Cabinet table options  
            ["cabinet_table"] = new Situation
            {
                // option if user ends up back at the start
                Description = () => hasFlag("cabinet_table") ? "You stop befor the cabinet table." : "You hit something!",
                Options = () => new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = () => "Turn back.",  OnChosen = () => 
                    {
                        setFlag("cabinet_table");
                        LoadSituation("cabinet_start");
                    }, Row = 1 },
                    new Option { Text = () => "Keep going.",  OnChosen = () => 
                    {
                        setFlag("cabinet_table");
                        if(hasFlag("cabinet_door_open"))
                        {
                            LoadSituation("cabinet_door_outer_open");
                        }
                        else
                        {
                            LoadSituation("cabinet_door_outer");
                        }
                    }, Row = 1 },
                    new Option { Text = () => hasFlag("cabinet_table") ? "Check table." : "Check what you hit.",  OnChosen = () => 
                    {
                        setFlag("cabinet_table");
                        // load minigame
                        //LoadSituation("");
                    }, Row = 2 },
                }
            },

            ["cabinet_window"] = new Situation
            {
                // Options for when you 
                Description = () => hasFlag("cabinet_window") ? "You stop before the open window inside of the cabinet." : "Your fingertips find the wall. It is cold and smooth — painted plaster. You move slowly along it letting your left hand guide you.\n{INSERT TEXT ZA OKNO}",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Turn back.", OnChosen = () => 
                    {
                        setFlag("cabinet_window");
                        LoadSituation("cabinet_start");
                    }, Row = 1 },
                    new Option { Text = () => "Keep going.", OnChosen = () => 
                    {
                        setFlag("cabinet_window");
                        LoadSituation("cabinet_door_outer");
                    }, Row = 1},
                    new Option { Text = () => "Jump through the open window.", OnChosen = () => 
                    {
                        setFlag("cabinet_window");
                        LoadSituation("cabinet_window_interact");
                    }, Row = 2},
                    // option to go through the middle if we decide you have that option
                    //new Option { Text = () => "",      OnChosen = () => LoadSituation("carpet-"), Row = 2}
                }
            },

            ["cabinet_window_interact"] = new Situation
            {
                // Game over screen 
                Description = () => "As you feel the cold air on your skin, it reminds you of freedom. Not wanting to be bound to an unknown place you jump. ",
                // Load game over screen and return to menu
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Start over.", OnChosen = () => LoadSituation("start"), Row = 1 },
                    new Option { Text = () => "Return to menu.", OnChosen = () => Debug.Log("TODO: load main menu"), Row = 1 },
                }
            },

            ["cabinet_door_outer"] = new Situation
            {
                // Options for the cabinet door inside
                Description = () =>
                {
                    if (hasFlag("cabinet_door_outer") && hasFlag("cabinet_door_knob"))
                    {
                        return "You feel the wooden frame of the door, but this time you have a way to open it!";    
                    }
                    else if (hasFlag("cabinet_door_outer") && !hasFlag("cabinet_door_knob"))
                    {
                        return "You feel the wooden frame of the door. But you still have no way of opening it.";
                    }
                    else if (!hasFlag("cabinet_door_outer") && hasFlag("cabinet_door_knob"))
                    {
                        return "You feel the wooden frame of a door.\n";
                    }
                    else
                    {
                        return "You feel the wooden frame of a door.\nHuzzah!\nA way out of this awfull room!\nAs you feel around disaster stikes!\nThe door is missing a doorknob.";
                    }
                },
                Options = () =>
                { 
                    var list = new List<Option>
                    {
                        new Option { Text = () => "Turn around and explore the right wall.", OnChosen = () => 
                        {
                            setFlag("cabinet_door_outer");
                            LoadSituation("cabinet_window");
                        }, Row = 1},
                        new Option { Text = () => "Turn around and explore the left wall.", OnChosen = () =>
                        {
                            setFlag("cabinet_door_outer");
                            LoadSituation("cabinet_table");
                        },Row = 1 },                        
                        new Option { Text = () => "Turn around and walk straight through the cabinet.", OnChosen = () =>
                        {
                            setFlag("cabinet_door_outer");
                            LoadSituation("cabinet_carpet");
                        },Row = 2 }
                    };

                    if (hasFlag("cabinet_door_knob")) 
                    { 
                        list.Add
                        (
                            new Option { Text = () => "Attach door knob and open the door.", OnChosen = () => 
                            { 
                                setFlag("cabinet_door_open");
                                LoadSituation("cabinet_door_outer_open");
                            },Row = 3}
                        );
                    }
                    return list;
                }
            },

            ["cabinet_door_outer_open"] = new Situation
            {
                Description = () => hasFlag("cabinet_door_open") ? "You stand before the exit of the cabinet." : "The door opens!\nCould this be it?\nHas the exit been reached?",
                Options = () => 
                {
                    var list = new List<Option> 
                    {
                        new Option { Text = () => hasFlag("cabinet_door_outer_open") ? "Step into the hallway." : "Step in to the unknown.", OnChosen = () =>
                        {
                            setFlag("cabinet_door_outer_open");
                            if (hasFlag("cabinet_door_outer_open"))
                            {    
                                LoadSituation("hallway_cabinet");
                            }
                            else
                            {
                            LoadSituation("hallway_opposite_cabinet");
                            }
                        }, Row = 2}
                    };
                    if (hasFlag("cabinet_door_outer_open"))
                    {
                        list.Add
                        (        
                            new Option { Text = () => "Turn around and explore the left wall of the cabinet.", OnChosen = () =>
                            {
                                LoadSituation("cabinet_table");
                            }, Row = 1}
                        );
                        list.Add
                        (
                            new Option { Text = () => "Turn around and explore the right wall of the cabinet.", OnChosen = () =>
                            {
                                LoadSituation("cabinet_window");
                            }, Row = 1}
                        );
                        list.Add
                        (
                            new Option { Text = () => "Turn around and walk straight through the cabinet.", OnChosen = () =>
                            {
                                LoadSituation("cabinet_carpet");
                            }, Row = 2}
                        );
                    }
                    return list;
                }
            },

            

            


            // ── Bottom half ─────────────────────────────────────

            ["study_door_inner"] = new Situation 
            {
                Description = () => hasFlag("study_start") ? "The air smells like books. You must be in the study room again." : "You feel the air has changed a bit. You are surrounded by a smell of books.",
                Options = () => new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = () => "Trace your hand along the left wall.",  OnChosen = () => 
                    {
                        setFlag("study_door_inner");
                        LoadSituation("study_scale");
                    }, Row = 1},

                    new Option { Text = () => "Trace your hand along the right wall.", OnChosen = () => 
                    {
                        setFlag("study_door_inner");
                        LoadSituation("study_skeleton");
                    }, Row = 1 },

                    new Option { Text = () => "Go back.", OnChosen = () => 
                    {
                        setFlag("study_door_inner");
                        LoadSituation("TODO: study door outer");
                    }, Row = 2 },
                }
            },

            ["study_skeleton"] = new Situation 
            {
                Description = () => hasFlag("study_skeleton") ? "You stop at the skeleton." : "You hit something!",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Turn back.",  OnChosen = () => 
                    {
                        setFlag("study_skeleton");
                        LoadSituation("study_door_inner");
                    }, Row = 1 },

                    new Option { Text = () => hasFlag("study_skeleton") ? "Check skeleton." : "Check what you hit.",  OnChosen = () => 
                    {
                        setFlag("study_skeleton");
                        // load minigame
                        //LoadSituation("");
                    }, Row = 1 },

                    new Option { Text = () => "Keep tracing forward.",  OnChosen = () => 
                    {
                        setFlag("study_skeleton");
                        LoadSituation("study_scale");
                    }, Row = 2 },
                }
            },

            ["dot_game_complete"] = new Situation
            {
                Description = () =>"Your fingers trace the connected dots, forming a pattern. A shape emerges — a doorway, perhaps, or a way forward. The pattern felt complete under your touch.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Continue exploring.", OnChosen = () => LoadSituation("cabinet_door_inside"), Row = 1 },
                }     
            },

            ["study_scale"] = new Situation
            {
                Description = () => hasFlag("study_scale") ? "You stop at the table." : "You hit something!",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Turn back.",  OnChosen = () => 
                    {
                        setFlag("study_scale");
                        LoadSituation("study_door_inner");
                    }, Row = 1 },

                    new Option { Text = () => hasFlag("study_scale") ? "Check table." : "Check what you hit.",  OnChosen = () => 
                    {
                        setFlag("study_scale");
                        // load minigame
                        //LoadSituation("");
                    }, Row = 1 },

                    new Option { Text = () => "Keep tracing forward.",  OnChosen = () => 
                    {
                        setFlag("study_scale");
                        LoadSituation("study_door_inner");
                    }, Row = 2 },
                }
            },
            
            ["bathroom_door_inner"] = new Situation
            {
                Description = () => hasFlag("bathroom_door_inner") ? "You smell fragrance in the air. You are in the bathroom again." : "The floor feels slippery and the room has a slight smell of fragrance.\nThis must be the bathroom.",
                Options = () => new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = () => "Trace your hand along the left wall.",  OnChosen = () => 
                    {
                        setFlag("bathroom_door_inner");
                        LoadSituation("bathroom_sink");
                    }, Row = 1},

                    new Option { Text = () => "Trace your hand along the right wall.", OnChosen = () => 
                    {
                        setFlag("bathroom_door_inner");
                        LoadSituation("bathroom_bath");
                    }, Row = 1 },

                    new Option { Text = () => "Go back.", OnChosen = () => 
                    {
                        setFlag("bathroom_door_inner");
                        LoadSituation("TODO: bathroom door outer");
                    }, Row = 2 },
                }
            },

            ["bathroom_sink"] = new Situation
            {
                Description = () => hasFlag("bathroom_sink") ? "You hit the sink again." : "You hit something!",
                Options = () => new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = () => "Trace foward.",  OnChosen = () => 
                    {
                        setFlag("bathroom_sink");
                        LoadSituation("bathroom_toilet");
                    }, Row = 1},

                    new Option { Text = () => hasFlag("bathroom_sink") ? "Check sink." : "Check what you hit.",  OnChosen = () => 
                    {
                        setFlag("bathroom_sink");
                        // load minigame
                        //LoadSituation("");
                    }, Row = 1 },

                    new Option { Text = () => "Go back.",  OnChosen = () => 
                    {
                        setFlag("bathroom_sink");
                        LoadSituation("bathroom_toilet");
                    }, Row = 2 },
                }
            },

            ["bathroom_bath"] = new Situation
            {
                Description = () => hasFlag("bathroom_bath") ? "You hit the bathtub again." : "You hit something with your foot! You check and it seems you found a bathtub.",
                Options = () => new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = () => "Trace foward.",  OnChosen = () => 
                    {
                        setFlag("bathroom_bath");
                        LoadSituation("bathroom_toilet");
                    }, Row = 1},

                    new Option { Text = () => "Go back.",  OnChosen = () => 
                    {
                        setFlag("bathroom_bath");
                        LoadSituation("bathroom_door_inner");
                    }, Row = 1 },
                }
            },

            ["bathroom_toilet"] = new Situation
            {
                Description = () => hasFlag("bathroom_toilet") ? "You hit the toilet again. Ughhh..." : "You hit something! You check and it feels like a toilet. Yuck!",
                Options = () => new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = () => "Trace foward.",  OnChosen = () => 
                    {
                        setFlag("bathroom_toilet");
                        LoadSituation("bathroom_sink");
                    }, Row = 1},

                    new Option { Text = () => "Go back.",  OnChosen = () => 
                    {
                        setFlag("bathroom_toilet");
                        LoadSituation("bathroom_bath");
                    }, Row = 1 },
                }
            },

            ["bedroom_door_inner"] = new Situation
            {
                Description = () => hasFlag("bedroom_door_inner") ? "Bedroom again." : "This room feels warmer.",
                Options = () => new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = () => "Trace your hand along the left wall.",  OnChosen = () => 
                    {
                        setFlag("bedroom_door_inner");
                        LoadSituation("bedroom_closet");
                    }, Row = 1},

                    new Option { Text = () => "Trace your hand along the right wall.", OnChosen = () => 
                    {
                        setFlag("bedroom_door_inner");
                        LoadSituation("bedroom_nightstand_2");
                    }, Row = 1 },

                    new Option { Text = () => "Go back.", OnChosen = () => 
                    {
                        setFlag("bedroom_door_inner");
                        LoadSituation("TODO: bedroom door outer");
                    }, Row = 2 },
                }
            },

            ["bedroom_closet"] = new Situation
            {
                Description = () => hasFlag("bedroom_closet") ? "You hit the closet again." : "You hit something!",
                Options = () => new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = () => "Trace foward.",  OnChosen = () => 
                    {
                        setFlag("bedroom_closet");
                        LoadSituation("bedroom_nightstand_1");
                    }, Row = 1},

                    new Option { Text = () => "Go back.",  OnChosen = () => 
                    {
                        setFlag("bedroom_closet");
                        LoadSituation("bedroom_door_inner");
                    }, Row = 1 },
                }
            },

            ["bedroom_nightstand_1"] = new Situation
            {
                Description = () => hasFlag("bedroom_nightstand_1") ? "You hit the nightstand again." : "You hit something! It feels like a small drawer.",
                Options = () => new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = () => "Trace foward.",  OnChosen = () => 
                    {
                        setFlag("bedroom_nightstand_1");
                        LoadSituation("bedroom_bed");
                    }, Row = 1},

                    new Option { Text = () => "Go back.",  OnChosen = () => 
                    {
                        setFlag("bedroom_nightstand_1");
                        LoadSituation("bedroom_closet");
                    }, Row = 1 },
                }
            },

            ["bedroom_nightstand_2"] = new Situation
            {
                Description = () => hasFlag("bedroom_nightstand_2") ? "You hit the nightstand again." : "You hit something! It feels like a small drawer.",
                Options = () => new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = () => "Trace foward.",  OnChosen = () => 
                    {
                        setFlag("bedroom_nightstand_2");
                        LoadSituation("bedroom_bed");
                    }, Row = 1},

                    new Option { Text = () => "Go back.",  OnChosen = () => 
                    {
                        setFlag("bedroom_nightstand_2");
                        LoadSituation("bedroom_door_inner");
                    }, Row = 1 },
                }
            },

            ["bedroom_bed"] = new Situation
            {
                Description = () => hasFlag("bedroom_bed") ? "You hit the bed again." : "You hit something!",
                Options = () => new List<Option>
                {
                    // the starting options you are met with when you wake up in the cabinet
                    // trace left wall, trace right wall and go straight
                    new Option { Text = () => "Trace your hand to the left.",  OnChosen = () => 
                    {
                        setFlag("bedroom_bed");
                        LoadSituation("bedroom_nightstand_1");
                    }, Row = 1},

                    new Option { Text = () => "Trace your hand to the right.", OnChosen = () => 
                    {
                        setFlag("bedroom_bed");
                        LoadSituation("bedroom_nightstand_2");
                    }, Row = 1 },

                    new Option { Text = () => hasFlag("bedroom_bed") ? "Check the bed." : "Check what you hit.",  OnChosen = () => 
                    {
                        setFlag("bedroom_bed");
                        // load minigame
                        //LoadSituation("");
                    }, Row = 2 },

                    new Option { Text = () => "Go back.",  OnChosen = () => 
                    {
                        setFlag("bedroom_bed");
                        LoadSituation("bedroom_door_inner");
                    }, Row = 2 },
                }
            },

        };
    }

    private void StartDotGame()
    {
        if (DotConnectingMinigame.Instance == null)
        {
            GameObject minigameGO = new GameObject("DotConnectingMinigame");
            minigameGO.AddComponent<DotConnectingMinigame>();
        }
        
        DotConnectingMinigame.Instance.StartGame(() => OnDotGameComplete());
    }

    public void OnDotGameComplete()
    {
        Debug.Log("Dot game completed!");
        UIManager.Instance.ShowGameplay();

        LoadSituation("dot_game_complete");
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

        // Type out each option button one by one
        foreach (var option in situation.Options())
        {
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

            // Small pause between options
            yield return new WaitForSeconds(0.15f);
        }
    }
}
