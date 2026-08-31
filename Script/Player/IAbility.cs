using UnityEngine;

public interface IAbility
{
    // 초기화 (생성 시점에 필요한 데이터 주입)
    void Initialize(Player player);

    // 매 프레임 체크가 필요한 로직 (예: 점수 추가)
    void OnUpdate();

    // 특정 이벤트 발생 시 로직 (예: 베리어는 충돌 시 실행)
    bool Execute();
}

public class EmptyAbility : IAbility
{
    public void Initialize(Player player) { }
    public void OnUpdate() { }
    public bool Execute() => false;
}

public class BonusScoreAbility : IAbility
{
    private Player _player;
    private PlayerUI _playerUI; // 캐싱을 위한 변수

    private float _timer = 0f;
    private const float _interval = 15f;
    private int _currentLevel = 1;
    private const int _baseBonus = 25;

    public void Initialize(Player player)
    {
        _player = player;

        // [핵심] 처음에 한 번만 컴포넌트를 찾아서 저장해둡니다.
        _playerUI = player.GetComponent<PlayerUI>();

        if (_playerUI == null)
        {
            Debug.LogWarning($"{player.name}에 PlayerUI 컴포넌트가 없습니다. 시각 연출이 생략됩니다.");
        }
        EventBus.Subscribe<ChangeDifficultyEvent>(OnDifficultyChanged);
    }

    // 레벨이 바뀌면 호출되는 메서드
    private void OnDifficultyChanged(ChangeDifficultyEvent evt)
    {
        _currentLevel = evt.level;
        Debug.Log($"[BonusAbility] 레벨 변경 감지: {_currentLevel}레벨. 이제 보너스는 {_currentLevel * _baseBonus}점입니다.");
    }

    public void OnUpdate()
    {
        _timer += Time.deltaTime;

        if (_timer >= _interval)
        {
            _timer = 0f;

            // 현재 레벨에 비례해서 보너스 점수 계산
            int finalBonus = (_currentLevel - 1) * _baseBonus;

            if (_playerUI != null)
            {
                // UI에도 계산된 점수를 넘겨줍니다.
                _playerUI.ShowBonusEffect("Bonus !", finalBonus);
            }

            // 점수 추가 요청 발행
            EventBus.Publish(new RequestAddScoreEvent(finalBonus));
        }
    }

    public bool Execute() => false;

    // 만약 어빌리티가 파괴되거나 교체된다면 구독 해제도 고려해야 합니다.
    // (현재 구조상 Player와 수명을 같이 한다면 생략 가능하지만, 안전을 위해 기재)
    public void Dispose()
    {
        EventBus.Unsubscribe<ChangeDifficultyEvent>(OnDifficultyChanged);
    }
}

public class BarrierAbility : IAbility
{
    private Player _player;
    private bool _hasBarrier = true;
    private GameObject _barrierVisual;

    public void Initialize(Player player)
    {
        _player = player;
        _barrierVisual = player.transform.Find("BarrierVisual")?.gameObject;
        _barrierVisual?.SetActive(true);
    }

    public void OnUpdate() { }

    public bool Execute()
    {
        if (_hasBarrier)
        {
            _hasBarrier = false;

            if (_barrierVisual != null) _barrierVisual.SetActive(false);

            _player.TriggerInvincible(1f).Forget();

            Debug.Log("베리어 소모! 생존 성공");
            return true; // 방어 성공했으므로 true 반환
        }
        return false; // 베리어 없으면 false
    }
}