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

		/**
		 * Canvas => Group을 변경하는 곳이긴 한데...
		 * 화면이 틀어져서 일단 삭제...
		 * 정확히 무엇을 뜻하는지는 잘 모르겠다...
		 */
		//RectTransform myRect = (RectTransform) transform;
		//Vector2 groupSizeDelta = group.sizeDelta;
		//
		//groupSizeDelta.x = myRect.sizeDelta.x;
		//groupSizeDelta.y = myRect.sizeDelta.x / textures.Length;

		//if (groupSizeDelta.y > myRect.sizeDelta.y)
		//{
		//	groupSizeDelta *= myRect.sizeDelta.y / groupSizeDelta.y;
		//}
		//group.sizeDelta = groupSizeDelta;
	}
}