using System;
using UnityEngine;

public class AndroidNativeDetector : MonoBehaviour
{
    private AndroidJavaObject nativeLib;
    private bool isInitialized = false;
    private float currentConfidenceThreshold = 0.25f;

    // YOLO class names (COCO dataset - 80 classes)
    private readonly string[] classNames = {
        "person", "bicycle", "car", "motorcycle", "airplane", "bus", "train", "truck", "boat",
        "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat",
        "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack",
        "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball",
        "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket",
        "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple",
        "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake",
        "chair", "couch", "potted plant", "bed", "dining table", "toilet", "tv", "laptop",
        "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink",
        "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush"
    };

    public bool IsInitialized => isInitialized;

    void Start()
    {
        InitializeNativeLib();
    }

    private void InitializeNativeLib()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("========================================");
        Debug.Log("[AR Detection] Starting native library initialization...");
        Debug.Log("========================================");
        
        bool success = false;
        
        // Try to create instance directly (most common pattern)
        try
        {
            Debug.Log("[AR Detection] Method 1: Trying direct instantiation of com.example.fastnativedetect.NativeLib");
            nativeLib = new AndroidJavaObject("com.example.fastnativedetect.NativeLib");
            
            if (nativeLib != null)
            {
                // Verify it's the correct class - use Call on the object, not GetRawClass
                AndroidJavaObject classObj = nativeLib.Call<AndroidJavaObject>("getClass");
                string className = classObj.Call<string>("getName");
                Debug.Log($"[AR Detection] ✓ Object created successfully!");
                Debug.Log($"[AR Detection] ✓ Class type: {className}");
                
                if (className == "com.example.fastnativedetect.NativeLib")
                {
                    Debug.Log($"[AR Detection] ✓✓✓ Correct class type verified!");
                    success = true;
                }
                else
                {
                    Debug.LogError($"[AR Detection] ✗ Wrong class type!");
                    Debug.LogError($"[AR Detection] Expected: com.example.fastnativedetect.NativeLib");
                    Debug.LogError($"[AR Detection] Got: {className}");
                }
            }
        }
        catch (Exception ex1)
        {
            Debug.LogError($"[AR Detection] ✗ Direct instantiation failed!");
            Debug.LogError($"[AR Detection] Error: {ex1.Message}");
            Debug.LogError($"[AR Detection] Full exception: {ex1}");
            
            // Try getInstance() pattern as fallback
            try
            {
                Debug.Log("[AR Detection] Method 2: Trying getInstance() pattern...");
                AndroidJavaClass nativeLibClass = new AndroidJavaClass("com.example.fastnativedetect.NativeLib");
                nativeLib = nativeLibClass.CallStatic<AndroidJavaObject>("getInstance");
                
                if (nativeLib != null)
                {
                    AndroidJavaObject classObj = nativeLib.Call<AndroidJavaObject>("getClass");
                    string className = classObj.Call<string>("getName");
                    Debug.Log($"[AR Detection] ✓ Object created via getInstance!");
                    Debug.Log($"[AR Detection] ✓ Class type: {className}");
                    success = true;
                }
            }
            catch (Exception ex2)
            {
                Debug.LogError($"[AR Detection] ✗ getInstance() also failed!");
                Debug.LogError($"[AR Detection] Error: {ex2.Message}");
                Debug.LogError($"[AR Detection] Full exception: {ex2}");
            }
        }
        
        if (!success || nativeLib == null)
        {
            Debug.LogError("========================================");
            Debug.LogError("[AR Detection] ✗✗✗ CRITICAL ERROR ✗✗✗");
            Debug.LogError("[AR Detection] Native library NOT loaded!");
            Debug.LogError("========================================");
            Debug.LogError("[AR Detection] TROUBLESHOOTING:");
            Debug.LogError("[AR Detection] 1. The .aar file is NOT in the APK");
            Debug.LogError("[AR Detection] 2. Check Unity → Assets/Plugins/Android/FastNativeDetect-debug.aar");
            Debug.LogError("[AR Detection] 3. Select the .aar, verify Inspector shows:");
            Debug.LogError("[AR Detection]    - Platform: Android (CHECKED)");
            Debug.LogError("[AR Detection]    - CPU: ARMv7, ARM64");
            Debug.LogError("[AR Detection] 4. Delete Library/ and Temp/ folders");
            Debug.LogError("[AR Detection] 5. Reopen Unity and rebuild");
            Debug.LogError("========================================");
            nativeLib = null;
        }
        else
        {
            Debug.Log("========================================");
            Debug.Log("[AR Detection] ✓✓✓ SUCCESS ✓✓✓");
            Debug.Log("[AR Detection] Native library ready!");
            Debug.Log("========================================");
        }
        
        #else
        Debug.LogWarning("[AR Detection] Running in editor mode - native detection disabled");
        isInitialized = true; // For editor testing
        #endif
    }

    public bool InitializeDetector(string modelPath, bool enableGPU = true, int numThreads = 4)
    {
        Debug.Log("========================================");
        Debug.Log("[AR Detection] InitializeDetector called");
        Debug.Log($"[AR Detection] Model path: {modelPath}");
        Debug.Log($"[AR Detection] GPU Acceleration: {(enableGPU ? "ENABLED" : "DISABLED")}");
        Debug.Log($"[AR Detection] CPU Threads: {numThreads}");
        Debug.Log("========================================");
        
        if (nativeLib == null && !Application.isEditor)
        {
            Debug.LogError("========================================");
            Debug.LogError("[AR Detection] ✗ CRITICAL: nativeLib is NULL!");
            Debug.LogError("========================================");
            Debug.LogError("[AR Detection] The native library failed to load in Start()");
            Debug.LogError("[AR Detection] This means the .aar is NOT in your APK!");
            Debug.LogError("[AR Detection] ");
            Debug.LogError("[AR Detection] REQUIRED ACTIONS:");
            Debug.LogError("[AR Detection] 1. Close Unity");
            Debug.LogError("[AR Detection] 2. Delete these folders:");
            Debug.LogError("[AR Detection]    - Library/");
            Debug.LogError("[AR Detection]    - Temp/");
            Debug.LogError("[AR Detection] 3. Reopen Unity");
            Debug.LogError("[AR Detection] 4. Select FastNativeDetect-debug.aar in Project");
            Debug.LogError("[AR Detection] 5. Verify Inspector shows 'Android' is CHECKED");
            Debug.LogError("[AR Detection] 6. Build → Build and Run");
            Debug.LogError("========================================");
            return false;
        }

        try
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log($"[AR Detection] Verifying native library object type...");
            
            // Check if we have a valid object - use Call on the object, not GetRawClass
            AndroidJavaObject classObj = nativeLib.Call<AndroidJavaObject>("getClass");
            string className = classObj.Call<string>("getName");
            Debug.Log($"[AR Detection] Native library class name: {className}");
            
            if (className != "com.example.fastnativedetect.NativeLib")
            {
                Debug.LogError("========================================");
                Debug.LogError("[AR Detection] ✗ WRONG CLASS TYPE!");
                Debug.LogError("========================================");
                Debug.LogError($"[AR Detection] Expected: com.example.fastnativedetect.NativeLib");
                Debug.LogError($"[AR Detection] Got: {className}");
                Debug.LogError("[AR Detection] ");
                Debug.LogError("[AR Detection] This means the .aar was NOT loaded!");
                Debug.LogError("========================================");
                return false;
            }
            
            Debug.Log($"[AR Detection] ✓ Correct class type verified");
            Debug.Log($"[AR Detection] Checking model file...");
            Debug.Log($"[AR Detection] Model path: {modelPath}");
            Debug.Log($"[AR Detection] Model file exists: {System.IO.File.Exists(modelPath)}");
            
            if (!System.IO.File.Exists(modelPath))
            {
                Debug.LogError($"[AR Detection] ✗ Model file not found at: {modelPath}");
                return false;
            }
            
            Debug.Log($"[AR Detection] ✓ Model file verified");
            
            // Double-check the object is still valid before calling method
            Debug.Log($"[AR Detection] Verifying nativeLib object is still valid...");
            Debug.Log($"[AR Detection] nativeLib == null? {(nativeLib == null)}");
            
            if (nativeLib == null)
            {
                Debug.LogError($"[AR Detection] ✗ nativeLib became null!");
                return false;
            }
            
            // Re-verify class type right before calling the method
            AndroidJavaObject classObj2 = nativeLib.Call<AndroidJavaObject>("getClass");
            string className2 = classObj2.Call<string>("getName");
            Debug.Log($"[AR Detection] Re-verified class type: {className2}");
            
            // Try to get method information
            try
            {
                Debug.Log($"[AR Detection] Checking if method 'initializeDetector' exists...");
                AndroidJavaObject[] methods = classObj2.Call<AndroidJavaObject[]>("getMethods");
                Debug.Log($"[AR Detection] Found {methods.Length} methods in class");
                
                bool foundMethod = false;
                foreach (var method in methods)
                {
                    string methodName = method.Call<string>("getName");
                    if (methodName == "initializeDetector")
                    {
                        Debug.Log($"[AR Detection] ✓ Found method: {methodName}");
                        foundMethod = true;
                        break;
                    }
                }
                
                if (!foundMethod)
                {
                    Debug.LogError($"[AR Detection] ✗ Method 'initializeDetector' NOT FOUND in class!");
                    Debug.LogError($"[AR Detection] The .aar may not have the correct implementation");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AR Detection] Could not check methods: {ex.Message}");
            }
            
            Debug.Log($"[AR Detection] Calling nativeLib.Call<bool>(\"initializeDetector\", \"{modelPath}\", {enableGPU}, {numThreads})...");
            Debug.Log($"[AR Detection] Parameters: modelPath={modelPath}, enableGPU={enableGPU}, numThreads={numThreads}");
            
            // Kotlin method signature: fun initializeDetector(modelPath: String, enableGPU: Boolean = false, numThreads: Int = 4)
            // Must pass all 3 parameters when calling from Unity (Kotlin default params don't work via JNI)
            bool success = nativeLib.Call<bool>("initializeDetector", modelPath, enableGPU, numThreads);
            isInitialized = success;
            
            if (success)
            {
                Debug.Log("========================================");
                Debug.Log("[AR Detection] ✓✓✓ DETECTOR INITIALIZED! ✓✓✓");
                Debug.Log("========================================");
                Debug.Log($"[AR Detection] Model: {modelPath}");
                SetConfidenceThreshold(0.25f);
            }
            else
            {
                Debug.LogError("========================================");
                Debug.LogError("[AR Detection] ✗ Native method returned FALSE");
                Debug.LogError("========================================");
                Debug.LogError("[AR Detection] TensorFlow Lite failed to load model");
                Debug.LogError("[AR Detection] Possible causes:");
                Debug.LogError("[AR Detection]   1. Model file corrupted");
                Debug.LogError("[AR Detection]   2. Incompatible model format");
                Debug.LogError("[AR Detection]   3. TFLite library missing from .aar");
                Debug.LogError("========================================");
            }
            
            return success;
            #else
            Debug.Log("[AR Detection] Editor mode - returning mock success");
            isInitialized = true;
            return true;
            #endif
        }
        catch (Exception e)
        {
            Debug.LogError("========================================");
            Debug.LogError("[AR Detection] ✗ EXCEPTION during initialization!");
            Debug.LogError("========================================");
            Debug.LogError($"[AR Detection] Exception type: {e.GetType().Name}");
            Debug.LogError($"[AR Detection] Message: {e.Message}");
            Debug.LogError($"[AR Detection] ");
            Debug.LogError($"[AR Detection] Full exception:");
            Debug.LogError($"{e}");
            Debug.LogError("========================================");
            Debug.LogError("[AR Detection] This usually means:");
            Debug.LogError("  1. .aar file NOT in APK (most common)");
            Debug.LogError("  2. Method 'initializeDetector' doesn't exist in Java class");
            Debug.LogError("  3. Method signature mismatch");
            Debug.LogError("========================================");
            return false;
        }
    }

    public DetectionResult DetectObjects(byte[] imageData, int width, int height)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[AR Detection] Detector not initialized");
            return CreateEmptyResult();
        }

        try
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log($"[AR Detection] DetectObjects called - Image: {width}x{height}, Data size: {imageData.Length} bytes");
            
            // Measure native inference time
            var inferenceStopwatch = System.Diagnostics.Stopwatch.StartNew();
            string jsonResult = nativeLib.Call<string>("getDetailedDetections", imageData, width, height);
            inferenceStopwatch.Stop();
            
            float nativeInferenceMs = (float)inferenceStopwatch.Elapsed.TotalMilliseconds;
            Debug.Log($"[AR Detection] ⏱️ NATIVE INFERENCE TIME: {nativeInferenceMs:F1}ms");
            
            Debug.Log($"[AR Detection] JSON result length: {(jsonResult != null ? jsonResult.Length : 0)} chars");
            
            if (string.IsNullOrEmpty(jsonResult))
            {
                Debug.LogWarning("[AR Detection] Empty JSON result returned from native detector");
                return CreateEmptyResult();
            }

            // Log first 500 chars and last 500 chars to see structure
            if (jsonResult.Length > 1000)
            {
                Debug.Log($"[AR Detection] JSON start: {jsonResult.Substring(0, 500)}");
                Debug.Log($"[AR Detection] JSON end: {jsonResult.Substring(jsonResult.Length - 500)}");
            }
            else
            {
                Debug.Log($"[AR Detection] JSON result: {jsonResult}");
            }
            
            DetectionResult result = JsonUtility.FromJson<DetectionResult>(jsonResult);
            
            if (result == null)
            {
                Debug.LogWarning("[AR Detection] Failed to parse JSON result");
                return CreateEmptyResult();
            }
            
            Debug.Log($"[AR Detection] Parsed {result.totalDetections} detections");
            
            // Add class names to detections
            if (result.detections != null)
            {
                Debug.Log($"[AR Detection] Mapping {result.detections.Length} detections to class names...");
                for (int i = 0; i < result.detections.Length; i++)
                {
                    var detection = result.detections[i];
                    Debug.Log($"[AR Detection] Detection {i}: classId={detection.class_id}, confidence={detection.confidence:F3}, bbox=({detection.x:F3},{detection.y:F3},{detection.width:F3},{detection.height:F3})");
                    
                    if (detection.class_id >= 0 && detection.class_id < classNames.Length)
                    {
                        detection.className = classNames[detection.class_id];
                        Debug.Log($"[AR Detection] Mapped classId {detection.class_id} to '{detection.className}'");
                    }
                    else
                    {
                        detection.className = $"Unknown({detection.class_id})";
                        Debug.LogWarning($"[AR Detection] Invalid classId {detection.class_id} (valid range: 0-{classNames.Length - 1})");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[AR Detection] result.detections array is null!");
            }
            
            if (result.totalDetections > 0)
            {
                Debug.Log($"[AR Detection] ✓ Detected {result.totalDetections} objects!");
            }
            
            return result;
            #else
            // Editor mode - return mock data for testing
            return CreateMockResult();
            #endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[AR Detection] Exception during object detection: {e.Message}");
            return CreateEmptyResult();
        }
    }

    public void SetConfidenceThreshold(float threshold)
    {
        currentConfidenceThreshold = threshold;
        
        if (nativeLib == null) return;

        try
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            nativeLib.Call("setConfidenceThresholdNative", threshold);
            Debug.Log($"[AR Detection] Confidence threshold set to {threshold}");
            #endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[AR Detection] Exception setting confidence threshold: {e.Message}");
        }
    }

    private DetectionResult CreateEmptyResult()
    {
        return new DetectionResult 
        { 
            detections = new DetectedObject[0], 
            total_detections = 0,
            confidence_threshold = currentConfidenceThreshold
        };
    }

    private DetectionResult CreateMockResult()
    {
        // Create mock detection for editor testing
        return new DetectionResult 
        { 
            detections = new DetectedObject[] 
            { 
                new DetectedObject 
                { 
                    class_id = 0,
                    className = "person", 
                    confidence = 0.85f,
                    bbox = new BBox
                    {
                        x = 0.5f,
                        y = 0.5f,
                        width = 0.2f,
                        height = 0.3f
                    }
                } 
            },
            total_detections = 1,
            confidence_threshold = 0.25f,
            image_size = new ImageSize { width = 640, height = 480 },
            model_input_size = new ImageSize { width = 640, height = 640 }
        };
    }

    void OnDestroy()
    {
        if (nativeLib != null)
        {
            try
            {
                #if UNITY_ANDROID && !UNITY_EDITOR
                nativeLib.Call("cleanup");
                #endif
            }
            catch (Exception e)
            {
                Debug.LogError($"[AR Detection] Exception during cleanup: {e.Message}");
            }
            finally
            {
                nativeLib?.Dispose();
                nativeLib = null;
            }
        }
    }
}