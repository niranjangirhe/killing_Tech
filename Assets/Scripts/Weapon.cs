
using UnityEngine;
using Photon.Pun;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Photon.Pun.Demo.SlotRacer;

public class Weapon : MonoBehaviourPunCallbacks
{
    #region Variables
    public List<Gun> loadout;
    [HideInInspector] public Gun currentGunData;
    public Transform weaponParent;
    public int currentIndex;
    private GameObject currentWeapon;
    public GameObject bulletholePrefab;
    public LayerMask canBeShot;
    public AudioSource SFX;
    private float currentCooldown;
    private Look look;
    private Image hitmarkerImage;
    private float hitmarkerWait;
    private Color ClearWhite = new Color(1, 1, 1, 0);
    [HideInInspector] public bool isReloading;
    [HideInInspector] public bool isEquiping;
    [HideInInspector] public bool isAiming;
    #endregion
    #region MonoBehaviour Callback
    private void Start()
    {
        foreach (Gun a in loadout)
            a.Initialize();
        hitmarkerImage = GameObject.Find("HUD/Hitmarker/Image").GetComponent<Image>();
        hitmarkerImage.color = ClearWhite;
        Equip(0);
        if (photonView.IsMine)
        {
            look = GetComponent<Look>();
        }
    }
    void Update()
    {
        //pause
        if (Pause.paused && photonView.IsMine)
            return;


        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha1))
        {
            photonView.RPC("Equip", RpcTarget.All, 0);
        }
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha2))
        {
            photonView.RPC("Equip", RpcTarget.All, 1);
        }
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.F))
        {
            photonView.RPC("Equip", RpcTarget.All, 2);
        }
        if (currentWeapon != null) 
        {
            if (photonView.IsMine && currentGunData.burst >=0)
            {
                //Aim(Input.GetMouseButton(1) && !(isEquiping || isReloading));
                if (currentGunData.burst != 1)
                {
                    if (Input.GetMouseButtonDown(0) && currentCooldown <= 0 && !isReloading && !isEquiping)
                    {
                        if (currentGunData.FireBullet())
                            photonView.RPC("Shoot", RpcTarget.All);
                    }
                    else if(!isReloading && currentGunData.isAmmoEmpty() && !isEquiping)
                        StartCoroutine(Reload(currentGunData.reload));
                }
                else
                {
                    if (Input.GetMouseButton(0) && currentCooldown <= 0 && !isReloading && !isEquiping)
                    {
                        if (currentGunData.FireBullet())
                            photonView.RPC("Shoot", RpcTarget.All);
                    }
                    else if (!isReloading && currentGunData.isAmmoEmpty() && !isEquiping)
                        StartCoroutine(Reload(currentGunData.reload));
                }
                if (Input.GetKeyDown(KeyCode.R) && !isReloading && !currentGunData.isAmmoFull())
                    photonView.RPC("ReloadRPC", RpcTarget.All);
                //cooldown
                if (currentCooldown > 0) currentCooldown -= Time.deltaTime;
            }
            else if (photonView.IsMine && currentGunData.burst == -1)
            {
                if (Input.GetMouseButtonDown(0) && currentCooldown <= 0  && !isEquiping)
                {
                    photonView.RPC("ReloadRPC", RpcTarget.All);
                    StartCoroutine(Knife());
                }
                if (currentCooldown > 0) currentCooldown -= Time.deltaTime;
            }
            

            //weapon position elasticity
            currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 4f);
            
        }
        if(photonView.IsMine)
        {
            if(hitmarkerWait>0)
            {
                hitmarkerWait -= Time.deltaTime;
            }
            else
            {
                hitmarkerImage.color = Color.Lerp(hitmarkerImage.color, ClearWhite, Time.deltaTime*1f);
            }

        }
    }
    #endregion
    IEnumerator Knife()
    {
        yield return new WaitForSeconds(currentGunData.firerate/2);
        photonView.RPC("Shoot", RpcTarget.All);
    }
    #region Private methods
    [PunRPC]
    private void ReloadRPC ()
    {
        StartCoroutine(Reload(currentGunData.reload));
    }
    IEnumerator Reload(float p_wait)
    {
        isReloading = true;
        //currentWeapon.gameObject.GetComponent<Animator>().enabled = true;
        currentWeapon.GetComponent<Animator>().Play("Reload", 0, 0);
        yield return new WaitForSeconds(p_wait);
        //currentWeapon.gameObject.GetComponent<Animator>().enabled = false;
        if(currentGunData.burst>=0)
            currentGunData.Reload();
        isReloading = false;

    }

    [PunRPC]
    void Equip (int p_ind)
    {
        isEquiping = true;
        if (currentWeapon != null)
        {
            if (isReloading)
                StopCoroutine("Reload");
            Destroy(currentWeapon);
        } 
        currentIndex = p_ind;
        GameObject t_newWeapon = Instantiate(loadout[p_ind].prefab, weaponParent.position,weaponParent.rotation,weaponParent) as GameObject;
        t_newWeapon.transform.localPosition = Vector3.zero;
        t_newWeapon.transform.localEulerAngles = Vector3.zero;
        t_newWeapon.GetComponent<Sway>().isMine= photonView.IsMine;

        if (photonView.IsMine)
            ChangeLayersRecursively(t_newWeapon, 10);
        else
            ChangeLayersRecursively(t_newWeapon, 0);
        
        currentWeapon = t_newWeapon;
        t_newWeapon.GetComponent<Animator>().Play("Equip",0,0);
        currentGunData = loadout[p_ind];
        Invoke("IsEquipSetter",currentGunData.equipTime);
    }
    void IsEquipSetter()
    {
        isEquiping = false;
    }
    [PunRPC]
    void PickupWeapon(string name)
    {
        if (currentIndex == 2)
            return;
        Gun newWeapon = GunLibrary.FindGun(name);
        newWeapon.Initialize();
        if(loadout.Count >= 2)
        {
            if (!GunAlready(name))
            {
                loadout[currentIndex] = newWeapon;
                loadout[currentIndex].Initialize();
                Equip(currentIndex);
            }
        }
        else
        {
            loadout.Add(newWeapon);
            loadout[loadout.Count - 1].Initialize();
            Equip(loadout.Count - 1);
        }
    }
    private bool GunAlready(string name)
    {
        foreach (Gun a in loadout)
        {
            if (a.name.Equals(name))
            {
                a.Initialize();
                return true;
            }
        }
        return false;
    }
    private void ChangeLayersRecursively(GameObject p_target, int p_layer)
    {
        p_target.layer = p_layer;
        foreach (Transform a in p_target.transform)
            ChangeLayersRecursively(a.gameObject, p_layer);
    }
    public void Aim(bool p_isAiming)
    {
        if (!currentWeapon)
            return;
        if (currentIndex == 2)
            return;
        isAiming = p_isAiming;
        Transform t_anchor = currentWeapon.transform.Find("Root");
        Transform t_state_ads = currentWeapon.transform.Find("States/ADS");
        Transform t_state_hip = currentWeapon.transform.Find("States/Hip");
        if (p_isAiming)
        {
            //aim
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_ads.position, Time.deltaTime * currentGunData.aimSpeed);
            look.gunAimSensi = currentGunData.aimSensitivity;
        }
        else
        {
            //hip
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_hip.position, Time.deltaTime * currentGunData.aimSpeed);
            look.gunAimSensi = 1;
        }

    }
    [PunRPC]
    void Shoot()
    {
        Transform t_spawn = transform.Find("Camera/NormalCamera");
        //cooldown
        currentCooldown = currentGunData.firerate;
        for (int i = 0; i < Mathf.Max(1, currentGunData.pellets); i++)
        {
            //bloom
            Vector3 t_bloom = t_spawn.position + t_spawn.forward * 1000f;
            t_bloom += Random.Range(-currentGunData.bloom, currentGunData.bloom) * t_spawn.up;
            t_bloom += Random.Range(-currentGunData.bloom, currentGunData.bloom) * t_spawn.right;
            t_bloom -= t_spawn.position;
            t_bloom.Normalize();

            //Raycast
            RaycastHit t_hit = new RaycastHit();
            if(currentGunData.burst<0)
            {
                if (Physics.Raycast(t_spawn.position, t_bloom, out t_hit, 2f, canBeShot))
                {
                    if (photonView.IsMine)
                    {
                        //shooting a player on network
                        if (t_hit.rigidbody != null)
                        {
                            if (t_hit.collider.gameObject.layer == 0)
                                t_hit.rigidbody.AddForce(-t_hit.normal * currentGunData.bulletForce);
                        }
                        if (t_hit.collider.gameObject.layer >= 11 && t_hit.collider.gameObject.layer <= 15)
                        {
                            t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, currentGunData.bodyDamage, PhotonNetwork.LocalPlayer.ActorNumber);
                            hitmarkerImage.color = Color.red;
                            hitmarkerWait = 0.5f;
                        }
                    }
                }
            }
            else if (Physics.Raycast(t_spawn.position, t_bloom, out t_hit, 1000f, canBeShot))
            {
                GameObject t_newHole = Instantiate(bulletholePrefab, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
                t_newHole.transform.LookAt(t_hit.point + t_hit.normal);
                Destroy(t_newHole, 0.5f);

                if (photonView.IsMine)
                {
                    //shooting a player on network
                    if(t_hit.rigidbody!=null)
                    {
                        Debug.Log("Here");
                        if(t_hit.collider.gameObject.layer == 0)
                            t_hit.rigidbody.AddForce(-t_hit.normal * currentGunData.bulletForce);
                    }
                    if (t_hit.collider.gameObject.layer == 11)
                    {
                        t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, currentGunData.bodyDamage, PhotonNetwork.LocalPlayer.ActorNumber);
                        hitmarkerImage.color = Color.white;
                        hitmarkerWait = 0.5f;
                    }
                    else if (t_hit.collider.gameObject.layer == 12)
                    {
                        t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, currentGunData.headDamage, PhotonNetwork.LocalPlayer.ActorNumber);
                        hitmarkerImage.color = Color.red;
                        hitmarkerWait = 0.5f;
                    }
                    else if (t_hit.collider.gameObject.layer == 13)
                    {
                        t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, currentGunData.bodyDamage, PhotonNetwork.LocalPlayer.ActorNumber);
                        hitmarkerImage.color = Color.yellow;
                        hitmarkerWait = 0.5f;
                    }
                    else if (t_hit.collider.gameObject.layer == 14)
                    {
                        t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, currentGunData.limbDamage, PhotonNetwork.LocalPlayer.ActorNumber);
                        hitmarkerImage.color = Color.white;
                        hitmarkerWait = 0.5f;
                    }
                    else if (t_hit.collider.gameObject.layer == 10)
                    {
                        t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, currentGunData.limbDamage, PhotonNetwork.LocalPlayer.ActorNumber);
                        hitmarkerImage.color = Color.white;
                        hitmarkerWait = 0.5f;
                    }
                }
            }
            if (currentGunData.burst >= 0)
            {
                GameObject imapact = Instantiate(currentGunData.impactFx, t_hit.point, Quaternion.LookRotation(t_hit.normal));
                imapact.GetComponent<ParticleSystem>().Play();
                Destroy(imapact, 2f);
            }
        }
        //muzle flash
        if (currentGunData.burst >= 0)
        {
            Transform muzleTrans = currentWeapon.transform.Find("Root/Anchor/Design/PistolDesign/MuzzleFlashPoint");
            GameObject muzleFlash = Instantiate(currentGunData.muzleFlash, muzleTrans.position, muzleTrans.rotation);
            GameObject sparks = Instantiate(currentGunData.sparks, muzleTrans.position, muzleTrans.rotation);
            GameObject pointLight = Instantiate(currentGunData.pointLight, muzleTrans.position, muzleTrans.rotation);
            muzleFlash.GetComponent<ParticleSystem>().Play();
            sparks.GetComponent<ParticleSystem>().Play();
            Destroy(pointLight, 0.1f);
            Destroy(muzleFlash, 2f);
            Destroy(sparks, 2f);
        }
        //sound
        SFX.Stop();
        SFX.clip = currentGunData.gunshotSound;
        SFX.pitch = 1 - currentGunData.pitchRandomization + Random.Range(-currentGunData.pitchRandomization, currentGunData.pitchRandomization);
        SFX.volume = currentGunData.gunshootVolume;
        SFX.Play();

        //gun fx
        float t_adj_recoil = 1f;
        if (isAiming)
            t_adj_recoil = 0.5f;
        currentWeapon.transform.Rotate(-currentGunData.recoil * t_adj_recoil, 0, 0);
        currentWeapon.transform.position -= currentWeapon.transform.forward * currentGunData.kickback * t_adj_recoil;
        if (currentGunData.recovery)
            currentWeapon.GetComponent<Animator>().Play("Recovery", 0, 0);
        
    }
    [PunRPC]
    private void TakeDamage(int p_damage, int p_actor)
    {
        GetComponent<Player>().TakeDamage(p_damage, p_actor);
    }
    #endregion
    #region Public methods

    public void RefreshAmmo (Text p_text)
    {
        int t_clip = loadout[currentIndex].GetClip();
        int t_stash = loadout[currentIndex].GetStash();

        p_text.text = t_clip.ToString("D2") + " / " + t_stash.ToString("D2");
    }

    #endregion

}
