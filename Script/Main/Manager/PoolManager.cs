using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public enum ChunkType
{
    Chunk_1,
    Chunk_2,
    Chunk_3,
    Chunk_4,
    Chunk_5,
    Chunk_6,
    Chunk_7,
    Chunk_8,
    Chunk_9,
}

public class PoolManager : DestroySingleton<PoolManager>
{
    public Dictionary<string, ObjectPool> poolDic = new Dictionary<string, ObjectPool>();
    public Dictionary<ChunkType, ObjectPool> platformPools = new Dictionary<ChunkType, ObjectPool>();

    // [추가] 모든 플랫폼 풀이 준비되었는지 확인하는 플래그
    public bool IsAllPoolsReady { get; private set; } = false;

    // [중요] Awake는 찾기만 하고, 로딩 로직은 외부(GameManager)에서 제어하도록 public으로 변경
    public async UniTask InitializePools()
    {
        IsAllPoolsReady = false;

        // 1. 일반 풀 생성 대기
        await UniTask.WhenAll(
            CreatePools("Square", 5),
            CreatePools("Circle", 5),
            CreatePools("Coin", 10)
        );

        // 2. 모든 플랫폼 청크 로딩 대기 (Forget 금지)
        List<UniTask> tasks = new List<UniTask>();
        foreach (ChunkType type in System.Enum.GetValues(typeof(ChunkType)))
        {
            tasks.Add(CreatePlatformPools(type));
        }

        await UniTask.WhenAll(tasks); // 9개 청크가 모두 딕셔너리에 들어올 때까지 대기

        IsAllPoolsReady = true;
        Debug.Log("[PoolManager] 모든 플랫폼 풀 준비 완료!");
    }

    private async UniTask CreatePools(string prefabName, int count = 1)
    {
        GameObject obj = await AddressableLoader.LoadToPrefab(prefabName);
        if (obj != null) poolDic[prefabName] = new ObjectPool(obj, count, transform);
    }

    private async UniTask CreatePlatformPools(ChunkType type, int count = 1)
    {
        GameObject obj = await AddressableLoader.LoadToPrefab(type.ToString());
        if (obj != null) platformPools[type] = new ObjectPool(obj, count, transform);
    }

    //public Dictionary<string, ObjectPool> poolDic = new Dictionary<string, ObjectPool>();
    //public Dictionary<ChunkType, ObjectPool> platformPools = new Dictionary<ChunkType, ObjectPool>();

    //private async void Awake()
    //{
    //    await UniTask.WhenAll(
    //        CreatePools("Square", 5),
    //        CreatePools("Circle", 5),
    //        CreatePools("Coin", 10)
    //    );

    //    foreach (ChunkType type in System.Enum.GetValues(typeof(ChunkType)))
    //    {
    //        CreatePlatformPools(type).Forget();
    //    }
    //}

    //private async UniTask CreatePools(string prefabName, int count = 1)
    //{
    //    GameObject obj = await AddressableLoader.LoadToPrefab(prefabName);
    //    poolDic[prefabName] = new ObjectPool(obj, count, transform);                
    //}    

    //private async UniTask CreatePlatformPools(ChunkType type, int count =1)
    //{
    //    GameObject obj = await AddressableLoader.LoadToPrefab(type.ToString());
    //    platformPools[type] = new ObjectPool(obj, count, transform);

    //}
}
