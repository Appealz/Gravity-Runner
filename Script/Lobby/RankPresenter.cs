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

        bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;
        bool isGoogleUser = !AccountManager.Instance.IsGuestAccount;

        if (hasInternet)
        {
            await model.LoadGPGSRankAsync();

            RankData myDisplayData;
            if (isGoogleUser)
            {
                myDisplayData = model.GetMyRank() ?? new RankData(-1, myName, myScore, "me");
            }
            else
            {
                // [추가] 게스트 계정일 때 안내 메시지 출력
                GPGSManager.Instance.ShowToast("게스트 계정은 실시간 랭킹에 반영되지 않습니다.");
                myDisplayData = new RankData(-1, myName, myScore, "guest");
            }

            view.SetRows(model.GetRankList(), myDisplayData);
        }
        else
        {
            model.LoadOfflineMyData(myName, myScore);
            view.SetRows(model.GetRankList(), model.GetMyRank());
            GPGSManager.Instance.ShowToast("네트워크 연결이 없어 로컬 기록만 표시합니다.");
        }
    }

    private void Hide()
    {
        SoundManager.Instance.PlaySFX("TouchClose");
        view.Hide();
    }
}