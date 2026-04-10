using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public Rigidbody rb;
    public float initialSpeed = 10f;
    public bool isLaunched = false;

    private TrailRenderer trail;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Bouncy physics material
        PhysicMaterial pm = new PhysicMaterial("BallBounce");
        pm.bounciness = 1f;
        pm.frictionCombine = PhysicMaterialCombine.Minimum;
        pm.bounceCombine  = PhysicMaterialCombine.Maximum;
        GetComponent<SphereCollider>().material = pm;

        // Glow material
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.9f, 0.1f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0f) * 2f);
        GetComponent<MeshRenderer>().material = mat;

        // Trail
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.18f;
        trail.startWidth = 0.3f;
        trail.endWidth   = 0f;
        trail.material   = new Material(Shader.Find("Sprites/Default"));
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.yellow, 0), new GradientColorKey(Color.red, 1) },
            new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
        trail.colorGradient = grad;
    }

    public void ResetBall(float speed)
    {
        initialSpeed = speed;
        isLaunched   = false;
        transform.position = new Vector3(0, -3.5f, 0);
        if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
    }

    void Update()
    {
        if (!isLaunched && GameManager.Instance != null && GameManager.Instance.isPlaying)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                Launch();
        }
    }

    void Launch()
    {
        isLaunched = true;
        Vector3 dir = new Vector3(Random.Range(-0.35f, 0.35f), 1f, 0).normalized;
        rb.velocity = dir * initialSpeed;
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Paddle"))
        {
            float offset = (transform.position.x - col.transform.position.x)
                         / (col.collider.bounds.size.x * 0.5f);
            offset = Mathf.Clamp(offset, -0.95f, 0.95f);
            Vector3 dir = new Vector3(offset, 1f - Mathf.Abs(offset) * 0.2f, 0).normalized;
            rb.velocity = dir * initialSpeed;
            if (GameManager.Instance) GameManager.Instance.TriggerScreenShake(0.05f, 0.08f);
        }

        // enforce constant speed one frame later
        StartCoroutine(LockSpeed());
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone"))
        {
            isLaunched = false;
            if (GameManager.Instance) GameManager.Instance.LoseLife();
        }
    }

    IEnumerator LockSpeed()
    {
        yield return new WaitForFixedUpdate();
        if (rb && rb.velocity.sqrMagnitude > 0.01f)
            rb.velocity = rb.velocity.normalized * initialSpeed;
    }
}
