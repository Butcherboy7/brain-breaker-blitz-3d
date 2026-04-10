using UnityEngine;

/// <summary>
/// Paddle controller – smooth movement, correctly clamped to inner wall edges,
/// bouncy physics material so ball reflects cleanly off it.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PaddleController : MonoBehaviour
{
    // Wall inner-edge is at 8.5 (see LevelManager.WALL_X).
    // Paddle half-width is subtracted so the paddle never overlaps the wall.
    const float WALL_INNER = 8.5f;

    public float speed        = 18f;
    public float currentWidth = 3.2f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity  = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Bouncy physics material (ball reflects cleanly)
        var pm = new PhysicMaterial("PaddleBounce")
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
        if (GameManager.Instance == null || !GameManager.Instance.isPlaying) return;

        float h = Input.GetAxis("Horizontal");
        Vector3 p = transform.position;

        // Half-width of the paddle in world units
        float halfW = currentWidth * 0.5f;

        // Clamp so paddle edges never go past wall inner face
        float maxX = WALL_INNER - halfW;
        p.x = Mathf.Clamp(p.x + h * speed * Time.deltaTime, -maxX, maxX);
        transform.position = p;
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
