using Cysharp.Threading.Tasks;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;
using System;

public enum LoginState
{
    None,
    Authenticating,
    Google,
    Guest
}


public class GPGSManager : DontDestroySingleton<GPGSManager>
{
    private bool _isAuthenticating = false;
    public bool IsAuthenticating => _isAuthenticating; // Presenter에서 UI 제어용

    public event Action OnLoginProcessCompleted;
    private int timeoutSeconds;

    public LoginState CurrentLoginState { get; private set; } = LoginState.None;

    public bool IsGoogleUser => CurrentLoginState == LoginState.Google;
    public bool IsGuest => CurrentLoginState == LoginState.Guest;
    public bool IsAuthenticatedNow =>
    PlayGamesPlatform.Instance != null &&
    PlayGamesPlatform.Instance.IsAuthenticated();

    protected override void DoAwake()
    {
        base.DoAwake();
        PlayGamesPlatform.Activate();
    }

    private async void Start()
    {
        await UniTask.Delay(500);        
        await StartLoginFlow();
    }

    public async UniTask StartLoginFlow()
    {
        if (_isAuthenticating)
            return;

        _isAuthenticating = true;
        CurrentLoginState = LoginState.Authenticating;

        SignInStatus status = await AuthenticateAsync(isManual: true);

        if (status == SignInStatus.Success ||
            PlayGamesPlatform.Instance.IsAuthenticated())
        {
            await ProcessAuthenticationSuccess();
        }
        else
        {
            ShowToast("로그인에 실패했습니다. 게스트로 진행합니다.");

            EnterGuestMode();
        }

        FinishAuth();
    }

    private void FinishAuth()
    {
        _isAuthenticating = false;
        OnLoginProcessCompleted?.Invoke();
    }

    private async UniTask<SignInStatus> AuthenticateAsync(bool isManual)
    {
        if (isManual)
        {
            for (int i = 0; i < 3; i++)
            {
                if (PlayGamesPlatform.Instance.IsAuthenticated())
                    return SignInStatus.Success;

                await UniTask.Delay(100);
            }
        }

        var tcs = new UniTaskCompletionSource<SignInStatus>();

        if (isManual)
        {
            PlayGamesPlatform.Instance.ManuallyAuthenticate(status => tcs.TrySetResult(status));
        }
        else
        {
            PlayGamesPlatform.Instance.Authenticate(status => tcs.TrySetResult(status));
        }

        return await tcs.Task;
    }



    public async UniTask<bool> ManualLogin()
    {
        if (_isAuthenticating)
            return false;

        _isAuthenticating = true;
        CurrentLoginState = LoginState.Authenticating;

        try
        {
            SignInStatus status = await AuthenticateAsync(isManual: true);

            if (status == SignInStatus.Success ||
                PlayGamesPlatform.Instance.IsAuthenticated())
            {
                await ProcessAuthenticationSuccess();
            }
            else
            {
                ShowToast("로그인에 실패했습니다.");
                EnterGuestMode();
            }

            return CurrentLoginState == LoginState.Google;
        }
        finally
        {
            FinishAuth();
        }
    }

    public bool IsNetworkConnected()
    {        
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    private async UniTask ProcessAuthenticationSuccess()
    {
        var user =
            (PlayGamesLocalUser)PlayGamesPlatform.Instance.localUser;

        int retry = 0;


        // Google ID와 닉네임 준비 대기
        while ((string.IsNullOrEmpty(user.id) ||
                string.IsNullOrEmpty(user.userName)) &&
               retry < 10)
        {
            await UniTask.Delay(300);
            retry++;
        }


        string id = user.id;
        string name = user.userName;


        // Google 인증은 됐다고 나왔지만
        // 실제 Google ID를 가져오지 못한 경우
        if (string.IsNullOrEmpty(id) ||
            id == "LocalUser")
        {
            Debug.LogWarning(
                "[GPGS] Google 사용자 ID를 가져오지 못했습니다.");

            EnterGuestMode();

            return;
        }


        if (string.IsNullOrEmpty(name))
            name = "Player";


        // 일단 신규 Google 기본 데이터를
        // 메모리에만 준비한다.
        //
        // Cloud 데이터가 있으면 이후 덮어쓰게 되고,
        // Cloud 데이터가 정말 없을 때만 신규 계정으로 사용된다.
        AccountManager.Instance.SetAccount(
            new AccountData(id, name),
            false);


        // Saved Game 준비 대기
        await WaitUntilSavedGameReady();


        // Cloud 데이터 로드
        bool cloudLoadSuccess =
            await AccountManager.Instance.LoadFromCloud();


        // Cloud 자체를 못 읽었다면
        // 신규 Google 계정으로 착각하면 안 된다.
        if (!cloudLoadSuccess)
        {
            Debug.LogWarning(
                "[GPGS] 계정 데이터를 안전하게 불러오지 못했습니다.");

            ShowToast(
                "계정 데이터를 불러오지 못했습니다. 게스트 모드로 진행합니다.");

            EnterGuestMode();

            return;
        }


        // 여기까지 왔다는 것은
        //
        // 1. 기존 Cloud 데이터를 정상적으로 읽었거나
        // 2. Cloud에 데이터가 정말 없다는 것을 정상적으로 확인했거나
        //
        // 둘 중 하나다.
        CurrentLoginState = LoginState.Google;


        // Google 리더보드 점수 동기화
        await SyncLeaderboardScore();


        AccountManager.Instance.SetLoadedForce();


        Debug.Log(
            "[GPGS] Google 로그인 및 계정 데이터 로드 완료");
    }

    private void EnterGuestMode()
    {
        CurrentLoginState = LoginState.Guest;


        // Guest 전용 저장 파일 확인
        AccountData guestData =
            AccountManager.Instance.LoadGuestBackup();


        if (guestData == null)
        {
            guestData = new AccountData(
                "Guest_" + Guid.NewGuid().ToString(),
                "Guest Player");

            Debug.Log(
                "[GPGS] 신규 Guest 데이터를 생성했습니다.");
        }
        else
        {
            Debug.Log(
                "[GPGS] 기존 Guest 데이터를 불러왔습니다.");
        }


        // Guest 규칙 강제
        guestData.nickname = "Guest Player";

        guestData.coin = 0;

        guestData.selectedCharacterId = "Char_0";

        guestData.unlockedCharacterIds.Clear();
        guestData.unlockedCharacterIds.Add("Char_0");


        // SetAccount() → SaveLocalBackup()
        // Guest_ ID이므로 guest_account.json으로 저장됨
        AccountManager.Instance.SetAccount(guestData);


        AccountManager.Instance.SetLoadedForce();

        Debug.Log("[GPGS] Guest 모드로 진입했습니다.");
    }

    private async UniTask SyncLeaderboardScore()
    {
        var tcs = new UniTaskCompletionSource<bool>();
                
        PlayGamesPlatform.Instance.LoadScores(
            GPGSIds.leaderboard_bestscore,
            LeaderboardStart.PlayerCentered,
            1,
            LeaderboardCollection.Public,
            LeaderboardTimeSpan.AllTime,
            (data) => {
                if (data.Valid && data.PlayerScore != null)
                {
                    int serverScore = (int)data.PlayerScore.value;                    
                    AccountManager.Instance.UpdateBestScore(serverScore);
                }
                tcs.TrySetResult(true);
            });

        await tcs.Task;
    }


    private async UniTask WaitUntilSavedGameReady()
    {
        for (int i = 0; i < 10; i++)
        {
            if (PlayGamesPlatform.Instance?.SavedGame != null) return;
            await UniTask.Delay(500);
        }
    }

    public void ShowToast(string message)
    {
#if !UNITY_EDITOR && UNITY_ANDROID
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity?.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                using (var toastClass = new AndroidJavaClass("android.widget.Toast"))
                {
                    var toast = toastClass.CallStatic<AndroidJavaObject>("makeText", activity, message, 0);
                    toast.Call("show");
                }
            }));
        }
#else
        Debug.Log($"[Toast] {message}");
#endif
    }
}