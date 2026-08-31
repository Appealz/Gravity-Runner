using Cysharp.Threading.Tasks;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class RankModel
{
    // [통일] 변수명을 rankList로 하나만 사용합니다.
    private List<RankData> rankList = new List<RankData>();
    private RankData myRank;

    public List<RankData> GetRankList() => rankList;
    public RankData GetMyRank() => myRank;

    public async UniTask LoadGPGSRankAsync()
    {
        rankList.Clear();
        var myData = AccountManager.Instance.currentAccountData;
        myRank = new RankData(-1, myData.nickname, myData.bestScore, "me");

        var tcs = new UniTaskCompletionSource<bool>();

        PlayGamesPlatform.Instance.LoadScores(
            GPGSIds.leaderboard_bestscore,
            LeaderboardStart.TopScores,
            20,
            LeaderboardCollection.Public,
            LeaderboardTimeSpan.AllTime,
            async (data) => // 비동기 처리를 위해 async 추가
            {
                if (data.Status == ResponseStatus.Success)
                {
                    // 1. 점수판에서 고유 ID들을 싹 모읍니다.
                    string[] userIds = new string[data.Scores.Length];
                    for (int i = 0; i < data.Scores.Length; i++)
                        userIds[i] = data.Scores[i].userID;

                    // 2. [핵심] ID를 들고 가서 진짜 닉네임이 적힌 장부를 받아옵니다.
                    var nicknameMap = await LoadUserNames(userIds);

                    // 3. 이제 랭킹 리스트를 만듭니다.
                    for (int i = 0; i < data.Scores.Length; i++)
                    {
                        var score = data.Scores[i];
                        string actualNick;

                        if (score.userID == PlayGamesPlatform.Instance.GetUserId())
                        {
                            actualNick = myData.nickname; // 내 이름은 내 로컬 데이터 우선
                        }
                        else
                        {
                            // 4. [중요] 장부에서 이름을 찾습니다. 없으면 마지막 수단으로 ID를 가공합니다.
                            if (!nicknameMap.TryGetValue(score.userID, out actualNick))
                            {
                                actualNick = "Player_" + (score.userID.Length > 5 ? score.userID.Substring(0, 5) : score.userID);
                            }
                        }

                        rankList.Add(new RankData(i + 1, actualNick, score.value, score.userID));
                    }

                    if (data.PlayerScore != null)
                    {
                        myRank = new RankData((int)data.PlayerScore.rank, myData.nickname, data.PlayerScore.value, "me");
                        SyncScore(data.PlayerScore.value);
                    }
                    tcs.TrySetResult(true);
                }
                else { tcs.TrySetResult(false); }
            });

        await tcs.Task;
    }

    // 고유 ID 리스트를 던지면 [ID : 진짜 닉네임] 장부를 반환하는 비동기 함수
    private async UniTask<Dictionary<string, string>> LoadUserNames(string[] userIds)
    {
        var nameMap = new Dictionary<string, string>();

        // 1. 구글 전용이 아닌, 유니티 표준 IUserProfile을 사용합니다.
        var profileTcs = new UniTaskCompletionSource<IUserProfile[]>();

        // 2. PlayGamesPlatform이 유니티 인터페이스 형태로 데이터를 넘겨줍니다.
        PlayGamesPlatform.Instance.LoadUsers(userIds, (users) =>
        {
            profileTcs.TrySetResult(users);
        });

        var profiles = await profileTcs.Task;
        if (profiles != null)
        {
            foreach (var p in profiles)
            {
                // 3. p.id와 p.userName을 사용합니다. (노란 줄은 무시해도 빌드는 됩니다!)
                if (p != null)
                {
                    nameMap[p.id] = p.userName;
                }
            }
        }
        return nameMap;
    }


    private void SyncScore(long serverScore)
    {
        if (serverScore > AccountManager.Instance.currentAccountData.bestScore)
        {
            AccountManager.Instance.UpdateBestScore((int)serverScore);
        }
    }

    // 오프라인용 데이터 로드
    public void LoadOfflineMyData(string myName, long myScore)
    {
        rankList.Clear();
        myRank = new RankData(-1, myName, myScore, "local_user");
    }
}

public class RankData
{
    public int Rank { get; }
    public string UserId {  get; }
    public string NickName { get; }
    public long Score { get; }

    public RankData(int rank, string nickName, long score, string userId)
    {
        Rank = rank;
        NickName = nickName;
        Score = score;
        UserId = userId;
    }
}

