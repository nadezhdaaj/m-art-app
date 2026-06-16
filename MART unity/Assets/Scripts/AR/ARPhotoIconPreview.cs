using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Renders exhibit prefab into icon UI. Uses RenderTexture on RawImage (reliable),
/// then optional tight sprite for model-shaped outline.
/// </summary>
public class ARPhotoIconPreview : MonoBehaviour
{
    private const int TextureSize = 512;
    private const int PreviewLayer = 6;
    private const byte AlphaThreshold = 8;
    private const int MinOpaquePixels = 64;

    [SerializeField] private float iconPreviewMaxSize = 0.55f;
    [SerializeField] private float previewFramePadding = 1.1f;
    [Tooltip("Higher = larger model inside the icon preview.")]
    [SerializeField] private float previewZoom = 2.78f;

    private static readonly Vector3 PreviewWorldPosition = new Vector3(1000f, 1000f, 1000f);

    private Camera previewCamera;
    private Light keyLight;
    private Light fillLight;
    private RenderTexture renderTexture;
    private GameObject previewRoot;
    private RawImage rawTarget;
    private Image spriteTarget;
    private Texture2D capturedTexture;
    private Sprite capturedSprite;
    private Coroutine renderCoroutine;
    private Color previewFallbackColor = Color.white;

    public void ConfigureScale(float maxSize)
    {
        iconPreviewMaxSize = Mathf.Max(0.01f, maxSize);
    }

    public void Bind(RawImage rawImage, Image silhouetteImage = null)
    {
        rawTarget = rawImage;
        spriteTarget = silhouetteImage;
        EnsurePreviewCamera();
        ResetDisplay();
    }

    public void ShowPrefab(GameObject prefab)
    {
        ShowPrefab(prefab, Color.white);
    }

    public void ShowPrefab(GameObject prefab, Color fallbackColor)
    {
        if (prefab == null || rawTarget == null)
        {
            return;
        }

        previewFallbackColor = fallbackColor;
        EnsurePreviewCamera();
        ClearPreviewModel();
        ReleaseCapturedAssets();
        ResetDisplay();

        previewRoot = Instantiate(prefab, PreviewWorldPosition, prefab.transform.rotation);
        previewRoot.name = "ARPhotoIconPreviewModel";
        previewRoot.SetActive(true);
        SetLayerRecursively(previewRoot.transform, PreviewLayer);
        ARPhotoExhibitScaling.FitMaxDimension(previewRoot, iconPreviewMaxSize);
        FrameCameraOnModel();

        if (renderCoroutine != null)
        {
            StopCoroutine(renderCoroutine);
        }

        renderCoroutine = StartCoroutine(RenderAfterLayout());
    }

    public void ShowColor(Color color)
    {
        ClearPreviewModel();
        ReleaseCapturedAssets();
        ResetDisplay();

        if (rawTarget != null)
        {
            rawTarget.color = color;
        }
    }

    public void Clear()
    {
        ClearPreviewModel();
        ReleaseCapturedAssets();
        ResetDisplay();
    }

    private void ResetDisplay()
    {
        if (rawTarget != null)
        {
            rawTarget.texture = renderTexture;
            rawTarget.material = null;
            rawTarget.color = Color.white;
            rawTarget.enabled = true;
        }

        if (spriteTarget != null)
        {
            spriteTarget.sprite = null;
            spriteTarget.material = null;
            spriteTarget.enabled = false;
        }
    }

    private IEnumerator RenderAfterLayout()
    {
        yield return null;
        yield return RenderWithUrpCamera();
        renderCoroutine = null;
    }

    private IEnumerator RenderWithUrpCamera()
    {
        if (previewCamera == null || previewRoot == null || rawTarget == null)
        {
            yield break;
        }

        ClearRenderTarget();

        // URP: enable camera for one frame, then manual Render() as fallback.
        previewCamera.enabled = true;
        yield return new WaitForEndOfFrame();
        previewCamera.Render();
        previewCamera.enabled = false;

        rawTarget.texture = renderTexture;
        rawTarget.material = null;
        rawTarget.color = Color.white;
        rawTarget.enabled = true;

        if (spriteTarget != null)
        {
            spriteTarget.sprite = null;
            spriteTarget.enabled = false;
        }

        if (!RenderTextureHasContent())
        {
            ApplyPreviewFallback();
        }
    }

    private bool RenderTextureHasContent()
    {
        if (renderTexture == null)
        {
            return false;
        }

        Texture2D probe = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        probe.ReadPixels(new Rect(0, 0, TextureSize, TextureSize), 0, 0);
        probe.Apply();
        RenderTexture.active = previous;

        bool hasContent = HasEnoughOpaquePixels(probe);
        Destroy(probe);
        return hasContent;
    }

    private void ApplyPreviewFallback()
    {
        if (rawTarget == null)
        {
            return;
        }

        rawTarget.texture = null;
        rawTarget.color = previewFallbackColor;

        if (spriteTarget != null)
        {
            spriteTarget.enabled = false;
        }
    }

    private void TryApplyTightSprite()
    {
        if (spriteTarget == null)
        {
            return;
        }

        ReleaseCapturedAssets();

        capturedTexture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        capturedTexture.ReadPixels(new Rect(0, 0, TextureSize, TextureSize), 0, 0);
        capturedTexture.Apply();
        RenderTexture.active = previous;

        if (!HasEnoughOpaquePixels(capturedTexture))
        {
            return;
        }

        RectInt crop = CalculateOpaqueBounds(capturedTexture);
        Rect spriteRect = new Rect(crop.x, crop.y, crop.width, crop.height);
        capturedSprite = Sprite.Create(
            capturedTexture,
            spriteRect,
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.Tight);

        spriteTarget.sprite = capturedSprite;
        spriteTarget.preserveAspect = true;
        spriteTarget.useSpriteMesh = true;
        spriteTarget.color = Color.white;
        spriteTarget.material = null;
        spriteTarget.enabled = true;
    }

    private static bool HasEnoughOpaquePixels(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        int count = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > AlphaThreshold)
            {
                count++;
                if (count >= MinOpaquePixels)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static RectInt CalculateOpaqueBounds(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();

        int minX = width;
        int minY = height;
        int maxX = 0;
        int maxY = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[(y * width) + x].a <= AlphaThreshold)
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return new RectInt(0, 0, width, height);
        }

        const int padding = 6;
        minX = Mathf.Max(0, minX - padding);
        minY = Mathf.Max(0, minY - padding);
        maxX = Mathf.Min(width - 1, maxX + padding);
        maxY = Mathf.Min(height - 1, maxY + padding);

        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private void ClearRenderTarget()
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = previous;
    }

    private void FrameCameraOnModel()
    {
        if (previewRoot == null || previewCamera == null)
        {
            return;
        }

        Quaternion exhibitRotation = previewRoot.transform.rotation;

        // Level camera, same prefab rotation as in AR.
        previewCamera.transform.SetPositionAndRotation(PreviewWorldPosition, Quaternion.identity);
        previewRoot.transform.SetParent(previewCamera.transform, false);
        previewRoot.transform.localRotation = exhibitRotation;
        previewRoot.transform.localPosition = Vector3.zero;

        Bounds bounds = ARPhotoExhibitScaling.GetRendererBounds(previewRoot);
        Vector3 boundsCenterLocal = previewCamera.transform.InverseTransformPoint(bounds.center);
        float zoom = Mathf.Max(0.1f, previewZoom);
        float fitDistance = ComputeFitDistance(bounds, previewCamera, previewFramePadding) / zoom;
        previewRoot.transform.localPosition = new Vector3(
            -boundsCenterLocal.x,
            -boundsCenterLocal.y,
            fitDistance - boundsCenterLocal.z);

        Vector3 center = ARPhotoExhibitScaling.GetRendererBounds(previewRoot).center;
        UpdatePreviewLights(center, previewCamera.transform.forward);
    }

    private static float ComputeFitDistance(Bounds bounds, Camera camera, float padding)
    {
        float radius = bounds.extents.magnitude;
        if (radius < 0.0001f)
        {
            return 1.2f;
        }

        float safePadding = Mathf.Max(1f, padding);
        float verticalFov = camera.fieldOfView * Mathf.Deg2Rad;
        float horizontalFov = 2f * Mathf.Atan(Mathf.Tan(verticalFov * 0.5f) * camera.aspect);
        float distanceForHeight = radius / Mathf.Sin(verticalFov * 0.5f);
        float distanceForWidth = radius / Mathf.Sin(horizontalFov * 0.5f);
        return Mathf.Max(distanceForHeight, distanceForWidth) * safePadding;
    }

    private void EnsurePreviewCamera()
    {
        if (previewCamera != null)
        {
            return;
        }

        renderTexture = new RenderTexture(TextureSize, TextureSize, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false
        };
        renderTexture.Create();

        var cameraObject = new GameObject("ARPhotoIconPreviewCamera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        cameraObject.transform.SetParent(transform, false);

        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = Color.clear;
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.orthographic = false;
        previewCamera.fieldOfView = 28f;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 30f;
        previewCamera.targetTexture = renderTexture;
        previewCamera.allowHDR = false;
        previewCamera.allowMSAA = false;
        previewCamera.enabled = false;
        previewCamera.depth = -100f;
        previewCamera.forceIntoRenderTexture = true;

        UniversalAdditionalCameraData urpData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderType = CameraRenderType.Base;
        urpData.renderShadows = false;
        urpData.renderPostProcessing = false;

        EnsurePreviewLights();
    }

    private void EnsurePreviewLights()
    {
        if (keyLight != null)
        {
            return;
        }

        var keyObject = new GameObject("ARPhotoIconPreviewKeyLight");
        keyObject.hideFlags = HideFlags.HideAndDontSave;
        keyObject.transform.SetParent(transform, false);
        keyLight = keyObject.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.cullingMask = 1 << PreviewLayer;
        keyLight.intensity = 1.5f;

        var fillObject = new GameObject("ARPhotoIconPreviewFillLight");
        fillObject.hideFlags = HideFlags.HideAndDontSave;
        fillObject.transform.SetParent(transform, false);
        fillLight = fillObject.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.cullingMask = 1 << PreviewLayer;
        fillLight.intensity = 0.65f;
    }

    private void UpdatePreviewLights(Vector3 targetCenter, Vector3 viewDirection)
    {
        EnsurePreviewLights();
        keyLight.transform.position = targetCenter - (viewDirection * 0.6f) + (Vector3.up * 0.45f);
        keyLight.transform.LookAt(targetCenter);

        Vector3 fillDirection = Vector3.Cross(viewDirection, Vector3.up).normalized;
        fillLight.transform.position = targetCenter + (fillDirection * 0.5f) + (Vector3.up * 0.2f);
        fillLight.transform.LookAt(targetCenter);
    }

    private void ReleaseCapturedAssets()
    {
        if (capturedSprite != null)
        {
            Destroy(capturedSprite);
            capturedSprite = null;
        }

        if (capturedTexture != null)
        {
            Destroy(capturedTexture);
            capturedTexture = null;
        }
    }

    private void ClearPreviewModel()
    {
        if (renderCoroutine != null)
        {
            StopCoroutine(renderCoroutine);
            renderCoroutine = null;
        }

        if (previewRoot != null)
        {
            Destroy(previewRoot);
            previewRoot = null;
        }
    }

    private static void SetLayerRecursively(Transform current, int layer)
    {
        current.gameObject.layer = layer;
        for (int i = 0; i < current.childCount; i++)
        {
            SetLayerRecursively(current.GetChild(i), layer);
        }
    }

    private void OnDestroy()
    {
        ClearPreviewModel();
        ReleaseCapturedAssets();

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
