using UnityEngine;

public class DamagePowerUp : PowerUp
{
    [Tooltip("Các mức tăng sát thương: 1.1 = +10%, 1.2 = +20%, 1.5 = +50%")]
    public float[] multipliers = new float[] { 1.1f, 1.2f, 1.5f };

    [Tooltip("Màu tương ứng để đổi màu đạn (index tương ứng với multipliers)")]
    public Color[] colors = new Color[] {
        new Color(1f, 0.9f, 0f),   // vàng nhạt (10%)
        new Color(1f, 0.6f, 0f),   // cam (20%)
        new Color(1f, 0.27f, 0f)   // đỏ-cam (50%)
    };

    public AudioClip pickupSound; // (tùy chọn)

    // Bắt buộc override theo base PowerUp
    public override void Awake()
    {
        // (nếu cần init visual)
    }

    public override void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // chọn random 1 trong 3 mức
        int idx = Random.Range(0, multipliers.Length);
        float mul = multipliers[idx];
        Color col = (idx < colors.Length) ? colors[idx] : Color.white;

        // lấy component PlayerBuffController trên player
        PlayerBuffController buff = other.GetComponent<PlayerBuffController>();
        if (buff == null)
            buff = other.GetComponentInChildren<PlayerBuffController>();

        if (buff != null)
        {
            buff.ApplyDamageBuff(mul, col);
        }

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        // tiêu hủy item sau khi nhặt
        Destroy(gameObject);
    }
}
