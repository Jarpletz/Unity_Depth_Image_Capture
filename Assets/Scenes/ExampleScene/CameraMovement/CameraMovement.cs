using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

public class CameraMovement : MonoBehaviour
{

    [Serializable]
    struct TransformFrame
    {
        public float playbackTime;
        public Quaternion rotation;
        public Vector3 position;
    };

    [SerializeField] TextAsset csvFile;
    [SerializeField] List<TransformFrame> transformFrames = new List<TransformFrame>();

    float playbackTime = 0f;

    int currentFame = 0;

    void Start()
    {
        //parse CSV file
        if (csvFile != null)
        {
            string[] lines = csvFile.text.Split('\n');

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue; // Skip empty lines

                string[] values = lines[i].Split(',');

                TransformFrame frame = new TransformFrame();
                frame.playbackTime = float.Parse(values[1]);

                frame.rotation.x = float.Parse(values[2]);
                frame.rotation.y = float.Parse(values[3]);
                frame.rotation.z = float.Parse(values[4]);
                frame.rotation.w = float.Parse(values[5]);

                frame.position.x = float.Parse(values[6]);
                frame.position.y = float.Parse(values[7]);
                frame.position.z = float.Parse(values[8]);

                transformFrames.Add(frame);
            }
        }
        else
        {
            Debug.LogError("CSV file not assigned!");
        }

        // Sort by playback time 
        transformFrames = transformFrames.OrderBy(f => f.playbackTime).ToList();

        //set position to first frame 
        if (transformFrames.Count > 0)
        {
            ApplyFrame(transformFrames[0]);
        }
    }

    void Update()
    {
        playbackTime += Time.deltaTime;

        UpdateTransform();
    }

    void UpdateTransform()
    {
        // If we've advanced past the next frame, move forward
        while (currentFame < transformFrames.Count - 1 &&
               playbackTime > transformFrames[currentFame + 1].playbackTime)
        {
            currentFame++;
        }

        // If we're on the last frame now, stop interpolating
        if (currentFame >= transformFrames.Count - 1)
        {
            ApplyFrame(transformFrames[currentFame]);
            return;
        }

        TransformFrame a = transformFrames[currentFame];
        TransformFrame b = transformFrames[currentFame + 1];

        // Normalized t between current frame and next
        float t = Mathf.InverseLerp(a.playbackTime, b.playbackTime, playbackTime);

        // Local interpolation
        transform.localPosition = Vector3.Lerp(a.position, b.position, t);
        transform.localRotation = Quaternion.Slerp(a.rotation, b.rotation, t);
    }

    void ApplyFrame(TransformFrame f)
    {
        transform.localPosition = f.position;
        transform.localRotation = f.rotation;
    }


}
