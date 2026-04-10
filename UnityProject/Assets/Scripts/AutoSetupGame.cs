using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// AutoSetupGame — Cyberpunk Neon Edition.
/// Attach to ANY empty object in the scene.
/// Assign the Brick prefab, then click AUTO SETUP SCENE.
/// Everything is created in code — no external assets required.
/// Press PLAY and you're good to go.
/// </summary>
public class AutoSetupGame : MonoBehaviour
{
    [Tooltip("Assign your Brick prefab here before clicking Auto Setup")]
    public GameObject brickPrefab;

    public void SetupScene()
    {
        Application.targetFrameRate = 60;
        Time.fixedDeltaTime         = 1f / 60f;
        Physics.bounceThreshold     = 0f;

        // ── Camera ─────────────────────────────────────────────
        Camera cam = Camera.main;
        if (cam == null)
        {
            var co = new GameObject("Main Camera");
            cam = co.AddComponent<Camera>();
            co.tag = "MainCamera";
        }
        cam.transform.position  = new Vector3(0f, 1f, -13f);
        cam.transform.rotation  = Quaternion.Euler(8f, 0f, 0f);
        cam.backgroundColor     = new Color(0.039f, 0.031f, 0.078f); // Cyberpunk deep dark
        cam.clearFlags          = CameraClearFlags.SolidColor;
        cam.fieldOfView         = 60f;

        // ── CameraController ────────────────────────────────────
        if (cam.GetComponent<CameraController>() == null)
            cam.gameObject.AddComponent<CameraController>();

        // ── AudioManager ────────────────────────────────────────
        EnsureSingleton<AudioManager>("AudioManager");

        // ── BackgroundManager ───────────────────────────────────
        EnsureSingleton<BackgroundManager>("BackgroundManager");

        // ── Directional Light ───────────────────────────────────
        DestroyImmediate(GameObject.Find("Directional Light"));
        var lo = new GameObject("Directional Light");
        var lt = lo.AddComponent<Light>();
        lt.type      = LightType.Directional;
        lt.color     = new Color(0.6f, 0.5f, 0.9f);
        lt.intensity = 0.8f;
        lo.transform.rotation = Quaternion.Euler(40f, -55f, 0f);

        // Ambient
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.02f, 0.12f);

        // ── LevelManager ────────────────────────────────────────
        var lmGO = GameObject.Find("LevelManager") ?? new GameObject("LevelManager");
        var lm   = lmGO.GetComponent<LevelManager>() ?? lmGO.AddComponent<LevelManager>();
        if (lm.brickContainer == null)
            lm.brickContainer = (new GameObject("BrickContainer")).transform;
        lm.brickPrefab = brickPrefab;

        // ── GameManager ─────────────────────────────────────────
        var gmGO = GameObject.Find("GameManager") ?? new GameObject("GameManager");
        var gm   = gmGO.GetComponent<GameManager>() ?? gmGO.AddComponent<GameManager>();

        // ── Paddle ──────────────────────────────────────────────
        var padGO = GameObject.Find("Paddle");
        if (padGO == null)
        {
            padGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            padGO.name = "Paddle";
            padGO.tag  = "Paddle";
        }
        padGO.transform.localScale = new Vector3(2.8f, 0.4f, 0.6f);
        padGO.transform.position   = new Vector3(0f, -4.5f, 0f);

        var padPM = new PhysicMaterial("PaddlePM")
        {
            bounciness      = 1f,
            dynamicFriction = 0f,
            staticFriction  = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum,
            bounceCombine   = PhysicMaterialCombine.Maximum,
        };
        padGO.GetComponent<BoxCollider>().material = padPM;
        var pc = padGO.GetComponent<PaddleController>() ?? padGO.AddComponent<PaddleController>();
        gm.paddle = pc;

        // ── Ball ────────────────────────────────────────────────
        var ballGO = GameObject.Find("Ball");
        if (ballGO == null)
        {
            ballGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballGO.name = "Ball";
            ballGO.tag  = "Ball";
        }
        ballGO.transform.localScale = Vector3.one * 0.45f;
        ballGO.transform.position   = new Vector3(0f, -3.5f, 0f);

        // Let BallController handle Rigidbody setup via [RequireComponent]

        var pm = new PhysicMaterial("BallPM")
        {
            bounciness      = 1f,
            dynamicFriction = 0f,
            staticFriction  = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum,
            bounceCombine   = PhysicMaterialCombine.Maximum,
        };
        ballGO.GetComponent<SphereCollider>().material = pm;

        var bc = ballGO.GetComponent<BallController>() ?? ballGO.AddComponent<BallController>();
        gm.ball = bc;

        // ── Dead Zone ───────────────────────────────────────────
        DestroyImmediate(GameObject.Find("DeadZone"));
        var dz = new GameObject("DeadZone");
        dz.tag = "DeadZone";
        dz.transform.position   = new Vector3(0f, -7f, 0f);
        dz.transform.localScale = new Vector3(40f, 1f, 5f);
        var dzc = dz.AddComponent<BoxCollider>();
        dzc.isTrigger = true;

        // ── Remove old walls (LevelManager recreates neon ones) ─
        foreach (var wn in new[] { "WallLeft", "WallRight", "WallTop" })
        {
            var w = GameObject.Find(wn);
            if (w) DestroyImmediate(w);
        }

        Debug.Log("✅ Cyberpunk Neon Setup complete! Press PLAY to launch the game.");
    }

    // Helper: find or create a singleton GameObject
    static T EnsureSingleton<T>(string name) where T : MonoBehaviour
    {
        var go = GameObject.Find(name) ?? new GameObject(name);
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AutoSetupGame))]
public class AutoSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(8);
        var style = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 14,
            fontStyle = FontStyle.Bold,
        };
        style.normal.textColor  = Color.cyan;
        style.hover.textColor   = Color.white;
        if (GUILayout.Button("▶  AUTO SETUP SCENE  ◀", style, GUILayout.Height(46)))
            ((AutoSetupGame)target).SetupScene();
    }
}
#endif
