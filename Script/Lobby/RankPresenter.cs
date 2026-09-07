using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;

public class RankPresenter
{
    private RankModel model;
    private RankView view;
    public bool HasLoaded { get; private set; } = false;

    public RankPresenter(RankModel model, RankView view)
    {
        this.model = model;
        this.view = view;
        view.CloseBtn.onClick.AddListener(Hide);
    }

    public async Task ShowRank(string myName, long myScore)
    {
        view.Clear();
        view.gameObject.SetActive(true);

        bool isGoogleUser = GPGSManager.Instance.IsGoogleUser && GPGSManager.Instance.IsAuthenticatedNow;

        // Guest 또는 Google 인증이 없는 상태
        if (!isGoogleUser)
        {
            model.LoadOfflineMyData(myName, myScore);
            view.SetRows(model.GetRankList(), model.GetMyRank());

            GPGSManager.Instance.ShowToast("Sign in to view the leaderboard.");

            return;
        }

        // Google 로그인 상태지만 네트워크 없음
        if (!GPGSManager.Instance.IsNetworkConnected())
        {
            model.LoadOfflineMyData(myName, myScore);
            view.SetRows(model.GetRankList(), model.GetMyRank());

            GPGSManager.Instance.ShowToast("Network unavailable. Showing local record.");

            return;
        }

        // 정상 Google 로그인
        await model.LoadGPGSRankAsync();

        RankData myDisplayData = model.GetMyRank() ?? new RankData(-1, myName, myScore, "me");

        view.SetRows(model.GetRankList(), myDisplayData);
    }

    private void Hide()
    {
        SoundManager.Instance.PlaySFX("TouchClose");
        view.Hide();
    }

    public void Dispose()
    {
        view.CloseBtn.onClick.RemoveListener(Hide);
    }
}