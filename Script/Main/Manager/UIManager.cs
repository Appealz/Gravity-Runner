using Cysharp.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class UIManager : BaseManager
{
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private HUDView hudView;
    private HUDPresenter hudPresenter;

    private bool startGame = false;
    private float timer;

    [SerializeField] private GameOverView gameOverView;
    private GameOverPresenter gameOverPresenter;

    [SerializeField] private PauseView pauseView;
    private PausePresenter pausePresenter;

    private CancellationTokenSource cts;

    public override UniTask Initialize()
    {        
        startGame = false;

        hudPresenter = new HUDPresenter(new HUDModel(), hudView);
        gameOverPresenter = new GameOverPresenter(new GameOverModel(), gameOverView);
        pausePresenter = new PausePresenter(new PauseModel(), pauseView);
        cts = new CancellationTokenSource();

        return base.Initialize();
    }

    public async UniTask ShowCountdown(int seconds)
    {
        countdownText.gameObject.SetActive(true);

        for (int i = seconds; i > 0; i--)
        {
            countdownText.text = i.ToString();
            await DelayForSecond(1f);
        }

        countdownText.text = "START!";
        await DelayForSecond(1f);

        countdownText.gameObject.SetActive(false);
        startGame = true;
        timer = 0;
    }

    public async UniTask DelayForSecond(float delayTime)
    {
        CancellationToken token = cts.Token;

        float elapseTime = 0f;
        while(elapseTime < delayTime)
        {
            if(GameManager.Instance.State != GameState.Paused)
            {
                elapseTime += Time.unscaledDeltaTime;                
            }
            token.ThrowIfCancellationRequested();
            await UniTask.Yield(token);
        }
    }



    public override void CustomUpdate()
    {
        base.CustomUpdate();
        if (startGame)
        {
            timer += Time.deltaTime;
            hudPresenter.UpdateTimer(timer);
        }

        // 게임 오버 상황에서만 돌아가는 UI 타이머 (Presenter 내부에 체크 로직)
        gameOverPresenter?.Tick();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        hudPresenter.Dispose();
        gameOverPresenter.Dispose();
        pausePresenter.Dispose();

        cts.Cancel();
        cts.Dispose();
    }

}
