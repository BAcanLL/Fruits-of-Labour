using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


// Main character controller
public class MainCharController : CharController {

    public Text status;

    // Constants
    private const float SPAWN_DELAY = 0.5f,
                        WALK_SPEED = 0.25f,
                        JUMP_FORCE = 350f,
                        INVULNERABLE_DELAY = 0.75f;

    // Projectile variables
    public GameObject bulletPrefab;
    private BulletController bulletControl;
    private Timer bulletTimer = null;

    // Healthbar variables
    private float BASE_HEALTH = 100f, BASE_ENERGY = 100f;
    public float Energy { get; private set; }
    public float BaseEnergy { get; private set; }

    // Animation variables
    private GameObject mFlashPrefab;
    private Color defaultColor;

    // Sound variables
    private AudioSource[] audioSource =  new AudioSource[2];
    private AudioClip pickupSFX, pickUpBubbleSFX, shootingSFX, takeDamageSFX, errorSFX;

    // Behaviour variables
    private Vector2 BOUNCE_BACK = new Vector2(125f, 200f);
    private float DOUBLE_TAP_WINDOW = 0.2f,
                  DASH_MAGNITUDE = 10f;
    private Vector3 startPos;
    private Timer allowDashTimer;
    private bool allowDash;
    public float DASH_COST = 50;

    // Gameplay variables
    public Weapon DefaultWeapon { get; private set; }
    public Weapon CurrentWeapon { get; private set; }
    private List<Weapon> weapons = new List<Weapon>();
    private List<Collider2D> touchingItems = new List<Collider2D>(); // All currently contacting colliders
    public float HealthBubbles { get; private set; }

	void Start () {

        // Initialize animation info
        spawnInfo = new AnimationInfo("tomato_spawn", 20, 20);
        idleInfo = new AnimationInfo("tomato_idle", 8, 12);
        defaultColor = GetComponent<SpriteRenderer>().color;

        // Initalize sfx
        audioSource = GetComponents<AudioSource>();
        pickupSFX = (AudioClip)Resources.Load("Pickup_sfx");
        pickUpBubbleSFX = (AudioClip)Resources.Load("Waterdrop_sfx");
        takeDamageSFX = (AudioClip)Resources.Load("Damage_sfx");

        // Initialize resources
        mFlashPrefab = (GameObject)Resources.Load("muzzle_flash");

        // Save starting position
        startPos = transform.position;

        // Initialize character
        InitChar(BASE_HEALTH, WALK_SPEED, JUMP_FORCE);
        bounceBack = BOUNCE_BACK;
        HealthBubbles = 0;
        BaseEnergy = BASE_ENERGY;
        Energy = BASE_ENERGY;

        // Show / Hide healthbar
        if(true)
        {
            healthbar.GetComponent<SpriteRenderer>().color = Color.clear;
            currentHealthbar.color = Color.clear;
        }

        // Initialize  items
        Item.prefab = (GameObject)Resources.Load("Item_prefab");
        DefaultWeapon = Items.Seeds;
        CurrentWeapon = DefaultWeapon;

        // Initialize sounds
        shootingSFX = (AudioClip)Resources.Load(CurrentWeapon.SfxName);
        errorSFX = (AudioClip)Resources.Load("Error-2_sfx");

        // Initialize timers
        bulletTimer = new Timer(DefaultWeapon.AtkDelay);
        allowDashTimer = new Timer(DOUBLE_TAP_WINDOW);
        regenTimer = new Timer(regenInterval);

        // Collision behaviour
        Physics2D.IgnoreLayerCollision(ENEMY_LAYER, ENEMY_LAYER);
        Physics2D.IgnoreLayerCollision(ENEMY_LAYER, ITEM_LAYER);
        Physics2D.IgnoreLayerCollision(PLAYER_LAYER, PROJECTILE_LAYER);
    }
	
	void Update () {
        if (!UIController.paused)
        {
            // State text for debugging
            status.text = state.Name;

            // Ensure weapon is never null
            if (CurrentWeapon == null)
                CurrentWeapon = DefaultWeapon;

            // Update shooting sound
            if (shootingSFX.name != CurrentWeapon.SfxName)
                shootingSFX = (AudioClip)Resources.Load(CurrentWeapon.SfxName);

            // Update graphics
            UpdateHealthbar();
            if(state != SPAWNING)
                AnimInvulnerable();

            // Update timers
            bulletTimer.Update();
            allowDashTimer.Update();
            regenTimer.Update();

            // Regenerate health every few seconds
            if (regenTimer.Done && healthRegen > 0)
            {
                if (Health < BaseHealth)
                {
                    Heal(healthRegen);
                    GenerateFloatingText("+" + healthRegen.ToString(), GREEN, 12, 1);
                }

                regenTimer.Reset();
            }

            // Regenerate energy
            if (Energy < BaseEnergy)
                Energy += 0.2f;
            // Can't exceed max energy
            else if (Energy > BaseEnergy)
                Energy = BaseEnergy;

            // Reset dash double tap window
            if (allowDashTimer.Done)
            {
                allowDash = false;
                allowDashTimer.Reset();
            }

            // If the current state allows for movement
            if (state.AllowMovement)
            {
                // Double tap to dash left
                if (Input.GetKeyDown(KeyCode.LeftArrow) && state != JUMPING)
                {
                    // If this is the second sucessive tap in the same direction
                    if (allowDash && GetDirection() == LEFT)
                    {
                        // If not enough energy
                        if (Energy < DASH_COST)
                        {
                            audioSource[0].PlayOneShot(errorSFX, 0.1f);
                        }
                        // Dash if enough energy
                        else
                        {
                            allowDash = false;
                            Energy -= DASH_COST;
                            Dash(LEFT);
                        }
                    }
                    // Else register the first tap
                    else
                    {
                        allowDash = true;
                        allowDashTimer.Reset();
                    }
                }
                // Double tap to dash right
                else if (Input.GetKeyDown(KeyCode.RightArrow) && state != JUMPING)
                {
                    // Dash if this is the second sucessive tap in the same direction
                    if (allowDash && GetDirection() == RIGHT)
                    {
                        // If not enough energy
                        if (Energy < DASH_COST)
                        {
                            audioSource[0].PlayOneShot(errorSFX, 0.1f);
                        }
                        // Dash if enough energy
                        else
                        {
                            allowDash = false;
                            Energy -= DASH_COST;
                            Dash(RIGHT);
                        }
                    }
                    // Else register the first tap
                    else
                    {
                        allowDash = true;
                        allowDashTimer.Reset();
                    }
                }

                // Walk left and right with arrow keys
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    Walk(LEFT * walkSpeed);
                    FaceLeft();
                }
                else if (Input.GetKey(KeyCode.RightArrow))
                {
                    Walk(RIGHT * walkSpeed);
                    FaceRight();
                }

                // Fire bullet on Q with delays in between
                if (Input.GetKey(KeyCode.Q) && bulletTimer.Done)
                {
                    if (CurrentWeapon.RapidFire)
                    {
                        RaycastBullet();
                    }
                    else
                    {
                        FireBullet();
                    }

                    // Play sound
                    if (!audioSource[0].isPlaying)
                        audioSource[0].PlayOneShot(shootingSFX, 0.5f);

                    bulletTimer.Reset();
                }

                // Jump when space is pressed
                if (Input.GetKeyDown(KeyCode.Space) && state != JUMPING && state.AllowMovement)
                    DoJump();
            }

            // Pick up the first touching item
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (touchingItems.Count > 0)
                    PickUpItem(touchingItems[0]);
            }

            // Gameplay checks
            CheckIfOOB();
            CheckIfDead();
        }
	}

    // Collision behaviour
    private void OnCollisionStay2D(Collision2D collision)
    {
        OnTouchTerrain(collision);

        // If the character touches an enemy
        if (collision.gameObject.CompareTag("Enemy"))
        {
            CollideAndDmg(collision, collision.gameObject.GetComponent<EnemyController>().Strength);
        }

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject obj = collision.gameObject;
        //Debug.Log(collision.gameObject);

        // Bullet behaviour
        if (obj.CompareTag("Enemy Projectile"))
        {
            // Destroy and take damage from bullets
            if (CollideAndDmg(collision, obj.GetComponent<BulletController>().Strength))
                Destroy(obj);
            // Do not collide if damage wasn't taken
            else
                Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collision.collider);
        }
    }

    // Add all contacted colliders to list
    private void OnTriggerEnter2D(Collider2D collider)
    {
        // Generate a list of all touching items
        if (collider.gameObject.CompareTag("Item") && !touchingItems.Contains(collider))
        {
            Item item = collider.gameObject.GetComponent<ItemController>().item;

            // Pick up heals automatically
            if(item is Bubble)
            {
                PickUpItem(collider);
            }
            // Pick up modifiers first
            else if (item is Modifier)
            {
                touchingItems.Insert(0, collider);
            }
            // Pick up items after
            else
            {
                touchingItems.Add(collider);
            }
        }
    }

    // Remove collider from list after leaving
    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Item") && touchingItems.Contains(collider))
            touchingItems.Remove(collider);
    }


    //-------------------
    // Animation methods
    //-------------------

    // Play the idle animation at start of falling
    // The idle animation sprite sheet starts at jumping
    private void PlayIdleAnimation()
    {
        anim.Play(idleInfo.Name, 0, 6f / idleInfo.NumFrames);
    }

    // Performed all character spawning actions
    protected override void Spawn()
    {
        // Default behaviour
        base.Spawn();

        // Spawning functions
        rigidBody.velocity = Vector2.zero;
        SetGravity(0);
        Invulnerable(true);
        anim.Play(spawnInfo.Name);

        // Call coroutine
        StartCoroutine(Spawning());
    }

    // End the spawning state after the animation completes
    IEnumerator Spawning()
    {
        // Wait to enable gravity
        yield return new WaitForSeconds(spawnInfo.Length);
        anim.enabled = false;
        SetGravity(1);

        // Wait to enable animation
        yield return new WaitUntil(() => state == IDLE);
        anim.enabled = true;
        PlayIdleAnimation();

        // Wait to disable invulnerability
        yield return new WaitForSeconds(1.5f);
        Invulnerable(false);
    }

    //-------------------
    // Movement methods
    //-------------------

    // Dash forward, ignoring all projectiles and enemies
    private void Dash(Vector2 direction)
    {
        // Set state and behaviour
        state = DASHING;
        rigidBody.gravityScale = 0;
        Physics2D.IgnoreLayerCollision(PLAYER_LAYER, ENEMY_LAYER);
        rigidBody.AddForce(DASH_MAGNITUDE * direction, ForceMode2D.Impulse);

        // Change transparency
        Color color = defaultColor;
        color.a = 0.75f;
        GetComponent<SpriteRenderer>().color = color;

        // End dash coroutine
        StartCoroutine(EndDash());
    }

    // End the dash
    IEnumerator EndDash()
    {
        yield return new WaitForSeconds(0.5f);

        rigidBody.gravityScale = 1;
        Physics2D.IgnoreLayerCollision(PLAYER_LAYER, ENEMY_LAYER, false);
        rigidBody.velocity = Vector2.zero;
        GetComponent<SpriteRenderer>().color = defaultColor;
        state = IDLE;
    }

    //-------------------
    // Behaviour methods
    //-------------------

    // Invulnerable players can't touch the enemy
    protected override void Invulnerable(bool on)
    {
        invulnerable = on;

        // Ignore collision with enemies
        Physics2D.IgnoreLayerCollision(PLAYER_LAYER, ENEMY_LAYER, on);

        // Ignore collision with projectiles
        Physics2D.IgnoreLayerCollision(PLAYER_LAYER, PROJECTILE_LAYER, on);
    }

    // Override to respawn on death
    protected override IEnumerator Dying()
    {
        // Drop any picked up weapons
        if (CurrentWeapon != DefaultWeapon)
        {
            DropItem(CurrentWeapon);
            CurrentWeapon = DefaultWeapon;
        }

        yield return new WaitForSeconds(deathFade);

        EnableInteraction(true);
        Color color = GetComponent<SpriteRenderer>().color;
        color.a = 1;
        GetComponent<SpriteRenderer>().color = color;
        Respawn();
    }

    // Respawn
    private void Respawn()
    {
        StopAllCoroutines();
        Spawn();
        bulletTimer.Set(DefaultWeapon.AtkDelay);
        transform.position = startPos;
        Health = BASE_HEALTH;
    }

    // Wait until the jump finishes to restart idling
    protected override IEnumerator WaitForJumpEnd()
    {
        yield return new WaitWhile(() => state == JUMPING);
        if (state == IDLE)
            PlayIdleAnimation();
    }

    // Shoot a projectile in the current direction
    private void FireBullet()
    {
        float offset = GetComponent<Renderer>().bounds.size.x / 2;

        // Instantiate bullet prefab at current character position
        Vector3 pos = transform.position + GetDirection3D() * offset;       
        GameObject bullet = Instantiate(bulletPrefab, pos, transform.rotation);

        // Add bullet controller component
        BulletController bulletControl = bullet.gameObject.AddComponent<BulletController>();
        bulletControl.SetProperties(CurrentWeapon.Strength, GetDirection(), 0.1f, 1);
    }

    // Shoot and damage in the first enemy instantly
    private void RaycastBullet()
    {
        // Show muzzle flash
        GameObject muzzleFlash = Instantiate(mFlashPrefab, transform.position, transform.rotation);
        muzzleFlash.transform.SetParent(transform);
        Vector2 flashOffset = new Vector2(0.9f, 0);
        if (GetComponent<SpriteRenderer>().flipX)
        {
            muzzleFlash.transform.Translate(flashOffset);
            muzzleFlash.GetComponent<SpriteRenderer>().flipX = true;
            muzzleFlash.GetComponent<SpriteRenderer>().sortingLayerName = "Player";
            muzzleFlash.GetComponent<SpriteRenderer>().sortingOrder = 2;
        }
        else
        {
            muzzleFlash.transform.Translate(-flashOffset);
        }
        StartCoroutine(DelayedDestroy(muzzleFlash, 0.02f));

        // Determine ray starting position
        float rayOffset = GetComponent<Renderer>().bounds.size.x / 2;
        Vector2 pos = transform.position + GetDirection3D() * rayOffset;

        // Determine ray layer mask
        int layMask = 3 << 8;

        // Get raycast hit
        RaycastHit2D hit = Physics2D.Raycast(pos, GetDirection(), 5, layMask);

        // If hit something
        if (hit.collider != null && hit.collider.gameObject.CompareTag("Enemy"))
        {
            EnemyController enemy = hit.collider.gameObject.GetComponent<EnemyController>();

            enemy.RayDamage(CurrentWeapon.Strength);

        }
    }

    // Destroy object after delay
    IEnumerator DelayedDestroy(GameObject obj, float seconds)
    {
        yield return new WaitForSeconds(seconds);

        Destroy(obj);
    }

    // End of taking damage behaviour
    protected override IEnumerator WaitForDmgEnd()
    {
        // Invulnerability frame after damage
        yield return new WaitForSeconds(INVULNERABLE_DELAY);
        Invulnerable(false);

        // Idle if on ground
        if (state == IDLE)
            PlayIdleAnimation();
        // Else go into jumping mode
        else
        {
            state = JUMPING;
            StartCoroutine(WaitForJumpEnd());
        }
    }

    //------------------
    // Gameplay methods
    //------------------

    // Take damage with sfx
    protected override bool TakeDamage(float damage)
    {
        audioSource[0].PlayOneShot(takeDamageSFX, 0.3f);
        return base.TakeDamage(damage);
    }

    private void PickUpItem(Collider2D collision)
    {
        // Extract the item information
        Item item = collision.gameObject.GetComponent<ItemController>().item;
        
        // Play audio
        if (item is Bubble)
            audioSource[1].PlayOneShot(pickUpBubbleSFX, 0.2f);
        else
            audioSource[1].PlayOneShot(pickupSFX, 1f);

        // Item specific behaviours
        // Weapons
        if (item is Weapon)
        {
            // Swap weapons
            if(CurrentWeapon != DefaultWeapon)
                DropItem(CurrentWeapon);

            CurrentWeapon = (Weapon)item;
            bulletTimer.Set(CurrentWeapon.AtkDelay);
        }
        // Modifiers
        else if (item is Modifier)
        {
            Modifier mod = (Modifier)item;

            // Increase total health
            if (mod.Type == Modifier.ModType.HealthBonus)
            {
                BaseHealth += mod.Value;
            }
            // Increase health regen
            else if (mod.Type == Modifier.ModType.HealthRegen)
            {
                healthRegen += mod.Value;
            }
            // Enemies drop health bubbles
            else if (mod.Type == Modifier.ModType.Lifesteal)
            {
                HealthBubbles += mod.Value;
            }
        }
        // Bubbles
        else if (item is Bubble)
        {
            Bubble bubble = (Bubble)item;

            if (bubble.Type == Bubble.BubbleType.Health)
            {
                Heal(bubble.Value);

                // Different behaviour for heal
                Destroy(collision.gameObject);
                GenerateFloatingText("+" + bubble.Value, GREEN, 12, 1);
                return;
            }

        }

        // Remove item from scene
        Destroy(collision.gameObject);

        //Generate description text
        GenerateFloatingText("\"" + item.Desc + "\"", Color.white, 16, 2);
    }
}
