using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class ObstacleSpawner : BaseManager
{
    [SerializeField]
    Transform[] spawnPoints;

    private GameConfigSO config;
    private float moveSpeed;
    private int currentLevel = 1;
    private bool isRunning = false;
    private CancellationTokenSource cts;
    


    public override async UniTask Initialize()
    {
        // SO 데이터 로드 (ScrollManager와 동일한 방식)
        config = await AddressableLoader.LoadToConfig("GameConfig");

        // 자식 오브젝트들을 스폰 포인트로 자동 등록
        List<Transform> list = new List<Transform>();
        foreach (Transform child in transform) list.Add(child);
        spawnPoints = list.ToArray();

        // [중요] 필요한 풀이 생성될 때까지 안전하게 대기
        await UniTask.WaitUntil(() => PoolManager.Instance.poolDic.ContainsKey("Circle"));
        await UniTask.WaitUntil(() => PoolManager.Instance.poolDic.ContainsKey("Square"));

        // 난이도 변경 이벤트 구독
        EventBus.Subscribe<ChangeDifficultyEvent>(OnDifficultyChanged);

        IsInitialized = true;
    }

    private void OnDifficultyChanged(ChangeDifficultyEvent evt)
    {
        currentLevel = evt.level; // 3레벨 체크를 위해 저장
    }

    public void SetRunning(bool running)
    {
        if (isRunning == running) return;
        isRunning = running;

        if (isRunning)
        {
            cts = new CancellationTokenSource();
            SpawnLoop(cts.Token).Forget(); // 루프 시작
        }
        else
        {
            cts?.Cancel(); // 루프 즉시 종료
        }
    }

    public void StartSpawn()
    {
        if (!isRunning)
            return;

        isRunning = true;
        cts = new CancellationTokenSource();
        SpawnLoop(cts.Token).Forget();
    }

    public async UniTask SpawnLoop(CancellationToken token)
    {
        await UniTask.Delay(2000, cancellationToken: token);

        while (!token.IsCancellationRequested)
        {
            // 3. [가변 스폰 타이밍] 속도가 빨라지면 간격을 좁힘
            // 공식: 기준시간 / (현재속도 / 초기속도)
            float speedRatio = moveSpeed / config.baseMoveSpeed;
            float calculatedTime = config.baseSpawnTime / Mathf.Max(1f, speedRatio);

            float currentSpawnTime = Mathf.Max(config.minSpawnLimit, calculatedTime);

            float randomFactor = Random.Range(config.minSpawnFactor, config.maxSpawnFactor);
            int delayMs = (int)(currentSpawnTime * randomFactor * 1000);

            await UniTask.Delay(delayMs, cancellationToken: token);

            if (isRunning && !token.IsCancellationRequested)
            {
                Spawn();
            }
        }
    }

    public void SetMoveSpeed(float newSpeed)
    {
        this.moveSpeed = newSpeed;
    }

    public void Spawn()
    {
        // 4. [조건부 생성] 레벨 3 이상부터 Circle 등장
        bool canSpawnCircle = currentLevel >= 3;

        // 레벨 3 미만이면 무조건 0(Square), 3 이상이면 0~1(Square/Circle)
        int randomChoice = canSpawnCircle ? Random.Range(0, 2) : 0;

        if (randomChoice == 0)
        {
            GameObject obj = PoolManager.Instance.poolDic["Square"].PopObject();
            obj.transform.position = spawnPoints[Random.Range(0, spawnPoints.Length)].position;

            if (obj.TryGetComponent<IScrollMove>(out var scroll))
                scroll.SetSpeed(moveSpeed+1f);
        }
        else
        {
            GameObject obj = PoolManager.Instance.poolDic["Circle"].PopObject();
            // Circle은 스폰 지점 중 랜덤 혹은 특정 지점 설정 가능
            obj.transform.position = spawnPoints[Random.Range(0, spawnPoints.Length)].position;

            if (obj.TryGetComponent<CircleObstacle>(out var circle))
            {
                circle.SetSpeed(moveSpeed+1f);
            }
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        cts?.Cancel(); // 비동기 루프 파괴
        EventBus.Unsubscribe<ChangeDifficultyEvent>(OnDifficultyChanged);
    }

    public void ClearAllObstacles()
    {
        // 씬에 있는 모든 MonoBehaviour를 찾습니다.
        var activeObstacles = GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (var mono in activeObstacles)
        {
            // IScrollMove 인터페이스를 가진 것들(장애물)만 골라냅니다.
            if (mono is IScrollMove)
            {
                GameObject targetObj = mono.gameObject;

                // 이름에 따라 각각의 풀에 반환합니다. 
                // 이제 PushObject(targetObj)가 가능해졌습니다!
                if (targetObj.name.Contains("Square"))
                {
                    PoolManager.Instance.poolDic["Square"].PushObject(targetObj);
                }
                else if (targetObj.name.Contains("Circle"))
                {
                    PoolManager.Instance.poolDic["Circle"].PushObject(targetObj);
                }
                else
                {
                    Destroy(targetObj);
                }
            }
        }
        Debug.Log("[ObstacleSpawner] 모든 장애물 회수 완료");
    }
}


