# dh-2026 — Game Design Document

## Concept

A 2D text-based adventure game about a blind protagonist who wakes up in an unknown house and must navigate using his senses to avoid obstacles and solve puzzles in order to escape.

---

## UI / Visual Style

- **Entirely black screen** — no environment visuals during normal gameplay
- Text description of the current situation shown on screen
- **Clickable text options** — player makes choices by clicking them (no keyboard input for choices)
- The only overlays are the **Memory Map** and **Memory Journal**, accessed via the `M` key

---

## Core Navigation

The player navigates by choosing from a set of options presented as clickable text.

**Example — first room:**
- Trace hand along left wall
- Stand up and walk forward
- Trace hand along right wall

The number of options per situation is not fixed — most rooms will have 3, but can have more or fewer depending on context.

---

## Obstacle / Collision System

When the player walks into an object (e.g. a table), the game plays an audible sound (e.g. a "thunk") and the character reacts with a line like *"Hmm, I wonder what I hit."*

The player is then given three new options:
- **Check** — triggers the Feel Around minigame (see below)
- **Go back** — retreat to previous position/state
- **Continue past** — ignore the object and keep moving

---

## Feel Around Minigame

Triggered when the player chooses **Check** on an unknown object.

**Flow:**
1. Rustling/ambient sounds play in the background to signal the character is moving around the object
2. Screen goes completely black
3. Text at the top guides the player (e.g. *"Use your hands to feel what's in front of you"*)
4. The player uses the mouse as the character's hand:
   - **Hold left mouse button + drag** = feel around
   - Wherever the mouse moves while held, a portion of the object is revealed
5. Underneath the black foreground is a black image with **white outlines** of the object
6. The black foreground is erased progressively (scratch-card style) as the player moves the mouse

**Open questions to decide:**
- Reveal threshold — 100% or partial (e.g. 70%) to count as identified?
- After identification — return to the 3 options (Check / Go back / Continue past) or straight back to the room?
- Is the revealed outline drawing saved to the Memory Journal so the player can view it again later?

---

## Memory System (M key)

Pressing `M` opens an overlay with two sections:

### 1. Memory Map
- Builds up as the player traces walls and explores
- Only walls/areas the player has physically traced are drawn — unknown areas remain blank
- Gives the player a spatial understanding of the house layout over time

### 2. Memory Journal
- Stores things the player has discovered and needs to remember:
  - **Object outlines** — identified via the Feel Around minigame
  - **Puzzle keys/clues** — information needed to solve puzzles
  - Potentially more as the game design develops

---

## Puzzle System

Puzzles are solved using information gathered through exploration and the Feel Around minigame. Design of individual puzzles is still in progress.

**Known puzzle elements:**
- Object shapes discovered through touch
- Keys or codes found in the environment

---

## Project Stack

- **Engine:** Unity 6 (6.4)
- **Render Pipeline:** URP (Universal Render Pipeline)
- **Input:** Unity Input System
- **Language:** C#
- **Platform:** PC (Windows primary)
