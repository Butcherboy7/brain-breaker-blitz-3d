using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Attach to ANY empty object in the scene.
/// Assign the Brick prefab, then click AUTO SETUP SCENE.
/// That's it — click Play and the game runs.
/// </summary>
public class AutoSetupGame : MonoBehaviour
{
    public GameObject brickPrefab;

    public void SetupScene()
    {
        // 60 FPS + physics settings
        Application.targetFrameRate = 60;
        Time.fixedDeltaTime         = 1f / 60f;
        Physics.bounceThreshold     = 0f;    // essential for brick breaker

        // ── Camera ─────────────────────────────────────
        Camera cam = Camera.main;
        if (cam == null)
        {
            var co = new GameObject("Main Camera");
            cam = co.AddComponent<Camera>();
            co.tag = "MainCamera";
        }
        cam.transform.position = new Vector3(0, 1f, -13f);
        cam.transform.rotation = Quaternion.Euler(8, 0, 0);
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.12f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.fieldOfView = 60;

        // ── Lighting ────────────────────────────────────
        DestroyImmediate(GameObject.Find("Directional Light"));
        var lo = new GameObject("Directional Light");
        var lt = lo.AddComponent<Light>();
        lt.type = LightType.Directional;
        lt.color = new Color(0.85f, 0.85f, 1f);
        lt.intensity = 1.3f;
        lo.transform.rotation = Quaternion.Euler(40, -55, 0);

        // Ambient
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.1f, 0.1f, 0.25f);

        // ── GameManager ─────────────────────────────────
        var gmGO = GameObject.Find("GameManager") ?? new GameObject("GameManager");
        GameManager gm = gmGO.GetComponent<GameManager>() ?? gmGO.AddComponent<GameManager>();

        // ── LevelManager ────────────────────────────────
        var lmGO = GameObject.Find("LevelManager") ?? new GameObject("LevelManager");
        LevelManager lm = lmGO.GetComponent<LevelManager>() ?? lmGO.AddComponent<LevelManager>();
        if (lm.brickContainer == null)
            lm.brickContainer = (new GameObject("BrickContainer")).transform;
        lm.brickPrefab = brickPrefab;

        // ── Paddle ──────────────────────────────────────
        var padGO = GameObject.Find("Paddle");
        if (padGO == null)
        {
            padGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            padGO.name = "Paddle";
            padGO.tag  = "Paddle";
        }
        padGO.transform.localScale = new Vector3(2.8f, 0.4f, 0.6f);
        padGO.transform.position   = new Vector3(0, -4.5f, 0);

        // Bouncy paddle collider
        var padPM = new PhysicMaterial("PaddlePM")
        {
            bounciness = 1f, dynamicFriction = 0f, staticFriction = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum,
            bounceCombine   = PhysicMaterialCombine.Maximum,
        };
        padGO.GetComponent<BoxCollider>().material = padPM;
        PaddleController pc = padGO.GetComponent<PaddleController>() ?? padGO.AddComponent<PaddleController>();
        gm.paddle = pc;

        // ── Ball ────────────────────────────────────────
        var ballGO = GameObject.Find("Ball");
        if (ballGO == null)
        {
            ballGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballGO.name = "Ball";
            ballGO.tag  = "Ball";
        }
        ballGO.transform.localScale = Vector3.one * 0.45f;
        ballGO.transform.position   = new Vector3(0, -3.5f, 0);

        var rb = ballGO.GetComponent<Rigidbody>();
        if (rb == null) rb = ballGO.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        BallController bc = ballGO.GetComponent<BallController>() ?? ballGO.AddComponent<BallController>();
        gm.ball = bc;

        // ── Dead Zone ────────────────────────────────────
        DestroyImmediate(GameObject.Find("DeadZone"));
        var dz = new GameObject("DeadZone");
        dz.tag = "DeadZone";
        dz.transform.position   = new Vector3(0, -7f, 0);
        dz.transform.localScale = new Vector3(40, 1, 5);
        var dzc = dz.AddComponent<BoxCollider>();
        dzc.isTrigger = true;

        // ── Remove old walls – LevelManager will recreate correctly ─
        foreach (var wn in new[]{ "WallLeft","WallRight","WallTop" })
        {
            var w = GameObject.Find(wn);
            if (w) DestroyImmediate(w);
        }

        Debug.Log("✅ Setup complete! Press PLAY.");
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
        if (GUILayout.Button("▶  AUTO SETUP SCENE  ◀", GUILayout.Height(42)))
            ((AutoSetupGame)target).SetupScene();
    }
}
#endif
