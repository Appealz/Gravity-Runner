using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlatformSpawner : BaseManager
{
    [SerializeField]
    GameObject[] platforms;

    int minCount = 3;    
    float chunkLength = GameConfig.chunkLength;
    bool gameStart = false;

    float camLeftPos;
    float camRightPos;
    float nextSpawnX;

    Queue<GameObject> chunkQueue = new Queue<GameObject>();

    PlatformMover mover;

    int minLevel;
    int maxLevel;
    

    public override async UniTask Initialize()
    {
        //Debug.Log(camLeftPos);
        mover = FindAnyObjectByType<PlatformMover>();
        if (mover != null)
        {
            GameManager.Instance.Scroll.Register(mover);
            Debug.Log("[PlatformSpawner] PlatformMover를 ScrollManager에 등록 완료.");
        }

        await UniTask.WaitUntil(() => PoolManager.Instance.platformPools.ContainsKey(ChunkType.Chunk_1));

        minLevel = 0;
        maxLevel = 3;
        InitChunkSpawn();
        EventBus.Subscribe<ChangeDifficultyEvent>(ChangeChunkList);
    }

    public void SetCameraValues(float newCamLeft, float newCamRight)
    {
        camLeftPos = newCamLeft;
        camRightPos = newCamRight;
        mover.SetPosition(camLeftPos);
    }

    public override void CustomUpdate()
    {
        if (!gameStart) return;

        if (chunkQueue.Count > 0 && chunkQueue.Peek().transform.position.x < camLeftPos - chunkLength)
        {
            Platform old = chunkQueue.Dequeue().GetComponent<Platform>();
            old.ReturnPool();
            ChunkSpawn();
        }
    }


    private void InitChunkSpawn()
    {
        for(int i = 0; i < minCount; ++i)
        {
            ChunkSpawn((ChunkType)i);
        }
    }

    
    private void ChunkSpawn(ChunkType? forcedType = null)
    {
        ChunkType type = forcedType ?? (ChunkType)Random.Range(minLevel, maxLevel);
        GameObject obj = PoolManager.Instance.platformPools[type].PopObject();

        // Mover의 자식으로 붙임
        obj.transform.SetParent(mover.transform, false);

        // 로컬좌표로 배치 → 항상 정확히 chunkLength 간격 유지
        obj.transform.localPosition = new Vector3(nextSpawnX, 0f, 0f);
        nextSpawnX += chunkLength;

        chunkQueue.Enqueue(obj);

        GameManager.Instance.GetManager<CoinSpawnManager>()?.TrySpawnCoins(obj);
    }

    public void SetRunning(bool running)
    {
        gameStart = running;
    }

    private void ChangeChunkList(ChangeDifficultyEvent evt)
    {
        switch (evt.level)
        {
            case 1:
                minLevel = 0; maxLevel = 2;
                break;
            case 2:
                minLevel = 3; maxLevel = 5;
                break;
            case 3:
            case 4:
                minLevel = 0; maxLevel = 7;
                break;
            case 5:
                minLevel = 4; maxLevel = 8;
                break;
            default: // 6단계 이상
                minLevel = 3; maxLevel = 9;
                break;
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        EventBus.Unsubscribe<ChangeDifficultyEvent>(ChangeChunkList);
    }
}