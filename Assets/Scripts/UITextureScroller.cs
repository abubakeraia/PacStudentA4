using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UITextureScroller : MonoBehaviour
{
    public Vector2 scrollSpeed = new Vector2(0.25f, 0f);
    private Image img;
    private Material runtimeMat;
    private Vector2 offset;

    void Awake()
    {
        img = GetComponent<Image>();

        runtimeMat = (img.material != null)
            ? new Material(img.material)
            : new Material(Shader.Find("UI/Default"));

        img.material = runtimeMat;
    }

    void OnDestroy()
    {
        if (runtimeMat != null)
            Destroy(runtimeMat);
    }

    void Update()
    {
        offset += scrollSpeed * Time.unscaledDeltaTime;
        if (offset.x > 1f) offset.x -= 1f;
        if (offset.y > 1f) offset.y -= 1f;
        runtimeMat.SetTextureOffset("_MainTex", offset);
    }
}
