using UnityEngine;

public class CharacterPresenter
{
    CharacterModel model;
    CharacterView view;    

    public CharacterPresenter(CharacterModel newModel, CharacterView newView)
    {
        model = newModel;
        view = newView;

        // 캐릭터 버튼 클릭
        view.OnCharacterSelected += OnCharacterClicked;

        // 설명창 내부 버튼들
        view.DescriptionView.OnBuyClicked += OnBuyCharacter;
        view.DescriptionView.OnCancel += () => view.DescriptionView.Hide();

        CurrencyManager.Instance.OnCurrencyChanged += view.UpdateCoin;
    }

    private void OnCharacterClicked(CharacterRuntimeData data)
    {
        Debug.Log($"[CharacterPresenter] 클릭된 캐릭터: {data?.BaseData?.displayName ?? "null"}");
        if (data == null)
        {
            Debug.LogWarning("[CharacterPresenter] null data passed!");
            return;
        }

        // 1. 잠금 상태면
        if (!data.IsUnlocked)
        {
            // 로그인, 인터넷 검사 (필요시)
            if (AccountManager.Instance.IsGuestAccount)
            {
                Debug.Log("게스트 모드에서는 캐릭터를 해금할 수 없습니다.");
                view.ShowErrorPopup("게스트 모드에서는 캐릭터를 해금할 수 없습니다.");
                return;
            }

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.Log("인터넷 연결이 필요합니다.");
                view.ShowErrorPopup("인터넷 연결이 필요합니다.");
                return;
            }

            // 설명창 표시
            view.ShowDescription(data);
            return;
        }

        //  2. 이미 해금된 상태면 선택 처리        
        AccountManager.Instance.currentAccountData.selectedCharacterId = data.BaseData.id;
        view.UpdateSelectedCharacter(data.BaseData.icon);
    }

    private void OnBuyCharacter(CharacterRuntimeData data)
    {
        if (AccountManager.Instance.IsGuestAccount)
        {
            Debug.Log("[CharacterPresenter] 게스트 모드에서는 캐릭터를 구매할 수 없습니다.");
            view.ShowErrorPopup("게스트 모드에서는 캐릭터를 구매할 수 없습니다!");
            return;
        }

        var coin = CurrencyManager.Instance.GetCoin();

        if (coin < data.BaseData.price)
        {
            
            view.ShowErrorPopup("코인이 부족합니다!");
            return;
        }

        // 코인 차감
        EventBus.Publish(new OnChangeCoin(-data.BaseData.price));

        // 캐릭터 해금
        model.UnlockCharacter(data.BaseData.id);

        // 뷰 갱신        
        view.DescriptionView.Hide();
        view.UpdateSelectedCharacter(data.BaseData.icon);

        view.RefreshButtons();

        OnCharacterClicked(data);
        Debug.Log($"[CharacterPresenter] 캐릭터 {data.BaseData.displayName} 구매 완료!");

    }

    public void Dispose()
    {
        CurrencyManager.Instance.OnCurrencyChanged -= view.UpdateCoin;
    }
}