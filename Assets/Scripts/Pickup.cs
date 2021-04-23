using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Pickup : MonoBehaviourPunCallbacks
{
    public Gun weapon;
    public float cooldown;
    private float wait;
    private bool isDisabled;
    public List<GameObject> targets;
    public GameObject gunDisplay;
    private float rotate;
    private void Start()
    {
        foreach (Transform t in gunDisplay.transform) Destroy(t.gameObject);
        GameObject newDisplay = Instantiate(weapon.display, gunDisplay.transform.position, gunDisplay.transform.rotation) as GameObject;
        newDisplay.transform.SetParent(gunDisplay.transform);
    }
    private void Update()
    {
        if (isDisabled)
        {
            if (wait >= 0)
            {
                wait -= Time.deltaTime;
            }
            else
            {
                Enable();
            }
        }
        transform.Rotate(Vector3.up*Time.deltaTime*50);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null)
            return;
        if(other.gameObject.layer==9)
        {
            Weapon weaponController = other.attachedRigidbody.gameObject.GetComponent<Weapon>();
            weaponController.photonView.RPC("PickupWeapon", RpcTarget.All, weapon.name);
            photonView.RPC("Disable", RpcTarget.All);
        }
    }
    [PunRPC]
    public void Disable()
    {
        wait = cooldown;
        isDisabled = true;
        foreach (GameObject a in targets)
            a.SetActive(false);
    }
    private void Enable()
    {
        isDisabled = false;
        wait = 0;
        foreach (GameObject a in targets)
            a.SetActive(true);
    }
}
