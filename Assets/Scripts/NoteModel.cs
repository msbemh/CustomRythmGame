using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteModel : MonoBehaviour
{
	public Transform myTransform;
	public SpriteRenderer spriteRenderer;
	public MeshRenderer line;
	public Material materialInstance;
    public PlayerInput playerInput;

    //[SerializeField]
    //private bool canBePressed = false;

    //private void OnTriggerEnter(Collider other)
    //{
    //    Debug.Log("[NoteObject] OnTriggerEnter 동작");
    //    if (other.tag == "Activator")
    //    {
    //        canBePressed = true;
    //    }
    //}

    //private void OnTriggerExit(Collider other)
    //{

    //    Debug.Log("[NoteObject] OnTriggerExit 동작");
    //    if (other.tag == "Activator")
    //    {
    //        canBePressed = false;
    //    }

    //}


 //   private void Update()
 //   {
	//	if (Input.GetKeyDown(KeyCode.S))
 //       {
 //           if (canBePressed)
 //           {
 //               //for (int i = 0; i < nextLine.note.Count; i++)
 //               //{
 //               //    NoteInstance noteInstance = nextLine.note[i];
 //               //    uint fred = noteInstance.fred;
 //               //    if (nextLine.fred[fred] != playerInput.fred[fred])
 //               //    {
 //               //        correctColors = false;

 //               //    }
 //               //    //correctColors &= (playerInput.fred[i] == nextLine.note[i].fred);
 //               //}
 //               //gameObject.SetActive(false);
 //           }  
 //       }
	//}

    public void SetLengt(float meters)
	{
        /**
		 * localPosition : 부모 오브젝트의 좌표계를 기준으로 해당 오브젝트의 위치를 나타냄.
		 * localPos : 해당 오브젝트의 크기를 나타냅니다.
		 */
        if (line != null)
        {
			Vector3 localPos = line.transform.localPosition;
			localPos.z = meters * 0.5f;
			line.transform.localPosition = localPos;

			Vector3 localScale = line.transform.localScale;
			localScale.y = meters;
			line.transform.localScale = localScale;

			materialInstance.mainTextureScale = new Vector2(1, localScale.y / localScale.x);
			materialInstance.mainTextureOffset = new Vector2(0, -localScale.y / localScale.x + 1);

        }
	}
}
