using System;
using UnityEngine;

[Serializable]
public class BBox
{
    public float x;
    public float y;
    public float width;
    public float height;
}

[Serializable]
public class DetectedObject
{
    // JSON fields (snake_case to match native detector output)
    public int class_id;
    public float confidence;
    public BBox bbox;
    
    // Runtime fields (set after parsing)
    [NonSerialized]
    public string className;
    
    // Convenience properties for backward compatibility and easier access
    public int classId
    {
        get => class_id;
        set => class_id = value;
    }
    
    public float x => bbox?.x ?? 0;
    public float y => bbox?.y ?? 0;
    public float width => bbox?.width ?? 0;
    public float height => bbox?.height ?? 0;
}

[Serializable]
public class ImageSize
{
    public int width;
    public int height;
}

[Serializable]
public class DetectionResult
{
    public DetectedObject[] detections;
    public int total_detections;  // Snake case to match JSON
    public float confidence_threshold;  // Snake case to match JSON
    public ImageSize image_size;  // Snake case to match JSON
    public ImageSize model_input_size;  // Snake case to match JSON
    
    // Note: performance info is NOT in the JSON from native detector
    // It would need to be added in Kotlin if needed
    [NonSerialized]
    public PerformanceInfo performance;
    
    // Convenience property for backward compatibility
    public int totalDetections => total_detections;
    public float confidenceThreshold => confidence_threshold;
    public Vector2 imageSize => image_size != null ? new Vector2(image_size.width, image_size.height) : Vector2.zero;
    public Vector2 modelInputSize => model_input_size != null ? new Vector2(model_input_size.width, model_input_size.height) : Vector2.zero;
}

[Serializable]
public class PerformanceInfo
{
    public float inferenceTimeMs;
    public float preprocessingTimeMs;
    public float postprocessingTimeMs;
    public int memoryUsageMB;
}