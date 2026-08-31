public class HUDModel
{
    private float score;
    public float Score => score;
    private float timer;
    public float Timer => timer;

    private float highScore;
    public float HighScore => highScore;

    private int coin;
    public int Coin => coin;

    public void InitHighScore(float newHighScore)
    {
        highScore = newHighScore;
    }

    public void UpdateScore(float newScore)
    {
        score = newScore;
        SetHighScore();
    }

    public void UpdateTimer(float newTime)
    {
        timer = newTime;
    }

    public void AddCoin(int amount)
    {
        coin += amount;
    }


    public void SetHighScore()
    {
        if(score >  HighScore)
        {
            highScore = score;
        }
    }

    public void Reset()
    {
        timer = 0;
        score = 0;
        coin = 0;
    }
}