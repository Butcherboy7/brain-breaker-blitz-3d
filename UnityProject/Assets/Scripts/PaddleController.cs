using UnityEngine;

/// <summary>
/// Paddle controller - uses direct KeyCode input (no Input Manager dependency),
/// clamps correctly to wall inner edges accounting for paddle half-width.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class PaddleController : MonoBehaviour
{
    // Must match LevelManager.WALL_X = 8.5
    const float WALL_INNER = 8.5f;

    public float speed        = 18f;
    public float currentWidth = 3.2f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity    = false;
        rb.isKinematic   = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Bouncy physics material
        var pm = new PhysicMaterial("PaddlePM")
        {
            bounciness      = 1f,
            dynamicFriction = 0f,
            staticFriction  = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum,
            bounceCombine   = PhysicMaterialCombine.Maximum,
        };
        GetComponent<BoxCollider>().material = pm;

        ApplyGlowMaterial();
    }

    void ApplyGlowMaterial()
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0f, 1f, 0.6f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0f, 1f, 0.6f) * 2f);
        GetComponent<MeshRenderer>().material = mat;
    }

    void Update()
    {
        // ── Detect input directly from keys – no Input Manager required ──
        float h = 0f;

        // Arrow keys
        if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A)) h = -1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) h =  1f;

        // Also support GetAxis as fallback (joystick / other input)
        if (h == 0f) h = Input.GetAxisRaw("Horizontal");

        if (h == 0f) return; // nothing pressed

        Vector3 pos = transform.position;
        float halfW = currentWidth * 0.5f;
        float maxX  = WALL_INNER - halfW - 0.05f; // tiny margin so paddle never overlaps wall

        pos.x = Mathf.Clamp(pos.x + h * speed * Time.deltaTime, -maxX, maxX);
        transform.position = pos;
    }

    public void SetWidth(float w)
    {
        currentWidth = w;
        Vector3 s = transform.localScale;
        s.x = w;
        transform.localScale = s;
    }

    public void ResetPaddle()
    {
        transform.position = new Vector3(0f, -4.5f, 0f);
    }
}
