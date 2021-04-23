using UnityEngine;
using Photon.Pun;

public class Look : MonoBehaviourPunCallbacks
{
    #region Variables
    public static bool cursorLock = true;
    public Transform player;
    public Transform noramalCam;
    public Transform weaponCam;
    public Transform weapon;
    private float sensiMulti;
    public float xSensitivity;
    public float ySensitivity;
    public float maxAngle;
    [HideInInspector] public float gunAimSensi;
    private Quaternion camCenter;
    #endregion

    #region MonoBehaviour CAllback
    void Start()
    {
        sensiMulti = 1;
        gunAimSensi = 1;
        camCenter = noramalCam.localRotation;
    }
    void Update()
    {
        if (!photonView.IsMine) return;
        if (Pause.paused)
            return;
        Sety();
        Setx();
        if(PlayerPrefs.HasKey("Sensi"))
            sensiMulti = PlayerPrefs.GetFloat("Sensi");
        UpdateCursorLock();
        weaponCam.rotation = noramalCam.rotation;
    }
    #endregion

    #region Private Methods
    void Sety()
    {
        float t_input = Input.GetAxis("Mouse Y") * ySensitivity * Time.deltaTime * gunAimSensi *sensiMulti;
        Quaternion t_adj = Quaternion.AngleAxis(t_input, -Vector3.right);
        Quaternion t_delta = noramalCam.localRotation * t_adj; 
        if (Quaternion.Angle(camCenter, t_delta) < maxAngle)
        {
            noramalCam.localRotation = t_delta;
        }
        weapon.rotation = noramalCam.rotation;
    }
    void Setx()
    {
        float t_input = Input.GetAxis("Mouse X") * xSensitivity * Time.deltaTime * gunAimSensi *sensiMulti;
        Quaternion t_adj = Quaternion.AngleAxis(t_input, Vector3.up);
        Quaternion t_delta = player.localRotation * t_adj;
        player.localRotation = t_delta;
    
    }
    void UpdateCursorLock()
    {
        if (cursorLock)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    #endregion
}














