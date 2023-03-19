using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionEvent : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material.color = Color.white;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Destroy(collision.gameObject);
    }

    private void OnCollisionExit(Collision collision)
    {
        //meshRenderer.material.color = Color.white;
    }
}
