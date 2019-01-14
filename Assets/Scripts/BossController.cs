using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : CharController {

    // Constants
    private float SPAWN_DELAY = 0.5f,
                  WALK_SPEED = 0.01f,
                  JUMP_FORCE = 300f,
                  INVULNERABLE_DELAY = 0.1f;

    // Gameplay variables
    private float BASE_HEALTH = 20;
    private float BASE_STRENGTH = 15;

    // Projectile variables
    private const float FIRE_DELAY = 2, NUM_FIRE = 16;
    private GameObject fireballPrefab;
    private Timer fireballTimer = new Timer(FIRE_DELAY);

    // Behaviour variables
    private const float MOVE_TIME = 2;
    private float moveDir = 1;
    private Vector2 BOUNCE_BACK = Vector2.zero;
    private Timer moveTimer = new Timer(MOVE_TIME);

    // Use this for initialization
    void Start () {

        // Initialize animation info
        spawnInfo = new AnimationInfo("enemy_spawn", 38, 12);
        idleInfo = new AnimationInfo("enemy_idle", 16, 8);

        // Initialize projectile
        fireballPrefab = (GameObject)Resources.Load("Fireball");

        // Initialize character
        InitChar(BASE_HEALTH, WALK_SPEED, JUMP_FORCE);
        bounceBack = BOUNCE_BACK;

        // Disable character collision
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), player.GetComponent<Collider2D>());

        // Initialize timers
        fireballTimer.Set(FIRE_DELAY);
        moveTimer.Set(MOVE_TIME);
    }
	
	// Update is called once per frame
	void Update () {
		if(!UIController.paused)
        {
            // Update timers
            fireballTimer.Update();
            moveTimer.Update();

            // Update graphics
            UpdateHealthbar();

            // Regularly shoot fireballs in a circle pattern
            if(fireballTimer.Done && state != SPAWNING && state != DYING)
            {
                float angle = 0, deltaAngle = 2 * Mathf.PI / NUM_FIRE;

                for(int i = 0; i < NUM_FIRE; i++)
                {
                    spitFire(angle);
                    angle += deltaAngle;
                }

                fireballTimer.Reset();
            }

            // Regularly move
            transform.Translate(new Vector3(1, 0, 0) * WALK_SPEED * moveDir);

            if(moveTimer.Done)
            {
                moveDir *= -1;
                moveTimer.Reset();
            }

            // Gameplay checks
            CheckIfOOB();
            CheckIfDead();
        }
	}

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject obj = collision.gameObject;
        //Debug.Log(collision.gameObject);

        // Bullet behaviour
        if (obj.CompareTag("Projectile"))
        {
            // Destroy and take damage from bullets
            if (CollideAndDmg(collision, obj.GetComponent<BulletController>().Strength))
                Destroy(obj);
            // Do not collide if damage wasn't taken
            else
                Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collision.collider);
        }
    }


    //-------------------
    // Behaviour methods
    //------------------

    // Invulnerable enemies can't be hurt by the player
    protected override void Invulnerable(bool on)
    {
        invulnerable = on;
    }

    //-------------------
    // Animation methods
    //-------------------

    // Boss spawn behaviour
    protected override void Spawn()
    {
        base.Spawn();
        Invulnerable(true);
        healthbar.SetActive(false);

        StartCoroutine(EndSpawn());
    }

    // End of spawning state
    IEnumerator EndSpawn()
    {
        yield return new WaitForSeconds(spawnInfo.Length);
        Invulnerable(false);
        healthbar.SetActive(true);
        state = IDLE;
        anim.Play(idleInfo.Name);
    }

    //------------------
    // Gameplay methods
    //------------------

    public GameObject SetProperties(float strength, float baseHealth, float walkSpeed)
    {
        BASE_STRENGTH = strength;
        BASE_HEALTH = baseHealth;
        WALK_SPEED = walkSpeed;

        return gameObject;
    }

    // Generate a fireball
    // "angle" is measured in radians, counterclockwise from the positive x-axis
    private void spitFire(float angle)
    {
        // Create a direction unit vector from the angle
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        // Create the fireball at the boss location
        Vector3 pos = transform.position;
        GameObject fireball = Instantiate(fireballPrefab, pos, transform.rotation);
        fireball.transform.GetChild(0).Rotate(0, 0, angle * Mathf.Rad2Deg);

        // Add the controller
        BulletController controller = fireball.gameObject.AddComponent<BulletController>();
        controller.SetProperties(10, direction, 0.05f, 2, false);
    }
}
