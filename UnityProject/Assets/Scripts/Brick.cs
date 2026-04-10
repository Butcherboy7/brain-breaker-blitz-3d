using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Brick : MonoBehaviour
{
    public int health;
    public int maxHealth;
    public bool isBonus;
    public string bonusType;
    private Color baseColor;
    private Material mat;

    public void Initialize(int h, bool bonus, Color color, string bt = "")
    {
        health = h; maxHealth = h;
        isBonus = bonus; bonusType = bt;
        baseColor = color;

        mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * (isBonus ? 3f : 0.5f));
        GetComponent<MeshRenderer>().material = mat;
        UpdateVisual();
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Ball")) Hit();
    }

    void Hit()
    {
        health--;
        if (health <= 0) Die();
        else { UpdateVisual(); StartCoroutine(Flash()); }
    }

    void UpdateVisual()
    {
        if (maxHealth <= 1) return;
        float pct = (float)health / maxHealth;
        Color c = Color.Lerp(new Color(0.6f, 0f, 0f), baseColor, pct);
        mat.color = c;
        mat.SetColor("_EmissionColor", c * (0.3f + pct * 0.7f));
        transform.localScale = new Vector3(
            transform.localScale.x,
            Mathf.Lerp(0.30f, 0.5f, pct),
            transform.localScale.z);
    }

    IEnumerator Flash()
    {
        mat.color = Color.white;
        yield return new WaitForSeconds(0.05f);
        UpdateVisual();
    }

    void Die()
    {
        Explode();
        if (isBonus && bonusType != "" && GameManager.Instance)
            GameManager.Instance.ApplyPowerUp(bonusType);
        if (GameManager.Instance)
            GameManager.Instance.AddScore(isBonus ? 50 : 10 * maxHealth, transform.position);
        Destroy(gameObject);
    }

    void Explode()
    {
        for (int i = 0; i < 14; i++)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(p.GetComponent<BoxCollider>());
            p.transform.position = transform.position;
            p.transform.localScale = Vector3.one * Random.Range(0.05f, 0.18f);
            var pm = new Material(Shader.Find("Standard"));
            pm.color = baseColor;
            pm.EnableKeyword("_EMISSION");
            pm.SetColor("_EmissionColor", baseColor * 2.5f);
            p.GetComponent<MeshRenderer>().material = pm;
            Rigidbody prb = p.AddComponent<Rigidbody>();
            prb.velocity = new Vector3(Random.Range(-6f, 6f), Random.Range(3f, 9f), Random.Range(-3f, 3f));
            prb.angularVelocity = Random.insideUnitSphere * 10f;
            Destroy(p, 1.2f);
        }
    }
}
