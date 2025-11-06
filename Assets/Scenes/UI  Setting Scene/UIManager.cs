using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UniversalSettingsUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;
    public TMP_InputField recordTimeInput;

    public Button startButton;

    private void Start()
    {
        // Ensure the singleton exists
        if (UniversalSettings.Instance == null)
        {
            Debug.LogError("UniversalSettings instance not found!");
            return;
        }

        // Load initial values into the UI
        widthInput.text = UniversalSettings.Instance.resolution.x.ToString();
        heightInput.text = UniversalSettings.Instance.resolution.y.ToString();
        recordTimeInput.text = UniversalSettings.Instance.recordingDuration.ToString();


        // Subscribe to value changes
        widthInput.onEndEdit.AddListener(OnWidthChanged);
        heightInput.onEndEdit.AddListener(OnHeightChanged);
        recordTimeInput.onEndEdit.AddListener(OnRuntimeChanged);
        startButton.onClick.AddListener(OnStart);
    }

    private void OnWidthChanged(string newValue)
    {
        if (int.TryParse(newValue, out int width))
        {
            Vector2Int res = UniversalSettings.Instance.resolution;
            UniversalSettings.Instance.resolution = new Vector2Int(width, res.y);
        }
    }

    private void OnHeightChanged(string newValue)
    {
        if (int.TryParse(newValue, out int height))
        {
            Vector2Int res = UniversalSettings.Instance.resolution;
            UniversalSettings.Instance.resolution = new Vector2Int(res.x, height);
        }
    }

    private void OnRuntimeChanged(string newValue)
    {
        if (int.TryParse(newValue, out int recordTime))
        {
            UniversalSettings.Instance.recordingDuration = recordTime;
        }
    }

    private void OnStart()
    {
        OutputTexturesFeature.generateRenderTextureHandles();
        SceneManager.LoadSceneAsync(1);
    }


}
