using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

public enum GameState
{
    Ready,    // 카운트다운 중
    Playing,  // 실제 게임 플레이
    Paused,   // 일시정지
    GameOver  // 게임 종료
}

public class GameManager : DestroySingleton<GameManager>
{
    public List<BaseManager> managers = new List<BaseManager>();
    public CameraManager cameraManager;
    public PlatformSpawner platformSpawnManager;
    private ScrollManager scrollManager;
    public ScrollManager Scroll => scrollManager;

    [SerializeField] TextMeshProUGUI countdownText;

    private UIManager uiManager;

    private ScoreManager scoreManager;

    private Player player;
    public Player Player => player;

    private ObstacleSpawner obstacleSpawner;
    public ObstacleSpawner ObstacleSpawner => obstacleSpawner;

    private bool isGameStart;
    public GameState State { get; private set; }

    [SerializeField] private AudioClip gameBGM;
    public SoundManager soundManager;

    private CoinSpawnManager coinSpawnManager;

    private DifficultyManager difficultyManager;


    private void Awake()
    {
        FindAllManagers();        
    }

    private void FindAllManagers()
    {
        //player = FindAnyObjectByType<Player>();
        cameraManager = FindAnyObjectByType<CameraManager>();
        platformSpawnManager = FindAnyObjectByType<PlatformSpawner>();
        scrollManager = FindAnyObjectByType<ScrollManager>();
        uiManager = FindAnyObjectByType<UIManager>();
        difficultyManager = FindAnyObjectByType<DifficultyManager>();
        scoreManager = FindAnyObjectByType<ScoreManager>();
        obstacleSpawner = FindAnyObjectByType<ObstacleSpawner>();
        coinSpawnManager = FindAnyObjectByType<CoinSpawnManager>();
        soundManager = FindAnyObjectByType<SoundManager>();
    }

    private async UniTask SpawnPlayerAsync()
    {
        if (AccountManager.Instance?.currentAccountData == null) return;
        string selectedId = AccountManager.Instance.currentAccountData.selectedCharacterId;

        // 만약 LoadToPrefab이 단순 로드라면 Instantiate를 해줘야 합니다.
        GameObject prefab = await AddressableLoader.LoadToPrefab(selectedId);
        GameObject playerObj = Instantiate(prefab); // 실제 씬에 생성

        player = playerObj.GetComponent<Player>();

        IAbility ability = CreateAbilityById(selectedId);
        player.SetAbility(ability);
        player.PlayerStartPosition();
    }

    private IAbility CreateAbilityById(string id)
    {
        return id switch
        {
            "Char_4" => new BarrierAbility(),     // 베리어 능력
            "Char_3" => new BonusScoreAbility(),  // 점수 보너스
            _ => new EmptyAbility()              // 기본 캐릭터
        };
    }
    private async UniTask InitializeAllManagers()
    {
        await Addressables.InitializeAsync().ToUniTask();
        Debug.Log("[GameManager] Addressables Catalog 로드 완료");

        // 모든 매니저의 초기화를 동시에 시작하고 전부 끝날 때까지 기다림
        await UniTask.WhenAll(
            PoolManager.Instance.InitializePools(),
            uiManager.Initialize(),
            scoreManager.Initialize(),
            difficultyManager.Initialize(),
            scrollManager.Initialize(),
            coinSpawnManager.Initialize(),
            cameraManager.Initialize(),            
            obstacleSpawner.Initialize()
        );

        await platformSpawnManager.Initialize();
        await SpawnPlayerAsync();

        // 모든 로드가 끝난 후 의존성 주입 및 상태 설정
        platformSpawnManager.SetCameraValues(cameraManager.Left, cameraManager.Right);
        scrollManager.SetRunning(false);
        platformSpawnManager.SetRunning(false);

        State = GameState.Ready;
        isGameStart = false;
    }

    private async void Start()
    {
        await InitializeAllManagers();

        scoreManager.PostInitialize();
        difficultyManager.PostInitialize();

        if (FadeManager.Instance != null)
        {
            await FadeManager.Instance.FadeOut();
        }
        
        gameBGM = await AddressableLoader.LoadToClip("MainBGM");
        Time.timeScale = 0f;

        await uiManager.ShowCountdown(3);

        Time.timeScale = 1f;
        GameStart();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
        EventBus.Subscribe<OnPauseEvent>(OnGamePause);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        EventBus.Unsubscribe<OnPauseEvent>(OnGamePause);

        scoreManager?.Shutdown();
        coinSpawnManager?.Shutdown();
        uiManager.Shutdown();        
        platformSpawnManager?.Shutdown();
    }

    private void Update()
    {
        uiManager.CustomUpdate();
        scoreManager.CustomUpdate();
        difficultyManager.CustomUpdate();
        scrollManager.CustomUpdate();
        platformSpawnManager.CustomUpdate();
    }

    public T GetManager<T>() where T : BaseManager
    {
        foreach (var manager in managers)
        {
            if (manager is T target)
                return target;
        }

        // 못 찾았으면 씬에서 검색 (예: FindAnyObjectByType)
        T found = FindAnyObjectByType<T>();
        if (found != null)
        {
            managers.Add(found);
            return found;
        }

        Debug.LogWarning($"[GameManager] {typeof(T).Name}을(를) 찾을 수 없습니다.");
        return null;
    }

    public void GameStart()
    {
        if (player == null) return;

        isGameStart = true;
        State = GameState.Playing;

        // 매니저들 구동
        scrollManager.SetRunning(true);
        platformSpawnManager.SetRunning(true);
        obstacleSpawner.SetRunning(true);

        // 플레이어 초기화 (여기서 Init을 호출하면 중력이 0이 됨)
        player.Init();
        scoreManager.SetEnable(true);
        soundManager.PlayBGM(gameBGM);
    }

    private void GameStop()
    {
        State = GameState.Paused;
        Time.timeScale = 0f;
        player.Pause();
        soundManager.PauseBGM();        
    }

    private void GameResume()
    {
        if (State == GameState.GameOver)
            return;

        if (!isGameStart)
        {
            Time.timeScale = 1f;
            State = GameState.Ready;
            return;
        }

        Time.timeScale = 1f;
        if(State == GameState.Paused)
        {
            player.ReStart();
            State = GameState.Playing;
            soundManager.RePlayBGM();
            
        }
            
    }

    private async void OnGameOver(GameOverEvent e)
    {
        scoreManager.SetEnable(false);
        State = GameState.GameOver;
        isGameStart = false;        
        Debug.Log("게임 오버");
        
        Time.timeScale = 0f; // GameStop() 호출 대신 명시적 제어
        player.Pause();
        soundManager.PauseBGM();

        await UniTask.Delay(1000, ignoreTimeScale: true);        
        scoreManager.PublishFinalScore();
        CurrencyManager.Instance.ResultCoin();
    }

    private void OnGamePause(OnPauseEvent e)
    {
        if (State == GameState.GameOver)
            return;

        if (e.isPause)
        {
            GameStop();
        }
        else
        {
            GameResume();
        }
    }

    public async UniTask GameReStart()
    {
        player.Revive();

        await UniTask.Delay(100, ignoreTimeScale: true);

        isGameStart = true;
        State = GameState.Playing;

        Time.timeScale = 1f;

        scoreManager.SetEnable(true);
        soundManager.RePlayBGM();
    }
}

public class GameOverEvent
{
    public bool isGameOver;

    public GameOverEvent(bool isOver)
    {
        isGameOver = isOver;
    }
}
