using UnityEngine;
using UnityEngine.SceneManagement;

public class PausePresenter
{
    private PauseModel model;
    private PauseView view;

    public PausePresenter(PauseModel model, PauseView view)
    {
        this.model = model;
        this.view = view;

        EventBus.Subscribe<OnPauseEvent>(OnPauseEventHandle);
        view.ResumeBtn.onClick.AddListener(OnResume);
        view.ExitBtn.onClick.AddListener(OnResume);
        view.ReturnLobbyBtn.onClick.AddListener(OnReturnLobby);
        view.RestartBtn.onClick.AddListener(OnRestart);
        view.BGMSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        view.SFXSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        view.SetBGMVolume(model.BGMVolume);
        view.SetSFXVolume(model.SFXVolume);
    }

    private void OnPauseEventHandle(OnPauseEvent e)
    {
        if (GameManager.Instance.State == GameState.GameOver)
            return;

        if (e.isPause)
            view.Show();
        else
            view.Hide();
    }

    private void OnResume()
    {
        EventBus.Publish(new OnPauseEvent(false));
    }

    public void Dispose()
    {
        EventBus.Unsubscribe<OnPauseEvent>(OnPauseEventHandle);

        view.ResumeBtn.onClick.RemoveListener(OnResume);
        view.ExitBtn.onClick.RemoveListener(OnResume);
        view.ReturnLobbyBtn.onClick.RemoveListener(OnReturnLobby);
        view.RestartBtn.onClick.RemoveListener(OnRestart);

        view.BGMSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        view.SFXSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
    }

    private void OnBGMVolumeChanged(float value)
    {
        model.SetBGMVolume(value);
        SoundManager.Instance.SetBGMVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        model.SetSFXVolume(value);
        SoundManager.Instance.SetSFXVolume(value);
    }

    private async void OnRestart()
    {
        bool validRun = PlaySessionManager.Instance.EndRun();

        ScoreManager scoreManager = GameManager.Instance.GetManager<ScoreManager>();

        if (scoreManager != null)
            await scoreManager.FinalizeRunScore(validRun);

        await CurrencyManager.Instance.FinalizeRunCoin(validRun);

        Time.timeScale = 1f;

        view.Hide();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private async void OnReturnLobby()
    {
        bool validRun = PlaySessionManager.Instance.EndRun();

        ScoreManager scoreManager = GameManager.Instance.GetManager<ScoreManager>();

        if (scoreManager != null)
            await scoreManager.FinalizeRunScore(validRun);

        await CurrencyManager.Instance.FinalizeRunCoin(validRun);

        Time.timeScale = 1f;

        SceneManager.LoadScene("LobbyScene");
    }
}

public class OnPauseEvent
{
    public bool isPause;

    public OnPauseEvent(bool isPause)
    {
        this.isPause = isPause;
    }
}