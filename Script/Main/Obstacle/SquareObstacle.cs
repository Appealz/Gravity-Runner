using UnityEngine;

public class SquareObstacle : PoolLabel, IScrollMove
{
    float moveSpeed;
    float moveDelta = 5f;

    private void OnEnable()
    {
        GameManager.Instance.Scroll.Register(this);
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null && GameManager.Instance.Scroll != null)
        {
            GameManager.Instance.Scroll.UnRegister(this);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null && GameManager.Instance.Scroll != null)
        {
            GameManager.Instance.Scroll.UnRegister(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("DestroyZone") || collision.CompareTag("Player"))
        {
            ReturnPool();
        }
    }

    public void MoveUpdate()
    {        
        transform.Translate(Vector3.left * (moveSpeed * Time.deltaTime), Space.World);
        transform.Rotate(Vector3.forward * 180f * Time.deltaTime);        
    }

    public void SetSpeed(float newSpeed)
    {        
        moveSpeed = (newSpeed * 1.1f) + moveDelta;
    }
}
