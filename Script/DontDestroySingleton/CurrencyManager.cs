using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class CurrencyManager : DontDestroySingleton<CurrencyManager>
{
    // 이미 계정에 확정되어 있는 골드
    private int coin;

    // 현재 한 판에서 획득했지만 아직 계정에 확정되지 않은 골드
    private int runCoin;

    public int GetCoin() => coin;
    public int GetRunCoin() => runCoin;

    public event Action<int> OnCurrencyChanged;


    protected override async void DoAwake()
    {
        base.DoAwake();

        await AccountManager.Instance.WaitUntilLoaded();

        RefreshFromCurrentAccount();
    }


    private void OnEnable()
    {
        EventBus.Subscribe<OnChangeCoin>(OnChangeCoinEvent);

        GPGSManager.Instance.OnLoginProcessCompleted +=
            OnLoginProcessCompleted;
    }


    private void OnDisable()
    {
        EventBus.Unsubscribe<OnChangeCoin>(OnChangeCoinEvent);

        if (GPGSManager.Instance != null)
        {
            GPGSManager.Instance.OnLoginProcessCompleted -=
                OnLoginProcessCompleted;
        }
    }

    private void OnLoginProcessCompleted()
    {
        RefreshFromCurrentAccount();
    }

    public void RefreshFromCurrentAccount()
    {
        runCoin = 0;

        if (!GPGSManager.Instance.IsGoogleUser ||
            AccountManager.Instance.currentAccountData == null)
        {
            coin = 0;

            OnCurrencyChanged?.Invoke(coin);

            Debug.Log("[CurrencyManager] Guest 상태 -> 골드 0");

            return;
        }


        coin = AccountManager.Instance.currentAccountData.coin;

        OnCurrencyChanged?.Invoke(coin);

        Debug.Log(
            $"[CurrencyManager] 계정 골드 로드 완료: {coin}");
    }

    public void BeginRunCoin()
    {
        runCoin = 0;

        Debug.Log("[CurrencyManager] 이번 플레이 골드 초기화");
    }

    private void AddRunCoin(int amount)
    {
        if (amount <= 0)
            return;
         
        if (!GPGSManager.Instance.IsGoogleUser)
        {
            Debug.Log(
                "[CurrencyManager] Guest 모드 -> 골드 획득 무효");

            return;
        }
              
        if (!PlaySessionManager.Instance.ValidateNow())
        {
            Debug.LogWarning(
                "[CurrencyManager] Unranked 상태 -> 골드 획득 무효");

            return;
        }


        runCoin += amount;

        Debug.Log(
            $"[CurrencyManager] 플레이 골드 +{amount}, 임시 골드: {runCoin}");
    }

    public void ChangeCoin(int amount)
    {
        if (!GPGSManager.Instance.IsGoogleUser)
            return;


        coin += amount;

        if (coin < 0)
            coin = 0;


        if (AccountManager.Instance.currentAccountData != null)
        {
            AccountManager.Instance.currentAccountData.coin = coin;
        }


        OnCurrencyChanged?.Invoke(coin);

        Debug.Log(
            $"[CurrencyManager] 계정 골드 변경: {amount:+#;-#;0} -> {coin}");
    }


    public void SpendCoin(int amount)
    {
        if (amount <= 0)
            return;

        ChangeCoin(-amount);
    }

    public async UniTask FinalizeRunCoin(bool validRun)
    {
        if (!validRun ||
            !GPGSManager.Instance.IsGoogleUser ||
            !GPGSManager.Instance.IsAuthenticatedNow)
        {
            Debug.Log(
                $"[CurrencyManager] 플레이 골드 폐기: {runCoin}");

            runCoin = 0;

            return;
        }


        if (runCoin > 0)
        {
            coin += runCoin;

            Debug.Log(
                $"[CurrencyManager] 플레이 골드 확정 +{runCoin}, 총 골드: {coin}");

            runCoin = 0;


            AccountManager.Instance.currentAccountData.coin = coin;

            OnCurrencyChanged?.Invoke(coin);


            // 우선 로컬에도 확정 데이터 저장
            AccountManager.Instance.SaveLocalBackup();

            // Google Cloud 저장
            await AccountManager.Instance.SaveToCloud();
        }
    }

    private void OnChangeCoinEvent(OnChangeCoin e)
    {
        if (e == null)
            return;


        if (e.coin > 0 &&
            PlaySessionManager.Instance.IsRunActive)
        {
            AddRunCoin(e.coin);

            return;
        }


        ChangeCoin(e.coin);
    }


    // 기존 외부 코드 호환용
    public void OnChangeAddCoin(OnChangeCoin e)
    {
        if (e == null)
            return;

        AddRunCoin(e.coin);
    }
}


public class OnChangeCoin
{
    public int coin;

    public OnChangeCoin(int amount)
    {
        coin = amount;
    }
}