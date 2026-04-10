using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ── State ──────────────────────────────────────────
    public int score = 0;
    public int lives = 3;
    public int currentLevel = 1;
    public int selectedIQ = 1;
    public bool isPlaying = false;
    public int combo = 0;
    public int scoreMultiplier = 1;

    // ── References ─────────────────────────────────────
    public BallController ball;
    public PaddleController paddle;

    // ── HUD Text (built via code) ──────────────────────
    private Text scoreText;
    private Text livesText;
    private Text levelText;
    private Text comboText;
    private Text powerUpText;
    private Text launchHint;
    private Text iqLabel;

    // ── Panels ─────────────────────────────────────────
    private GameObject gameOverPanel;
    private GameObject winPanel;
    private GameObject levelSelectPanel;

    // ── Misc ───────────────────────────────────────────
    private Camera mainCam;
    private Canvas hudCanvas;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        mainCam = Camera.main;
    }

    void Start()
    {
        BuildHUD();
        ShowLevelSelect();
    }

    // ═══════════════════════════════════════════════════
    //  HUD BUILDER  (No TMP — pure UnityEngine.UI.Text)
    // ═══════════════════════════════════════════════════
    void BuildHUD()
    {
        // ── EventSystem (REQUIRED for button clicks to work) ──
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Root canvas
        GameObject cvObj = new GameObject("HUD");
        Canvas cv = cvObj.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        CanvasScaler cs = cvObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cvObj.AddComponent<GraphicRaycaster>();
        hudCanvas = cv;

        // ── Top bar ─────────────────────────
        GameObject topBar = UIPanel(cv.transform, Anchor.TopStretch, new Vector2(0, -35), new Vector2(0, 70), new Color(0, 0, 0, 0.75f));
        scoreText  = UIText(topBar.transform, new Vector2(-700, 0), "Score: 0",  28, Color.white, TextAnchor.MiddleLeft);
        livesText  = UIText(topBar.transform, new Vector2(0, 0),    "♥ ♥ ♥",     30, new Color(1f, 0.3f, 0.3f), TextAnchor.MiddleCenter);
        levelText  = UIText(topBar.transform, new Vector2(700, 0),  "Level 1",   28, new Color(0.4f, 1f, 0.8f), TextAnchor.MiddleRight);

        // ── IQ label ────────────────────────
        iqLabel = UIText(cv.transform, new Vector2(0, -85), "Beginner | IQ 80", 22, new Color(0.7f, 0.7f, 0.7f), TextAnchor.UpperCenter, Anchor.TopCenter);

        // ── Combo (bottom-left) ──────────────
        comboText = UIText(cv.transform, new Vector2(80, 80), "", 34, Color.yellow, TextAnchor.LowerLeft, Anchor.BottomLeft);

        // ── Power-up message (bottom center) ─
        powerUpText = UIText(cv.transform, new Vector2(0, 130), "", 32, new Color(0f, 1f, 0.8f), TextAnchor.LowerCenter, Anchor.BottomCenter);

        // ── Launch hint ──────────────────────
        launchHint = UIText(cv.transform, new Vector2(0, 60), "SPACE or CLICK to launch", 26, new Color(1f, 1f, 0.4f), TextAnchor.LowerCenter, Anchor.BottomCenter);

        // ── GAME OVER Panel ──────────────────
        gameOverPanel = UIPanel(cv.transform, Anchor.Center, Vector2.zero, new Vector2(440, 280), new Color(0.08f, 0.03f, 0.18f, 0.97f));
        UIText(gameOverPanel.transform, new Vector2(0, 90),  "GAME OVER",  42, new Color(1f, 0.2f, 0.2f), TextAnchor.MiddleCenter);
        UIText(gameOverPanel.transform, new Vector2(0, 40),  "Score: 0",   24, Color.white, TextAnchor.MiddleCenter);
        UIButton(gameOverPanel.transform, new Vector2(0,  -10), new Vector2(320, 50), "RETRY",        new Color(0.6f, 0.1f, 0.1f), () => StartGame(selectedIQ));
        UIButton(gameOverPanel.transform, new Vector2(0,  -70), new Vector2(320, 50), "CHANGE LEVEL", new Color(0.1f, 0.1f, 0.4f), () => { gameOverPanel.SetActive(false); ShowLevelSelect(); });
        gameOverPanel.SetActive(false);

        // ── WIN Panel ────────────────────────
        winPanel = UIPanel(cv.transform, Anchor.Center, Vector2.zero, new Vector2(440, 260), new Color(0.03f, 0.18f, 0.08f, 0.97f));
        UIText(winPanel.transform, new Vector2(0, 90), "LEVEL CLEAR!",  42, new Color(0.2f, 1f, 0.4f), TextAnchor.MiddleCenter);
        UIButton(winPanel.transform, new Vector2(0,   0), new Vector2(320, 55), "NEXT LEVEL >>", new Color(0.1f, 0.5f, 0.2f), () => { winPanel.SetActive(false); NextLevel(); });
        UIButton(winPanel.transform, new Vector2(0, -65), new Vector2(320, 48), "MAIN MENU",     new Color(0.1f, 0.1f, 0.3f), () => { winPanel.SetActive(false); ShowLevelSelect(); });
        winPanel.SetActive(false);

        // ── LEVEL SELECT Panel ───────────────
        levelSelectPanel = UIPanel(cv.transform, Anchor.Center, Vector2.zero, new Vector2(520, 500), new Color(0.05f, 0.05f, 0.18f, 0.97f));
        UIText(levelSelectPanel.transform, new Vector2(0, 195), "BRAIN BREAKER BLITZ", 36, new Color(0.7f, 0.4f, 1f), TextAnchor.MiddleCenter);
        UIText(levelSelectPanel.transform, new Vector2(0, 150), "Select Your IQ Level", 20, Color.gray, TextAnchor.MiddleCenter);

        string[] labels = { "BEGINNER  |  IQ 80", "AVERAGE  |  IQ 100", "SMART  |  IQ 120", "GENIUS  |  IQ 140", "MASTERMIND  |  IQ 160+" };
        Color[] btnCols = { Color.green, Color.cyan, Color.yellow, new Color(1f, 0.5f, 0f), Color.magenta };

        // FIX: capture iq in a local copy inside the loop to avoid C# closure bug
        CreateLevelButton(levelSelectPanel.transform, 1, labels[0], btnCols[0], 90f);
        CreateLevelButton(levelSelectPanel.transform, 2, labels[1], btnCols[1], 30f);
        CreateLevelButton(levelSelectPanel.transform, 3, labels[2], btnCols[2], -30f);
        CreateLevelButton(levelSelectPanel.transform, 4, labels[3], btnCols[3], -90f);
        CreateLevelButton(levelSelectPanel.transform, 5, labels[4], btnCols[4], -150f);
    }

    void CreateLevelButton(Transform parent, int iq, string label, Color col, float y)
    {
        Color bg = new Color(col.r * 0.25f, col.g * 0.25f, col.b * 0.25f, 0.9f);
        UIButton(parent, new Vector2(0, y), new Vector2(420, 50), label, bg, () =>
        {
            levelSelectPanel.SetActive(false);
            StartGame(iq);
        }, col);
    }

    // ═══════════════════════════════════════════════════
    //  GAME FLOW
    // ═══════════════════════════════════════════════════
    void ShowLevelSelect()
    {
        isPlaying = false;
        if (levelSelectPanel) levelSelectPanel.SetActive(true);
    }

    public void StartGame(int iq)
    {
        selectedIQ = iq;
        currentLevel = 1;
        score = lives = 3;
        score = 0; combo = 0; scoreMultiplier = 1;
        levelSelectPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);
        LoadLevel();
    }

    void LoadLevel()
    {
        isPlaying = false;
        LevelManager.Instance.GenerateLevel(selectedIQ, currentLevel);
        IQConfig cfg = LevelManager.Instance.iqConfigs[selectedIQ];
        float speedMult = 1f + (currentLevel - 1) * 0.12f;
        paddle.SetWidth(cfg.paddleWidth);
        paddle.ResetPaddle();
        ball.ResetBall(cfg.ballSpeed * speedMult);
        isPlaying = true;
        RefreshHUD();
    }

    void NextLevel() { currentLevel++; LoadLevel(); }

    // ═══════════════════════════════════════════════════
    //  EVENTS
    // ═══════════════════════════════════════════════════
    public void AddScore(int amount, Vector3 worldPos)
    {
        combo++;
        scoreMultiplier = combo >= 5 ? 3 : combo >= 3 ? 2 : 1;
        score += amount * scoreMultiplier;
        RefreshHUD();
        CheckWin();
    }

    public void LoseLife()
    {
        combo = 0; scoreMultiplier = 1;
        TriggerScreenShake(0.25f, 0.35f);
        lives--;
        RefreshHUD();
        if (lives <= 0) { isPlaying = false; gameOverPanel.SetActive(true); }
        else
        {
            IQConfig cfg = LevelManager.Instance.iqConfigs[selectedIQ];
            float sm = 1f + (currentLevel - 1) * 0.12f;
            ball.ResetBall(cfg.ballSpeed * sm);
            paddle.ResetPaddle();
        }
    }

    void CheckWin()
    {
        if (FindObjectsOfType<Brick>().Length == 0)
        { isPlaying = false; winPanel.SetActive(true); }
    }

    // ═══════════════════════════════════════════════════
    //  POWER-UPS
    // ═══════════════════════════════════════════════════
    static readonly string[] PowerLabels = { "extra_life:+1 LIFE!", "wide_paddle:WIDE PADDLE!", "slow_ball:SLOW BALL!", "score_x2:2x SCORE!", "multiball:MULTI-BALL!" };

    public void ApplyPowerUp(string type)
    {
        foreach (var pl in PowerLabels)
        {
            var p = pl.Split(':');
            if (p[0] == type && powerUpText) { powerUpText.text = p[1]; StartCoroutine(ClearPowerText(2.5f)); break; }
        }

        IQConfig cfg = LevelManager.Instance.iqConfigs[selectedIQ];
        float sm = 1f + (currentLevel - 1) * 0.12f;
        float baseSpeed = cfg.ballSpeed * sm;

        switch (type)
        {
            case "extra_life": lives++; RefreshHUD(); break;
            case "wide_paddle": paddle.SetWidth(cfg.paddleWidth + 1.5f); StartCoroutine(Delayed(8f, () => paddle.SetWidth(cfg.paddleWidth))); break;
            case "slow_ball": ball.initialSpeed = baseSpeed * 0.5f; ball.rb.velocity = ball.rb.velocity.normalized * ball.initialSpeed; StartCoroutine(Delayed(6f, () => { ball.initialSpeed = baseSpeed; ball.rb.velocity = ball.rb.velocity.normalized * baseSpeed; })); break;
            case "score_x2": scoreMultiplier = 2; StartCoroutine(Delayed(10f, () => scoreMultiplier = 1)); break;
            case "multiball": SpawnMultiBall(baseSpeed); break;
        }
    }

    void SpawnMultiBall(float speed)
    {
        GameObject extra = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        extra.name = "Ball_Extra"; extra.tag = "Ball";
        extra.transform.position = ball.transform.position;
        extra.transform.localScale = Vector3.one * 0.5f;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = Color.magenta; mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.magenta * 2f);
        extra.GetComponent<MeshRenderer>().material = mat;
        Rigidbody erb = extra.AddComponent<Rigidbody>();
        erb.useGravity = false; erb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        erb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        erb.velocity = new Vector3(Random.Range(-0.5f, 0.5f), 1f, 0).normalized * speed;
        BallController ebc = extra.AddComponent<BallController>();
        ebc.initialSpeed = speed; ebc.isLaunched = true; ebc.rb = erb;
        Destroy(extra, 20f);
    }

    // ═══════════════════════════════════════════════════
    //  UI REFRESH
    // ═══════════════════════════════════════════════════
    void RefreshHUD()
    {
        if (scoreText) scoreText.text = "Score: " + score;
        if (livesText) { string h = ""; for (int i = 0; i < lives; i++) h += "♥ "; livesText.text = h.TrimEnd(); }
        if (levelText) levelText.text = "Level " + currentLevel;
        if (comboText) comboText.text = combo >= 3 ? $"COMBO x{scoreMultiplier}  ({combo})" : "";
        if (iqLabel)
        {
            string[] names = { "", "Beginner | IQ 80", "Average | IQ 100", "Smart | IQ 120", "Genius | IQ 140", "Mastermind | IQ 160+" };
            iqLabel.text = names[Mathf.Clamp(selectedIQ, 0, 5)];
        }
        bool showHint = ball != null && !ball.isLaunched && isPlaying;
        if (launchHint) launchHint.gameObject.SetActive(showHint);
    }

    // ═══════════════════════════════════════════════════
    //  SCREEN SHAKE
    // ═══════════════════════════════════════════════════
    public void TriggerScreenShake(float mag, float dur)
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam) StartCoroutine(ShakeRoutine(mag, dur));
    }

    IEnumerator ShakeRoutine(float mag, float dur)
    {
        Vector3 orig = mainCam.transform.localPosition;
        float t = 0;
        while (t < dur) { mainCam.transform.localPosition = orig + (Vector3)Random.insideUnitCircle * mag; t += Time.deltaTime; yield return null; }
        mainCam.transform.localPosition = orig;
    }

    // ═══════════════════════════════════════════════════
    //  UTILITY COROUTINES
    // ═══════════════════════════════════════════════════
    IEnumerator ClearPowerText(float d) { yield return new WaitForSeconds(d); if (powerUpText) powerUpText.text = ""; }
    IEnumerator Delayed(float d, System.Action a) { yield return new WaitForSeconds(d); a?.Invoke(); }

    // ═══════════════════════════════════════════════════
    //  UI FACTORY HELPERS  (No TMP)
    // ═══════════════════════════════════════════════════
    enum Anchor { Center, TopStretch, TopCenter, BottomLeft, BottomCenter, BottomRight }

    GameObject UIPanel(Transform parent, Anchor anchor, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject("Panel"); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        ApplyAnchor(rt, anchor);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.AddComponent<Image>().color = color;
        return go;
    }

    Text UIText(Transform parent, Vector2 pos, string msg, int fontSize, Color col, TextAnchor align, Anchor anchor = Anchor.Center)
    {
        var go = new GameObject("Txt"); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        ApplyAnchor(rt, anchor);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(700, 50);
        var t = go.AddComponent<Text>();
        t.text = msg; t.fontSize = fontSize; t.color = col; t.alignment = align;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.resizeTextForBestFit = false;
        return t;
    }

    Button UIButton(Transform parent, Vector2 pos, Vector2 size, string label, Color bg, System.Action onClick, Color? textCol = null)
    {
        var goB = new GameObject("Btn_" + label); goB.transform.SetParent(parent, false);
        var rt = goB.AddComponent<RectTransform>(); rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = goB.AddComponent<Image>(); img.color = bg;
        var btn = goB.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor    = bg;
        cb.highlightedColor = Color.white;
        cb.pressedColor   = new Color(bg.r * 0.5f, bg.g * 0.5f, bg.b * 0.5f);
        btn.colors = cb;
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var goL = new GameObject("Label"); goL.transform.SetParent(goB.transform, false);
        var lrt = goL.AddComponent<RectTransform>(); lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
        var t = goL.AddComponent<Text>();
        t.text = label; t.fontSize = 20; t.color = textCol ?? Color.white; t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontStyle = FontStyle.Bold;
        return btn;
    }

    void ApplyAnchor(RectTransform rt, Anchor a)
    {
        switch (a)
        {
            case Anchor.Center:       rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f); break;
            case Anchor.TopStretch:   rt.anchorMin = new Vector2(0, 1); rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 1); break;
            case Anchor.TopCenter:    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1); rt.pivot = new Vector2(0.5f, 1); break;
            case Anchor.BottomLeft:   rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0); break;
            case Anchor.BottomCenter: rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0); break;
            case Anchor.BottomRight:  rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0); break;
        }
    }
}
