using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Variables
    public Transform grenadePoint;
    public float speed;
    public float crouchModifier;
    public float sprintModifier;
    public float sprintFOVModifier;
    public float crouchAmount;
    public float slideAmount;   //cam down amount
    public GameObject BodyCollider;
    public GameObject HeadCollider;
    public GameObject LimbCollider;
    public GameObject crouchingCollider;
    public TextMeshPro playerUsername;
    Rigidbody rig;
    public Camera normalCam;
    public Camera weaponCam;
    public GameObject cameraParent;
    public Transform weaponParent;
    private Vector3 weaponParentCurrentPos;
    public float jumpForce;
    public float jetAcc;
    public float GravityAcc;
    public float terminalVel;
    public float jetWait;
    public float jetRecovery;
    public float max_fuel;
    public float healthRecoveryTime;
    public static float canY = 450;
    public int healthRecoveryAmt;
    float baseFOV;
    public Transform groundDetector;
    public LayerMask ground;
    private Vector3 targetWeaponBobPosition;
    private float movementCounter, idleCounter;
    public int max_health;
    private int current_health;
    private Manager manager;
    private Weapon weapon;
    private Transform ui_healthbar;
    private RawImage ui_blood;
    private Transform ui_fuelbar;
    private Text ui_username;
    private Text ui_ammo;
    public float lengthOfSlide;
    private float slide_time;
    private bool sliding;
    private bool crouched;
    private Vector3 slide_dir;
    public float slideModifier;
    private Vector3 origin;
    private float aimAngle;
    private float camAngle;
    private bool isAiming;
    private float current_fuel;
    private float current_recovery;
    public AudioSource sfxWalk;
    public AudioClip[] movementSound;
    public GameObject mesh;
    private int soundIndex;
    private bool jump;
    private bool canjet;
    private Animator anim;
    private float healthTimer = 0f;
    private bool healthTimerBool = false;
    private float alphaCol;
    public GameObject boom;
    [HideInInspector]public ProfileData playerProfile;
    Vector2 hitPos;
    #endregion
    #region MonoBehaviour Callback
    [System.Obsolete]
    void Start()
    {
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        weapon = GetComponent<Weapon>();
        current_health = max_health;
        current_fuel = max_fuel;
        cameraParent.SetActive(photonView.IsMine);
        if (!photonView.IsMine)
        {
            ChangeLayerRecurseively(mesh.transform,11);
            gameObject.layer = 11;
            BodyCollider.layer = 13;
            HeadCollider.layer = 12;
            LimbCollider.layer = 14;
            crouchingCollider.layer = 11;
        }
        baseFOV = normalCam.fieldOfView;
        origin = normalCam.transform.localPosition;

        rig = GetComponent<Rigidbody>();
        weaponParentCurrentPos = weaponParent.localPosition;
        if (photonView.IsMine)
        {
            weaponCam.gameObject.SetActive(false);
            ui_healthbar = GameObject.Find("HUD/Health/Bar").transform;
            ui_blood = GameObject.Find("HUD/Blood/Blood").GetComponent<RawImage>();
            ui_fuelbar = GameObject.Find("HUD/Fuel/Bar").transform;
            ui_ammo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
            ui_username = GameObject.Find("HUD/Username/Text").GetComponent<Text>();
            if (ui_blood.color.a > 0)
                TakeDamage(1, -1);
            RefreshHealthBar();
            ui_username.text = Launcher.myProfile.username;
            photonView.RPC("SyncProfile", RpcTarget.All, Launcher.myProfile.username, Launcher.myProfile.xp, Launcher.myProfile.level);
            anim = GetComponent<Animator>();
            weaponCam.gameObject.SetActive(true);
        }
    }
    
    void Update()
    {
        if (!photonView.IsMine)
        {
            RefreshMultiplayerState();
            return;
        }

        //greanade
        if(Input.GetKeyDown(KeyCode.G))
        {
            ThrowGreanade();
        }
        
        //Axis
        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");
        //control
        jump = Input.GetKeyDown(KeyCode.Space);
        bool sprint = Input.GetKey(KeyCode.LeftShift);
        bool crouch = Input.GetKeyDown(KeyCode.LeftControl);
        bool pause = Input.GetKeyDown(KeyCode.Escape);

        //States
        bool isGrounded = Physics.CheckSphere(groundDetector.position, 0.2f, ground) && !canjet; 

        //Raycast(groundDetector.position, Vector3.down, 0.2f, ground);
        bool isJumping = jump && isGrounded;
        bool isSprinting = sprint && t_vmove > 0 && !isJumping && !Input.GetMouseButton(1) && isGrounded;
        bool isCrouching = crouch && !isSprinting && !isJumping && isGrounded;

        

        //Pause
        if (pause)
        {
            GameObject.Find("Pause").GetComponent<Pause>().TogglePause();
        }
        if(Pause.paused)
        {
            t_hmove = 0f;
            t_vmove = 0f;
            sprint = false;
            jump = false;
            crouch = false;
            pause = false;
            isGrounded = false;
            isJumping = false;
            isSprinting = false;
            isCrouching = false;
        }

        //Crouching 
        if(isCrouching)
        {
            photonView.RPC("SetCrouch", RpcTarget.All, !crouched);
        }

        //Jumping
        if (isJumping)
        {
            if (crouched)
                photonView.RPC("SetCrouch", RpcTarget.All, false);
            rig.AddForce(Vector3.up * jumpForce,ForceMode.Impulse);
            current_recovery = 0;
            if (sliding)
                slide_time = 0;
        }


        //Take damage
        if (Input.GetKeyDown(KeyCode.U))
            TakeDamage(100,-1);



        //Head Bob
        if(!isGrounded)
        {
            HeadBob(movementCounter, 0.08f, 0.035f);
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, weaponParentCurrentPos, Time.deltaTime * 2f);
            if(!canjet)
                photonView.RPC("WalkSound", RpcTarget.All, movementSound.Length);
        }
        else if(sliding)
        {
            //sliding
            HeadBob(movementCounter, 0.08f, 0.035f);
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, weaponParentCurrentPos, Time.deltaTime * 8f);
            photonView.RPC("WalkSound", RpcTarget.All, movementSound.Length);
        }
        else if (t_hmove == 0 && t_vmove == 0)
        {
            //idling
            HeadBob(idleCounter*2f, 0.01f, 0.02f);
            idleCounter += Time.deltaTime;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
            photonView.RPC("WalkSound", RpcTarget.All, movementSound.Length);

        }
        else if(isSprinting)
        {
            //sprinting
            HeadBob(movementCounter*12, 0.15f , 0.035f);
            movementCounter += Time.deltaTime;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f);
            if (!sfxWalk.isPlaying || soundIndex != 1)
            {
                soundIndex = 1;
                photonView.RPC("WalkSound", RpcTarget.All, soundIndex);
            }
        }
        else if(crouched)
        {
            //crouched
            HeadBob(movementCounter * 4.5f, 0.008f, 0.013f);
            movementCounter += Time.deltaTime;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 8f);
            photonView.RPC("WalkSound", RpcTarget.All, movementSound.Length);
        }
        else
        {
            //walking

            //sound      
            if (!sfxWalk.isPlaying  || soundIndex != 0)
            {
                soundIndex = 0;
                photonView.RPC("WalkSound", RpcTarget.All, soundIndex);
            }
            //movement
            HeadBob(movementCounter*9, 0.02f, 0.04f);
            movementCounter += Time.deltaTime;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 8f);
        }
        //refresh UI
        RefreshHealthBar();
        weapon.RefreshAmmo(ui_ammo);

        //health Recovery
        if (healthTimerBool)
            healthTimer = Mathf.Min(healthRecoveryTime, healthTimer + Time.deltaTime);
        if (healthTimer == healthRecoveryTime)
            TakeDamage(-healthRecoveryAmt, -2);


    }
    void FixedUpdate()
    {
        if (!photonView.IsMine) return;
        //Axis
        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");


        //Controls
        bool sprint = Input.GetKey(KeyCode.LeftShift);
        bool slide = Input.GetMouseButton(2);
        bool aim = Input.GetMouseButton(1) && weapon.currentIndex !=2;
        bool jet = Input.GetKey(KeyCode.Q);
        


        //State
        bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.2f, ground) && !canjet;
        bool isJumping = jump && isGrounded;
        bool isSprinting = sprint && t_vmove > 0 && !isJumping && !aim;
        bool isSliding = isSprinting && slide && !sliding && isGrounded;
        isAiming = aim && !weapon.isEquiping && !weapon.isReloading;

        //Gravity
        Vector3 Gravity = rig.velocity;
        Gravity.y =Mathf.Max(terminalVel ,Gravity.y + GravityAcc * Time.fixedDeltaTime); 
        if (isGrounded)
            Gravity.y = 0f;
        rig.velocity = Gravity;

        //pause
        if (Pause.paused)
        {
            t_hmove = 0f;
            t_vmove = 0f;
            sprint = false;
            jump = false;
            crouched = false;
            isGrounded = false;
            isJumping = false;
            isSprinting = false;
            isAiming = false;
        }



        //Aiming
        weapon.Aim(isAiming);

        //camera stuff && Field Of view
        float t_adjustSpeed = speed;
        
        if (sliding)
        {
            t_adjustSpeed *= slideModifier;
            normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * slideModifier * 0.9f, Time.deltaTime * 4f);
            normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime * 6f);
            weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * slideModifier * 0.9f, Time.deltaTime * 4f);
            weaponCam.transform.localPosition = Vector3.Lerp(weaponCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime * 6f);
        }
        else
        {
            if (isSprinting)
            {
                t_adjustSpeed *= sprintModifier;
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier, Time.deltaTime * 4f);
                normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin, Time.deltaTime * 6f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * sprintFOVModifier, Time.deltaTime * 4f);
                weaponCam.transform.localPosition = Vector3.Lerp(weaponCam.transform.localPosition, origin, Time.deltaTime * 6f);
            }
            else if(isAiming)
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * weapon.currentGunData.mainFOV, Time.deltaTime * 8f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * weapon.currentGunData.weapoFOV, Time.deltaTime * 8f);
            }
            else if (crouched)
            {
                t_adjustSpeed *= crouchModifier;
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * crouchModifier, Time.deltaTime * 4f);
                normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime * 6f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * crouchModifier, Time.deltaTime * 4f);
                weaponCam.transform.localPosition = Vector3.Lerp(weaponCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime * 6f);
            }
            else if (t_vmove > 0)
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier * 0.825f, Time.deltaTime * 4f);
                normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin, Time.deltaTime * 6f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * sprintFOVModifier * 0.825f, Time.deltaTime * 4f);
                weaponCam.transform.localPosition = Vector3.Lerp(weaponCam.transform.localPosition, origin, Time.deltaTime * 6f);
            }
            else
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV, Time.deltaTime * 4f);
                normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin, Time.deltaTime * 6f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV, Time.deltaTime * 4f);
                weaponCam.transform.localPosition = Vector3.Lerp(weaponCam.transform.localPosition, origin, Time.deltaTime * 6f);
            }
        }









        //Movement
        Vector3 t_direction = Vector3.zero;
        if (!sliding)
        {
            t_direction = new Vector3(t_hmove, 0f, t_vmove);
            t_direction.Normalize();
            t_direction = transform.TransformDirection(t_direction);

            if(isSprinting)
            {
                if(crouched)
                    photonView.RPC("SetCrouch", RpcTarget.All, false);
            }
        }
        else
        {
            t_direction = slide_dir;
            slide_time -= Time.deltaTime;
            if (!isGrounded)
                slide_time -= Time.deltaTime * 5f;
            if(slide_time <= 0)
            {
                sliding = false;
                weaponParentCurrentPos += Vector3.up * (slideAmount-crouchAmount);
            }
        }

        Vector3 targetVelocity = t_direction * t_adjustSpeed * Time.deltaTime;
        targetVelocity.y = rig.velocity.y;
        rig.velocity = targetVelocity;

        //sliding
        if (isSliding)
        {
            sliding = true;
            slide_dir = t_direction;
            slide_time = lengthOfSlide;
            weaponParentCurrentPos += Vector3.down * (slideAmount - crouchAmount);
            if(!crouched)
                photonView.RPC("SetCrouch", RpcTarget.All, true);
        }

        //jetting
        canjet = jet && current_fuel > 0;
        if(canjet)
        {
            current_recovery = 0;
            Vector3 jetVelocity = rig.velocity;
            jetVelocity.y += jetAcc*Time.fixedDeltaTime;
            rig.velocity = jetVelocity;
            current_fuel = Mathf.Max(0, current_fuel - Time.fixedDeltaTime);
            if (!sfxWalk.isPlaying || soundIndex != 2)
            {
                soundIndex = 2;
                photonView.RPC("WalkSound", RpcTarget.All, soundIndex);
            }  
        }
        if(isGrounded)
        {
            if (current_recovery < jetWait)
                current_recovery = Mathf.Min(jetWait, current_recovery + Time.fixedDeltaTime);
            else
                current_fuel = Mathf.Min(max_fuel, current_fuel + Time.fixedDeltaTime * jetRecovery);
        }
        ui_fuelbar.localScale = new Vector3(current_fuel / max_fuel, 1, 1);



        //animation
        float t_anim_horizontal = 0f;
        float t_anim_vertical = 0f;
        if (isGrounded)
        {
            t_anim_horizontal = t_direction.z;
            t_anim_vertical = t_direction.x;
        }
        anim.SetFloat("Horizontal", t_anim_horizontal);
        anim.SetFloat("Vertical", t_anim_vertical);


    }

    #endregion

    #region Private Methods
    [PunRPC]
    private void SyncProfile(string p_username,int p_xp,int p_level)
    {
        playerProfile = new ProfileData(p_username, p_xp, p_level);
        playerUsername.text = playerProfile.username;
    }
    private void healthRec()
    { 

    }
    private void ChangeLayerRecurseively(Transform p_trans, int p_layer)
    {
        p_trans.gameObject.layer = p_layer;
        foreach (Transform t in p_trans)
            ChangeLayerRecurseively(t, p_layer);
    }
    void HeadBob(float p_z, float p_x_intensity, float p_y_intemsity)
    {
        float t_aim_adjust = 1f;
        if (isAiming)
            t_aim_adjust = 0.1f;
        targetWeaponBobPosition = weaponParentCurrentPos + new Vector3(Mathf.Cos(p_z) * p_x_intensity * t_aim_adjust, Mathf.Sin(p_z) * p_y_intemsity * t_aim_adjust, 0f);
    }
    void RefreshHealthBar()
    {
        
        float t_health_ratio = (float)current_health / (float)max_health;
        ui_healthbar.localScale = Vector3.Lerp(ui_healthbar.localScale, new Vector3(t_health_ratio, 1, 1), Time.deltaTime * 6f);
        if (alphaCol>0)
        {
            alphaCol = Mathf.Max(0, alphaCol - Time.deltaTime);
            ui_blood.color = new Color(ui_blood.color.r, ui_blood.color.b, ui_blood.color.g, alphaCol);
        }
    }
    [PunRPC]
    void SetCrouch(bool p_state)
    {
        if (crouched == p_state)
            return;
        crouched = p_state;

        if(crouched)
        {
            /*BodyCollider.SetActive(false);
            HeadCollider.SetActive(false);
            LimbCollider.SetActive(false);
            crouchingCollider.SetActive(true);*/
            BodyCollider.SetActive(true);
            HeadCollider.SetActive(true);
            LimbCollider.SetActive(true);
            crouchingCollider.SetActive(false);
            weaponParentCurrentPos += Vector3.down * crouchAmount;
        }
        else
        {
            BodyCollider.SetActive(true);
            HeadCollider.SetActive(true);
            LimbCollider.SetActive(true);
            crouchingCollider.SetActive(false);
            weaponParentCurrentPos += Vector3.up * crouchAmount;
        }
    }
    [PunRPC]
    void De_Dhaka(Vector3 p_force)
    {
        rig.AddForce(p_force+(Vector3.up*3));
    }
    void RefreshMultiplayerState()
    {

        //Aimangle
        float aimEulY = weaponParent.localEulerAngles.y;
        Quaternion targetAimRotation = Quaternion.identity * Quaternion.AngleAxis(aimAngle, Vector3.right);
        weaponParent.rotation = Quaternion.Slerp(weaponParent.rotation, targetAimRotation, Time.deltaTime * 8f);
        Vector3 finalAimRoation = weaponParent.localEulerAngles;
        finalAimRoation.y = aimEulY;
        weaponParent.localEulerAngles = finalAimRoation;

        //camAngle
        float camEulY = normalCam.transform.localEulerAngles.y;
        Quaternion targetCamRotation = Quaternion.identity * Quaternion.AngleAxis(aimAngle, Vector3.right);
        normalCam.transform.rotation = Quaternion.Slerp(normalCam.transform.rotation, targetCamRotation, Time.deltaTime * 8f);
        Vector3 finalCamRoation = normalCam.transform.localEulerAngles;
        finalCamRoation.y = camEulY;
        normalCam.transform.localEulerAngles = finalCamRoation;

    }
    #endregion
    #region Public Methods
    public void TakeDamage(int p_damage, int p_actor)
    {
        if (photonView.IsMine)
        {
            if (p_damage > 0)
                alphaCol = 1f;
            current_health = Mathf.Min(max_health, current_health - p_damage);
            RefreshHealthBar();
            if (p_actor >= -1)
            {
                healthTimer = 0;
            }
            else
                healthTimer *= 0.8f;
            healthTimerBool = true;

            if(current_health<=0)
            {
                healthTimerBool = false;
                manager.Spawn();
                manager.ChangeStat_S(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);
                if (p_actor >= 0)
                    manager.ChangeStat_S(p_actor, 0, 1);
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
    [PunRPC]
    public void WalkSound(int p)
    {
        if (p == movementSound.Length)
        {
            sfxWalk.Stop();
            return;
        }
        sfxWalk.clip = movementSound[p]; 
        sfxWalk.Play();
    }
    //greanade
    private void ThrowGreanade()
    {
        if (current_fuel >= max_fuel / 2)
        {
            GameObject grenade = PhotonNetwork.Instantiate("Boom", grenadePoint.position, grenadePoint.rotation);
            grenade.GetPhotonView().RPC("Killer", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
            current_fuel = Mathf.Max(0, current_fuel - max_fuel/2);
        }

    }
    //death zone
    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Floor")
        {
            TakeDamage(1000, -1);
        }
    }
    #endregion
    #region Photon Callbacks
    public void OnPhotonSerializeView(PhotonStream p_stream, PhotonMessageInfo p_message)
    {
        if(p_stream.IsWriting)
        {
            int tempS;
            tempS = (int)(weaponParent.transform.localEulerAngles.x * 10);
            tempS += 10000*((int)(normalCam.transform.localEulerAngles.x * 10));
            p_stream.SendNext((int)(tempS));
        }
        else 
        {
            int tempR;
            tempR = (int)p_stream.ReceiveNext();
            camAngle = tempR % 10000;
            aimAngle = tempR - (camAngle * 10000);
            aimAngle /= 10f;
            camAngle /= 10f;
        }
    }
    #endregion
}
