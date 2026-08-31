using UnityEngine;

[CreateAssetMenu(fileName = "GameConfigSO", menuName = "Scriptable Objects/GameConfigSO")]
public class GameConfigSO : ScriptableObject
{
    [Header("Movement Settings")]
    public float baseMoveSpeed = 7f;
    public float chunkLength = 40f;

    [Header("Difficulty Settings")]
    public float speedMultiplier = 1.2f; // 레벨당 가중치
    public float maxSpeed = 20f;
    public AnimationCurve speedCurve;
    public float difficultyInterval = 15f;
    public float initialGrowth = 1f;
    public float growthIncreaseStep = 0.5f;

    [Header("Score Setting")]
    public float baseScorePerSecond = 3.5f;

    [Header("Obstacle Spawn Setting")]
    public float baseSpawnTime = 5f;
    public float minSpawnFactor = 0.8f;
    public float maxSpawnFactor = 1.2f;
    public float minSpawnLimit = 1f;

    
}
