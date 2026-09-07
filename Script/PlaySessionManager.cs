using System;
using UnityEngine;

public class PlaySessionManager : DontDestroySingleton<PlaySessionManager>
{
    // 현재 실제 게임 한 판이 진행 중인지
    public bool IsRunActive { get; private set; }

    // 현재 판의 점수/골드를 정상 기록으로 인정할 수 있는지
    public bool IsValidRun { get; private set; }

    // 플레이 도중 정상 판의 자격을 잃었을 때
    // 나중에 UI에서 안내 메시지를 띄우기 위한 이벤트
    public event Action OnRunInvalidated;


    private const float networkCheckInterval = 0.5f;
    private float networkCheckTimer;

    private const float disconnectGraceTime = 3f;
    private float disconnectedTime;

    /// <summary>
    /// 실제 플레이 한 판이 시작될 때 호출
    /// </summary>
    public void BeginRun()
    {
        IsRunActive = true;
        networkCheckTimer = 0f;
        disconnectedTime = 0f;

        IsValidRun =
            GPGSManager.Instance.IsGoogleUser &&
            GPGSManager.Instance.IsAuthenticatedNow &&
            GPGSManager.Instance.IsNetworkConnected();

        if (IsValidRun)
            Debug.Log("[PlaySession] 정상 플레이 시작");
        else
            Debug.Log("[PlaySession] Unranked 플레이 시작");
    }


    private void Update()
    {
        if (!IsRunActive || !IsValidRun)
            return;

        networkCheckTimer += Time.unscaledDeltaTime;

        if (networkCheckTimer < networkCheckInterval)
            return;

        float elapsed = networkCheckTimer;
        networkCheckTimer = 0f;

        bool isConnected =
            GPGSManager.Instance.IsGoogleUser &&
            GPGSManager.Instance.IsAuthenticatedNow &&
            GPGSManager.Instance.IsNetworkConnected();

        if (isConnected)
        {
            // 3초가 되기 전에 복구되면 정상 처리
            disconnectedTime = 0f;
            return;
        }

        disconnectedTime += elapsed;

        if (disconnectedTime >= disconnectGraceTime)
        {
            InvalidateRun();
        }
    }


    /// <summary>
    /// 현재 판의 랭킹/재화 획득 자격을 영구적으로 제거
    /// 한번 false가 되면 해당 판에서는 다시 true가 되지 않는다.
    /// </summary>
    public void InvalidateRun()
    {
        if (!IsRunActive || !IsValidRun)
            return;

        IsValidRun = false;
        disconnectedTime = 0f;

        Debug.LogWarning("[PlaySession] 연결이 3초 이상 끊겨 현재 플레이가 Unranked 처리되었습니다.");

        GPGSManager.Instance.ShowToast("Network disconnected. Ranking and coins are disabled for this run.");

        OnRunInvalidated?.Invoke();
    }


    /// <summary>
    /// 게임오버 등 실제 한 판이 종료될 때 호출.
    /// 반환값은 이 판의 점수/골드를 인정해도 되는지 여부.
    /// </summary>
    public bool EndRun()
    {
        if (!IsRunActive)
            return false;


        bool result = IsValidRun;


        IsRunActive = false;
        IsValidRun = false;
        networkCheckTimer = 0f;


        Debug.Log(result ? "[PlaySession] 정상 플레이 종료" : "[PlaySession] Unranked 플레이 종료");


        return result;
    }

    public bool ValidateNow()
    {
        return IsRunActive && IsValidRun;
    }
}