using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JudgeText : MonoBehaviour
{
    public static JudgeText instance;

    float time;
    public float _fadeTime = 1f;
    private bool fadeOut;

    void Start()
    {
        instance = this;
        gameObject.SetActive(true);
        GetComponent<Text>().color = new Color(1, 1, 1, 0f);
        fadeOut = false;
  
    }

    void Update()
    {
        // ���� ��ο���
        if (fadeOut)
        {
            if (time < _fadeTime)
            {
                /**
                 * ���� ��ȭ
                 */
                Color color = GetComponent<Text>().color;
                GetComponent<Text>().color = new Color(color.r, color.g, color.b, 1f - time / _fadeTime);

                /**
                 * ��ġ ��ȭ
                 */
                GetComponent<RectTransform>().offsetMin = new Vector2(80 * time, 80 * time);

            }
            else
            {
                time = 0;
                fadeOut = false;
                GetComponent<Text>().color = new Color(1, 1, 1, 0f);
            }
            // ��Ÿ��><
            time += Time.deltaTime;
        }
    }


    public void resetAnim(string judgeStr, string color = "white")
    {
        Color textColor = Color.white;

        if ("white".Equals(color))
        {
            textColor = Color.white;
        }
        else if ("red".Equals(color))
        {
            textColor = Color.red; 
        }

        GetComponent<Text>().text = judgeStr;

        /**
         * �� �ʱ�ȭ(�������ϰ�)
         */
        fadeOut = true;
        GetComponent<Text>().color = textColor;
        time = 0;

        /**
         * ��ġ �ʱ�ȭ
         */
        GetComponent<RectTransform>().offsetMin = new Vector2(0, 0);
    }
}
