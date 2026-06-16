using UnityEngine;

[DisallowMultipleComponent]
public class ProfileArtworksGalleryBootstrap : MonoBehaviour
{
    private void Awake()
    {
        ProfileArtworksGallery gallery = GetComponent<ProfileArtworksGallery>();
        if (gallery == null)
        {
            gallery = gameObject.AddComponent<ProfileArtworksGallery>();
        }
    }
}
