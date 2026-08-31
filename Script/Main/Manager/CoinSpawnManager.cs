using Cysharp.Threading.Tasks;
using UnityEngine;

public class CoinSpawnManager : BaseManager
{
    [Header("기본 설정")]
    [SerializeField, Range(0f, 1f)] private float baseSpawnChance = 0.3f;
    [SerializeField] private int baseSpawnCount = 1;
    [SerializeField] private int maxSpawnCount = 5;

    private float currentChance;
    private int currentCount;

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }

    public override UniTask Initialize()
    {        
        EventBus.Subscribe<ChangeDifficultyEvent>(OnDifficultyChanged);
        currentChance = baseSpawnChance;
        currentCount = baseSpawnCount;

        return base.Initialize();
    }



    private void OnDifficultyChanged(ChangeDifficultyEvent e)
    {
        // 난이도(또는 레벨)에 따라 확률/개수 증가
        currentChance = Mathf.Clamp01(baseSpawnChance + (e.level - 1) * 0.1f);
        currentCount = Mathf.Min(baseSpawnCount + Mathf.FloorToInt(e.level / 2f), maxSpawnCount);

        Debug.Log($"[CoinSpawnManager] 난이도 {e.level}, 스폰확률 {currentChance * 100:F0}%, 개수 {currentCount}");
    }

    // PlatformSpawner에서 호출
    public void TrySpawnCoins(GameObject chunk)
    {
        if (Random.value > currentChance)
            return;

        var spawners = chunk.GetComponentsInChildren<CoinSpawner>();
        if (spawners.Length == 0)
            return;

        int spawnCount = Mathf.Min(currentCount, spawners.Length);

        for (int i = 0; i < spawnCount; i++)
        {
            var spawner = spawners[Random.Range(0, spawners.Length)];
            spawner.SpawnCoin();
        }

        Debug.Log($"[CoinSpawnManager] 코인 {spawnCount}개 스폰 (확률 {currentChance * 100:F1}%)");
    }

    public override void Shutdown()
    {
        base.Shutdown();
        EventBus.Unsubscribe<ChangeDifficultyEvent>(OnDifficultyChanged);
    }
}
