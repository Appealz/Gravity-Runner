using System.Collections.Generic;
using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    private string coinKey = "Coin";
    [SerializeField]
    private List<Transform> spawnPoint = new List<Transform>();
    private List<GameObject> activeCoins = new List<GameObject>();

    private void Awake()
    {
        foreach (Transform child in transform)
        {
            if(child.name.StartsWith("spawnPoint"))
                spawnPoint.Add(child);
        }
    }


    private void OnEnable()
    {
        ClearCoins();
    }

    private void OnDisable()
    {
        ClearCoins();
    }

    public void SpawnCoin()
    {
        if (spawnPoint.Count == 0)
            return;

        var point = spawnPoint[Random.Range(0, spawnPoint.Count)];

        var coinObj = PoolManager.Instance.poolDic[coinKey].PopObject();
        if (coinObj == null)
            return;

        coinObj.transform.position = point.position;

        coinObj.transform.SetParent(transform, worldPositionStays: true);

        activeCoins.Add(coinObj);
        Debug.Log("코인 스폰 완료");
    }

    private void ClearCoins()
    {
        for (int i = 0; i < activeCoins.Count; i++)
        {
            if (activeCoins[i] != null)
            {
                if (activeCoins[i].TryGetComponent(out PoolLabel poolObj))
                    poolObj.ReturnPool();
            }
        }
        activeCoins.Clear();
    }
}
