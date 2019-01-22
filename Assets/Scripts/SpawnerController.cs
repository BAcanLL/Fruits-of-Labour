using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpawnerController : MonoBehaviour {

    // Spawning variables
    public GameObject egg; // The object to be instantiated
    public float delay;
    public int maxInstances;
    public bool isBoss;
    private bool bossSpawning = false;
    private float strength, baseHealth, walkSpeed;
    private List<GameObject> list = new List<GameObject>();
    private Vector3 pos = new Vector3();
    private Timer timer = new Timer(0);

    // Gameplay variables
    private Text text;
    public const float MAX_POINTS = 2;
    public static float Points { get; private set; }

    // Player reference
    private GameObject player;

	// Use this for initialization
	void Start () {
        // Initialize references
        player = GameObject.FindGameObjectWithTag("Player");
        text = GetComponentInChildren<Text>();
        if (isBoss)
        {
            text.text = "Press <W> to inspect vines.";
        }
        else
        {
            // Intialize instance stats
            CharacterStats stats = egg.GetComponent<CharacterStats>();
            strength = stats.strength;
            baseHealth = stats.baseHealth;
            walkSpeed = stats.walkSpeed;
        }

        // Hide the spawner
        GetComponent<SpriteRenderer>().enabled = false;

        // Intialize timer
        timer.Set(delay);

        // Set starting position
        pos = transform.position;

        // Initialize text
        text.enabled = false;
	}
	
	// Update is called once per frame
	void Update () {

        if (isBoss)
        {
            // Boss spawner behaviour
        }
        else
        {
            // If timer's up
            if (timer.Done)
            {
            // Refresh the list of instances to delete all null references
            foreach (GameObject instance in list)
                if (instance == null)
                    list.Remove(instance);

            // If there's room for more instances
            if (list.Count < maxInstances)
            {
                // Spawn an instance from egg
                list.Add(Instantiate(egg, pos, transform.rotation).GetComponent<EnemyController>().SetProperties(strength, baseHealth, walkSpeed));
            }

            // Reset timer
            timer.Reset();
            }
        }

        // Update timer
        timer.Update();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == player )
        {
            text.enabled = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject == player)
        {
            text.enabled = false;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isBoss)
        {
            if (Points >= MAX_POINTS && Input.GetKey(KeyCode.W) && !bossSpawning)
            {
                bossSpawning = true;
                Destroy(text);
                StartCoroutine(SpawnBoss());
            }
        }
        else if (collision.gameObject == player)
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                Points++;
                Destroy(gameObject);
            }
        }
    }

    private IEnumerator SpawnBoss()
    {
        yield return new WaitForSeconds(2);

        Instantiate(egg, pos + new Vector3(0, 1, 0), transform.rotation);
        Destroy(gameObject);
    }
}