using DG.Tweening;
using TMPro;
using UnityEngine;

public class TextBlink : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI newText;
    private Tween blinkTween;

    private void OnEnable()
    {
        StartBlink();
    }

    private void OnDisable()
    {
        StopBlink();
    }

    public void StartBlink()
    {
        // 항상 기존 트윈 제거
        if (blinkTween != null)
        {
            blinkTween.Kill();
            blinkTween = null;
        }

        // 알파 초기화
        newText.alpha = 1f;

        // 새 트윈 시작
        blinkTween = newText.DOFade(0f, 1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
    }

    public void StopBlink()
    {
        if (blinkTween != null && blinkTween.IsActive())
        {
            blinkTween.Kill();
        }
        newText.alpha = 1f;
    }


}
