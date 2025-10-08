using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleDetectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI detectionCountText;
    [SerializeField] private TextMeshProUGUI detectionListText;
    [SerializeField] private TextMeshProUGUI performanceText;
    [SerializeField] private Slider confidenceSlider;

    [Header("Settings")]
    [SerializeField] private bool showPerformanceInfo = true;
    [SerializeField] private int maxDisplayedDetections = 5;

    private ARObjectDetector arDetector;

    void Start()
    {
        SetupUI();
        FindDetector();
    }

    private void SetupUI()
    {
        // Initialize UI elements
        if (detectionCountText != null)
            detectionCountText.text = "Detections: 0";

        if (detectionListText != null)
            detectionListText.text = "No objects detected";

        if (performanceText != null && !showPerformanceInfo)
            performanceText.gameObject.SetActive(false);

        // Setup confidence slider
        if (confidenceSlider != null)
        {
            confidenceSlider.minValue = 0.1f;
            confidenceSlider.maxValue = 1.0f;
            confidenceSlider.value = 0.25f;
            confidenceSlider.onValueChanged.AddListener(OnConfidenceChanged);
        }
    }

    private void FindDetector()
    {
        // Find AR Object Detector in the scene (Unity 6 compatible)
        arDetector = FindFirstObjectByType<ARObjectDetector>();
        
        if (arDetector != null)
        {
            arDetector.OnDetectionComplete += UpdateDetectionDisplay;
            Debug.Log("[Simple Detection UI] Connected to AR Object Detector");
        }
        else
        {
            Debug.LogWarning("[Simple Detection UI] ARObjectDetector not found in scene");
        }
    }

    private void UpdateDetectionDisplay(DetectionResult result)
    {
        if (result == null)
        {
            Debug.LogWarning("[Simple Detection UI] Result is null!");
            return;
        }

        Debug.Log($"[Simple Detection UI] Updating display - {result.totalDetections} detections");

        // Update detection count
        if (detectionCountText != null)
        {
            detectionCountText.text = $"Detections: {result.totalDetections}";
            Debug.Log($"[Simple Detection UI] Updated count text: {detectionCountText.text}");
        }
        else
        {
            Debug.LogWarning("[Simple Detection UI] detectionCountText is null!");
        }

        // Update detection list
        if (detectionListText != null)
        {
            UpdateDetectionList(result);
            Debug.Log($"[Simple Detection UI] Updated list text");
        }
        else
        {
            Debug.LogWarning("[Simple Detection UI] detectionListText is null!");
        }

        // Update performance info
        if (performanceText != null && showPerformanceInfo)
        {
            if (result.performance != null)
            {
                performanceText.text = $"Inference: {result.performance.inferenceTimeMs:F1}ms\n" +
                                     $"Memory: {result.performance.memoryUsageMB}MB";
                Debug.Log($"[Simple Detection UI] Updated performance text: {performanceText.text}");
            }
            else
            {
                // Performance info not available from native detector
                // Show image and model info instead
                performanceText.text = $"Image: {result.imageSize.x}x{result.imageSize.y}\n" +
                                     $"Model: {result.modelInputSize.x}x{result.modelInputSize.y}\n" +
                                     $"Threshold: {result.confidenceThreshold:F2}";
                Debug.Log($"[Simple Detection UI] Updated info text (no performance): {performanceText.text}");
            }
        }
        else
        {
            if (performanceText == null)
                Debug.LogWarning("[Simple Detection UI] performanceText is null!");
        }
    }

    private void UpdateDetectionList(DetectionResult result)
    {
        if (result.detections == null || result.detections.Length == 0)
        {
            detectionListText.text = "No objects detected";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int displayCount = Mathf.Min(result.detections.Length, maxDisplayedDetections);

        for (int i = 0; i < displayCount; i++)
        {
            var detection = result.detections[i];
            sb.AppendLine($"{detection.className}: {detection.confidence:F2}");
        }

        if (result.detections.Length > maxDisplayedDetections)
        {
            sb.AppendLine($"... and {result.detections.Length - maxDisplayedDetections} more");
        }

        detectionListText.text = sb.ToString();
    }

    public void OnConfidenceChanged(float value)
    {
        if (arDetector != null)
        {
            arDetector.SetConfidenceThreshold(value);
        }
    }

    void OnDestroy()
    {
        if (arDetector != null)
        {
            arDetector.OnDetectionComplete -= UpdateDetectionDisplay;
        }
    }
}