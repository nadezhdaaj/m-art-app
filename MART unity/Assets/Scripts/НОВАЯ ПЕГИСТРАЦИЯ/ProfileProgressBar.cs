using UnityEngine;
using UnityEngine.UI;

public class ProfileProgressBar : MonoBehaviour
{
    public Image progressFill;

    public int maxPoints = 350;
    public int currentPoints;

    public void SetPoints(int points)
    {
        currentPoints = points;

        float value = (float)currentPoints / maxPoints;
        progressFill.fillAmount = value;
    }
}