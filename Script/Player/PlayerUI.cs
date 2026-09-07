using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject characterCanvas;
    [SerializeField] private TextMeshProUGUI bonusText;

    private void Awake()
    {        
        if (characterCanvas != null) characterCanvas.SetActive(false);
    }

    public void ShowBonusEffect(string message, int amount)
    {
        if (characterCanvas == null || bonusText == null) return;

        bonusText.text = $"{message} +{amount}";
        AnimateBonusText().Forget();
    }

    private async UniTaskVoid AnimateBonusText()
    {
        characterCanvas.SetActive(true);
        bonusText.alpha = 1f;

        float duration = 1.0f;
        float elapsed = 0f;
                
        Rigidbody2D rb = GetComponentInParent<Rigidbody2D>();
        bool isInverted = (rb != null && rb.gravityScale < 0);
                
        float targetY = isInverted ? -1.0f : 1.0f; // ÃµÀåÀÏ ¶© ¾Æ·¡·Î(-1), ¹Ù´ÚÀÏ ¶© À§·Î(1)
        Vector3 startPos = new Vector3(-1.25f, targetY, 0);
                
        Vector3 moveDirection = isInverted ? Vector3.down : Vector3.up;
        Vector3 endPos = startPos + moveDirection * 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            characterCanvas.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            bonusText.alpha = Mathf.Lerp(1f, 0f, t);

            await UniTask.Yield();
        }

        characterCanvas.SetActive(false);
    }
}
