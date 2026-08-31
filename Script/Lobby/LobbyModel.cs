using UnityEngine;

public class LobbyModel
{
    private float bgmVolume;
    public float BGMVolume => bgmVolume;
    private float sfxVolume;
    public float SFXVolume => sfxVolume;

    private string mainSceneName = "MainScene";
    public string MainSceneName => mainSceneName;

    public LobbyModel()
    {
        bgmVolume = SoundManager.Instance.BGMVolume;
        sfxVolume = SoundManager.Instance.SFXVolume;
    }

    public void SetBGMVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
    }
    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
    }
}