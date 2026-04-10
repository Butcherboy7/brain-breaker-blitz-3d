# 🎮 How To Run Brain Breaker Blitz 3D (Cyberpunk Neon Edition)

> **You are new to Unity? No problem. Follow these exact steps and it will work.**

---

## STEP 1 — Install Unity Hub & Unity Editor

1. Go to **https://unity.com/download** and download **Unity Hub** (free).
2. Install Unity Hub, then open it.
3. Click **Installs** → **Install Editor**.
4. Install **Unity 2022.3 LTS** (any 2022.3.xx version works).
   - During install, check **Windows Build Support** (for building to PC).
5. Wait for install to finish (may take 10–30 min).

---

## STEP 2 — Open the Project

1. Open **Unity Hub**.
2. Click **Projects** → **Add** → **Add project from disk**.
3. Navigate to and select the `UnityProject` folder inside `brick braker`.
4. The project will appear in Unity Hub. Click it to open.
5. Unity may take 1–5 minutes to import on first open — this is normal.

---

## STEP 3 — Create the Brick Prefab (one-time setup)

> Unity needs a "Brick" prefab to spawn. Here's how to make it:

1. In Unity, go to **Hierarchy** (left panel) → Right-click → **3D Object** → **Cube**.
2. Rename it to `Brick`.
3. In the **Inspector** (right panel), click **Add Component**.
4. Type `Brick` → click **Brick (Script)**.
5. Also add: **Box Collider** (if not already present).
6. In the **Project** window (bottom), find or create a folder called `Assets/Prefabs`.
7. **Drag** your `Brick` from the Hierarchy into the `Assets/Prefabs` folder.
8. The Brick object turns **blue** in the Hierarchy — that means it's now a Prefab ✅.
9. Delete the original `Brick` from the Hierarchy (right-click → Delete).

---

## STEP 4 — Auto-Setup the Scene

1. In the **Hierarchy**, right-click → **Create Empty** → name it `Setup`.
2. In the **Inspector**, click **Add Component** → type `AutoSetupGame` → add it.
3. You'll see a field called **Brick Prefab**. Drag your `Brick` prefab from the Project window into it.
4. Click the button **▶ AUTO SETUP SCENE ◀** in the Inspector.
5. Unity Console (bottom) should say: `✅ Cyberpunk Neon Setup complete! Press PLAY.`

---

## STEP 5 — Press PLAY 🎮

1. Click the **▶ Play** button at the top center of Unity.
2. The game launches with the **Cyberpunk Neon** main menu.
3. Select your IQ difficulty level and start playing!

### Controls:
| Action | Key |
|--------|-----|
| Move Paddle | Arrow Keys or A/D |
| Launch Ball | Space or Left Click |
| Pause | Escape |

---

## TROUBLESHOOTING

### 🔴 "The name 'BackgroundManager' does not exist"
- Make sure ALL scripts from `Assets/Scripts/` are saved. Unity auto-compiles them.
- If errors persist: **Edit** → **Project Settings** → **Script Compilation** → click **Recompile**.

### 🔴 Ball falls through everything
- Make sure the **Ball** has a `SphereCollider` and `BallController` component.
- Check that `Physics.bounceThreshold = 0` was applied (AutoSetup does this).

### 🔴 No bricks appear
- Make sure you assigned the **Brick Prefab** to the `AutoSetupGame` component before clicking setup.
- In the `LevelManager` in the Hierarchy, verify `Brick Prefab` is assigned.

### 🔴 Game stuck on "3... 2... 1..." forever
- Make sure `Time.timeScale` is not set to 0 elsewhere. Pause should only be triggered by Escape.

### 🔴 Compile errors in Unity console
- Close Unity, then re-open. Unity sometimes needs a fresh compile.
- If specific errors remain, copy the error text and ask for help.

---

## WHAT THE CYBERPUNK NEON EDITION INCLUDES

✅ Dark space background (#0A0814) that slowly cycles  
✅ Neon glow on all bricks (8 row-based colors: pink, orange, green, cyan, purple, yellow, red, blue)  
✅ Ball with dual-layer trail that rainbow-shifts at max combo  
✅ Breathing glow pulse on every brick  
✅ Pre-destruction crack strobe before dissolve animation  
✅ Holographic paddle with proximity energy glow  
✅ RGB underglow point light under paddle  
✅ Neon border UI panels with glow edges  
✅ Bounce-in/out combo popup (x2, x3, x5, x10)  
✅ Animated score counter (rolls up visually)  
✅ Ready... 3-2-1-GO countdown before each level  
✅ Red flash overlay on life lost  
✅ IQ selection cards with stat preview bars  
✅ Falling power-up pickups with neon capsule visuals  
✅ Neon grid floor plane  
✅ Floating ambient particles in background  
✅ Procedural audio (no external files needed)  
