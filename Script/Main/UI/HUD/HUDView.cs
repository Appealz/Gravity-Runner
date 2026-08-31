using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDView : MonoBehaviour
{
    TextMeshProUGUI scoreText;
    TextMeshProUGUI timerText;
    TextMeshProUGUI highScoreText;
    TextMeshProUGUI coinText;
    [SerializeField] Button pauseBtn;

    private void Awake()
    {        
        scoreText = GameObject.Find("Score").GetComponent<TextMeshProUGUI>();
        timerText = GameObject.Find("Timer").GetComponent<TextMeshProUGUI>();
        highScoreText = GameObject.Find("HighScore").GetComponent<TextMeshProUGUI>();
        coinText = GameObject.Find("CoinText").GetComponent<TextMeshProUGUI>();

        pauseBtn.onClick.AddListener(() =>
        {
            EventBus.Publish(new OnPauseEvent(true));
        });
    }


    public void SetScore(float score)
    {
        int intScore = (int)score;
        scoreText.text = $"Score : {intScore:F0}";
    }

    public void SetTimer(float time)
    {
        timerText.text = $"{time:F2}";
    }

    public void SetHighScore(float highScore)
    {
        int intScore = (int)highScore;
        highScoreText.text = $"HighScore : {intScore:F0}";
    }
    public void SetCoin(int coin)
    {
        coinText.text = $"Coin : {coin:D2}";
    }

}
