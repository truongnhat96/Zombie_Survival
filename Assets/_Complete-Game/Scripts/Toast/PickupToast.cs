using System.Collections;
using UnityEngine;
using TMPro;

public class PickupToast : MonoBehaviour
{
    [Header("Refs")]
    public TextMeshProUGUI text;
    public CanvasGroup canvasGroup;


    [Header("Auto-resize padding (pixels)")]
    public float horizontalPadding = 24f; // tổng padding trái + phải
    public float verticalPadding = 24f;   // tổng padding trên + dưới

    [Header("Timing / Animation")]
    public float duration = 2.5f;
    public float fadeTime = 0.25f;
    public float slideDistance = 30f;

    RectTransform rect;
    Coroutine routine;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (text == null) text = GetComponentInChildren<TextMeshProUGUI>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    // backward-compatible: giữ Init(content) — dùng current text.color nếu gọi
    public void Init(string content)
    {
        Color c = (text != null) ? text.color : Color.white;
        Init(content, c);
    }

    // New: Init with color
    public void Init(string content, Color color)
    {
        if (text != null)
        {
            text.color = color;         // set màu chữ ở đây
            text.text = content;
        }

        // --- AUTO RESIZE NỀN THEO NỘI DUNG TEXT ---
        if (text != null && rect != null)
        {
            Vector2 pref = text.GetPreferredValues(content, 9999f, 9999f);
            float newWidth = pref.x + horizontalPadding;
            float newHeight = pref.y + verticalPadding;
            rect.sizeDelta = new Vector2(newWidth, newHeight);
        }
        // ------------------------------------------------

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        canvasGroup.alpha = 0f;
        Vector2 startPos = rect.anchoredPosition;
        rect.anchoredPosition = startPos - new Vector2(0, slideDistance * 0.5f);

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeTime);
            canvasGroup.alpha = p;
            rect.anchoredPosition = Vector2.Lerp(startPos - new Vector2(0, slideDistance * 0.5f),
                                                 startPos + new Vector2(0, slideDistance * 0.25f), p);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        rect.anchoredPosition = startPos + new Vector2(0, slideDistance * 0.25f);

        yield return new WaitForSecondsRealtime(duration);

        t = 0f;
        Vector2 begin = rect.anchoredPosition;
        Vector2 end = startPos + new Vector2(0, slideDistance);
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeTime);
            canvasGroup.alpha = 1f - p;
            rect.anchoredPosition = Vector2.Lerp(begin, end, p);
            yield return null;
        }

        Destroy(gameObject);
    }

    public void CancelNow()
    {
        if (routine != null) StopCoroutine(routine);
        Destroy(gameObject);
    }
}
