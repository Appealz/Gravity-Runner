using Cysharp.Threading.Tasks;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class RankModel
{    
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
            async (data) => 
            {
                if (data.Status == ResponseStatus.Success)
                {                    
                    string[] userIds = new string[data.Scores.Length];
                    for (int i = 0; i < data.Scores.Length; i++)
                        userIds[i] = data.Scores[i].userID;
                    
                    var nicknameMap = await LoadUserNames(userIds);
                                        
                    for (int i = 0; i < data.Scores.Length; i++)
                    {
                        var score = data.Scores[i];
                        string actualNick;

                        if (score.userID == PlayGamesPlatform.Instance.GetUserId())
                        {
                            actualNick = myData.nickname;
                        }
                        else
                        {                            
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
        
    private async UniTask<Dictionary<string, string>> LoadUserNames(string[] userIds)
    {
        var nameMap = new Dictionary<string, string>();                
        var profileTcs = new UniTaskCompletionSource<IUserProfile[]>();
                
        PlayGamesPlatform.Instance.LoadUsers(userIds, (users) =>
        {
            profileTcs.TrySetResult(users);
        });

        var profiles = await profileTcs.Task;
        if (profiles != null)
        {
            foreach (var p in profiles)
            {                
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

