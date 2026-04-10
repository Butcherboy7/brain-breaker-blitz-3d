using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("State")]
    public int score = 0;
    public int lives = 3;
    public int currentLevel = 1;
    public int selectedIQ = 1;
    public bool isPlaying = false;
    public int combo = 0;
    public int scoreMultiplier = 1;

    [Header("References")]
    public BallController ball;
    public PaddleController paddle;
    public List<BallController> extraBalls = new List<BallController>();

    [Header("UI - HUD")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI iqText;
    public TextMeshProUGUI powerUpText;
    public TextMeshProUGUI launchHint;

    [Header("UI - Panels")]
    public GameObject gameOverPanel;
    public GameObject winPanel;
    public GameObject hudPanel;

    [Header("Screen Shake")]
    private Camera mainCam;
    private Vector3 camDefaultPos;

    [Header("Score Popup")]
    public TextMeshProUGUI scorePopupPrefab;
    private Canvas mainCanvas;

    private List<Coroutine> activePowerUpTimers = new List<Coroutine>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        mainCam = Camera.main;
        if (mainCam != null) camDefaultPos = mainCam.transform.localPosition;
    }

    void Start()
    {
        // Create HUD Canvas
        SetupHUDCanvas();
        StartGame(1);
    }

    void SetupHUDCanvas()
    {
        // We'll create a simple on-screen HUD via script
        GameObject canvasObj = new GameObject("HUD Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();
        mainCanvas = canvas;

        // Dark HUD background at top
        GameObject topBar = MakePanel(canvasObj.transform, new Vector2(0, -30), new Vector2(Screen.width, 60), new Color(0, 0, 0, 0.6f));
        topBar.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
        topBar.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        topBar.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
        topBar.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        topBar.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 60);

        scoreText = MakeText(topBar.transform, new Vector2(-300, 0), "Score: 0", 22, Color.white, TextAlignmentOptions.Left);
        livesText = MakeText(topBar.transform, new Vector2(0, 0), "Lives: ❤️❤️❤️", 22, new Color(1f, 0.4f, 0.4f), TextAlignmentOptions.Center);
        levelText = MakeText(topBar.transform, new Vector2(300, 0), "Level 1", 22, new Color(0.4f, 1f, 0.8f), TextAlignmentOptions.Right);

        iqText = MakeText(canvasObj.transform, new Vector2(0, -75), "Beginner | IQ 80", 18, new Color(0.8f, 0.8f, 0.8f), TextAlignmentOptions.Center);
        iqText.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1f);
        iqText.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1f);

        comboText = MakeText(canvasObj.transform, new Vector2(-350, -80), "", 28, Color.yellow, TextAlignmentOptions.Left);
        comboText.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1f);
        comboText.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1f);

        powerUpText = MakeText(canvasObj.transform, new Vector2(0, 80), "", 30, new Color(0f, 1f, 0.8f), TextAlignmentOptions.Center);
        powerUpText.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
        powerUpText.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);

        launchHint = MakeText(canvasObj.transform, new Vector2(0, 40), "Press SPACE to launch", 24, new Color(1f, 1f, 0.5f), TextAlignmentOptions.Center);
        launchHint.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
        launchHint.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);

        // Game Over Panel
        gameOverPanel = MakePanel(canvasObj.transform, Vector2.zero, new Vector2(400, 250), new Color(0.1f, 0.05f, 0.2f, 0.95f));
        gameOverPanel.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        gameOverPanel.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        MakeText(gameOverPanel.transform, new Vector2(0, 80), "⚡ GAME OVER ⚡", 32, Color.red, TextAlignmentOptions.Center);
        Button restartBtn = MakeButton(gameOverPanel.transform, new Vector2(0, 0), "RETRY", () => StartGame(selectedIQ));
        MakeButton(gameOverPanel.transform, new Vector2(0, -60), "CHANGE LEVEL", () => ShowLevelSelect(gameOverPanel));
        gameOverPanel.SetActive(false);

        // Win Panel
        winPanel = MakePanel(canvasObj.transform, Vector2.zero, new Vector2(400, 250), new Color(0.05f, 0.2f, 0.1f, 0.95f));
        winPanel.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        winPanel.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        MakeText(winPanel.transform, new Vector2(0, 80), "🏆 YOU WIN! 🏆", 32, Color.green, TextAlignmentOptions.Center);
        MakeButton(winPanel.transform, new Vector2(0, 0), "NEXT LEVEL ▶", () => { winPanel.SetActive(false); NextLevel(); });
        winPanel.SetActive(false);

        // Level Select panel visible at start
        ShowLevelSelect(null);
    }

    void ShowLevelSelect(GameObject previous)
    {
        isPlaying = false;
        if (previous != null) previous.SetActive(false);

        GameObject canvasObj = mainCanvas.gameObject;

        // Remove old level select if exists
        GameObject old = GameObject.Find("__LevelSelect");
        if (old != null) Destroy(old);

        GameObject panel = MakePanel(canvasObj.transform, Vector2.zero, new Vector2(500, 450), new Color(0.05f, 0.05f, 0.15f, 0.97f));
        panel.name = "__LevelSelect";
        panel.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        panel.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);

        MakeText(panel.transform, new Vector2(0, 170), "🧠 BRAIN BREAKER BLITZ", 26, new Color(0.6f, 0.4f, 1f), TextAlignmentOptions.Center);
        MakeText(panel.transform, new Vector2(0, 130), "Select Your IQ Level", 18, Color.gray, TextAlignmentOptions.Center);

        string[] labels = { "🟢 Beginner  |  IQ 80", "🔵 Average   |  IQ 100", "🟡 Smart      |  IQ 120", "🔴 Genius    |  IQ 140", "💀 Mastermind  |  IQ 160+" };
        Color[] cols    = { Color.green, Color.cyan, Color.yellow, new Color(1f, 0.4f, 0f), Color.magenta };
        for (int i = 0; i < 5; i++)
        {
            int iq = i + 1;
            float y = 75 - i * 55;
            Color c = cols[i];
            string lbl = labels[i];
            MakeButton(panel.transform, new Vector2(0, y), lbl, () => { Destroy(panel); StartGame(iq); }, c);
        }
    }

    // ===================== Game Flow =====================

    public void StartGame(int iq)
    {
        selectedIQ = iq;
        currentLevel = 1;
        score = 0;
        lives = 3;
        combo = 0;
        scoreMultiplier = 1;

        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);
        LoadLevel();
    }

    void LoadLevel()
    {
        isPlaying = false;
        LevelManager.Instance.GenerateLevel(selectedIQ, currentLevel);
        UpdateUI();

        var config = LevelManager.Instance.iqConfigs[selectedIQ];
        float speedMult = 1f + (currentLevel - 1) * 0.12f;
        float speed = config.ballSpeed * speedMult;
        paddle.SetWidth(config.paddleWidth);
        paddle.ResetPaddle();
        ball.ResetBall(speed);

        isPlaying = true;
        if (launchHint) launchHint.gameObject.SetActive(true);
    }

    void NextLevel()
    {
        currentLevel++;
        LoadLevel();
    }

    // ===================== Events =====================

    public void AddScore(int amount, Vector3 worldPos)
    {
        combo++;
        if (combo >= 5) scoreMultiplier = 3;
        else if (combo >= 3) scoreMultiplier = 2;
        else scoreMultiplier = 1;

        int earned = amount * scoreMultiplier;
        score += earned;

        if (comboText != null)
        {
            if (combo >= 3)
            {
                comboText.text = $"🔥 {combo}x COMBO  x{scoreMultiplier}";
                comboText.color = combo >= 5 ? Color.magenta : Color.yellow;
            }
            else comboText.text = "";
        }

        UpdateUI();
        CheckWin();
    }

    public void LoseLife()
    {
        combo = 0;
        scoreMultiplier = 1;
        if (comboText) comboText.text = "";

        TriggerScreenShake(0.2f, 0.4f);
        lives--;
        UpdateUI();

        if (lives <= 0) GameOver();
        else
        {
            var config = LevelManager.Instance.iqConfigs[selectedIQ];
            float speedMult = 1f + (currentLevel - 1) * 0.12f;
            ball.ResetBall(config.ballSpeed * speedMult);
            paddle.ResetPaddle();
            if (launchHint) launchHint.gameObject.SetActive(true);
        }
    }

    void CheckWin()
    {
        Brick[] remaining = FindObjectsOfType<Brick>();
        if (remaining.Length == 0)
        {
            isPlaying = false;
            winPanel.SetActive(true);
        }
    }

    void GameOver()
    {
        isPlaying = false;
        gameOverPanel.SetActive(true);
    }

    // ===================== Power-Ups =====================

    public void ApplyPowerUp(string type)
    {
        ShowPowerUpMessage(type);
        var config = LevelManager.Instance.iqConfigs[selectedIQ];
        float speedMult = 1f + (currentLevel - 1) * 0.12f;

        switch (type)
        {
            case "extra_life":
                lives++;
                UpdateUI();
                break;
            case "wide_paddle":
                paddle.SetWidth(4f);
                StartCoroutine(RevertPaddleWidth(config.paddleWidth, 8f));
                break;
            case "slow_ball":
                ball.initialSpeed = ball.initialSpeed * 0.5f;
                ball.rb.velocity = ball.rb.velocity.normalized * ball.initialSpeed;
                StartCoroutine(RevertBallSpeed(config.ballSpeed * speedMult, 6f));
                break;
            case "score_x2":
                scoreMultiplier = 2;
                StartCoroutine(RevertMultiplier(10f));
                break;
            case "multiball":
                SpawnExtraBall();
                break;
        }
    }

    void SpawnExtraBall()
    {
        GameObject extra = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        extra.name = "Ball_Extra";
        extra.tag = "Ball";
        extra.transform.position = ball.transform.position;
        extra.transform.localScale = Vector3.one * 0.5f;

        Rigidbody erb = extra.AddComponent<Rigidbody>();
        erb.useGravity = false;
        erb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        erb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        erb.velocity = new Vector3(Random.Range(-0.5f, 0.5f), 1f, 0).normalized * ball.initialSpeed;

        MeshRenderer ren = extra.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.magenta;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.magenta * 2f);
        ren.material = mat;

        BallController ebc = extra.AddComponent<BallController>();
        ebc.initialSpeed = ball.initialSpeed;
        ebc.isLaunched = true;
        ebc.rb = erb;

        // Auto-destroy extra ball after 15 seconds
        Destroy(extra, 15f);
    }

    IEnumerator RevertPaddleWidth(float orig, float delay) { yield return new WaitForSeconds(delay); if (paddle) paddle.SetWidth(orig); }
    IEnumerator RevertBallSpeed(float orig, float delay) { yield return new WaitForSeconds(delay); if (ball) { ball.initialSpeed = orig; ball.rb.velocity = ball.rb.velocity.normalized * orig; } }
    IEnumerator RevertMultiplier(float delay) { yield return new WaitForSeconds(delay); scoreMultiplier = 1; }

    // ===================== UI =====================

    void UpdateUI()
    {
        if (scoreText) scoreText.text = $"Score: {score}";
        if (livesText)
        {
            string hearts = "";
            for (int i = 0; i < lives; i++) hearts += "❤ ";
            livesText.text = hearts.TrimEnd();
        }
        if (levelText) levelText.text = $"Level {currentLevel}";
        if (iqText)
        {
            string[] iqlabels = { "", "Beginner | IQ 80", "Average | IQ 100", "Smart | IQ 120", "Genius | IQ 140", "Mastermind | IQ 160+" };
            iqText.text = iqlabels[selectedIQ];
        }
        if (launchHint && ball) launchHint.gameObject.SetActive(!ball.isLaunched);
    }

    void ShowPowerUpMessage(string type)
    {
        string[] msgs = {
            "extra_life:❤️ +1 Life!",
            "wide_paddle:📏 Wide Paddle!",
            "slow_ball:🐢 Slow Ball!",
            "score_x2:✨ 2x Score!",
            "multiball:⚡ Multi-Ball!"
        };
        foreach (var m in msgs)
        {
            var parts = m.Split(':');
            if (parts[0] == type)
            {
                if (powerUpText)
                {
                    powerUpText.text = parts[1];
                    StartCoroutine(HidePowerUpMsg());
                }
                return;
            }
        }
    }

    IEnumerator HidePowerUpMsg() { yield return new WaitForSeconds(2.5f); if (powerUpText) powerUpText.text = ""; }

    // ===================== Screen Shake =====================

    public void TriggerScreenShake(float magnitude, float duration)
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null) StartCoroutine(ScreenShake(magnitude, duration));
    }

    IEnumerator ScreenShake(float mag, float dur)
    {
        float elapsed = 0f;
        Vector3 orig = mainCam.transform.localPosition;
        while (elapsed < dur)
        {
            float x = Random.Range(-1f, 1f) * mag;
            float y = Random.Range(-1f, 1f) * mag;
            mainCam.transform.localPosition = new Vector3(orig.x + x, orig.y + y, orig.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        mainCam.transform.localPosition = orig;
    }

    // ===================== UI Factory Helpers =====================

    GameObject MakePanel(Transform parent, Vector2 pos, Vector2 size, Color bg)
    {
        GameObject obj = new GameObject("Panel");
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = obj.AddComponent<Image>();
        img.color = bg;
        return obj;
    }

    TextMeshProUGUI MakeText(Transform parent, Vector2 pos, string text, float size, Color col, TextAlignmentOptions align)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(450, 40);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = col;
        tmp.alignment = align;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    Button MakeButton(Transform parent, Vector2 pos, string label, System.Action onClick, Color? col = null)
    {
        GameObject obj = new GameObject("Btn_" + label);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(340, 45);
        Image img = obj.AddComponent<Image>();
        img.color = col.HasValue ? new Color(col.Value.r * 0.3f, col.Value.g * 0.3f, col.Value.b * 0.3f, 0.9f) : new Color(0.2f, 0.2f, 0.4f, 0.9f);

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = img.color;
        cb.highlightedColor = col.HasValue ? col.Value : Color.cyan;
        cb.pressedColor = Color.white;
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick());

        // Label
        GameObject lbl = new GameObject("Label");
        lbl.transform.SetParent(obj.transform, false);
        RectTransform lrt = lbl.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        return btn;
    }
}
