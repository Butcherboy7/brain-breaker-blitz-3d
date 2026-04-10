using UnityEngine;

/// <summary>
/// Persists all player settings via PlayerPrefs.
/// Singleton – survives scene loads.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    // ── Saved keys ───────────────────────────────────────
    const string K_PADDLE  = "s_paddle_speed";
    const string K_BALL    = "s_ball_mult";
    const string K_TRAIL   = "s_trail";
    const string K_HI      = "s_hiscore";

    // ── Settings properties ───────────────────────────────
    public float PaddleSpeed   { get; private set; } = 22f;
    public float BallSpeedMult { get; private set; } = 1.0f;
    public bool  ShowTrail     { get; private set; } = true;
    public int   HighScore     { get; private set; } = 0;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
        Load();
    }

    // ── Setters (call these from UI callbacks) ────────────
    public void SetPaddleSpeed(float v)
    {
        PaddleSpeed = v;
        PlayerPrefs.SetFloat(K_PADDLE, v);
        // Apply immediately if paddle exists
        if (GameManager.Instance?.paddle != null)
            GameManager.Instance.paddle.speed = v;
    }

    public void SetBallMult(float v)
    {
        BallSpeedMult = v;
        PlayerPrefs.SetFloat(K_BALL, v);
    }

    public void SetTrail(bool v)
    {
        ShowTrail = v;
        PlayerPrefs.SetInt(K_TRAIL, v ? 1 : 0);
        // Apply to active ball
        if (GameManager.Instance?.ball != null)
        {
            var t = GameManager.Instance.ball.GetComponent<TrailRenderer>();
            if (t) t.enabled = v;
        }
    }

    public void TryUpdateHighScore(int score)
    {
        if (score > HighScore)
        {
            HighScore = score;
            PlayerPrefs.SetInt(K_HI, score);
        }
    }

    void Load()
    {
        PaddleSpeed   = PlayerPrefs.GetFloat(K_PADDLE, 22f);
        BallSpeedMult = PlayerPrefs.GetFloat(K_BALL,   1.0f);
        ShowTrail     = PlayerPrefs.GetInt  (K_TRAIL,  1) == 1;
        HighScore     = PlayerPrefs.GetInt  (K_HI,     0);
    }

    public void Save() => PlayerPrefs.Save();
    public void ResetAll()
    {
        PlayerPrefs.DeleteAll();
        Load();
    }
}
