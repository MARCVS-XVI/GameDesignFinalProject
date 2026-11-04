using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public static bool debugLogs = true;

    private RectTransform fillRect;
    private Image fillImage;
    private Camera cam;
    private bool warnedNoCamera = false;

    // log a message if debug logs are enabled
    private static void Log(string message, Object context = null)
    {
        if (debugLogs)
            Debug.Log("[HealthBar] " + message, context);
    }

    // create a new health bar
    public static HealthBar Create(Transform parent, Vector3 localOffset, Vector2 worldSize)
    {
        if (parent == null)
        {
            Debug.LogError("[HealthBar] Create failed: parent is null.");
            return null;    
        }

        if (worldSize.x <= 0f || worldSize.y <= 0f)
        {
            Debug.LogError("[HealthBar] Create failed: worldSize must be above 0, got " + worldSize, parent);
            return null;
        }

        // create the health bar
        GameObject go = new GameObject("HealthBar");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localOffset;

        // set up the canvas
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // set up the health bar dimensions
        float scale = worldSize.x / 100f;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, worldSize.y / scale);
        go.transform.localScale = Vector3.one * scale;

        // create the background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.7f);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        // create the fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(go.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

        // initialize the health bar
        HealthBar bar = go.AddComponent<HealthBar>();
        bar.fillRect = fillRt;
        bar.fillImage = fillImg;
        bar.cam = Camera.main;

        // warn if no camera is found
        if (bar.cam == null)
            Debug.LogWarning("[HealthBar] Camera.main is null. Tag your camera 'MainCamera' or the bar won't face it.", go);

        // log creation details
        Log("Created under '" + parent.name + "' | local offset " + localOffset +
            " | world size " + worldSize + " | canvas scale " + scale.ToString("F4") +
            " | parent lossyScale " + parent.lossyScale, go);

        // warn if the parent is very small
        if (parent.lossyScale.x < 0.1f)
            Debug.LogWarning("[HealthBar] Parent '" + parent.name + "' is very small (scale " + parent.lossyScale + "). The bar will shrink with it and may be invisible.", go);

        // initialize the fill
        bar.SetFill(1f);
        return bar;
    }

    // sets the fill amount for the health bar
    public void SetFill(float percent)
    {
        // validate the fill references
        if (fillRect == null || fillImage == null)
        {
            Debug.LogError("[HealthBar] SetFill called but the fill references are missing.", this);
            return;
        }

        // check for NaN
        if (float.IsNaN(percent))
        {
            Debug.LogError("[HealthBar] SetFill received NaN. Check that maxHealth is not 0.", this);
            return;
        }

        // clamp the fill amount
        float clamped = Mathf.Clamp01(percent);
        if (!Mathf.Approximately(clamped, percent))
            Log("SetFill value " + percent.ToString("F2") + " was out of range and was clamped to " + clamped.ToString("F2"), this);

        fillRect.anchorMax = new Vector2(clamped, 1f);
        fillImage.color = Color.Lerp(Color.red, Color.green, clamped);

        // log the fill amount
        Log("Fill set to " + Mathf.RoundToInt(clamped * 100f) + "%", this);
    }

    // updates the health bar's orientation each frame
    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;

        // face the camera
        if (cam == null)
        {
            if (!warnedNoCamera)
            {
                Debug.LogWarning("[HealthBar] No camera found, so the bar can't face it.", this);
                warnedNoCamera = true;
            }
            return;
        }

        // rotate the bar to face the camera
        transform.rotation = cam.transform.rotation;
    }

    // cleans up the health bar when it's destroyed
    void OnDestroy()
    {
        Log("Destroyed with its parent (normal when the owner dies)");
    }
}