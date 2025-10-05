using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Robust PowerUpItem: nhiều fallback detection để đảm bảo player có thể nhặt item.
/// Giữ nguyên tên file/class PowerUpItem (kế thừa PowerUp trong project của bạn).
/// Bật debugLogs để xem console chi tiết.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PowerUpItem : PowerUp
{
    [Header("Spawn / Buff")]
    public bool randomizeOnSpawn = true;
    public float[] multipliers = new float[] { 1.1f, 1.2f, 1.5f };
    public Color[] colors = new Color[] { Color.green, Color.blue, new Color(0.6f, 0f, 1f) };

    [Header("Pickup behaviour")]
    public float pickupDistance = 1.2f;        // distance from player's root to pick
    public float spawnDisableDuration = 0.12f; // disable collider right after spawn to avoid instant pickup
    public bool debugLogs = true;
    public float buffDuration = 10f;

    [Header("Optional SFX/UI")]
    public AudioClip pickupClip;
    public GameObject pickupToastPrefab;
    public float toastDuration = 2.5f;

    // --- blink (hiệu ứng cho tier cao nhất) ---
    List<Material> _blinkMaterials = new List<Material>();
    Coroutine _blinkCoroutine = null;
    public float blinkSpeed = 7f;        // tốc độ nhấp
    public float emissionMin = 0.35f;    // emission tối thiểu
    public float emissionMax = 2.2f;     // emission tối đa

    // internals
    int chosenIndex = 0;
    float chosenMultiplier = 1.1f;
    Color chosenColor = Color.green;

    Collider myCollider;
    Renderer[] allRenderers;
    ParticleSystem[] allParticles;
    bool picked = false;

    PickupMessageManager messageManager;

    // Awake override (PowerUp abstract requires this signature)
    public override void Awake()
    {
        messageManager = PickupMessageManager.Instance;

        // choose random buff
        if (randomizeOnSpawn && multipliers != null && multipliers.Length > 0)
        {
            chosenIndex = Random.Range(0, multipliers.Length);
            chosenMultiplier = multipliers[chosenIndex];
            chosenColor = (chosenIndex < colors.Length) ? colors[chosenIndex] : Color.white;
        }
        else
        {
            chosenIndex = 0;
            chosenMultiplier = (multipliers != null && multipliers.Length > 0) ? multipliers[0] : 1.1f;
            chosenColor = (colors != null && colors.Length > 0) ? colors[0] : Color.green;
        }

        // cache renderers and particles (including children)
        allRenderers = GetComponentsInChildren<Renderer>(true);
        allParticles = GetComponentsInChildren<ParticleSystem>(true);

        // color everything robustly
        ApplyColorToAllRenderers(chosenColor);
        ApplyColorToAllParticles(chosenColor);

        // --- start blink nếu đây là tier cao nhất (ví dụ multiplier == max) ---
        float maxMul = (multipliers != null && multipliers.Length > 0) ? Mathf.Max(multipliers) : chosenMultiplier;
        if (Mathf.Approximately(chosenMultiplier, maxMul))
        {
            _blinkMaterials.Clear();

            if (allRenderers != null)
            {
                foreach (var r in allRenderers)
                {
                    if (r == null) continue;
                    // lấy tất cả material slots của renderer (đã là material instances từ ApplyColor)
                    foreach (var mat in r.materials)
                    {
                        if (mat == null) continue;
                        // bật emission nếu có
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", chosenColor * emissionMin);
                        }
                        else if (mat.HasProperty("_BaseColor"))
                        {
                            // set base color darker (fallback)
                            mat.SetColor("_BaseColor", chosenColor * 0.9f);
                        }
                        // thêm vào danh sách để xử lý nhấp
                        _blinkMaterials.Add(mat);
                    }
                }
            }

            if (_blinkMaterials.Count > 0)
                _blinkCoroutine = StartCoroutine(BlinkEmissionRoutine());
        }


        // collider & rb
        myCollider = GetComponent<Collider>();
        if (myCollider == null) myCollider = gameObject.AddComponent<SphereCollider>();
        myCollider.isTrigger = true;

        if (!TryGetComponent<Rigidbody>(out var rb)) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // temporarily disable collider to avoid instant pickup on spawn overlap
        if (spawnDisableDuration > 0f)
        {
            myCollider.enabled = false;
            Invoke(nameof(EnableColliderSafely), spawnDisableDuration);
            if (debugLogs) Debug.Log($"[PowerUpItem] collider disabled for {spawnDisableDuration}s after spawn");
        }

        if (debugLogs)
            Debug.Log($"[PowerUpItem] Awake: chosenIndex={chosenIndex}, mul={chosenMultiplier}, color={chosenColor}");
    }

    void EnableColliderSafely()
    {
        if (myCollider != null)
        {
            myCollider.enabled = true;
            if (debugLogs) Debug.Log("[PowerUpItem] collider enabled");
        }
    }

    // OnTriggerEnter is first attempt
    public override void OnTriggerEnter(Collider other)
    {
        if (picked) return;
        if (debugLogs) Debug.Log($"[PowerUpItem] OnTriggerEnter by {other.name} (tag={other.tag})");

        TryPickupFromCollider(other);
    }

    // OnTriggerStay: fallback when Enter might not fire (e.g., collider enabled after overlap)
    void OnTriggerStay(Collider other)
    {
        if (picked) return;
        // minimal throttle? not needed; quick immediate pickup is ok
        TryPickupFromCollider(other);
    }

    // Helper: attempt to identify player root from the incoming collider and pick if conditions ok
    void TryPickupFromCollider(Collider other)
    {
        if (picked) return;

        // 1) Quick: is there a PlayerBuffController in parents of 'other' ?
        PlayerBuffController pb = other.GetComponentInParent<PlayerBuffController>();
        GameObject playerRoot = null;
        if (pb != null)
        {
            playerRoot = pb.gameObject;
            if (debugLogs) Debug.Log($"[PowerUpItem] Found PlayerBuffController in parents: root={playerRoot.name}");
        }

        // 2) If not found, check attachedRigidbody -> its gameObject might be player root
        if (playerRoot == null && other.attachedRigidbody != null)
        {
            var rbGO = other.attachedRigidbody.gameObject;
            if (rbGO != null && rbGO.GetComponent<PlayerBuffController>() != null)
            {
                playerRoot = rbGO;
                pb = playerRoot.GetComponent<PlayerBuffController>();
                if (debugLogs) Debug.Log($"[PowerUpItem] Found player via attachedRigidbody: {playerRoot.name}");
            }
        }

        // 3) If still not found, check other.transform.root (and its components)
        if (playerRoot == null)
        {
            var root = other.transform.root;
            if (root != null && root.GetComponent<PlayerBuffController>() != null)
            {
                playerRoot = root.gameObject;
                pb = playerRoot.GetComponent<PlayerBuffController>();
                if (debugLogs) Debug.Log($"[PowerUpItem] Found player via transform.root: {playerRoot.name}");
            }
        }

        // 4) If still not found, check tag "Player" on other or parent chain
        if (playerRoot == null)
        {
            Transform t = other.transform;
            while (t != null)
            {
                if (t.CompareTag("Player"))
                {
                    playerRoot = t.gameObject;
                    pb = playerRoot.GetComponent<PlayerBuffController>();
                    if (debugLogs) Debug.Log($"[PowerUpItem] Found player via tag on {t.name}");
                    break;
                }
                t = t.parent;
            }
        }

        // If still not a player, bail out
        if (playerRoot == null || pb == null)
        {
            if (debugLogs) Debug.Log($"[PowerUpItem] TryPickup: no player root/pb found for collider {other.name}.");
            return;
        }

        // Distance check between player's root position and this item
        float dist = Vector3.Distance(playerRoot.transform.position, transform.position);
        if (debugLogs) Debug.Log($"[PowerUpItem] distance to player root = {dist:F3} (pickupDistance={pickupDistance})");

        if (dist <= pickupDistance)
        {
            // apply buff
            ApplyBuffToPlayer(playerRoot, pb);
        }
        else
        {
            if (debugLogs) Debug.Log("[PowerUpItem] Player root too far to pick (ignored).");
        }
    }

    void ApplyBuffToPlayer(GameObject playerRoot, PlayerBuffController pb)
    {
        if (picked) return;
        picked = true;

        // prefer ApplyBuff(level) if available; otherwise ApplyDamageBuff(mult,color)
        int level = Mathf.Clamp(chosenIndex + 1, 1, multipliers.Length);

        bool applied = false;
        if (pb != null)
        {
            try
            {
                pb.ApplyBuff(level, buffDuration);
                applied = true;
                messageManager.SpawnAbovePlayer(playerRoot.transform.position, 2.5f, $"Damage: +{(chosenMultiplier - 1) * 100}%", chosenColor, 2.5f);
                if (debugLogs) Debug.Log($"[PowerUpItem] Applied pb.ApplyBuff(level {level}) - color {chosenColor}");
            }
            catch { }
        }

        if (!applied)
        {
            // try singleton
            if (PlayerBuffController.Instance != null)
            {
                try
                {
                    PlayerBuffController.Instance.ApplyBuff(level, buffDuration);
                    applied = true;
                    if (debugLogs) Debug.Log($"[PowerUpItem] Applied Instance.ApplyBuff(level {level})");
                }
                catch
                {
                    // fallback to ApplyDamageBuff
                    PlayerBuffController.Instance.ApplyDamageBuff(chosenMultiplier, chosenColor);
                    applied = true;
                    if (debugLogs) Debug.Log($"[PowerUpItem] Fallback Instance.ApplyDamageBuff x{chosenMultiplier}");
                }
            }
        }

        // SFX/toast
        if (pickupClip != null) AudioSource.PlayClipAtPoint(pickupClip, transform.position);
        if (pickupToastPrefab != null)
        {
            var t = Instantiate(pickupToastPrefab, playerRoot.transform.position + Vector3.up * 1.6f, Quaternion.identity);
            Destroy(t, toastDuration);
        }

        // disable like SpeedUp does
        gameObject.SetActive(false);
        if (debugLogs) Debug.Log("[PowerUpItem] picked & disabled.");
    }

    // Robustly color all renderers and particles
    void ApplyColorToAllRenderers(Color color)
    {
        if (allRenderers == null || allRenderers.Length == 0) allRenderers = GetComponentsInChildren<Renderer>(true);
        if (allRenderers == null) return;

        foreach (var r in allRenderers)
        {
            if (r == null) continue;
            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                Material m = new Material(mats[i]);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
                if (m.HasProperty("_Color")) m.SetColor("_Color", color);
                if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", color);
                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", color * 0.35f);
                }
                m.color = color;
                mats[i] = m;
            }
            r.materials = mats;
        }
    }

    void ApplyColorToAllParticles(Color color)
    {
        if (allParticles == null || allParticles.Length == 0) allParticles = GetComponentsInChildren<ParticleSystem>(true);
        if (allParticles == null) return;
        foreach (var p in allParticles)
        {
            if (p == null) continue;
            var main = p.main;
            main.startColor = color;
        }
    }

    IEnumerator BlinkEmissionRoutine()
    {
        while (true)
        {
            float t = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f; // 0..1
            float factor = Mathf.Lerp(emissionMin, emissionMax, t);

            for (int i = 0; i < _blinkMaterials.Count; i++)
            {
                var m = _blinkMaterials[i];
                if (m == null) continue;

                if (m.HasProperty("_EmissionColor"))
                {
                    m.SetColor("_EmissionColor", chosenColor * factor);
                }
                else if (m.HasProperty("_BaseColor"))
                {
                    // fallback: pulse base color brightness slightly
                    m.SetColor("_BaseColor", Color.Lerp(chosenColor * 0.6f, chosenColor * 1.15f, t));
                }
                else
                {
                    // final fallback: tint material.color
                    m.color = Color.Lerp(chosenColor * 0.6f, chosenColor * 1.15f, t);
                }
            }

            yield return null;
        }
    }

}
