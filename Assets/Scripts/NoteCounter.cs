using UnityEngine;
public class NoteCounter : MonoBehaviour
{
	public NumberRenderer[] numberRenderer;
	public GameObject musicNote;

	public int score = 0;
	public int good = 0;
	public int perfect = 0;
	public int miss = 0;
	public int comboCount = 0;
	public int maxComboCount = 0;

	private int[] show = new int[] { 0, 0, 0, 0 };
	private int[] previousFrame = new int[] { 0, 0, 0, 0 };
	public float showCounterAnimation = 0;
	public Vector3 hidePosition, showPosition;
	public void Initialize()
	{
		score = 0;
		for (int i = 0; i < 4; ++i)
		{
			numberRenderer[i].time = 1;
		}
		gameObject.SetActive(false);
	}
	public void UpdateCounter()
	{
		show[3] = score % 10;
		show[2] = (score / 10) % 10;
		show[1] = (score / 100) % 10;
		show[0] = (score / 1000) % 10;
		musicNote.SetActive(score < 1000);
		for (int i = 0; i < 4; ++i)
		{
			if (show[i] != previousFrame[i]) numberRenderer[i].time = 0.5f;
			numberRenderer[i].mySpriteRenderer.sprite = numberRenderer[i].number[show[i]];
			previousFrame[i] = show[i];
			numberRenderer[i].UpdateNumber();
		}

		// 27을 -1로 수정
		if (score > -1)
		{
			showCounterAnimation = Mathf.Min(showCounterAnimation + (Time.deltaTime*2f), 1);
			//transform.localPosition = Vector3.LerpUnclamped(hidePosition, showPosition, elastic(showCounterAnimation));
			gameObject.SetActive(true);
		}
		else
		{
			showCounterAnimation = Mathf.Max(showCounterAnimation - (Time.deltaTime*3f), 0);
			//transform.localPosition = Vector3.LerpUnclamped(hidePosition, showPosition, BackEaseOut(showCounterAnimation));
			if(showCounterAnimation==0) gameObject.SetActive(false);
			//showCounterAnimation = Mathf.Max(showCounterAnimation - Time.deltaTime, 0f);
		}
		
	}
	static public float elastic(float p)
	{
		return Mathf.Sin(-13 * Mathf.PI*0.5f * (p + 1)) * Mathf.Pow(2, -10 * p) + 1;
	}

	static public float BackEaseOut(float p)
	{
		float f = (1 - p);
		return 1 - (f * f * f - f * Mathf.Sin(f * Mathf.PI));
	}

	public void clear()
    {
		score = 0;
		good = 0;
		perfect = 0;
		miss = 0;
		comboCount = 0;
		maxComboCount = 0;
	}
}
