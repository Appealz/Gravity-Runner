using UnityEngine;

public class ScrollBackground : MonoBehaviour
{
    private float resetPoint = -40.5f;
    private Vector3 resetPosition = new Vector3(40.5f, 0f, 0f);
    private float scrollSpeed;
    private Vector3 resetOffset = new Vector3(81f, 0f, 0f);

    private void Awake()
    {
        SetSpeed(5f);
    }
    public void ResetPosition()
    {
        transform.position = resetPosition;
    }

    private void Update()
    {
        transform.position += Vector3.left * (scrollSpeed * Time.deltaTime);
        while (transform.position.x <= resetPoint)
        {
            transform.position += resetOffset;
        }
    }

    public void SetSpeed(float newSpeed)
    {
        scrollSpeed = Mathf.Clamp(newSpeed, 0.0f, 15.0f);
    }
}
