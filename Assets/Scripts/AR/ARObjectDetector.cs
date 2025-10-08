using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using UnityEngine.Networking;

[RequireComponent(typeof(ARCameraManager))]
public class ARObjectDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private string modelFileName = "yolo11n_float32.tflite"; // KEEP NANO! Medium is too slow for real-time AR
    [SerializeField] private float detectionInterval = 0.2f; // 5 FPS detection (0.1 = 10 FPS, 0.033 = 30 FPS)
    [SerializeField] private float confidenceThreshold = 0.25f;
    
    [Header("Performance Settings")]
    private bool enableGPU = true; // GPU acceleration - Re-enabled! The slowdown was from using MEDIUM model, not delegates
    [SerializeField] private int numThreads = 4; // CPU threads (reset to 4, GPU will handle most work)

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool autoDetectThreads = false; // Automatically calculate optimal threads based on CPU cores

    // Components
    private ARCameraManager arCameraManager;
    private AndroidNativeDetector nativeDetector;

    // State
    private bool isDetecting = false;
    private string modelPath;
    private DetectionResult lastResult;

    // Events
    public System.Action<DetectionResult> OnDetectionComplete;

    void Start()
    {
        // Auto-detect optimal thread count if enabled
        if (autoDetectThreads)
        {
            numThreads = CalculateOptimalThreads();
            Debug.Log($"[AR Object Detector] Auto-detected optimal threads: {numThreads} (Device has {SystemInfo.processorCount} cores)");
        }
        else
        {
            Debug.Log($"[AR Object Detector] Using manual thread setting: {numThreads} (Device has {SystemInfo.processorCount} cores)");
        }
        
        SetupComponents();
        StartCoroutine(InitializeDetector());
    }
    
    private int CalculateOptimalThreads()
    {
        int coreCount = SystemInfo.processorCount;
        
        // Rule of thumb: Use 50-75% of available cores
        // Reserve cores for Android OS, Unity, AR tracking
        
        if (coreCount <= 4)
            return 2;  // Budget phones: use half
        else if (coreCount <= 6)
            return 4;  // Mid-range: use 4 cores
        else
            return Mathf.Min(6, coreCount - 2);  // High-end: leave 2 for system
    }

    void OnEnable()
    {
        if (arCameraManager != null)
        {
            arCameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    void OnDisable()
    {
        if (arCameraManager != null)
        {
            arCameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }

    private void SetupComponents()
    {
        // Get AR Camera Manager
        arCameraManager = GetComponent<ARCameraManager>();
        if (arCameraManager == null)
        {
            Debug.LogError("[AR Object Detector] ARCameraManager not found!");
            return;
        }

        // Add native detector component
        nativeDetector = gameObject.AddComponent<AndroidNativeDetector>();

        DebugLog("AR Object Detector initialized");
    }

    private IEnumerator InitializeDetector()
    {
        Debug.Log("[AR Object Detector] === INITIALIZATION START ===");
        
        // Wait a frame to ensure everything is set up
        yield return null;

        // Copy model to persistent data path
        Debug.Log("[AR Object Detector] Step 1: Copying model to persistent data...");
        yield return StartCoroutine(CopyModelToPersistentData());
        Debug.Log("[AR Object Detector] Step 1: Complete");

        // Verify model exists
        if (!System.IO.File.Exists(modelPath))
        {
            Debug.LogError($"[AR Object Detector] ✗ CRITICAL: Model file does not exist at {modelPath}");
            Debug.LogError($"[AR Object Detector] ✗ Cannot proceed with initialization!");
            yield break;
        }
        
        Debug.Log($"[AR Object Detector] ✓ Model file verified at {modelPath}");

        // Initialize detector
        Debug.Log("[AR Object Detector] Step 2: Initializing native detector...");
        Debug.Log($"[AR Object Detector] Performance: GPU={enableGPU}, Threads={numThreads}");
        if (nativeDetector != null)
        {
            bool success = nativeDetector.InitializeDetector(modelPath, enableGPU, numThreads);
            if (success)
            {
                Debug.Log("[AR Object Detector] ✓ Detector initialized successfully!");
                Debug.Log($"[AR Object Detector] Step 3: Setting confidence threshold to {confidenceThreshold}");
                nativeDetector.SetConfidenceThreshold(confidenceThreshold);
                Debug.Log($"[AR Object Detector] Step 4: Starting detection loop (interval: {detectionInterval}s)");
                StartCoroutine(DetectionLoop());
                Debug.Log("[AR Object Detector] === INITIALIZATION COMPLETE ===");
            }
            else
            {
                Debug.LogError("[AR Object Detector] ✗ Failed to initialize detector!");
                Debug.LogError($"[AR Object Detector] ✗ Model path was: {modelPath}");
                Debug.LogError("[AR Object Detector] ✗ Check native library and TensorFlow Lite installation!");
            }
        }
        else
        {
            Debug.LogError("[AR Object Detector] ✗ Native detector component is null!");
        }
    }

    private IEnumerator CopyModelToPersistentData()
    {
        string sourcePath = System.IO.Path.Combine(Application.streamingAssetsPath, modelFileName);
        modelPath = System.IO.Path.Combine(Application.persistentDataPath, modelFileName);

        Debug.Log($"[AR Object Detector] Source path: {sourcePath}");
        Debug.Log($"[AR Object Detector] Destination path: {modelPath}");

        if (!System.IO.File.Exists(modelPath))
        {
            Debug.Log($"[AR Object Detector] Model not found in persistent data, copying now...");
            Debug.Log($"[AR Object Detector] Copying model from {sourcePath} to {modelPath}");
            
            // Use UnityWebRequest instead of deprecated WWW
            using (UnityWebRequest www = UnityWebRequest.Get(sourcePath))
            {
                var operation = www.SendWebRequest();
                yield return operation;
                
                Debug.Log($"[AR Object Detector] Download result: {www.result}");
                
                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[AR Object Detector] Download successful, data size: {www.downloadHandler.data.Length} bytes");
                    
                    try
                    {
                        System.IO.File.WriteAllBytes(modelPath, www.downloadHandler.data);
                        Debug.Log($"[AR Object Detector] ✓ Model copied successfully to {modelPath}");
                        Debug.Log($"[AR Object Detector] ✓ Model file size: {new System.IO.FileInfo(modelPath).Length} bytes");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[AR Object Detector] ✗ Failed to write model file: {ex.Message}");
                        Debug.LogError($"[AR Object Detector] Exception: {ex}");
                    }
                }
                else
                {
                    Debug.LogError($"[AR Object Detector] ✗ Failed to copy model: {www.error}");
                    Debug.LogError($"[AR Object Detector] Response Code: {www.responseCode}");
                    Debug.LogError($"[AR Object Detector] Make sure {modelFileName} exists in Assets/StreamingAssets/");
                }
            }
        }
        else
        {
            Debug.Log($"[AR Object Detector] ✓ Model already exists in persistent data");
            Debug.Log($"[AR Object Detector] ✓ Model file size: {new System.IO.FileInfo(modelPath).Length} bytes");
        }
    }

    private IEnumerator DetectionLoop()
    {
        DebugLog("Detection loop started");
        while (true)
        {
            yield return new WaitForSeconds(detectionInterval);
            
            if (!isDetecting && arCameraManager != null && arCameraManager.enabled)
            {
                TryPerformDetection();
            }
            else if (isDetecting)
            {
                // Already processing
            }
            else if (arCameraManager == null)
            {
                Debug.LogError("[AR Object Detector] ARCameraManager is null in detection loop!");
            }
            else if (!arCameraManager.enabled)
            {
                Debug.LogWarning("[AR Object Detector] ARCameraManager is disabled!");
            }
        }
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        // This is called every frame when camera data is available
        // We don't perform detection here to avoid blocking the main thread
    }

    private void TryPerformDetection()
    {
        if (isDetecting)
        {
            return;
        }
        
        if (nativeDetector == null)
        {
            Debug.LogError("[AR Object Detector] Native detector is null!");
            return;
        }
        
        if (!nativeDetector.IsInitialized)
        {
            // Only log once every 5 seconds to avoid spam
            if (Time.frameCount % 300 == 0) // Every ~5 seconds at 60fps
            {
                Debug.LogWarning("[AR Object Detector] Detector not initialized yet, waiting...");
            }
            return;
        }

        // Try to get the latest camera image
        if (!arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            // Only log occasionally to avoid spam
            if (showDebugLogs && Time.frameCount % 300 == 0)
            {
                Debug.LogWarning("[AR Object Detector] Could not acquire camera image - camera may not be ready yet");
            }
            return;
        }

        if (showDebugLogs && Time.frameCount % 30 == 0) // Log every 30 frames
        {
            DebugLog($"Starting detection... (Image: {image.width}x{image.height})");
        }
        StartCoroutine(PerformDetectionCoroutine(image));
    }

    private IEnumerator PerformDetectionCoroutine(XRCpuImage image)
    {
        isDetecting = true;
        
        // Track total frame processing time
        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

        byte[] imageData = null;
        int width = image.width;
        int height = image.height;
        bool conversionSuccessful = false;

        // Convert XRCpuImage to RGB byte array
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, width, height),
            outputDimensions = new Vector2Int(width, height),
            outputFormat = TextureFormat.RGB24,
            transformation = XRCpuImage.Transformation.None
        };

        int size = image.GetConvertedDataSize(conversionParams);
        
        // Measure image conversion time
        var conversionStopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Use NativeArray for image conversion (no try-catch to allow yield)
        using (var rawImageData = new NativeArray<byte>(size, Allocator.Temp))
        {
            image.Convert(conversionParams, rawImageData);
            
            // Copy to managed byte array using ToArray()
            imageData = rawImageData.ToArray();
            conversionSuccessful = true;
        }
        
        conversionStopwatch.Stop();
        float conversionTimeMs = (float)conversionStopwatch.Elapsed.TotalMilliseconds;
        
        image.Dispose();
        
        if (showDebugLogs)
        {
            DebugLog($"⏱️ IMAGE CONVERSION TIME: {conversionTimeMs:F1}ms ({width}x{height} → {imageData?.Length ?? 0} bytes)");
        }

        // Yield to not block main thread (outside any try-catch)
        yield return null;

        // Perform detection if conversion was successful
        if (conversionSuccessful && imageData != null)
        {
            // Measure total inference time (includes JSON parsing)
            var inferenceStopwatch = System.Diagnostics.Stopwatch.StartNew();
            DetectionResult result = nativeDetector.DetectObjects(imageData, width, height);
            inferenceStopwatch.Stop();
            
            float totalInferenceMs = (float)inferenceStopwatch.Elapsed.TotalMilliseconds;
            
            totalStopwatch.Stop();
            float totalFrameTimeMs = (float)totalStopwatch.Elapsed.TotalMilliseconds;
            
            if (result != null)
            {
                lastResult = result;
                OnDetectionComplete?.Invoke(result);
                
                // Calculate breakdown
                float otherTimeMs = totalFrameTimeMs - conversionTimeMs - totalInferenceMs;
                
                if (showDebugLogs)
                {
                    DebugLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    DebugLog($"⏱️ FRAME TIMING BREAKDOWN:");
                    DebugLog($"  • Image Conversion: {conversionTimeMs,6:F1}ms ({conversionTimeMs/totalFrameTimeMs*100:F1}%)");
                    DebugLog($"  • Inference (Total): {totalInferenceMs,6:F1}ms ({totalInferenceMs/totalFrameTimeMs*100:F1}%)");
                    DebugLog($"  • Other (overhead):  {otherTimeMs,6:F1}ms ({otherTimeMs/totalFrameTimeMs*100:F1}%)");
                    DebugLog($"  ═══════════════════════════════════════");
                    DebugLog($"  • TOTAL FRAME TIME:  {totalFrameTimeMs,6:F1}ms");
                    DebugLog($"  • MAX POSSIBLE FPS:  {1000f/totalFrameTimeMs,6:F1}");
                    DebugLog($"  • GPU ENABLED:       {enableGPU}");
                    DebugLog($"  • CPU THREADS:       {numThreads}");
                    DebugLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    
                    if (result.totalDetections > 0)
                    {
                        DebugLog($"✓ Detected {result.totalDetections} objects:");
                        foreach (var detection in result.detections)
                        {
                            DebugLog($"  - {detection.className}: {detection.confidence:P} at ({detection.x:F2}, {detection.y:F2})");
                        }
                    }
                    else
                    {
                        DebugLog($"No objects detected in this frame");
                    }
                }
            }
        }
        else
        {
            totalStopwatch.Stop();
        }

        isDetecting = false;
    }

    public DetectionResult GetLastResult()
    {
        return lastResult;
    }

    public void SetConfidenceThreshold(float threshold)
    {
        confidenceThreshold = threshold;
        nativeDetector?.SetConfidenceThreshold(threshold);
    }

    public void SetDetectionInterval(float interval)
    {
        detectionInterval = Mathf.Max(0.1f, interval);
    }

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[AR Object Detector] {message}");
        }
    }

    void OnDestroy()
    {
        if (arCameraManager != null)
        {
            arCameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }
}