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
                Description = () => "You wake up on a cold floor. The air is still and unfamiliar. You have no idea where you are.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Get up.", OnChosen = () => {
                        LoadSituation("cabinet_start");
                        try { SoundManager.Instance.PlaySound("door_open"); }
                        catch (Exception e) { Debug.Log("Sound not found. " + e.Message); }
                    }, Row = 1 }
                }
            },

            // ── Cabinet ──────────────────────────────────────────────────────────────────

            ["cabinet_door"] = new Situation
            {
                Description = () => hasFlag("has_doorknob")
                    ? (hasFlag("cabinet_door_visited")
                        ? "Back at the door. The spindle is still bare and waiting. This time, so is the doorknob in your hand."
                        : "Your fingers find a doorframe. The wood is cold and smooth. Where a handle should be there is only a bare metal spindle — and your other hand happens to be holding something that feels exactly like a match for it.")
                    : (hasFlag("cabinet_door_visited")
                        ? "The door again. The spindle is still bare. You are still not going anywhere."
                        : "Your fingers find a doorframe. The wood is cold and smooth. Where a handle should be there is only a bare metal spindle. You try to turn it. Nothing moves."),
                Options = () =>
                {
                    var opts = new List<Option>
                    {
                        new Option { Text = () => "Go north hugging the wall.", OnChosen = () => { setFlag("cabinet_door_visited"); LoadSituation("cabinet_window"); }, Row = 1 },
                        new Option { Text = () => "Go south hugging the wall.", OnChosen = () => { setFlag("cabinet_door_visited"); LoadSituation("cabinet_table"); }, Row = 1 },
                    };
                    if (hasFlag("has_doorknob"))
                        opts.Add(new Option { Text = () => "Fit the doorknob and step through.", OnChosen = () => { removeFlag("has_doorknob"); LoadSituation("hallway_cabinet_door"); }, Row = 2 });
                    else
                        opts.Add(new Option { Text = () => "Try the door again.", OnChosen = () => { setFlag("cabinet_door_visited"); LoadSituation("cabinet_door"); }, Row = 2 });
                    return opts;
                },
            },

            ["cabinet_start"] = new Situation
            {
                Description = () => "You get up, scared but determined to escape this place you are now lost in.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("cabinet_window"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation("cabinet_table"), Row = 1 },
                    new Option { Text = () => "Go east, straight into the unknown.", OnChosen = () => LoadSituation("cabinet_carpet"), Row = 2 },
                }
            },

            ["cabinet_carpet"] = new Situation
            {
                Description = () => hasFlag("cabinet_carpet_checked")
                    ? "After a thorough investigation, you find a key hidden underneath the carpet."
                    : (hasFlag("cabinet_carpet_visited")
                        ? "You approach the spot where you absolutely did not embarrass yourself earlier."
                        : "Your foot catches on something soft and you stumble forward like a newborn deer on a freshly waxed floor. Graceful. Very graceful."),
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east.", OnChosen = () => { setFlag("cabinet_carpet_visited"); LoadSituation("cabinet_door"); }, Row = 1 },
                    new Option { Text = () => "Go west.", OnChosen = () => { setFlag("cabinet_carpet_visited"); LoadSituation("cabinet_start"); }, Row = 1 },
                    new Option { Text = () => hasFlag("cabinet_carpet_checked") ? "Check the carpet." : "Check what you tripped over.", OnChosen = () => {
                        setFlag("cabinet_carpet_checked");
                        LoadSituation("cabinet_carpet");
                    }, Row = 2 },
                }
            },

            ["cabinet_table"] = new Situation
            {
                Description = () => hasFlag("cabinet_table_drawer_unlocked")
                    ? (hasFlag("cabinet_door_visited")
                        ? "The drawer is open. Inside, your fingers close around a small metal knob — the exact kind missing from the cabinet door. Without it, you were never getting out of here."
                        : "The drawer is open. Inside, your fingers close around a small metal knob. It feels like a doorknob. You're not sure which door needs it, but you pocket it anyway.")
                    : hasFlag("cabinet_table_checked")
                        ? "After a thorough investigation you find a cassette player on top and a locked drawer below. You'll need a key."
                        : (hasFlag("cabinet_table_visited")
                            ? "You return to the scene of the hip crime."
                            : "Your hip connects with something solid and completely unapologetic. Whatever this is, it is not sorry. Not even a little."),
                Options = () =>
                {
                    var opts = new List<Option>
                    {
                        new Option { Text = () => "Go east along the wall.", OnChosen = () => { setFlag("cabinet_table_visited"); LoadSituation("cabinet_door"); }, Row = 1 },
                        new Option { Text = () => "Go west along the wall.", OnChosen = () => { setFlag("cabinet_table_visited"); LoadSituation("cabinet_start"); }, Row = 1 },
                        new Option { Text = () => hasFlag("cabinet_table_checked") ? "Check the table." : "Check what you hit.", OnChosen = () => { setFlag("cabinet_table_checked"); LoadSituation("cabinet_table"); }, Row = 2 },
                    };
                    if (hasFlag("cabinet_table_checked") && hasFlag("cabinet_carpet_checked") && !hasFlag("cabinet_table_drawer_unlocked"))
                        opts.Add(new Option { Text = () => "Use the key on the drawer.", OnChosen = () => { setFlag("cabinet_table_drawer_unlocked"); setFlag("has_doorknob"); LoadSituation("cabinet_table"); }, Row = 3 });
                    return opts;
                },
            },

            ["cabinet_window"] = new Situation
            {
                Description = () => "Cool air brushes your face. Your fingers find a windowsill. The window is open — or broken — either way, outside is out there, taunting you.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("cabinet_door"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("cabinet_start"), Row = 1 },
                    //new Option { Text = () => "Jump through the open window.", OnChosen = () => LoadSituation("cabinet_window_check"), Row = 2 },
                }
            },

            // ── Hallway ───────────────────────────────────────────────────────────────────

            ["hallway_cabinet_door"] = new Situation
            {
                Description = () => "You step into a corridor. The air is different here — wider, emptier. You trace the wall and find the door you came through behind you. The hallway stretches in both directions. Someone lived here. Maybe still does. You prefer not to think about that.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("livingroom_gramophone"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation("hallway_closet_door"), Row = 1 },
                    new Option { Text = () => "Enter cabinet.", OnChosen = () => LoadSituation("cabinet_door"), Row = 2 },
                }
            },

            ["hallway_o_cabinet_door"] = new Situation
            {
                Description = () => "You reach the far wall. It is disappointingly just a wall. No door, no window, no secret passage. Just wall. Classic wall behavior.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("livingroom_casete_table"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation("hallway_bathroom_door"), Row = 1 },
                    new Option { Text = () => "Go east, back to the cabinet.", OnChosen = () => LoadSituation("hallway_cabinet_door"), Row = 2 },
                }
            },

            ["hallway_closet_door"] = new Situation
            {
                Description = () => "Your hand brushes over a door. Smaller than the last one. The kind of door that quietly suggests 'coats in here' — or possibly 'things that wear coats.'",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("hallway_cabinet_door"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation("hallway_study_door"), Row = 1 },
                    //new Option { Text = () => "Check the closet.", OnChosen = () => LoadSituation("closet_check"), Row = 2 },
                }
            },

            ["hallway_o_closet_door"] = new Situation
            {
                Description = () => "The south wall. Flat, featureless, and profoundly unhelpful. You appreciate its commitment to mediocrity.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("livingroom_casete_table"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation("hallway_bathroom_door"), Row = 1 },
                    new Option { Text = () => "Go east, back to the closet.", OnChosen = () => LoadSituation("hallway_closet_door"), Row = 2 },
                }
            },

            ["hallway_study_door"] = new Situation
            {
                Description = () => "Another door. This one feels heavier, older. The handle is brass and slightly sticky. You choose not to investigate why.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("hallway_closet_door"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation("hallway_creaking_board_s"), Row = 1 },
                    new Option { Text = () => "Enter study.", OnChosen = () => LoadSituation("study_door"), Row = 2 },
                }
            },

            ["hallway_o_study_door"] = new Situation
            {
                Description = () => "The north wall here offers nothing but plaster and existential dread. You've touched more interesting walls today, and that is saying something.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("livingroom_casete_table"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation("hallway_bathroom_door"), Row = 1 },
                    new Option { Text = () => "Go east, back to the study.", OnChosen = () => LoadSituation("hallway_study_door"), Row = 2 },
                }
            },

            ["hallway_bathroom_door"] = new Situation
            {
                Description = () => "You detect it before you touch it — the faint smell of damp tile and old soap. A bathroom door. At least someone in this house had priorities.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("hallway_creaking_board_n"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("livingroom_casete_table"), Row = 1 },
                    new Option { Text = () => "Enter bathroom.", OnChosen = () => LoadSituation("bathroom_door"), Row = 2 },
                }
            },

            ["hallway_o_bathroom_door"] = new Situation
            {
                Description = () => "The wall across from the bathroom. You can still smell the soap from here. It's almost comforting. Almost.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("hallway_creaking_board_s"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("hallway_study_door"), Row = 1 },
                    new Option { Text = () => "Go north, back to the bathroom.", OnChosen = () => LoadSituation("hallway_bathroom_door"), Row = 2 },
                }
            },

            ["hallway_creaking_board_n"] = new Situation
            {
                Description = () => "You step and — CREEEAK. The floorboard beneath you announces your presence to anyone within a quarter mile. Stealth: zero.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("hallway_kitchen_door"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("hallway_bathroom_door"), Row = 1 },
                    // new Option { Text = () => "Check under the board.", OnChosen = () => LoadSituation("creaking_board_check"), Row = 2 },
                }
            },

            ["hallway_creaking_board_s"] = new Situation
            {
                Description = () => "CREEEAK. The floorboard groans under your foot. You lift it. It groans again on the way up. The house is definitely doing this on purpose.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("hallway_bedroom_door"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("hallway_study_door"), Row = 1 },
                    // new Option { Text = () => "Check under the board.", OnChosen = () => LoadSituation("creaking_board_check"), Row = 2 },
                }
            },

            ["hallway_bedroom_door"] = new Situation
            {
                Description = () => "A door, and behind it the unmistakable smell of old fabric, dust, and the ghost of a perfume that has been slowly giving up for years. A bedroom, almost certainly.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("exit"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("hallway_creaking_board_s"), Row = 1 },
                    new Option { Text = () => "Enter bedroom.", OnChosen = () => LoadSituation("bedroom_door"), Row = 2 },
                }
            },

            ["hallway_o_bedroom_door"] = new Situation
            {
                Description = () => "North wall. Nothing here except the echo of your own breathing, which is doing absolutely nothing to calm you down.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("hallway_bedroom_door"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("hallway_creaking_board_s"), Row = 1 },
                    new Option { Text = () => "Go north, back to the bedroom.", OnChosen = () => LoadSituation("hallway_bedroom_door"), Row = 2 },
                }
            },

            ["hallway_kitchen_door"] = new Situation
            {
                Description = () => "A door, and through it the distant ghost of cooked food. Your stomach, despite absolutely everything, growls. Priorities.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("exit"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("hallway_creaking_board_n"), Row = 1 },
                    new Option { Text = () => "Enter kitchen.", OnChosen = () => LoadSituation("kitchen_door"), Row = 2 },
                }
            },

            ["hallway_o_kitchen_door"] = new Situation
            {
                Description = () => "Another wall. You are getting very good at walls. You could write a book. 'Walls I Have Touched: A Memoir.' It would be a short book. But an honest one.",
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
                Description = () => "The study smells like old paper and long-abandoned ambition. Someone spent a great deal of time in here. You can feel that in the air — the weight of it.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => LoadSituation("study_skeleton"), Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "Exit the study.", OnChosen = () => LoadSituation("hallway_study_door"), Row = 2 },
                }
            },

            ["study_skeleton"] = new Situation
            {
                Description = () => (hasFlag("study_skeleton_visited")
                    ? "You ease back toward the thing that rattled. It hangs there, silently judging your life choices."
                    : "Your hand sweeps forward and catches something that sways gently and makes a sound that can only be described as rattling. Several things rattle. Several bony, articulated things. You freeze. You breathe. The rattling stops.\n\nOh no.")
                    + (hasFlag("study_skeleton_checked") ? "\nDo you need something here?" : ""),
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => { setFlag("study_skeleton_visited"); LoadSituation("study_door"); }, Row = 1 },
                    new Option { Text = () => "Go south along the wall.", OnChosen = () => { setFlag("study_skeleton_visited"); LoadSituation("study_table"); }, Row = 1 },
                    new Option { Text = () => hasFlag("study_skeleton_checked") ? "Check the skeleton." : "Check what you hit.", OnChosen = () => { setFlag("study_skeleton_checked"); LoadSituation("study_skeleton"); }, Row = 2 },
                }
            },

            ["study_table"] = new Situation
            {
                Description = () => (hasFlag("study_table_visited")
                    ? "You return to the table. The objects have resettled. The shin is still bruised."
                    : "You walk into something at shin height and the objects on top shift and clatter loudly. You stand very still, waiting to see if anyone heard that. No one responds. Which might be worse.")
                    + (hasFlag("study_table_checked") ? "\nDo you need something here?" : ""),
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => { setFlag("study_table_visited"); LoadSituation("study_door"); }, Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => { setFlag("study_table_visited"); LoadSituation("study_skeleton"); }, Row = 1 },
                    new Option { Text = () => hasFlag("study_table_checked") ? "Check the table." : "Check what you hit.", OnChosen = () => { setFlag("study_table_checked"); LoadSituation("study_table"); }, Row = 2 },
                }
            },

            // ── Bathroom ──────────────────────────────────────────────────────────────────

            ["bathroom_door"] = new Situation
            {
                Description = () => "Tiles underfoot, cool and slightly gritty. The echo here is different — tighter, wetter. It smells aggressively clean, which feels suspicious given everything else about this place.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => LoadSituation("bathroom_bath"), Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => LoadSituation("bathroom_sink"), Row = 1 },
                    new Option { Text = () => "Exit the bathroom.", OnChosen = () => LoadSituation("hallway_bathroom_door"), Row = 2 },
                }
            },

            ["bathroom_bath"] = new Situation
            {
                Description = () => (hasFlag("bathroom_bath_visited")
                    ? "You approach the bathtub with appropriate caution this time. It sits there, completely smug."
                    : "You step forward and your knee finds the edge of the bathtub with surgical precision. The bathtub does not apologize. You bite your lip.")
                    + (hasFlag("bathroom_bath_checked") ? "\nDo you need something here?" : ""),
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go north hugging the wall.", OnChosen = () => { setFlag("bathroom_bath_visited"); LoadSituation("bathroom_toilet"); }, Row = 1 },
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => { setFlag("bathroom_bath_visited"); LoadSituation("bathroom_door"); }, Row = 1 },
                    new Option { Text = () => hasFlag("bathroom_bath_checked") ? "Check the bathtub." : "Check what you hit.", OnChosen = () => { setFlag("bathroom_bath_checked"); LoadSituation("bathroom_bath"); }, Row = 2 },
                }
            },

            ["bathroom_toilet"] = new Situation
            {
                Description = () => (hasFlag("bathroom_toilet_visited")
                    ? "The toilet. It continues to exist, cold and porcelain and utterly unbothered by your situation."
                    : "Your shin catches something cold and ceramic at exactly the wrong height. It wobbles very slightly. The wobble is somehow more embarrassing than the impact.")
                    + (hasFlag("bathroom_toilet_checked") ? "\nDo you need something here?" : ""),
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go west along the wall.", OnChosen = () => { setFlag("bathroom_toilet_visited"); LoadSituation("bathroom_sink"); }, Row = 1 },
                    new Option { Text = () => "Go south hugging the wall.", OnChosen = () => { setFlag("bathroom_toilet_visited"); LoadSituation("bathroom_bath"); }, Row = 1 },
                    new Option { Text = () => hasFlag("bathroom_toilet_checked") ? "Check the toilet." : "Check what you hit.", OnChosen = () => { setFlag("bathroom_toilet_checked"); LoadSituation("bathroom_toilet"); }, Row = 2 },
                }
            },

            ["bathroom_sink"] = new Situation
            {
                Description = () => (hasFlag("bathroom_sink_visited")
                    ? "Back at the sink. The tap is still dripping. Still judging."
                    : "Your hands find cold pipes and then a basin edge that stops your forward momentum with a sharp jab to the stomach. The tap drips once, as if laughing.")
                    + (hasFlag("bathroom_sink_checked") ? "\nDo you need something here?" : ""),
                Options = () => new List<Option>
                {
                    new Option { Text = () => "Go east along the wall.", OnChosen = () => { setFlag("bathroom_sink_visited"); LoadSituation("bathroom_toilet"); }, Row = 1 },
                    new Option { Text = () => "Go south along the wall.", OnChosen = () => { setFlag("bathroom_sink_visited"); LoadSituation("bathroom_door"); }, Row = 1 },
                    new Option { Text = () => hasFlag("bathroom_sink_checked") ? "Check the sink." : "Check what you hit.", OnChosen = () => { setFlag("bathroom_sink_checked"); LoadSituation("bathroom_sink"); }, Row = 2 },
                }
            },

            // ── Bedroom ───────────────────────────────────────────────────────────────────

            ["bedroom_door"] = new Situation
            {
                Description = () => "The bedroom. The air is stale in the specific way of rooms that haven't been opened in a while. Thin carpet. The sense of furniture arranged in the dark by someone who knew exactly where everything was.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["bedroom_closet"] = new Situation
            {
                Description = () => "Your fingers trace a long flat surface — a sliding door. Behind it: fabric. Lots of fabric. Coats, shirts, the soft geography of someone's wardrobe. Nothing obviously dangerous. Probably.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["bedroom_nightstand_1"] = new Situation
            {
                Description = () => "A small table beside where a bed should be. Your hands find it immediately — because your hip finds it first. A lamp. A drawer. The texture of an old coaster.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["bedroom_nightstand_2"] = new Situation
            {
                Description = () => "The other side. Another nightstand, the mirror image of the first. You reach out and accidentally knock something small. It rolls. You listen to it roll for longer than you'd like.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["bedroom_bed"] = new Situation
            {
                Description = () => "Your knees find the bed frame and you topple forward onto the mattress. It is softer than everything else in this nightmare. For one brief moment you consider just staying here forever.",
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
                Description = () => "Your hands find a large curved horn and you immediately know what this is. A gramophone. Someone had taste. Someone also had a strange life. The two are not mutually exclusive.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["livingroom_casete_table"] = new Situation
            {
                Description = () => "A low table covered in small rectangles. Your fingers sort through them — tapes. Dozens of tapes. Someone was very serious about their music collection. Or their surveillance operation. Hard to tell.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["livingroom_painting_safe"] = new Situation
            {
                Description = () => "The wall here has a frame. Large, ornate, the kind of frame that says 'I am hiding something important behind me.' You press behind it and feel cold metal. Of course. Of course there's a safe behind the painting.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["livingroom_fireplace"] = new Situation
            {
                Description = () => "Stone. Ash. The faint residual warmth of something that burned a long time ago. The fireplace takes up most of the wall. You carefully keep your hands well away from the interior.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["livingroom_glass_table"] = new Situation
            {
                Description = () => "You walk into a table that offers absolutely no warning and makes a loud glass-on-glass sound as its contents rattle dramatically. You are becoming an expert in furniture-based combat.",
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
                Description = () => "The kitchen. The air changes here — sharper, with the memory of spices and something that might have been onions. Linoleum underfoot. The kitchen is large and full of edges that are looking forward to meeting you.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["kitchen_drawer"] = new Situation
            {
                Description = () => "You find the counter and then the row of drawers below it. You pull one open. It rattles with the sound of various implements. This is either the junk drawer or a very organized person's knife collection.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["kitchen_counter"] = new Situation
            {
                Description = () => "The counter is long and cold and slightly sticky in one corner. You find a chopping board. An empty glass. The stub of what was once a candle. Someone cooked here. Once.",
                Options = () => new List<Option>
                {
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 1 },
                    new Option { Text = () => "", OnChosen = () => LoadSituation(""), Row = 2 },
                }
            },

            ["kitchen_fridge"] = new Situation
            {
                Description = () => "The refrigerator hums at you in a friendly way that feels deeply out of place. You open it. Cold air spills out. The contents are a mystery of varying textures and temperatures. You close it. This is not the time.",
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
                Description = () => "Your hand finds a door unlike the others — heavier, sealed with weatherstripping and the definite weight of a deadbolt. A front door. You press your palm flat against it. Outside is on the other side of this. Outside.",
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
        if (id == "start") {
            if (_currentSituation != "start"){
                SoundManager.Instance.PlaySoundLoop("fireplace-1", 0.5f);
            } else {
                SoundManager.Instance.StopLoop();
            }
        }

        _currentSituation = id;

        var ui = UIManager.Instance;

        Debug.Log("LoadSituation called: " + id);
        StopAllCoroutines();
        StartCoroutine(TypeSituation(situation, ui));

        ui.ShowGameplay();

        // Read situation description with TTS
        try
        {
            string description = situation.Description();
            if (!string.IsNullOrEmpty(description))
            {
                TextToSpeechManager.Instance.SpeakText(description);
            }
        }
        catch (Exception e)
        {
            Debug.Log("TTS failed: " + e.Message);
        }
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
