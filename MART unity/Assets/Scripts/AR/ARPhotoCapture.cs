using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Captures the AR camera view (without UI overlay) and shows it in front of the user.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Museum AR/AR Photo Capture")]
public class ARPhotoCapture : MonoBehaviour
{
    private readonly List<GameObject> hiddenForCapture = new List<GameObject>();

    private bool captureInProgress;
    private ARPhotoPreview photoPreview;

    public bool CanCapture => !captureInProgress;

    public event Action CaptureFinished;

    private void Awake()
    {
        photoPreview = GetComponent<ARPhotoPreview>();
        if (photoPreview == null)
        {
            photoPreview = gameObject.AddComponent<ARPhotoPreview>();
        }
    }

    public void TakePhoto()
    {
        if (captureInProgress || !isActiveAndEnabled)
        {
            return;
        }

        StartCoroutine(TakePhotoCoroutine());
    }

    private IEnumerator TakePhotoCoroutine()
    {
        captureInProgress = true;

        // Всё тело обёрнуто в try/finally: что бы ни случилось внутри (исключение в
        // ShowPhoto, недоступный шейдер и т.п.), FinishCapture обязан выполниться,
        // иначе captureInProgress навсегда останется true, кнопка съёмки залипнет
        // неактивной и переход к превью не произойдёт.
        try
        {
            Camera arCamera = ARPhotoExhibitPlacer.FindArCamera();
            if (arCamera == null)
            {
                ToastNotification.Show("Камера AR недоступна");
                yield break;
            }

            bool previewWasVisible = photoPreview != null && photoPreview.IsVisible;
            if (photoPreview != null)
            {
                photoPreview.HidePhoto();
            }

            HideOverlayUi();

            yield return null;
            yield return new WaitForEndOfFrame();

            Texture2D screenshot = CaptureScreen(arCamera);

            RestoreOverlayUi();

            if (screenshot == null)
            {
                if (previewWasVisible && photoPreview != null)
                {
                    photoPreview.RestoreVisible();
                }

                Debug.LogWarning("AR Photo: не удалось получить кадр ни одним из способов захвата.");
                ToastNotification.Show("Не удалось сделать снимок");
                yield break;
            }

            if (photoPreview == null)
            {
                photoPreview = GetComponent<ARPhotoPreview>();
                if (photoPreview == null)
                {
                    photoPreview = gameObject.AddComponent<ARPhotoPreview>();
                }
            }

            bool shown = false;
            try
            {
                shown = photoPreview.ShowPhoto(screenshot, arCamera);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            if (!shown)
            {
                DestroyTexture(ref screenshot);
                ToastNotification.Show("Не удалось показать снимок");
            }
        }
        finally
        {
            // На случай, если исключение прервало кадр между Hide и Restore.
            RestoreOverlayUi();
            FinishCapture();
        }
    }

    /// <summary>
    /// Делает снимок экрана устойчиво на разных устройствах. На Android (URP + AR Foundation)
    /// штатный ScreenCapture.CaptureScreenshotAsTexture нередко возвращает null/пустую текстуру,
    /// поэтому при неудаче пробуем чтение бэкбуфера, а затем рендер AR-камеры в RenderTexture.
    /// Вызывать строго после WaitForEndOfFrame.
    /// </summary>
    private Texture2D CaptureScreen(Camera arCamera)
    {
        Texture2D screenshot = null;
        try
        {
            screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }

        if (IsUsableTexture(screenshot))
        {
            return screenshot;
        }

        DestroyTexture(ref screenshot);
        Debug.LogWarning("AR Photo: ScreenCapture не дал кадр, пробуем чтение бэкбуфера.");

        try
        {
            int width = Screen.width;
            int height = Screen.height;
            if (width > 0 && height > 0)
            {
                screenshot = new Texture2D(width, height, TextureFormat.RGBA32, false);
                screenshot.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                screenshot.Apply();
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            DestroyTexture(ref screenshot);
        }

        if (IsUsableTexture(screenshot))
        {
            return screenshot;
        }

        DestroyTexture(ref screenshot);
        Debug.LogWarning("AR Photo: чтение бэкбуфера не помогло, рендерим AR-камеру в RenderTexture.");

        return CaptureViaCamera(arCamera);
    }

    private Texture2D CaptureViaCamera(Camera arCamera)
    {
        if (arCamera == null)
        {
            return null;
        }

        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);

        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousTarget = arCamera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;

        Texture2D result = null;
        try
        {
            arCamera.targetTexture = renderTexture;
            arCamera.Render();

            RenderTexture.active = renderTexture;
            result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            result.Apply();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            DestroyTexture(ref result);
        }
        finally
        {
            arCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(renderTexture);
        }

        return IsUsableTexture(result) ? result : null;
    }

    private static bool IsUsableTexture(Texture2D texture)
    {
        return texture != null && texture.width > 1 && texture.height > 1;
    }

    private void DestroyTexture(ref Texture2D texture)
    {
        if (texture != null)
        {
            Destroy(texture);
            texture = null;
        }
    }

    private void HideOverlayUi()
    {
        hiddenForCapture.Clear();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        Transform canvasTransform = canvas.transform;
        for (int i = 0; i < canvasTransform.childCount; i++)
        {
            Transform child = canvasTransform.GetChild(i);
            GameObject childObject = child.gameObject;
            if (!childObject.activeSelf)
            {
                continue;
            }

            if (childObject == gameObject)
            {
                HideActiveChildren(transform);
            }
            else
            {
                hiddenForCapture.Add(childObject);
                childObject.SetActive(false);
            }
        }
    }

    private void HideActiveChildren(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (!child.gameObject.activeSelf)
            {
                continue;
            }

            hiddenForCapture.Add(child.gameObject);
            child.gameObject.SetActive(false);
        }
    }

    private void RestoreOverlayUi()
    {
        for (int i = 0; i < hiddenForCapture.Count; i++)
        {
            GameObject hiddenObject = hiddenForCapture[i];
            if (hiddenObject != null)
            {
                hiddenObject.SetActive(true);
            }
        }

        hiddenForCapture.Clear();
    }

    private void FinishCapture()
    {
        captureInProgress = false;
        CaptureFinished?.Invoke();
    }
}
