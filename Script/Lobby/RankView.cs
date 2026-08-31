using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankView : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private Button closeBtn;
    public Button CloseBtn => closeBtn;

    private List<GameObject> rankList = new List<GameObject> ();

    [Header("MyRank Text")]
    [SerializeField] private TextMeshProUGUI myRankText;
    [SerializeField] private TextMeshProUGUI myNickNameText;
    [SerializeField] private TextMeshProUGUI myScoreText;

    [Header("Settings")]
    [SerializeField] private int maxRowCount = 20; // 최대 표시 개수

    private void Awake()
    {
        for (int i = 0; i < maxRowCount; i++)
        {
            GameObject row = Instantiate(rowPrefab, content);
            row.name = $"RankRow_{i + 1}";
            row.SetActive(false);
            rankList.Add(row);
        }
    }
    public void Clear()
    {
        // Destroy 대신 SetActive(false)로 변경
        foreach (var item in rankList)
        {
            item.SetActive(false);
        }
    }

    public void AddRow(int rank, string nickName, long score, bool isMine)
    {
        GameObject row = rankList.Find(r => !r.activeSelf);
        if (row == null)
        {
            Debug.LogWarning("[RankView] 활성화 가능한 Row가 없습니다!");
            return;
        }        
        row.SetActive(true);

        TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();
        if(texts.Length >= 3)
        {
            texts[0].text = rank.ToString();
            texts[1].text = nickName;
            texts[2].text = score.ToString();
        }

        // 내 랭크면 색상 강조
        if (isMine)
        {
            texts[0].color = new Color(1f, 0f, 0f);
            texts[1].color = new Color(1f, 0f, 0f);
            texts[2].color = new Color(1f, 0f, 0f);
        }        
    }

    public void SetRows(List<RankData> newRankList, RankData myData)
    {
        Clear();

        // 데이터 수만큼만 활성화
        int count = Mathf.Min(newRankList.Count, rankList.Count);
        for (int i = 0; i < count; i++)
        {
            var rank = newRankList[i];
            bool isMine = rank.UserId == myData.UserId && myData.Rank > 0;
            AddRow(rank.Rank, rank.NickName, rank.Score, isMine);
        }

        SetMyRank(myData);
    }

    private void SetMyRank(RankData myData)
    {
        myRankText.text = myData.Rank <= 0 ? "UnRank" : myData.Rank.ToString();
        myNickNameText.text = myData.NickName;
        myScoreText.text = myData.Score.ToString();
    }

    public void Hide()
    {        
        gameObject.SetActive (false);
    }
}
