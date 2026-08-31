using UnityEngine;
using UnityEngine.UI;

public class PauseView : MonoBehaviour
{
    [SerializeField] private Button resumeBtn;
    public Button ResumeBtn => resumeBtn;
    [SerializeField] private Button restartBtn;
    public Button RestartBtn => restartBtn;
    [SerializeField] private Button returnLobbyBtn;
    public Button ReturnLobbyBtn => returnLobbyBtn;
    [SerializeField] private Slider bgmSlider;
    public Slider BGMSlider => bgmSlider;
    [SerializeField] private Slider sfxSlider;
    public Slider SFXSlider => sfxSlider;
    [SerializeField] private Button exitBtn;
    public Button ExitBtn => exitBtn;


    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetBGMVolume(float volume)
    {
        bgmSlider.value = volume;
    }
    public void SetSFXVolume(float volume)
    {
        sfxSlider.value = volume;
    }
}
