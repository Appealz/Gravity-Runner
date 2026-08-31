using UnityEngine;

public class GameOverModel
{
    public int ReviveChance { get; private set; } = 1;
    public bool CanRevive => ReviveChance > 0;

    public void UseRevive()
    {
        if (ReviveChance > 0)
            ReviveChance--;
    }

    public void Reset()
    {
        ReviveChance = 1;
    }
}