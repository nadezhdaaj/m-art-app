using UnityEngine;

/// <summary>
/// Shows the latest captured AR photo on a plane in front of the user (not saved to disk).
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Museum AR/AR Photo Preview")]
public class ARPhotoPreview : MonoBehaviour
{
    private const string PreviewRootName = "ARPhotoPreviewPlane";

    [SerializeField] private float previewDistance = 0.55f;
    [SerializeField] private float previewMaxWidth = 0.32f;

    private GameObject previewRoot;
    private Material previewMaterial;
    private Texture2D lastPhotoTexture;

    public bool HasPhoto => lastPhotoTexture != null;

    public bool IsVisible => previewRoot != null && previewRoot.activeSelf;

    public Texture2D CurrentPhoto => lastPhotoTexture;

    public bool ShowPhoto(Texture2D photo, Camera arCamera)
    {
        if (photo == null || arCamera == null)
        {
            return false;
        }

        if (lastPhotoTexture != null && lastPhotoTexture != photo)
        {
            Destroy(lastPhotoTexture);
        }

        lastPhotoTexture = photo;
        EnsurePreviewPlane(arCamera);

        if (previewRoot == null || previewMaterial == null)
        {
            Debug.LogWarning("AR Photo: не удалось создать плоскость превью (шейдер недоступен?).");
            return false;
        }

        ApplyTextureToPlane(lastPhotoTexture);
        previewRoot.SetActive(true);
        return true;
    }

    public void HidePhoto()
    {
        if (previewRoot != null)
        {
            previewRoot.SetActive(false);
        }
    }

    public void RestoreVisible()
    {
        if (previewRoot != null && lastPhotoTexture != null)
        {
            previewRoot.SetActive(true);
        }
    }

    public void ClearPhoto()
    {
        HidePhoto();

        if (lastPhotoTexture != null)
        {
            Destroy(lastPhotoTexture);
            lastPhotoTexture = null;
        }
    }

    private void EnsurePreviewPlane(Camera arCamera)
    {
        if (previewRoot != null)
        {
            AttachToCamera(arCamera);
            return;
        }

        previewRoot = GameObject.CreatePrimitive(PrimitiveType.Quad);
        previewRoot.name = PreviewRootName;

        Collider collider = previewRoot.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        MeshRenderer renderer = previewRoot.GetComponent<MeshRenderer>();
        Shader shader = ResolvePreviewShader();
        if (shader == null)
        {
            // Без шейдера материал создавать нельзя (new Material(null) роняет показ).
            Debug.LogWarning("AR Photo: не найден ни один шейдер для превью.");
            Destroy(previewRoot);
            previewRoot = null;
            return;
        }

        previewMaterial = new Material(shader);
        renderer.material = previewMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        AttachToCamera(arCamera);
    }

    private static Shader ResolvePreviewShader()
    {
        // Перебираем кандидатов в порядке надёжности. В сборке часть шейдеров может
        // быть вырезана, поэтому подстраховываемся встроенными вариантами.
        string[] candidates =
        {
            "Universal Render Pipeline/Unlit",
            "Unlit/Texture",
            "Sprites/Default",
            "UI/Default"
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            Shader shader = Shader.Find(candidates[i]);
            if (shader != null)
            {
                return shader;
            }
        }

        return null;
    }

    private void AttachToCamera(Camera arCamera)
    {
        Transform cameraTransform = arCamera.transform;
        previewRoot.transform.SetParent(cameraTransform, false);
        previewRoot.transform.localPosition = new Vector3(0f, 0f, previewDistance);
        previewRoot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
    }

    private void ApplyTextureToPlane(Texture2D texture)
    {
        if (previewRoot == null || previewMaterial == null || texture == null)
        {
            return;
        }

        previewMaterial.mainTexture = texture;

        float aspect = texture.height > 0 ? (float)texture.width / texture.height : 1f;
        float width = previewMaxWidth;
        float height = width / Mathf.Max(aspect, 0.01f);
        previewRoot.transform.localScale = new Vector3(width, height, 1f);
    }

    private void OnDestroy()
    {
        ClearPhoto();

        if (previewMaterial != null)
        {
            Destroy(previewMaterial);
            previewMaterial = null;
        }

        if (previewRoot != null)
        {
            Destroy(previewRoot);
            previewRoot = null;
        }
    }
}
