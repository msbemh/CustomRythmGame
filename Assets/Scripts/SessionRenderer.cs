using UnityEngine;
using UnityEngine.UI;
/**
 * Canvas에 장착되어져 있음
 */
public class SessionRenderer : MonoBehaviour
{
	public RectTransform[] outputs;
	public RectTransform group;
	public void Initialize(RenderTexture[] textures)
	{
		/**
		 * 생성된 texture에 맞게 output을 활성화
		 */
		for (int i = 0; i < outputs.Length; ++i)
		{
			outputs[i].gameObject.SetActive(i < textures.Length);
		}
		/**
		 * Output의 RawImage에 texture를 삽입
		 */
		for (int i = 0; i < textures.Length; ++i)
		{
			outputs[i].GetComponent<RawImage>().texture = textures[i];
		}

		RectTransform myRect = (RectTransform)transform;
		Vector2 groupSizeDelta = group.sizeDelta;

		groupSizeDelta.x = myRect.sizeDelta.x;
		groupSizeDelta.y = myRect.sizeDelta.x / textures.Length;
		/**
		 * Group 사이즈 보다 outPut 사이즈가 크다면
		 * Group 사이즈를 높인다. 
		 */
		if (groupSizeDelta.y > myRect.sizeDelta.y)
		{
			groupSizeDelta *= myRect.sizeDelta.y / groupSizeDelta.y;
		}
		group.sizeDelta = groupSizeDelta;
	}
}