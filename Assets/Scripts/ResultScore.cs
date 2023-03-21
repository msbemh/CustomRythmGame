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
        //Debug.Log("update ����");
    }

    public void ShowResult()
    {

        /**
         * fade�� ȭ�� ������
         */
        fade.gameObject.SetActive(true);
        fade.color = new Color(0, 0, 0, 1);
        
        /**
         * ���â ������ �� Ȱ��ȭ
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
         * Fade Ȱ��ȭ �� ���� �����ϰ� �۾�
         */
        StartCoroutine(FadeOutStart(() => {
            Debug.Log("�ݹ鵿��");
        }));
    }

    private IEnumerator FadeOutStart(Action callback)
    {
        fade.color = new Color(0, 0, 0, 1);
        while (fade.color.a > 0)
        {
            // ���� ������ ���� �Ǳ� ������ ���
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
            // ���� ������ ���� �Ǳ� ������ ���
            yield return null;
            fade.color += new Color(0, 0, 0, Time.deltaTime);
        }
        callback();
    }

    public void moveMain()
    {
        /**
         * ���â Fade ���� ��Ӱ�
         */
        fade.gameObject.SetActive(true);
        StartCoroutine(FadeInStart(() => {
            /**
		     * ���â ��Ȱ��ȭ
		     */
            gameObject.SetActive(false);

            /**
             * �뷡 ����â Ȱ��ȭ
             */
            songSelect.activateScreen();
        }));

        
    }

    public void retryGameBtn()
    {
        /**
         * ���â Fade ���� ��Ӱ�
         */
        fade.gameObject.SetActive(true);
        StartCoroutine(FadeInStart(() => {
            /**
             * ���â ��Ȱ��ȭ
             */
            gameObject.SetActive(false);

            /**
             * �뷡 ����â ��Ȱ��ȭ
             */
            songSelect.inActivateScreen();

            /**
             * �ٽý���
             */
            Session.songBlock.Play();
        }));

        
    }
}
