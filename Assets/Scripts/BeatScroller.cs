using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatScroller : MonoBehaviour {

    public bool hasStarted;
    public float beatTempo;

    void Start()
    {
        beatTempo = beatTempo / 60f;
    }

    void Update()
    {
        if (hasStarted)
        {
            transform.position -= new Vector3(0f, beatTempo * Time.deltaTime, 0f);
        }
    }
}
