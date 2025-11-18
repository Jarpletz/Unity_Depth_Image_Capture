using UnityEngine;
using System.IO;

public class CameraMovement : MonoBehaviour
{

    [SerializeField] TextAsset csvFile;

    void Start()
    {
        if (csvFile != null)
        {
            string[] lines = csvFile.text.Split('\n');

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue; // Skip empty lines

                string[] values = line.Split(',');

                // Process your data here
                // Example: Debug.Log($"Name: {values[0]}, Score: {values[1]}");
            }
        }
        else
        {
            Debug.LogError("CSV file not assigned!");
        }
    }


}
