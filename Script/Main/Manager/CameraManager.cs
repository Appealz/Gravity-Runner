using Cysharp.Threading.Tasks;
using UnityEngine;

public class CameraManager : BaseManager
{
    private float targetAspectWidth = 21f;
    private float targetAspectHeight = 9f;
    private Camera cam;
    [SerializeField] private Camera uiCam;     // UI Àü¿ë
    public float HalfHeight { get; private set; }
    public float HalfWidth { get; private set; }
    public float Left { get; private set; }
    public float Right { get; private set; }

    public override UniTask Initialize()
    {        
        if(cam == null )
            cam = Camera.main;

        if (uiCam == null)
            uiCam = GameObject.Find("UICamera")?.GetComponent<Camera>();
        ApplyLetterBox();

        return base.Initialize();
    }

    private void ApplyLetterBox()
    {
        float targetAspect = targetAspectWidth / targetAspectHeight;
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            Rect rect = new Rect(0, (1 - scaleHeight) / 2, 1, scaleHeight);
            cam.rect = rect;
            uiCam.rect = rect;
        }
        else
        {
            float scaleWidth = 1f / scaleHeight;
            Rect rect = new Rect((1 - scaleWidth) / 2, 0, scaleWidth, 1);
            cam.rect = rect;
            uiCam.rect = rect;
        }

        SetCameraValues();
    }

    private void SetCameraValues()
    {
        HalfHeight = cam.orthographicSize;
        HalfWidth = HalfHeight * cam.aspect;
        Left = -HalfWidth;
        Right = -HalfWidth;
    }


    public override void CustomUpdate()
    {
        base.CustomUpdate();
    }

    public override void Shutdown()
    {
        base.Shutdown();
    }
}
