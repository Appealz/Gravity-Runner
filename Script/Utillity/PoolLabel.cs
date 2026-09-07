using UnityEngine;

public class PoolLabel : MonoBehaviour
{
    ObjectPool myPool;

    public void SetPool(ObjectPool newPool)
    {
        myPool = newPool;
        gameObject.SetActive(false);
    }

    public void ReturnPool()
    {        
        myPool.PushObject(this);
    }
}
