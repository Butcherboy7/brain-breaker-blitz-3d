# Project Handoff: Brain Breaker Blitz (Unity 3D)

## Project Overview
This project is a conversion of a lovable.ai React/Three.js Brick Breaker game into a native Unity 3D project. It features 5 difficulty levels based on "IQ" configs.

## Key Components (Unity)
- **GameManager.cs**: Central state controller (Score, Lives, Game Flow).
- **LevelManager.cs**: Handles level generation and IQ-based difficulty scaling.
- **BallController.cs**: Rigidbody-based ball physics and reset logic.
- **PaddleController.cs**: Keyboard/Mouse movement for the paddle.
- **Brick.cs**: Health system and bonus detection.

## Session State Management
- Currently, session state (Score, Lives, Selected IQ) is handled in memory via `GameManager.Instance`.
- **Persistence**: For saving High Scores or Unlocked Levels, use `PlayerPrefs`:
  - `PlayerPrefs.SetInt("HighScore", score);`
  - `PlayerPrefs.Save();`
- **Between Scenes**: Transitioning from `MainMenu` to `GameScene` uses a static variable or a `DontDestroyOnLoad` persistent object.

## Setup Instructions for Unity Hub
1. Open **Unity Hub**.
2. Click **Add** -> **Add project from disk**.
3. Select the `UnityProject` folder.
4. Use Unity **2022.3.10f1** (or any 2022 LTS).
5. Open `Assets/Scenes/MainScene` (if created) or create a new scene and hook up the scripts.

## TODO / Next Steps
- Implement the UI Canvas (MainMenu, LevelSelect, HUD) using the provided `GameManager` methods.
- Refine 3D lighting and materials for the "Mastermind" level depth.
- Add sound effects for collisions and brick breaks.

## Developer Context
- The original logic from `src/game/useBrickBreaker.ts` has been ported to C#.
- Difficulty configurations are identical to the original web version.
- GUID for `Brick.cs` is hardcoded as `88888888888888888888888888888801` to match the prefab.
