using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ScrollManager : BaseManager
{
    float speed = GameConfig.moveSpeed;
    private float baseSpeed;
    private float currentSpeed;
    private float targetSpeed;  // 이번 레벨에서 도달해야 할 최대 속도

    [Header("지수 가속 설정")]
    [SerializeField] private float exponent = 1.1f; // 1.05~1.15 사이 추천
    [SerializeField] private float maxSpeed = 30f;   // 물리적 한계치

    [Header("부드러운 가속 설정")]
    // 이 값이 낮을수록 다음 레벨 목표까지 아주 서서히 올라갑니다.
    [SerializeField] private float accelerationRate = 0.2f;

    private List<IScrollMove> scrollMoves = new List<IScrollMove>();
    private bool isRunning = false;

    private GameConfigSO config;
    public override async UniTask Initialize()
    {
        config = await AddressableLoader.LoadToConfig("GameConfig");

        if (config != null)
        {
            baseSpeed = config.baseMoveSpeed;
            exponent = config.speedMultiplier;

            // [수정] 0이 아니라 baseSpeed에서 시작하게 합니다.
            currentSpeed = baseSpeed;
            targetSpeed = baseSpeed;
        }

        EventBus.Subscribe<ChangeDifficultyEvent>(OnChangeDifficulty);

        IsInitialized = true;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        EventBus.Unsubscribe<ChangeDifficultyEvent>(OnChangeDifficulty);
        scrollMoves.Clear();
    }

    public override void CustomUpdate()
    {
        base.CustomUpdate();

        if (!isRunning) return;

        // [중요] 매 프레임 현재 속도를 목표 속도로 조금씩 올립니다.
        // 다음 레벨 이벤트가 오기 전까지 계속 이 로직에 의해 서서히 빨라집니다.

        targetSpeed += 0.05f * Time.deltaTime;
        // 가속 중일 때만 수치 계산
        if (currentSpeed < targetSpeed)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelerationRate * Time.deltaTime);
        }

        // [중요] 계산된 속도를 '항상' 모든 객체에 전달 (조건문 밖)
        UpdateAllObjectsSpeed();

        for (int i = 0; i < scrollMoves.Count; i++)
        {
            scrollMoves[i]?.MoveUpdate();
        }
    }
    private void UpdateAllObjectsSpeed()
    {
        for (int i = 0; i < scrollMoves.Count; i++)
        {
            scrollMoves[i]?.SetSpeed(currentSpeed);
        }
        GameManager.Instance.ObstacleSpawner?.SetMoveSpeed(currentSpeed);
    }

    public void Register(IScrollMove newObject)
    {
        if(!scrollMoves.Contains(newObject))
        {
            scrollMoves.Add(newObject);
            newObject.SetSpeed(currentSpeed);            
        }            
    }

    public void UnRegister(IScrollMove removeObject)
    {
        if(scrollMoves.Contains(removeObject))
            scrollMoves.Remove(removeObject);
    }

    private void OnChangeDifficulty(ChangeDifficultyEvent newEvent)
    {
        float currentExponent = config.speedCurve.Evaluate(newEvent.level);

        float calculated = baseSpeed + Mathf.Pow(newEvent.difficulty, currentExponent);
        targetSpeed = Mathf.Min(calculated, maxSpeed);

        Debug.Log($"[Scroll] 레벨 {newEvent.level} 도달! 목표 속도 상향: {targetSpeed:F2}");
    }

    private void SetSpeed(float newSpeed)
    {
        //this.speed = newSpeed;
        //for (int i = 0; i < scrollMoves.Count; i++)
        //{
        //    var scroll = scrollMoves[i];
        //    if (scroll != null)
        //        scroll.SetSpeed(speed);
        //}

        this.currentSpeed = newSpeed;
        for (int i = 0; i < scrollMoves.Count; i++)
        {
            var scroll = scrollMoves[i];
            if (scroll != null)
                scroll.SetSpeed(currentSpeed);
        }

        // 추가: ObstacleSpawner의 속도도 여기서 같이 제어하고 계셨죠?
        // GameManager.Instance.ObstacleSpawner가 null이 아닐 때만 호출
        GameManager.Instance.ObstacleSpawner?.SetMoveSpeed(currentSpeed);
    }

    public void SetRunning(bool isOn)
    {
        isRunning = isOn;
    }
}
