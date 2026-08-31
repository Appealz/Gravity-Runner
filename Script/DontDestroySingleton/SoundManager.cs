using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : DontDestroySingleton<SoundManager>
{
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

    public float BGMVolume => bgmVolume;
    public float SFXVolume => sfxVolume;

    public AudioSource bgmSource;
    private List<AudioSource> sfxSources = new List<AudioSource>();
    private Dictionary<string, AudioClip> sfxCache = new Dictionary<string, AudioClip>();
    private float pausedTime = 0f; // 일시정지 시 재생 위치 저장

    protected override void DoAwake()
    {
        base.DoAwake();

        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        bgmSource = GetComponent<AudioSource>();
        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;

        for (int i = 0; i < 10; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.volume = sfxVolume;
            sfxSources.Add(src);
        }
    }

    // --------------------------------------------------
    // BGM 관련
    // --------------------------------------------------

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (clip == null)
            return;

        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = bgmVolume;
        bgmSource.time = 0f;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
            pausedTime = 0f;
        }
    }

    public void RePlayBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying && bgmSource.clip != null)
        {
            bgmSource.time = pausedTime;
            bgmSource.Play();
        }
    }

    public void PauseBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            pausedTime = bgmSource.time;
            bgmSource.Pause();
        }
    }

    public void RestartBGM(bool loop = true)
    {
        if (bgmSource != null && bgmSource.clip != null)
        {
            bgmSource.Stop();
            bgmSource.loop = loop;
            bgmSource.volume = bgmVolume;
            bgmSource.time = 0f;
            bgmSource.Play();
        }
    }

    // sfx 관련
    public async UniTask<AudioClip> LoadSFX(string key)
    {
        if (sfxCache.TryGetValue(key, out var clip))
            return clip;

        var loaded = await AddressableLoader.LoadToClip(key);
        if (loaded != null)
            sfxCache[key] = loaded;
        else
            Debug.LogWarning($"SFX {key} not found.");

        return loaded;
    }

    public async void PlaySFX(string key)
    {
        var clip = await LoadSFX(key);
        if (clip == null) return;

        AudioSource src = GetAvailableSFXSource();
        src.clip = clip;
        src.volume = sfxVolume;
        src.Play();
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (var src in sfxSources)
        {
            if (!src.isPlaying)
                return src;
        }
        return sfxSources[0]; // 다 재생 중이면 첫 번째 재사용
    }

    // --------------------------------------------------
    // 볼륨 관련
    // --------------------------------------------------

    public void SetBGMVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }

        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }


}