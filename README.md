# Snake Sweeper

**Snake meets Minesweeper.** A 3D snake game where mines hide in the fog of war, hitting one makes your snake grow (punishment), and skillfully passing by one makes it shrink (reward). Circumnavigate a mine to safely defuse it for massive bonus points. Read the numbers. Avoid the mines. Eat all the food.

Built entirely with AI — the Unity editor was only used to press Play.

Created at the **Supercell AI Lab Hackathon** (February 6–8, 2026) by **Mark Ollila** ([Endless Games and Learning Lab](https://gamelab.asu.edu), Arizona State University) as Human Overlord, with AI doing the rest.

## How It Was Built

This project demonstrates a fundamentally different way to build a Unity game:

**The Game Design Document** was created collaboratively with **Claude Desktop** (Anthropic's desktop app). Starting from a historical reconstruction of Nokia's Snakes 3D (2005), the GDD evolved through iterative conversation into the Snake Sweeper concept — a mashup of classic Snake and Minesweeper mechanics.

**The entire codebase** was written by **Claude Code** (Anthropic's CLI tool), which directly created and modified Unity project files on the filesystem. This includes:

- `Assets/Scripts/SnakeGame.cs` — The complete game in a single MonoBehaviour (~2700 lines), written and iteratively refined by Claude Code
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

### Screens
- **Splash Screen** — Neon title with decorative 3D mines, press any key to start
- **How to Play** — Press **H** from splash screen (3 pages, arrow keys to navigate)
- **Credits** — Press **C** from splash screen

### Core Mechanics
- **Fog of War** — You can only see tiles near your snake's head
- **Number Hints** — Tiles show Minesweeper-style numbers (1-4) indicating adjacent mines, color-coded blue/green/orange/red
- **Eat All Food** — Clear every food item to advance to the next level (tracked with gold circle icons in the HUD)
- **Obstacles** — Dark blocks that increase each level, instant death on contact

### Mine Mechanics
- **Mine Hit** — Snake grows +3 segments, fog shrinks, speed spikes, screen flash + 3-phase explosion effect. Adjacent mines may chain-detonate (50% each)
- **Mine Avoidance** — Pass adjacent to a mine without hitting it to shrink by 1 segment and earn bonus points
- **Mine Sweep (Circumnavigation)** — Visit all 8 tiles surrounding a mine in consecutive moves to safely defuse it for a massive +100 x level bonus. Step away and progress resets. Edge/corner mines need fewer tiles
- **Proximity Heartbeat** — A heartbeat sound gets louder and faster as you approach hidden mines

### Score Multiplier
- Step on a **"1"** tile for a flat +15 bonus
- Step on a **"2" / "3" / "4"** tile to activate that number as a score multiplier for 5 seconds
- Multiplier applies to all positive scoring (food, avoidance, mine sweep)

### Scoring
| Event | Points |
|---|---|
| Food eaten | +10 x level x multiplier |
| Mine avoided | +25 x level x multiplier |
| Mine swept (defused) | +100 x level x multiplier |
| Number "1" tile bonus | +15 x level x multiplier |
| Mine hit | -15 x level |
| Chain detonation | -10 x level each |
| Level complete | +50 x level |

### Progression
- **10 levels** to victory, then **Endless mode** (level 11+)
- Food capped at 12 per level, obstacles capped at 35
- All levels validated with flood-fill reachability check
- Neon-styled level clear overlays with bold celebration text

## Tech Stack

- **Engine**: Unity 6000.3.7f1 (URP — Universal Render Pipeline)
- **Platform**: macOS (Apple Silicon)
- **Language**: C# (.NET)
- **Rendering**: URP Forward Renderer, procedural materials using `Universal Render Pipeline/Lit` shader
- **Audio**: Sound effects and background music generated with [ElevenLabs](https://elevenlabs.io), loaded at runtime from `Resources/Audio/`. Heartbeat and shrink sparkle remain procedurally generated via `AudioClip.Create()`
- **UI**: Unity Canvas with runtime-created Text, Image, and layout components. Rich text with `<size>`, `<color>`, `<b>` tags for neon-styled overlays
- **3D Objects**: All built from Unity primitives (Cube, Sphere) composed at runtime — mines are sphere+spike composites, explosions are multi-phase particle effects
- **Effects**: 3-phase mine explosions (fireball flash → shrapnel burst → lingering embers), screen flash overlay, camera shake, screen freeze

## Project Structure

```
Assets/
  Scripts/
    SnakeGame.cs          # The entire game — single MonoBehaviour (~2700 lines)
  Scenes/
    SampleScene.unity     # Minimal scene: Camera + empty GameObject with SnakeGame
  Resources/
    Audio/
      eat.mp3             # Food pickup sound (ElevenLabs)
      explosion.mp3       # Mine explosion sound (ElevenLabs)
      death.mp3           # Death/wall crash sound (ElevenLabs)
      music.mp3           # "Snakes Lair" background loop (ElevenLabs)
  Settings/
    ForwardRenderer.asset # URP 3D Forward Renderer
    UniversalRP.asset     # URP pipeline settings
GAME_DESIGN_DOCUMENT.md   # Full GDD including Snake Sweeper design (Section 16)
```

## Running the Game

1. Open the project in **Unity 6000.3.7f1** (or compatible Unity 6 version)
2. In Unity, go to File > Open Scene (or use the Project panel)
3. Navigate to Assets/Scenes/SampleScene.unity and open it
4. Then hit the Play button

## Tools Used

| Tool | Role |
|---|---|
| [Claude Desktop](https://claude.ai) | Game design document creation and iteration |
| [Claude Code](https://claude.ai/claude-code) | All code authoring, file system operations, git management, GitHub setup |
| [ElevenLabs](https://elevenlabs.io) | Sound effects (explosion, food pickup, death) and background music loop ("Snakes Lair") |
| [Unity 6](https://unity.com) | Game engine (press Play to test) |

## License

MIT License — see [LICENSE](LICENSE) for details.
