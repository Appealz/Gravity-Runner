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

    // Google 계정용 로컬 백업
    private string LocalPath => Path.Combine(Application.persistentDataPath, "account.json");

    // Guest 계정용 로컬 백업
    private string GuestLocalPath => Path.Combine(Application.persistentDataPath, "guest_account.json");

    public bool IsLoaded { get; private set; }
    private bool isSaving = false; 

    private enum CloudReadStatus
    {
        Success,    // 정상적으로 데이터 읽음
        NoData,     // 읽기는 성공했지만 저장된 데이터가 없음
        Failed      // Cloud 접근/읽기 자체가 실패
    }

    private class CloudReadResult
    {
        public CloudReadStatus Status;
        public AccountData Data;

        public CloudReadResult(
            CloudReadStatus status,
            AccountData data = null)
        {
            Status = status;
            Data = data;
        }
    }

        
    public bool IsGuestAccount =>
        currentAccountData == null ||
        string.IsNullOrEmpty(currentAccountData.acountID) ||
        currentAccountData.acountID == "LocalUser" ||
        currentAccountData.acountID.StartsWith("Guest_");



    public void SetAccount(AccountData accountData, bool saveLocal = true)
    {
        currentAccountData = accountData;

        if (saveLocal)
            SaveLocalBackup();

        Debug.Log($"[Account] 데이터 갱신 완료: {accountData.nickname}");
    }

    
    public void UpdateBestScore(int newScore)
    {
        if (currentAccountData == null) return;
        if (newScore > currentAccountData.bestScore)
        {
            currentAccountData.bestScore = newScore;
            SaveLocalBackup();
        }
    }

    public async UniTask<bool> LoadFromCloud()
    {
        IsLoaded = false;
                        
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            Debug.LogWarning(
                "[Account] Google 인증이 없어 Cloud를 읽을 수 없습니다.");

            return false;
        }


        CloudReadResult result =
            await InternalReadFromCloud();


        switch (result.Status)
        {
            case CloudReadStatus.Success:
            {
                ApplyLoadedData(result.Data);

                IsLoaded = true;

                Debug.Log(
                    "[Account] 기존 Google Cloud 데이터 로드 완료");

                return true;
            }

            case CloudReadStatus.NoData:
            {
                if (currentAccountData == null)
                {
                    Debug.LogError(
                        "[Account] 신규 Google 계정 데이터가 준비되지 않았습니다.");

                    return false;
                }


                SaveLocalBackup();

                IsLoaded = true;

                Debug.Log(
                    "[Account] 신규 Google 계정으로 시작합니다.");

                return true;
            }

            case CloudReadStatus.Failed:
            default:
            {             

                Debug.LogWarning(
                    "[Account] Cloud 데이터 로드 실패");

                return false;
            }
        }
    }

    private async UniTask<CloudReadResult> InternalReadFromCloud()
    {
        var tcs = new UniTaskCompletionSource<CloudReadResult>();

        if (PlayGamesPlatform.Instance?.SavedGame == null)
        {
            Debug.LogWarning("[Account] Saved Game API가 준비되지 않았습니다.");

            return new CloudReadResult(
                CloudReadStatus.Failed);
        }

        PlayGamesPlatform.Instance.SavedGame
            .OpenWithAutomaticConflictResolution(
                saveFileName,
                DataSource.ReadNetworkOnly,
                ConflictResolutionStrategy.UseLongestPlaytime,
                (status, game) =>
                {
                    if (status != SavedGameRequestStatus.Success)
                    {
                        Debug.LogWarning($"[Account] Cloud 파일 열기 실패: {status}");
                        tcs.TrySetResult(new CloudReadResult(CloudReadStatus.Failed));
                        return;
                    }

                    PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(game,(readStatus, data) =>
                    {
                        if (readStatus != SavedGameRequestStatus.Success)
                        {
                            Debug.LogWarning($"[Account] Cloud 데이터 읽기 실패: {readStatus}");
                            tcs.TrySetResult(new CloudReadResult(CloudReadStatus.Failed));
                            return;
                        }

                        if (data == null || data.Length == 0)
                        {
                            Debug.Log("[Account] Cloud 저장 데이터가 없습니다.");
                            tcs.TrySetResult(new CloudReadResult(CloudReadStatus.NoData));
                            return;
                        }

                        try
                        {
                            string json = Encoding.UTF8.GetString(data);
                            AccountData accountData = JsonUtility.FromJson<AccountData>(json);

                            if (accountData == null)
                            {
                                Debug.LogWarning("[Account] Cloud JSON 변환 실패");
                                tcs.TrySetResult(new CloudReadResult(CloudReadStatus.Failed));
                                return;
                            }

                            tcs.TrySetResult(new CloudReadResult(CloudReadStatus.Success,accountData));
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"[Account] Cloud 데이터 파싱 실패: {e.Message}");
                            tcs.TrySetResult(new CloudReadResult(CloudReadStatus.Failed));
                        }
                    });
                });

        return await tcs.Task;
    }

    private void LoadLocalOrNew()
    {
        AccountData guestData = LoadGuestBackup();

        if (guestData == null)
        {
            guestData = new AccountData("Guest_" + System.Guid.NewGuid().ToString(),"Guest Player");
            Debug.Log("[Account] 신규 Guest 데이터를 생성했습니다.");
        }
        else
        {
            Debug.Log("[Account] 기존 Guest 데이터를 불러왔습니다.");
        }

        guestData.selectedCharacterId = "Char_0";

        if (guestData.unlockedCharacterIds == null)
            guestData.unlockedCharacterIds = new System.Collections.Generic.List<string>();

        guestData.unlockedCharacterIds.Clear();
        guestData.unlockedCharacterIds.Add("Char_0");

        guestData.coin = 0;

        currentAccountData = guestData;

        SaveLocalBackup();

        IsLoaded = true;
    }

    private void ApplyLoadedData(AccountData cloudData)
    {
        if (cloudData == null)
        {
            Debug.LogWarning("[Account] 적용할 Cloud 데이터가 없습니다.");
            return;
        }

        if (currentAccountData == null)
        {
            Debug.LogError("[Account] Google 계정 기본 데이터가 준비되지 않았습니다.");
            return;
        }

        string authenticatedId = currentAccountData.acountID;
        string authenticatedName = currentAccountData.nickname;

        currentAccountData.bestScore = cloudData.bestScore;
        currentAccountData.coin = cloudData.coin;

        currentAccountData.selectedCharacterId =
            string.IsNullOrEmpty(cloudData.selectedCharacterId)
                ? "Char_0"
                : cloudData.selectedCharacterId;

        if (cloudData.unlockedCharacterIds != null)
        {
            currentAccountData.unlockedCharacterIds = new System.Collections.Generic.List<string>(cloudData.unlockedCharacterIds);
        }
        else
        {
            currentAccountData.unlockedCharacterIds = new System.Collections.Generic.List<string>();
        }

        if (!currentAccountData.unlockedCharacterIds.Contains("Char_0"))
            currentAccountData.unlockedCharacterIds.Add("Char_0");

        currentAccountData.lastLoginData = cloudData.lastLoginData;

        currentAccountData.acountID = authenticatedId;
        currentAccountData.nickname = authenticatedName;

        SaveLocalBackup();

        Debug.Log("[Account] Cloud 데이터를 기준으로 Google 계정 동기화 완료");
    }

    public async UniTask<bool> SaveToCloud()
    {
        if (currentAccountData == null)
            return false;

        // 다른 저장이 진행 중이면 끝날 때까지 기다린다.
        while (isSaving)
            await UniTask.Yield();

        if (GPGSManager.Instance == null ||
            !GPGSManager.Instance.IsGoogleUser ||
            !GPGSManager.Instance.IsAuthenticatedNow ||
            !GPGSManager.Instance.IsNetworkConnected())
        {
            Debug.LogWarning("[Account] Cloud 저장 조건을 만족하지 않습니다.");
            return false;
        }

        isSaving = true;

        try
        {
            string json = JsonUtility.ToJson(currentAccountData);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            var tcs = new UniTaskCompletionSource<bool>();

            PlayGamesPlatform.Instance.SavedGame
                .OpenWithAutomaticConflictResolution(
                    saveFileName,
                    DataSource.ReadCacheOrNetwork,
                    ConflictResolutionStrategy.UseLongestPlaytime,
                    (status, game) =>
                    {
                        if (status != SavedGameRequestStatus.Success)
                        {
                            tcs.TrySetResult(false);
                            return;
                        }

                        var update = 
                            new SavedGameMetadataUpdate.Builder()
                                .WithUpdatedDescription(
                                    "Saved at " + System.DateTime.Now)
                                .Build();

                        PlayGamesPlatform.Instance.SavedGame
                            .CommitUpdate(
                                game,
                                update,
                                bytes,
                                (saveStatus, _) =>
                                {
                                    tcs.TrySetResult(saveStatus == SavedGameRequestStatus.Success);
                                });
                    });

            bool success = await tcs.Task;

            if (!success)
            {
                Debug.LogWarning("[Account] Cloud 저장 실패");
                return false;
            }

            SaveLocalBackup();
            Debug.Log("[Account] Cloud 저장 성공");

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Account] Cloud 저장 중 예외: {e.Message}");
            return false;
        }
        finally
        {
            isSaving = false;
        }
    }

    public void SaveLocalBackup()
    {
        if (currentAccountData == null)
            return;

        try
        {
            string json = JsonUtility.ToJson(currentAccountData);
            string accountId = currentAccountData.acountID;

            bool isGuestData =
                string.IsNullOrEmpty(accountId) ||
                accountId == "LocalUser" ||
                accountId.StartsWith("Guest_");

            string path = isGuestData
                ? GuestLocalPath
                : LocalPath;

            File.WriteAllText(path, json);

            Debug.Log($"[Account] 로컬 백업 완료: " + $"{(isGuestData ? "Guest" : "Google")}");
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
        if (GPGSManager.Instance == null ||
            !GPGSManager.Instance.IsGoogleUser ||
            !GPGSManager.Instance.IsAuthenticatedNow)
        {
            return;
        }


        PlayGamesPlatform.Instance.ReportScore(
            score,
            GPGSIds.leaderboard_bestscore,
            success =>
            {
                if (success)
                {
                    Debug.Log($"[Account] 리더보드 점수 등록 성공: {score}");
                }
                else
                {
                    Debug.LogWarning("[Account] 리더보드 점수 등록 실패");
                }
            });
    }

    public AccountData LoadLocalBackup()
    {
        if (!File.Exists(LocalPath))
            return null;

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

    public AccountData LoadGuestBackup()
    {
        if (!File.Exists(GuestLocalPath))
            return null;

        try
        {
            string json = File.ReadAllText(GuestLocalPath);

            return JsonUtility.FromJson<AccountData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Account] Guest 로컬 백업 읽기 실패: {e.Message}");

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
        SaveLocalBackup();
    }  

}
