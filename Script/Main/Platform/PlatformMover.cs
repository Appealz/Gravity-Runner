using UnityEngine;

public class PlatformMover : MonoBehaviour, IScrollMove
{
    [SerializeField]
    float moveSpeed;    
        

    public void SetPosition(float newPositionX)
    {
        transform.position = new Vector3(newPositionX, 0, 0);
    }

    public void MoveUpdate()
    {
        transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);
    }

    public void SetSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }


}
