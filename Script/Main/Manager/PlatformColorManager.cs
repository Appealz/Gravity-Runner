using UnityEngine;

public class PlatformColorManager : MonoBehaviour
{
    [SerializeField] private Material tileMaterial;

    // 무지개 팔레트
    private Color[] rainbowColors = new Color[]
    {
        new Color(1f, 0.4f, 0.4f),   // 더 진한 파스텔 레드
        new Color(1f, 0.65f, 0.4f),  // 더 진한 파스텔 오렌지
        new Color(1f, 1f, 0.5f),     // 더 진한 파스텔 옐로우
        new Color(0.5f, 1f, 0.5f),   // 더 진한 파스텔 그린
        new Color(0.5f, 0.65f, 1f),  // 더 진한 파스텔 블루
        new Color(0.65f, 0.5f, 1f),  // 더 진한 파스텔 퍼플
        new Color(1f, 0.5f, 0.75f)   // 더 진한 파스텔 핑크
    };

    private int currentIndex = 0;
    private Color startColor;
    private Color targetColor;
    private float lerpTime;
    [SerializeField] private float transitionDuration = 2f; // 색 변환 시간(초)

    private async void Awake()
    {
        tileMaterial = await AddressableLoader.LoadToMaterial("NeonMaterial");

        // 초기 색상 세팅
        startColor = rainbowColors[0];
        targetColor = rainbowColors[0];
        tileMaterial.SetColor("_Color", startColor);

        EventBus.Subscribe<ChangeDifficultyEvent>(OnChangeDifficulty);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<ChangeDifficultyEvent>(OnChangeDifficulty);
    }

    private void Update()
    {
        if (tileMaterial == null) return;

        // 진행률 계산 (0~1)
        float t = Mathf.Clamp01(lerpTime / transitionDuration);

        // 보간된 색상 적용
        Color newColor = Color.Lerp(startColor, targetColor, t);
        tileMaterial.SetColor("_Color", newColor);

        // 시간이 남아있다면 진행률 업데이트
        if (t < 1f)
            lerpTime += Time.deltaTime;
    }

    private void OnChangeDifficulty(ChangeDifficultyEvent evt)
    {
        // 다음 무지개 색으로 전환
        currentIndex = (currentIndex + 1) % rainbowColors.Length;

        startColor = tileMaterial.GetColor("_Color");   // 현재 색에서 시작
        targetColor = rainbowColors[currentIndex];     // 목표 색
        lerpTime = 0f;                                 // 새 보간 시작
    }
}
