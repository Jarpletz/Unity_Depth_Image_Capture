using System;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule.Util;

// This Render Feature takes the rendered product (intended to be run at AfterRenderPostProcesssing)
// and outputs two static/globally accessible textures of a specified width/height:
//   OutputTexturesFeature.ColorTexture: A copy of the rendered color data as will be displayed to the screen
//   OutputTexturesFeature.DepthTexture: A depth image of the same render. 
public class OutputTexturesFeature : ScriptableRendererFeature
{
    // Pass which outputs a texture from rendering to inspect a texture 
    class OutputTexturePass : ScriptableRenderPass
    {
        // The texture name you wish to bind the texture handle to for a given material.
        string m_TextureName;
        // The texture type you want to retrive from URP.
        // The material used for blitting to the color output.
        Material m_Material;

        RTHandle m_DepthTexture;
        RTHandle m_ColorTexture;


        // Function set setup the ConfigureInput() and transfer the renderer feature settings to the render pass.
        public void Setup(RTHandle depthHandle, RTHandle colorHandle)
        {

            // Setup the texture name, type and material used when blitting.
            // In this example we will use a mateial using a custom name for the input texture name when blitting.
            // This texture name has to match the material texture input you are using.
            m_TextureName = "_InputTexture";
            // The material is used to blit the texture to the cameras color attachment.
            Shader shader = Shader.Find("Shader Graphs/BlitTargetTexture");
            if (shader == null)
            {
                Debug.LogError("Shader 'Shader Graphs/BlitTargetTexture' not found. Unable to render feature.");
            }
            m_Material = new Material(shader);

            m_DepthTexture = depthHandle;
            m_ColorTexture = colorHandle;
        }

        // Records a render graph render pass which blits the BlitData's active texture back to the camera's color attachment.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Fetch UniversalResourceData from frameData to retrive the URP's texture handles.
            var resourceData = frameData.Get<UniversalResourceData>();

            // COLOR
            var colorSource = resourceData.cameraColor;

            if (!colorSource.IsValid())
            {
                Debug.Log("Color Input texture is not created. Likely the pass event is before the creation of the resource. Skipping pass");
                return;
            }

            var colorHandle = renderGraph.ImportTexture(m_ColorTexture);
            RenderGraphUtils.BlitMaterialParameters colorPara = new(colorSource, colorHandle, m_Material, 0);
            colorPara.sourceTexturePropertyID = Shader.PropertyToID(m_TextureName);
            renderGraph.AddBlitPass(colorPara, passName: "Blit Color Resource");

            // DEPTH
            var depthSource = resourceData.cameraDepthTexture;

            if (!depthSource.IsValid())
            {
                Debug.Log("Depth texture is not created. Likely the pass event is before the creation of the resource. Skipping pass.");
                return;
            }

            var depthHandle = renderGraph.ImportTexture(m_DepthTexture);
            RenderGraphUtils.BlitMaterialParameters depthPara = new(depthSource, depthHandle, m_Material, 0);
            depthPara.sourceTexturePropertyID = Shader.PropertyToID(m_TextureName);
            renderGraph.AddBlitPass(depthPara, passName: "Blit Depth Resource");


        }
    }

    // Inputs in the inspector to change the settings for the renderer feature.
    [SerializeField]
    RenderPassEvent m_PassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    [SerializeField]
    Vector2Int textureSize = new Vector2Int(1080, 720);

    OutputTexturePass m_ScriptablePass;
    static RTHandle depthHandle;
    static RTHandle colorHandle;


    public static RenderTexture DepthTexture => depthHandle?.rt;
    public static RenderTexture ColorTexture => colorHandle?.rt;


    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new OutputTexturePass();
        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = m_PassEvent;

        if (depthHandle == null)
        {
            depthHandle = RTHandles.Alloc(
                width: textureSize.x,
                height: textureSize.y,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                name: "_OutputDepthRT"
            );
        }

        if (colorHandle == null)
        {
            colorHandle = RTHandles.Alloc(
                width: textureSize.x,
                height: textureSize.y,
                colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                name: "_OutputColorRT"
            );
        }
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Setup the correct data for the render pass, and transfers the data from the renderer feature to the render pass.
        m_ScriptablePass.Setup(depthHandle, colorHandle);
        renderer.EnqueuePass(m_ScriptablePass);
    }


    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // ✅ Release your handle to avoid memory leak or double-init warning
        RTHandles.Release(depthHandle);
        RTHandles.Release(colorHandle);

        depthHandle = null;
        colorHandle = null;
    }
}


