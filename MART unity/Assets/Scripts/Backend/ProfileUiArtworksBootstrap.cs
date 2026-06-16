using UnityEngine;

/// <summary>
/// Runtime fallback: restores See All and User's work if missing from the scene.
/// </summary>
public static class ProfileUiArtworksBootstrap
{
    public static void EnsureAll()
    {
        ProfileUiArtworksHierarchyBuilder.EnsureAll();
    }

    public static GameObject EnsureUserWorksScreen()
    {
        return ProfileUiArtworksHierarchyBuilder.EnsureUserWorksScreen();
    }

    public static void EnsureSeeAllButton()
    {
        ProfileUiArtworksHierarchyBuilder.EnsureSeeAllButton();
    }
}
