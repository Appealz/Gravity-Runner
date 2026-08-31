using GooglePlayGames;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyView : MonoBehaviour
{
    Button playBtn;
    public Button PlayBtn => playBtn;
    Button optionBtn;
    public Button OptionBtn => optionBtn;   
    Button rankBtn;
    public Button RankBtn => rankBtn;
    [SerializeField] Slider bgmSlider;
    public Slider BGMSlider => bgmSlider;
    [SerializeField] Slider sfxSlider;
    public Slider SFXSlider => sfxSlider;

    [SerializeField] GameObject optionImage;
    [SerializeField] Button closeOptionBtn;
    public Button CloseOptionBtn => closeOptionBtn;

    [SerializeField] private GameObject rankImage;
    [SerializeField] private GameObject exitImage;
    [SerializeField] Button exitCanceledBtn;
    public Button ExitCanceledBtn => exitCanceledBtn;
    [SerializeField] Button exitBtn;
    public Button ExitBtn => exitBtn;

    [SerializeField]Button characterBtn;
    public Button CharacterBtn => characterBtn;
    public GameObject characterImage;

    [SerializeField] private Button loginBtn;
    [SerializeField] private TextMeshProUGUI nickNameText;

    public Button LoginBtn => loginBtn;

    private bool _isLoading = false;

    public void Init()
    {
        playBtn = GameObject.Find("PlayBtn").GetComponent<Button>();
        optionBtn = GameObject.Find("OptionBtn").GetComponent<Button>();
        rankBtn = GameObject.Find("RankBtn").GetComponent<Button>();

        _isLoading = false;
        nickNameText.gameObject.SetActive(true);
    }

    private void Update()
    {
        // 5번 항목: 로그인 중일 때 닉네임 위치의 텍스트 깜빡임 연출
        if (_isLoading)
        {
            Color color = nickNameText.color;
            // 0.3(흐릿함) ~ 1.0(선명함) 사이를 왕복
            color.a = Mathf.PingPong(Time.time , 0.7f) + 0.3f;
            nickNameText.color = color;
        }
    }

    public void SetLoadingState(bool isLoading)
    {
        _isLoading = isLoading;
        loginBtn.interactable = !isLoading; // 로그인 중에는 버튼 클릭 방지

        if (isLoading)
        {
            // 닉네임 대신 "Signing in..." 표시
            nickNameText.text = "Signing in...";
        }
        else
        {
            // 로딩 종료 시 투명도 복구 및 UI 갱신
            Color color = nickNameText.color;
            color.a = 1.0f;
            nickNameText.color = color;
            RefreshLoginUI();
        }
    }

    public void RefreshLoginUI()
    {
        // 로딩 중일 때는 닉네임 정보를 덮어쓰지 않도록 방어
        if (_isLoading) return;

        bool isAuthenticated = PlayGamesPlatform.Instance.IsAuthenticated();
        var btnText = loginBtn.GetComponentInChildren<TextMeshProUGUI>();

        // 3번 항목: 왼쪽 상단 닉네임 출력
        if (AccountManager.Instance.currentAccountData != null)
        {
            nickNameText.text = AccountManager.Instance.currentAccountData.nickname;
        }

        // 4번 항목: 로그인 상태에 따라 버튼 텍스트 변경
        btnText.text = isAuthenticated ? "Switch Account" : "Google Login";
    }

    public void SetBGMVolume(float volume)
    {
        bgmSlider.value = volume;
    }
    public void SetSFXVolume(float volume)
    {
        sfxSlider.value = volume;
    }

    public void ShowOption()
    {
        optionImage.SetActive(true);
    }

    public void HideOption()
    {
        optionImage.SetActive(false);
    }

    public void ShowRank()
    {
        rankImage.SetActive(true);
    }

    public void HideRank()
    {
        rankImage.SetActive(false);
    }

    public void ShowExit()
    {
        exitImage.SetActive(true);
    }

    public void HideExit()
    {
        exitImage.SetActive(false);
    }

    public bool IsOptionActive()
    {
        return optionImage.activeSelf;
    }

    public bool IsRankActive()
    {
        return rankImage.activeSelf;
    }

    public bool IsExitActive()
    {
        return exitImage.activeSelf; // exitPanel은 Exit UI GameObject
    }

    public void ShowCharacterSelect()
    {
        // 인터넷 연결 확인
        if (!NetworkChecker.CheckInternet())
            return;

        characterImage.SetActive(true);
    }
}
