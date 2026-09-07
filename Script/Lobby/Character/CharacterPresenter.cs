using UnityEngine;

public class CharacterPresenter
{
    CharacterModel model;
    CharacterView view;
    private bool isPurchasing;
    public CharacterPresenter(CharacterModel newModel, CharacterView newView)
    {
        model = newModel;
        view = newView;

        view.OnCharacterSelected += OnCharacterClicked;
        view.DescriptionView.OnBuyClicked += OnBuyCharacter;
        view.DescriptionView.OnCancel += OnCancelCharacter;

        CurrencyManager.Instance.OnCurrencyChanged += view.UpdateCoin;

        GPGSManager.Instance.OnLoginProcessCompleted +=
            OnLoginProcessCompleted;
    }

    private async void OnCharacterClicked(CharacterRuntimeData data)
    {
        if (data == null)
            return;

        if (!data.IsUnlocked)
        {
            if (!CanUseGoogleAccountFeature())
            {
                GPGSManager.Instance.ShowToast("Sign in to unlock characters.");
                return;
            }

            view.ShowDescription(data);
            return;
        }

        // Guest는 기본 캐릭터만 선택 가능
        if (!GPGSManager.Instance.IsGoogleUser &&
            data.BaseData.id != "Char_0")
        {
            GPGSManager.Instance.ShowToast(
                "Sign in to use this character.");
            return;
        }

        var account = AccountManager.Instance.currentAccountData;

        if (account == null)
            return;

        string previousId = account.selectedCharacterId;

        account.selectedCharacterId = data.BaseData.id;

        view.UpdateSelectedCharacter(data.BaseData.icon);

        // Guest는 Char_0만 로컬 저장
        if (!GPGSManager.Instance.IsGoogleUser)
        {
            AccountManager.Instance.SaveLocalBackup();
            return;
        }

        bool success = await AccountManager.Instance.SaveToCloud();

        if (!success)
        {
            account.selectedCharacterId = previousId;

            CharacterRuntimeData previous = model.GetCharacter(previousId);

            if (previous != null)
                view.UpdateSelectedCharacter(previous.BaseData.icon);

            GPGSManager.Instance.ShowToast("Failed to save character selection.");
        }
    }

    private void OnCancelCharacter()
    {
        view.DescriptionView.Hide();
    }

    private async void OnBuyCharacter(CharacterRuntimeData data)
    {
        if (isPurchasing || data == null)
            return;


        // 이미 해금된 캐릭터면 구매할 필요 없음
        if (data.IsUnlocked)
            return;


        // ============================================
        // Google 로그인 + 인증 + 인터넷 필수
        // ============================================
        if (!CanUseGoogleAccountFeature())
        {
            if (GPGSManager.Instance == null || !GPGSManager.Instance.IsGoogleUser || !GPGSManager.Instance.IsAuthenticatedNow)
            {
                view.ShowErrorPopup("게스트 모드에서는 캐릭터를 구매할 수 없습니다.");
            }
            else
            {
                view.ShowErrorPopup("인터넷 연결이 필요합니다.");
            }

            return;
        }


        var account =
            AccountManager.Instance.currentAccountData;


        if (account == null)
            return;


        int price =
            data.BaseData.price;


        if (CurrencyManager.Instance.GetCoin() < price)
        {
            view.ShowErrorPopup(
                "코인이 부족합니다!");

            return;
        }


        isPurchasing = true;


        // ============================================
        // 실패했을 때 원상복구하기 위한 이전 상태
        // ============================================
        string previousSelectedId =
            account.selectedCharacterId;

        bool wasInUnlockedList =
            account.unlockedCharacterIds.Contains(
                data.BaseData.id);


        try
        {
            // ============================================
            // 1. 코인 차감
            // ============================================
            CurrencyManager.Instance.SpendCoin(price);


            // ============================================
            // 2. 캐릭터 해금
            // ============================================
            model.UnlockCharacter(
                data.BaseData.id);


            // 혹시 해금 자체가 실패했다면 바로 복구
            if (!data.IsUnlocked)
            {
                CurrencyManager.Instance.ChangeCoin(price);

                view.ShowErrorPopup(
                    "캐릭터 구매에 실패했습니다.");

                return;
            }


            // 구매한 캐릭터를 바로 선택
            account.selectedCharacterId =
                data.BaseData.id;


            // ============================================
            // 3. Cloud에 최종 저장
            // ============================================
            bool saveSuccess =
                await AccountManager.Instance.SaveToCloud();


            if (!saveSuccess)
            {
                // ========================================
                // Cloud 저장 실패 -> 구매 전체 롤백
                // ========================================

                // 코인 복구
                CurrencyManager.Instance.ChangeCoin(price);


                // 런타임 캐릭터 다시 잠금
                data.SetUnlocked(false);


                // 구매 전에는 해금 리스트에 없었다면 제거
                if (!wasInUnlockedList)
                {
                    account.unlockedCharacterIds.Remove(
                        data.BaseData.id);
                }


                // 기존 선택 캐릭터 복구
                account.selectedCharacterId =
                    previousSelectedId;


                // 로컬 백업도 롤백 상태로 맞춤
                AccountManager.Instance.SaveLocalBackup();


                view.RefreshButtons();


                CharacterRuntimeData previousCharacter =
                    model.GetCharacter(previousSelectedId);


                if (previousCharacter != null)
                {
                    view.UpdateSelectedCharacter(
                        previousCharacter.BaseData.icon);
                }


                view.ShowErrorPopup(
                    "구매 정보를 저장하지 못했습니다. 다시 시도해주세요.");


                Debug.LogWarning(
                    "[CharacterPresenter] Cloud 저장 실패 -> 구매 롤백");


                return;
            }


            // ============================================
            // 4. 여기까지 와야 실제 구매 성공
            // ============================================
            view.DescriptionView.Hide();

            view.RefreshButtons();

            view.UpdateSelectedCharacter(
                data.BaseData.icon);


            Debug.Log(
                $"[CharacterPresenter] " +
                $"{data.BaseData.displayName} 구매 완료!");
        }
        finally
        {
            isPurchasing = false;
        }
    }

    private void OnLoginProcessCompleted()
    {
        model.RefreshFromCurrentAccount();

        view.RefreshButtons();


        var account =
            AccountManager.Instance.currentAccountData;

        if (account == null)
            return;


        CharacterRuntimeData selected =
            model.GetCharacter(
                account.selectedCharacterId);

        if (selected != null)
        {
            view.UpdateSelectedCharacter(
                selected.BaseData.icon);
        }
    }

    private bool CanUseGoogleAccountFeature()
    {
        return
            GPGSManager.Instance != null &&
            GPGSManager.Instance.IsGoogleUser &&
            GPGSManager.Instance.IsAuthenticatedNow &&
            GPGSManager.Instance.IsNetworkConnected();
    }
    public void Dispose()
    {
        view.OnCharacterSelected -= OnCharacterClicked;
        view.DescriptionView.OnBuyClicked -= OnBuyCharacter;
        view.DescriptionView.OnCancel -= OnCancelCharacter;

        CurrencyManager.Instance.OnCurrencyChanged -= view.UpdateCoin;

        if (GPGSManager.Instance != null)
            GPGSManager.Instance.OnLoginProcessCompleted -= OnLoginProcessCompleted;
    }
}