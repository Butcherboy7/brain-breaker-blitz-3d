using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Brick : MonoBehaviour
{
    [Header("Stats")]
    public int health = 1;
    public int maxHealth = 1;
    public bool isBonus = false;
    public string bonusType = "";

    [Header("Visual")]
    public MeshRenderer meshRenderer;
    private Color baseColor;
    private Material mat;

    // Particle burst on destroy
    public GameObject explosionPrefab; 

    public void Initialize(int h, bool bonus, Color color, string bonusT = "")
    {
        health = h;
        maxHealth = h;
        isBonus = bonus;
        bonusType = bonusT;
        baseColor = color;

        meshRenderer = GetComponent<MeshRenderer>();
        mat = new Material(Shader.Find("Standard"));
        mat.color = color;

        if (isBonus)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 3f);
        }
        else
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 0.4f);
        }

        meshRenderer.material = mat;
        UpdateHealthVisual();
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Ball"))
        {
            TakeDamage(1);
        }
    }

    void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
        else
        {
            UpdateHealthVisual();
            StartCoroutine(HitFlash());
        }
    }

    void UpdateHealthVisual()
    {
        if (maxHealth <= 1) return;
        float healthPct = (float)health / maxHealth;
        // Shift color from original to dark-red as health drops
        Color damaged = Color.Lerp(Color.red * 0.5f, baseColor, healthPct);
        mat.color = damaged;
        mat.SetColor("_EmissionColor", damaged * (0.4f + healthPct * 0.6f));
        // Scale down slightly as damaged
        transform.localScale = new Vector3(1f, Mathf.Lerp(0.3f, 0.5f, healthPct), 0.5f);
    }

    IEnumerator HitFlash()
    {
        mat.color = Color.white;
        yield return new WaitForSeconds(0.05f);
        UpdateHealthVisual();
    }

    void Die()
    {
        SpawnExplosion();

        if (isBonus && !string.IsNullOrEmpty(bonusType))
        {
            GameManager.Instance.ApplyPowerUp(bonusType);
        }

        int pts = isBonus ? 50 : (10 * maxHealth);
        GameManager.Instance.AddScore(pts, transform.position);

        Destroy(gameObject);
    }

    void SpawnExplosion()
    {
        // Spawn particles procedurally
        for (int i = 0; i < 12; i++)
        {
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(particle.GetComponent<BoxCollider>());
            particle.transform.position = transform.position;
            particle.transform.localScale = Vector3.one * Random.Range(0.05f, 0.15f);
            MeshRenderer ren = particle.GetComponent<MeshRenderer>();
            Material pMat = new Material(Shader.Find("Standard"));
            pMat.color = baseColor;
            pMat.EnableKeyword("_EMISSION");
            pMat.SetColor("_EmissionColor", baseColor * 2f);
            ren.material = pMat;

            Rigidbody prb = particle.AddComponent<Rigidbody>();
            prb.useGravity = true;
            prb.velocity = new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(2f, 8f),
                Random.Range(-2f, 2f)
            );
            Destroy(particle, 1f);
        }
    }
}
