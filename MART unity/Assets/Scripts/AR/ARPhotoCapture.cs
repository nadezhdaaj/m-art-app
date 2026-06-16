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

        Camera arCamera = ARPhotoExhibitPlacer.FindArCamera();
        if (arCamera == null)
        {
            ToastNotification.Show("Камера AR недоступна");
            FinishCapture();
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

        Texture2D screenshot = null;
        try
        {
            screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }

        RestoreOverlayUi();

        if (screenshot == null)
        {
            if (previewWasVisible && photoPreview != null)
            {
                photoPreview.RestoreVisible();
            }

            ToastNotification.Show("Не удалось сделать снимок");
            FinishCapture();
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

        photoPreview.ShowPhoto(screenshot, arCamera);
        FinishCapture();
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
