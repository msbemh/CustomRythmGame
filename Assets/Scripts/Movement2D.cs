using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement2D : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 5.0f;

    private void Update()
    {
        // ���ο� ��ġ = ���� ��ġ + (���� * �ӵ�)
        //transform.position = transform.position + new Vector3(1, 0, 0) * 1;
        transform.position += Vector3.down * moveSpeed * Time.deltaTime;
    }
}
