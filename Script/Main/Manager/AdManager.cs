using GoogleMobileAds.Api;
using System;
using UnityEngine;

public class AdManager : DontDestroySingleton<AdManager>
{
    private RewardedAd rewardedAd;
    private const string AD_NEXT_TIME_KEY = "NextAdAvailableTime";
    private const int COOLDOWN_MINUTES = 3;

    protected override void DoAwake()
    {
        base.DoAwake();

#if !UNITY_EDITOR
        MobileAds.Initialize(initStatus => {
            Debug.Log("Google Mobile Ads Initialized");
        });
#endif
        LoadRewardAd();
    }

    private void LoadRewardAd()
    {
#if UNITY_EDITOR
        Debug.Log("Editor: Pretend to load rewarded ad");
#else
        string adUnitId = "YOUR_REWARDED_AD_UNIT_ID";
        AdRequest request = new AdRequest();

        RewardedAd.Load(adUnitId, request, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("Failed to load rewarded ad: " + error);
                return;
            }
            rewardedAd = ad;
            rewardedAd.OnAdFullScreenContentClosed += () => { LoadRewardAd(); };
        });
#endif
    }

    public bool IsAdReady()
    {
        string savedTime = PlayerPrefs.GetString(AD_NEXT_TIME_KEY, string.Empty);
        if (string.IsNullOrEmpty(savedTime)) return true;

        long binaryTime = Convert.ToInt64(savedTime);
        DateTime nextAvailableTime = DateTime.FromBinary(binaryTime);
        return DateTime.Now >= nextAvailableTime;
    }

    private void SetCooldown()
    {
        DateTime nextTime = DateTime.Now.AddMinutes(COOLDOWN_MINUTES);
        PlayerPrefs.SetString(AD_NEXT_TIME_KEY, nextTime.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    public void ShowRewardAd(Action onSuccess)
    {        
        if (RemoteConfigManager.Instance != null && !RemoteConfigManager.Instance.IsAdEnabled)
        {
            Debug.Log("원격 설정: 광고 비활성화 상태. 무료 부활을 제공합니다.");
            onSuccess?.Invoke(); // 부활 성공 처리
            return;
        }
                
        if (!NetworkChecker.CheckInternet()) return;
                
        if (!IsAdReady())
        {
            double remainingSeconds = GetRemainingCooldownSeconds();
            int minutes = (int)remainingSeconds / 60;
            int seconds = (int)remainingSeconds % 60;
                        
            PlatformUtil.ShowToast($"부활 에너지가 충전 중입니다. ({minutes:D2}:{seconds:D2} 남음)");
            return;
        }

#if UNITY_EDITOR
        Debug.Log("Editor: 광고 시청 성공 시뮬레이션");
        SetCooldown();
        onSuccess?.Invoke();
#else        
        if (rewardedAd != null)
        {
            rewardedAd.Show((Reward reward) =>
            {
                SetCooldown(); 
                onSuccess?.Invoke(); 
            });
        }
        else
        {            
            PlatformUtil.ShowToast("광고를 준비 중입니다. 잠시 후 다시 시도해주세요.");
            LoadRewardAd();
        }
#endif
    }

    public double GetRemainingCooldownSeconds()
    {
        string savedTime = PlayerPrefs.GetString(AD_NEXT_TIME_KEY, string.Empty);
        if (string.IsNullOrEmpty(savedTime)) return 0;

        DateTime nextTime = DateTime.FromBinary(Convert.ToInt64(savedTime));
        double seconds = (nextTime - DateTime.Now).TotalSeconds;
        return seconds > 0 ? seconds : 0;
    }
}

