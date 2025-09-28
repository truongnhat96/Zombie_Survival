using UnityEngine;

public class PickupMessageManager : MonoBehaviour
{
    // Singleton Instance
    public static PickupMessageManager Instance { get; private set; }

    [Tooltip("Drag your HUD Canvas (Screen Space - Overlay or Camera) here")]
    public Canvas hudCanvas;
    [Tooltip("Drag the PickupToast prefab here")]
    public GameObject toastPrefab;
    [Tooltip("Default display duration if not provided")]
    public float defaultDuration = 2.5f;

    RectTransform uiRoot;
    GameObject currentToast;

    void Awake()
    {
        // --- Singleton setup ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // --- Original logic ---
        if (hudCanvas == null) Debug.LogError("PickupMessageManager: hudCanvas not assigned!");
        else uiRoot = hudCanvas.GetComponent<RectTransform>();
        if (toastPrefab == null) Debug.LogError("PickupMessageManager: toastPrefab not assigned!");
    }

    // --- Backwards-compatible overloads (no color) ---
    public void SpawnAtWorldPosition(Vector3 worldPos, string content, float duration = -1f, Vector2 additionalScreenOffset = default)
    {
        SpawnAtWorldPosition(worldPos, content, Color.white, duration, additionalScreenOffset);
    }

    public void SpawnAbovePlayer(Vector3 playerWorldPos, float worldYOffset, string content, float duration = -1f)
    {
        SpawnAbovePlayer(playerWorldPos, worldYOffset, content, Color.white, duration);
    }

    public void SpawnAtScreenPoint(Vector2 screenPoint, string content, float duration = -1f)
    {
        SpawnAtScreenPoint(screenPoint, content, Color.white, duration);
    }

    // --- New: overloads with Color ---
    public void SpawnAtWorldPosition(Vector3 worldPos, string content, Color color, float duration = -1f, Vector2 additionalScreenOffset = default)
    {
        if (hudCanvas == null || toastPrefab == null) return;

        Camera cam = (hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : hudCanvas.worldCamera;
        Vector3 screenPoint3 = (cam == null) ? Camera.main.WorldToScreenPoint(worldPos) : cam.WorldToScreenPoint(worldPos);
        Vector2 screenPoint = new Vector2(screenPoint3.x, screenPoint3.y) + additionalScreenOffset;

        SpawnAtScreenPoint(screenPoint, content, color, duration);
    }

    public void SpawnAbovePlayer(Vector3 playerWorldPos, float worldYOffset, string content, Color color, float duration = -1f)
    {
        SpawnAtWorldPosition(playerWorldPos + Vector3.up * worldYOffset, content, color, duration);
    }

    public void SpawnAtScreenPoint(Vector2 screenPoint, string content, Color color, float duration = -1f)
    {
        if (uiRoot == null) return;

        // cancel current
        if (currentToast != null)
        {
            var old = currentToast.GetComponent<PickupToast>();
            if (old != null) old.CancelNow(); else Destroy(currentToast);
            currentToast = null;
        }

        Camera cam = (hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : hudCanvas.worldCamera;
        Vector2 localPoint;
        bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, screenPoint, cam, out localPoint);

        GameObject go = Instantiate(toastPrefab, uiRoot);
        RectTransform tr = go.GetComponent<RectTransform>();
        if (tr == null) tr = go.AddComponent<RectTransform>();
        tr.pivot = new Vector2(0.5f, 0.5f);
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
        tr.anchoredPosition = ok ? localPoint : Vector2.zero;

        var toast = go.GetComponent<PickupToast>();
        if (toast != null)
        {
            if (duration > 0) toast.duration = duration;
            toast.Init(content, color);
        }

        currentToast = go;
    }

    public void CancelCurrentToast()
    {
        if (currentToast == null) return;
        var t = currentToast.GetComponent<PickupToast>();
        if (t != null) t.CancelNow(); else Destroy(currentToast);
        currentToast = null;
    }
}
