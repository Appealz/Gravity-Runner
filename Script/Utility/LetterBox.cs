using UnityEngine;

public class LetterBox : MonoBehaviour
{
    private float targetAspectWidth = 21f;
    private float targetAspectHeight = 9f;

    private void Awake()
    {
        Camera cam;
        if(!TryGetComponent<Camera>(out cam))
        {
            Debug.Log("cam ! Camera is missing");
        }

        float targetAspect = targetAspectWidth/ targetAspectHeight;
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if(scaleHeight < 1.0f)
        {
            Rect rect = new Rect(0, (1 - scaleHeight) / 2, 1, scaleHeight);
            cam.rect = rect;
        }
        else
        {
            float scaleWidth = 1f / scaleHeight;
            Rect rect = new Rect((1 - scaleWidth) / 2, 0, scaleWidth, 1);
            cam.rect = rect;            
        }
    }
}
