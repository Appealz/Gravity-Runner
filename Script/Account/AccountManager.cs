using Cysharp.Threading.Tasks;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;

public class AccountManager : DontDestroySingleton<AccountManager>
{
    public AccountData currentAccountData { get; private set; }

    private const string saveFileName = "Account";
    private string LocalPath => Path.Combine(Application.persistentDataPath, "account.json");

    public bool IsLoaded { get; private set; }
    private bool isSaving = false; // [유지] 지우지 마세요!

    // [수정] 수동으로 true/false를 넣지 않고, ID를 보고 자동으로 판정합니다.
    public bool IsGuestAccount => currentAccountData == null ||
                                  currentAccountData.acountID == "LocalUser" ||
                                  currentAccountData.acountID.StartsWith("Guest_");

    public void SetAccount(AccountData accountData)
    {
        currentAccountData = accountData;
        SaveLocalBackup();
        Debug.Log($"[Account] 데이터 갱신 완료: {accountData.nickname}");
    }

    // [추가] 리더보드 점수와 동기화할 때 사용할 메서드
    public void UpdateBestScore(int newScore)
    {
        if (currentAccountData == null) return;
        if (newScore > currentAccountData.bestScore)
        {
            currentAccountData.bestScore = newScore;
            SaveLocalBackup();
        }
    }

    public async UniTask LoadFromCloud()
    {
        IsLoaded = false;
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            LoadLocalOrNew();
            return;
        }

        AccountData cloudData = await InternalReadFromCloud();

        if (cloudData != null)
        {
            ApplyLoadedData(cloudData);
            // [삭제] IsGuestAccount = false; -> 이제 ID 보고 자동 판정하므로 필요 없음
        }
        else
        {
            LoadLocalOrNew();
        }

        IsLoaded = true;
    }

    private async UniTask<AccountData> InternalReadFromCloud()
    {
        var tcs = new UniTaskCompletionSource<AccountData>();

        // 구글 Saved Game API를 사용해 'Account' 파일을 엽니다.
        PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(
            saveFileName,
            DataSource.ReadCacheOrNetwork,
            ConflictResolutionStrategy.UseLongestPlaytime,
            (status, game) => {
                if (status != SavedGameRequestStatus.Success)
                {
                    tcs.TrySetResult(null); // 열기 실패 시 null 반환
                    return;
                }

                // 파일이 열렸으면 그 안의 바이트 데이터를 읽습니다.
                PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(game, (readStatus, data) => {
                    if (readStatus == SavedGameRequestStatus.Success && data?.Length > 0)
                    {
                        string json = Encoding.UTF8.GetString(data);
                        tcs.TrySetResult(JsonUtility.FromJson<AccountData>(json));
                    }
                    else { tcs.TrySetResult(null); }
                });
            });

        return await tcs.Task;
    }

    private void LoadLocalOrNew()
    {
        // 1. 내 폰에 백업 파일이 있는지 확인
        var local = LoadLocalBackup();

        if (local != null)
        {
            currentAccountData = local;
            Debug.Log("[Account] 로컬 백업 데이터 로드 완료");
        }
        else
        {
            // 2. 백업도 없다면 완전 신규! (Guid로 고유 아이디 부여)
            string guestId = System.Guid.NewGuid().ToString();
            currentAccountData = new AccountData(guestId, "Guest");
            Debug.Log("[Account] 신규 게스트 데이터 생성 완료");
        }

        // 3. [어필님의 제약 조건] 로그인 안 된 상태면 무조건 기본 캐릭터로!
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            currentAccountData.selectedCharacterId = "Char_0";
            // 코인은 지급 로직(인게임)에서 막히겠지만, 데이터도 안전하게 게스트 플래그를 세웁니다.
        }        
        IsLoaded = true;
    }

    private void ApplyLoadedData(AccountData cloudData)
    {
        // 1. [최초 실행 방어] 현재 메모리에 데이터가 아예 없다면 서버 데이터를 그대로 할당합니다.
        if (currentAccountData == null)
        {
            currentAccountData = cloudData;
        }
        else
        {
            if (currentAccountData.bestScore > cloudData.bestScore)
            {
                Debug.Log($"[Account] 게스트 점수({currentAccountData.bestScore})가 더 높음! 서버 데이터를 갱신합니다.");
                cloudData.bestScore = currentAccountData.bestScore;
            }


            currentAccountData.acountID = cloudData.acountID;
            currentAccountData.nickname = cloudData.nickname;
            currentAccountData.bestScore = cloudData.bestScore; // 위에서 비교된 결과값
            currentAccountData.coin = cloudData.coin;           // 서버 코인으로 덮어쓰기
            currentAccountData.selectedCharacterId = cloudData.selectedCharacterId;
            currentAccountData.unlockedCharacterIds = cloudData.unlockedCharacterIds;
            currentAccountData.lastLoginData = cloudData.lastLoginData;
        }

        // 4. [동기화 완료] 서버와 합쳐진 최신 데이터를 즉시 로컬(내 폰)에도 백업해둡니다.
        SaveLocalBackup();
        Debug.Log("[Account] 클라우드와 로컬 데이터 동기화 완료!");
    }

    public async UniTask SaveToCloud()
    {
        // 1. [방어 코드] 저장할 데이터가 없거나 이미 저장 중이면 중단
        if (currentAccountData == null || isSaving) return;
        isSaving = true;

        // 2. [데이터 준비] 객체를 JSON 문자열로, 다시 바이트 배열로 변환
        string json = JsonUtility.ToJson(currentAccountData);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        // 3. [어필님의 핵심 조건] 인증 및 인터넷 체크
        // 로그인이 안 되어 있거나 인터넷이 없으면 클라우드 저장을 포기하고 로컬 백업만 합니다.
        if (!PlayGamesPlatform.Instance.IsAuthenticated() || Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("[Account] 클라우드 저장 불가 상태 -> 로컬 백업만 진행");
            SaveLocalBackup();
            isSaving = false;
            return;
        }

        // 4. [실행] 클라우드 금고 열기 및 데이터 쓰기
        var tcs = new UniTaskCompletionSource<bool>();

        PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(
            saveFileName, DataSource.ReadCacheOrNetwork, ConflictResolutionStrategy.UseLongestPlaytime,
            (status, game) => {
                if (status == SavedGameRequestStatus.Success)
                {
                    // 저장 시점의 설명을 메타데이터로 기록 (예: "2026-01-27 저장됨")
                    var update = new SavedGameMetadataUpdate.Builder()
                        .WithUpdatedDescription("Saved at " + System.DateTime.Now).Build();

                    // 실제로 데이터를 서버에 커밋(Commit)합니다.
                    PlayGamesPlatform.Instance.SavedGame.CommitUpdate(game, update, bytes, (saveStatus, _) => {
                        tcs.TrySetResult(saveStatus == SavedGameRequestStatus.Success);
                    });
                }
                else { tcs.TrySetResult(false); }
            });

        bool success = await tcs.Task;

        // 5. [후속 처리] 저장이 성공했다면 리더보드에도 점수를 보고합니다.
        if (success)
        {
            Debug.Log("[Account] 클라우드 저장 성공!");
            if (currentAccountData.bestScore > 0)
            {
                ReportScoreToLeaderboard(currentAccountData.bestScore);
            }
        }
        else
        {
            // 서버 저장 실패 시 보험으로 로컬에라도 남깁니다.
            SaveLocalBackup();
        }

        isSaving = false;
    }

    public void SaveLocalBackup()
    {
        if (currentAccountData == null) return;

        try
        {
            string json = JsonUtility.ToJson(currentAccountData);
            File.WriteAllText(LocalPath, json);
            // Debug.Log($"[Account] 로컬 백업 성공: {LocalPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Account] 로컬 백업 실패: {e.Message}");
        }
    }

    public async UniTask WaitUntilLoaded()
    {
        await UniTask.WaitUntil(() => IsLoaded);
    }

    public void ReportScoreToLeaderboard(long score)
    {
        // [방어 코드] 로그인 상태가 아니면 리더보드에 접근할 수 없습니다.
        if (!PlayGamesPlatform.Instance.IsAuthenticated()) return;

        // GPGS 리더보드 ID를 넣어서 점수를 보고합니다.
        // "YOUR_LEADERBOARD_ID" 부분은 나중에 구글 콘솔에서 생성한 ID로 바꿔주세요!
        PlayGamesPlatform.Instance.ReportScore(score, "YOUR_LEADERBOARD_ID", (success) => {
            if (success) Debug.Log($"[Account] 리더보드 점수 등록 성공: {score}");
            else Debug.LogWarning("[Account] 리더보드 점수 등록 실패");
        });
    }

    public AccountData LoadLocalBackup()
    {
        // 내 폰에 저장된 파일이 있는지 확인
        if (!File.Exists(LocalPath)) return null;

        try
        {
            string json = File.ReadAllText(LocalPath);
            return JsonUtility.FromJson<AccountData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Account] 로컬 백업 읽기 실패: {e.Message}");
            return null;
        }
    }

    public void SetLoadedForce()
    {
        IsLoaded = true;
        Debug.Log("[Account] 로딩 상태 강제 완료 (버튼 잠금 해제)");
    }

    private void OnApplicationQuit()
    {
        // 게임 종료 시 현재까지의 데이터를 로컬에 안전하게 남깁니다.
        SaveLocalBackup();
    }  

}
