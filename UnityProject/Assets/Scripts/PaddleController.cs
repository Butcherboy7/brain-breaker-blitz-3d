using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaddleController : MonoBehaviour
{
    public float speed = 16f;
    public float limit = 7.2f;

    void Awake()
    {
        // Neon green glowing paddle
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0f, 1f, 0.55f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0f, 1f, 0.55f) * 1.8f);
        GetComponent<MeshRenderer>().material = mat;

        // Add a Rigidbody for physics collisions to work properly
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isPlaying) return;
        float h = Input.GetAxis("Horizontal");
        var p = transform.position;
        p.x = Mathf.Clamp(p.x + h * speed * Time.deltaTime, -limit, limit);
        transform.position = p;
    }

    public void SetWidth(float w)
    {
        var s = transform.localScale;
        s.x = w;
        transform.localScale = s;
    }

    public void ResetPaddle()
    {
        transform.position = new Vector3(0, -4.5f, 0);
    }
}
