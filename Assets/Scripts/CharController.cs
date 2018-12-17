using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Base class for game characters
public abstract class CharController : MonoBehaviour
{
    // Class definition for character states
    protected class State
    {
        public string Name { get; private set; }
        public bool AllowMovement { get; private set; }
        public bool AutoIdle { get; private set; }

        public State(string name, bool allowMovement, bool autoIdle)
        {
            Name = name;
            AllowMovement = allowMovement;
            AutoIdle = autoIdle;
        }
    }

    // Available states
    protected State IDLE = new State("idle", true, true),
                    SPAWNING = new State("spawning", false, true),
                    DYING = new State("dying", false, false),
                    JUMPING = new State("jumping", true, true),
                    DASHING = new State("dashing", false, false),
                    TAKING_DMG = new State("taking_dmg", false, true);

    // Current state
    protected State state = null, saveState = null;

    // Gameplay variables
    public float Health { get; protected set; }
    public float BaseHealth { get; protected set; }
    protected bool invulnerable, dead = false;
    protected float healthRegen = 0, regenInterval = 5;
    protected Timer regenTimer;
    protected GameObject healthPrefab;
    protected GameObject healthbar;
    protected SpriteRenderer currentHealthbar;
    protected GameObject player;

    // Animation variables
    public Color GREEN = new Color(0,0.7f,0.1f,1);
    protected Animator anim;
    protected AnimationInfo spawnInfo, idleInfo; // Should be defined in each derived class instance
    protected Object dmgTxtPrefab;
    protected float deathFade;
    protected Timer deathTimer = new Timer(1);

    // Behaviour variables
    protected const float LOW_HEIGHT_LIMIT = -10;
    public static int PLAYER_LAYER = 0,
                        TERRAIN_LAYER = 8,
                        ENEMY_LAYER = 9,
                        PROJECTILE_LAYER = 10,
                        ITEM_LAYER = 11;
    protected Vector2 LEFT = new Vector2(-1, 0),
                      RIGHT = new Vector2(1, 0),
                      UP = new Vector2(0, 1),
                      DOWN = new Vector2(0, -1);
    public Rigidbody2D rigidBody;
    protected float jumpForce;
    protected Vector2 bounceBack = new Vector2(30f, 75f);

    // Movement variables
    protected float walkSpeed;

    // Initialize properties common to all characters
    protected void InitChar(float health, float walkSpeed, float jumpForce)
    {
        // Get components
        anim = GetComponent<Animator>();
        rigidBody = GetComponent<Rigidbody2D>();
        dmgTxtPrefab = Resources.Load("DamageText");
        player = GameObject.FindGameObjectWithTag("Player");
        healthPrefab = (GameObject)Resources.Load("Healthbar");

        // Initialize healthbar
        Vector3 healthPos = new Vector3(0, GetComponent<Renderer>().bounds.size.y, 0) + transform.position;
        healthbar = Instantiate(healthPrefab, healthPos, transform.rotation, transform);
        currentHealthbar = healthbar.GetComponentsInChildren<SpriteRenderer>()[1];

        // Define variables
        this.Health = health;
        BaseHealth = health;
        this.walkSpeed = walkSpeed;
        this.jumpForce = jumpForce;

        // Deathfade default
        deathFade = 1;

        // Spawn character
        Spawn();
    }

    //-------------------
    // Animation methods
    //-------------------

    // Custom spawn behaviour for each character
    protected virtual void Spawn()
    {
        state = SPAWNING;
    }

    // Recursive coroutine to fade the sprite back to white after a number of seconds
    protected IEnumerator FadeToWhite(float seconds)
    {
        // Wait for one frame
        yield return new WaitForSeconds(Time.deltaTime);

        Color color = GetComponent<SpriteRenderer>().color;
        // Set to white when time's up
        if (seconds <= 0)
            GetComponent<SpriteRenderer>().color = Color.white;

        // Otherwise slightly fade and recurse
        if (color != Color.white)
        {
            // The amount the color needs to change in per frame to be white
            // after the specified amount of time
            Color deltaColor = (Color.white - color) / (seconds / Time.deltaTime); 
            GetComponent<SpriteRenderer>().color += deltaColor;

            // Recurse
            StartCoroutine(FadeToWhite(seconds -= Time.deltaTime));
        }
    }

    protected void AnimInvulnerable()
    {
        if(invulnerable)
        {
            GetComponent<SpriteRenderer>().enabled = !(GetComponent<SpriteRenderer>().enabled);
        }
        else
        {
            GetComponent<SpriteRenderer>().enabled = true;
        }
    }

    //-------------------
    // Movement methods
    //-------------------

    protected void Walk(Vector2 direction)
    {
        transform.Translate(direction * walkSpeed);
    }

    protected void FaceLeft()
    {
        GetComponent<SpriteRenderer>().flipX = false;
    }

    protected void FaceRight()
    {
        GetComponent<SpriteRenderer>().flipX = true;
    }

    protected Vector2 GetDirection()
    {
        Vector2 direction = new Vector2(1, 0);

        if (GetComponent<SpriteRenderer>().flipX == false)
            direction.x = -1;

        return direction;
    }

    protected Vector3 GetDirection3D()
    {
        Vector3 direction = new Vector3(1, 0, 0);

        if (GetComponent<SpriteRenderer>().flipX == false)
            direction.x = -1;

        return direction;
    }

    //-------------------
    // Behaviour methods
    //-------------------

    public void SetGravity(float gravityScale)
    {
        rigidBody.gravityScale = gravityScale;
    }

    // Set invulnerable behavious for specific characters
    protected abstract void Invulnerable(bool on);

    protected void Jump(Vector2 force)
    {
        rigidBody.AddForce(force);
    }

    // Start jumping
    protected void DoJump()
    {
        // Do jump
        transform.Translate(0, 0.01f, 0); // Small translation to avoid OnCollisionStay
        Jump(UP * jumpForce);

        // Jump behaviour
        anim.Play("paused");
        state = JUMPING;

        StartCoroutine(WaitForJumpEnd());
    }

    // Wait until the jump finishes to restart idling
    protected virtual IEnumerator WaitForJumpEnd()
    {
        yield return new WaitWhile(() => state == JUMPING);
        if (state == IDLE)
            anim.Play(idleInfo.Name);
    }

    protected virtual void OnTouchTerrain(Collision2D collision)
    {
        if (state.AutoIdle)
        {
            EnableInteraction(true);

            // If the character touches the ground
            if (collision.gameObject.CompareTag("Terrain"))
            {
                // Change the state from jumping to idle
                state = IDLE;
            }
        }
    }

    // Take collision dmg
    public bool CollideAndDmg(Collision2D collision, float dmg)
    {
        bool damaged = false;

        if (!invulnerable)
        {
            // Generate damage text
            GenerateFloatingText("-" + dmg.ToString(), Color.red);

            // Take damage
            if (TakeDamage(dmg))
            {
                // If still alive after damage
                Vector2 bounceForce = bounceBack;

                // If colliding with bullet, bounce according to bullet direction
                if (collision.gameObject.CompareTag("Projectile"))
                {
                    if(collision.gameObject.GetComponent<BulletController>().Direction.x < 0)
                        bounceForce.x *= -1;
                }
                // If colliding with character, bounce according to relative direction
                else if (collision.gameObject.transform.position.x > transform.position.x)
                {
                    bounceForce.x *= -1;
                }

                // Do bounce
                transform.Translate(new Vector3(0, 0.01f, 0)); // Slight translation to avoid OnCollisionStay
                rigidBody.AddForce(bounceForce);

                // Damaged behaviour
                anim.Play("paused");
                Invulnerable(true);
                state = TAKING_DMG;

                StartCoroutine(WaitForDmgEnd());
            }

            damaged = true;
        }

        return damaged;
    } 

    // Generate text at the character
    protected GameObject GenerateFloatingText(string txt, Color color)
    {
        // Generate text slightly in front of the current character
        Vector3 pos = transform.position;
        pos.z += 0.1f;

        // Create instance of damage text prefab with controller
        GameObject newTxt = (GameObject) Instantiate(dmgTxtPrefab, pos, transform.rotation);
        newTxt.transform.SetParent(transform);
        newTxt.GetComponentInChildren<Text>().text = txt;
        newTxt.GetComponentInChildren<Text>().color = color;
        IGTextController controller = newTxt.AddComponent<IGTextController>();

        return newTxt;
    }

    // Generate text of a specific size at the character
    protected GameObject GenerateFloatingText(string txt, Color color, int size, float duration)
    {
        GameObject newTxt = GenerateFloatingText(txt, color);
        newTxt.GetComponentInChildren<Text>().fontSize = size;
        newTxt.GetComponent<IGTextController>().SetTime(duration);

        return newTxt;
    }

    // Coroutine for end of taking damage behaviour
    protected virtual IEnumerator WaitForDmgEnd()
    {
        yield return new WaitForSeconds(0.2f);
        Invulnerable(false);
        anim.Play(idleInfo.Name);
    }

    // Called during Update() to execute death behaviour
    protected virtual void CheckIfDead()
    {
        if (dead)
            Destroy(gameObject);

        // If dead, play death fade animation
        if (state == DYING)
        {
            Color color = GetComponent<SpriteRenderer>().color;
            color.a = (deathFade - deathTimer.time) / deathFade;
            GetComponent<SpriteRenderer>().color = color;

            deathTimer.Update();
        }
        // Else, check if dead
        else if (Health <= 0)
        {
            // Dying behaviour
            rigidBody.velocity = Vector2.zero;
            EnableInteraction(false);
            GetComponent<SpriteRenderer>().sortingOrder = 0; // Send dying items to back
            state = DYING;
            deathTimer.Set(deathFade); // Timer for animation

            // Coroutine to delete character
            StartCoroutine(Dying());
        }
    }

    // Disable all interactable components
    protected void EnableInteraction(bool enabled)
    {
        healthbar.SetActive(enabled);
        GetComponent<Rigidbody2D>().gravityScale = (enabled ? 1 : 0);
        GetComponent<Collider2D>().enabled = enabled;
        anim.enabled = enabled;
    }

    // Delay to destroy gameobject
    protected virtual IEnumerator Dying()
    {
        Debug.Log(dead);
        yield return new WaitForSeconds(deathFade);

        dead = true;      
    }

    // Called during Update() to check if out of bounds
    protected virtual void CheckIfOOB()
    {
        // Falling below lower bound kills the character
        if (transform.position.y < LOW_HEIGHT_LIMIT)
        {
            Health = 0;
        }
    }

    //------------------
    // Gameplay methods
    //------------------

    protected virtual bool TakeDamage(float damage)
    {
        // Can't take negative damage
        // Can't damage invincible characters
        if (damage < 0 || invulnerable)
            damage = 0;

        // Check if damage kills character
        bool stillAlive = (damage < Health ? true : false);

        if (stillAlive)
        {
            Health -= damage;

            // Tint color from damage and fade to normal
            if (gameObject.CompareTag("Player"))
                gameObject.GetComponent<SpriteRenderer>().color = Color.magenta;
            else
                gameObject.GetComponent<SpriteRenderer>().color = Color.red;

            StartCoroutine(FadeToWhite(0.5f));
        }
        else
        {
            Health = 0;
        }

        // Return alive status
        return stillAlive;
    }

    protected float Heal(float ammount)
    {
        // Can't overheal
        if (Health + ammount > BaseHealth)
            ammount = BaseHealth - Health;

        Health += ammount;

        return ammount;
    }

    protected void UpdateHealthbar()
    {
        if(healthbar != null)
        {
            currentHealthbar.transform.position = healthbar.transform.position - new Vector3((BaseHealth - Health) / (2 * BaseHealth), 0, 0.1f);
            currentHealthbar.size = new Vector2(Health / (BaseHealth * 10), 0.16f);
        }
    }

    // Return an instantiated a game object 
    public GameObject DropItem(Item item)
    {
        // Create an in game instance of the item
        GameObject instance = Instantiate(Item.Prefab(), transform.position - new Vector3(0, 0, 1), transform.rotation);

        // Force with which the object is dropped
        Vector2 force = new Vector2(0, 300f);

        // Randomize drop direction for bubbles
        if(item is Bubble)
        {
            float randX = Random.Range(-100f, 100f),
                  randY = Random.Range(0.9f, 1.1f);

            force.x += randX;
            force.y *= randY;
        }

        // Throw the object with force
        instance.GetComponent<Rigidbody2D>().AddForce(force);

        // Add the controller to the item
        instance.AddComponent<ItemController>().InitializeItem(item);
        return instance;
    }

    public GameObject DropItem(Item item, int sortingOrder)
    {
        GameObject instance = DropItem(item);
        instance.GetComponent<SpriteRenderer>().sortingOrder = sortingOrder;

        return instance;
    }
}
