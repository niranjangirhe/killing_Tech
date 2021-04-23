
using UnityEngine;
[CreateAssetMenu(fileName ="New Gun", menuName = "Gun")]
public class Gun : ScriptableObject
{
    public new string name;
    public int bodyDamage;
    public int headDamage;
    public int limbDamage;
    public float firerate;
    public float aimSpeed;
    public float recoil;
    public float bloom;
    public float kickback;
    public float bulletForce;
    public GameObject muzleFlash;
    public GameObject sparks;
    public GameObject impactFx;
    public GameObject pointLight;
    public int ammo;
    public float equipTime;
    public int clipSize;
    public float reload;
    public float aimSensitivity;
    [Range(0, 1)] public float mainFOV;
    [Range(0, 1)] public float weapoFOV;
    public AudioClip gunshotSound;
    public float gunshootVolume;
    public float pitchRandomization;
    public int burst; //0 semi | 1 aout | 2+ burst fire
    public int pellets;
    public bool recovery;
    public GameObject prefab;
    public GameObject display;


    private int clip;
    private int stash;
    public void Initialize()
    {
        stash = ammo;
        clip = clipSize;
    }
    public bool FireBullet()
    {
        if (clip > 0)
        {
            clip -= 1;
            return true;
        }
        else
            return false;
    }
    public bool isAmmoEmpty()
    {
        if(clip<=0)
            return true;
        else
            return false;
    }
    public bool isAmmoFull()
    {
        if (clip == clipSize)
            return true;
        else
            return false;
    }
    public void Reload()
    {
        stash += clip;
        clip = Mathf.Min(clipSize, stash);
        stash -= clip;
    }
    public int GetStash()
    {
        return stash;
    }
    public int GetClip()
    {
        return clip;
    }
}
