using UnityEngine;

public class DontDestroySingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            lock (_lock)
            {
                if (instance == null)
                {
                    instance = (T)FindAnyObjectByType(typeof(T));

                    if (instance == null)
                    {
                        GameObject singletonObj = new GameObject(typeof(T).Name + " (DontDestroySingleton)");
                        instance = singletonObj.AddComponent<T>();
                        DontDestroyOnLoad(singletonObj);
                    }
                }
                return instance;
            }
        }
    }

    protected virtual void DoAwake() { }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        DoAwake();
    }
}

public class DestroySingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            lock (_lock)
            {
                if (instance == null)
                {
                    instance = (T)FindAnyObjectByType(typeof(T));

                    //if (instance == null)
                    //{
                    //    GameObject singletonObj = new GameObject(typeof(T).Name + " (DestroySingleton)");
                    //    instance = singletonObj.AddComponent<T>();
                    //}
                }
                return instance;
            }
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
        }
        else if (instance != this)
        {
            Destroy(gameObject); // 같은 타입이 여러 개 생기면 자기 자신 파괴
        }

        DoAwake();
    }

    protected virtual void DoAwake() { }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}