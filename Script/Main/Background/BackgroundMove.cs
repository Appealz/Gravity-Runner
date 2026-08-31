using UnityEngine;

public class BackgroundMove : MonoBehaviour,IScrollMove
{
    private float resetPoint = -40.85f;
    private Vector3 resetPosition = new Vector3(40.85f, 0f, 0f);
    private float scrollSpeed;
    private float backgroundWidth = 81.7f; // 배경 1장의 전체 너비 (40.85 * 2)

    private void Start()
    {
        GameManager.Instance.Scroll.Register(this);
    }

    public void ResetPosition()
    {
        transform.position = resetPosition;
    }

    public void MoveUpdate()
    {
        transform.position += Vector3.left * (scrollSpeed * Time.deltaTime);

        // 프레임 드랍 때문에 너무 많이 넘어가도 보정
        while (transform.position.x <= resetPoint)
        {
            transform.position += Vector3.right * backgroundWidth;
        }
    }

    public void SetSpeed(float newSpeed)
    {
        scrollSpeed = Mathf.Clamp(newSpeed, 0.0f, 15.0f);
    }
}
