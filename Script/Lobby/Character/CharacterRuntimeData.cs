using UnityEngine;

public class CharacterRuntimeData
{
    public CharacterData BaseData { get; private set; }
    public bool IsUnlocked { get; private set; }

    public CharacterRuntimeData(CharacterData data, bool unlocked)
    {
        BaseData = data;
        IsUnlocked = unlocked || data.defaultUnlocked;
    }

    public void Unlock()
    {
        IsUnlocked = true;
    }
    public void SetUnlocked(bool unlocked)
    {
        IsUnlocked = unlocked || BaseData.defaultUnlocked;
    }
}
