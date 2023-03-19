using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement2D : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 5.0f;

    private void Update()
    {
        // 새로운 위치 = 현재 위치 + (방향 * 속도)
        //transform.position = transform.position + new Vector3(1, 0, 0) * 1;
        transform.position += Vector3.down * moveSpeed * Time.deltaTime;
    }
}
