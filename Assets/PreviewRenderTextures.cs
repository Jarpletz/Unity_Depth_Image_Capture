using UnityEngine;

[ExecuteInEditMode]
public class PreviewRenderTextures : MonoBehaviour
{
    public UnityEngine.UI.RawImage depthDisplay;
    public UnityEngine.UI.RawImage colorDisplay;


    void Update()
    {
        if (depthDisplay && OutputTexturesFeature.DepthTexture)
            depthDisplay.texture = OutputTexturesFeature.DepthTexture;

        if (colorDisplay && OutputTexturesFeature.ColorTexture)
            colorDisplay.texture = OutputTexturesFeature.ColorTexture;
    }
}
