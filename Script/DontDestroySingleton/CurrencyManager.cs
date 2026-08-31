using GooglePlayGames;
using System;
using UnityEngine;

public class CurrencyManager : DontDestroySingleton<CurrencyManager>
{
    private int coin;
    public int GetCoin() => coin;
    public event Action<int> OnCurrencyChanged;

    protected override async void DoAwake()
    {
        base.DoAwake();

        //  계정 데이터 로드 완료될 때까지 대기
        await AccountManager.Instance.WaitUntilLoaded();

        // 게스트 모드 
        if (AccountManager.Instance.IsGuestAccount)
        {
            coin = 1;            
            return;
        }

        // 게스트모드 아닌경우 재화 로드
        coin = AccountManager.Instance.currentAccountData.coin;        
    }

    private void OnEnable()
    {
        EventBus.Subscribe<OnChangeCoin>(OnChangeCoinEvent);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<OnChangeCoin>(OnChangeCoinEvent);
    }

    public void ChangeCoin(int amount)
    {
        if (AccountManager.Instance.IsGuestAccount) return;

        coin += amount;
        Debug.Log($"[CurrencyManager] 코인 변경: {amount:+#;-#;0} → 현재 {coin}");

        OnCurrencyChanged?.Invoke(coin);
    }
    public void SpendCoin(int amount)
    {
        if (AccountManager.Instance.IsGuestAccount) return;
        coin -= amount;
        
    }

    public async void ResultCoin()
    {        
        if (AccountManager.Instance.IsGuestAccount)
        {
            Debug.Log("[CurrencyManager] 게스트 모드, 코인 저장 생략");
            return;
        }
        
        Debug.Log($"코인 저장 로직 시작 coin : {coin}");

        bool isAuthenticated = false;
        try
        {
            isAuthenticated = PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.IsAuthenticated();
        }
        catch
        {
            isAuthenticated = false;
        }

        if (isAuthenticated)
        {
            AccountManager.Instance.currentAccountData.coin = coin;
            await AccountManager.Instance.SaveToCloud();            
        }
    }

    private void OnChangeCoinEvent(OnChangeCoin e)
    {
        ChangeCoin(e.coin);
    }

    public void OnChangeAddCoin(OnChangeCoin e)
    {        
        coin += e.coin;
        Debug.Log($"코인추가, 현재 코인량 : {coin}");
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
