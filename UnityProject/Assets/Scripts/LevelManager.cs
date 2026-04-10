using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class IQConfig
{
    public string label;
    public float  ballSpeed;
    public int    brickStrength;
    public int    rows;
    public int    cols;
    public float  bonusChance;
    public float  paddleWidth;
    public float  movingChance;
}

/// <summary>
/// LevelManager — Cyberpunk Neon Edition.
/// Generates levels with neon row colors, neon-styled walls,
/// and cascade spawn delays for visual flair.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public GameObject brickPrefab;
    public Transform  brickContainer;

    const float WALL_X   =  8.5f;
    const float WALL_TOP =  6.5f;
    const float WALL_H   =  20f;

    // IQ Configs — exact specs from design doc
    public Dictionary<int, IQConfig> iqConfigs = new Dictionary<int, IQConfig>()
    {
        { 1, new IQConfig { label="Beginner",   ballSpeed= 8f,  brickStrength=1, rows=4, cols=8,  bonusChance=0.15f, paddleWidth=4f,   movingChance=0f   } },
        { 2, new IQConfig { label="Average",    ballSpeed=12f,  brickStrength=2, rows=5, cols=10, bonusChance=0.15f, paddleWidth=3f,   movingChance=0f   } },
        { 3, new IQConfig { label="Smart",      ballSpeed=16f,  brickStrength=3, rows=6, cols=12, bonusChance=0.15f, paddleWidth=2.5f, movingChance=0.10f} },
        { 4, new IQConfig { label="Genius",     ballSpeed=20f,  brickStrength=4, rows=7, cols=14, bonusChance=0.15f, paddleWidth=2f,   movingChance=0.25f} },
        { 5, new IQConfig { label="Mastermind", ballSpeed=25f,  brickStrength=5, rows=8, cols=16, bonusChance=0.15f, paddleWidth=1.5f, movingChance=0.40f} },
    };

    // Power-up types available from bonus bricks
    static readonly string[] BonusTypes =
        { "multiball", "wide_paddle", "extra_life", "score_x2", "slow_ball", "fireball", "shrink_paddle", "sticky" };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        Physics.bounceThreshold = 0f;
    }

    // ─────────────────────────────────────────────────────
    public void GenerateLevel(int iqLevel, int gameLevel)
    {
        if (brickContainer == null)
            brickContainer = new GameObject("BrickContainer").transform;

        // Clear old bricks
        foreach (Transform t in brickContainer) Destroy(t.gameObject);
        Brick.ResetCount();

        EnsureNeonWalls();

        IQConfig cfg = iqConfigs[iqLevel];
        int rows = Mathf.Min(cfg.rows + (gameLevel - 1) / 2, 9);
        int cols = Mathf.Min(cfg.cols + (gameLevel - 1) / 3, 16);

        float totalW  = (WALL_X * 2f) * 0.85f;
        float brickW  = totalW / cols;
        float brickH  = 0.55f;
        float startX  = -(totalW - brickW) / 2f;
        float startY  = WALL_TOP - 0.6f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float zOff = (iqLevel == 5)
                    ? Mathf.Sin((r + c) * 0.7f) * 0.4f
                    : 0f;

                Vector3 pos = new Vector3(
                    startX + c * brickW,
                    startY - r * (brickH + 0.1f),
                    zOff);

                if (brickPrefab == null)
                {
                    Debug.LogError("Brick Prefab is not assigned in LevelManager!");
                    return;
                }

                GameObject go = Instantiate(brickPrefab, pos, Quaternion.identity, brickContainer);
                go.transform.localScale = new Vector3(brickW * 0.92f, brickH, 0.35f);

                // Neon row color
                Color col = NeonVisuals.BrickNeonColors[r % NeonVisuals.BrickNeonColors.Length];

                bool   isBonus = Random.value < cfg.bonusChance;
                string bonusT  = isBonus ? BonusTypes[Random.Range(0, BonusTypes.Length)] : "";
                int    health  = isBonus ? 1 : Mathf.Max(1, cfg.brickStrength + r / 2);

                bool  isMoving = Random.value < cfg.movingChance;
                float ms       = isMoving ? Random.Range(1f, 3f) : 0f;
                float mr       = isMoving ? brickW * 0.45f : 0f;

                go.GetComponent<Brick>().Initialize(health, isBonus, col, bonusT, isMoving, ms, mr);
            }
        }
    }

    // ─────────────────────────────────────────────────────
    void EnsureNeonWalls()
    {
        CreateNeonWall("WallLeft",  new Vector3(-WALL_X - 0.25f, 0, 0), new Vector3(0.5f, WALL_H, 1f), NeonVisuals.NeonPurple);
        CreateNeonWall("WallRight", new Vector3( WALL_X + 0.25f, 0, 0), new Vector3(0.5f, WALL_H, 1f), NeonVisuals.NeonPurple);
        CreateNeonWall("WallTop",   new Vector3(0, WALL_TOP + 0.25f, 0), new Vector3(WALL_X * 2f + 1f, 0.5f, 1f), NeonVisuals.NeonCyan);
    }

    void CreateNeonWall(string wallName, Vector3 pos, Vector3 scale, Color neonColor)
    {
        if (GameObject.Find(wallName)) return;

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.position   = pos;
        wall.transform.localScale = scale;

        var pm = new PhysicMaterial(wallName + "_Mat")
        {
            bounciness      = 1f,
            dynamicFriction = 0f,
            staticFriction  = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum,
            bounceCombine   = PhysicMaterialCombine.Maximum
        };
        wall.GetComponent<BoxCollider>().material = pm;

        // Neon glowing wall material
        var mat = NeonVisuals.MakeNeonMaterial(neonColor, emissionIntensity: 1.5f, metallic: 0.2f, smoothness: 0.7f);
        mat.color = neonColor * 0.1f; // very dark base — let emission do the work
        wall.GetComponent<MeshRenderer>().material = mat;

        // Add a point light along walls (left/right only — top wall has its own)
        if (wallName != "WallTop")
        {
            var lightGO = new GameObject(wallName + "_Light");
            lightGO.transform.SetParent(wall.transform);
            lightGO.transform.localPosition = Vector3.zero;
            var l = lightGO.AddComponent<Light>();
            l.type      = LightType.Point;
            l.color     = neonColor;
            l.range     = 6f;
            l.intensity = 1.5f;
        }
    }
}
