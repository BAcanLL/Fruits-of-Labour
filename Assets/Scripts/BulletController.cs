using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class for controlling projectiles
public class BulletController : MonoBehaviour {

    // Variables
    private static float DEFAULT_TIME = 2;
    private float speed, lifetime;
    public float Strength { get; private set; }
    public Vector2 Direction { get; private set; }
    private Timer timer = new Timer(DEFAULT_TIME);

	void Start () {
        // Initialize timer
        timer.Set(DEFAULT_TIME);

        // Initialize collider behaviour
        Physics2D.IgnoreLayerCollision(CharController.ITEM_LAYER, CharController.PROJECTILE_LAYER);
	}
	
	void Update () {
        // Destroy after exceeding lifetime
        if (timer.Done)
            Destroy(gameObject);

        // Increment timer
        timer.Update();

        // Set sprite orientation
        if (Direction.x < 0)
            GetComponentInChildren<SpriteRenderer>().flipY = true; // flipY instead of flipX due to 90 deg rotation

        // Move bullet
        transform.Translate(Direction * speed);
	}

    // Destroy bullets on terrain
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Terrain"))
            Destroy(gameObject);
    }

    public void SetProperties(float strength, Vector2 direction, float speed, float lifetime)
    {
        Strength = strength;
        this.Direction = direction;
        this.speed = speed;
        this.lifetime = lifetime;
        timer.Set(lifetime);
    }
}


