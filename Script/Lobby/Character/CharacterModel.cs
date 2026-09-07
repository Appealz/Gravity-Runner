using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterModel
{
    private List<CharacterRuntimeData> characters = new List<CharacterRuntimeData>();
    public IReadOnlyList<CharacterRuntimeData> Characters => characters;


    public async UniTask Initialize()
    {
        var allDataList =
            await AddressableLoader.LoadAllCharacterData();

        allDataList.Sort(
            (a, b) => string.Compare(
                a.id,
                b.id,
                StringComparison.Ordinal));

        await AccountManager.Instance.WaitUntilLoaded();

        var account =
            AccountManager.Instance.currentAccountData;

        if (account.unlockedCharacterIds == null)
        {
            account.unlockedCharacterIds =
                new List<string>();
        }

        if (!account.unlockedCharacterIds.Contains("Char_0"))
        {
            account.unlockedCharacterIds.Add("Char_0");
        }

        characters.Clear();

        foreach (var data in allDataList)
        {
            bool isUnlocked =
                account.unlockedCharacterIds.Contains(data.id);

            characters.Add(
                new CharacterRuntimeData(
                    data,
                    isUnlocked));
        }
    }

    /// <summary>
    /// ID로 캐릭터 찾기
    /// </summary>
    public CharacterRuntimeData GetCharacter(string id)
    {
        return characters.Find(c => c.BaseData.id == id);        
    }

    /// <summary>
    /// 캐릭터 해금 (코인 차감 등은 Presenter 쪽에서 처리)
    /// </summary>
    public void UnlockCharacter(string id)
    {
        if (AccountManager.Instance == null ||
            AccountManager.Instance.currentAccountData == null)
        {
            Debug.LogWarning(
                "[CharacterModel] AccountManager 또는 AccountData가 없습니다.");

            return;
        }


        // 저장된 Account ID가 아니라
        // 현재 실제 Google 로그인 세션을 기준으로 판단
        if (GPGSManager.Instance == null ||
            !GPGSManager.Instance.IsGoogleUser ||
            !GPGSManager.Instance.IsAuthenticatedNow)
        {
            Debug.LogWarning(
                "[CharacterModel] Google 로그인 상태가 아니므로 캐릭터를 해금할 수 없습니다.");

            return;
        }


        var character = GetCharacter(id);

        if (character == null)
        {
            Debug.LogWarning(
                $"[CharacterModel] 캐릭터가 존재하지 않습니다: {id}");

            return;
        }


        if (character.IsUnlocked)
        {
            Debug.LogWarning(
                $"[CharacterModel] 이미 해금된 캐릭터입니다: {id}");

            return;
        }


        character.Unlock();


        var unlockedList =
            AccountManager.Instance.currentAccountData.unlockedCharacterIds;


        if (!unlockedList.Contains(id))
        {
            unlockedList.Add(id);
        }


        Debug.Log(
            $"[CharacterModel] 캐릭터 해금 완료: {id}");
    }

    public void RefreshFromCurrentAccount()
    {
        if (AccountManager.Instance == null ||
            AccountManager.Instance.currentAccountData == null)
        {
            Debug.LogWarning(
                "[CharacterModel] 캐릭터 상태를 갱신할 AccountData가 없습니다.");

            return;
        }

        var account =
            AccountManager.Instance.currentAccountData;

        bool isGoogleUser =
            GPGSManager.Instance.IsGoogleUser;


        // ============================================
        // 캐릭터 해금 상태 재동기화
        // ============================================
        foreach (var character in characters)
        {
            bool isUnlocked;


            // Guest는 Char_0만 사용 가능
            if (!isGoogleUser)
            {
                isUnlocked =
                    character.BaseData.id == "Char_0";
            }
            else
            {
                bool isSavedUnlocked =
                    account.unlockedCharacterIds != null &&
                    account.unlockedCharacterIds.Contains(
                        character.BaseData.id);

                isUnlocked =
                    character.BaseData.defaultUnlocked ||
                    isSavedUnlocked;
            }


            character.SetUnlocked(isUnlocked);
        }


        // ============================================
        // 현재 선택 캐릭터 검증
        // ============================================
        CharacterRuntimeData selected =
            GetCharacter(account.selectedCharacterId);

        if (selected == null ||
            !selected.IsUnlocked)
        {
            CharacterRuntimeData defaultCharacter =
                GetCharacter("Char_0");

            if (defaultCharacter != null)
            {
                account.selectedCharacterId =
                    defaultCharacter.BaseData.id;
            }
        }


        Debug.Log(
            $"[CharacterModel] 계정 기준 캐릭터 상태 갱신 완료 / " +
            $"Google: {isGoogleUser}, " +
            $"Selected: {account.selectedCharacterId}");
    }
}
