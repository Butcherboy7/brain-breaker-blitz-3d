# 🚀 How to Run Your 3D Brick Breaker

Follow these steps exactly, even if you've never used Unity before!

### Step 1: Install Unity (If you haven't)
1. Download **Unity Hub** from [unity.com](https://unity.com/download).
2. Install a version (I recommend **Unity 2022 LTS**).

### Step 2: Open the Project
1. Open **Unity Hub**.
2. Click the **Add** button at the top right -> **Add project from disk**.
3. Select the folder named `UnityProject` inside `brain-breaker-blitz-main`.
4. Click on the project name to open it. (Wait a few minutes for it to load).

### Step 3: One-Click Scene Setup
Once Unity is open:
1. In the bottom "Project" tab, go to `Assets` -> `Scripts`.
2. Right-click in the "Hierarchy" tab (the list on the left) and select **Create Empty**. Name it `GameBootstrapper`.
3. Drag the `AutoSetupGame.cs` script from the bottom tab onto your `GameBootstrapper` object.
4. With `GameBootstrapper` selected, look at the "Inspector" tab (on the right).
5. Drag the **Brick** prefab from `Assets` -> `Prefabs` into the **Brick Prefab** slot in the script.
6. Click the big button that says **AUTO SETUP SCENE**.
   * *Magic happens: It creates the camera, light, paddle, ball, and level managers for you!*

### Step 4: Play!
1. Press the **Play** button (the triangle ▶️) at the top of the Unity window.
2. Use **Left/Right Arrows** or **A/D** keys to move the paddle.
3. Use **Space** to launch the ball.

### Pro Tip (The UI):
The scripts are ready for a menu. To add one:
1. Right-click in Hierarchy -> **UI** -> **Canvas**.
2. Add buttons and link them to `GameManager.StartGame(1)` for Beginner, etc.
3. But for now, you can just test the physics and logic immediately!

---
**Need help?** Just ask! I've made the code robust so it shouldn't crash.
