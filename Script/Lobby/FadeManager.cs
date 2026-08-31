using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FadeManager : DestroySingleton<FadeManager>
{
    [SerializeField]
    Image fadeImage;
    float fadeDuration = 1f;

    private void Awake()
    {
        //fadeImage = GameObject.Find("FadePanel").GetComponent<Image>();
        fadeImage.raycastTarget = false;        
    }

    public async UniTask WaitToSceneLoad(string sceneName)
    {
        fadeImage.raycastTarget = true;
        await Fade(0, 1);
        await SceneManager.LoadSceneAsync(sceneName);
    }

    public async UniTask FadeOut()
    {
        await Fade(1, 0);
        fadeImage.raycastTarget = false;
    }

    private async UniTask Fade(float from, float to)
    {
        float elapsed = 0f;
        Color c = fadeImage.color;

        while(elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            c.a = Mathf.Lerp(from, to, t);
            fadeImage.color = c;
            await UniTask.Yield();
        }

        c.a = to;
        fadeImage.color = c;
    }
}
