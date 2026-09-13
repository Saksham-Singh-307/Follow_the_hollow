# VoiceRunner

Endless runner controlled entirely by voice — shout to jump. Built in Unity 2D/URP, generates its own art and level at runtime.

## Controls

| Input | Effect |
|---|---|
| Shout / Space / ↑ | Jump |
| Shout again mid-air | Double jump |
| Keep shouting while rising | Jump a little higher/longer |

- Mic auto-calibrates to room noise in ~1 second.
- No mic detected → keyboard fallback kicks in automatically.

## Core features

- **Auto-run** — forward speed is constant; skill = jump timing.
- **The Hollow (chaser)** — always somewhere behind you. Eases off traps when it's close, backs off after clean runs. Exposes a `Pressure` (0→1) value for anything reactive.
- **Coin combo** — coins chained within ~1.1s stack a multiplier (1x → 5x). Let it lapse, back to 1x.
- **Power-ups** — Speed, High Jump, Invisibility (also makes you uncatchable). Each on its own timer.
- **Branching lanes (Fork chunks)** — low lane: safe ground, few coins. High lane: platforms above with a power-up + coin trail, reached via a held/double jump. Miss a hop → drop safely to the low lane, no death.
- **Hazards** — spikes (static/popup), chompers (some leap), fake coins, fake/invisible/falling blocks, crushers, walls, moving platforms.
- **Difficulty director** — one `Difficulty` value (0→1) driven by distance, survival time, and recent deaths (mercy). Every jump/gap it generates is checked against the cat's real jump arc, so nothing impossible ever spawns.
- **Progression** — best distance + best coins saved locally (`PlayerPrefs`). No accounts, no server.

## Project layout

| Script | Responsibility |
|---|---|
| `GameBootstrap` | Builds the whole scene at runtime — camera, backdrop, mic, cat, chaser, level, HUD. Only thing that needs to be in the scene. |
| `GameManager` | State machine (`Calibrating → Ready → Playing → Dead`), distance/coin tracking, combo system, best-score saves. |
| `VoicePlayer` | The cat — movement, jump/double-jump/hold physics, power-up timers, death. |
| `MicVoiceInput` | Mic capture, noise-floor calibration, burst/hold events, keyboard fallback. |
| `ChaserHollow` | Chaser AI + `Pressure` value. |
| `LevelDirector` | Chunk streaming, difficulty curve, chunk-type menu (incl. `Fork`). |
| `ChunkBuilder` | Low-level geometry — ground, platforms, blocks, hazards, coins, power-ups. |
| `Traps` | Spikes, chompers, fake/falling/invisible blocks, crushers. |
| `PowerUps` | Pickup + timer logic for the three buffs. |
| `ForwardEnemy` | Enemies that move independently of their spawn chunk. |
| `RunnerCamera` | Follow cam, parallax backdrop, screen-shake. |
| `RunnerHUD` | Runtime `OnGUI` HUD — distance, coin badge + combo tag, power-up timers, mic meter. |
| `SpriteFactory` | Draws every placeholder sprite in code. |
| `AssetLibrary` / `AudioLibrary` | Optional slots for real sprites/audio — leave empty, game stays fully playable. |
| `Sfx` | SFX + music playback wrapper. |
| `VRLayers` | Physics layers + shared `DeathCause` enum. |

## Running it

1. Unity 6+, 2D + URP.
2. Empty scene → empty GameObject → attach `GameBootstrap`.
3. Press Play. Grant mic permission if asked (optional — Space/↑ always works).

#PPT LINK :-
https://drive.google.com/file/d/1O1NgR7DtPtmCV60MqhbHyIFAWsff69he/view?usp=sharing
