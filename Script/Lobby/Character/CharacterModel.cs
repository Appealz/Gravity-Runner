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
        var allDataList = await AddressableLoader.LoadAllCharacterData();

        allDataList.Sort((a, b) => string.Compare(a.id, b.id, StringComparison.Ordinal));

        // [추가] 내 계정 정보(해금 리스트 등)를 쓰기 전에 데이터 로드 완료를 기다립니다.
        await AccountManager.Instance.WaitUntilLoaded();

        var account = AccountManager.Instance.currentAccountData;

        foreach (var data in allDataList)
        {
            bool isUnlocked = account.unlockedCharacterIds.Contains(data.id);
            characters.Add(new CharacterRuntimeData(data, isUnlocked));
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
        // 방어 코드: 계정 로드 여부 & 로그인 여부 체크
        if (AccountManager.Instance == null ||
            AccountManager.Instance.currentAccountData == null)
        {
            Debug.LogWarning("[CharacterModel] AccountManager 또는 AccountData가 없습니다.");
            return;
        }

        if (AccountManager.Instance.IsGuestAccount)
        {
            Debug.LogWarning("[CharacterModel] 게스트 모드에서는 캐릭터 해금이 불가능합니다.");
            return;
        }

        // 실제 해금 로직
        var character = GetCharacter(id);
        if (character != null && !character.IsUnlocked)
        {
            character.Unlock();

            if (!AccountManager.Instance.currentAccountData.unlockedCharacterIds.Contains(id))
            {
                AccountManager.Instance.currentAccountData.unlockedCharacterIds.Add(id);
            }

            Debug.Log($"[CharacterModel] 캐릭터 해금 완료: {id}");
        }
        else
        {
            Debug.LogWarning($"[CharacterModel] 캐릭터 {id}는 이미 해금되어 있거나 존재하지 않습니다.");
        }
    }
}
