# Snakes 3D — Game Design Document

**Document Version:** 1.0
**Game Title:** Snakes / Snakes Subsonic
**Developer:** IOMO (Snakes) / Barking Lizards (Snakes Subsonic)
**Publisher:** Nokia Oyj
**Platforms:** Nokia N-Gage, N-Gage 2.0, Symbian S60
**Release:** January 25, 2005 (Snakes) / May 22, 2008 (Snakes Subsonic)
**Genre:** Arcade / Puzzle
**Players:** 1–4
**Rating:** All Ages

---

## 1. Executive Summary

Snakes 3D represents the definitive evolution of Nokia's legendary Snake franchise — the most widely installed mobile game in history with over 350 million copies. Originally created by Taneli Armanto for the Nokia 6110 in 1998, the series' sixth major iteration brought the iconic eat-and-grow gameplay into full three-dimensional environments for the first time.

The game shipped in two major releases. *Snakes* (2005) introduced 3D multi-planar grids, Bluetooth multiplayer, and a viral distribution model that let players beam the entire game to friends wirelessly. *Snakes Subsonic* (2008) expanded the formula with elemental-themed worlds, reactive music by DJ Champion, vertical level geometry, and N-Gage Arena online leaderboards.

Both titles served as flagship showcases for the N-Gage platform, demonstrating that smooth 3D gaming was achievable on mobile hardware years before the smartphone era. Snakes received universal critical acclaim (Metacritic 85+) and remains a landmark in mobile gaming history — later recognized by the Museum of Modern Art as a culturally significant work.

This document reconstructs the complete game design of both titles as a unified reference, covering every major system from core mechanics to audio integration.

---

## 2. Game Concept

### 2.1 Core Fantasy

The player controls a snake navigating increasingly complex 3D environments. The snake moves perpetually forward and cannot stop. The player's job is to steer the snake to consume pellets, grow longer, avoid hazards, and complete level objectives — all while managing the escalating difficulty of maneuvering an ever-longer body through tighter spaces.

### 2.2 Design Pillars

**Accessible Depth.** The foundational mechanic — steer a snake, eat things, don't crash — is immediately understandable to anyone who has played the original Snake. The 3D environments, power-ups, and progression systems layer complexity on top of that foundation without obscuring it.

**Spatial Mastery.** Moving from 2D to 3D fundamentally changes the player's relationship with space. Navigating ramps, tunnels, wraparound edges, and multi-surface grids demands spatial reasoning that the original game never required. Mastery comes from internalizing three-dimensional routes.

**Social Competition.** Bluetooth multiplayer for up to four players, aggressive combat mechanics (tail-chopping, missiles, drones), and worldwide leaderboards via N-Gage Arena transform a traditionally solitary experience into a competitive one.

**Musical Synesthesia (Subsonic).** In Snakes Subsonic, the reactive soundtrack by DJ Champion accelerates and intensifies as the player collects blue pellets. Missing a pellet resets the music. Audio becomes a gameplay feedback channel, not just atmosphere.

### 2.3 Target Audience

Nokia N-Gage owners and Symbian S60 smartphone users — primarily 14–30 year olds looking for bite-sized gaming sessions on the go. The game caters both to casual players familiar with the Snake brand and to competitive players seeking depth through multiplayer and score-chasing.

### 2.4 Unique Selling Points

- First fully 3D Snake game on mobile platforms
- Four-player Bluetooth multiplayer with combat mechanics
- Viral distribution: beam the entire 1.4 MB game to a friend over Bluetooth
- Reactive soundtrack tied to gameplay performance (Subsonic)
- 40–42 handcrafted levels across themed elemental zones
- Online leaderboards via N-Gage Arena

---

## 3. Gameplay Mechanics

### 3.1 Movement

The snake always moves forward at a base speed. The player cannot stop or reverse direction. Input is limited to turning left, turning right, and modifying speed.

**Grid Types (Snakes 2005):**

- *Square Grid:* The snake turns at 90-degree angles. Movement feels crisp and predictable, similar to the original 2D Snake translated into 3D space.
- *Hexagonal Grid:* The snake turns at 120-degree angles. This creates smoother curves but introduces ambiguity in tight spaces, as the player must think in terms of three directional axes rather than two.

**3D Navigation (Snakes Subsonic):**

The playfield is no longer a flat plane. The snake can traverse ramps, climb steep inclines, descend into tunnels, and loop around terrain features. Going up an incline and cresting the top creates a dramatic shift in camera perspective. Wraparound edges function as virtual portals — the snake exits one edge and enters from the opposite side with a 180-degree orientation flip.

### 3.2 Speed Control

The player can manually adjust speed using dedicated buttons:

- *Turbo Boost:* Accelerates the snake. Useful for rushing toward a power-up or outrunning an opponent in multiplayer, but dangerous in tight spaces.
- *Slow Boost:* Decelerates the snake. Gives the player more reaction time for precision navigation.

Additionally, environmental tiles modify speed passively:

- *Green Cells:* Increase snake speed when traversed.
- *Red Cells:* Decrease snake speed when traversed.

The interplay between manual speed control and environmental modifiers creates a rhythm of acceleration and caution that the original 2D Snake never had.

### 3.3 Growth and Consumption

Green pellets are scattered across each level. Consuming a green pellet extends the snake by one segment. A longer snake is worth more points but is harder to maneuver. This core tension — growth as both reward and risk — is the heart of the Snake formula and is preserved faithfully in 3D.

### 3.4 Collision Rules

The 3D games introduce a more nuanced collision model than the binary alive/dead system of classic Snake:

| Collision Type | Consequence |
|---|---|
| Self (own tail) | Instant death |
| Classic wall | Instant death |
| Breakable wall | Wall destroyed; snake loses length segments |
| Environment obstacle | Damage to life bar (not instant death) |
| Opponent snake (multiplayer) | Depends on context — see Section 6 |
| Hole / tunnel entrance | Snake enters tunnel; exits at linked point |
| Wraparound edge | Snake teleports to opposite edge, 180° flip |

The life bar system is a significant departure. In classic Snake, any collision with a wall ended the game. In the 3D versions, environmental collisions deplete a health meter, giving the player a buffer. This accommodates the increased spatial complexity of 3D navigation, where occasional bumps are nearly inevitable.

### 3.5 Level Progression — The Evolver Bar (Subsonic)

Snakes Subsonic introduces a structured progression mechanic called the Evolver Bar:

1. The player begins each level by consuming green pellets to grow the snake.
2. After eating a threshold number of green pellets, glowing blue pellets begin appearing in a trail formation.
3. The player must follow and collect the entire blue pellet trail without missing any.
4. Successfully completing the trail empties the Evolver Bar and advances the player to the next level.
5. Missing a blue pellet resets the trail — the player must re-trigger blue pellets by eating more green ones.

This system layers a secondary objective on top of the basic eat-and-grow loop. The blue trail often snakes through hazardous terrain, forcing the player to execute precise maneuvers under time pressure while the reactive soundtrack builds in intensity.

---

## 4. Power-Up and Item System

### 4.1 Consumable Power-Ups

| Power-Up | Effect | Availability |
|---|---|---|
| **Green Pellet** | Extends snake length by one segment | All levels |
| **Blue Pellet** | Part of Evolver trail; required for level completion | Subsonic only |
| **Shield** | Provides temporary invulnerability to collisions | Both titles |
| **Drone** | Autonomous defensive unit that attacks nearby enemy snakes | Snakes 2005 |
| **Missile** | Projectile weapon; damages enemy snakes or destroys obstacles | Both titles |
| **Teleport** | Instantly transports the snake to a random location on the playfield | Snakes 2005 |
| **Extra Life** | Grants an additional life | Snakes 2005 |
| **Energy Boost** | Partially restores the life bar | Both titles |

### 4.2 Hidden Collectibles

In Snakes Subsonic, the letters S-N-A-K-E-S are hidden across levels. Collecting all six unlocks secret bonus levels. These letters are time-sensitive — they disappear when the blue Evolver Bar is completed, creating a tension between progressing through the level quickly and exploring for secrets. The timing window is deliberately tight, rewarding players who have memorized level layouts.

---

## 5. Level Design

### 5.1 Snakes (2005) — Grid-Based Levels

The original game features 42 handcrafted levels (37 on N-Series Symbian devices). Each level offers a choice between square and hexagonal grid types, effectively doubling the playfield variety.

**Progression structure:**

- *Early levels (1–14):* Simple flat grids with minimal obstacles. Introduces the player to 3D navigation, speed controls, and basic power-ups.
- *Mid levels (15–28):* Multi-surface gameplay. The snake can climb around edges of the playfield and play on both sides of the grid surface, adding a second navigable plane.
- *Late levels (29–42):* Full multi-planar gameplay with complex geometry, tight corridors, and aggressive environmental hazards. Demands mastery of 3D spatial reasoning.

### 5.2 Snakes Subsonic — Elemental Zones

Subsonic organizes its 40 main levels into four thematic zones of 10 levels each, progressing in difficulty:

**Earth Zone (Levels 1–10)**
Natural, earth-toned environments with gentle terrain. Introduces basic 3D mechanics: ramps, simple tunnels, and breakable walls. The visual palette is grounded — greens, browns, and warm tones. Designed as an onboarding zone.

**Air Zone (Levels 11–20)**
Light, sky-themed aesthetics with open layouts and elevated platforms. Introduces vertical gameplay with steep inclines and dramatic camera perspective shifts. Speed-boosting green cells appear more frequently, increasing pace.

**Water Zone (Levels 21–30)**
Aquatic visual themes with fluid, winding level geometry. Introduces complex tunnel networks and wraparound edges. Emphasizes route memorization as tunnels obscure visibility.

**Fire Zone (Levels 31–40)**
Warm, aggressive color palettes with the densest obstacle layouts. Combines all previously introduced mechanics — ramps, tunnels, breakable walls, speed tiles, and wraparound edges — into demanding, maze-like playfields. The culmination of the difficulty curve.

**Bonus Content:**

- 20 additional levels accessible via N-Gage Arena (online download)
- Secret levels unlocked by collecting hidden S-N-A-K-E-S letters

### 5.3 Environmental Hazards and Features

| Feature | Behavior |
|---|---|
| **Classic Wall** | Contact kills the snake instantly |
| **Breakable Wall** | Can be destroyed on contact but costs snake length segments |
| **Tunnel** | Snake enters and exits at a linked point; path is hidden while inside |
| **Wraparound Edge** | Snake exits one edge and enters from the opposite side with a 180° orientation flip |
| **Ramp / Incline** | Snake traverses vertical geometry; camera shifts dramatically |
| **Loop** | Continuous circular path through 3D space |
| **Green Speed Cell** | Accelerates the snake when crossed |
| **Red Speed Cell** | Decelerates the snake when crossed |
| **Hole** | Gap in the playfield; falling in may damage or relocate the snake |

---

## 6. Multiplayer Design

### 6.1 Snakes (2005) — Bluetooth Competitive

Up to four players connect via Bluetooth using separate N-Gage devices. The game supports low-latency wireless play with virtually no perceptible lag.

**Mechanics:**

- All players share the same playfield and compete for the highest score.
- Players can attack each other using power-ups: missiles deal damage, drones hunt nearby opponents, and shields provide temporary protection.
- A successful attack on an opponent's tail severs segments, shortening their snake and reducing their score.
- Eliminating an opponent's snake entirely awards bonus points.
- A sonar display appears on-screen showing the approximate positions of all opponents, adding tactical awareness to the competition.

**Session flow:** Players agree on a target score or time limit. The first player to reach the target or the player with the highest score when time expires wins.

### 6.2 Snakes Subsonic — Arena and Ghost Racing

Subsonic shifts the multiplayer model toward asynchronous competition:

- Up to four players compete for a set point total.
- Ghost racing allows players to compete against recorded performances rather than live opponents.
- N-Gage Arena integration enables worldwide leaderboard competition. Players with valid N-Gage data plans can upload scores directly from within the game.

### 6.3 Viral Distribution

A remarkable feature of the original Snakes: the entire 1.4 MB game file could be transmitted to another N-Gage device via Bluetooth using the "Send Game" menu option. This viral distribution mechanic was a deliberate design choice — by making the game free and frictionlessly shareable, Nokia ensured maximum multiplayer adoption and used Snakes as a platform ambassador for the N-Gage ecosystem.

---

## 7. Camera System

### 7.1 Default Perspective

The camera is positioned behind and slightly above the snake's head, providing a third-person trailing view. This perspective offers a clear view of the immediate path ahead while maintaining enough peripheral visibility for the player to anticipate turns.

### 7.2 Look Mechanic

Quick taps of the left or right directional input trigger a look function — the camera briefly swivels to show adjacent areas without changing the snake's heading. This allows the player to scout upcoming terrain without committing to a turn.

### 7.3 Alternative View

A dedicated button toggles to a classic overhead perspective reminiscent of the original 2D Snake. This is useful for getting a broader view of the playfield layout but sacrifices the depth perception needed for navigating 3D geometry.

### 7.4 Dynamic Perspective Shifts (Subsonic)

When the snake traverses ramps or crests steep inclines, the camera dynamically adjusts to maintain visual clarity. Going up an incline and over the top produces a dramatic perspective shift as the camera swings to follow the snake onto the new surface. These transitions are designed to be visually impressive while remaining readable during gameplay.

---

## 8. Controls and Input

### 8.1 Input Device

Both titles are designed for the Nokia N-Gage's physical keypad: a directional pad for steering and dedicated action buttons for speed control, view switching, and power-up activation.

### 8.2 Control Mapping

| Input | Action |
|---|---|
| D-pad Left | Turn left |
| D-pad Right | Turn right |
| D-pad Left (quick tap) | Look left (camera glance) |
| D-pad Right (quick tap) | Look right (camera glance) |
| Action Button 1 | Turbo boost (speed up) |
| Action Button 2 | Slow boost (speed down) |
| Action Button 3 | Change camera perspective |
| Action Button 4 | Activate equipped power-up |
| Menu Button | Access pause menu / Send Game / Arena options |

### 8.3 Input Philosophy

The control scheme is deliberately minimal. The snake's perpetual forward motion means the player only needs to make turning decisions and speed adjustments. This simplicity is essential for the N-Gage's small directional pad and ensures the game remains playable during short, one-handed sessions.

The distinction between a quick tap (look) and a sustained press (turn) on the directional input adds a layer of nuance without adding buttons. Players learn to scout ahead with quick taps before committing to turns — a skill that becomes critical in later levels.

---

## 9. Art Direction

### 9.1 Visual Philosophy

The 3D Snakes games adopt a clean, geometric aesthetic driven as much by hardware necessity as artistic choice. The N-Gage lacked dedicated 3D graphics hardware, so all rendering was software-based. This constraint forced a visual language of flat-textured polygons, simple meshes, and bold colors that reads clearly on small screens and runs at smooth framerates.

### 9.2 Snakes (2005) — Tron Meets The Jungle Book

The original game's visual design evokes two distinct influences. The grid-based polygon playfields recall the light-cycle arenas of Tron — glowing edges, geometric precision, pulsating color. The snake character itself, however, is rendered with a more organic, friendly appearance reminiscent of Kaa from The Jungle Book, complete with a lush green palette, fruit collectibles, and foliage accents.

This contrast — mechanical environments inhabited by an organic character — gives the game a distinctive identity. Pulsating textures on special cells (speed boosts, power-ups) provide at-a-glance gameplay information through color coding.

### 9.3 Snakes Subsonic — Elemental Worlds

Subsonic expands the visual vocabulary across its four themed zones. Each zone has a distinct color palette and environmental motif:

- *Earth:* Warm greens and browns. Rocky textures, natural geometry.
- *Air:* Light blues and whites. Open, airy spaces with sky elements.
- *Water:* Deep blues and teals. Fluid, curving level geometry.
- *Fire:* Reds, oranges, and blacks. Angular, aggressive architecture.

The polygon count is higher than the 2005 original, but the visual language remains deliberately simple. Clean geometry, strong color differentiation, and minimal visual clutter ensure that gameplay readability is never sacrificed for visual fidelity.

### 9.4 UI and HUD

The heads-up display is minimal to preserve screen real estate on small mobile displays:

- **Score counter** — top of screen
- **Snake length indicator** — top of screen
- **Life bar** — horizontal bar showing remaining health
- **Evolver Bar** (Subsonic) — shows progress toward level completion
- **Sonar display** (multiplayer) — shows approximate positions of opponents
- **Timer** — remaining time for level completion (where applicable)

---

## 10. Audio Design

### 10.1 Snakes (2005)

The original game features functional mobile-era sound design: chime-based sound effects for pellet collection, collision impacts, and power-up activation. Audio serves as confirmation feedback — the player hears that an action registered. Music is ambient and unobtrusive, appropriate for short play sessions in public spaces.

### 10.2 Snakes Subsonic — Reactive Soundtrack

Subsonic's audio design is its most innovative feature. DJ Champion composed a dynamic soundtrack that responds directly to player performance:

**The Audio-Gameplay Loop:**

1. During normal play, background music plays at a steady, moderate tempo.
2. When the player begins collecting blue Evolver pellets, the soundtrack intensifies — tempo increases, layers are added, energy builds.
3. Each consecutive blue pellet hit adds musical intensity, creating an accelerating sense of momentum.
4. Successfully completing the entire blue trail triggers a musical climax synchronized with the Evolver Bar emptying and the level transitioning.
5. Missing a blue pellet immediately resets the music to its base state, creating an audible sense of failure that mirrors the mechanical reset of the Evolver trail.

This system transforms the soundtrack from background noise into a gameplay feedback channel. Players learn to "hear" their performance — rising music means they're on track; a sudden drop means they've missed a pellet. The music becomes intrinsic to the game feel rather than decorative.

### 10.3 Sound Effects

| Event | Sound Design |
|---|---|
| Green pellet collected | Positive chime; pitch rises with consecutive collections |
| Blue pellet collected | Harmonic tone synchronized with soundtrack |
| Blue pellet missed | Dissonant drop; music resets |
| Collision with wall | Impact sound; severity indicates damage |
| Breakable wall destroyed | Crumbling/shattering effect |
| Power-up collected | Distinct activation tone per power-up type |
| Opponent attacked (multiplayer) | Aggressive hit confirmation |
| Level complete | Triumphant fanfare synchronized with music climax |
| Snake death | Deflating or crashing sound |

---

## 11. Scoring and Progression

### 11.1 Scoring Metrics

**Primary score** is determined by snake length — each consumed green pellet adds to both the snake's physical size and the running score. **Bonus points** are awarded for remaining time at level completion, encouraging efficient play. In multiplayer, additional points come from aggressive actions: severing opponent tail segments and eliminating rival snakes.

### 11.2 Progression Flow

```
Earth Zone (10 levels)
    └── Unlock: Air Zone
         └── Unlock: Water Zone
              └── Unlock: Fire Zone
                   └── Unlock: N-Gage Arena Levels (20 bonus)

Hidden S-N-A-K-E-S letters (scattered across all zones)
    └── Collect all 6 → Unlock Secret Bonus Levels
```

Each zone must be completed sequentially. Within a zone, levels are completed by emptying the Evolver Bar (Subsonic) or reaching a score/time threshold (Snakes 2005).

### 11.3 Leaderboards

N-Gage Arena provides a worldwide online leaderboard. Players with valid N-Gage data plans can upload their high scores directly from within the game. The leaderboard is per-level, encouraging players to optimize individual level performances rather than just progressing through content.

---

## 12. Technical Architecture

### 12.1 Rendering

All 3D rendering is software-based. The N-Gage lacks dedicated GPU hardware, so the game engine performs all vertex transformation, rasterization, and texturing on the CPU. This constraint shapes the entire visual design — flat textures, low polygon counts, and minimal overdraw are not style choices but engineering necessities.

Despite these limitations, both titles achieve smooth, consistent framerates. The developers at IOMO and Barking Lizards demonstrated exceptional optimization skill, delivering a fluid 3D experience on hardware that most developers considered inadequate for real-time 3D gaming.

### 12.2 Memory and Distribution

The original Snakes occupies just 1.4 MB — small enough to be transmitted wirelessly via Bluetooth to another N-Gage in a reasonable timeframe. This file size constraint influenced every aspect of the game's content pipeline: textures are tiny, geometry is minimal, audio is compressed aggressively.

### 12.3 Platform Compatibility

| Platform | Notes |
|---|---|
| Nokia N-Gage (original) | Primary target; full feature set |
| Nokia N-Gage 2.0 | Subsonic primary platform |
| Symbian S60 2nd Edition | Snakes 2005 compatible |
| Symbian S60 3rd Edition | Both titles; optimized for N73, N95, E65 |
| Symbian S60 5th Edition | Subsonic compatible |
| Symbian^3 | Subsonic compatible |

### 12.4 Bluetooth Networking

Multiplayer uses Bluetooth for device-to-device communication. The networking layer handles up to four simultaneous connections with low enough latency that competitive play is viable. Game state synchronization uses a lightweight protocol appropriate for Bluetooth bandwidth constraints. The sonar display in multiplayer is likely an optimization — transmitting approximate positions rather than full snake geometry reduces bandwidth requirements.

---

## 13. Monetization and Distribution

### 13.1 Pricing Model

Both titles were distributed free of charge. This was a deliberate strategic decision by Nokia to drive N-Gage platform adoption and demonstrate the device's gaming capabilities. Snakes served as both a showcase and a social catalyst — the viral Bluetooth distribution ensured that every N-Gage owner could share the game with friends, expanding the multiplayer pool and incentivizing N-Gage purchases.

### 13.2 N-Gage Arena

While the game itself was free, the N-Gage Arena online leaderboard service required a valid N-Gage data plan. This created an indirect monetization path — free game drives engagement, engagement drives desire for competitive ranking, competitive ranking drives data plan subscriptions.

### 13.3 Bonus Content

The 20 additional N-Gage Arena levels served as a content incentive for online connectivity, further encouraging data plan adoption.

---

## 14. Competitive Analysis

### 14.1 Versus Classic 2D Snake

| Feature | Classic Snake (1998) | Snakes 3D (2005/2008) |
|---|---|---|
| Perspective | Top-down 2D | Behind-view 3D |
| Playfield | Single flat grid | Multi-surface, multi-level |
| Collision | Binary (alive/dead) | Life bar with damage |
| Power-ups | None | Shields, missiles, teleports, drones |
| Multiplayer | None (on mobile) | 4-player Bluetooth + online |
| Level structure | Infinite (score chase) | 40–42 handcrafted levels |
| Speed control | None (auto-accelerating) | Manual turbo/slow + environmental |
| Audio | Beeps | Reactive soundtrack (Subsonic) |
| Content variety | Single mode | Themed zones, secret levels, arena |

### 14.2 Market Position

At the time of release, Snakes 3D had no direct competitors in the mobile 3D arcade space. The N-Gage's game library included ports of console titles (Tony Hawk, Tomb Raider) but few original titles that leveraged the device's unique capabilities as effectively. Snakes became the N-Gage's signature game — the title most frequently cited as a reason to own the platform.

---

## 15. Legacy and Cultural Impact

The Nokia Snake franchise holds a unique position in gaming history. The original 1998 Snake — with over 350 million installations — is one of the most played games ever created. The 3D iterations preserved the franchise's accessibility while proving that mobile devices could deliver genuine 3D gaming experiences years before the iPhone and modern smartphone era.

In November 2012, the Museum of Modern Art recognized Snake as a culturally significant work, cementing its place in design history.

The 3D versions served as a critical bridge between the monochrome simplicity of early mobile gaming and the sophisticated touch-based experiences that would follow. Design decisions made in Snakes — environmental power-ups, health bars replacing instant death, bite-sized level structures, social multiplayer — became standard vocabulary for mobile game design in the decade that followed.

---

## 16. Snake Sweeper — Snakes x Minesweeper Core Integration

### 16.1 Concept

Snake Sweeper fuses classic Snake gameplay with Minesweeper's hidden-information puzzle mechanic. Mines are concealed within the fog of war. The snake must eat all food on each level to advance, but hidden mines create a risk/reward layer: hitting a mine punishes the player with growth (making the snake harder to control), while successfully navigating adjacent to a mine rewards the player by shrinking the snake.

The result is a slower, more deliberate Snake game where reading Minesweeper-style number hints and managing risk is as important as reflexes.

### 16.2 Pacing and Speed

The snake moves at a deliberately slow pace (startSpeed: 0.28s per tile, maxSpeed: 0.12s) — significantly slower than traditional Snake. This gives players time to read number hints, plan routes, and make strategic decisions about which tiles to cross.

Speed increases slightly per level (0.02s per level), maintaining tension without overwhelming the puzzle-reading aspect.

### 16.3 Mine Placement

Mines are placed randomly at the start of each level, avoiding:
- A 2-tile radius around the snake's starting position
- Obstacle tiles
- Food tiles

**Mine density by level:**
| Level | Mine Count |
|---|---|
| 1 | 4 |
| 2–3 | 6 |
| 4–6 | 10 |
| 7+ | 14 |

Mines are invisible until the snake's fog of war reveals them. Within the fog inner radius, mines appear as dark metallic spheres with protruding spikes (classic naval mine silhouette).

### 16.4 Minesweeper Number Hints

Every non-mine tile displays a number indicating how many orthogonally adjacent tiles (up/down/left/right — 4-way, not diagonal) contain mines. Numbers are color-coded:

| Count | Color |
|---|---|
| 1 | Blue |
| 2 | Green |
| 3 | Orange |
| 4 | Red |

Numbers are only visible within the fog inner radius. Tiles with count 0 show nothing. Mine tiles themselves show no number. When a mine detonates, neighboring tile counts recalculate (numbers decrease as mines are removed).

### 16.5 Mine Collision (Growth Penalty)

When the snake moves onto a mine tile:
1. **Growth**: Snake grows +3 segments instantly (longer snake = harder to maneuver)
2. **Score penalty**: -15 x current level (minimum 0)
3. **Crater**: Tile permanently darkens (detonated mine becomes a crater)
4. **Explosion effect**: 12 orange-to-red particles expand outward over 0.6s
5. **Screen shake**: Camera shakes for 0.2s
6. **Freeze frame**: Game pauses for 0.3s (impact moment)
7. **Explosion sound**: Multi-layered bass thump + crackle
8. **Fog shrinks**: Vision radius drops to ~45% for 5 seconds, gradually restores over last 2s
9. **Panic speed**: Snake moves at 2x speed for 3.5 seconds (loss of control)
10. **Chain reaction**: Each orthogonally adjacent mine has 50% chance to also detonate (staggered 0.12s between each), adding +2 segments and -10 x level per chain

The combination of reduced vision, increased speed, and a longer snake creates a cascading danger spiral — one mistake can snowball.

### 16.6 Mine Avoidance (Shrink Reward)

When the snake's head moves to a tile orthogonally adjacent to a mine (without stepping on it):
1. **Shrink**: Snake loses 1 segment from the tail (minimum length: 1)
2. **Score bonus**: +25 x current level
3. **Sparkle effect**: 6 green spheres float upward in a circle over 0.4s
4. **Sparkle sound**: Ascending C6-E6-G6 arpeggio

Each mine can only grant one shrink reward per pass (cooldown resets when the head moves away).

### 16.7 Proximity Heartbeat

A procedural heartbeat sound (double-thump pulse) plays continuously, with volume and speed controlled by distance to the nearest active mine:
- **Beyond fog radius**: Silent
- **At fog outer edge**: Quiet, slow pulse
- **Within inner radius**: Medium intensity
- **Adjacent to mine**: Loud, rapid pounding

This creates escalating dread before the player even sees a mine, reinforcing the Minesweeper tension of approaching unknown tiles.

### 16.8 Multi-Food System

Multiple food items spawn simultaneously at the start of each level:
- **Food count**: 4 + current level (Level 1 = 5 food, Level 5 = 9 food)
- Food items bob and spin for visual appeal
- Food visibility scales with fog (invisible outside fog, full size within inner radius)
- **Win condition**: Eat all food to advance to the next level
- **Level bonus**: +50 x level on completion

### 16.9 System Flow

```
Level Start
  → Place mines (hidden) → Place obstacles → Spawn all food
  → Player navigates using WASD/Arrows
  → Fog reveals tiles, numbers, mines, and food as snake moves
  → Heartbeat intensifies near mines
  → Hit mine → Growth + Fog shrink + Panic speed + Chain reactions
  → Pass by mine → Shrink + Score bonus
  → Eat food → Growth + Score
  → All food eaten → Level bonus → Next level (more mines, more food, faster)
  → Hit wall/obstacle/self → Death → Reveal all mines and numbers
```

### 16.10 Scoring Summary

| Event | Points |
|---|---|
| Food eaten | +10 x level |
| Mine avoided (shrink) | +25 x level |
| Mine hit (penalty) | -15 x level |
| Chain detonation (penalty) | -10 x level per chain |
| Level complete (bonus) | +50 x level |

### 16.11 Death and Reveal

On death, the full board is revealed: all remaining mines shown as red spheres, all number hints displayed, all obstacles and food at full brightness. This gives the player a "Minesweeper reveal moment" — seeing where every mine was and understanding what killed them.

### 16.12 Design Rationale

The growth-as-punishment / shrink-as-reward inversion is the core innovation. In classic Snake, growth is progress. In Snake Sweeper, growth is danger — a longer snake is harder to control in tight spaces. This inverts the player's relationship with the snake's length and creates a natural tension: do you risk passing near a mine to shrink, or take the safe route and stay long?

The fog shrink + panic speed combo after a mine hit creates a genuine horror-survival moment: you made a mistake, and now you can see less, move faster, and are bigger. The heartbeat audio builds dread before visual contact, making the information-gathering phase feel tense rather than passive.

Chain reactions add unpredictability — one mine can cascade into a disaster, making every mine hit feel like it could end the run.

---

## Appendix A: Level Count Summary

| Content | Levels | Platform |
|---|---|---|
| Snakes (2005) — N-Gage | 42 | N-Gage |
| Snakes (2005) — N-Series | 37 | Symbian S60 |
| Snakes Subsonic — Main | 40 | N-Gage 2.0 / Symbian |
| Snakes Subsonic — Arena Bonus | 20 | N-Gage Arena |
| Snakes Subsonic — Secret | Variable | Both (hidden unlock) |

## Appendix B: Key Personnel

| Role | Name | Title |
|---|---|---|
| Original Snake Creator | Taneli Armanto | Snake (1998), Nokia 6110 |
| Snakes 3D Developer | IOMO | Snakes (2005) |
| Snakes Subsonic Developer | Barking Lizards | Snakes Subsonic (2008) |
| Subsonic Composer | DJ Champion | Reactive soundtrack |
| Publisher | Nokia Oyj | Both titles |

## Appendix C: Critical Reception

Snakes (2005) received widespread critical acclaim, earning a Metacritic score above 85. Reviewers praised the successful translation of the Snake formula into 3D, the smooth performance on N-Gage hardware, the innovative Bluetooth multiplayer and viral distribution, and the surprising depth hidden beneath the familiar brand. The game was consistently identified as the N-Gage's best original title and one of the strongest arguments for the platform's viability as a gaming device.

---

*This document reconstructs the game design of Snakes (2005) and Snakes Subsonic (2008) based on publicly available information including reviews, retrospectives, and wiki documentation. It is intended as a historical reference and design analysis.*
