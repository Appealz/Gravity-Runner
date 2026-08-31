using Cysharp.Threading.Tasks;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class DifficultyManager : BaseManager
{
    int level = 0;
    float elapsTime;

    float difficulty = 0f;
    float growth = 1f;    
    [SerializeField] float difficultyInterval = 15f;

    GameConfigSO config;

    public override async UniTask Initialize()
    {
        config = await AddressableLoader.LoadToConfig("GameConfig");

        level = 1;
        if (config != null)
        {
            difficulty = 1f;
            growth = config.initialGrowth;
        }
        else
        {
            difficulty = 1f;
            growth = 1f;
        }

        elapsTime = 0f;

        elapsTime = 0f;

        IsInitialized = true;
    }

    public override void PostInitialize()
    {
        base.PostInitialize();
        EventBus.Publish(new ChangeDifficultyEvent(level, difficulty));
    }
    public override void CustomUpdate()
    {
        base.CustomUpdate();

        if (!IsInitialized || config == null) return;

        elapsTime += Time.deltaTime;

        if (elapsTime > config.difficultyInterval)
        {
            elapsTime = 0f;
            IncreaseDifficulty();
        }
    }

    private void IncreaseDifficulty()
    {        
        level++;

        difficulty += growth;
        growth += config.growthIncreaseStep;

        Debug.Log($"level : {level}, difficulty : {difficulty}");
        EventBus.Publish(new ChangeDifficultyEvent(level, difficulty));
    }

}
public class ChangeDifficultyEvent
{
    public int level;
    public float difficulty;

    public ChangeDifficultyEvent(int newLevel, float difficulty )
    {
        level = newLevel;
        this.difficulty = difficulty;
    }
}