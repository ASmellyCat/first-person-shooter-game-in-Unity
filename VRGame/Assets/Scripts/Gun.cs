using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;



public class Gun : MonoBehaviour {

    public GameObject gun;
    public GameObject end;
    public GameObject start;
    public Animator animator;
    public GameObject bulletHole;
    public GameObject muzzleFlash;
    public GameObject shotSound;
    public float maxHealth = 100;
    public float health;
    public Text remainingHealth;
    public bool isDead;
    public bool winStatus;
    public Text magBullets;
    public Text remainingBullets;
    public GameObject headMesh;
    public GameObject gameOverPanel;
    public GameObject victoryMessage;
    public GameObject defeatMessage;
    public GameObject enemy1;
    public GameObject enemy2;
    public GameObject enemy3;
    public GameObject door;
    public GameObject spine;
    public GameObject handMag;
    public GameObject gunMag;
    public GameObject[] ammoCrates = new GameObject[2];
    public bool[] isTaken;
    public static bool leftHanded { get; private set; }
    Quaternion previousRotation;

    private float gunShotTime = 0.1f;
    private float gunReloadTime = 1.0f;
    private int magBulletsVal = 30;
    private int remainingBulletsVal = 90;
    private int magSize = 30;
    public float damageFromPlayerFactor;
    public float openAngle = 90.0f;
    public float openSpeed = 2.0f;


    // Use this for initialization
    void Start()
    {
        headMesh.GetComponent<SkinnedMeshRenderer>().enabled = false; // Hiding player character head to avoid bugs :)
        health = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {        // Cool down times
        if (gunShotTime >= 0.0f)
        {
            gunShotTime -= Time.deltaTime;
        }
        if (gunReloadTime >= 0.0f)
        {
            gunReloadTime -= Time.deltaTime;
        }


        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && gunShotTime <= 0 && gunReloadTime <= 0.0f && magBulletsVal > 0 && !isDead && !winStatus)
        { 
            shotDetection(); // Should be completed

            //addEffects(); // Should be completed

            animator.SetBool("fire", true);
            gunShotTime = 0.5f;
            
            // Instantiating the muzzle prefab and shot sound
            
            magBulletsVal = magBulletsVal - 1;
            if (magBulletsVal <= 0 && remainingBulletsVal > 0)
            {
                animator.SetBool("reloadAfterFire", true);
                gunReloadTime = 2.5f;
                Invoke("reloaded", 2.5f);
            }
        }
        else
        {
            animator.SetBool("fire", false);
        }

        if ((Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.R)) && gunReloadTime <= 0.0f && gunShotTime <= 0.1f && remainingBulletsVal > 0 && magBulletsVal < magSize && !isDead && !winStatus)
        {
            animator.SetBool("reload", true);
            gunReloadTime = 2.5f;
            Invoke("reloaded", 2.0f);
        }
        else
        {
            animator.SetBool("reload", false);
        }

        // restart when player win
        if (isDead)
        {
            gameOverPanel.GetComponent<CanvasGroup>().alpha = 1;
            defeatMessage.GetComponent<CanvasGroup>().alpha = 1;
            animator.SetBool("dead", true);
            animator.GetComponent<CharacterMovement>().isDead = true;
            Invoke("reStart", 10f);
        }

        // Refill Ammo
        for (int i = 0; i < ammoCrates.Length; i++)
        {
                if (Vector3.Distance(ammoCrates[i].transform.position, transform.position) < 3 && !isTaken[i] && Input.GetKeyDown(KeyCode.E)) {
                    refillAmmo(i);
                }
        }
        updateText();
    }


    // Getting hit from enemy
    public void Being_shot(float damageFromEnemyFactor)
    {
        if (damageFromEnemyFactor <= 0)
        {
            Debug.Log("Error: Damage amount should be greater than 0.");
            return;
        }

        // Reduce health by the damage amount
        health -= Mathf.Round(maxHealth * damageFromEnemyFactor);

        // Check if health has dropped below zero and update the isDead status
        if (health <= 0)
        {
            isDead = true;
            health = 0;
            Debug.Log("You're dead.");
        }
        else
        {
            Debug.Log("You took damage "+ Mathf.Round(maxHealth * damageFromEnemyFactor) + "!!! Remaining health: " + Mathf.Round(health));
        }
    }


    public void ReloadEvent(int eventNumber) // appearing and disappearing the handMag and gunMag
    {
        if (eventNumber == 1)
        {
            handMag.GetComponent<SkinnedMeshRenderer>().enabled = true;
            gunMag.GetComponent<SkinnedMeshRenderer>().enabled = false;
        }
        if (eventNumber == 2)
        {
            handMag.GetComponent<SkinnedMeshRenderer>().enabled = false;
            gunMag.GetComponent<SkinnedMeshRenderer>().enabled = true;
        }
    }

    void reloaded()
    {
        int newMagBulletsVal = Mathf.Min(remainingBulletsVal + magBulletsVal, magSize);
        int addedBullets = newMagBulletsVal - magBulletsVal;
        magBulletsVal = newMagBulletsVal;
        remainingBulletsVal = Mathf.Max(0, remainingBulletsVal - addedBullets);
        animator.SetBool("reloadAfterFire", false);
    }

    // Update health, ammo in the gun and you bring.
    void updateText()
    {
        magBullets.text = magBulletsVal.ToString() ;
        remainingBullets.text = remainingBulletsVal.ToString();
        remainingHealth.text = health.ToString();
    }

   void shotDetection() // Detecting the object which player shot
   {
       RaycastHit rayHit;
       int layerMask = 1 << 8;
       layerMask = ~layerMask;
       bool addHole = Physics.Raycast(end.transform.position, (end.transform.position - start.transform.position).normalized, out rayHit, 100.0f, layerMask);
       addEffects(addHole, rayHit);
   }
   // Determine the damage multiplier based on the body part hit
   void ApplyDamage(RaycastHit rayHit)
   {
       string bodyPartHit = rayHit.collider.tag;
       string hitMessage = "Hit enemy's ";
       switch (bodyPartHit)
       {
           case "head":
               hitMessage += "head - Instant kill.";
               damageFromPlayerFactor = 1.0f; // Instant kill
               break;
           case "hand":
               hitMessage += "hand.";
               damageFromPlayerFactor = 0.1f; // 10% damage
               break;
           case "chest":
               hitMessage += "chest.";
               damageFromPlayerFactor = 0.3f; // 30% damage
               break;
           case "leg":
               hitMessage += "leg.";
               damageFromPlayerFactor = 0.2f; // 20% damage
               break;
       }
       if (hitMessage != "Hit enemy's ")
       {
           Debug.Log(hitMessage);
           rayHit.collider.GetComponentInParent<Enemy>().Being_Shot(damageFromPlayerFactor);
       }
   }


    void addEffects(bool addHole, RaycastHit rayHit) // Adding muzzle flash, shoot sound and bullet hole on the wall
    {
        if (addHole)
        {
            Vector3 bulletHolePosition = rayHit.point + rayHit.normal * 0.01f;
            Quaternion bulletHoleRotation = Quaternion.LookRotation(rayHit.normal);
            GameObject bulletHoleObject = Instantiate(bulletHole, bulletHolePosition, bulletHoleRotation);
            ApplyDamage(rayHit);
            Destroy(bulletHoleObject, 2.0f);
        }
        GameObject muzzleFlashObject = Instantiate(muzzleFlash, end.transform.position, end.transform.rotation);
        muzzleFlashObject.GetComponent<ParticleSystem>().Play();
        Destroy(muzzleFlashObject, 2.0f);
        Destroy(Instantiate(shotSound, transform.position, transform.rotation), 1.0f);
    }

    // restart the game
    void reStart() {
        SceneManager.LoadScene(0);
    }

    // Door escape collider
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("door"))
        {
            WinGame();
            openDoor();
        }
    }

    // The message will be shown when win
    void WinGame()
    {
        winStatus = true;
        gameOverPanel.GetComponent<CanvasGroup>().alpha = 1;
        victoryMessage.GetComponent<CanvasGroup>().alpha = 1;
        Invoke("reStart", 10f);
    }

    // Refill Ammo
    void refillAmmo(int ammoSourceIndex)
    {
        // Check if ammo is below the maximum limit
        if (remainingBulletsVal < 90)
        {
            isTaken[ammoSourceIndex] = true;
            remainingBulletsVal = Mathf.Min(remainingBulletsVal + 10, 90);
        }
    }

    //open the door
    void openDoor()
    {
           StartCoroutine(RotateDoor(openAngle, openSpeed));

    }

    // Rotate the door to open
    IEnumerator RotateDoor(float angle, float speed)
    {
        Quaternion targetRotation = Quaternion.Euler(-90, angle, 0);
        while (Quaternion.Angle(door.transform.rotation, targetRotation) > 0.01f)
        {
            door.transform.rotation = Quaternion.Slerp(door.transform.rotation, targetRotation, Time.deltaTime * speed);
            yield return null;
        }
    }

}
