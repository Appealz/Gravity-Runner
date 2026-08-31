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
        view.RankBtn.onClick.AddListener(() => OnRankClicked());
        view.BGMSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        view.SFXSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        view.CloseOptionBtn.onClick.AddListener(CloseOptionCliked); 
        view.SetBGMVolume(model.BGMVolume);
        view.SetSFXVolume(model.SFXVolume);
       
        view.ExitCanceledBtn.onClick.AddListener(OnClickCancel);
        view.ExitBtn.onClick.AddListener(OnClickExit);

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
        // [중요] GPGSManager.Start의 Delay(500)보다 살짝 더 기다려야 
        // IsAuthenticating이 true가 된 것을 확인할 수 있음
        await UniTask.Delay(600);

        if (!GPGSManager.Instance.IsAuthenticating)
        {
            view.SetLoadingState(false);
            view.RefreshLoginUI();
            return;
        }

        // 로그인 중이면 깜빡임 시작
        view.SetLoadingState(true);

        // 최대 6초 대기 (이후엔 타임아웃으로 강제 진행)
        try
        {
            await UniTask.WaitUntil(() => !GPGSManager.Instance.IsAuthenticating)
                         .Timeout(TimeSpan.FromSeconds(6));
        }
        catch { }

        view.SetLoadingState(false);
        view.RefreshLoginUI();
    }

    public void SetRankPresenter(RankPresenter newRank)
    {
        rankPresenter = newRank;
    }

    private async void OnPlayClicked()
    {
        // 8번 항목: 비로그인 상태로 Play 시 토스트 메시지 출력
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            GPGSManager.Instance.ShowToast("게스트 계정은 랭킹에 기록되지 않습니다.");
        }

        await Addressables.InitializeAsync().ToUniTask();
        SoundManager.Instance.PlaySFX("GameStart");
        SoundManager.Instance.StopBGM();
        await FadeManager.Instance.WaitToSceneLoad(model.MainSceneName);
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

        // [수정] 여기서 띄우던 게스트 토스트 로직을 아예 삭제하세요!
        // 창만 열어주고, 판단은 RankPresenter에게 맡깁니다.
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

        // [보강] 이미 로딩 중이면 중복 클릭 방지
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
            // [중요] 성공하든 실패하든, 에러가 나든 버튼은 무조건 다시 풀어준다!
            view.SetLoadingState(false);
            view.RefreshLoginUI();
        }
    }




    public void Dispose()
    {
        inputActions.UI.Cancel.performed -= OnExit;
        if (GPGSManager.Instance != null)
            GPGSManager.Instance.OnLoginProcessCompleted -= OnLoginCompleted;
    }
}