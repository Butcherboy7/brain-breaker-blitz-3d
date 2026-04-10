using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaddleController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 15f;
    public float screenLimit = 7f;

    [Header("Smoothing")]
    private float targetX;
    private Rigidbody rb;

    [Header("Visual")]
    private Vector3 defaultScale;
    public float currentWidth = 2.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        defaultScale = transform.localScale;
        ApplyMaterial();
    }

    void ApplyMaterial()
    {
        MeshRenderer ren = GetComponent<MeshRenderer>();
        if (ren == null) return;
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0f, 1f, 0.6f); // Teal/Neon Green
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0f, 1f, 0.6f) * 1.5f);
        ren.material = mat;
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isPlaying) return;

        float horizontal = Input.GetAxis("Horizontal");
        Vector3 pos = transform.position;
        pos.x += horizontal * speed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, -screenLimit, screenLimit);
        transform.position = pos;
    }

    public void SetWidth(float width)
    {
        currentWidth = width;
        Vector3 scale = transform.localScale;
        scale.x = width;
        transform.localScale = scale;
    }

    public void ResetPaddle()
    {
        transform.position = new Vector3(0, -4.5f, 0);
        SetWidth(currentWidth);
    }
}
