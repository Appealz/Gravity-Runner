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
                FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread(activateTask => {
                                        
                    IsAdEnabled = FirebaseRemoteConfig.DefaultInstance.GetValue("is_ad_active").BooleanValue;
                                        
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
        
    public bool IsUpdateRequired()
    {
        if (string.IsNullOrEmpty(LatestVersion)) return false;

        try
        {            
            System.Version currentVersion = new System.Version(Application.version);                        
            System.Version latestVersion = new System.Version(LatestVersion);            
            return latestVersion > currentVersion;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"버전 형식이 잘못되었습니다: {e.Message}");
            return false;
        }
    }

}
