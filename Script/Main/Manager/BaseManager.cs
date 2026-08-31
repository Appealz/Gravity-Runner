using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class BaseManager : MonoBehaviour
{
    public bool IsInitialized { get; protected set; } = false;
    public virtual UniTask Initialize() 
    {
        IsInitialized = true;

        return UniTask.CompletedTask;
    }
    public virtual void CustomUpdate() { }
    public virtual void Shutdown() { }

    public virtual void PostInitialize() { }
}