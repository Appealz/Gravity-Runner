using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterView : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Image selectImage;
    [SerializeField] private DescriptionView descriptionView;
    public DescriptionView DescriptionView => descriptionView;
    [SerializeField] private GameObject errorPopup;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private Button closeBtn;

    List<CharacterButton> characterButtons = new List<CharacterButton>();
    
    public event Action<CharacterRuntimeData> OnCharacterSelected;

    private void Awake()
    {
        closeBtn.onClick.AddListener(CloseCharacter);
    }
    private void OnEnable()
    {
        // 창 열릴 때 코인 최신값 한 번만 반영
        UpdateCoin(CurrencyManager.Instance.GetCoin());
    }

    public async UniTask Init(List<CharacterRuntimeData> runtimeDataList)
    {
        // Addressable로 버튼 프리팹 로드
        GameObject prefab = await AddressableLoader.LoadToPrefab("CharacterButton");
        var buttonPrefab = prefab.GetComponent<CharacterButton>();

        // 기존 버튼 정리
        foreach (Transform child in buttonParent)
            Destroy(child.gameObject);
        characterButtons.Clear();

        // 버튼 생성
        foreach (var data in runtimeDataList)
        {
            var button = Instantiate(buttonPrefab, buttonParent);
            button.Init(data);

            button.OnClicked += (clickedData) =>
            {
                OnCharacterSelected?.Invoke(clickedData);
            };

            characterButtons.Add(button);
        }

        Debug.Log($"[CharacterView] 버튼 {characterButtons.Count}개 생성 (Addressable Prefab)");        
    }

    public void ShowDescription(CharacterRuntimeData data)
    {
        if (descriptionView == null)
        {
            Debug.LogError("[CharacterView] DescriptionView reference is null! (Inspector에 연결 필요)");
            return;
        }

        Debug.Log($"[CharacterView] ShowDescription 실행: {data.BaseData.displayName}");
        descriptionView.Show(data);
    }

    // 선택된 메인 캐릭터 설정.
    public void UpdateSelectedCharacter(Sprite icon)
    {
        selectImage.sprite = icon;
        Debug.Log($"현재 선택된 캐릭터 아이디: {AccountManager.Instance.currentAccountData.selectedCharacterId}");
    }
    public void UpdateCoin(int coin) => coinText.text = $"{coin}";


    public void ShowErrorPopup(string message)
    {
        // 잠금 되어 있는 캐릭터 선택시 에러 메세지 팝업 show
        Debug.Log(message);
    }

    public void HidePopup()
    {
        // 에러 메세지 팝업 hide
    }

    private void CloseCharacter()
    {
        SoundManager.Instance.PlaySFX("TouchClose");
        gameObject.SetActive(false);
    }

    public void RefreshButtons()
    {
        foreach (var button in characterButtons)
        {
            var data = button.RuntimeData;
            button.Refresh(data);
        }
    }

}
