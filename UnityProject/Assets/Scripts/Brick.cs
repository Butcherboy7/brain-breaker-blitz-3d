using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Brick : MonoBehaviour
{
    public int health = 1;
    public bool isBonus = false;
    public MeshRenderer meshRenderer;

    public void Initialize(int h, bool bonus, Color color)
    {
        health = h;
        isBonus = bonus;
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material.color = color;
        
        if (isBonus)
        {
            meshRenderer.material.EnableKeyword("_EMISSION");
            meshRenderer.material.SetColor("_EmissionColor", color * 2f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            TakeDamage(1);
        }
    }

    void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            GameManager.Instance.AddScore(isBonus ? 50 : 10);
            Destroy(gameObject);
        }
        else
        {
            // Optional: change color or scale to show damage
            transform.localScale *= 0.95f;
        }
    }
}
