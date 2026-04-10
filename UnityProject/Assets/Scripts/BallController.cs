using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public Rigidbody rb;
    public float initialSpeed = 10f;
    private bool isLaunched = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void ResetBall(float speed)
    {
        initialSpeed = speed;
        transform.position = new Vector3(0, -3.5f, 0);
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        isLaunched = false;
    }

    void Update()
    {
        if (!isLaunched && GameManager.Instance.isPlaying)
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
        Vector3 launchDir = new Vector3(Random.Range(-0.5f, 0.5f), 1, 0).normalized;
        rb.velocity = launchDir * initialSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ensure velocity stays constant (simple physics)
        Vector3 velocity = rb.velocity;
        rb.velocity = velocity.normalized * initialSpeed;

        if (collision.gameObject.CompareTag("DeadZone"))
        {
            GameManager.Instance.LoseLife();
        }
    }

    private void FixedUpdate()
    {
        // Maintain velocity height for 2.5D feel if needed, but here we just keep speed
        if (isLaunched)
        {
            rb.velocity = rb.velocity.normalized * initialSpeed;
        }
    }
}
