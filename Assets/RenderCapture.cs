using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections;

public class RenderCapture : MonoBehaviour
{
    struct FrameData
    {
        public byte[] colorRaw;
        public byte[] depthRaw;
        public int width;
        public int height;
        public int frameNum;
    }

    [SerializeField] float renderTimeSeconds = 10f;
    [SerializeField] GameObject doneCapturingRendersText;
    [SerializeField] GameObject doneSavingPNGsText;

    
    [Tooltip("How many frames to process (encode+write) per Update to spread cost.")]
    [SerializeField] int savesPerFrame = 1;

    ConcurrentQueue<FrameData> saveQueue = new ConcurrentQueue<FrameData>();    
    bool capturing = true;
    int frameCount = 0;
    bool hasOpenedDir = false;
    RenderTexture depthTexture;

    void Start()
    {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;
        depthTexture = OutputTexturesFeature.DepthTexture;
    }

    void Update()
    {
        if (capturing)
        {

            renderTimeSeconds -= Time.deltaTime;
            if (renderTimeSeconds <= 0)
            {
                if (doneCapturingRendersText) doneCapturingRendersText.SetActive(true);
                capturing = false;
                Debug.Log("Stopped capturing.");
            }
        }
        else
        {
            if(saveQueue.Count() < 1)
            {
                if (doneSavingPNGsText) doneSavingPNGsText.SetActive(true);

            }

            // Process up to savesPerFrame queued frames on main thread.
            for (int i = 0; i < savesPerFrame; ++i)
            {
                if (!saveQueue.TryDequeue(out var fd)) break;
                StartCoroutine(SaveFrameCoroutine(fd));
            }
            
        }

    }

    void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!capturing) return;

        if(!OutputTexturesFeature.ColorTexture || !OutputTexturesFeature.DepthTexture)
        {
            Debug.LogWarning("Textures not found. Be sure to add the OutputTexturesFeature to your Render Features list in your URP asset.");
            return;
        }


        // Issue async GPU readback, non-blocking
        AsyncGPUReadback.Request(OutputTexturesFeature.ColorTexture, 0, TextureFormat.RGB24, colorRequest =>
        {
            if (colorRequest.hasError)
            {
                Debug.LogWarning("Color AsyncGPUReadback error for frame " + frameCount);
                return;
            }

            // Copy data to managed array right away (this is cheap and safe).
            var colorData = colorRequest.GetData<byte>();
            byte[] colorCopy = new byte[colorData.Length];
            colorData.CopyTo(colorCopy);
          
            AsyncGPUReadback.Request(OutputTexturesFeature.DepthTexture, 0, TextureFormat.RGB24, request =>
            {
                if (request.hasError)
                {
                    Debug.LogWarning("Depth AsyncGPUReadback error for frame " + frameCount);
                    return;
                }

                // Copy data to managed array right away (this is cheap and safe).
                var depthData = request.GetData<byte>();
                byte[] depthCopy = new byte[depthData.Length];
                depthData.CopyTo(depthCopy);
            
                var fd = new FrameData
                {
                    colorRaw = colorCopy,
                    depthRaw = depthCopy,
                    width = OutputTexturesFeature.DepthTexture.width,
                    height = OutputTexturesFeature.DepthTexture.height,
                    frameNum = frameCount++
                };

                saveQueue.Enqueue(fd);
            });
        });
    }

   
    IEnumerator SaveFrameCoroutine(FrameData fd)
    {
        // This runs on the main thread (coroutine), so Unity APIs are safe here.
        // Create texture, load raw bytes, encode, write to disk.
        try
        {
            // Optionally yield a frame before heavy work to smooth spikes:
            // yield return null;

            string dir = Path.Combine(Application.dataPath, "../Output");
            Directory.CreateDirectory(dir);

            //  ---------- COLOR ------------------
            Texture2D colorTexture = new Texture2D(fd.width, fd.height, TextureFormat.RGB24, false);
            colorTexture.LoadRawTextureData(fd.colorRaw);
            colorTexture.Apply(false, false); 
            byte[] colorPng = ImageConversion.EncodeToPNG(colorTexture);
            Destroy(colorTexture);

            
            string colorPath = Path.Combine(dir, $"Color_{fd.frameNum:D05}.png");
            File.WriteAllBytes(colorPath, colorPng);
            Debug.Log($"Saved {colorPath}");

            //  ---------- DEPTH ------------------
            Texture2D depthTexture = new Texture2D(fd.width, fd.height, TextureFormat.RGB24, false);
            depthTexture.LoadRawTextureData(fd.depthRaw);
            depthTexture.Apply(false, false); 
            byte[] depthPng = ImageConversion.EncodeToPNG(depthTexture);
            Destroy(depthTexture);

            
            string depthPath = Path.Combine(dir, $"Depth_{fd.frameNum:D05}.png");
            File.WriteAllBytes(depthPath, depthPng);
            Debug.Log($"Saved {depthPath}");

            if (!hasOpenedDir)
            {
                hasOpenedDir = true;
                Application.OpenURL(dir);
            }
        }
        catch (System.Exception ex)
        {
            // Catch exceptions and log them (we're on main thread so Debug.Log works)
            Debug.LogError("Error saving frame: " + ex);
        }

        // allow other coroutines/frames to run
        yield return null;
    }
}
