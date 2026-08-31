using Cysharp.Threading.Tasks;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;
using System;

public class GPGSManager : DontDestroySingleton<GPGSManager>
{
    private bool _isAuthenticating = false;
    public bool IsAuthenticating => _isAuthenticating; // Presenter에서 UI 제어용

    public event Action OnLoginProcessCompleted;
    private int timeoutSeconds;

    protected override void DoAwake()
    {
        base.DoAwake();
        PlayGamesPlatform.Activate();
    }

    private async void Start()
    {
        await UniTask.Delay(500);
        // 1번 항목: 게임 실행 시 자동 로그인 시도
        await StartLoginFlow();
    }

    public async UniTask StartLoginFlow()
    {
        if (_isAuthenticating) return;
        _isAuthenticating = true;

        // 1. 여기서 바로 3초 타임아웃을 파라미터로 던집니다. (에디터/실기기 통합 처리)
        SignInStatus status = await AuthenticateAsync(isManual: false, timeoutSeconds: 3);

        // 2. 결과가 3초 안에 오면 Success, 안 오면 InternalError로 넘어옵니다.
        if (status == SignInStatus.Success || PlayGamesPlatform.Instance.IsAuthenticated())
        {
            await ProcessAuthenticationSuccess();
        }
        else
        {
            ShowToast("로그인에 실패했습니다. 게스트로 진행합니다.");
            LoadLocalDataOnly();
        }

        FinishAuth();
    }

    private void FinishAuth()
    {
        _isAuthenticating = false;
        OnLoginProcessCompleted?.Invoke();
    }

    private async UniTask<SignInStatus> AuthenticateAsync(bool isManual, int timeoutSeconds)
    {
        // 수동 로그인 시 이미 인증되어 있는지 짧게 체크
        if (isManual)
        {
            for (int i = 0; i < 3; i++)
            {
                if (PlayGamesPlatform.Instance.IsAuthenticated()) return SignInStatus.Success;
                await UniTask.Delay(100);
            }
        }

        var tcs = new UniTaskCompletionSource<SignInStatus>();

        // 구글 인증 시도
        if (isManual)
            PlayGamesPlatform.Instance.ManuallyAuthenticate(status => tcs.TrySetResult(status));
        else
            PlayGamesPlatform.Instance.Authenticate(status => tcs.TrySetResult(status));

        // 경주 시작: 서버 응답 vs 사용자가 정의한 타임아웃(timeoutSeconds)
        // 튜플 분해 문법 (성공여부, 결과값)
        var (hasResultLeft, result) = await UniTask.WhenAny(tcs.Task, UniTask.Delay(TimeSpan.FromSeconds(timeoutSeconds)));

        // 3초 안에 응답 왔으면 result 반환, 아니면 에러 반환
        return hasResultLeft ? result : SignInStatus.InternalError;
    }



    public async UniTask<bool> ManualLogin()
    {
        _isAuthenticating = true;

        // 수동 로그인은 유저가 계정을 선택하는 시간이 필요하므로 넉넉하게 15초를 줍니다.
        SignInStatus status = await AuthenticateAsync(isManual: true, timeoutSeconds: 15);

        if (status == SignInStatus.Success)
        {
            await ProcessAuthenticationSuccess();
        }

        _isAuthenticating = false;
        OnLoginProcessCompleted?.Invoke();

        return PlayGamesPlatform.Instance.IsAuthenticated();
    }

    // --- [복구] 이미지 d4b36b, d6e680의 에러를 해결하는 메서드 ---
    public bool IsNetworkConnected()
    {
        // 인터넷 연결 상태 확인 (기존에 사용하시던 로직 복구)
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    private async UniTask ProcessAuthenticationSuccess()
    {
        var user = (PlayGamesLocalUser)PlayGamesPlatform.Instance.localUser;

        int retry = 0;
        // 1. 구글로부터 ID와 닉네임이 완전히 넘어올 때까지 대기
        while ((string.IsNullOrEmpty(user.id) || string.IsNullOrEmpty(user.userName)) && retry < 10)
        {
            await UniTask.Delay(300);
            retry++;
        }

        string id = user.id;
        string name = user.userName;

        // [신분 확인] 로그인이 성공했다면 절대 Guest_ ID를 주지 않습니다.
        if (string.IsNullOrEmpty(id) || id == "LocalUser")
        {
            id = !string.IsNullOrEmpty(name) ? $"Google_{name}" : "Guest_" + Guid.NewGuid().ToString().Substring(0, 8);
        }

        if (string.IsNullOrEmpty(name)) name = "Guest Player";


        AccountManager.Instance.SetAccount(new AccountData(id, name));

        // 3. 클라우드 세이브(.json)를 먼저 불러옵니다.
        await WaitUntilSavedGameReady();
        await AccountManager.Instance.LoadFromCloud();


        await SyncLeaderboardScore();

        AccountManager.Instance.SetLoadedForce();
    }

    // [추가] 리더보드 서버 점수를 가져와서 로컬 데이터와 동기화하는 함수
    private async UniTask SyncLeaderboardScore()
    {
        var tcs = new UniTaskCompletionSource<bool>();

        // GPGSIds.leaderboard_bestscore는 어필님의 리더보드 ID입니다.
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
                    // 리더보드 점수(1012)가 현재 로컬 점수(383)보다 높으면 갱신!
                    AccountManager.Instance.UpdateBestScore(serverScore);
                }
                tcs.TrySetResult(true);
            });

        await tcs.Task;
    }

    private void LoadLocalDataOnly()
    {
        var local = AccountManager.Instance.LoadLocalBackup();
        AccountManager.Instance.SetAccount(local ?? new AccountData("LocalUser", "Guest Player"));

        AccountManager.Instance.SetLoadedForce();
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