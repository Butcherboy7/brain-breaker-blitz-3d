using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class IQConfig
{
    public string label;
    public float ballSpeed;
    public int   brickStrength;
    public int   rows;
    public int   cols;
    public float bonusChance;
    public float paddleWidth;
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public GameObject brickPrefab;
    public Transform  brickContainer;

    // Camera at z=-13, FOV 60, 16:9 → half-height ≈ 7.5u, half-width ≈ 13.3u
    // Keep walls well inside visible bounds.
    const float WALL_X   =  8.5f;
    const float WALL_TOP =  6.5f;
    const float WALL_H   = 20f;

    public Dictionary<int, IQConfig> iqConfigs = new Dictionary<int, IQConfig>()
    {
        { 1, new IQConfig { label="Beginner",    ballSpeed= 6f,  brickStrength=1, rows=3, cols=6,  bonusChance=0.30f, paddleWidth=3.2f } },
        { 2, new IQConfig { label="Average",     ballSpeed= 9f,  brickStrength=2, rows=4, cols=7,  bonusChance=0.25f, paddleWidth=2.8f } },
        { 3, new IQConfig { label="Smart",       ballSpeed=12f,  brickStrength=3, rows=5, cols=8,  bonusChance=0.20f, paddleWidth=2.2f } },
        { 4, new IQConfig { label="Genius",      ballSpeed=15f,  brickStrength=4, rows=6, cols=9,  bonusChance=0.15f, paddleWidth=1.8f } },
        { 5, new IQConfig { label="Mastermind",  ballSpeed=18f,  brickStrength=5, rows=7, cols=10, bonusChance=0.10f, paddleWidth=1.4f } },
    };

    static readonly Color[] BrickColors =
    {
        new Color(0f, 1f, 1f),       // Cyan
        new Color(1f, 0f, 1f),       // Magenta
        new Color(1f, 1f, 0f),       // Yellow
        new Color(0f, 1f, 0f),       // Green
        new Color(1f, 0.45f, 0f),    // Orange
        new Color(0.5f, 0.5f, 1f),   // Indigo
        new Color(1f, 0.2f, 0.2f),   // Red
    };

    static readonly string[] BonusTypes =
        { "multiball", "wide_paddle", "extra_life", "score_x2", "slow_ball" };

    static readonly Dictionary<string, Color> BonusColors =
        new Dictionary<string, Color>()
        {
            { "multiball",   Color.magenta },
            { "wide_paddle", Color.green   },
            { "extra_life",  Color.red     },
            { "score_x2",    Color.yellow  },
            { "slow_ball",   Color.cyan    },
        };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // ── Set global bounce threshold so low-speed balls still bounce ──
        // (Key Unity setting referenced in all brick-breaker tutorials)
        Physics.bounceThreshold = 0f;
    }

    // ─────────────────────────────────────────────────────
    public void GenerateLevel(int iqLevel, int gameLevel)
    {
        if (brickContainer == null)
            brickContainer = new GameObject("BrickContainer").transform;

        // Clear old bricks
        foreach (Transform t in brickContainer)
            Destroy(t.gameObject);

        // Ensure walls exist
        EnsureWalls();

        IQConfig cfg  = iqConfigs[iqLevel];
        int rows = Mathf.Min(cfg.rows + (gameLevel - 1) / 2, 9);
        int cols = Mathf.Min(cfg.cols + (gameLevel - 1) / 3, 12);

        // Layout bricks to fit within ±WALL_X
        float totalW  = (WALL_X * 2f) * 0.85f;   // 85% of available width
        float brickW  = totalW / cols;
        float brickH  = 0.55f;
        float startX  = -(totalW - brickW) / 2f;
        float startY  = WALL_TOP - 0.6f;           // just below ceiling wall

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float zOff = (iqLevel == 5)
                    ? Mathf.Sin((r + c) * 0.7f) * 0.4f  // Mastermind 3D waves
                    : 0f;

                Vector3 pos = new Vector3(
                    startX + c * brickW,
                    startY - r * (brickH + 0.08f),
                    zOff);

                GameObject go = Instantiate(brickPrefab, pos, Quaternion.identity, brickContainer);
                go.transform.localScale = new Vector3(brickW * 0.92f, brickH, 0.35f);

                bool   isBonus  = Random.value < cfg.bonusChance;
                string bonusT   = isBonus ? BonusTypes[Random.Range(0, BonusTypes.Length)] : "";
                int    health   = isBonus ? 1 : Mathf.Max(1, cfg.brickStrength + r / 2);
                Color  col      = isBonus ? BonusColors[bonusT] : BrickColors[r % BrickColors.Length];

                go.GetComponent<Brick>().Initialize(health, isBonus, col, bonusT);
            }
        }
    }

    // ─────────────────────────────────────────────────────
    void EnsureWalls()
    {
        CreateWallIfMissing("WallLeft",  new Vector3(-WALL_X - 0.25f, 0, 0), new Vector3(0.5f, WALL_H, 1f));
        CreateWallIfMissing("WallRight", new Vector3( WALL_X + 0.25f, 0, 0), new Vector3(0.5f, WALL_H, 1f));
        CreateWallIfMissing("WallTop",   new Vector3(0,  WALL_TOP + 0.25f, 0), new Vector3(WALL_X * 2f + 1f, 0.5f, 1f));
    }

    void CreateWallIfMissing(string wallName, Vector3 pos, Vector3 scale)
    {
        if (GameObject.Find(wallName)) return;

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.position   = pos;
        wall.transform.localScale = scale;

        // Bouncy wall physics
        var pm = new PhysicMaterial(wallName + "_Mat")
        {
            bounciness      = 1f,
            dynamicFriction = 0f,
            staticFriction  = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum,
            bounceCombine   = PhysicMaterialCombine.Maximum
        };
        wall.GetComponent<BoxCollider>().material = pm;

        // Dark translucent wall color
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.2f, 0.2f, 0.35f, 0.8f);
        wall.GetComponent<MeshRenderer>().material = mat;
    }
}
