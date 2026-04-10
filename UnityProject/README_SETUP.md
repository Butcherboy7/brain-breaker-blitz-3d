# Brain Breaker Blitz - Unity Setup Guide

This project was converted from a lovable.ai web project.

## How to Quickstart:
1. Add this folder to **Unity Hub**.
2. Open the project (Unity 2022.3.x recommended).
3. In the project tab, go to `Assets/Scripts`:
   - Ensure all scripts compile without errors.
4. Go to `Assets/Prefabs`:
   - Drag the `Brick` prefab into your scene (or it will be spawned by `LevelManager`).
5. Set up a **GameScene**:
   - Create an Empty Object called `GameManager` and attach `GameManager.cs`.
   - Create an Empty Object called `LevelManager` and attach `LevelManager.cs`.
   - Create a Sphere for the `Ball` and attach `BallController.cs`.
   - Create a Cube for the `Paddle` and attach `PaddleController.cs`.
   - Setup a `DeadZone` (Cube with Trigger) below the paddle with the tag "DeadZone".
   - Setup a UI Canvas with Buttons for Level Selection and Text for Score/Lives.

## Project Structure:
- `Assets/Scripts`: Core game logic.
- `Assets/Prefabs`: Reusable game objects.
- `ProjectSettings`: Tag and Input configurations.
- `Packages`: Dependency manifest.
