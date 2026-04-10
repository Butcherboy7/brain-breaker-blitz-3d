using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    [Header("Physics")]
    public Rigidbody rb;
    public float initialSpeed = 10f;
    public bool isLaunched = false;

    [Header("Trail")]
    private TrailRenderer trail;

    [Header("Spin")]
    private Vector3 lastPaddleHitNormal;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Add a trail renderer
        trail = GetComponent<TrailRenderer>();
        if (trail == null) trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.2f;
        trail.startWidth = 0.3f;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(Color.red, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        trail.colorGradient = g;

        ApplyGlowMaterial(Color.yellow);
    }

    void ApplyGlowMaterial(Color col)
    {
        MeshRenderer ren = GetComponent<MeshRenderer>();
        if (ren == null) return;
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = col;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", col * 2f);
        ren.material = mat;
    }

    public void ResetBall(float speed)
    {
        initialSpeed = speed;
        transform.position = new Vector3(0, -3.5f, 0);
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        isLaunched = false;
    }

    void Update()
    {
        if (!isLaunched && GameManager.Instance != null && GameManager.Instance.isPlaying)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                Launch();
            }
        }
    }

    void Launch()
    {
        isLaunched = true;
        float randX = Random.Range(-0.4f, 0.4f);
        Vector3 launchDir = new Vector3(randX, 1, 0).normalized;
        rb.velocity = launchDir * initialSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Paddle"))
        {
            // Add spin based on hit position for skill-based play
            float hitOffset = (transform.position.x - collision.transform.position.x) / (collision.collider.bounds.size.x / 2f);
            hitOffset = Mathf.Clamp(hitOffset, -1f, 1f);
            Vector3 dir = new Vector3(hitOffset * 0.8f, 1, 0).normalized;
            rb.velocity = dir * initialSpeed;
            // Screen shake
            GameManager.Instance.TriggerScreenShake(0.05f, 0.1f);
        }

        // Speed lock
        if (isLaunched)
        {
            StartCoroutine(LockSpeed());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone"))
        {
            isLaunched = false;
            GameManager.Instance.LoseLife();
        }
    }

    IEnumerator LockSpeed()
    {
        yield return new WaitForFixedUpdate();
        if (rb != null && rb.velocity.magnitude > 0.1f)
            rb.velocity = rb.velocity.normalized * initialSpeed;
    }
}
