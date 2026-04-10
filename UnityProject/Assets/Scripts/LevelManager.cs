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
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public GameObject brickPrefab;
    public Transform brickContainer;

    public Dictionary<int, IQConfig> iqConfigs = new Dictionary<int, IQConfig>()
    {
        { 1, new IQConfig { label = "Beginner (IQ 80)", ballSpeed = 6f, brickStrength = 1, rows = 3, cols = 6, bonusChance = 0.3f } },
        { 2, new IQConfig { label = "Average (IQ 100)", ballSpeed = 8f, brickStrength = 2, rows = 4, cols = 7, bonusChance = 0.25f } },
        { 3, new IQConfig { label = "Smart (IQ 120)", ballSpeed = 10f, brickStrength = 3, rows = 5, cols = 8, bonusChance = 0.2f } },
        { 4, new IQConfig { label = "Genius (IQ 140)", ballSpeed = 12f, brickStrength = 4, rows = 6, cols = 9, bonusChance = 0.15f } },
        { 5, new IQConfig { label = "Mastermind (IQ 160+)", ballSpeed = 15f, brickStrength = 5, rows = 7, cols = 10, bonusChance = 0.1f } }
    };

    public Color[] brickColors = new Color[]
    {
        Color.cyan, Color.magenta, Color.yellow, Color.green, new Color(1f, 0.4f, 0f), new Color(0f, 0.4f, 1f)
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void GenerateLevel(int iqLevel, int gameLevel)
    {
        // Clear existing bricks
        foreach (Transform child in brickContainer)
        {
            Destroy(child.gameObject);
        }

        IQConfig config = iqConfigs[iqLevel];
        int rows = Mathf.Min(config.rows + (gameLevel / 2), 8);
        int cols = Mathf.Min(config.cols + (gameLevel / 3), 10);

        float spacingX = 1.2f;
        float spacingY = 0.6f;
        float startX = -(cols - 1) * spacingX / 2f;
        float startY = 4f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 pos = new Vector3(startX + c * spacingX, startY - r * spacingY, 0);
                
                // For Mastermind, add some Z depth
                if (iqLevel == 5)
                {
                    pos.z = Random.Range(-0.5f, 0.5f);
                }

                GameObject brickObj = Instantiate(brickPrefab, pos, Quaternion.identity, brickContainer);
                Brick brick = brickObj.GetComponent<Brick>();

                bool isBonus = Random.value < config.bonusChance;
                int health = isBonus ? 1 : config.brickStrength + (r / 2);
                
                Color color = isBonus ? Color.white : brickColors[r % brickColors.Length];
                brick.Initialize(health, isBonus, color);
            }
        }
    }
}
