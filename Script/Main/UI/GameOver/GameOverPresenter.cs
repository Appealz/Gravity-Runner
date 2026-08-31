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
        // 뷰가 꺼져있거나 타이머 대상이 아니면 리턴
        if (!isTick || view == null || !view.gameObject.activeSelf) return;

        // 현재 부활 기회가 있는지 확인
        bool hasChance = model.CanRevive;
        bool isAdReady = AdManager.Instance.IsAdReady();

        // 1. 원격에서 광고가 꺼진 경우 (무료 부활 모드)
        if (RemoteConfigManager.Instance != null && !RemoteConfigManager.Instance.IsAdEnabled)
        {
            // [수정] UpdateContinueUI 호출 (메시지, 버튼활성화, 깜빡임여부)
            view.UpdateContinueUI("Free Revive!", hasChance, hasChance);
            return;
        }

        // 2. 광고 활성화 상태일 때
        if (isAdReady)
        {
            // [수정] 광고 준비 완료 시 깜빡임 활성화
            view.UpdateContinueUI("Continue (Ad)", hasChance, hasChance);
        }
        else
        {
            // 쿨타임 중: 시간 표시, 버튼 비활성화, 깜빡임 중지
            double remaining = AdManager.Instance.GetRemainingCooldownSeconds();
            int mins = (int)remaining / 60;
            int secs = (int)remaining % 60;
            view.UpdateContinueUI($"[ {mins:D2}:{secs:D2} ]", false, false);
        }
    }

    private void OnGameOverEvent(FinalScoreEvent e)
    {
        isTick = true; // 타이머 시작
        view.Show(e.finalScore, e.highScore, model.CanRevive, model.ReviveChance, e.isNew);
    }

    private void OnRestart()
    {
        // 1. 시간 정상화 (가장 중요)
        Time.timeScale = 1f;

        // 2. 현재 화면의 장애물 싹 제거
        if (GameManager.Instance.ObstacleSpawner != null)
        {
            GameManager.Instance.ObstacleSpawner.ClearAllObstacles();
            GameManager.Instance.ObstacleSpawner.SetRunning(false);
        }

        // 3. 모델 리셋 및 씬 로드
        model.Reset();
        view.Hide();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnReturnLobby()
    {

        SceneManager.LoadScene("LobbyScene");
    }

    public void Dispose()
    {
        EventBus.Unsubscribe<FinalScoreEvent>(OnGameOverEvent);
    }
}