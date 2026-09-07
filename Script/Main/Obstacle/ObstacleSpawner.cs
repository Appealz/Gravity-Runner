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
        config = await AddressableLoader.LoadToConfig("GameConfig");
             
        List<Transform> list = new List<Transform>();
        foreach (Transform child in transform) list.Add(child);
        spawnPoints = list.ToArray();
                
        await UniTask.WaitUntil(() => PoolManager.Instance.poolDic.ContainsKey("Circle"));
        await UniTask.WaitUntil(() => PoolManager.Instance.poolDic.ContainsKey("Square"));
                
        EventBus.Subscribe<ChangeDifficultyEvent>(OnDifficultyChanged);

        IsInitialized = true;
    }

    private void OnDifficultyChanged(ChangeDifficultyEvent evt)
    {
        currentLevel = evt.level; 
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
        bool canSpawnCircle = currentLevel >= 3;
                
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
        cts?.Cancel(); 
        EventBus.Unsubscribe<ChangeDifficultyEvent>(OnDifficultyChanged);
    }

    public void ClearAllObstacles()
    {        
        var activeObstacles = GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (var mono in activeObstacles)
        {            
            if (mono is IScrollMove)
            {
                GameObject targetObj = mono.gameObject;
                         
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


