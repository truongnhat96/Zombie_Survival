using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PlayerBuffController: quản lý buff tăng sát thương và thời hạn của nó.
/// - ApplyBuff(level, duration): áp buff theo level; duration > 0 => timed buff; duration <= 0 => persistent until cleared (by hit).
/// - ApplyDamageBuff(multiplier, color, duration) tương tự (dùng khi PowerUpItem gọi trực tiếp multiplier).
/// - ClearDamageBuff(): xóa buff ngay (ví dụ khi player bị trúng).
/// </summary>
public class PlayerBuffController : MonoBehaviour
{
    public static PlayerBuffController Instance { get; private set; }

    [Header("Default visuals")]
    public Color defaultBulletColor = new Color(1f, 0.843f, 0f); // vàng mặc định

    [Header("Preset buff levels (1=10%,2=20%,3=50%)")]
    public float[] presetMultipliers = new float[] { 1.1f, 1.2f, 1.5f };
    public Color[] presetColors = new Color[] {
        Color.green,              // level 1 → xanh lá (10%)
        Color.blue,               // level 2 → xanh dương (20%)
        new Color(0.6f, 0f, 1f)   // level 3 → tím (50%)
    };

    [Header("Optional HUD")]
    public Image buffIcon;
    public Text buffText;

    // Internal state
    float currentMultiplier = 1f;
    Color currentBulletColor;
    bool isBuffActive = false;

    Coroutine buffCoroutine = null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        currentMultiplier = 1f;
        currentBulletColor = defaultBulletColor;
        UpdateHUD();
    }

    /// <summary>
    /// Apply a direct multiplier + color; if duration > 0 the buff will auto-expire after duration seconds.
    /// If duration <= 0 the buff persists until ClearDamageBuff() is called (e.g., when player is hit).
    /// </summary>
    public void ApplyDamageBuff(float multiplier, Color color, float duration = -1f)
    {
        // stop any existing timed buff
        if (buffCoroutine != null)
        {
            StopCoroutine(buffCoroutine);
            buffCoroutine = null;
        }

        currentMultiplier = multiplier;
        currentBulletColor = color;
        isBuffActive = true;
        UpdateHUD();

        if (duration > 0f)
        {
            buffCoroutine = StartCoroutine(BuffTimerCoroutine(duration));
        }

        Debug.Log($"[PlayerBuffController] ApplyDamageBuff -> x{multiplier} duration={duration}");
    }

    /// <summary>
    /// Apply buff by level (1..N). duration same semantics as ApplyDamageBuff.
    /// </summary>
    public void ApplyBuff(int level, float duration = -1f)
    {
        int idx = Mathf.Clamp(level - 1, 0, presetMultipliers.Length - 1);
        float mul = (presetMultipliers != null && presetMultipliers.Length > idx) ? presetMultipliers[idx] : 1f;
        Color col = (presetColors != null && presetColors.Length > idx) ? presetColors[idx] : defaultBulletColor;
        ApplyDamageBuff(mul, col, duration);
        Debug.Log($"[PlayerBuffController] ApplyBuff(level {level}) -> x{mul} dur={duration}");
    }

    IEnumerator BuffTimerCoroutine(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            // optional: update HUD countdown if you want; currently HUD shows static multiplier.
            yield return null;
        }
        // time expired -> clear buff same as being hit
        ClearDamageBuff();
        buffCoroutine = null;
    }

    /// <summary>
    /// Clear buff and return to defaults immediately.
    /// Call this when player is hit (PlayerHealth.TakeDamage should call this).
    /// </summary>
    public void ClearDamageBuff()
    {
        if (buffCoroutine != null)
        {
            StopCoroutine(buffCoroutine);
            buffCoroutine = null;
        }

        currentMultiplier = 1f;
        currentBulletColor = defaultBulletColor;
        isBuffActive = false;
        UpdateHUD();

        Debug.Log("[PlayerBuffController] ClearDamageBuff()");
    }

    // Accessors used by PlayerShooting
    public float GetDamageMultiplier() => currentMultiplier;
    public Color GetBulletColor() => currentBulletColor;

    void UpdateHUD()
    {
        if (buffIcon != null) buffIcon.enabled = isBuffActive;
        if (buffText != null) buffText.text = isBuffActive ? $"DMG x{currentMultiplier:F2}" : "";
    }
}
