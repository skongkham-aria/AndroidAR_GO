# AR Object Detection Scripts

## Core Scripts (Production Ready)

### 🔧 Core Detection System

#### `AndroidNativeDetector.cs`
**Purpose:** Bridge between Unity C# and native Android library (.aar)
- Initializes TensorFlow Lite detector
- Loads YOLO model from persistent storage
- Calls native methods for object detection
- Handles JSON deserialization of detection results
- Manages COCO class names (80 classes)

**Key Methods:**
- `InitializeDetector(string modelPath)` - Initialize with model file
- `DetectObjects(byte[] imageData, int width, int height)` - Run detection
- `SetConfidenceThreshold(float threshold)` - Adjust detection sensitivity

---

#### `ARObjectDetector.cs`
**Purpose:** Main AR detection coordinator
- Integrates with ARFoundation's ARCameraManager
- Manages detection loop and timing
- Converts ARCamera frames to byte arrays
- Copies model from StreamingAssets to persistent storage
- Raises events with detection results

**Key Features:**
- Runs detection at configurable intervals (default: 5 FPS)
- Non-blocking coroutine-based detection
- Automatic model file management
- Performance-optimized frame processing

**Inspector Settings:**
- `modelFileName` - TensorFlow Lite model (default: yolo11n_float32.tflite)
- `detectionInterval` - Time between detections (0.2s = 5 FPS)
- `confidenceThreshold` - Minimum confidence for detections (0.25)
- `showDebugLogs` - Enable/disable debug logging

**Events:**
- `OnDetectionComplete(DetectionResult)` - Called when detection finishes

---

#### `DetectionResult.cs`
**Purpose:** Data structures for detection results
- `DetectedObject` - Individual object detection with bounding box
- `DetectionResult` - Complete detection result with all objects
- `PerformanceInfo` - Timing and performance metrics

**Data Structure:**
```csharp
DetectionResult {
    DetectedObject[] detections;
    int totalDetections;
    PerformanceInfo performance;
}

DetectedObject {
    int classId;
    string className;
    float confidence;
    float x, y, width, height; // Normalized 0-1
}
```

---

### 📱 Android Support

#### `AndroidCameraPermission.cs`
**Purpose:** Runtime camera permission handling for Android
- Automatically requests camera permission on app start
- Continuously checks until permission granted
- Required for AR camera access on Android 6.0+

**Usage:** Attach to ARSession or Main Camera GameObject

---

### 🎨 User Interface

#### `SimpleDetectionUI.cs`
**Purpose:** Simple UI to display detection results
- Shows detection count
- Lists detected objects with confidence scores
- Displays performance metrics (FPS, inference time)
- Adjustable confidence threshold slider

**Inspector Setup:**
- `detectionCountText` - TextMeshProUGUI for count
- `detectionListText` - TextMeshProUGUI for object list
- `performanceText` - TextMeshProUGUI for performance info
- `confidenceSlider` - Slider for threshold adjustment

---

## Removed Scripts (Test/Diagnostic)

The following scripts were **removed** as they were only for testing/diagnostics:

- ❌ `CompilationTest.cs` - Compilation verification (no longer needed)
- ❌ `NativeLibraryDiagnostic.cs` - Native library testing
- ❌ `NativeDetectorTest.cs` - Detector testing
- ❌ `ARSetupDiagnostic.cs` - AR setup verification
- ❌ `DetectionDebugLogger.cs` - Real-time debug display

---

## Script Dependencies

```
AndroidNativeDetector.cs
    ↓
ARObjectDetector.cs
    ↓
SimpleDetectionUI.cs

DetectionResult.cs (used by all)
AndroidCameraPermission.cs (independent)
```

---

## Setup Instructions

### 1. Scene Setup
1. Add `ARSession` GameObject
2. Add Main Camera with:
   - `ARCameraManager` component
   - `ARCameraBackground` component
   - `ARObjectDetector` component
   - `AndroidCameraPermission` component (Android only)

### 2. Model Setup
1. Place `yolo11n_float32.tflite` in `Assets/StreamingAssets/`
2. Model will be automatically copied to persistent storage on first run

### 3. UI Setup (Optional)
1. Add Canvas with:
   - TextMeshProUGUI for detection count
   - TextMeshProUGUI for object list
   - TextMeshProUGUI for performance
   - Slider for confidence threshold
2. Attach `SimpleDetectionUI` to Canvas
3. Connect UI references in Inspector

### 4. Native Library
1. Ensure `FastNativeDetect-debug.aar` is in `Assets/Plugins/Android/`
2. Verify Android platform is enabled in .aar Inspector settings

---

## Events & Callbacks

### Subscribe to Detection Events

```csharp
void Start()
{
    ARObjectDetector detector = Camera.main.GetComponent<ARObjectDetector>();
    detector.OnDetectionComplete += HandleDetection;
}

void HandleDetection(DetectionResult result)
{
    foreach (var obj in result.detections)
    {
        Debug.Log($"Detected: {obj.className} ({obj.confidence:P})");
    }
}
```

---

## Performance Notes

- **Detection Frequency:** 5 FPS (configurable via `detectionInterval`)
- **Camera Capture:** 30-60 FPS (ARFoundation)
- **Typical Inference Time:** 50-200ms depending on device
- **Memory:** ~200MB for YOLO11n model

---

## Troubleshooting

### No Detections
1. Check camera permission granted
2. Verify model file copied (check logs)
3. Adjust confidence threshold (try 0.5 for more sensitive detection)
4. Ensure good lighting conditions

### Native Library Errors
1. Verify .aar file in `Assets/Plugins/Android/`
2. Check .aar has Android platform enabled in Inspector
3. Clean and rebuild project (delete Library/ and Temp/)

### Performance Issues
1. Increase `detectionInterval` (e.g., 0.5s for 2 FPS)
2. Use smaller image resolution if possible
3. Check device CPU/thermal throttling

---

## COCO Classes (80 Total)

Person, bicycle, car, motorcycle, airplane, bus, train, truck, boat, traffic light, fire hydrant, stop sign, parking meter, bench, bird, cat, dog, horse, sheep, cow, elephant, bear, zebra, giraffe, backpack, umbrella, handbag, tie, suitcase, frisbee, skis, snowboard, sports ball, kite, baseball bat, baseball glove, skateboard, surfboard, tennis racket, bottle, wine glass, cup, fork, knife, spoon, bowl, banana, apple, sandwich, orange, broccoli, carrot, hot dog, pizza, donut, cake, chair, couch, potted plant, bed, dining table, toilet, tv, laptop, mouse, remote, keyboard, cell phone, microwave, oven, toaster, sink, refrigerator, book, clock, vase, scissors, teddy bear, hair drier, toothbrush

---

## License & Credits

- YOLO11 by Ultralytics
- ARFoundation by Unity Technologies
- TensorFlow Lite by Google
