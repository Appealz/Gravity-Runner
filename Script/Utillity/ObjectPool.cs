using System.Collections.Generic;
using UnityEngine;

public class ObjectPool 
{
    private GameObject prefab;
    private Queue<PoolLabel> poolLabels = new Queue<PoolLabel>();
    private Transform parentTrans;
    public ObjectPool(GameObject newPrefab, int count = 1, Transform parentTrans = null)
    {
        prefab = newPrefab;
        Allocate(count);
        this.parentTrans = parentTrans;
    }

    public void Allocate(int count)
    {
        for(int i = 0; i < count; i++)
        {
            GameObject obj = GameObject.Instantiate(prefab);
            if(obj.TryGetComponent<PoolLabel>(out PoolLabel label))
            {
                poolLabels.Enqueue(label);
                label.SetPool(this);
            }
        }
    }

    public GameObject PopObject()
    {
        if(poolLabels.Count <= 0)
        {
            Allocate(2);
        }

        PoolLabel label = poolLabels.Dequeue();
        label.gameObject.SetActive(true);

        return label.gameObject;
    }

    public void PushObject(PoolLabel returnLabel)
    {
        PoolLabel label = returnLabel;        
        poolLabels.Enqueue(label);
        label.gameObject.SetActive(false);        
    }

    public void PushObject(GameObject obj)
    {        
        if (obj.TryGetComponent<PoolLabel>(out PoolLabel label))
        {
            PushObject(label);
        }
        else
        {            
            GameObject.Destroy(obj);
        }
    }
}
