using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerController : MonoBehaviour {

    public GameObject egg; // The object to be instantiated
    public float delay;
    public int maxInstances;

    private float strength, baseHealth, walkSpeed;
    private List<GameObject> list = new List<GameObject>();
    private Vector3 pos = new Vector3();
    private Timer timer = new Timer(0);

	// Use this for initialization
	void Start () {
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
}