using UnityEngine;

public class MovePlatform : MonoBehaviour
{
    [SerializeField] private float moveDistance = 2f; 
    [SerializeField] private float moveSpeed = 2f;

    private Vector3 initialLocalPos; 
    private float localTime;

    private void Awake()
    {        
        initialLocalPos = transform.localPosition;
    }

    private void OnEnable()
    {
        localTime = 0f;
    }

    private void Update()
    {        
        if (GameManager.Instance.State != GameState.Playing) return;

        localTime += Time.deltaTime;
             
        float pingpong = Mathf.PingPong(localTime * moveSpeed, moveDistance);
        float offset = -pingpong;

        transform.localPosition = initialLocalPos + new Vector3(offset, 0, 0);
    }
}
