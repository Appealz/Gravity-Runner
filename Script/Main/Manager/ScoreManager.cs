using Cysharp.Threading.Tasks;
using GooglePlayGames;
using System.Threading.Tasks;
using UnityEngine;

public class ScoreManager : BaseManager
{    
    public int level;
    private float timer;
    private float scorePerSecond;
    private float totalScore;    
    private float highScore;
    private bool startGame;

    private GameConfigSO config;

    public float CurrentScore => totalScore;
    public float HighScore => highScore;

    public override async UniTask Initialize()
    {
        config = await AddressableLoader.LoadToConfig("GameConfig");

        scorePerSecond = config != null ? config.baseScorePerSecond : 3.5f;
        level = 1;

        startGame = false;

        if (AccountManager.Instance != null && AccountManager.Instance.currentAccountData != null)
        {
            highScore = AccountManager.Instance.currentAccountData.bestScore;
            Debug.Log($"[ScoreManager] 기존 하이스코어 불러오기 성공: {highScore}");
        }
        else
        {
            highScore = 0;
            Debug.Log("[ScoreManager] AccountManager 데이터 없음, 기본값 0으로 초기화");
        }

        EventBus.Subscribe<RequestAddScoreEvent>(OnRequestAddScore);

        EventBus.Subscribe<ChangeDifficultyEvent>(OnChangeLevelEvent);
        IsInitialized = true;        
    }

    public override void PostInitialize()
    {
        base.PostInitialize();
        EventBus.Publish<InitScoreEvent>(new InitScoreEvent(highScore));
    }

    public override void CustomUpdate()
    {
        base.CustomUpdate();
        if (!startGame)
            return;

        timer += Time.deltaTime;

        // 프레임 드랍 보정: 1초마다 점수 누적
        while (timer >= 1f)
        {
            timer -= 1f;

            float addedScore = level * scorePerSecond;
            totalScore += addedScore;

            EventBus.Publish(new AddScoreEvent(totalScore));
        }
    }

    private void OnRequestAddScore(RequestAddScoreEvent evt)
    {
        totalScore += evt.amount;

        // 현재 UI 갱신을 AddScoreEvent가 담당하고 있으니 이를 통해 알립니다.
        EventBus.Publish(new AddScoreEvent(totalScore));

        Debug.Log($"[ScoreManager] 보너스 점수 반영 완료: {evt.amount}");
    }

    public override void Shutdown()
    {
        base.Shutdown();
        EventBus.Unsubscribe<RequestAddScoreEvent>(OnRequestAddScore);
        EventBus.Unsubscribe<ChangeDifficultyEvent>(OnChangeLevelEvent);
    }

    public async void PublishFinalScore()
    {
        totalScore = Mathf.Floor(totalScore);
        bool isNewHigh = totalScore > highScore;

        // [로직 1, 3번] 실시간 인증 상태 및 네트워크 연결 체크
        bool isOnline = PlayGamesPlatform.Instance.IsAuthenticated() && GPGSManager.Instance.IsNetworkConnected();

        if (isOnline)
        {
            // --- 온라인: 모든 데이터(코인, 점수, 리더보드) 등록 ---
            if (isNewHigh)
            {
                highScore = totalScore;
                AccountManager.Instance.currentAccountData.bestScore = (long)highScore;
                AccountManager.Instance.ReportScoreToLeaderboard((long)highScore); //
            }

            // 코인 누적 로직 (예시: totalScore의 10%를 코인으로 환산)
            // AccountManager.Instance.currentAccountData.coin += (int)(totalScore * 0.1f);

            await AccountManager.Instance.SaveToCloud(); //
            Debug.Log("[ScoreManager] 온라인 저장 완료: 모든 데이터 동기화");
        }
        else
        {
            // --- 오프라인: 하이스코어만 로컬 기록 (로직 3번) ---
            if (isNewHigh)
            {
                highScore = totalScore;
                AccountManager.Instance.currentAccountData.bestScore = (long)highScore;
                AccountManager.Instance.SaveLocalBackup(); //
                Debug.Log("[ScoreManager] 오프라인: 하이스코어만 로컬에 기록됨 (코인/랭킹 미반영)");
            }
        }

        EventBus.Publish(new FinalScoreEvent(totalScore, highScore, isNewHigh && isOnline));
    }

    public void SetEnable(bool setEnable)
    {
        startGame = setEnable;
    }

    private void OnChangeLevelEvent(ChangeDifficultyEvent evt)
    {
        level = evt.level;
    }
}


public class AddScoreEvent
{
    public float score;

    public AddScoreEvent(float newScore)
    {
        score = newScore;
    }
}

public class FinalScoreEvent
{
    public float finalScore;
    public float highScore;
    public bool isNew;

    public FinalScoreEvent(float score, float highScore, bool isNew )
    {
        this.finalScore = score;
        this.highScore = highScore;
        this.isNew = isNew;
    }
}

public class InitScoreEvent
{
    public float highScore;

    public InitScoreEvent(float highScore)
    {
        this.highScore = highScore;
    }
}

public class RequestAddScoreEvent
{
    public float amount;
    public RequestAddScoreEvent(float amount) { this.amount = amount; }
}