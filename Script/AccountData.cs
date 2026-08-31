using System.Collections.Generic;
using UnityEngine;

public class AccountData
{
    public string acountID; // 구글 고유 ID
    public string nickname; // 구글플레이 닉네임
    public long bestScore; // 최고 점수
    public int coin; // 보유 코인
    public string selectedCharacterId; // 현재 선택된 캐릭터의 ID(이름)
    public List<string> unlockedCharacterIds = new(); // 해금된 캐릭터 목록    
    public string lastLoginData; // 마지막 로그인 시간 (동기화 확인용)

    public AccountData(string newID, string newNickname)
    {
        acountID = newID;
        nickname = newNickname;
        bestScore = 0;        
        coin = 0;
        unlockedCharacterIds = new List<string> { "Char_0" };
        selectedCharacterId = "Char_0";
        lastLoginData = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}