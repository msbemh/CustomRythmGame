using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource theMusic;

    [SerializeField]
    private bool startPlaying;

    [SerializeField]
    private BeatScroller theBS;

    public static GameManager instance;

    [SerializeField]
    private Text scoreText;
    [SerializeField]
    private Text multiText;

    [SerializeField]
    private int currentScore;
    [SerializeField]
    private int scorePerNote = 100;
    [SerializeField]
    private int scorePerGoodNote = 125;
    [SerializeField]
    private int scorePerPerfactNote = 150;

    [SerializeField]
    private int currentMultiplier;
    [SerializeField]
    private int multiplierTracker;
    [SerializeField]
    private int[] multiplierThresholds;


    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        scoreText.text = "Score: 0";
        multiText.text = "Multiplier: x1";
        currentMultiplier = 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (!startPlaying)
        {
            if (Input.anyKeyDown)
            {
                startPlaying = true;
                theBS.hasStarted = true;

                theMusic.Play();
            }
        }
    }

    public void NoteHit()
    {
        //Debug.Log("Hit On Time");

        /**
         * Multi 기능은 Combo가 4, 8, 16되면 
         * currentMultiplier x2 (실제 4배)
         * currentMultiplier x3 (실제 8배)
         * currentMultiplier x4 (실제 16배) 로 적용된다.
         */
        if (currentMultiplier -1 < multiplierThresholds.Length)
        {
            multiplierTracker++;

            if(multiplierThresholds[currentMultiplier - 1] <= multiplierTracker)
            {
                multiplierTracker = 0;
                currentMultiplier++;
            }
        }

        multiText.text = "Multiplier: x" + currentMultiplier;

        currentScore += scorePerNote * currentMultiplier;
        scoreText.text = "Score: " + currentScore;
    }

    public void NormalHit()
    {
        //Debug.Log("NormalHit");
        currentScore += scorePerNote * currentMultiplier;
        NoteHit();

    }
    public void GoodHit()
    {
        //Debug.Log("GoodHit");
        currentScore += scorePerGoodNote * currentMultiplier;
        NoteHit();
    }
    public void PerfactHit()
    {
        //Debug.Log("PerfactHit");
        currentScore += scorePerPerfactNote * currentMultiplier;
        NoteHit();
    }

    public void NoteMissed()
    {
        //Debug.Log("Missed Note");
        currentMultiplier = 1;
        multiplierTracker = 0;
        multiText.text = "Multiplier: x" + currentMultiplier;
    }
}
