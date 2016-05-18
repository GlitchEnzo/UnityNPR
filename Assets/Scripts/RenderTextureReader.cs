using UnityEngine;

[RequireComponent(typeof(Camera))]
public class RenderTextureReader : MonoBehaviour
{
    public KeyCode keyToRead = KeyCode.R;

    private Camera attachedCamera;

    private RenderTexture attachedRenderTexture;

    void Start()
    {
        attachedCamera = GetComponent<Camera>();
        attachedRenderTexture = attachedCamera.targetTexture;
    }

    void Update()
    {
        if (Input.GetKeyDown(keyToRead))
        {
            if (attachedRenderTexture != null)
            {
                //RenderBuffer buffer = attachedRenderTexture.colorBuffer;
                //buffer.???
            }
        }
    }
}
