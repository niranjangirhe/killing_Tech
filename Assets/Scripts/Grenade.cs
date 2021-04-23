using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Grenade : MonoBehaviourPunCallbacks
{
    public float Delay = 3f;
    public GameObject Explosion;
    public GameObject Design;
    public Rigidbody rb;
    public float blastRadius = 15f;
    private float DamageModifier;
    public float explosionForce = 700f;
    public float pitchRandomization;
    public float soundoffset;
    bool alreadyPlayed = false;
    float counter;
    public int killer=-1;
    bool alreadyKaboom = false;
    bool alreadyDistroied = false;
    public float grenadeThrow;
    public AudioSource AudioS;
       
    // Start is called before the first frame update
    void Start()
    {
        DamageModifier = 2500/(blastRadius * blastRadius);
        counter = Delay;
        if(photonView.IsMine)
            rb.AddForce(transform.forward * grenadeThrow, ForceMode.VelocityChange);
    }

    // Update is called once per frame
    void Update()
    {
      
        counter -= Time.deltaTime;
        if (counter <= 0f && !alreadyKaboom)
        {
            Kaboom();
        }
        if(counter <= soundoffset && !alreadyPlayed)
        {
            Sound();
        }
        if(counter <= -5f && !alreadyDistroied)
        {
            DestoryGrenade();
        }
            


    }
    [PunRPC]
    public void Killer(int k)
    {
        killer = k;
    }
    void Sound()
    {
        AudioS.pitch = 1 - pitchRandomization + Random.Range(-pitchRandomization, pitchRandomization);
        AudioS.Play();
        Destroy(AudioS, 4.5f);
        alreadyPlayed = true;
    }
    void DestoryGrenade()
    {
        alreadyDistroied = true;
        if (photonView.IsMine)
            PhotonNetwork.Destroy(gameObject);
        
    }
    void Kaboom()
    {
        alreadyKaboom = true;
        Instantiate(Explosion, transform.position, transform.rotation);
        ParticleSystem ps = Explosion.GetComponent<ParticleSystem>();
        Design.SetActive(false);
        Collider[] colliders = Physics.OverlapSphere(transform.position, blastRadius);
        foreach (Collider victims in colliders)
        {
           
            Rigidbody rb = victims.GetComponent<Rigidbody>();
            if(rb!=null)
            {
                if(victims.name[0]=='P')
                {
                    
                    Player p = victims.GetComponent<Player>();
                   
                    if(p.photonView.IsMine)
                    {
                        if (killer == p.photonView.OwnerActorNr)
                            killer = -1;
                        Vector3 temp = transform.position - victims.transform.position;
                        p.TakeDamage(Mathf.Min(1000,(int)(DamageModifier* Mathf.Pow((blastRadius - temp.magnitude), 2))), killer);
                    }
                    
                }  
                else if(photonView.IsMine)
                    rb.AddExplosionForce(explosionForce, transform.position, blastRadius);
            }
        }
        
    }
}
