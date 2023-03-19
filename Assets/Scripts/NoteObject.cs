using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteObject : MonoBehaviour
{
    [SerializeField]
    private bool canBePressed;

    [SerializeField]
    private KeyCode keyToPress;

    [SerializeField]
    private GameObject hitEffect;

    bool hasExited = false;

    // Start is called before the first frame update
    void Start()
    {
        //Debug.Log("[NoteObject] keyToPress:" + keyToPress);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(keyToPress))
        {
            if (canBePressed)
            {
                gameObject.SetActive(false);

                //GameManager.instance.NoteHit();
                if((transform.position.y <= -8.81 && transform.position.y >= -8.95) 
                    ||(transform.position.y <= -9.46 && transform.position.y >= -9.55))
                {
                    GameManager.instance.NormalHit();
                    Instantiate(hitEffect, transform.position, hitEffect.transform.rotation);
                }else if (transform.position.y <= -8.96 && transform.position.y >= -9.06 
                    || (transform.position.y <= -9.27 && transform.position.y >= -9.46))
                {
                    GameManager.instance.GoodHit();
                    Instantiate(hitEffect, transform.position, hitEffect.transform.rotation);
                }
                else if (transform.position.y <= -9.07 && transform.position.y >= -9.26)
                {
                    GameManager.instance.PerfactHit();
                    Instantiate(hitEffect, transform.position, hitEffect.transform.rotation);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("[NoteObject] OnTriggerEnter 동작");
        if(other.tag == "Activator")
        {
            canBePressed = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {

        //Debug.Log("[NoteObject] OnTriggerExit 동작");
        if (other.tag == "Activator")
        {
            canBePressed = false;

            if (!hasExited)
            {
                hasExited = true;
                GameManager.instance.NoteMissed();
            }
        }

    }
}
