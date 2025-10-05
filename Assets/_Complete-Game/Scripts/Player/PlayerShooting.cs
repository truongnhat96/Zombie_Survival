using UnityEngine;
using UnitySampleAssets.CrossPlatformInput;

namespace CompleteProject
{
    public class PlayerShooting : MonoBehaviour
    {
        public int damagePerShot = 20;                  // The damage inflicted by each bullet.
        public float timeBetweenBullets = 0.15f;        // The time between each shot.
        public float range = 100f;                      // The distance the gun can fire.


        float timer;                                    // A timer to determine when to fire.
        Ray shootRay = new Ray();                       // A ray from the gun end forwards.
        RaycastHit shootHit;                            // A raycast hit to get information about what was hit.
        int shootableMask;                              // A layer mask so the raycast only hits things on the shootable layer.
        ParticleSystem gunParticles;                    // Reference to the particle system.
        LineRenderer gunLine;                           // Reference to the line renderer.
        AudioSource gunAudio;                           // Reference to the audio source.
        Light gunLight;                                 // Reference to the light component.
		public Light faceLight;								// Duh
        float effectsDisplayTime = 0.2f;                // The proportion of the timeBetweenBullets that the effects will display for.


        void Awake ()
        {
            // Create a layer mask for the Shootable layer.
            shootableMask = LayerMask.GetMask ("Shootable");

            // Set up the references.
            gunParticles = GetComponent<ParticleSystem> ();
            gunLine = GetComponent <LineRenderer> ();
            gunAudio = GetComponent<AudioSource> ();
            gunLight = GetComponent<Light> ();
			//faceLight = GetComponentInChildren<Light> ();
        }


        void Update ()
        {
            // Add the time since Update was last called to the timer.
            timer += Time.deltaTime;

#if !MOBILE_INPUT
            // If the Fire1 button is being press and it's time to fire...
			if(Input.GetButton ("Fire1") && timer >= timeBetweenBullets && Time.timeScale != 0)
            {
                // ... shoot the gun.
                Shoot ();
            }
#else
            // If there is input on the shoot direction stick and it's time to fire...
            if ((CrossPlatformInputManager.GetAxisRaw("Mouse X") != 0 || CrossPlatformInputManager.GetAxisRaw("Mouse Y") != 0) && timer >= timeBetweenBullets)
            {
                // ... shoot the gun
                Shoot();
            }
#endif
            // If the timer has exceeded the proportion of timeBetweenBullets that the effects should be displayed for...
            if(timer >= timeBetweenBullets * effectsDisplayTime)
            {
                // ... disable the effects.
                DisableEffects ();
            }
        }


        public void DisableEffects ()
        {
            // Disable the line renderer and the light.
            gunLine.enabled = false;
			faceLight.enabled = false;
            gunLight.enabled = false;
        }


        void Shoot()
        {
            // Reset the timer
            timer = 0f;

            // Play gunshot sound
            if (gunAudio != null) gunAudio.Play();

            // Enable muzzle flash lights
            if (gunLight != null) gunLight.enabled = true;
            if (faceLight != null) faceLight.enabled = true;

            // Lấy thông tin buff từ PlayerBuffController
            Color bulletColor = Color.white;
            float damageMultiplier = 1f;

            if (PlayerBuffController.Instance != null)
            {
                bulletColor = PlayerBuffController.Instance.GetBulletColor();
                damageMultiplier = PlayerBuffController.Instance.GetDamageMultiplier();
            }

            // Set màu cho particle effect
            if (gunParticles != null)
            {
                var main = gunParticles.main;
                main.startColor = bulletColor;
                gunParticles.Stop();
                gunParticles.Play();
            }

            // Set màu cho line renderer (tia đạn)
            if (gunLine != null)
            {
                gunLine.enabled = true;
                gunLine.startColor = bulletColor;
                gunLine.endColor = bulletColor;
                gunLine.SetPosition(0, transform.position);
            }

            // Bắn raycast
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

                if (gunLine != null)
                    gunLine.SetPosition(1, shootHit.point);
            }
            else
            {
                if (gunLine != null)
                    gunLine.SetPosition(1, shootRay.origin + shootRay.direction * range);
            }
        }
    }
}