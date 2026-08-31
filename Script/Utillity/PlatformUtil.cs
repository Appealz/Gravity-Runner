using UnityEngine;

public static class PlatformUtil
{
    public static void ShowToast(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                if (activity == null)
                {
                    Debug.LogWarning("[Toast] activity is null, 메시지 표시 실패");
                    return;
                }

                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    using (var toastClass = new AndroidJavaClass("android.widget.Toast"))
                    {
                        var toast = toastClass.CallStatic<AndroidJavaObject>(
                            "makeText", activity, message, 0);
                        toast.Call("show");
                    }
                }));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Toast] 실패: {e.Message}");
        }
#else
        Debug.Log($"[Toast] {message}");
#endif
    }
}

public static class NetworkChecker
{
    public static bool CheckInternet()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            PlatformUtil.ShowToast("인터넷 연결이 되어있지 않습니다.");
            return false;
        }
        return true;
    }
}
