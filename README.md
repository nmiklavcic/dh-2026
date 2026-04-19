# dh-2026 — Game Design Document

## Concept

A 2D escape-room style game about a blind protagonist who wakes up in an unknown house and must navigate using his senses, avoid obstacles, and solve a series of interlocked puzzles in order to escape. Minimal story — the situation speaks for itself.

---

## Team

4 people (collaborative project).

---

## UI / Visual Style

- **Entirely black screen** during normal gameplay — no environment visuals
- Text description of the current situation shown on screen
- **Clickable text options** — player makes choices by clicking them
- The only overlays are the **Memory Screen** (map + journal), accessed via `M`

---

## Core Navigation

The player navigates by choosing from clickable text options. Number of options varies by situation, most rooms will have 3.

**Example — first room:**
- Trace hand along left wall
- Stand up and walk forward
- Trace hand along right wall

---

## Obstacle / Collision System

When the player walks into an object the game plays an audible impact sound (e.g. a "thunk") and the character reacts (e.g. *"Hmm, I wonder what I hit."*).

The player is given three options:
- **Check** — triggers the Feel Around minigame
- **Go back** — retreat to previous position/state
- **Continue past** — skip the object and keep moving

**Continue past behaviour:**
- A placeholder memory entry is created — the map marks that *something* is at that location, but it is unidentified (no outline, no details)
- The next time the player reaches that spot they are prompted to check it out rather than hitting it as a surprise again

---

<!---## Feel Around Minigame

Triggered when the player chooses **Check** on an unknown object.

**Flow:**
1. Rustling/ambient sounds play in the background
2. Screen goes completely black
3. Text at the top guides the player (e.g. *"Use your hands to feel what's in front of you"*)
4. The player uses the mouse as the character's hand:
   - **Hold left mouse button + drag** = feel around
   - Wherever the mouse moves while held, a portion of the object is revealed
5. Underneath the black foreground is a black image with **white outlines** of the object (scratch-card style reveal)
6. Once a reveal threshold is reached the object is identified (threshold to be tweaked during development)

**After identification:**
- The player is presented with **interaction options specific to that object** (grab items, combine things, solve puzzles — designed per object)
- Press `Q` to exit the interaction and return to the black screen with three options:
  - Continue along the path
  - Go back
  - Re-examine the object (return to Feel Around / interaction)

**Memory saving:**
- The revealed white-outline drawing is saved to the Memory Journal as an image
- The object's location on the map is also saved so the player can cross-reference

---

## Memory Screen (M key)

A full-screen overlay split into two sections:

### Left — Memory Map
- Simple line drawing that builds up as the player traces walls
- A wall segment is revealed from the player's position up to the first **door** or **obstacle hit**, then stops
- Walls never traced remain blank
- **Doors** are marked as a gap in the wall line:
  - **Unlocked** — standard gap
  - **Locked** — marked differently (visual style TBD) with a journal entry noting what is required to open it
- Locked doors act as map-based todo items

### Right — Memory Journal
- A list of text entries
- Clicking an entry expands it into its visual (object outline drawing, key shape, puzzle clue image, etc.)
- Contains:
  - Identified objects (outline image + map location)
  - Puzzle clues and keys
  - Lock requirements for locked doors
  - Placeholder entries for objects the player passed without checking (text only, no visual)

--->

## World Layout

- **7 rooms + 1 non-linear hallway** connecting them
- Escape-room style — many interlocked puzzles, items found in one room are often needed in another
- Puzzle details to be documented per puzzle as they are built

---

## Audio

Serves dual purpose:
- **Atmospheric** — ambient sounds setting mood throughout
- **Gameplay-critical** — some puzzles require the player to follow or interpret sounds (e.g. a dripping tap indicates water is nearby in the bathroom)

---

<!--## Save System

The game is designed to be completable in one long sitting but includes a save system as a safety net.

--->

<!--## Fail States

None — no game over, no punishment for choices. The player can take as long as they want.

--->

<!--## Puzzles

Mostly designed. To be documented individually as each is built.

**Known example:**
- Fill a bottle with the correct amount of water to balance a scale
- Player discovers water is nearby by hearing an unclosed tap dripping in the bathroom

--->

## Project Stack

- **Engine:** Unity 6 (6.4)
- **Render Pipeline:** URP (Universal Render Pipeline)
- **Input:** Unity Input System
- **Language:** C#
- **Platform:** PC (Windows primary)
