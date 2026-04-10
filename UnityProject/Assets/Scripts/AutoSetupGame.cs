using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AutoSetupGame : MonoBehaviour
{
    [Header("Settings")]
    public GameObject brickPrefab;

    public void SetupScene()
    {
        // ----- Camera -----
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camObj = new GameObject("Main Camera"); 
            cam = camObj.AddComponent<Camera>(); 
            camObj.tag = "MainCamera";
        }
        cam.transform.position = new Vector3(0, 1, -13);
        cam.transform.rotation = Quaternion.Euler(10, 0, 0);
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.12f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        // ----- Lighting -----
        if (GameObject.Find("Directional Light") == null)
        {
            var lightObj = new GameObject("Directional Light");
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(0.8f, 0.8f, 1f);
            l.intensity = 1.2f;
            lightObj.transform.rotation = Quaternion.Euler(45, -60, 0);
        }

        // ----- GameManager -----
        var gmObj = GameObject.Find("GameManager") ?? new GameObject("GameManager");
        GameManager gm = gmObj.GetComponent<GameManager>() ?? gmObj.AddComponent<GameManager>();

        // ----- LevelManager -----
        var lmObj = GameObject.Find("LevelManager") ?? new GameObject("LevelManager");
        LevelManager lm = lmObj.GetComponent<LevelManager>() ?? lmObj.AddComponent<LevelManager>();
        if (lm.brickContainer == null)
        {
            lm.brickContainer = new GameObject("BrickContainer").transform;
        }
        lm.brickPrefab = brickPrefab;

        // ----- Paddle -----
        var paddleObj = GameObject.Find("Paddle");
        if (paddleObj == null)
        {
            paddleObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paddleObj.name = "Paddle";
            paddleObj.tag = "Paddle";
        }
        paddleObj.transform.localScale = new Vector3(2.8f, 0.45f, 0.45f);
        paddleObj.transform.position = new Vector3(0, -4.5f, 0);
        PaddleController pc = paddleObj.GetComponent<PaddleController>() ?? paddleObj.AddComponent<PaddleController>();
        gm.paddle = pc;

        // ----- Ball -----
        var ballObj = GameObject.Find("Ball");
        if (ballObj == null)
        {
            ballObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballObj.name = "Ball";
            ballObj.tag = "Ball";
        }
        ballObj.transform.localScale = Vector3.one * 0.5f;
        ballObj.transform.position = new Vector3(0, -3.5f, 0);

        Rigidbody rb = ballObj.GetComponent<Rigidbody>();
        if (rb == null) rb = ballObj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        PhysicMaterial ballPhysics = new PhysicMaterial("BallBounce");
        ballPhysics.bounciness = 1f;
        ballPhysics.frictionCombine = PhysicMaterialCombine.Minimum;
        ballPhysics.bounceCombine = PhysicMaterialCombine.Maximum;
        ballObj.GetComponent<SphereCollider>().material = ballPhysics;

        BallController bc = ballObj.GetComponent<BallController>() ?? ballObj.AddComponent<BallController>();
        gm.ball = bc;

        // ----- DeadZone -----
        var dzObj = GameObject.Find("DeadZone");
        if (dzObj == null)
        {
            dzObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dzObj.name = "DeadZone";
            dzObj.tag = "DeadZone";
        }
        dzObj.transform.position = new Vector3(0, -6.5f, 0);
        dzObj.transform.localScale = new Vector3(30, 1, 5);
        dzObj.GetComponent<MeshRenderer>().enabled = false;
        var dzCol = dzObj.GetComponent<BoxCollider>();
        dzCol.isTrigger = true;

        Debug.Log("✅ Auto-Setup Complete. Click PLAY!");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AutoSetupGame))]
public class AutoSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("🚀 AUTO SETUP SCENE", GUILayout.Height(40)))
        {
            ((AutoSetupGame)target).SetupScene();
        }
    }
}
#endif
