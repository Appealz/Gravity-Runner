using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class GameOverView : MonoBehaviour
{
    private Image continueBtnImage;
    private Color originalColor;
    [SerializeField] private Button continueBtn;
    public Button ContinueBtn => continueBtn;
    [SerializeField] private Button returnBtn;
    public Button ReturnBtn => returnBtn;
    [SerializeField] private Button restartBtn;
    public Button RestartBtn => restartBtn;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI newText;
    [SerializeField] private TextMeshProUGUI continuCountText;
    [SerializeField] private TextMeshProUGUI continueBtnText;

    public void Awake()
    {
        if (continueBtn != null)
        {
            continueBtnImage = continueBtn.GetComponent<Image>();
            if (continueBtnImage != null)
                originalColor = continueBtnImage.color;
        }
    }

    public void UpdateContinueText(string message, bool isInteractable)
    {
        continueBtnText.text = message;
        continueBtn.interactable = isInteractable;
    }

    public void SetContinueChance(int chance)
    {
        continuCountText.text = $"x {chance}";
        //continueBtn.interactable = chance > 0;
    }

    public void Show(float newScore, float newHighScore, bool canRevive, int reviveChance, bool isNew)
    {
        gameObject.SetActive(true);
        SetContinueChance(reviveChance);
        scoreText.text = $"{newScore:F0}";
        highScoreText.text = $"{newHighScore:F0}";


        var blink = newText.GetComponent<TextBlink>();
        if (isNew)
        {
            newText.gameObject.SetActive(isNew);
            blink.StopBlink();
            blink.StartBlink();
        }
        else
        {

            blink.StopBlink();
            newText.gameObject.SetActive(isNew);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void UpdateContinueUI(string message, bool isInteractable, bool shouldBlink)
    {
        continueBtnText.text = message;
        continueBtn.interactable = isInteractable;

        if (shouldBlink && continueBtnImage != null)
        {
            // 0.7(최소) ~ 1.0(최대) 사이를 왕복하는 알파값 계산
            // Sin 함수는 -1 ~ 1을 반환하므로, 이를 0.7 ~ 1.0으로 변환합니다.
            float lerpTime = (Mathf.Sin(Time.unscaledTime * 4.5f) + 1f) / 2f; // 0 ~ 1
            float alpha = Mathf.Lerp(0.5f, 1f, lerpTime);

            // 원래 색상에서 알파값만 변경해서 적용
            Color newColor = originalColor;
            newColor.a = alpha;
            continueBtnImage.color = newColor;
        }
        else
        {
            // 깜빡이지 않을 때는 원래 색상(알파 1.0)으로 복구
            if (continueBtnImage != null)
                continueBtnImage.color = originalColor;
        }
    }
}
