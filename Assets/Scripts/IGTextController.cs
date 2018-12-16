using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Controller for in game text
public class IGTextController : MonoBehaviour {
    
    // Default behaviour
    public Vector2 movement = new Vector2(0,0.01f);
    private static float DEFAULT_DUR = 1;
    public float duration = DEFAULT_DUR;

    private Text text;
    private Timer timer = new Timer(DEFAULT_DUR);

	void Start () {
        // Get text and set timer
        text = GetComponentInChildren<Text>();
        timer.Set(duration);
	}
	
	void Update () {
        if (!UIController.paused)
        {
            // Move text
            transform.Translate(movement);

            // Fade text
            Color color = text.color;
            color.a = (duration - timer.time) / duration;
            text.color = color;

            // Destroy text when time's up
            if (timer.Done)
                Destroy(gameObject);

            // Update timer
            timer.Update();
        }
    }

    // Define new behaviour
    public void SetTime(float duration)
    {
        this.duration = duration;
        timer.Set(duration);
    }
}
