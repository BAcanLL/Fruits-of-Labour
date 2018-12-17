using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyController : CharController {

    // Constants
    private float SPAWN_DELAY = 0.5f,
                  WALK_SPEED = 0.01f,
                  JUMP_FORCE = 300f,
                  INVULNERABLE_DELAY = 0.1f;

    // Healthbar variables
    private float BASE_HEALTH = 20;

    // Gameplay variables
    private float BASE_STRENGTH = 15;
    public float Strength { get; private set; }
    private SpawnerController spawner;

    // Behaviour variables
    private float rayKnockBack = 75,
                  followDistance = 15;
    private Timer jumpTimer, movementTimer;
    private float JUMP_DELAY = 1;
    private Vector2 currentDirection = new Vector2();

    // Use this for initialization
    void Start () {
        // Initialize animation info
        spawnInfo = new AnimationInfo("enemy_spawn", 17, 12);
        idleInfo = new AnimationInfo("enemy_idle", 4, 8);

        // Initialize timers
        jumpTimer = new Timer(1);
        movementTimer = new Timer(5);

        // Initialize character
        InitChar(BASE_HEALTH, WALK_SPEED, JUMP_FORCE);
        GetComponent<SpriteRenderer>().sortingOrder = 3;
        Strength = BASE_STRENGTH;
        jumpForce *= rigidBody.mass;
        currentHealthbar.color = Color.yellow;
        currentDirection = RandomDirection();
	}
	
	// Update is called once per frame
	void Update () {

        if (!UIController.paused)
        {
            // Update timers
            jumpTimer.Update();
            movementTimer.Update();

            // Update graphics
            UpdateHealthbar();

            // Get movement
            if (state.AllowMovement)
            {
                // Follow the player within a certain distance
                if (Distance2Player() < followDistance)
                {
                    Move(DirToPlayer());
                    movementTimer.Reset();
                }
                // Move in a preset direction
                else
                {
                    Move(currentDirection);

                    // Randomize the preset direction every once in a while
                    if (movementTimer.Done)
                    {
                        movementTimer.Reset();
                        currentDirection = RandomDirection();
                    }
                }

                if (jumpTimer.Done)
                {
                    AutoJump();
                    jumpTimer.Reset();
                }
            }

            // Gameplay checks
            CheckIfOOB();
            CheckIfDead();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Update IDLE state
        if(state != SPAWNING)
            OnTouchTerrain(collision);
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
    // Animation methods
    //-------------------

    // Enemy spawn behaviour
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

    //-------------------
    // Behaviour methods
    //-------------------

    // Invulnerable enemies can't be hurt by the player
    protected override void Invulnerable(bool on)
    {
        invulnerable = on;

        // Ignore collision with player
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), player.GetComponent<Collider2D>(), on);
    }

    // Damage from raycast bullets
    public bool RayDamage(float dmg)
    {
        bool damage = false;

        if (!invulnerable)
        {
            // Generate damage text
            GenerateFloatingText("-" + dmg.ToString(), Color.red);

            // If still alive after damage
            if(TakeDamage(dmg))
            {
                // Knock back proportional to damage
                rigidBody.velocity = Vector2.zero; 
                rigidBody.AddForce(rayKnockBack * -DirToPlayer() * Mathf.Sqrt(dmg));

                // Damaged behaviour
                anim.Play("paused");
                Invulnerable(true);
                state = TAKING_DMG;

                StartCoroutine(WaitForDmgEnd());
            }

            damage = true;
        }

        return damage;
    }

    // Move in a direction
    private void Move(Vector2 translation)
    {
        if (translation.x < 0)
            GetComponent<SpriteRenderer>().flipX = true;
        else
            GetComponent<SpriteRenderer>().flipX = false;

        transform.Translate(translation * WALK_SPEED);
    }

    private void AutoJump()
    {
        // Cast a ray to hit terrain
        int layMask = 1 << (TERRAIN_LAYER);
        RaycastHit2D wallCheck = Physics2D.Raycast(transform.position, -GetDirection(), 0.8f, layMask);

        // Is the player next to terrain
        bool next2Wall = wallCheck.collider != null;

        // Condition for jump
        if (state != JUMPING && next2Wall)
            DoJump();
    }

    // Dying behaviour
    protected override IEnumerator Dying()
    {
        // Drop random item
        Item dropItem = Items.RandomDrop();

        if(dropItem != null)
            DropItem(dropItem);

        // Drop heal bubbles
        for (int i = 0; i < player.GetComponent<MainCharController>().HealthBubbles; i++)
            DropItem(Items.Heal, 2);

        yield return new WaitForSeconds(deathFade);

        Destroy(gameObject);
    }

    //------------------
    // Gameplay methods
    //------------------

    // Initialize properties
    public GameObject SetProperties(float strength, float baseHealth, float walkSpeed)
    {
        Strength = strength;
        BASE_STRENGTH = strength;
        BASE_HEALTH = baseHealth;
        WALK_SPEED = walkSpeed;

        return gameObject;
    }

    // The distance between the player and enemy
    private float Distance2Player()
    {
        return Vector2.Distance(player.transform.position, transform.position);
    }

    // Return the direction to the player character
    private Vector2 DirToPlayer()
    {
        float x = 1;

        if (player.transform.position.x < transform.position.x)
            x = -1;

        return new Vector2(x, 0);
    }

    // Return a random direction (40% left, 40% right, 20% still)
    private Vector2 RandomDirection()
    {
        float rand = Random.value;
        Vector2 movement = Vector2.zero;

        if (rand < 0.4)
            movement = LEFT;
        else if (rand < 0.8)
            movement = RIGHT;

        return movement;
    }

}
