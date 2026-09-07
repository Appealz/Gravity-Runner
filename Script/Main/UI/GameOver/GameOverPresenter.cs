using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverPresenter
{
    GameOverModel model;
    GameOverView view;
    private bool isTick = false; // 타이머 작동 여부
    public GameOverPresenter(GameOverModel model, GameOverView view)
    {
        this.model = model;
        this.view = view;


        view.ContinueBtn.onClick.AddListener(OnAdContinue);
        view.ReturnBtn.onClick.AddListener(OnReturnLobby);
        view.RestartBtn.onClick.AddListener (OnRestart);

        EventBus.Subscribe<FinalScoreEvent>(OnGameOverEvent);
    }

    private void OnAdContinue()
    {
        if (!model.CanRevive) return;
        AdManager.Instance.ShowRewardAd(OnAdSuccess);
    }

    private void OnAdSuccess()
    {
        model.UseRevive();
        view.SetContinueChance(model.ReviveChance);

        view.Hide();
        GameManager.Instance.GameReStart().Forget();
    }

    public void Tick()
    {        
        if (!isTick || view == null || !view.gameObject.activeSelf) return;

        bool hasChance = model.CanRevive;
        bool isAdReady = AdManager.Instance.IsAdReady();
                
        if (RemoteConfigManager.Instance != null && !RemoteConfigManager.Instance.IsAdEnabled)
        {            
            view.UpdateContinueUI("Free Revive!", hasChance, hasChance);
            return;
        }
                
        if (isAdReady)
        {        
            view.UpdateContinueUI("Continue (Ad)", hasChance, hasChance);
        }
        else
        {         
            double remaining = AdManager.Instance.GetRemainingCooldownSeconds();
            int mins = (int)remaining / 60;
            int secs = (int)remaining % 60;
            view.UpdateContinueUI($"[ {mins:D2}:{secs:D2} ]", false, false);
        }
    }

    private void OnGameOverEvent(FinalScoreEvent e)
    {
        isTick = true; 
        view.Show(e.finalScore, e.highScore, model.CanRevive, model.ReviveChance, e.isNew);
    }

    private async void OnRestart()
    {
        bool validRun =
            PlaySessionManager.Instance.EndRun();


        // 점수 확정
        ScoreManager scoreManager =
            GameManager.Instance.GetManager<ScoreManager>();


        if (scoreManager != null)
        {
            await scoreManager.FinalizeRunScore(validRun);
        }


        // 골드 확정
        await CurrencyManager.Instance
            .FinalizeRunCoin(validRun);


        Time.timeScale = 1f;


        if (GameManager.Instance.ObstacleSpawner != null)
        {
            GameManager.Instance.ObstacleSpawner
                .ClearAllObstacles();

            GameManager.Instance.ObstacleSpawner
                .SetRunning(false);
        }


        model.Reset();

        view.Hide();


        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex);
    }

    private async void OnReturnLobby()
    {
        bool validRun =
            PlaySessionManager.Instance.EndRun();


        ScoreManager scoreManager =
            GameManager.Instance.GetManager<ScoreManager>();


        if (scoreManager != null)
        {
            await scoreManager.FinalizeRunScore(validRun);
        }


        await CurrencyManager.Instance
            .FinalizeRunCoin(validRun);


        SceneManager.LoadScene("LobbyScene");
    }
    public void Dispose()
    {
        EventBus.Unsubscribe<FinalScoreEvent>(OnGameOverEvent);

        view.ContinueBtn.onClick.RemoveListener(OnAdContinue);
        view.ReturnBtn.onClick.RemoveListener(OnReturnLobby);
        view.RestartBtn.onClick.RemoveListener(OnRestart);
    }
}