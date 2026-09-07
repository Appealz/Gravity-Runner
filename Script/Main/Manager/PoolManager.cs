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
        
    public bool IsAllPoolsReady { get; private set; } = false;
        
    public async UniTask InitializePools()
    {
        IsAllPoolsReady = false;

        await UniTask.WhenAll(
            CreatePools("Square", 5),
            CreatePools("Circle", 5),
            CreatePools("Coin", 10)
        );

        List<UniTask> tasks = new List<UniTask>();
        foreach (ChunkType type in System.Enum.GetValues(typeof(ChunkType)))
        {
            tasks.Add(CreatePlatformPools(type));
        }

        await UniTask.WhenAll(tasks); 

        IsAllPoolsReady = true;
        Debug.Log("[PoolManager] ¸ðµç ÇÃ·§Æû Ç® ÁØºñ ¿Ï·á!");
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
}
