using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResultScore : MonoBehaviour
{
    [SerializeField] GameObject goUI = null;
    [SerializeField] Text[] textCount = null;
    [SerializeField] Text textScore = null;
    [SerializeField] Text textMaxScore = null;

    NoteCounter noteCounter;

    // Start is called before the first frame update
    void Start()
    {
        noteCounter = FindObjectOfType<NoteCounter>();
    }

    public void ShowResult()
    {
        goUI.SetActive(true);
        for (int i = 0; i < textCount.Length; i++)
            textCount[i].text = noteCounter.miss + "";

        textScore.text = noteCounter.score + "";
        textMaxScore.text = noteCounter.maxComboCount + "";
    }
}
