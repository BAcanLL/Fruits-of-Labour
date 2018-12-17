using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : CharController {

	// Use this for initialization
	void Start () {
        // Initialize animation info
        spawnInfo = new AnimationInfo("enemy_spawn", 17, 12);
        idleInfo = new AnimationInfo("enemy_idle", 4, 8);
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    // Invulnerable enemies can't be hurt by the player
    protected override void Invulnerable(bool on)
    {
        invulnerable = on;

        // Ignore collision with player
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), player.GetComponent<Collider2D>(), on);
    }
}
