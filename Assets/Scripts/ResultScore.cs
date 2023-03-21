using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;


public class ResultScore : MonoBehaviour
{
    [SerializeField] Text[] textCount = null;
    [SerializeField] Text textScore = null;
    [SerializeField] Text textMaxScore = null;
    public RawImage fade;

    public Session session;

    public Button moveMainMenuBtn, retryBtn;

    public SongSelect songSelect;

    private void Start()
    {
        moveMainMenuBtn.onClick.AddListener(moveMain);
        retryBtn.onClick.AddListener(retryGameBtn);
    }

    private void Update()
    {
        //Debug.Log("update 동작");
    }

    public void ShowResult()
    {

        /**
         * fade로 화면 가리기
         */
        fade.gameObject.SetActive(true);
        fade.color = new Color(0, 0, 0, 1);
        
        /**
         * 결과창 렌더링 및 활성화
         */
        NoteCounter noteCounter = session.players[0].noteCounter;
        for (int i = 0; i < textCount.Length; i++)
        {
            if ("PerfectCountTxt".Equals(textCount[i].name))
            {
                textCount[i].text = noteCounter.perfect + "";
            }
            else if ("GoodCountTxt".Equals(textCount[i].name))
            {
                textCount[i].text = noteCounter.good + "";
            }
            else if ("MissCountTxt".Equals(textCount[i].name))
            {
                textCount[i].text = noteCounter.miss + "";
            }
        }

        textScore.text = noteCounter.score + "";
        textMaxScore.text = noteCounter.maxComboCount + "";
        gameObject.SetActive(true);

        /**
         * Fade 활성화 및 점점 투명하게 작업
         */
        StartCoroutine(FadeOutStart(() => {
            Debug.Log("콜백동작");
        }));
    }

    private IEnumerator FadeOutStart(Action callback)
    {
        fade.color = new Color(0, 0, 0, 1);
        while (fade.color.a > 0)
        {
            // 다음 프레임 동작 되기 전까지 대기
            yield return null;
            fade.color -= new Color(0, 0, 0, Time.deltaTime);
        }
        fade.gameObject.SetActive(false);
        callback();
    }

    private IEnumerator FadeInStart(Action callback)
    {
        fade.color = new Color(0, 0, 0, 0);
        while (fade.color.a < 1)
        {
            // 다음 프레임 동작 되기 전까지 대기
            yield return null;
            fade.color += new Color(0, 0, 0, Time.deltaTime);
        }
        callback();
    }

    public void moveMain()
    {
        /**
         * 결과창 Fade 점점 어둡게
         */
        fade.gameObject.SetActive(true);
        StartCoroutine(FadeInStart(() => {
            /**
		     * 결과창 비활성화
		     */
            gameObject.SetActive(false);

            /**
             * 노래 선택창 활성화
             */
            songSelect.activateScreen();
        }));

        
    }

    public void retryGameBtn()
    {
        /**
         * 결과창 Fade 점점 어둡게
         */
        fade.gameObject.SetActive(true);
        StartCoroutine(FadeInStart(() => {
            /**
             * 결과창 비활성화
             */
            gameObject.SetActive(false);

            /**
             * 노래 선택창 비활성화
             */
            songSelect.inActivateScreen();

            /**
             * 다시시작
             */
            Session.songBlock.Play();
        }));

        
    }
}
