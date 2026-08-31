using UnityEngine;

public class CircleObstacle : PoolLabel
{
    float moveSpeed;
    float jumpForce;
    float jumpInterval;
    Rigidbody2D rb;

    float timer;
    private void Awake()
    {
        if(!TryGetComponent<Rigidbody2D>(out rb))
        {
            Debug.Log("rb! Rigidbody2D is missing");
        }

        moveSpeed = GameConfig.moveSpeed + 3f;
        jumpForce = 6f;
        jumpInterval = 1f;
    }

    public void SetSpeed(float speed)
    {
        moveSpeed = speed + 3f;
    }

    private void Jump()
    {
        rb.linearVelocity = Vector3.left * moveSpeed; // 수평 속도 고정
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Ground"))
        {
            Jump();
        }
        else if(collision.collider.transform.parent != null && collision.collider.transform.parent.CompareTag("Ground"))
        {
            Jump();
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            ReturnPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("DestroyZone") || collision.CompareTag("Player"))
        {
            ReturnPool();
        }
    }

}

