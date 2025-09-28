using UnityEngine;

public class SpeedUp : PowerUp
{
    GameObject player;
    PlayerMovement playerMovement;

    [Header("UI / Audio")]
    public AudioSource speedUpAudio; // có thể gán clip (optional)
    public string itemName = "Tăng Tốc"; // tên hiện lên thông báo
    public float showDuration = 2.5f; // thời gian toast hiển thị

    PickupMessageManager messageManager;

    public override void Awake()
    {
        // Tìm Player và PlayerMovement
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerMovement = player.GetComponent<PlayerMovement>();

        // Tìm manager hiển thị (nếu bạn không gán thủ công)
        messageManager = PickupMessageManager.Instance;
    }

    public override void OnTriggerEnter(Collider other)
    {
        float dist = Vector3.Distance(other.transform.position, transform.position);
        if ((other.tag == "Player") && (dist < 1.5f))
        {
            // effect
            if (playerMovement != null)
                playerMovement.SpeedUp();

            // play sound (dùng PlayClipAtPoint để âm thanh vẫn nghe dù object bị deactivate)
            if (speedUpAudio != null && speedUpAudio.clip != null)
                AudioSource.PlayClipAtPoint(speedUpAudio.clip, transform.position);

            // show pickup toast
            if (messageManager != null)
            {
                messageManager.SpawnAbovePlayer(other.transform.position, 2.5f, "Speed: +50%", Color.yellow, 2.5f);
            }

            // disable object (hoặc Destroy nếu muốn)
            this.gameObject.SetActive(false);
        }
    }
}
