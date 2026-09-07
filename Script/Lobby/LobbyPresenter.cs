using Cysharp.Threading.Tasks;
using GooglePlayGames;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LobbyPresenter
{
    private LobbyModel model;
    private LobbyView view;

    private RankPresenter rankPresenter;
    private PlayerInputSystem inputActions;

    public LobbyPresenter( LobbyModel newModel , LobbyView newView )
    {
        model = newModel;
        view = newView;

        view.PlayBtn.onClick.AddListener(OnPlayClicked);
        view.OptionBtn.onClick.AddListener(OpenOptionClicked);
        view.RankBtn.onClick.AddListener(OnRankClicked);
        view.BGMSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        view.SFXSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        view.CloseOptionBtn.onClick.AddListener(CloseOptionCliked); 
        view.SetBGMVolume(model.BGMVolume);
        view.SetSFXVolume(model.SFXVolume);
       
        view.ExitCanceledBtn.onClick.AddListener(OnClickCancel);
        view.ExitBtn.onClick.AddListener(OnClickExit);

        view.GuestStartBtn.onClick.AddListener(OnGuestStart);
        view.GuestCancelBtn.onClick.AddListener(OnGuestCancel);

        inputActions = new PlayerInputSystem();
        inputActions.UI.Enable();
        inputActions.UI.Cancel.performed += OnExit;

        view.CharacterBtn.onClick.AddListener(OnClickCharacter);

        view.LoginBtn.onClick.AddListener(OnClickLogin);

        GPGSManager.Instance.OnLoginProcessCompleted += OnLoginCompleted;

        view.RefreshLoginUI();

        HandleInitialSignInState().Forget();
    }

    private void OnLoginCompleted()
    {
        view.SetLoadingState(false);
        view.RefreshLoginUI();
    }

    private async UniTaskVoid HandleInitialSignInState()
    {
        await UniTask.Delay(600);

        if (!GPGSManager.Instance.IsAuthenticating)
        {
            view.SetLoadingState(false);
            view.RefreshLoginUI();
            return;
        }

        view.SetLoadingState(true);

        await UniTask.WaitUntil(
            () => !GPGSManager.Instance.IsAuthenticating);

        view.SetLoadingState(false);
        view.RefreshLoginUI();
    }

    public void SetRankPresenter(RankPresenter newRank)
    {
        rankPresenter = newRank;
    }

    private void OnPlayClicked()
    {
        if (!GPGSManager.Instance.IsGoogleUser)
        {
            view.ShowGuestStart();
            return;
        }

        StartGame();
    }

    private void OpenOptionClicked()
    {
        SoundManager.Instance.PlaySFX("TouchOpen");
        view.ShowOption();
    }

    private void CloseOptionCliked()
    {
        SoundManager.Instance.PlaySFX("TouchClose");
        view.HideOption();
    }

    private void OnRankClicked()
    {
        if (GPGSManager.Instance.IsAuthenticating || !AccountManager.Instance.IsLoaded) return;

        SoundManager.Instance.PlaySFX("TouchOpen");
        view.ShowRank();

        var account = AccountManager.Instance.currentAccountData;
        _ = rankPresenter.ShowRank(account.nickname, account.bestScore);
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

    private void OnExit(InputAction.CallbackContext context)
    {
        if (view.IsOptionActive())
        {
            SoundManager.Instance.PlaySFX("TouchClose");
            view.HideOption();
            return;
        }

        
        if (view.IsRankActive())
        {
            SoundManager.Instance.PlaySFX("TouchClose");
            view.HideRank();
            return;
        }

        if (view.IsExitActive())
            view.HideExit();
        else
            view.ShowExit();
    }

    private void OnClickCancel()
    {
        view.HideExit();
    }

    private void OnClickCharacter()
    {
        SoundManager.Instance.PlaySFX("TouchOpen");
        view.ShowCharacterSelect();
    }

    private void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 에디터 실행 종료
#else
    Application.Quit(); // 빌드에서는 앱 종료
#endif
    }

    private async void OnClickLogin()
    {
        SoundManager.Instance.PlaySFX("TouchOpen");
                
        if (GPGSManager.Instance.IsAuthenticating) return;

        view.SetLoadingState(true);

        try
        {
            // 수동 로그인 시도
            bool isSuccess = await GPGSManager.Instance.ManualLogin();

            if (isSuccess)
            {
                Debug.Log("[Lobby] 수동 로그인 성공");
            }
            else
            {
                GPGSManager.Instance.ShowToast("로그인에 실패했습니다. 설정을 확인해주세요.");
            }
        }
        finally
        {            
            view.SetLoadingState(false);
            view.RefreshLoginUI();
        }
    }


    private async void StartGame()
    {
        view.HideGuestStart();

        await Addressables.InitializeAsync().ToUniTask();

        SoundManager.Instance.PlaySFX("GameStart");
        SoundManager.Instance.StopBGM();

        await FadeManager.Instance.WaitToSceneLoad(model.MainSceneName);
    }

    private void OnGuestStart()
    {
        StartGame();
    }

    private void OnGuestCancel()
    {
        view.HideGuestStart();
    }

    public void Dispose()
    {
        view.PlayBtn.onClick.RemoveListener(OnPlayClicked);
        view.OptionBtn.onClick.RemoveListener(OpenOptionClicked);
        view.BGMSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        view.SFXSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        view.CloseOptionBtn.onClick.RemoveListener(CloseOptionCliked);
        view.ExitCanceledBtn.onClick.RemoveListener(OnClickCancel);
        view.ExitBtn.onClick.RemoveListener(OnClickExit);
        view.CharacterBtn.onClick.RemoveListener(OnClickCharacter);
        view.LoginBtn.onClick.RemoveListener(OnClickLogin);
        view.RankBtn.onClick.RemoveListener(OnRankClicked);
        inputActions.UI.Cancel.performed -= OnExit;
        inputActions.UI.Disable();

        if (GPGSManager.Instance != null)
            GPGSManager.Instance.OnLoginProcessCompleted -= OnLoginCompleted;
    }
}