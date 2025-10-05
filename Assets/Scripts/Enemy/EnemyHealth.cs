using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public int startingHealth = 100;
    public int currentHealth;
    public float sinkSpeed = 2.5f;
    public int scoreValue = 10;
    public AudioClip deathClip;

    [Header("PowerUp Drop Settings")]
    [Range(0f, 1f)]
    public float dropChance = 0.2f; // 20% mặc định
    public GameObject powerUpPrefab; // gán prefab PowerUpItem vào đây
    public GameObject spawnEffectPrefab; // optional: hiệu ứng spawn khi rơi

    Animator anim;
    AudioSource enemyAudio;
    ParticleSystem hitParticles;
    CapsuleCollider capsuleCollider;
    bool isDead;
    bool isSinking;

    void Awake()
    {
        anim = GetComponent<Animator>();
        enemyAudio = GetComponent<AudioSource>();
        hitParticles = GetComponentInChildren<ParticleSystem>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        currentHealth = startingHealth;
    }

    void Update()
    {
        if (isSinking)
        {
            transform.Translate(-Vector3.up * sinkSpeed * Time.deltaTime);
        }
    }

    public void TakeDamage(int amount, Vector3 hitPoint)
    {
        if (isDead)
            return;

        enemyAudio.Play();
        currentHealth -= amount;
        if (hitParticles != null)
        {
            hitParticles.transform.position = hitPoint;
            hitParticles.Play();
        }

        if (currentHealth <= 0)
        {
            Death();
        }
    }

    void Death()
    {
        isDead = true;
        if (capsuleCollider != null) capsuleCollider.isTrigger = true;
        anim.SetTrigger("Dead");
        enemyAudio.clip = deathClip;
        enemyAudio.Play();

        // Spawn item *sau một khoảng nhỏ* để tránh overlap collider
        StartCoroutine(SpawnPowerUpDelayed());
    }

    IEnumerator SpawnPowerUpDelayed()
    {
        // đợi collider/quái, animation settle...
        yield return new WaitForSeconds(0.35f);

        if (powerUpPrefab == null) yield break;

        if (Random.value <= dropChance)
        {
            // spawn lên cao + random offset nhẹ để tránh nằm thẳng trên collider
            Vector3 randomOffset = new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f));
            Vector3 spawnPos = transform.position + Vector3.up * 1.0f + randomOffset;
            Quaternion spawnRot = Quaternion.identity;

            if (spawnEffectPrefab != null)
            {
                GameObject fx = Instantiate(spawnEffectPrefab, spawnPos, spawnRot);
                Destroy(fx, 2.0f);
            }

            // instantiate without parenting to enemy so it won't be destroyed with enemy
            GameObject spawned = Instantiate(powerUpPrefab, spawnPos, spawnRot);
            spawned.transform.SetParent(null);
            // optional: ensure it won't be auto-destroyed by any script (see checklist)
        }
    }

    public void StartSinking()
    {
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        isSinking = true;
        ScoreManager.score += scoreValue;
        Destroy(gameObject, 2f);
    }
}
