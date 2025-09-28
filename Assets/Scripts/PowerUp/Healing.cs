using UnityEngine;

public class Healing : PowerUp
{
    GameObject player;
    PlayerHealth playerHealth;

    [Header("UI / Audio")]
    public AudioSource healingAudio; // gán clip nếu có
    public string itemName = "Hồi máu";
    public float showDuration = 2.5f;

    PickupMessageManager messageManager;

    public override void Awake()
    {
        // Tìm Player và PlayerHealth
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        // Tìm manager hiển thị (nếu bạn không gán thủ công)
        messageManager = PickupMessageManager.Instance;
    }

    public override void OnTriggerEnter(Collider other)
    {
        float dist = Vector3.Distance(other.transform.position, transform.position);
        if ((other.tag == "Player") && (dist < 1.5f))
        {
            // effect
            if (playerHealth != null)
                playerHealth.Healing();

            // play sound bằng PlayClipAtPoint
            if (healingAudio != null && healingAudio.clip != null)
                AudioSource.PlayClipAtPoint(healingAudio.clip, transform.position);

            // show pickup toast
            if (messageManager != null)
            {
                messageManager.SpawnAbovePlayer(other.transform.position, 2.5f, "HP: +40", Color.green, 2.5f);
            }

            this.gameObject.SetActive(false);
        }
    }
}
