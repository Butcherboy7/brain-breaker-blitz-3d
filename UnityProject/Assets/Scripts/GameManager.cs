using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ── State ──────────────────────────────────────────
    public int  score = 0;
    public int  lives = 3;
    public int  currentLevel = 1;
    public int  selectedIQ   = 1;
    public bool isPlaying    = false;
    public bool isPaused     = false;
    public int  combo        = 0;
    public int  scoreMultiplier = 1;

    // ── References ─────────────────────────────────────
    public BallController   ball;
    public PaddleController paddle;

    // ── HUD Text ───────────────────────────────────────
    private Text scoreText;
    private Text livesText;
    private Text levelText;
    private Text comboText;
    private Text powerUpText;
    private Text launchHint;
    private Text iqLabel;
    private Text hiScoreText;

    // ── Panels ─────────────────────────────────────────
    private GameObject gameOverPanel;
    private GameObject winPanel;
    private GameObject levelSelectPanel;
    private GameObject pausePanel;
    private GameObject settingsPanel;

    // ── Misc ───────────────────────────────────────────
    private Camera mainCam;
    private Canvas hudCanvas;

    // ───────────────────────────────────────────────────
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
        // Make sure SettingsManager exists
        if (SettingsManager.Instance == null)
            new GameObject("SettingsManager").AddComponent<SettingsManager>();

        BuildHUD();
        ShowLevelSelect();
    }

    void Update()
    {
        // ESC = Pause / Unpause during gameplay
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPlaying && !isPaused) PauseGame();
            else if (isPaused)          ResumeGame();
        }
    }

    // ═══════════════════════════════════════════════════
    //  HUD BUILDER
    // ═══════════════════════════════════════════════════
    void BuildHUD()
    {
        // EventSystem – required for button clicks
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // Root canvas
        var cvObj = new GameObject("HUD");
        var cv    = cvObj.AddComponent<Canvas>();
        cv.renderMode  = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        var csc = cvObj.AddComponent<CanvasScaler>();
        csc.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        csc.referenceResolution = new Vector2(1920, 1080);
        cvObj.AddComponent<GraphicRaycaster>();
        hudCanvas = cv;

        // ── TOP BAR ─────────────────────────────────────
        var topBar = UIPanel(cv.transform, Anc.TopStretch, new Vector2(0,-35), new Vector2(0,70), new Color(0,0,0,0.75f));
        scoreText = UIText(topBar.transform, new Vector2(-700,0), "Score: 0",  28, Color.white,              TextAnchor.MiddleLeft);
        livesText = UIText(topBar.transform, new Vector2(0,0),    "♥ ♥ ♥",    30, new Color(1,.3f,.3f),     TextAnchor.MiddleCenter);
        levelText = UIText(topBar.transform, new Vector2(700,0),  "Level 1",   28, new Color(.4f,1f,.8f),    TextAnchor.MiddleRight);

        // Settings cog button (top-right)
        UIButton(topBar.transform, new Vector2(830,0), new Vector2(60,50),
            "⚙", new Color(.15f,.15f,.3f,.9f), () => OpenSettings(), Color.white);

        // ── SECONDARY ROW ───────────────────────────────
        iqLabel    = UIText(cv.transform, new Vector2(0,-80),  "Beginner | IQ 80",       20, new Color(.7f,.7f,.7f), TextAnchor.UpperCenter,  Anc.TopCenter);
        hiScoreText= UIText(cv.transform, new Vector2(0,-106), "Best: 0",                18, new Color(.6f,.5f,1f),  TextAnchor.UpperCenter,  Anc.TopCenter);
        comboText  = UIText(cv.transform, new Vector2(80,80),  "",                       34, Color.yellow,           TextAnchor.LowerLeft,    Anc.BottomLeft);
        powerUpText= UIText(cv.transform, new Vector2(0,140),  "",                       30, new Color(0,1,.8f),     TextAnchor.LowerCenter,  Anc.BottomCenter);
        launchHint = UIText(cv.transform, new Vector2(0,60),   "SPACE or CLICK to launch",26,new Color(1,1,.4f),    TextAnchor.LowerCenter,  Anc.BottomCenter);

        // ── GAME OVER ────────────────────────────────────
        gameOverPanel = BuildModal(cv.transform, new Color(.08f,.03f,.18f,.97f));
        UIText(gameOverPanel.transform, new Vector2(0, 100), "GAME OVER",  44, new Color(1f,.2f,.2f),  TextAnchor.MiddleCenter);
        UIText(gameOverPanel.transform, new Vector2(0,  52), "",           24, Color.white,             TextAnchor.MiddleCenter); // score placeholder
        UIButton(gameOverPanel.transform, new Vector2(0,  -5), new Vector2(330,50), "RETRY",        new Color(.5f,.1f,.1f,.9f), () => StartGame(selectedIQ));
        UIButton(gameOverPanel.transform, new Vector2(0, -65), new Vector2(330,50), "SETTINGS ⚙",  new Color(.1f,.1f,.35f,.9f), () => { gameOverPanel.SetActive(false); OpenSettings(); });
        UIButton(gameOverPanel.transform, new Vector2(0,-125), new Vector2(330,50), "MAIN MENU",    new Color(.1f,.1f,.25f,.9f), () => { gameOverPanel.SetActive(false); ShowLevelSelect(); });
        gameOverPanel.SetActive(false);

        // ── WIN ───────────────────────────────────────────
        winPanel = BuildModal(cv.transform, new Color(.03f,.18f,.08f,.97f));
        UIText(winPanel.transform, new Vector2(0,100), "LEVEL CLEAR!",   44, new Color(.2f,1f,.4f), TextAnchor.MiddleCenter);
        UIButton(winPanel.transform, new Vector2(0,  5), new Vector2(330,55), "NEXT LEVEL ▶", new Color(.1f,.5f,.2f,.9f), () => { winPanel.SetActive(false); NextLevel(); });
        UIButton(winPanel.transform, new Vector2(0,-60), new Vector2(330,50), "MAIN MENU",    new Color(.1f,.1f,.3f,.9f), () => { winPanel.SetActive(false); ShowLevelSelect(); });
        winPanel.SetActive(false);

        // ── LEVEL SELECT ─────────────────────────────────
        levelSelectPanel = UIPanel(cv.transform, Anc.Center, Vector2.zero, new Vector2(540,530), new Color(.04f,.04f,.16f,.97f));
        UIText(levelSelectPanel.transform, new Vector2(0, 218), "BRAIN BREAKER BLITZ", 34, new Color(.7f,.4f,1f), TextAnchor.MiddleCenter);
        UIText(levelSelectPanel.transform, new Vector2(0, 173), "Select Your IQ Level", 19, Color.gray,           TextAnchor.MiddleCenter);

        string[] lbls = { "BEGINNER  — IQ 80", "AVERAGE  — IQ 100", "SMART  — IQ 120", "GENIUS  — IQ 140", "MASTERMIND  — IQ 160+" };
        Color[]  cols = { Color.green, Color.cyan, Color.yellow, new Color(1f,.5f,0f), Color.magenta };
        for (int i = 0; i < 5; i++) CreateLevelBtn(levelSelectPanel.transform, i+1, lbls[i], cols[i], 110f - i*58f);

        UIButton(levelSelectPanel.transform, new Vector2(0,-185), new Vector2(310,46), "SETTINGS ⚙", new Color(.15f,.15f,.35f,.9f), () => OpenSettings(), new Color(.7f,.7f,1f));
        levelSelectPanel.SetActive(false);

        // ── PAUSE PANEL ──────────────────────────────────
        pausePanel = BuildModal(cv.transform, new Color(.04f,.04f,.15f,.96f));
        UIText(pausePanel.transform, new Vector2(0,100), "⏸  PAUSED",       40, Color.cyan,      TextAnchor.MiddleCenter);
        UIButton(pausePanel.transform, new Vector2(0,  15), new Vector2(320,52), "▶  RESUME",      new Color(.1f,.4f,.2f,.9f), () => ResumeGame());
        UIButton(pausePanel.transform, new Vector2(0, -48), new Vector2(320,52), "SETTINGS ⚙",    new Color(.15f,.15f,.35f,.9f), () => { pausePanel.SetActive(false); OpenSettings(); });
        UIButton(pausePanel.transform, new Vector2(0,-111), new Vector2(320,52), "QUIT TO MENU",   new Color(.3f,.05f,.05f,.9f), () => { pausePanel.SetActive(false); PauseGame(false); ShowLevelSelect(); });
        pausePanel.SetActive(false);

        // ── SETTINGS PANEL ───────────────────────────────
        BuildSettingsPanel(cv.transform);
    }

    // ═══════════════════════════════════════════════════
    //  SETTINGS PANEL
    // ═══════════════════════════════════════════════════
    void BuildSettingsPanel(Transform parent)
    {
        settingsPanel = UIPanel(parent, Anc.Center, Vector2.zero, new Vector2(560, 520), new Color(.04f,.04f,.18f,.97f));
        UIText(settingsPanel.transform, new Vector2(0, 215), "⚙  SETTINGS", 34, new Color(.7f,.4f,1f), TextAnchor.MiddleCenter);

        float y = 150f;

        // ── Paddle Speed ─────────────────────────────────
        UIText(settingsPanel.transform, new Vector2(-200, y), "Paddle Speed", 22, Color.white, TextAnchor.MiddleLeft);
        var paddleValLabel = UIText(settingsPanel.transform, new Vector2(210, y), "22", 22, Color.yellow, TextAnchor.MiddleLeft);
        var paddleSlider = BuildSlider(settingsPanel.transform, new Vector2(0, y-36), 10f, 35f, SettingsManager.Instance?.PaddleSpeed ?? 22f);
        paddleSlider.onValueChanged.AddListener(v =>
        {
            int vi = Mathf.RoundToInt(v);
            paddleValLabel.text = vi.ToString();
            SettingsManager.Instance?.SetPaddleSpeed(vi);
        });

        y -= 90f;

        // ── Ball Speed Multiplier ─────────────────────────
        UIText(settingsPanel.transform, new Vector2(-200, y), "Ball Speed", 22, Color.white, TextAnchor.MiddleLeft);
        var ballValLabel = UIText(settingsPanel.transform, new Vector2(210, y), "1.0x", 22, Color.yellow, TextAnchor.MiddleLeft);
        var ballSlider = BuildSlider(settingsPanel.transform, new Vector2(0, y-36), 0.5f, 2.0f, SettingsManager.Instance?.BallSpeedMult ?? 1.0f);
        ballSlider.onValueChanged.AddListener(v =>
        {
            float rv = Mathf.Round(v * 10f) / 10f;
            ballValLabel.text = rv.ToString("0.0") + "x";
            SettingsManager.Instance?.SetBallMult(rv);
        });

        y -= 90f;

        // ── Ball Trail Toggle ─────────────────────────────
        UIText(settingsPanel.transform, new Vector2(-200, y), "Ball Trail", 22, Color.white, TextAnchor.MiddleLeft);
        bool trailOn       = SettingsManager.Instance?.ShowTrail ?? true;
        var trailLabel     = UIText(settingsPanel.transform, new Vector2(100, y), trailOn ? "ON" : "OFF", 24, trailOn ? Color.green : Color.red, TextAnchor.MiddleCenter);
        var trailBg        = UIPanel(settingsPanel.transform, Anc.Center, new Vector2(100, y), new Vector2(120, 40), trailOn ? new Color(0,.4f,.1f,.9f) : new Color(.4f,.05f,.05f,.9f));
        var trailBtn       = trailBg.AddComponent<Button>();
        trailBtn.targetGraphic = trailBg.GetComponent<Image>();
        trailBtn.onClick.AddListener(() =>
        {
            bool now = !(SettingsManager.Instance?.ShowTrail ?? true);
            SettingsManager.Instance?.SetTrail(now);
            trailLabel.text  = now ? "ON" : "OFF";
            trailLabel.color = now ? Color.green : Color.red;
            trailBg.GetComponent<Image>().color = now ? new Color(0,.4f,.1f,.9f) : new Color(.4f,.05f,.05f,.9f);
        });
        trailLabel.transform.SetParent(trailBg.transform, false);
        var trt = trailLabel.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;

        y -= 90f;

        // ── High Score Reset ──────────────────────────────
        UIText(settingsPanel.transform, new Vector2(-200, y), "High Score", 22, Color.white, TextAnchor.MiddleLeft);
        var hsVal = UIText(settingsPanel.transform, new Vector2(80, y), (SettingsManager.Instance?.HighScore ?? 0).ToString(), 22, new Color(.6f,.5f,1f), TextAnchor.MiddleLeft);
        UIButton(settingsPanel.transform, new Vector2(220, y), new Vector2(130, 40), "RESET", new Color(.35f,.05f,.05f,.9f), () =>
        {
            SettingsManager.Instance?.ResetAll();
            hsVal.text = "0";
        }, new Color(1f,.5f,.5f));

        y -= 80f;

        // ── Close button ─────────────────────────────────
        UIButton(settingsPanel.transform, new Vector2(0, y), new Vector2(320, 52), "✔  SAVE & CLOSE", new Color(.1f,.4f,.2f,.9f), () =>
        {
            SettingsManager.Instance?.Save();
            settingsPanel.SetActive(false);
            // Return to appropriate screen
            if (isPaused) pausePanel.SetActive(true);
            else if (!isPlaying) levelSelectPanel.SetActive(true);
        });

        settingsPanel.SetActive(false);
    }

    // ═══════════════════════════════════════════════════
    //  GAME FLOW
    // ═══════════════════════════════════════════════════
    void ShowLevelSelect()
    {
        isPlaying = false;
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);
        settingsPanel.SetActive(false);
        levelSelectPanel.SetActive(true);

        if (hiScoreText && SettingsManager.Instance != null)
            hiScoreText.text = "Best: " + SettingsManager.Instance.HighScore;
    }

    void OpenSettings()
    {
        settingsPanel.SetActive(true);
        levelSelectPanel.SetActive(false);
    }

    void PauseGame(bool pause = true)
    {
        isPaused         = pause;
        isPlaying        = !pause;
        Time.timeScale   = pause ? 0f : 1f;
        pausePanel.SetActive(pause);
    }

    void ResumeGame()
    {
        PauseGame(false);
        isPlaying = true;
    }

    public void StartGame(int iq)
    {
        selectedIQ = iq; currentLevel = 1;
        score = 0; lives = 3; combo = 0; scoreMultiplier = 1;
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
        LevelManager.Instance.GenerateLevel(selectedIQ, currentLevel);

        var cfg = LevelManager.Instance.iqConfigs[selectedIQ];
        float sm  = 1f + (currentLevel - 1) * 0.12f;
        float spd = cfg.ballSpeed * sm * (SettingsManager.Instance?.BallSpeedMult ?? 1f);

        paddle.speed = SettingsManager.Instance?.PaddleSpeed ?? 22f;
        paddle.SetWidth(cfg.paddleWidth);
        paddle.ResetPaddle();
        ball.ResetBall(spd);

        // Apply trail preference
        var tr = ball.GetComponent<TrailRenderer>();
        if (tr && SettingsManager.Instance != null) tr.enabled = SettingsManager.Instance.ShowTrail;

        isPlaying = true;
        RefreshHUD();
    }

    void NextLevel() { currentLevel++; LoadLevel(); }

    // ═══════════════════════════════════════════════════
    //  SCORE / LIFE EVENTS
    // ═══════════════════════════════════════════════════
    public void AddScore(int amount, Vector3 _)
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
        TriggerScreenShake(.25f, .35f);
        lives--;
        RefreshHUD();
        if (lives <= 0)
        {
            isPlaying = false;
            SettingsManager.Instance?.TryUpdateHighScore(score);
            gameOverPanel.SetActive(true);
        }
        else
        {
            var cfg = LevelManager.Instance.iqConfigs[selectedIQ];
            float sm  = 1f + (currentLevel - 1) * 0.12f;
            float spd = cfg.ballSpeed * sm * (SettingsManager.Instance?.BallSpeedMult ?? 1f);
            ball.ResetBall(spd);
            paddle.ResetPaddle();
        }
    }

    void CheckWin()
    {
        if (FindObjectsOfType<Brick>().Length == 0)
        {
            isPlaying = false;
            SettingsManager.Instance?.TryUpdateHighScore(score);
            winPanel.SetActive(true);
        }
    }

    // ═══════════════════════════════════════════════════
    //  POWER-UPS
    // ═══════════════════════════════════════════════════
    static readonly string[] PowerMsgs =
        { "extra_life:+1 LIFE!", "wide_paddle:WIDE PADDLE!", "slow_ball:SLOW BALL!",
          "score_x2:2x SCORE!", "multiball:MULTI-BALL!" };

    public void ApplyPowerUp(string type)
    {
        foreach (var pm in PowerMsgs)
        {
            var p = pm.Split(':');
            if (p[0] == type && powerUpText)
                { powerUpText.text = p[1]; StartCoroutine(ClearText(powerUpText, 2.5f)); break; }
        }

        var cfg = LevelManager.Instance.iqConfigs[selectedIQ];
        float sm     = 1f + (currentLevel - 1) * 0.12f;
        float baseSpd= cfg.ballSpeed * sm * (SettingsManager.Instance?.BallSpeedMult ?? 1f);

        switch (type)
        {
            case "extra_life":  lives++; RefreshHUD(); break;
            case "wide_paddle": paddle.SetWidth(cfg.paddleWidth + 1.6f);
                                StartCoroutine(Delay(8f, () => paddle.SetWidth(cfg.paddleWidth))); break;
            case "slow_ball":   ball.initialSpeed = baseSpd * .5f;
                                ball.rb.velocity  = ball.rb.velocity.normalized * ball.initialSpeed;
                                StartCoroutine(Delay(6f, () => { ball.initialSpeed = baseSpd; ball.rb.velocity = ball.rb.velocity.normalized * baseSpd; })); break;
            case "score_x2":    scoreMultiplier = 2;
                                StartCoroutine(Delay(10f, () => scoreMultiplier = 1)); break;
            case "multiball":   SpawnExtraBall(baseSpd); break;
        }
    }

    void SpawnExtraBall(float spd)
    {
        var e = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        e.name = "Ball_Extra"; e.tag = "Ball";
        e.transform.position   = ball.transform.position;
        e.transform.localScale = Vector3.one * .45f;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = Color.magenta; mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.magenta * 2f);
        e.GetComponent<MeshRenderer>().material = mat;
        var erb = e.AddComponent<Rigidbody>();
        erb.useGravity = false; erb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        erb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        erb.interpolation = RigidbodyInterpolation.Interpolate;
        erb.velocity = new Vector3(Random.Range(-.5f,.5f), 1f, 0).normalized * spd;
        var ebc = e.AddComponent<BallController>();
        ebc.initialSpeed = spd; ebc.isLaunched = true; ebc.rb = erb;
        Destroy(e, 20f);
    }

    // ═══════════════════════════════════════════════════
    //  HUD REFRESH
    // ═══════════════════════════════════════════════════
    void RefreshHUD()
    {
        if (scoreText) scoreText.text = "Score: " + score;
        if (livesText)
        {
            var h = ""; for (int i = 0; i < lives; i++) h += "♥ ";
            livesText.text = h.TrimEnd();
        }
        if (levelText) levelText.text = "Level " + currentLevel;
        if (comboText) comboText.text = combo >= 3 ? $"COMBO x{scoreMultiplier} ({combo})" : "";
        if (iqLabel)
        {
            string[] n = {"","Beginner | IQ 80","Average | IQ 100","Smart | IQ 120","Genius | IQ 140","Mastermind | IQ 160+"};
            iqLabel.text = n[Mathf.Clamp(selectedIQ, 0, 5)];
        }
        if (hiScoreText && SettingsManager.Instance != null)
            hiScoreText.text = "Best: " + SettingsManager.Instance.HighScore;
        if (launchHint) launchHint.gameObject.SetActive(ball != null && !ball.isLaunched && isPlaying);
    }

    // ═══════════════════════════════════════════════════
    //  SCREEN SHAKE
    // ═══════════════════════════════════════════════════
    public void TriggerScreenShake(float mag, float dur)
    {
        if (!mainCam) mainCam = Camera.main;
        if (mainCam) StartCoroutine(ShakeRoutine(mag, dur));
    }

    IEnumerator ShakeRoutine(float mag, float dur)
    {
        var orig = mainCam.transform.localPosition; float t = 0;
        while (t < dur)
        { mainCam.transform.localPosition = orig + (Vector3)Random.insideUnitCircle * mag;
          t += Time.unscaledDeltaTime; yield return null; }
        mainCam.transform.localPosition = orig;
    }

    IEnumerator ClearText(Text t, float d) { yield return new WaitForSeconds(d); if (t) t.text = ""; }
    IEnumerator Delay(float d, System.Action a) { yield return new WaitForSeconds(d); a?.Invoke(); }

    // ═══════════════════════════════════════════════════
    //  UI FACTORY
    // ═══════════════════════════════════════════════════
    enum Anc { Center, TopStretch, TopCenter, BottomLeft, BottomCenter }

    GameObject BuildModal(Transform parent, Color bg)
    {
        var p = UIPanel(parent, Anc.Center, Vector2.zero, new Vector2(460, 320), bg);
        return p;
    }

    Slider BuildSlider(Transform parent, Vector2 pos, float min, float max, float val)
    {
        var go = new GameObject("Slider"); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>(); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(400, 28);

        var bg = new GameObject("BG"); bg.transform.SetParent(go.transform, false);
        var bgRt = bg.AddComponent<RectTransform>(); bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
        var bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(.15f,.15f,.25f);

        var fill = new GameObject("Fill"); fill.transform.SetParent(go.transform, false);
        var fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(.5f, 1f); fillRt.sizeDelta = Vector2.zero;
        var fillImg = fill.AddComponent<Image>(); fillImg.color = new Color(.4f,.2f,.9f);

        var handle = new GameObject("Handle"); handle.transform.SetParent(go.transform, false);
        var hrt = handle.AddComponent<RectTransform>(); hrt.sizeDelta = new Vector2(28,28);
        var hImg = handle.AddComponent<Image>(); hImg.color = new Color(.7f,.4f,1f);

        var slider = go.AddComponent<Slider>();
        slider.minValue       = min;
        slider.maxValue       = max;
        slider.value          = val;
        slider.fillRect       = fillRt;
        slider.handleRect     = hrt;
        slider.targetGraphic  = hImg;
        slider.direction      = Slider.Direction.LeftToRight;
        return slider;
    }

    void CreateLevelBtn(Transform parent, int iq, string label, Color col, float y)
    {
        Color bg = new Color(col.r*.25f, col.g*.25f, col.b*.25f, .9f);
        UIButton(parent, new Vector2(0,y), new Vector2(430,52), label, bg,
            () => { levelSelectPanel.SetActive(false); StartGame(iq); }, col);
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
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(750, 48);
        var t = go.AddComponent<Text>();
        t.text = msg; t.fontSize = fs; t.color = col; t.alignment = align;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return t;
    }

    Button UIButton(Transform parent, Vector2 pos, Vector2 size, string label, Color bg, System.Action onClick, Color? textCol = null)
    {
        var go = new GameObject("Btn"); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>(); rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>(); img.color = bg;
        var btn = go.AddComponent<Button>();
        var cb = btn.colors; cb.normalColor = bg; cb.highlightedColor = Color.white;
        cb.pressedColor = new Color(bg.r*.5f,bg.g*.5f,bg.b*.5f); btn.colors = cb; btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());
        var lbl = new GameObject("L"); lbl.transform.SetParent(go.transform, false);
        var lrt = lbl.AddComponent<RectTransform>(); lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
        var t = lbl.AddComponent<Text>(); t.text = label; t.fontSize = 19; t.color = textCol ?? Color.white;
        t.alignment = TextAnchor.MiddleCenter; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontStyle = FontStyle.Bold;
        return btn;
    }

    void ApplyAnc(RectTransform rt, Anc a)
    {
        switch (a)
        {
            case Anc.Center:      rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f,.5f); break;
            case Anc.TopStretch:  rt.anchorMin = new Vector2(0,1); rt.anchorMax = Vector2.one; rt.pivot = new Vector2(.5f,1); break;
            case Anc.TopCenter:   rt.anchorMin = rt.anchorMax = new Vector2(.5f,1); rt.pivot = new Vector2(.5f,1); break;
            case Anc.BottomLeft:  rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero; break;
            case Anc.BottomCenter:rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f,0); break;
        }
    }
}
