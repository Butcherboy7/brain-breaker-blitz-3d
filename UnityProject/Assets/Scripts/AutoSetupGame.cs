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
        // 1. Setup Camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        cam.transform.position = new Vector3(0, 0, -10);
        cam.transform.rotation = Quaternion.Euler(0, 0, 0);

        // 2. Setup Light
        if (GameObject.Find("Directional Light") == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        // 3. Setup Managers
        GameObject gmObj = GameObject.Find("GameManager");
        if (gmObj == null) gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.GetComponent<GameManager>() ?? gmObj.AddComponent<GameManager>();

        GameObject lmObj = GameObject.Find("LevelManager");
        if (lmObj == null) lmObj = new GameObject("LevelManager");
        LevelManager lm = lmObj.GetComponent<LevelManager>() ?? lmObj.AddComponent<LevelManager>();
        
        if (lm.brickContainer == null)
        {
            GameObject container = new GameObject("BrickContainer");
            lm.brickContainer = container.transform;
        }
        lm.brickPrefab = brickPrefab;

        // 4. Setup Paddle
        GameObject paddleObj = GameObject.Find("Paddle");
        if (paddleObj == null)
        {
            paddleObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paddleObj.name = "Paddle";
            paddleObj.tag = "Paddle";
        }
        paddleObj.transform.localScale = new Vector3(2, 0.5f, 0.5f);
        PaddleController pc = paddleObj.GetComponent<PaddleController>() ?? paddleObj.AddComponent<PaddleController>();
        gm.paddle = pc;

        // 5. Setup Ball
        GameObject ballObj = GameObject.Find("Ball");
        if (ballObj == null)
        {
            ballObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballObj.name = "Ball";
            ballObj.tag = "Ball";
        }
        ballObj.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        Rigidbody rb = ballObj.GetComponent<Rigidbody>() ?? ballObj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        BallController bc = ballObj.GetComponent<BallController>() ?? ballObj.AddComponent<BallController>();
        gm.ball = bc;

        // 6. Setup DeadZone
        GameObject dzObj = GameObject.Find("DeadZone");
        if (dzObj == null)
        {
            dzObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dzObj.name = "DeadZone";
            dzObj.tag = "DeadZone";
            dzObj.transform.position = new Vector3(0, -6, 0);
            dzObj.transform.localScale = new Vector3(30, 1, 1);
            dzObj.GetComponent<MeshRenderer>().enabled = false;
            dzObj.GetComponent<BoxCollider>().isTrigger = true;
        }

        Debug.Log("Scene Setup Complete! Just add a UI Canvas and click Start Game.");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AutoSetupGame))]
public class AutoSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        AutoSetupGame myScript = (AutoSetupGame)target;
        if (GUILayout.Button("AUTO SETUP SCENE"))
        {
            myScript.SetupScene();
        }
    }
}
#endif
