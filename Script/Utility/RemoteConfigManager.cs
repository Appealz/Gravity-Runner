using Firebase.Extensions;
using Firebase.RemoteConfig;
using System;
using UnityEngine;

public class RemoteConfigManager : DontDestroySingleton<RemoteConfigManager>
{
    public bool IsAdEnabled { get; private set; } = true;
    public string LatestVersion { get; private set; } = "1.0.0";

    protected override void DoAwake()
    {
        base.DoAwake();
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                FetchRemoteConfig();
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    private void FetchRemoteConfig()
    {
        Debug.Log("서버에서 원격 설정값을 가져오는 중...");

        ConfigSettings settings = new ConfigSettings { MinimumFetchIntervalInMilliseconds = 0 };
        FirebaseRemoteConfig.DefaultInstance.SetConfigSettingsAsync(settings);

        FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero).ContinueWithOnMainThread(fetchTask => {
            if (fetchTask.IsCompleted)
            {
                // [수정 핵심] ActivateAsync가 완료된 후 중첩해서 값을 읽습니다.
                FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread(activateTask => {

                    // 광고 활성화 여부 읽기
                    IsAdEnabled = FirebaseRemoteConfig.DefaultInstance.GetValue("is_ad_active").BooleanValue;

                    // 최신 버전 정보 읽기
                    LatestVersion = FirebaseRemoteConfig.DefaultInstance.GetValue("latest_version").StringValue;

                    Debug.Log($"[RemoteConfig] 광고:{IsAdEnabled}, 서버버전:{LatestVersion}");
                });
            }
            else
            {
                Debug.LogWarning("원격 설정값을 가져오지 못했습니다.");
            }
        });
    }

    // 버전 체크 함수 추가
    public bool IsUpdateRequired()
    {
        if (string.IsNullOrEmpty(LatestVersion)) return false;

        try
        {
            // 1. 현재 설치된 앱 버전 (유니티 Project Settings에 적힌 값)
            System.Version currentVersion = new System.Version(Application.version);

            // 2. 파이어베이스 콘솔에서 가져온 최신 버전
            System.Version latestVersion = new System.Version(LatestVersion);

            // 3. 서버 버전이 내 버전보다 더 높을 때만 true 반환
            return latestVersion > currentVersion;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"버전 형식이 잘못되었습니다: {e.Message}");
            return false;
        }
    }

}
