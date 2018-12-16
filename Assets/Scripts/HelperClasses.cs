using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Timer object class
public class Timer
{
    public float time { get; private set; }
    private float presetTime;
    public bool Done { get; private set; }

    public Timer(float duration)
    {
        time = 0;
        presetTime = duration;
        Done = false;
    }

    // Call during Update()
    public void Update()
    {
        time += Time.deltaTime;

        if (time > presetTime)
        {
            Done = true;
        }
    }

    public void Reset()
    {
        time = 0;
        Done = false;
    }

    public void Set(float duration)
    {
        presetTime = duration;
        Reset();
    }
}

// Animation info container class
public class AnimationInfo
{
    public string Name { get; set; }
    public float Length { get; set; }
    public float NumFrames { get; set; }
    public float FPS { get; set; }


    public AnimationInfo(string name, float numFrames, float fps)
    {
        Name = name;
        NumFrames = numFrames;
        FPS = fps;

        Length = numFrames / fps;
    }
}
