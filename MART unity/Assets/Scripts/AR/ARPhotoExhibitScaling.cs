using UnityEngine;

public static class ARPhotoExhibitScaling
{
    public static void FitMaxDimension(GameObject root, float maxDimension)
    {
        if (root == null || maxDimension <= 0f)
        {
            return;
        }

        float currentSize = GetMaxDimension(root);
        if (currentSize < 0.0001f)
        {
            return;
        }

        float factor = maxDimension / currentSize;
        root.transform.localScale *= factor;
    }

    public static float GetMaxDimension(GameObject root)
    {
        Bounds bounds = GetRendererBounds(root);
        return Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
    }

    public static Bounds GetRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(root.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }
}
