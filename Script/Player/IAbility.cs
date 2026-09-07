using UnityEngine;

public interface IAbility
{    
    void Initialize(Player player); 
    void OnUpdate();    
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
    private PlayerUI _playerUI;

    private float _timer = 0f;
    private const float _interval = 15f;
    private int _currentLevel = 1;
    private const int _baseBonus = 25;

    public void Initialize(Player player)
    {
        _player = player;        
        _playerUI = player.GetComponent<PlayerUI>();

        if (_playerUI == null)
        {
            Debug.LogWarning($"{player.name}에 PlayerUI 컴포넌트가 없습니다. 시각 연출이 생략됩니다.");
        }
        EventBus.Subscribe<ChangeDifficultyEvent>(OnDifficultyChanged);
    }
        
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
            int finalBonus = (_currentLevel - 1) * _baseBonus;

            if (_playerUI != null)
            {                
                _playerUI.ShowBonusEffect("Bonus !", finalBonus);
            }            
            EventBus.Publish(new RequestAddScoreEvent(finalBonus));
        }
    }

    public bool Execute() => false;
    
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
            return true;
        }
        return false;
    }
}