using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Scriptable Objects/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("기본 정보")]
    public string id;
    public string displayName;
    [TextArea] public string description;

    [Header("시각 자료")]
    public Sprite icon;    

    [Header("구매 정보")]
    public int price;
    public bool defaultUnlocked;  // 기본 해금 여부
}
