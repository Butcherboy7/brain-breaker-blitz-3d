using System.Collections;
using UnityEngine;

/// <summary>
/// BackgroundManager — Cyberpunk Neon Edition.
/// Creates and animates the game background:
///   - Dark deep-space gradient via camera background color cycling
///   - Ambient floating neon particles (80 slow-drifting dots)
///   - Neon grid floor plane with glowing material
///   - Dynamic ambient intensity that reacts to combo/gameplay state
///   - Subtle pulsing ambient light
/// Attach to a dedicated "BackgroundManager" GameObject.
/// AutoSetupGame creates this automatically.
/// </summary>
public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance;

    [Header("Background Colors")]
    [SerializeField] private Color bgDeep = new Color(0.039f, 0.031f, 0.078f); // #0A0814
    [SerializeField] private Color bgMid  = new Color(0.102f, 0.039f, 0.180f); // #1A0A2E

    [Header("Ambient")]
    [SerializeField] private float baseAmbientIntensity = 0.5f;

    // ── Internal ───────────────────────────────────────────
    private Camera      mainCam;
    private Light       directionalLight;
    private GameObject  ambientParticles;
    private GameObject  gridFloor;
    private Material    gridMat;
    private float       bgCycleTime;
    private float       targetAmbient;
    private float       currentAmbient;
    private float       pulsePhase;

    // ─────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        targetAmbient  = baseAmbientIntensity;
        currentAmbient = baseAmbientIntensity;
    }

    void Start()
    {
        mainCam = Camera.main;
        SetupCamera();
        SetupDirectionalLight();
        SetupGridFloor();
        SetupAmbientParticles();
    }

    // ─────────────────────────────────────────────────────
    void SetupCamera()
    {
        if (mainCam == null) return;
        mainCam.backgroundColor = bgDeep;
        mainCam.clearFlags      = CameraClearFlags.SolidColor;
    }

    void SetupDirectionalLight()
    {
        // Find or create directional light
        var existing = GameObject.Find("Directional Light");
        if (existing) directionalLight = existing.GetComponent<Light>();
        if (directionalLight == null)
        {
            var lo = new GameObject("Directional Light");
            directionalLight = lo.AddComponent<Light>();
        }
        directionalLight.type      = LightType.Directional;
        directionalLight.color     = new Color(0.6f, 0.5f, 0.9f); // cool purple-blue
        directionalLight.intensity = 0.8f;
        directionalLight.transform.rotation = Quaternion.Euler(40f, -55f, 0f);

        // Ambient lighting — dark purple
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.02f, 0.12f);
    }

    void SetupGridFloor()
    {
        // A flat plane at the bottom of the play area
        gridFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        gridFloor.name = "NeonGridFloor";
        gridFloor.transform.position   = new Vector3(0f, -5.5f, 2f);
        gridFloor.transform.localScale  = new Vector3(2.0f, 1f, 2.0f);
        Destroy(gridFloor.GetComponent<MeshCollider>()); // no collision needed

        gridMat = NeonVisuals.MakeNeonMaterial(NeonVisuals.NeonPurple, 0.6f, 0.1f, 0.3f);
        gridMat.color = NeonVisuals.BgDeep;
        gridFloor.GetComponent<MeshRenderer>().material = gridMat;
    }

    void SetupAmbientParticles()
    {
        ambientParticles = new GameObject("AmbientParticles");
        ambientParticles.transform.position = Vector3.zero;
        var ps = ambientParticles.AddComponent<ParticleSystem>();
        NeonVisuals.ConfigureAmbientDrift(ps, NeonVisuals.NeonPurple * 0.7f);
        ps.Play();
    }

    // ─────────────────────────────────────────────────────
    void Update()
    {
        AnimateCameraBackground();
        AnimateAmbient();
        AnimateGrid();
    }

    void AnimateCameraBackground()
    {
        if (mainCam == null) return;
        bgCycleTime += Time.deltaTime * 0.15f;
        // Very slowly oscillate between bgDeep and bgMid
        float t = (Mathf.Sin(bgCycleTime) + 1f) * 0.5f;
        mainCam.backgroundColor = Color.Lerp(bgDeep, bgMid * 0.5f, t * 0.4f);
    }

    void AnimateAmbient()
    {
        pulsePhase += Time.deltaTime * 1.5f;
        currentAmbient = Mathf.Lerp(currentAmbient, targetAmbient, Time.deltaTime * 2f);

        // Gentle pulse on top of base
        float pulse = 1f + Mathf.Sin(pulsePhase) * 0.08f;
        float final = currentAmbient * pulse;

        RenderSettings.ambientLight = new Color(0.05f * final, 0.02f * final, 0.12f * final);
        if (directionalLight) directionalLight.intensity = 0.8f * final;
    }

    void AnimateGrid()
    {
        if (gridMat == null) return;
        float gridPulse = 0.4f + Mathf.Sin(pulsePhase * 0.8f) * 0.2f;
        gridMat.SetColor("_EmissionColor", NeonVisuals.NeonPurple * gridPulse);
    }

    // ─────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────

    /// <summary>Set ambient intensity (0=dark, 1=full, >1=celebration).</summary>
    public void SetAmbientIntensity(float intensity) => targetAmbient = Mathf.Clamp(intensity, 0f, 2f);

    /// <summary>Trigger a brief ambient flash (e.g. on combo milestone).</summary>
    public void FlashAmbient(Color color, float duration) => StartCoroutine(AmbientFlash(color, duration));

    private IEnumerator AmbientFlash(Color color, float duration)
    {
        Color orig = RenderSettings.ambientLight;
        float t = 0f;
        while (t < duration * 0.4f)
        {
            t += Time.deltaTime;
            RenderSettings.ambientLight = Color.Lerp(orig, color * 0.4f, t / (duration * 0.4f));
            yield return null;
        }
        t = 0f;
        while (t < duration * 0.6f)
        {
            t += Time.deltaTime;
            RenderSettings.ambientLight = Color.Lerp(color * 0.4f, orig, t / (duration * 0.6f));
            yield return null;
        }
        RenderSettings.ambientLight = orig;
    }
}
