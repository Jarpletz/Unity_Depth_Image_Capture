using UnityEngine;

public class UniversalSettings : MonoBehaviour
{
    public static UniversalSettings Instance { get; private set; }

    public Vector2Int resolution = new Vector2Int(1920, 1080);
    public int recordingDuration = 10;

    private void Awake()
    {
        // Singleton pattern to ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
