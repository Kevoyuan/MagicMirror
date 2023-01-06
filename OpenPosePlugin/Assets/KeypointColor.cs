using UnityEngine;
using UnityEngine.UI;

public class KeypointColor : MonoBehaviour
{
    public Color color;

    private void Awake()
    {
        Image image = GetComponent<Image>();
        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
        }
        image.color = color;
    }
}
