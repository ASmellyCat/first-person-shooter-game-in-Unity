using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public Animator animator;        
    public GameObject[] targets;     
    public bool isDead;             
    public bool isDetected;         
    public float maxHealth = 100;
    public float health;
    public bool beingShoot;          
    public int enemyID;         
    public GameObject player;
    public GameObject gun;
    public GameObject bulletHole;   
    public GameObject muzzleFlash;   
    public GameObject shotSound;     
    public GameObject end, start;    
    private Vector3 enemyCenter;
    private float roomWidth = 40;
    private float roomDepth = 40;
    private float gunShotTime = 0.5f;
    private float damageFromEnemyFactor = 0.2f;
    private int currTargetIdx;
    private float dis2Player;
    private float fieldOfView = 120f;
    private float chaseDistance = 10f;
    private float rotationSpeed = 0.2f;
    private float moveSpeed = 0.1f;






    void Start()
    {
        currTargetIdx = 0; // enemy move from target 1->2->3->4->1 as a loop
        health = maxHealth;
    }

    void Update()
    {
        if (!isDead && !player.GetComponent<Gun>().winStatus)
        {
            dis2Player = Vector3.Distance(player.transform.position, transform.position);
            if (!isDetected && !beingShoot)
            {
                Invoke("RandomPatrolRoute", 2.0f);
                DetectPlayerInRoom();
            }
            else
            {
                if (dis2Player > chaseDistance)
                {
                    animator.SetBool("fire", false);
                    RunTowardsPlayer();
                }
                else
                {
                    animator.SetTrigger("fire");
                    if (!player.GetComponent<Gun>().isDead)
                    {
                        Invoke("Shoot", 0.3f);
                    }
                }
            }
        }
        else if (isDead)
        {
            animator.SetBool("run", false);
            animator.SetBool("fire", false);
            animator.SetBool("walk", false);
            Destroy(gameObject.GetComponent<CharacterController>());
            animator.SetTrigger("dead");
            Invoke("ReleaseGun", 1.0f);
        }
        else if (player.GetComponent<Gun>().winStatus)
        {
            animator.SetBool("run", false);
            animator.SetBool("fire", false);
            animator.SetBool("walk", false);
        }
    }

    // Make sure that if the player doesn't enter the room, the enemy in the room won't detect
    private void DetectPlayerInRoom()
    {
        if (enemyID == 1)
        {
            enemyCenter = new Vector3(0, 0, 0);
        }
        else if (enemyID == 2)
        {
            enemyCenter = new Vector3(0, 0, 40);
        }
        else
        {
            enemyCenter = new Vector3(40, 0, 40);
        }
        Vector3 playerPosition = player.transform.position;
        if (IsPlayerInRoom(playerPosition, enemyCenter))
        {
            DetectPlayer();
        }
    }

    // Check if the player's position is within the room's bounds
    private bool IsPlayerInRoom(Vector3 playerPosition, Vector3 roomCenter)
    {
        return playerPosition.x > roomCenter.x - roomWidth / 2 &&
               playerPosition.x < roomCenter.x + roomWidth / 2 &&
               playerPosition.z > roomCenter.z - roomDepth / 2 &&
               playerPosition.z < roomCenter.z + roomDepth / 2;
    }

    // Enemy wandering in the room, from target 1->2->3->4->1 as a loop.
    void RandomPatrolRoute()
    {
        if (Vector3.Distance(targets[currTargetIdx].transform.position, transform.position) < 3)
        {
            currTargetIdx = (currTargetIdx + 1) % targets.Length;
        }
        Quaternion targetRotation = Quaternion.LookRotation(targets[currTargetIdx].transform.position - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    // How enemy can detect player
    void DetectPlayer()
    {
        Vector3 directionToPlayer = player.transform.position - transform.position;
        float angleWithPlayer = Vector3.Angle(directionToPlayer, transform.forward);
        if (angleWithPlayer < fieldOfView * 0.5f) {
            isDetected = true;
            animator.SetTrigger("run");
            Debug.Log("Player detected within room bounds!");
        }
    }

    // After detect player, enemy will run towards player
    void RunTowardsPlayer()
    {
        animator.SetTrigger("run");
        transform.rotation = Quaternion.LookRotation(player.transform.position - transform.position);
        transform.position = transform.position + transform.forward * Time.deltaTime * rotationSpeed;

    }

    // Apply damage to the player
    void ShootDetection()
    {
        RaycastHit rayHit;
        int layerMask = 1 << 8;
        layerMask = ~layerMask;
        bool addHole = Physics.Raycast(end.transform.position, (end.transform.position - start.transform.position).normalized, out rayHit, 100.0f, layerMask);
        addEffects(addHole, rayHit);
    }

    void Shoot()
    {
        //cool down
        if (gunShotTime >= 0.0f)
        {
            gunShotTime -= Time.deltaTime;
        }
        if (gunShotTime <= 0.0f) {
            float randomChance = Random.Range(0f, 1f);
            // 80% chance to shoot in random direction, 20% chance to shoot accurately
            if (randomChance <= 0.2)
            {
                transform.rotation = Quaternion.LookRotation(player.transform.position - transform.position);
            }
            else
            {
                var randomAngle = Quaternion.Euler(0, Random.Range(-30.0f, 30.0f), 0);
                transform.rotation = Quaternion.LookRotation(randomAngle * (player.transform.position - transform.position));
            }
            ShootDetection();
            gunShotTime = 0.5f;
        }
    }

    void ApplyDamage()
    {
        player.GetComponent<Gun>().Being_shot(damageFromEnemyFactor);
    }

    void addEffects(bool addHole, RaycastHit rayHit) // Adding muzzle flash, shoot sound and bullet hole on the wall
    {
        if (addHole)
        {
            Vector3 bulletHolePosition = rayHit.point + rayHit.normal * 0.01f;
            Quaternion bulletHoleRotation = Quaternion.LookRotation(rayHit.normal);
            GameObject bulletHoleObject = Instantiate(bulletHole, bulletHolePosition, bulletHoleRotation);
            if (rayHit.collider.CompareTag("Player"))
            {
                ApplyDamage();
            }
            Destroy(bulletHoleObject, 2.0f);
        }
        GameObject muzzleFlashObject = Instantiate(muzzleFlash, end.transform.position, end.transform.rotation);
        muzzleFlashObject.GetComponent<ParticleSystem>().Play();
        Destroy(muzzleFlashObject, 2.0f);
        Destroy(Instantiate(shotSound, transform.position, transform.rotation), 1.0f);
    }

    // Recalculate the health
    public void Being_Shot(float damageFromPlayerFactor)
    {
        beingShoot = true;
        if (damageFromPlayerFactor <= 0) {
            Debug.Log("Damage amount must be greater than 0.");
            return;
        }
        health -= Mathf.Round(maxHealth * damageFromPlayerFactor);
        if (health <= 0) {
            isDead = true;
            health = 0;
            Debug.Log("Enemy has died.");
        } else {
            Debug.Log("Enemy took damage " + Mathf.Round(maxHealth * damageFromPlayerFactor) + ". Remaining health: " + Mathf.Round(health));
        }

    }

    // make the gun independent after enemy dies
    void ReleaseGun()
    {
        Rigidbody gunRigidbody = gun.GetComponent<Rigidbody>();
        BoxCollider gunCollider = gun.GetComponent<BoxCollider>();
        gunRigidbody.constraints = RigidbodyConstraints.None;
        gunRigidbody.useGravity = true;
        gunRigidbody.constraints &= ~RigidbodyConstraints.FreezePositionY;
        gunRigidbody.constraints &= ~RigidbodyConstraints.FreezeRotationY;
        gunCollider.isTrigger = false;

    }
}
