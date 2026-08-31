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
        // 시작할 때 캔버스는 꺼둡니다.
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
        //characterCanvas.SetActive(true);

        //float duration = 1.0f;
        //float elapsed = 0f;
        //Vector3 startPos = new Vector3(-1.25f, 0.5f, 0); // 머리 위 높이
        //Vector3 endPos = startPos + Vector3.up * 0.5f;

        //while (elapsed < duration)
        //{
        //    elapsed += Time.deltaTime;
        //    float t = elapsed / duration;

        //    characterCanvas.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
        //    bonusText.alpha = Mathf.Lerp(1f, 0f, t);

        //    await UniTask.Yield();
        //}

        //characterCanvas.SetActive(false);
        //bonusText.alpha = 1f;

        characterCanvas.SetActive(true);
        bonusText.alpha = 1f;

        float duration = 1.0f;
        float elapsed = 0f;

        // 1. 현재 캐릭터의 중력 상태를 확인합니다.
        Rigidbody2D rb = GetComponentInParent<Rigidbody2D>();
        bool isInverted = (rb != null && rb.gravityScale < 0);

        // 2. 중력에 따라 시작 위치와 이동 방향을 결정합니다.
        // 어필님이 찾으신 X값 -1.25f를 기준으로 사용합니다.
        float targetY = isInverted ? -1.0f : 1.0f; // 천장일 땐 아래로(-1), 바닥일 땐 위로(1)
        Vector3 startPos = new Vector3(-1.25f, targetY, 0);

        // 이동 방향도 중력의 반대 방향으로 흐르게 설정
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
