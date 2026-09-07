using UnityEngine;

public class MovePlatform : MonoBehaviour
{
    [SerializeField] private float moveDistance = 2f; // 왼쪽으로 이동할 최대 거리
    [SerializeField] private float moveSpeed = 2f;

    private Vector3 initialLocalPos; // 에디터에서 배치한 '오른쪽 끝' 위치
    private float localTime;

    private void Awake()
    {
        // 1. 에디터에서 배치한 가시의 오른쪽 끝 위치를 최초 1회만 저장합니다.
        // OnEnable에서 잡으면 생성 시점의 오차 때문에 위치가 튈 수 있습니다.
        initialLocalPos = transform.localPosition;
    }

    private void OnEnable()
    {
        localTime = 0f;
    }

    private void Update()
    {
        // 게임 플레이 중이 아닐 때는 멈춤 (선택 사항)
        if (GameManager.Instance.State != GameState.Playing) return;

        localTime += Time.deltaTime;

        // 2. 0 ~ moveDistance(2) 사이를 왕복하는 값 생성
        float pingpong = Mathf.PingPong(localTime * moveSpeed, moveDistance);

        // 3. 음수로 만들어 왼쪽으로 이동하게 함
        // 0(오른쪽 끝) -> -2(왼쪽 끝) -> 0(오른쪽 끝) 반복
        float offset = -pingpong;

        transform.localPosition = initialLocalPos + new Vector3(offset, 0, 0);
    }
}
