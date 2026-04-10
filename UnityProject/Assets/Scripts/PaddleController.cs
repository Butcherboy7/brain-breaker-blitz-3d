using UnityEngine;

/// <summary>
/// Smooth paddle controller.
/// - Input read in Update() every frame (holding keys works perfectly)
/// - Movement applied via rb.MovePosition() in FixedUpdate (proper physics)
/// - Lerp smoothing gives buttery, responsive feel
/// - Clamps to wall inner edges accounting for paddle half-width
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class PaddleController : MonoBehaviour
{
    const float WALL_INNER = 8.5f; // must match LevelManager

    [Header("Movement")]
    public float speed        = 22f;   // world units per second
    public float smoothing    = 12f;   // higher = snappier, lower = more floaty
    public float currentWidth = 3.2f;

    private Rigidbody rb;
    private float targetX;     // updated every Update frame
    private float currentX;    // smoothly chased toward targetX

    // ─────────────────────────────────────────────────────────
    void Awake()
    {
        Application.targetFrameRate = 60;
        Time.fixedDeltaTime         = 1f / 60f;

        rb = GetComponent<Rigidbody>();
        rb.useGravity    = false;
        rb.isKinematic   = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // visual smoothness

        var pm = new PhysicMaterial("PaddlePM")
        {
            bounciness      = 1f,
            dynamicFriction = 0f,
            staticFriction  = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum,
            bounceCombine   = PhysicMaterialCombine.Maximum,
        };
        GetComponent<BoxCollider>().material = pm;

        ApplyGlow();
        targetX  = transform.position.x;
        currentX = transform.position.x;
    }

    void ApplyGlow()
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0f, 1f, 0.6f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0f, 1f, 0.6f) * 2.5f);
        GetComponent<MeshRenderer>().material = mat;
    }

    // ─────────────────────────────────────────────────────────
    // Update: read input EVERY frame (ensures holding keys works)
    void Update()
    {
        float h = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A)) h = -1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) h =  1f;
        // Analogue gamepad / keyboard axis combo
        float axis = Input.GetAxisRaw("Horizontal");
        if (h == 0f && Mathf.Abs(axis) > 0.1f) h = Mathf.Sign(axis);

        float halfW = currentWidth * 0.5f;
        float maxX  = WALL_INNER - halfW - 0.05f;

        // Accumulate desired position
        targetX += h * speed * Time.deltaTime;
        targetX  = Mathf.Clamp(targetX, -maxX, maxX);
    }

    // ─────────────────────────────────────────────────────────
    // FixedUpdate: smooth chase + physics move (looks silky smooth)
    void FixedUpdate()
    {
        // Smoothly interpolate currentX toward targetX
        currentX = Mathf.Lerp(currentX, targetX, smoothing * Time.fixedDeltaTime);

        Vector3 newPos = transform.position;
        newPos.x = currentX;
        rb.MovePosition(newPos);  // proper kinematic move — collision-aware
    }

    // ─────────────────────────────────────────────────────────
    public void SetWidth(float w)
    {
        currentWidth = w;
        var s = transform.localScale;
        s.x = w;
        transform.localScale = s;
        // Re-clamp targetX with new width
        float maxX = WALL_INNER - (w * 0.5f) - 0.05f;
        targetX = Mathf.Clamp(targetX, -maxX, maxX);
    }

    public void ResetPaddle()
    {
        transform.position = new Vector3(0f, -4.5f, 0f);
        targetX  = 0f;
        currentX = 0f;
    }
}
