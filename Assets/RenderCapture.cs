using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections;
using System.Text;
using NUnit.Compatibility;

public class RenderCapture : MonoBehaviour
{

    struct CameraData
    {
        public Vector3 position;
        public Quaternion rotation;
        public int frameNumber;
        public float playbackTime;
    }
    struct FrameData
    {
        public byte[] colorRaw;
        public byte[] depthRaw;
        public int width;
        public int height;
        public int frameNum;
    }

    [Tooltip("How long, in seconds, frame data will be captured.")]
    [SerializeField] float renderTimeSeconds = 10f;
    [SerializeField] GameObject doneCapturingRendersText;
    [SerializeField] GameObject doneSavingPNGsText;


    [Tooltip("How many frames to process (encode+write) per Update to spread cost.")]
    [SerializeField] int savesPerFrame = 2;

    ConcurrentQueue<FrameData> saveQueue = new ConcurrentQueue<FrameData>();
    List<CameraData> cameraData = new List<CameraData>();
    bool capturing = true;
    bool hasOpenedDir = false;
    bool hasSavedCamData = false;


    int frameCount = 0;
    float timer = 0f;

    GameObject cameraObj;

    void Awake()
    {
        Time.timeScale = UniversalSettings.Instance.timeScale;

        Camera.main.depthTextureMode = DepthTextureMode.Depth;

        cameraObj = Camera.main.gameObject;
    }


    void Update()
    {
        if (capturing)
        {

            timer += Time.deltaTime;
            if (timer >= renderTimeSeconds)
            {
                if (doneCapturingRendersText) doneCapturingRendersText.SetActive(true);
                capturing = false;
                Debug.Log("Stopped capturing.");
            }
        }
        else
        {
            if (saveQueue.Count() < 1)
            {
                if (doneSavingPNGsText) doneSavingPNGsText.SetActive(true);
            }

            if (!hasSavedCamData)
            {
                SaveCameraDataToCSV();
                hasSavedCamData = true;
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

        if (!OutputTexturesFeature.ColorTexture || !OutputTexturesFeature.DepthTexture)
        {
            Debug.LogWarning("Textures not found. Be sure to add the OutputTexturesFeature to your Render Features list in your URP asset.");
            return;
        }

        // get the camera position/orientation at time of capture
        var newCamData = new CameraData
        {
            position = cameraObj.transform.position,
            rotation = cameraObj.transform.rotation,
            frameNumber = frameCount,
            playbackTime = timer
        };
        cameraData.Add(newCamData);


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
                    frameNum = frameCount++,
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

    void SaveCameraDataToCSV()
    {
        StringBuilder sb = new StringBuilder();

        string dir = Path.Combine(Application.dataPath, "../Output");
        string csvPath = Path.Combine(dir, "camera_data.csv");

        // Header
        sb.AppendLine("frameNumber,playbackTime,posX,posY,posZ,rotX,rotY,rotZ,rotW");

        foreach (var d in cameraData)
        {
            sb.Append(d.frameNumber).Append(",");
            sb.Append(d.playbackTime).Append(",");

            sb.Append(d.position.x).Append(",");
            sb.Append(d.position.y).Append(",");
            sb.Append(d.position.z).Append(",");

            sb.Append(d.rotation.x).Append(",");
            sb.Append(d.rotation.y).Append(",");
            sb.Append(d.rotation.z).Append(",");
            sb.Append(d.rotation.w);

            sb.AppendLine();
        }

        File.WriteAllText(csvPath, sb.ToString());
    }
}
