using UnityEngine;

public class PauseModel
{
    public bool isPause { get; private set; }
    private float bgmVolume;
    public float BGMVolume => bgmVolume;
    private float sfxVolume;
    public float SFXVolume => sfxVolume;
    public bool isVibrationOn { get; private set; }

    public PauseModel()
    {
        // 씬 진입 시 SoundManager 값 동기화
        bgmVolume = SoundManager.Instance.BGMVolume;
        sfxVolume = SoundManager.Instance.SFXVolume;
    }

    public void SetPause(bool isOn)
    {
        isPause = isOn;
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void SetVibration(bool isOn)
    {
        isVibrationOn = isOn;
    }

}