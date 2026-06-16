using UnityEngine;

/// <summary>
/// Builds exhibit objects for AR photo mode. Uses a prefab from Resources when present,
/// otherwise a colored primitive (easy to swap for real models later).
/// </summary>
public static class ARPhotoExhibitFactory
{
    private const string ModelsResourcePath = "Exhibits/Models/";
    private const string SelectableTag = "Placeable";
    private const int InteractiveLayer = 0;

    private static readonly PrimitiveType[] FallbackPrimitives =
    {
        PrimitiveType.Cube,
        PrimitiveType.Sphere,
        PrimitiveType.Capsule,
        PrimitiveType.Cylinder,
    };

    private static readonly Color[] FallbackColors =
    {
        new Color(0.85f, 0.35f, 0.25f),
        new Color(0.25f, 0.55f, 0.9f),
        new Color(0.35f, 0.75f, 0.4f),
        new Color(0.9f, 0.75f, 0.2f),
    };

    public static GameObject Create(string exhibitId, int exhibitIndex)
    {
        GameObject prefab = Resources.Load<GameObject>(ModelsResourcePath + exhibitId);
        GameObject instance = prefab != null
            ? Object.Instantiate(prefab)
            : CreatePrimitive(exhibitIndex);

        instance.name = "ARPhotoExhibit_" + exhibitId;
        PrepareInstance(instance, exhibitIndex);
        return instance;
    }

    public static void PrepareSpawnedInstance(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        PrepareInstance(instance, 0);
    }

    private static GameObject CreatePrimitive(int exhibitIndex)
    {
        PrimitiveType primitiveType = FallbackPrimitives[exhibitIndex % FallbackPrimitives.Length];
        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.transform.localScale = Vector3.one * 0.35f;

        Color color = FallbackColors[exhibitIndex % FallbackColors.Length];
        Renderer renderer = primitive.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (material.shader == null || material.shader.name.Contains("Hidden"))
            {
                material = new Material(Shader.Find("Standard"));
            }

            material.color = color;
            renderer.sharedMaterial = material;
        }

        return primitive;
    }

    private static void PrepareInstance(GameObject instance, int exhibitIndex)
    {
        if (!string.IsNullOrWhiteSpace(SelectableTag))
        {
            instance.tag = SelectableTag;
        }

        SetLayerRecursively(instance.transform, InteractiveLayer);

        if (instance.GetComponentInChildren<Collider>(true) == null)
        {
            Renderer renderer = instance.GetComponentInChildren<Renderer>(true);
            if (renderer != null)
            {
                BoxCollider boxCollider = instance.AddComponent<BoxCollider>();
                Bounds bounds = renderer.bounds;
                Vector3 localCenter = instance.transform.InverseTransformPoint(bounds.center);
                Vector3 localSize = instance.transform.InverseTransformVector(bounds.size);
                boxCollider.center = localCenter;
                boxCollider.size = new Vector3(
                    Mathf.Abs(localSize.x),
                    Mathf.Abs(localSize.y),
                    Mathf.Abs(localSize.z));
            }
        }

        if (instance.transform.localScale == Vector3.one)
        {
            instance.transform.localScale = Vector3.one * 0.35f;
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
}
