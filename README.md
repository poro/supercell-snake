# Snake Sweeper

**Snake meets Minesweeper.** A 3D snake game where mines hide in the fog of war, hitting one makes your snake grow (punishment), and skillfully passing by one makes it shrink (reward). Read the numbers. Avoid the mines. Eat all the food.

Built entirely with AI — the Unity editor was only used to press Play.

## How It Was Built

This project demonstrates a fundamentally different way to build a Unity game:

**The Game Design Document** was created collaboratively with **Claude Desktop** (Anthropic's desktop app). Starting from a historical reconstruction of Nokia's Snakes 3D (2005), the GDD evolved through iterative conversation into the Snake Sweeper concept — a mashup of classic Snake and Minesweeper mechanics.

**The entire codebase** was written by **Claude Code** (Anthropic's CLI tool), which directly created and modified Unity project files on the filesystem. This includes:

- `Assets/Scripts/SnakeGame.cs` — The complete game in a single MonoBehaviour (~1700 lines), written and iteratively refined by Claude Code
- `Assets/Scenes/SampleScene.unity` — Scene YAML edited directly by Claude Code to set serialized field values
- `.gitignore`, project configuration, git setup, and GitHub repository — all managed by Claude Code

**The Unity Editor was never used for game development.** It was only opened to:
1. Hit the Play button to test
2. Set up build targets

No dragging objects in the scene view. No clicking through inspectors. No importing assets through the UI. Every game object, material, light, camera, and UI element is created procedurally at runtime from code.

## Gameplay

### Controls
- **WASD / Arrow Keys** — Move the snake
- **P / Escape** — Pause

### Mechanics
- **Fog of War** — You can only see tiles near your snake's head
- **Number Hints** — Tiles show Minesweeper-style numbers (1-4) indicating adjacent mines, color-coded blue/green/orange/red
- **Mine Hit** — Snake grows +3 segments, fog shrinks, speed increases temporarily, adjacent mines may chain-detonate
- **Mine Avoidance** — Pass adjacent to a mine without hitting it to shrink by 1 segment and earn bonus points
- **Proximity Heartbeat** — A heartbeat sound gets louder and faster as you approach hidden mines
- **Chain Reactions** — Adjacent mines have a 50% chance to detonate in sequence after a mine hit
- **Eat All Food** — Clear every food item to advance to the next level

### Scoring
| Event | Points |
|---|---|
| Food eaten | +10 x level |
| Mine avoided | +25 x level |
| Mine hit | -15 x level |
| Chain detonation | -10 x level each |
| Level complete | +50 x level |

## Tech Stack

- **Engine**: Unity 6000.3.7f1 (URP — Universal Render Pipeline)
- **Platform**: macOS (Apple Silicon)
- **Language**: C# (.NET)
- **Rendering**: URP Forward Renderer, procedural materials using `Universal Render Pipeline/Lit` shader
- **Audio**: Sound effects and background music generated with [ElevenLabs](https://elevenlabs.io), loaded at runtime from `Resources/Audio/`. Heartbeat and shrink sparkle remain procedurally generated via `AudioClip.Create()`
- **UI**: Unity Canvas with runtime-created Text, Image, and layout components
- **3D Objects**: All built from Unity primitives (Cube, Sphere) composed at runtime

## Project Structure

```
Assets/
  Scripts/
    SnakeGame.cs          # The entire game — single MonoBehaviour
  Scenes/
    SampleScene.unity     # Minimal scene: Camera + empty GameObject with SnakeGame
  Settings/
    ForwardRenderer.asset # URP 3D Forward Renderer
    UniversalRP.asset     # URP pipeline settings
GAME_DESIGN_DOCUMENT.md   # Full GDD including Snake Sweeper design (Section 16)
```

## Running the Game

1. Open the project in **Unity 6000.3.7f1** (or compatible Unity 6 version)
2. Open `Assets/Scenes/SampleScene.unity`
3. Press **Play**

## Branches

- `main` — Base Unity project with initial Snake 3D implementation
- `snake-sweeper` — Full Snakes x Minesweeper integration with mine mechanics, sound, chain reactions, fog penalty, panic mode, and proximity heartbeat

## Tools Used

| Tool | Role |
|---|---|
| [Claude Desktop](https://claude.ai) | Game design document creation and iteration |
| [Claude Code](https://claude.ai/claude-code) | All code authoring, file system operations, git management, GitHub setup |
| [ElevenLabs](https://elevenlabs.io) | Sound effects (explosion, food pickup, death) and background music loop ("Snakes Lair") |
| [Unity 6](https://unity.com) | Game engine (press Play to test) |

## License

This project is a personal experiment in AI-assisted game development.
