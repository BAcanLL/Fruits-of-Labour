using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpawnerController : MonoBehaviour {

    // Spawning variables
    public GameObject egg; // The object to be instantiated
    public float delay;
    public int maxInstances;
    private float strength, baseHealth, walkSpeed;
    private List<GameObject> list = new List<GameObject>();
    private Vector3 pos = new Vector3();
    private Timer timer = new Timer(0);

    // Gameplay variables
    private Text text;

    // Player reference
    private GameObject player;

	// Use this for initialization
	void Start () {
        // Initialize references
        player = GameObject.FindGameObjectWithTag("Player");
        text = GetComponentInChildren<Text>();

        // Hide the spawner
        GetComponent<SpriteRenderer>().enabled = false;

        // Intialize timer
        timer.Set(delay);

        // Set starting position
        pos = transform.position;

        // Intialize instance stats
        CharacterStats stats = egg.GetComponent<CharacterStats>();
        strength = stats.strength;
        baseHealth = stats.baseHealth;
        walkSpeed = stats.walkSpeed;

        // Initialize text
        text.enabled = false;
	}
	
	// Update is called once per frame
	void Update () {

        // If timer's up
        if(timer.Done)
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
        if (collision.gameObject == player)
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                Destroy(gameObject);
            }
        }
    }
}