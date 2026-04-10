using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int score = 0;
    public int lives = 3;
    public int currentLevel = 1;
    public int selectedIQ = 2;
    public bool isPlaying = false;

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI levelText;
    public GameObject gameOverPanel;
    public GameObject winPanel;
    public GameObject mainMenuPanel;
    public GameObject levelSelectPanel;

    public BallController ball;
    public PaddleController paddle;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        isPlaying = false;
        mainMenuPanel.SetActive(true);
        levelSelectPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);
    }

    public void ShowLevelSelect()
    {
        mainMenuPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
    }

    public void StartGame(int iq)
    {
        selectedIQ = iq;
        currentLevel = 1;
        score = 0;
        lives = 3;
        
        mainMenuPanel.SetActive(false);
        levelSelectPanel.SetActive(false);
        
        LoadLevel();
    }

    void LoadLevel()
    {
        isPlaying = true;
        LevelManager.Instance.GenerateLevel(selectedIQ, currentLevel);
        UpdateUI();
        ResetBallAndPaddle();
    }

    public void ResetBallAndPaddle()
    {
        float speed = LevelManager.Instance.iqConfigs[selectedIQ].ballSpeed;
        float speedMult = 1f + (currentLevel - 1) * 0.1f;
        ball.ResetBall(speed * speedMult);
        paddle.ResetPaddle();
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();
        
        // Check for win
        if (GameObject.FindGameObjectsWithTag("Brick").Length <= 1) // One brick left (the one being destroyed)
        {
            WinLevel();
        }
    }

    public void LoseLife()
    {
        lives--;
        UpdateUI();
        if (lives <= 0)
        {
            GameOver();
        }
        else
        {
            ResetBallAndPaddle();
        }
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = "Score: " + score;
        if (livesText) livesText.text = "Lives: " + lives;
        if (levelText) levelText.text = "Level: " + currentLevel;
    }

    void WinLevel()
    {
        isPlaying = false;
        currentLevel++;
        LoadLevel();
    }

    void GameOver()
    {
        isPlaying = false;
        gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        StartGame(selectedIQ);
    }
}
