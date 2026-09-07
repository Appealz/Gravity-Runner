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

    [SerializeField] private GameObject guestStart;
    [SerializeField] private Button guestStartBtn;
    [SerializeField] private Button guestCancelBtn;

    public Button GuestStartBtn => guestStartBtn;
    public Button GuestCancelBtn => guestCancelBtn;

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
        if (_isLoading)
        {
            Color color = nickNameText.color;            
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
            nickNameText.text = "Signing in...";
        }
        else
        {         
            Color color = nickNameText.color;
            color.a = 1.0f;
            nickNameText.color = color;
            RefreshLoginUI();
        }
    }

    public void RefreshLoginUI()
    {
        if (_isLoading)
            return;

        bool isGoogleUser =
            GPGSManager.Instance != null &&
            GPGSManager.Instance.IsGoogleUser;

        var btnText =
            loginBtn.GetComponentInChildren<TextMeshProUGUI>();

        if (AccountManager.Instance.currentAccountData != null)
        {
            nickNameText.text =
                AccountManager.Instance.currentAccountData.nickname;
        }

        btnText.text =
            isGoogleUser ? "Switch Account" : "Google Login";
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

    public void ShowGuestStart()
    {
        guestStart.SetActive(true);
    }

    public void HideGuestStart()
    {
        guestStart.SetActive(false);
    }

    public void ShowCharacterSelect()
    {
        // 인터넷 연결 확인
        if (!NetworkChecker.CheckInternet())
            return;

        characterImage.SetActive(true);
    }
}
