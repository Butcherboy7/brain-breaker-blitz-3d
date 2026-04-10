using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaddleController : MonoBehaviour
{
    public float speed = 15f;
    public float screenLimit = 8f;

    void Update()
    {
        if (!GameManager.Instance.isPlaying) return;

        float horizontal = Input.GetAxis("Horizontal");
        transform.Translate(Vector3.right * horizontal * speed * Time.deltaTime);

        // Clamp position
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -screenLimit, screenLimit);
        transform.position = pos;
    }

    public void ResetPaddle()
    {
        transform.position = new Vector3(0, -4.5f, 0);
    }
}
