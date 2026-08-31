using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class AddressableLoader
{
    // 공통 Generic 로더
    private static async UniTask<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object
    {
        //await Addressables.InitializeAsync().ToUniTask();

        //var handle = Addressables.LoadAssetAsync<T>(key);
        //T asset = await handle.Task;

        //if (asset == null)
        //    Debug.LogWarning($"[AddressableLoader] {key} ({typeof(T).Name}) is missing!");

        //return asset;

        try
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            T asset = await handle.ToUniTask();

            if (asset == null)
                Debug.LogError($"[AddressableLoader] {key} 로드 실패: 에셋이 null입니다.");

            return asset;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AddressableLoader] {key} 로드 중 치명적 에러: {e.Message}");
            return null;
        }
    }

    // 타입별 래퍼 (가독성용)
    public static UniTask<GameObject> LoadToPrefab(string prefabName)
        => LoadAssetAsync<GameObject>(prefabName);

    public static UniTask<CharacterData> LoadToCharacterData(string dataName)
        => LoadAssetAsync<CharacterData>(dataName);

    public static UniTask<Material> LoadToMaterial(string materialName)
        => LoadAssetAsync<Material>(materialName);

    public static UniTask<AudioClip> LoadToClip(string clipName)
        => LoadAssetAsync<AudioClip>(clipName);
    public static UniTask<GameConfigSO> LoadToConfig(string dataName)
    => LoadAssetAsync<GameConfigSO>(dataName);

    public static async UniTask<List<CharacterData>> LoadAllCharacterData(string label = "CharacterData")
    {
        var handle = Addressables.LoadAssetsAsync<CharacterData>(
            label,
            null  // 개별 콜백 필요 없을 때 null
        );

        var loaded = await handle.Task;

        if (loaded == null || loaded.Count == 0)
            Debug.LogWarning($"[AddressableLoader] '{label}' 라벨로 로드된 CharacterData가 없습니다.");

        return new List<CharacterData>(loaded);
    }
}