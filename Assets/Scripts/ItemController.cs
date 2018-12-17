using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Base class for game items
public abstract class Item
{
    // Rarities
    public enum Rarity { Common, Uncommon, Rare, Legendary };

    // Variables
    public string Name { get; protected set; }
    public string Desc { get; protected set; }
    public string SpriteName { get; protected set; }
    public Rarity ItemRarity { get; protected set; }
    public static GameObject prefab;

    // Get the prefab
    public static GameObject Prefab()
    {
        return prefab;
    }

    // Instance constructor
    public Item(string name, string spriteName, string desc, Rarity rarity)
    {
        Name = name;
        Desc = desc;
        SpriteName = spriteName;
        ItemRarity = rarity;
    }

}

// Items that are weapons
public class Weapon : Item
{
    public float Strength { get; private set; }
    public float AtkDelay { get; private set; }
    public bool RapidFire { get; private set; }
    public string SfxName { get; private set; }

    public Weapon(string name, string spriteName, string sfxName, string desc, Rarity rarity, float strength, float atkDelay, bool rapidFire)
        : base(name, spriteName, desc, rarity)
    {
        SfxName = sfxName;
        Strength = strength;
        AtkDelay = atkDelay;
        RapidFire = rapidFire;
    }
}

// Items that modify values
public class Modifier : Item
{
    // Types of modifiers
    public enum ModType { HealthBonus, HealthRegen, Lifesteal };

    public ModType Type { get; private set; }
    public float Value { get; private set; }

    // Instance constructor
    public Modifier(string name, string spriteName, string desc, Rarity rarity, ModType type, float value) 
        : base(name, spriteName, desc, rarity)
    {
        Type = type;
        Value = value;
    }
}

public class Bubble : Item
{
    //Types of bubbles
    public enum BubbleType { Health, Score };

    public BubbleType Type { get; private set; }
    public float Value { get; private set; }

    public Bubble(string name, string spriteName, Rarity rarity, BubbleType type, float value) : base(name, spriteName, "", rarity)
    {
        Type = type;
        Value = value;
    }
}

// All items that exist
public static class Items
{
    /* Item drop rates
     * || None || Common || Uncommon || Rare ||
     * || 0.50 ||  0.35  ||   0.12   || 0.03 || 
     */

    // Weapons
    public static Weapon Seeds = new Weapon("Seeds", "Projectile_MC", "Beep_sfx", "Default weapon", Item.Rarity.Common, 1, 0.5f, false);
    public static Weapon Shovel = new Weapon("Shovel", "Shovel_sprite", "Beep_sfx", "Shove it", Item.Rarity.Common, 2, 0.35f, false);
    public static Weapon Shears = new Weapon("Shears", "Clippers_sprite", "Beep_sfx", "Snip snip", Item.Rarity.Common, 3, 0.5f, false);
    public static Weapon Magnum = new Weapon("M500 Magnum", "Magnum_sprite", "Magnum_sfx", "Oh yeah", Item.Rarity.Rare, 8, 0.5f, true);
    public static Weapon Uzi = new Weapon("Uzi", "Uzi_sprite", "Uzi_sfx", "Time to get serious", Item.Rarity.Rare, 3, 0.05f, true);

    // Modifiers
    //public static Modifier Heal = new Modifier("Heal", "Blueorb-2_sprite", "Heal", Item.Rarity.Common, Modifier.ModType.Heal, 1);
    public static Modifier Waterdrop = new Modifier("Waterdrop", "Waterdrop_sprite", "More health", Item.Rarity.Common, Modifier.ModType.HealthBonus, 2);
    public static Modifier Fertilizer = new Modifier("Fertilizer", "Fertilizer_sprite", "More health", Item.Rarity.Uncommon, Modifier.ModType.HealthBonus, 20);
    public static Modifier WateringCan = new Modifier("Watering can", "Watercan_sprite", "Regenerate health", Item.Rarity.Uncommon, Modifier.ModType.HealthRegen, 2);
    public static Modifier Sickle = new Modifier("Sickle", "Sickle_sprite", "Reap health from fallen enemies", Item.Rarity.Uncommon, Modifier.ModType.Lifesteal, 1);
    public static Modifier GoldenSickle = new Modifier("Golden Sickle", "Goldensickle_sprite", "Reap health from fallen enemies", Item.Rarity.Rare, Modifier.ModType.Lifesteal, 3);

    // Bubbles
    public static Bubble Heal = new Bubble("Heal", "Greenorb-2_sprite", Item.Rarity.Common, Bubble.BubbleType.Health, 1);

    // Lists of dropable Items
    public static List<Item> CommonDrops = new List<Item>() { Shovel, Shears };
    public static List<Item> UncommonDrops = new List<Item>() { Fertilizer, WateringCan, Sickle };
    public static List<Item> RareDrops = new List<Item>() { Magnum, Uzi, GoldenSickle };

    //--------------
    // Drop methods
    //--------------

    public static Item RandomDrop()
    {
        Item item = null;

        float rand = Random.Range(0, 1f);

        if (rand < 0.5)
        {
            // No drop
        }
        else if (rand < 0.85)
        {
            // Common
            item = RandomItem(CommonDrops);
        }
        else if (rand < 0.97)
        {
            // Uncommon
            item = RandomItem(UncommonDrops);
        }
        else
        {
            // Rare
            item = RandomItem(RareDrops);
        }

        return item;
    }

    private static Item RandomItem(List<Item> list)
    {
        Item item = null;

        if(list.Count > 0)
        {
            int index = Random.Range(0, list.Count);
            item = list[index];
        }
        return item;
    }
}

// Controller for dropped items
public class ItemController : MonoBehaviour{

    public Item item;

    private const float DURATION = 10;
    private Text text = null;
    private bool showTxt = false;
    private GameObject player;
    private Timer timer = new Timer(DURATION), healTimer = new Timer(0);
    private float randForce = 1;

    // Initialize script
    public void InitializeItem(Item item)
    {
        // Main character
        player = GameObject.FindGameObjectWithTag("Player");

        // Intialize references
        this.item = item;
        text = GetComponentInChildren<Text>();
        text.text = "Press <E> to pick up " + item.Name;

        // Initialize sprite
        GetComponent<SpriteRenderer>().sprite = GetSprite(item.SpriteName);
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), player.GetComponent<Collider2D>()); // Collider ignores player

        // Initialize timer
        timer.Set(DURATION);

    }

    void Start()
    {
        // Items don't collide with itself
        Physics2D.IgnoreLayerCollision(CharController.ITEM_LAYER, CharController.ITEM_LAYER);

        // Main character
        player = GameObject.FindGameObjectWithTag("Player");

        // Bubble item special case
        if (item is Bubble)
        {
            InitializeBubble();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Bubbles ignore terrain
        if (item is Bubble)
        {
            if (collision.gameObject.CompareTag("Terrain"))
                Physics2D.IgnoreCollision(collision.gameObject.GetComponent<Collider2D>(), GetComponent<Collider2D>());
        }
    }

    void Update()
    {
        if (!UIController.paused)
        {
            // Bubble item special case
            if (item is Bubble)
            {
                BubbleBehaviour();
            }
            // Other items
            else
            {
                // Show text after 1 second
                if (text != null && timer.time > 1)
                {
                    text.enabled = showTxt;

                    // Semi-transparent when player is not close
                    Color color = GetComponent<SpriteRenderer>().color;
                    if (showTxt)
                        color.a = 1f;
                    else
                        color.a = 0.5f;
                    GetComponent<SpriteRenderer>().color = color;
                }

                // Show (non-heal) item text if player is close
                showTxt = Distance2Player() < 1.5;
            }

            // Despawn after a delay
            if (timer.Done)
                Destroy(gameObject);

            // Update timer
            timer.Update();
        }
    }

    //-------------------
    // Behaviour methods
    //-------------------

    private void InitializeBubble()
    {
        // No hover text
        showTxt = false;

        // Change size
        float randScale = Random.Range(0.2f, 0.5f);
        transform.localScale = new Vector3(1, 1, 1) * randScale;

        // Semi transparent
        Color color = GetComponent<SpriteRenderer>().color;
        color.a = 1f;
        GetComponent<SpriteRenderer>().color = color;

        // Smaller collider
        GetComponent<BoxCollider2D>().size *= 0.5f;

        // Set variables
        healTimer.Set(0.875f);
        randForce = Random.Range(0.5f, 1.3f);
    }

    private void BubbleBehaviour()
    {
        if (!healTimer.Done)
        {
            GetComponent<Rigidbody2D>().gravityScale = healTimer.time;
            healTimer.Update();
        }
        else
        {
            GetComponent<Rigidbody2D>().gravityScale = 0;
        }

        if (healTimer.Done)
        {
            GetComponent<Rigidbody2D>().AddForce(PlayerDirection() * randForce, ForceMode2D.Impulse);
        }           
    }


    //------------------
    // Gameplay methods
    //------------------

    // Get a sprite from the resources folder
    public static Sprite GetSprite(string name)
    {
        return Resources.Load<Sprite>(name);
    }

    // Distance to player
    private float Distance2Player()
    {
        return Vector2.Distance(player.transform.position, transform.position);
    }

    // Direction to player
    private Vector2 PlayerDirection()
    {
        return (player.transform.position - transform.position) / Distance2Player();
    }
}
