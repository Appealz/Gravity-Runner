using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : DestroySingleton<LobbyManager>
{
    CameraManager cameraManager;

    [Header("Lobby UI")]
    [SerializeField]LobbyView lobbyView;
    LobbyModel lobbyModel;
    LobbyPresenter lobbyPresenter;

    [Header("Rank UI")]
    [SerializeField] private RankView rankView;
    private RankModel rankModel;
    private RankPresenter rankPresenter;


    [Header("Character UI")]
    [SerializeField] private CharacterView characterView;
    private CharacterModel characterModel;
    private CharacterPresenter characterPresenter;

    private async void Awake()
    {
        float timer = 0;
        while (RemoteConfigManager.Instance.LatestVersion == "1.0.0" && timer < 2.0f)
        {
            await UniTask.Yield();
            timer += Time.deltaTime;
        }

        if (RemoteConfigManager.Instance.IsUpdateRequired())
        {
            // 어필님이 GPGSManager에 만들어두신 '토스트' 발동!
            GPGSManager.Instance.ShowToast("버전이 낮아 게임을 이용할 수 없습니다. 업데이트 후 재실행해주세요.");

            // 플레이스토어 열기
            Application.OpenURL($"market://details?id={Application.identifier}");

            // 유저가 다른 짓 못하게 앱 바로 끕니다.
            Application.Quit();
            return; // 아래 초기화 로직(카메라, 사운드 등) 실행 안 함
        }


        cameraManager = FindAnyObjectByType<CameraManager>();
        cameraManager.Initialize();

        var lobbyBGM = await AddressableLoader.LoadToClip("LobbyBGM");

        //  2. 중복 방지 (이미 같은 BGM이면 재생 안함)
        if (SoundManager.Instance.bgmSource != lobbyBGM)
        {
            SoundManager.Instance.PlayBGM(lobbyBGM);
        }


        if (!TryGetComponent<LobbyView>(out lobbyView))
        {
            Debug.Log("lobbyView ! LobbyView is missing");
        }
        lobbyView.Init();
        lobbyModel = new LobbyModel();
        lobbyPresenter = new LobbyPresenter(lobbyModel, lobbyView);
        rankModel = new RankModel();
        rankPresenter = new RankPresenter(rankModel, rankView);
        Time.timeScale = 1f;

        lobbyPresenter.SetRankPresenter(rankPresenter);

        await InitCharacterSystem();

        Time.timeScale = 1f;

        Debug.Log($"현재 선택된 캐릭터 아이디: {AccountManager.Instance.currentAccountData.selectedCharacterId}");
    }

    private async UniTask InitCharacterSystem()
    {
        ////  이미 캐시되어 있으면 재초기화 생략
        //if (!CharacterDataManager.Instance.HasCache)
        //{
        //    characterModel = new CharacterModel();
        //    await characterModel.Initialize();
        //    CharacterDataManager.Instance.CacheFrom(characterModel);
        //    Debug.Log("[LobbyManager] 캐릭터 데이터 최초 로드 및 캐싱 완료");
        //}
        //else
        //{
        //    Debug.Log("[LobbyManager] 기존 캐시 사용");
        //}

        ////  캐릭터 버튼 초기화
        //var characters = new List<CharacterRuntimeData>(CharacterDataManager.Instance.GetAll());
        //await characterView.Init(characters);

        ////  Presenter 연결
        //characterPresenter = new CharacterPresenter(characterModel, characterView);

        //// 선택 캐릭터 복원
        //var selectedId = AccountManager.Instance.currentAccountData.selectedCharacterId;
        //var selectedData = CharacterDataManager.Instance.Get(selectedId);
        //if (selectedData != null)
        //{
        //    characterView.UpdateSelectedCharacter(selectedData.BaseData.icon);
        //    Debug.Log($"[LobbyManager] 선택 캐릭터 복원 완료: {selectedData.BaseData.displayName}");
        //}

        characterModel = new CharacterModel();
        await characterModel.Initialize();
        CharacterDataManager.Instance.CacheFrom(characterModel);

        await characterView.Init(new List<CharacterRuntimeData>(characterModel.Characters));

        characterPresenter = new CharacterPresenter(characterModel, characterView);

        //  Presenter 등록 후에 복원 실행
        var selectedId = AccountManager.Instance.currentAccountData.selectedCharacterId;
        var selectedData = CharacterDataManager.Instance.Get(selectedId);
        if (selectedData != null)
        {
            characterView.UpdateSelectedCharacter(selectedData.BaseData.icon);
            Debug.Log($"[LobbyManager] 선택 캐릭터 복원 완료: {selectedData.BaseData.displayName}");
        }

        Debug.Log("[LobbyManager] 캐릭터 선택 시스템 초기화 완료 ");

    }

    private void OnDisable()
    {
        lobbyPresenter.Dispose();
        characterPresenter.Dispose();
    }

}
