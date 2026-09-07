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

    public void PublishFinalScore()
    {
        float finalScore = Mathf.Floor(totalScore);

        bool isNewHigh =
            finalScore > highScore;


        // ============================================
        // Guest
        //
        // 랭킹에는 절대 등록하지 않지만
        // Guest 전용 로컬 최고점수는 저장 가능
        // ============================================
        if (GPGSManager.Instance != null &&
            GPGSManager.Instance.IsGuest)
        {
            if (isNewHigh)
            {
                highScore = finalScore;


                if (AccountManager.Instance.currentAccountData != null)
                {
                    AccountManager.Instance
                        .currentAccountData
                        .bestScore = (long)highScore;

                    AccountManager.Instance.SaveLocalBackup();
                }
            }


            EventBus.Publish(
                new FinalScoreEvent(
                    finalScore,
                    highScore,
                    isNewHigh));


            Debug.Log(
                $"[ScoreManager] Guest 점수 표시 / " +
                $"Score: {finalScore}, High: {highScore}");

            return;
        }


        // ============================================
        // Google 플레이
        //
        // 아직 실제 저장은 하지 않는다.
        // 현재 GameOver UI에 보여줄 값만 계산한다.
        // ============================================
        bool validRun =
            PlaySessionManager.Instance != null &&
            PlaySessionManager.Instance.IsRunActive &&
            PlaySessionManager.Instance.IsValidRun;


        bool showAsNewHigh =
            validRun && isNewHigh;


        float displayHighScore =
            showAsNewHigh
                ? finalScore
                : highScore;


        EventBus.Publish(
            new FinalScoreEvent(
                finalScore,
                displayHighScore,
                showAsNewHigh));


        Debug.Log(
            $"[ScoreManager] 점수 표시 / " +
            $"Score: {finalScore}, " +
            $"Valid: {validRun}");
    }

    public void SetEnable(bool setEnable)
    {
        startGame = setEnable;
    }

    private void OnChangeLevelEvent(ChangeDifficultyEvent evt)
    {
        level = evt.level;
    }

    public async UniTask FinalizeRunScore(bool validRun)
    {
        float finalScore =
            Mathf.Floor(totalScore);


        // ============================================
        // Invalid / Guest 판은 Google 기록 금지
        // ============================================
        if (!validRun ||
            GPGSManager.Instance == null ||
            !GPGSManager.Instance.IsGoogleUser ||
            !GPGSManager.Instance.IsAuthenticatedNow)
        {
            Debug.Log(
                $"[ScoreManager] Unranked 점수 폐기: {finalScore}");

            return;
        }


        var account =
            AccountManager.Instance.currentAccountData;


        if (account == null)
            return;


        // 기존 최고점수를 못 넘었으면
        // 저장할 점수 변화가 없음
        if (finalScore <= highScore)
        {
            Debug.Log(
                $"[ScoreManager] 최고점수 갱신 없음: {finalScore}");

            return;
        }


        // 실패 시 되돌리기 위해 이전 값 보관
        float previousHighScore =
            highScore;

        long previousAccountBest =
            account.bestScore;


        // ============================================
        // 새로운 최고점수 메모리에 반영
        // ============================================
        highScore = finalScore;
        account.bestScore = (long)finalScore;


        // ============================================
        // Cloud 저장
        // ============================================
        bool saveSuccess =
            await AccountManager.Instance.SaveToCloud();


        if (!saveSuccess)
        {
            // Cloud에 기록하지 못했으므로
            // Google 계정 데이터도 이전 상태로 복구
            highScore = previousHighScore;
            account.bestScore = previousAccountBest;

            AccountManager.Instance.SaveLocalBackup();


            Debug.LogWarning(
                "[ScoreManager] Cloud 저장 실패 -> 최고점수 롤백");

            return;
        }


        // ============================================
        // Cloud 저장 성공 후에만 랭킹 등록
        // ============================================
        AccountManager.Instance.ReportScoreToLeaderboard(
            (long)highScore);


        Debug.Log(
            $"[ScoreManager] 최고점수 확정: {highScore}");
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