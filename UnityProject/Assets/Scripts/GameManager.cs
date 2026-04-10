using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// GameManager — Cyberpunk Neon Edition.
/// Full rewrite implementing:
///   - Singleton state machine (Menu, Playing, Paused, GameOver, Win)
///   - Cyberpunk neon UI (built entirely in code — no assets required)
///   - Animated score counter, combo popup with bounce-in/out
///   - Neon-styled buttons with hover glow + click bounce
///   - Ready... GO! countdown before launch
///   - Screen flash on life lost (red vignette overlay)
///   - Brick remaining counter in HUD
///   - Combo auto-reset timer (2s without brick hit resets combo)
///   - Power-up falling pickup system
///   - IQ difficulty cards with stat previews
///   - High score persistence via PlayerPrefs
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ── State ───────────────────────────────────────────────────
    public int  score = 0;
    public int  lives = 3;
    public int  currentLevel = 1;
    public int  selectedIQ   = 1;
    public bool isPlaying    = false;
    public bool isPaused     = false;
    public int  combo        = 0;
    public int  scoreMultiplier = 1;

    // ── References ──────────────────────────────────────────────
    public BallController   ball;
    public PaddleController paddle;

    // ── HUD Text ────────────────────────────────────────────────
    private Text scoreText;
    private Text livesText;
    private Text levelText;
    private Text comboText;
    private Text powerUpText;
    private Text launchHint;
    private Text iqLabel;
    private Text hiScoreText;
    private Text brickCountText;
    private Text comboPopup;   // big center-screen combo popup

    // ── Score animation ─────────────────────────────────────────
    private int   displayedScore;
    private Coroutine scoreAnimCo;

    // ── Panels ──────────────────────────────────────────────────
    private GameObject gameOverPanel;
    private GameObject winPanel;
    private GameObject levelSelectPanel;
    private GameObject pausePanel;
    private GameObject settingsPanel;
    private GameObject flashOverlay;   // full-screen color flash
    private GameObject countdownPanel; // Ready...GO countdown

    // ── Combo miss timer ────────────────────────────────────────
    private const float COMBO_MISS_TIMEOUT = 2f;
    private float comboMissTimer;
    private bool  comboTimerRunning;

    // ── Active power-up pickups ──────────────────────────────────
    private readonly List<GameObject> activePickups = new List<GameObject>();

    // ── Misc ────────────────────────────────────────────────────
    private Camera mainCam;

    // ─────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        mainCam = Camera.main;
        Application.targetFrameRate = 60;
        Time.fixedDeltaTime         = 1f / 60f;
        Physics.bounceThreshold     = 0f;
    }

    void Start()
    {
        if (SettingsManager.Instance == null)
            new GameObject("SettingsManager").AddComponent<SettingsManager>();

        AudioManager.Instance?.StartMusic();

        BuildHUD();
        ShowLevelSelect();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPlaying && !isPaused) PauseGame();
            else if (isPaused)          ResumeGame();
        }

        // Combo auto-reset timer
        if (comboTimerRunning && isPlaying && !isPaused)
        {
            comboMissTimer += Time.deltaTime;
            if (comboMissTimer >= COMBO_MISS_TIMEOUT)
            {
                ResetCombo();
                comboTimerRunning = false;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  HUD BUILDER — Cyberpunk Neon Style
    // ═══════════════════════════════════════════════════════════
    void BuildHUD()
    {
        // Prevent duplicates
        var existingHUD = GameObject.Find("HUD");
        if (existingHUD != null) Destroy(existingHUD);

        // EventSystem
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // Root canvas
        var cvObj = new GameObject("HUD");
        var cv    = cvObj.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        var csc = cvObj.AddComponent<CanvasScaler>();
        csc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        csc.referenceResolution = new Vector2(1920, 1080);
        cvObj.AddComponent<GraphicRaycaster>();

        // ── FULL-SCREEN FLASH OVERLAY ──────────────────────────
        flashOverlay = UIPanel(cv.transform, Anc.FullScreen, Vector2.zero, Vector2.zero, new Color(1, 0, 0, 0));
        flashOverlay.GetComponent<Image>().raycastTarget = false;

        // ── TOP BAR ───────────────────────────────────────────
        var topBar = UIPanel(cv.transform, Anc.TopStretch, new Vector2(0, -30), new Vector2(0, 60), new Color(0.04f, 0.02f, 0.10f, 0.92f));
        AddNeonBorderBottom(topBar, NeonVisuals.NeonPurple);

        scoreText    = UIText(topBar.transform, new Vector2(-700, 0), "SCORE:  0",          26, NeonVisuals.NeonCyan,   TextAnchor.MiddleLeft);
        livesText    = UIText(topBar.transform, new Vector2(0, 0),    "♥ ♥ ♥",             28, NeonVisuals.NeonPink,   TextAnchor.MiddleCenter);
        levelText    = UIText(topBar.transform, new Vector2(680, 0),  "LEVEL 1",            26, NeonVisuals.NeonGreen,  TextAnchor.MiddleRight);
        UIButton(topBar.transform, new Vector2(840, 0), new Vector2(60, 44), "⚙",
            new Color(0.2f, 0.1f, 0.35f, 0.9f), () => OpenSettings(), NeonVisuals.NeonPurple);

        // ── SECONDARY INFO ─────────────────────────────────────
        iqLabel       = UIText(cv.transform, new Vector2(0, -72),   "", 18, new Color(0.7f, 0.5f, 1f), TextAnchor.UpperCenter, Anc.TopCenter);
        hiScoreText   = UIText(cv.transform, new Vector2(0, -96),   "", 16, NeonVisuals.NeonPurple,     TextAnchor.UpperCenter, Anc.TopCenter);
        brickCountText= UIText(cv.transform, new Vector2(-820, -72), "", 18, NeonVisuals.NeonYellow,    TextAnchor.UpperLeft,   Anc.TopLeft);
        comboText     = UIText(cv.transform, new Vector2(60, 110),   "", 28, NeonVisuals.NeonYellow,    TextAnchor.LowerLeft,   Anc.BottomLeft);
        powerUpText   = UIText(cv.transform, new Vector2(0, 130),    "", 26, NeonVisuals.NeonGreen,     TextAnchor.LowerCenter, Anc.BottomCenter);
        launchHint    = UIText(cv.transform, new Vector2(0, 60),     "◆ SPACE or CLICK to launch ◆", 22, NeonVisuals.NeonYellow, TextAnchor.LowerCenter, Anc.BottomCenter);

        // Big center combo popup
        comboPopup    = UIText(cv.transform, new Vector2(0, 0), "", 64, NeonVisuals.NeonPink, TextAnchor.MiddleCenter, Anc.Center);
        comboPopup.gameObject.SetActive(false);

        // ── GAME OVER ──────────────────────────────────────────
        gameOverPanel = BuildNeonModal(cv.transform, new Color(0.05f, 0.01f, 0.15f, 0.97f), NeonVisuals.NeonPink);
        UIText(gameOverPanel.transform, new Vector2(0, 110), "GAME  OVER",   48, NeonVisuals.NeonPink,   TextAnchor.MiddleCenter);
        UIText(gameOverPanel.transform, new Vector2(0,  55), "",             22, NeonVisuals.White,       TextAnchor.MiddleCenter);
        UIButton(gameOverPanel.transform, new Vector2(0,   5), new Vector2(340, 52), "▶  RETRY",       new Color(0.5f, 0.02f, 0.2f, 0.9f), () => StartGame(selectedIQ), NeonVisuals.NeonPink);
        UIButton(gameOverPanel.transform, new Vector2(0, -62), new Vector2(340, 52), "⚙  SETTINGS",    new Color(0.15f, 0.05f, 0.3f, 0.9f), () => { gameOverPanel.SetActive(false); OpenSettings(); }, NeonVisuals.NeonPurple);
        UIButton(gameOverPanel.transform, new Vector2(0,-129), new Vector2(340, 52), "✕  MAIN MENU",   new Color(0.08f, 0.05f, 0.18f, 0.9f), () => { gameOverPanel.SetActive(false); ShowLevelSelect(); }, NeonVisuals.NeonCyan);
        gameOverPanel.SetActive(false);

        // ── WIN ────────────────────────────────────────────────
        winPanel = BuildNeonModal(cv.transform, new Color(0.02f, 0.10f, 0.05f, 0.97f), NeonVisuals.NeonGreen);
        UIText(winPanel.transform, new Vector2(0, 110), "LEVEL  CLEAR!", 48, NeonVisuals.NeonGreen,  TextAnchor.MiddleCenter);
        UIButton(winPanel.transform, new Vector2(0,  10), new Vector2(340, 55), "▶  NEXT LEVEL",  new Color(0.05f, 0.35f, 0.1f, 0.9f), () => { winPanel.SetActive(false); NextLevel(); }, NeonVisuals.NeonGreen);
        UIButton(winPanel.transform, new Vector2(0, -58), new Vector2(340, 50), "✕  MAIN MENU",   new Color(0.08f, 0.05f, 0.18f, 0.9f), () => { winPanel.SetActive(false); ShowLevelSelect(); }, NeonVisuals.NeonCyan);
        winPanel.SetActive(false);

        // ── LEVEL SELECT ───────────────────────────────────────
        BuildLevelSelect(cv.transform);

        // ── PAUSE ──────────────────────────────────────────────
        pausePanel = BuildNeonModal(cv.transform, new Color(0.04f, 0.02f, 0.14f, 0.96f), NeonVisuals.NeonCyan);
        UIText(pausePanel.transform, new Vector2(0, 110), "⏸  PAUSED",      42, NeonVisuals.NeonCyan,   TextAnchor.MiddleCenter);
        UIButton(pausePanel.transform, new Vector2(0,  18), new Vector2(320, 52), "▶  RESUME",       new Color(0.05f, 0.3f, 0.2f, 0.9f), () => ResumeGame(), NeonVisuals.NeonGreen);
        UIButton(pausePanel.transform, new Vector2(0, -46), new Vector2(320, 52), "⟲  RESTART",      new Color(0.2f, 0.1f, 0.02f, 0.9f), () => { pausePanel.SetActive(false); PauseGame(false); StartGame(selectedIQ); }, NeonVisuals.NeonYellow);
        UIButton(pausePanel.transform, new Vector2(0,-110), new Vector2(320, 52), "⚙  SETTINGS",     new Color(0.15f, 0.05f, 0.3f, 0.9f), () => { pausePanel.SetActive(false); OpenSettings(); }, NeonVisuals.NeonPurple);
        UIButton(pausePanel.transform, new Vector2(0,-174), new Vector2(320, 52), "✕  QUIT",          new Color(0.25f, 0.02f, 0.05f, 0.9f), () => { pausePanel.SetActive(false); PauseGame(false); ShowLevelSelect(); }, NeonVisuals.NeonPink);
        pausePanel.SetActive(false);

        // ── SETTINGS ───────────────────────────────────────────
        BuildSettingsPanel(cv.transform);

        // ── COUNTDOWN ──────────────────────────────────────────
        countdownPanel = UIPanel(cv.transform, Anc.Center, Vector2.zero, new Vector2(400, 200), new Color(0, 0, 0, 0));
        countdownPanel.GetComponent<Image>().raycastTarget = false;
        countdownPanel.SetActive(false);
    }

    // ── Level select cards ─────────────────────────────────────
    void BuildLevelSelect(Transform parent)
    {
        levelSelectPanel = UIPanel(parent, Anc.FullScreen, Vector2.zero, Vector2.zero, new Color(0.04f, 0.02f, 0.12f, 0.98f));

        // Title
        UIText(levelSelectPanel.transform, new Vector2(0, 220), "BRAIN BREAKER BLITZ", 48, NeonVisuals.NeonPurple, TextAnchor.MiddleCenter, Anc.Center);
        UIText(levelSelectPanel.transform, new Vector2(0, 160), "SELECT YOUR IQ LEVEL", 22, new Color(0.6f, 0.5f, 0.9f), TextAnchor.MiddleCenter, Anc.Center);

        // Settings button top-right
        UIButton(levelSelectPanel.transform, new Vector2(860, 490), new Vector2(80, 50), "⚙",
            new Color(0.15f, 0.05f, 0.3f, 0.9f), () => OpenSettings(), NeonVisuals.NeonPurple);

        // IQ Cards — 5 cards laid horizontally
        string[] labels = { "BEGINNER\nIQ 80",  "AVERAGE\nIQ 100", "SMART\nIQ 120", "GENIUS\nIQ 140", "MASTERMIND\nIQ 160+" };
        string[] emojis = { "🧠",               "🧠🧠",             "🧠🧠🧠",         "🧩🧠",           "🔮🧠🔮" };
        Color[]  colors = { NeonVisuals.NeonGreen, NeonVisuals.NeonCyan, NeonVisuals.NeonYellow, NeonVisuals.NeonPink, NeonVisuals.NeonPurple };
        float[]  speeds = { 8f, 12f, 16f, 20f, 25f };
        float[] paddles = { 4f, 3f, 2.5f, 2f, 1.5f };
        int[]   bricks  = { 32, 50, 72, 98, 128 };
        float cardW = 300f, cardH = 380f;
        float spacing = 320f;
        float totalW  = spacing * 4f;
        float startX  = -totalW / 2f;

        for (int i = 0; i < 5; i++)
        {
            int iq = i + 1;
            float px = startX + i * spacing;
            Color c = colors[i];
            BuildIQCard(levelSelectPanel.transform, iq, labels[i], c, px, cardW, cardH, speeds[i], paddles[i], bricks[i]);
        }

        // Hi score
        hiScoreText = UIText(levelSelectPanel.transform, new Vector2(0, -390), $"BEST: {PlayerPrefs.GetInt("s_hiscore", 0):N0}",
            22, NeonVisuals.NeonPurple, TextAnchor.MiddleCenter, Anc.Center);

        levelSelectPanel.SetActive(false);
    }

    void BuildIQCard(Transform parent, int iq, string label, Color accent, float x,
                     float cardW, float cardH, float speed, float padW, int brickCount)
    {
        // Card background
        var card = UIPanel(parent, Anc.Center, new Vector2(x, -30),
            new Vector2(cardW, cardH), new Color(accent.r * 0.12f, accent.g * 0.12f, accent.b * 0.18f, 0.95f));
        AddNeonBorder(card, accent);

        // Label
        var lbl = UIText(card.transform, new Vector2(0, 120), label, 22, accent, TextAnchor.MiddleCenter);
        lbl.lineSpacing = 1.4f;

        // Stats
        float barW = 180f;
        UIText(card.transform, new Vector2(0, 40), $"⚡ Speed  {speed:0}", 16, Color.white, TextAnchor.MiddleCenter);
        BuildStatBar(card.transform, new Vector2(0, 18), barW, speed / 25f, accent);
        UIText(card.transform, new Vector2(0, -12), $"📏 Paddle  {padW:0.0}", 16, Color.white, TextAnchor.MiddleCenter);
        BuildStatBar(card.transform, new Vector2(0, -34), barW, padW / 4f, accent);
        UIText(card.transform, new Vector2(0, -64), $"🧱 Bricks  {brickCount}", 16, Color.white, TextAnchor.MiddleCenter);
        BuildStatBar(card.transform, new Vector2(0, -86), barW, brickCount / 128f, accent);

        // Play button
        UIButton(card.transform, new Vector2(0, -145), new Vector2(220, 48), "▶  PLAY",
            new Color(accent.r * 0.3f, accent.g * 0.3f, accent.b * 0.4f, 0.95f),
            () => { levelSelectPanel.SetActive(false); StartGame(iq); }, accent);
    }

    void BuildStatBar(Transform parent, Vector2 pos, float width, float fillRatio, Color color)
    {
        // bg
        var bg = UIPanel(parent, Anc.Center, pos, new Vector2(width, 10), new Color(0.1f, 0.1f, 0.2f));
        // fill
        float clamped = Mathf.Clamp01(fillRatio);
        var fill = UIPanel(bg.transform, Anc.LeftStretch, new Vector2(0, 0), new Vector2(0, 0), color * 0.8f);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(clamped, 1f);
        fillRt.sizeDelta = Vector2.zero;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
    }

    // ── Settings panel ─────────────────────────────────────────
    void BuildSettingsPanel(Transform parent)
    {
        settingsPanel = UIPanel(parent, Anc.Center, Vector2.zero, new Vector2(580, 540),
            new Color(0.04f, 0.02f, 0.16f, 0.98f));
        AddNeonBorder(settingsPanel, NeonVisuals.NeonPurple);
        UIText(settingsPanel.transform, new Vector2(0, 230), "⚙  SETTINGS", 36, NeonVisuals.NeonPurple, TextAnchor.MiddleCenter);

        float y = 160f;

        // Paddle Speed
        UIText(settingsPanel.transform, new Vector2(-210, y), "Paddle Speed", 22, Color.white, TextAnchor.MiddleLeft);
        var padVal = UIText(settingsPanel.transform, new Vector2(230, y), "22", 22, NeonVisuals.NeonYellow, TextAnchor.MiddleLeft);
        var padSlider = BuildSlider(settingsPanel.transform, new Vector2(0, y - 36), 10f, 35f, SettingsManager.Instance?.PaddleSpeed ?? 22f, NeonVisuals.NeonPurple);
        padSlider.onValueChanged.AddListener(v => { int vi = Mathf.RoundToInt(v); padVal.text = vi.ToString(); SettingsManager.Instance?.SetPaddleSpeed(vi); });
        y -= 95f;

        // Ball Speed
        UIText(settingsPanel.transform, new Vector2(-210, y), "Ball Speed", 22, Color.white, TextAnchor.MiddleLeft);
        var ballVal = UIText(settingsPanel.transform, new Vector2(230, y), "1.0x", 22, NeonVisuals.NeonYellow, TextAnchor.MiddleLeft);
        var ballSlider = BuildSlider(settingsPanel.transform, new Vector2(0, y - 36), 0.5f, 2.0f, SettingsManager.Instance?.BallSpeedMult ?? 1f, NeonVisuals.NeonCyan);
        ballSlider.onValueChanged.AddListener(v => { float r = Mathf.Round(v * 10f) / 10f; ballVal.text = r.ToString("0.0") + "x"; SettingsManager.Instance?.SetBallMult(r); });
        y -= 95f;

        // Ball Trail Toggle
        UIText(settingsPanel.transform, new Vector2(-210, y), "Ball Trail", 22, Color.white, TextAnchor.MiddleLeft);
        bool trailOn = SettingsManager.Instance?.ShowTrail ?? true;
        var trailLbl = UIText(settingsPanel.transform, new Vector2(120, y), trailOn ? "ON" : "OFF", 22,
            trailOn ? NeonVisuals.NeonGreen : NeonVisuals.NeonPink, TextAnchor.MiddleCenter);
        var trailBg  = UIPanel(settingsPanel.transform, Anc.Center, new Vector2(120, y), new Vector2(120, 40),
            trailOn ? new Color(0, 0.3f, 0.08f, 0.9f) : new Color(0.3f, 0.03f, 0.08f, 0.9f));
        var trailBtn = trailBg.AddComponent<Button>();
        trailBtn.targetGraphic = trailBg.GetComponent<Image>();
        trailBtn.onClick.AddListener(() =>
        {
            bool now = !(SettingsManager.Instance?.ShowTrail ?? true);
            SettingsManager.Instance?.SetTrail(now);
            trailLbl.text  = now ? "ON" : "OFF";
            trailLbl.color = now ? NeonVisuals.NeonGreen : NeonVisuals.NeonPink;
            trailBg.GetComponent<Image>().color = now ? new Color(0, 0.3f, 0.08f, 0.9f) : new Color(0.3f, 0.03f, 0.08f, 0.9f);
        });
        trailLbl.transform.SetParent(trailBg.transform, false);
        var trt = trailLbl.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        y -= 95f;

        // High Score Reset
        UIText(settingsPanel.transform, new Vector2(-210, y), "High Score", 22, Color.white, TextAnchor.MiddleLeft);
        var hsVal = UIText(settingsPanel.transform, new Vector2(60, y), (SettingsManager.Instance?.HighScore ?? 0).ToString("N0"),
            22, NeonVisuals.NeonPurple, TextAnchor.MiddleLeft);
        UIButton(settingsPanel.transform, new Vector2(234, y), new Vector2(120, 40), "RESET",
            new Color(0.3f, 0.02f, 0.05f, 0.9f), () => { SettingsManager.Instance?.ResetAll(); hsVal.text = "0"; }, NeonVisuals.NeonPink);
        y -= 85f;

        // Close
        UIButton(settingsPanel.transform, new Vector2(0, y), new Vector2(340, 52), "✔  SAVE & CLOSE",
            new Color(0.05f, 0.35f, 0.15f, 0.9f), () =>
            {
                SettingsManager.Instance?.Save();
                settingsPanel.SetActive(false);
                if (isPaused) pausePanel.SetActive(true);
                else if (!isPlaying) levelSelectPanel.SetActive(true);
            }, NeonVisuals.NeonGreen);

        settingsPanel.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════
    //  GAME FLOW
    // ═══════════════════════════════════════════════════════════
    void ShowLevelSelect()
    {
        isPlaying = false;
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);
        settingsPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
        if (hiScoreText && SettingsManager.Instance != null)
            hiScoreText.text = $"BEST:  {SettingsManager.Instance.HighScore:N0}";
        BackgroundManager.Instance?.SetAmbientIntensity(0.3f);
    }

    void OpenSettings() => settingsPanel.SetActive(true);

    void PauseGame(bool pause = true)
    {
        isPaused       = pause;
        isPlaying      = !pause;
        Time.timeScale = pause ? 0f : 1f;
        pausePanel.SetActive(pause);
    }

    void ResumeGame() { PauseGame(false); isPlaying = true; }

    public void StartGame(int iq)
    {
        selectedIQ   = iq;
        currentLevel = 1;
        score        = 0;
        displayedScore = 0;
        lives        = 3;
        combo        = 0;
        scoreMultiplier = 1;
        Time.timeScale = 1f; isPaused = false;
        levelSelectPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);
        pausePanel.SetActive(false);
        LoadLevel();
    }

    void LoadLevel()
    {
        isPlaying = false;

        // Clear old power-up pickups
        foreach (var p in activePickups) if (p) Destroy(p);
        activePickups.Clear();

        LevelManager.Instance.GenerateLevel(selectedIQ, currentLevel);

        var cfg  = LevelManager.Instance.iqConfigs[selectedIQ];
        float sm = 1f + (currentLevel - 1) * 0.12f;
        float spd = cfg.ballSpeed * sm * (SettingsManager.Instance?.BallSpeedMult ?? 1f);

        paddle.speed = SettingsManager.Instance?.PaddleSpeed ?? 22f;
        paddle.SetWidth(cfg.paddleWidth);
        paddle.ResetPaddle();
        
        // Ball: find existing or use reference
        if (ball == null) RespawnMainBall(spd);
        else ball.ResetBall(spd);

        // Trail settings
        if (ball && SettingsManager.Instance != null)
            ball.SetTrailEnabled(SettingsManager.Instance.ShowTrail);

        RefreshHUD();
        BackgroundManager.Instance?.SetAmbientIntensity(0.5f);

        // Start countdown then enable play
        StartCoroutine(ReadyGoCountdown());
    }

    void NextLevel() { currentLevel++; LoadLevel(); }

    private IEnumerator ReadyGoCountdown()
    {
        if (countdownPanel == null) { isPlaying = true; yield break; }

        countdownPanel.SetActive(true);
        // Make sure ball is NOT launched
        if (ball) ball.isLaunched = false;

        // Clear children
        foreach (Transform c in countdownPanel.transform) Destroy(c.gameObject);

        Text cdText = UIText(countdownPanel.transform, Vector2.zero, "3", 90, NeonVisuals.NeonYellow, TextAnchor.MiddleCenter);

        string[] steps = { "3", "2", "1", "GO!" };
        Color[]  stepColors = { NeonVisuals.NeonYellow, NeonVisuals.NeonPink, NeonVisuals.NeonCyan, NeonVisuals.NeonGreen };

        for (int i = 0; i < steps.Length; i++)
        {
            AudioManager.Instance?.PlayCountdownBeep(i);
            cdText.text  = steps[i];
            cdText.color = stepColors[i];
            cdText.fontSize = (i == 3) ? 130 : 90;

            if (i == 3) // GO! effect
            {
                CameraController.Instance?.Shake(0.15f, 0.4f);
                FlashScreen(NeonVisuals.NeonGreen * 0.4f, 0.3f);
            }

            // Bounce in
            var rt = cdText.GetComponent<RectTransform>();
            rt.localScale = Vector3.zero;
            float t = 0f;
            float dur = (i == 3) ? 0.12f : 0.18f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float p = t / dur;
                rt.localScale = Vector3.one * Mathf.Lerp(0f, 1.25f, p);
                yield return null;
            }
            rt.localScale = Vector3.one * 1.25f;
            yield return new WaitForSecondsRealtime(0.06f);
            float t2 = 0f;
            while (t2 < 0.08f)
            {
                t2 += Time.unscaledDeltaTime;
                rt.localScale = Vector3.Lerp(Vector3.one * 1.25f, Vector3.one, t2 / 0.08f);
                yield return null;
            }
            rt.localScale = Vector3.one;

            if (i < steps.Length - 1) yield return new WaitForSecondsRealtime(0.65f);
            else yield return new WaitForSecondsRealtime(0.15f);
        }

        // Fade out GO! text
        if (cdText != null)
        {
            float ft = 0f;
            Color sc = cdText.color;
            Vector3 sScale = cdText.rectTransform.localScale;
            while (ft < 0.3f)
            {
                ft += Time.unscaledDeltaTime;
                float p = ft / 0.3f;
                cdText.color = new Color(sc.r, sc.g, sc.b, Mathf.Lerp(1f, 0f, p));
                cdText.rectTransform.localScale = Vector3.Lerp(sScale, sScale * 1.5f, p);
                yield return null;
            }
        }

        countdownPanel.SetActive(false);
        isPlaying = true;
    }

    // ═══════════════════════════════════════════════════════════
    //  SCORE / LIVES / COMBO
    // ═══════════════════════════════════════════════════════════
    public void AddScore(int amount, Vector3 worldPos)
    {
        combo++;
        // Multiplier tiers: x1/x2/x3/x5/x10
        scoreMultiplier = combo >= 10 ? 10
                        : combo >= 7  ? 5
                        : combo >= 5  ? 3
                        : combo >= 3  ? 2 : 1;
        score += amount * scoreMultiplier;

        // Animate score counter
        if (scoreAnimCo != null) StopCoroutine(scoreAnimCo);
        scoreAnimCo = StartCoroutine(AnimateScore(displayedScore, score));

        // Combo popup
        if (combo >= 3) StartCoroutine(ShowComboPopup());
        if (combo == 5 || combo == 7 || combo == 10)
            AudioManager.Instance?.PlayComboMilestone(combo);

        // Restart miss timer
        comboMissTimer    = 0f;
        comboTimerRunning = true;

        RefreshHUD();
        CheckWin();
    }

    public void ResetComboMiss()
    {
        // Called when ball hits paddle — resets miss timer only (not combo)
        comboMissTimer    = 0f;
        comboTimerRunning = true;
    }

    private void ResetCombo()
    {
        if (combo > 0)
        {
            combo           = 0;
            scoreMultiplier = 1;
            RefreshHUD();
        }
    }

    public void LoseLife()
    {
        combo           = 0;
        scoreMultiplier = 1;
        comboTimerRunning = false;

        CameraController.Instance?.Shake(0.25f, 0.35f);
        FlashScreen(new Color(1f, 0f, 0f, 0.35f), 0.5f);
        AudioManager.Instance?.PlayLifeLost();

        lives--;
        RefreshHUD();

        if (lives <= 0)
        {
            isPlaying = false;
            SettingsManager.Instance?.TryUpdateHighScore(score);
            gameOverPanel.SetActive(true);
            AudioManager.Instance?.PlayGameOver();
        }
        else
        {
            var cfg  = LevelManager.Instance.iqConfigs[selectedIQ];
            float sm = 1f + (currentLevel - 1) * 0.12f;
            float spd = cfg.ballSpeed * sm * (SettingsManager.Instance?.BallSpeedMult ?? 1f);
            RespawnMainBall(spd);
            paddle.ResetPaddle();
        }
    }

    void CheckWin()
    {
        if (Brick.BrickCount <= 0)
        {
            isPlaying = false;
            SettingsManager.Instance?.TryUpdateHighScore(score);
            AudioManager.Instance?.PlayLevelComplete();
            BackgroundManager.Instance?.SetAmbientIntensity(1f);
            StartCoroutine(DelayThen(1.0f, () => winPanel.SetActive(true)));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  POWER-UPS
    // ═══════════════════════════════════════════════════════════
    static readonly string[] PowerMsgs =
        { "extra_life:+1 LIFE!", "wide_paddle:WIDE PADDLE!", "slow_ball:SLOW BALL!",
          "score_x2:2× SCORE!", "multiball:MULTI–BALL!", "fireball:FIREBALL!",
          "shrink_paddle:SHRINK!", "sticky:STICKY!" };

    /// <summary>Spawns a falling pickup at the given world position.</summary>
    public void SpawnPowerUpPickup(string type, Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Pickup_" + type;
        go.transform.position   = pos;
        go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        go.tag = "PowerUp";

        // Neon color per type
        Color pCol = type switch
        {
            "extra_life"    => NeonVisuals.NeonGreen,
            "wide_paddle"   => NeonVisuals.NeonCyan,
            "shrink_paddle" => NeonVisuals.NeonPink,
            "slow_ball"     => NeonVisuals.NeonYellow,
            "multiball"     => NeonVisuals.NeonPurple,
            "fireball"      => new Color(1f, 0.4f, 0f),
            "score_x2"      => NeonVisuals.NeonYellow,
            _               => Color.white
        };
        go.GetComponent<MeshRenderer>().material = NeonVisuals.MakeNeonMaterial(pCol, 3f);

        // Remove sphere collider; add trigger collider
        Destroy(go.GetComponent<CapsuleCollider>());
        var bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = true;

        var pickup = go.AddComponent<PowerUpPickup>();
        pickup.powerType = type;
        pickup.gm        = this;

        activePickups.Add(go);
        Destroy(go, 12f); // auto-destroy if not collected
    }

    public void ApplyPowerUp(string type)
    {
        string msg = "";
        foreach (var pm in PowerMsgs)
        {
            var p = pm.Split(':');
            if (p[0] == type) { msg = p[1]; break; }
        }
        if (powerUpText) { powerUpText.text = msg; StartCoroutine(ClearText(powerUpText, 2.5f)); }
        AudioManager.Instance?.PlayPowerUpCollect();
        FlashScreen(new Color(0f, 0.9f, 1f, 0.15f), 0.4f);

        var cfg  = LevelManager.Instance.iqConfigs[selectedIQ];
        float sm = 1f + (currentLevel - 1) * 0.12f;
        float baseSpd = cfg.ballSpeed * sm * (SettingsManager.Instance?.BallSpeedMult ?? 1f);

        switch (type)
        {
            case "extra_life":
                lives++;
                RefreshHUD();
                break;
            case "wide_paddle":
                paddle.SetWidth(cfg.paddleWidth + 1.6f);
                StartCoroutine(DelayThen(8f, () => paddle.SetWidth(cfg.paddleWidth)));
                break;
            case "shrink_paddle":
                paddle.SetWidth(Mathf.Max(1f, cfg.paddleWidth - 1f));
                StartCoroutine(DelayThen(8f, () => paddle.SetWidth(cfg.paddleWidth)));
                break;
            case "slow_ball":
                SetAllBallSpeeds(baseSpd * 0.5f);
                StartCoroutine(DelayThen(7f, () => SetAllBallSpeeds(baseSpd)));
                break;
            case "score_x2":
                scoreMultiplier = Mathf.Max(scoreMultiplier, 2);
                StartCoroutine(DelayThen(12f, () => scoreMultiplier = 1));
                break;
            case "multiball":
                SpawnExtraBall(baseSpd);
                SpawnExtraBall(baseSpd);
                CameraController.Instance?.SetMultiBall(true);
                StartCoroutine(DelayThen(18f, () => CameraController.Instance?.SetMultiBall(false)));
                break;
            case "fireball":
                if (ball) StartCoroutine(FireballMode(ball, 4f));
                break;
        }
    }

    void SetAllBallSpeeds(float spd)
    {
        var balls = FindObjectsOfType<BallController>();
        foreach (var b in balls) { b.initialSpeed = spd; if (b.rb) b.rb.velocity = b.rb.velocity.normalized * spd; }
    }

    void SpawnExtraBall(float spd)
    {
        var e = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        e.name = "Ball_Extra"; e.tag = "Ball";
        e.transform.position   = ball ? ball.transform.position : Vector3.zero;
        e.transform.localScale = Vector3.one * 0.45f;
        e.GetComponent<MeshRenderer>().material = NeonVisuals.MakeNeonMaterial(NeonVisuals.NeonPurple, 3f, 0.8f, 0.95f);

        var erb = e.AddComponent<Rigidbody>();
        erb.useGravity = false;
        erb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        erb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        erb.interpolation = RigidbodyInterpolation.Interpolate;
        erb.velocity = new Vector3(Random.Range(-0.6f, 0.6f), 1f, 0).normalized * spd;

        var epm = new PhysicMaterial("ExtraBallPM") { bounciness = 1f, dynamicFriction = 0f, staticFriction = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum, bounceCombine = PhysicMaterialCombine.Maximum };
        e.GetComponent<SphereCollider>().material = epm;

        var ebc = e.AddComponent<BallController>();
        ebc.initialSpeed = spd; ebc.isLaunched = true;
        Destroy(e, 22f);
    }

    private IEnumerator FireballMode(BallController b, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && b != null)
        {
            elapsed += Time.deltaTime;
            b.GetComponent<MeshRenderer>()?.material?.SetColor("_EmissionColor",
                NeonVisuals.RainbowColor(elapsed * 2f) * 5f);
            yield return null;
        }
        if (b != null)
            b.GetComponent<MeshRenderer>()?.material?.SetColor("_EmissionColor", NeonVisuals.NeonCyan * 3f);
    }

    void RespawnMainBall(float spd)
    {
        // Destroy old ball if it still exists
        if (ball != null) Destroy(ball.gameObject);

        var ballGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballGO.name = "Ball"; ballGO.tag = "Ball";
        ballGO.transform.localScale = Vector3.one * 0.45f;
        ballGO.transform.position   = new Vector3(0f, -3.2f, 0f);

        var pm = new PhysicMaterial("BallPM") { bounciness = 1f, dynamicFriction = 0f, staticFriction = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum, bounceCombine = PhysicMaterialCombine.Maximum };
        ballGO.GetComponent<SphereCollider>().material = pm;

        var rb = ballGO.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Dead zone trigger
        var dzGO = new GameObject("DeadZone"); dzGO.tag = "DeadZone";
        dzGO.transform.position = new Vector3(0, -7f, 0);
        var dzc = dzGO.AddComponent<BoxCollider>(); dzc.isTrigger = true;
        dzGO.transform.localScale = new Vector3(40f, 1f, 5f);

        ball = ballGO.AddComponent<BallController>();
        ball.initialSpeed = spd;
        ball.SetTrailEnabled(SettingsManager.Instance?.ShowTrail ?? true);
    }

    // ═══════════════════════════════════════════════════════════
    //  HUD REFRESH
    // ═══════════════════════════════════════════════════════════
    void RefreshHUD()
    {
        // Lives as neon hearts
        if (livesText)
        {
            var h = "";
            for (int i = 0; i < lives; i++) h += "♥ ";
            livesText.text = h.TrimEnd();
        }
        if (levelText)   levelText.text   = $"LEVEL  {currentLevel}";
        if (comboText)   comboText.text   = combo >= 3 ? $"COMBO  x{scoreMultiplier}  ({combo})" : "";
        if (iqLabel)
        {
            string[] n = { "", "IQ 80 • BEGINNER", "IQ 100 • AVERAGE", "IQ 120 • SMART", "IQ 140 • GENIUS", "IQ 160+ • MASTERMIND" };
            iqLabel.text = n[Mathf.Clamp(selectedIQ, 0, 5)];
        }
        if (hiScoreText && SettingsManager.Instance != null)
            hiScoreText.text = $"BEST:  {SettingsManager.Instance.HighScore:N0}";
        if (brickCountText) brickCountText.text = $"BRICKS  {Brick.BrickCount}";
        if (launchHint) launchHint.gameObject.SetActive(ball != null && !ball.isLaunched && isPlaying);
    }

    private IEnumerator AnimateScore(int from, int to)
    {
        float dur = 0.4f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            displayedScore = (int)Mathf.Lerp(from, to, t / dur);
            if (scoreText) scoreText.text = $"SCORE:  {displayedScore:N0}";
            yield return null;
        }
        displayedScore = to;
        if (scoreText) scoreText.text = $"SCORE:  {to:N0}";
    }

    private IEnumerator ShowComboPopup()
    {
        if (comboPopup == null) yield break;

        string[] milestoneLabels = { "", "", "", "x2 COMBO!", "x2 COMBO!", "x3 HOT!", "x3 HOT!", "x5 FIRE!!", "x5 FIRE!!", "x5 FIRE!!", "⚡ x10 MAX!!" };
        string label = (combo < milestoneLabels.Length) ? milestoneLabels[combo] : "⚡ MAX!!";
        if (label == "") yield break;

        comboPopup.gameObject.SetActive(true);
        comboPopup.text  = label;
        comboPopup.color = scoreMultiplier >= 10 ? NeonVisuals.NeonPink
                         : scoreMultiplier >= 5  ? NeonVisuals.NeonPurple
                         : NeonVisuals.NeonYellow;

        var rt = comboPopup.GetComponent<RectTransform>();
        // Bounce in
        rt.localScale = Vector3.zero;
        float t = 0f;
        while (t < 0.2f) { t += Time.deltaTime; rt.localScale = Vector3.one * Mathf.Lerp(0f, 1.2f, t / 0.2f); yield return null; }
        rt.localScale = Vector3.one * 1.2f;
        float t2 = 0f;
        while (t2 < 0.1f) { t2 += Time.deltaTime; rt.localScale = Vector3.Lerp(Vector3.one * 1.2f, Vector3.one, t2 / 0.1f); yield return null; }
        rt.localScale = Vector3.one;

        yield return new WaitForSeconds(0.9f);

        // Bounce out
        float t3 = 0f;
        while (t3 < 0.15f) { t3 += Time.deltaTime; rt.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t3 / 0.15f); yield return null; }
        comboPopup.gameObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════
    //  SCREEN EFFECTS
    // ═══════════════════════════════════════════════════════════
    public void TriggerScreenShake(float mag, float dur) => CameraController.Instance?.Shake(mag, dur);

    void FlashScreen(Color color, float duration) => StartCoroutine(FlashRoutine(color, duration));

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        if (flashOverlay == null) yield break;
        var img = flashOverlay.GetComponent<Image>();
        if (img == null) yield break;
        float t = 0f, rise = duration * 0.3f, fall = duration * 0.7f;
        while (t < rise)  { t += Time.unscaledDeltaTime; img.color = Color.Lerp(Color.clear, color, t / rise); yield return null; }
        t = 0f;
        while (t < fall)  { t += Time.unscaledDeltaTime; img.color = Color.Lerp(color, Color.clear, t / fall); yield return null; }
        img.color = Color.clear;
    }

    // ═══════════════════════════════════════════════════════════
    //  UI FACTORY — Neon Cyberpunk Style
    // ═══════════════════════════════════════════════════════════
    private enum Anc { Center, TopStretch, TopCenter, TopLeft, BottomLeft, BottomCenter, FullScreen, LeftStretch }

    GameObject BuildNeonModal(Transform parent, Color bg, Color borderColor)
    {
        var p = UIPanel(parent, Anc.Center, Vector2.zero, new Vector2(480, 360), bg);
        AddNeonBorder(p, borderColor);
        return p;
    }

    void AddNeonBorder(GameObject go, Color color)
    {
        // Top edge
        AddEdge(go.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 2), color);
        // Bottom edge
        AddEdge(go.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 2), color);
        // Left edge
        AddEdge(go.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(2, 0), color);
        // Right edge
        AddEdge(go.transform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(2, 0), color);
    }

    void AddNeonBorderBottom(GameObject go, Color color)
    {
        AddEdge(go.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 2), color);
    }

    void AddEdge(Transform parent, Vector2 ancMin, Vector2 ancMax, Vector2 size, Color color)
    {
        var e = new GameObject("Edge"); e.transform.SetParent(parent, false);
        var rt = e.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.sizeDelta = size;
        var img = e.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    Slider BuildSlider(Transform parent, Vector2 pos, float min, float max, float val, Color accent)
    {
        var go = new GameObject("Slider"); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>(); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(420, 28);

        var bg = new GameObject("BG"); bg.transform.SetParent(go.transform, false);
        var bgRt = bg.AddComponent<RectTransform>(); bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.08f, 0.05f, 0.18f);

        var fill = new GameObject("Fill"); fill.transform.SetParent(go.transform, false);
        var fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(0.5f, 1f); fillRt.sizeDelta = Vector2.zero;
        fill.AddComponent<Image>().color = accent * 0.7f;

        var handle = new GameObject("Handle"); handle.transform.SetParent(go.transform, false);
        var hrt = handle.AddComponent<RectTransform>(); hrt.sizeDelta = new Vector2(28, 28);
        handle.AddComponent<Image>().color = accent;

        var slider = go.AddComponent<Slider>();
        slider.minValue = min; slider.maxValue = max; slider.value = val;
        slider.fillRect = fillRt; slider.handleRect = hrt;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    GameObject UIPanel(Transform parent, Anc anchor, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject("Panel"); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>(); ApplyAnc(rt, anchor);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.AddComponent<Image>().color = color;
        return go;
    }

    Text UIText(Transform parent, Vector2 pos, string msg, int fs, Color col, TextAnchor align, Anc anchor = Anc.Center)
    {
        var go = new GameObject("T"); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>(); ApplyAnc(rt, anchor);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(860, 60);
        var t = go.AddComponent<Text>();
        t.text = msg; t.fontSize = fs; t.color = col; t.alignment = align;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontStyle = FontStyle.Bold;
        // Soft shadow for depth
        var shad = go.AddComponent<Shadow>();
        shad.effectColor    = Color.black;
        shad.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    Button UIButton(Transform parent, Vector2 pos, Vector2 size, string label, Color bg, System.Action onClick, Color? textCol = null)
    {
        var go = new GameObject("Btn"); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>(); rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>(); img.color = bg;
        AddNeonBorder(go, (textCol ?? NeonVisuals.NeonCyan) * 0.8f);

        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor      = bg;
        cb.highlightedColor = Color.Lerp(bg, Color.white, 0.25f);
        cb.pressedColor     = bg * 0.6f;
        cb.colorMultiplier  = 1f;
        btn.colors = cb;
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => { AudioManager.Instance?.PlayUIClick(); onClick?.Invoke(); });

        var lbl = new GameObject("L"); lbl.transform.SetParent(go.transform, false);
        var lrt = lbl.AddComponent<RectTransform>(); lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
        var t = lbl.AddComponent<Text>();
        t.text = label; t.fontSize = 20; t.color = textCol ?? NeonVisuals.White;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontStyle = FontStyle.Bold;
        var shad = lbl.AddComponent<Shadow>(); shad.effectColor = Color.black; shad.effectDistance = new Vector2(1f, -1f);
        return btn;
    }

    void ApplyAnc(RectTransform rt, Anc a)
    {
        switch (a)
        {
            case Anc.Center:       rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f); break;
            case Anc.TopStretch:   rt.anchorMin = new Vector2(0, 1); rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 1); break;
            case Anc.TopCenter:    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1); rt.pivot = new Vector2(0.5f, 1); break;
            case Anc.TopLeft:      rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); break;
            case Anc.BottomLeft:   rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero; break;
            case Anc.BottomCenter: rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0); break;
            case Anc.FullScreen:   rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; break;
            case Anc.LeftStretch:  rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 0.5f); break;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────
    IEnumerator ClearText(Text t, float d) { yield return new WaitForSeconds(d); if (t) t.text = ""; }
    IEnumerator DelayThen(float d, System.Action a) { yield return new WaitForSeconds(d); a?.Invoke(); }

}

// ═══════════════════════════════════════════════════════════════
//  PowerUpPickup — simple component on falling pickup object
// ═══════════════════════════════════════════════════════════════
public class PowerUpPickup : MonoBehaviour
{
    public string      powerType;
    public GameManager gm;

    private float fallSpeed = 2.5f;
    private float rotSpeed  = 120f;
    private float bobPhase;

    void Update()
    {
        // Fall downward
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        // Spin and bob
        bobPhase += Time.deltaTime * 3f;
        transform.Rotate(Vector3.up * rotSpeed * Time.deltaTime, Space.World);
        // Destroy when off-screen
        if (transform.position.y < -9f) Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Paddle"))
        {
            gm?.ApplyPowerUp(powerType);
            Destroy(gameObject);
        }
    }
}
