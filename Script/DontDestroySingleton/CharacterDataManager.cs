using System.Collections.Generic;
using UnityEngine;

public class CharacterDataManager : DontDestroySingleton<CharacterDataManager>
{
    private Dictionary<string, CharacterRuntimeData> runtimeCache = new();

    /// <summary>
    /// 이미 캐시가 존재하는지 여부
    /// </summary>
    public bool HasCache => runtimeCache.Count > 0;

    /// <summary>
    /// 모델에서 런타임 캐릭터 데이터 복사
    /// </summary>
    public void CacheFrom(CharacterModel model)
    {
        runtimeCache.Clear();
        foreach (var c in model.Characters)
            runtimeCache[c.BaseData.id] = c;

        Debug.Log($"[CharacterDataManager] 캐릭터 {runtimeCache.Count}개 캐싱 완료");
    }

    /// <summary>
    /// ID로 캐릭터 런타임 데이터 조회
    /// </summary>
    public CharacterRuntimeData Get(string id)
    {
        runtimeCache.TryGetValue(id, out var data);
        return data;
    }

    public IEnumerable<CharacterRuntimeData> GetAll()
    {
        return runtimeCache.Values;
    }
}
