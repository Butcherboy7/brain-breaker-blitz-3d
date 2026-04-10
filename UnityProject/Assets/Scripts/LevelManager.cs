using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class IQConfig
{
    public string label;
    public float ballSpeed;
    public int brickStrength;
    public int rows;
    public int cols;
    public float bonusChance;
    public float paddleWidth;
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public GameObject brickPrefab;
    public Transform brickContainer;

    public Dictionary<int, IQConfig> iqConfigs = new Dictionary<int, IQConfig>()
    {
        { 1, new IQConfig { label="Beginner (IQ 80)",   ballSpeed=5f,  brickStrength=1, rows=3, cols=6,  bonusChance=0.30f, paddleWidth=3.5f } },
        { 2, new IQConfig { label="Average (IQ 100)",   ballSpeed=8f,  brickStrength=2, rows=4, cols=7,  bonusChance=0.25f, paddleWidth=2.8f } },
        { 3, new IQConfig { label="Smart (IQ 120)",     ballSpeed=11f, brickStrength=3, rows=5, cols=8,  bonusChance=0.20f, paddleWidth=2.2f } },
        { 4, new IQConfig { label="Genius (IQ 140)",    ballSpeed=14f, brickStrength=4, rows=6, cols=9,  bonusChance=0.15f, paddleWidth=1.8f } },
        { 5, new IQConfig { label="Mastermind (IQ 160+)",ballSpeed=18f, brickStrength=5, rows=7, cols=10, bonusChance=0.10f, paddleWidth=1.4f } },
    };

    static readonly Color[] BrickColors = {
        new Color(0f, 1f, 1f),       // Cyan
        new Color(1f, 0f, 1f),       // Magenta
        new Color(1f, 1f, 0f),       // Yellow
        new Color(0f, 1f, 0f),       // Green
        new Color(1f, 0.4f, 0f),     // Orange
        new Color(0.4f, 0.4f, 1f),   // Indigo
        new Color(1f, 0.2f, 0.2f),   // Red
    };

    static readonly string[] BonusTypes = { "multiball", "wide_paddle", "extra_life", "score_x2", "slow_ball" };

    static readonly Dictionary<string, Color> BonusColors = new Dictionary<string, Color>()
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
    }

    public void GenerateLevel(int iqLevel, int gameLevel)
    {
        if (brickContainer == null)
        {
            GameObject c = new GameObject("BrickContainer");
            brickContainer = c.transform;
        }

        // Clear old
        foreach (Transform child in brickContainer)
            Destroy(child.gameObject);

        IQConfig config = iqConfigs[iqLevel];
        int rows = Mathf.Min(config.rows + (gameLevel - 1) / 2, 9);
        int cols = Mathf.Min(config.cols + (gameLevel - 1) / 3, 12);

        float spacingX = 1.15f;
        float spacingY = 0.62f;
        float startX = -(cols - 1) * spacingX / 2f;
        float startY = 4.5f;

        // Create wall boundaries
        SetupWalls();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float zOffset = 0f;
                if (iqLevel == 5) // Mastermind: 3D depth
                    zOffset = Mathf.Sin(r + c * 0.5f) * 0.5f;

                Vector3 pos = new Vector3(startX + c * spacingX, startY - r * spacingY, zOffset);
                GameObject brickObj = Instantiate(brickPrefab, pos, Quaternion.identity, brickContainer);

                Brick brick = brickObj.GetComponent<Brick>();
                bool isBonus = Random.value < config.bonusChance;
                string bonusType = isBonus ? BonusTypes[Random.Range(0, BonusTypes.Length)] : "";
                int health = isBonus ? 1 : Mathf.Max(1, config.brickStrength + r / 2);

                Color col = isBonus ? BonusColors[bonusType] : BrickColors[r % BrickColors.Length];
                brick.Initialize(health, isBonus, col, bonusType);

                // Scale bricks slightly
                brickObj.transform.localScale = new Vector3(spacingX * 0.88f, spacingY * 0.82f, 0.4f);
            }
        }
    }

    void SetupWalls()
    {
        string[] wallNames = { "WallLeft", "WallRight", "WallTop" };
        if (GameObject.Find("WallLeft") != null) return; // Already created

        // Left
        CreateWall("WallLeft",  new Vector3(-9f, 0, 0), new Vector3(0.5f, 20, 1));
        // Right
        CreateWall("WallRight", new Vector3(9f,  0, 0), new Vector3(0.5f, 20, 1));
        // Top
        CreateWall("WallTop",   new Vector3(0, 7f, 0),  new Vector3(20f, 0.5f, 1));
    }

    void CreateWall(string name, Vector3 pos, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        wall.GetComponent<MeshRenderer>().material.color = new Color(0.2f, 0.2f, 0.35f);
        // No Tag needed — ball will just bounce off collider naturally
    }
}
