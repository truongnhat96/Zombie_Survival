using UnityEngine;
using UnitySampleAssets.CrossPlatformInput;

public class PlayerShooting : MonoBehaviour
{
    public int damagePerShot = 20;
    public float timeBetweenBullets = 0.15f;
    public float range = 100f;

    float timer;
    Ray shootRay = new Ray();
    RaycastHit shootHit;
    int shootableMask;
    ParticleSystem gunParticles;
    LineRenderer gunLine;
    AudioSource gunAudio;
    Light gunLight;
    public Light faceLight;
    float effectsDisplayTime = 0.2f;

    void Awake()
    {
        shootableMask = LayerMask.GetMask("Shootable");
        gunParticles = GetComponent<ParticleSystem>();
        gunLine = GetComponent<LineRenderer>();
        gunAudio = GetComponent<AudioSource>();
        gunLight = GetComponent<Light>();
    }

    void Update()
    {
        timer += Time.deltaTime;

#if !MOBILE_INPUT
        if (Input.GetButton("Fire1") && timer >= timeBetweenBullets && Time.timeScale != 0)
        {
            Shoot();
        }
#else
        if ((CrossPlatformInputManager.GetAxisRaw("Mouse X") != 0 || CrossPlatformInputManager.GetAxisRaw("Mouse Y") != 0)
            && timer >= timeBetweenBullets)
        {
            Shoot();
        }
#endif

        if (timer >= timeBetweenBullets * effectsDisplayTime)
        {
            DisableEffects();
        }
    }

    public void DisableEffects()
    {
        if (gunLine != null) gunLine.enabled = false;
        if (faceLight != null) faceLight.enabled = false;
        if (gunLight != null) gunLight.enabled = false;
    }

    public void Shoot()
    {
        timer = 0f;

        if (gunAudio != null) gunAudio.Play();
        if (gunLight != null) gunLight.enabled = true;
        if (faceLight != null) faceLight.enabled = true;

        // Lấy info buff
        Color bulletColor = new Color(1f, 0.843f, 0f); // mặc định vàng
        float damageMultiplier = 1f;
        if (PlayerBuffController.Instance != null)
        {
            bulletColor = PlayerBuffController.Instance.GetBulletColor();
            damageMultiplier = PlayerBuffController.Instance.GetDamageMultiplier();
        }

        // Particle (muzzle)
        if (gunParticles != null)
        {
            var main = gunParticles.main;
            main.startColor = bulletColor;
            gunParticles.Stop();
            gunParticles.Play();
        }

        // LineRenderer: instance material + gradient (đảm bảo đổi màu toàn bộ đường)
        if (gunLine != null)
        {
            gunLine.enabled = true;

            // Instance material nếu cần (tránh thay material shared)
            if (gunLine.material != null)
            {
                gunLine.material = new Material(gunLine.material);
                if (gunLine.material.HasProperty("_Color"))
                    gunLine.material.SetColor("_Color", bulletColor);
                else if (gunLine.material.HasProperty("_BaseColor"))
                    gunLine.material.SetColor("_BaseColor", bulletColor);
            }

            // set color gradient
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(bulletColor, 0f),
                    new GradientColorKey(bulletColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            gunLine.colorGradient = grad;
            gunLine.SetPosition(0, transform.position);
        }

        // Raycast
        shootRay.origin = transform.position;
        shootRay.direction = transform.forward;

        if (Physics.Raycast(shootRay, out shootHit, range, shootableMask))
        {
            EnemyHealth enemyHealth = shootHit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                int finalDamage = Mathf.RoundToInt(damagePerShot * damageMultiplier);
                enemyHealth.TakeDamage(finalDamage, shootHit.point);
            }
            if (gunLine != null) gunLine.SetPosition(1, shootHit.point);
        }
        else
        {
            if (gunLine != null) gunLine.SetPosition(1, shootRay.origin + shootRay.direction * range);
        }
    }
}
