using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    private GameObject player;

    private Vector2 offset;

	// Use this for initialization
	void Start () {
        // Initialize reference to player
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        offset = player.transform.position - transform.position;

        transform.Translate(offset);
    }
}
