using UnityEngine;
using Photon.Pun;

public class Sway : MonoBehaviour
{
    #region Variables
    public float intensity;
    public float smooth;
    public bool isMine;
    private Quaternion origin_rotation;
    private float sensiMulti;
    #endregion
    #region MonoBehaviour Callback
    private void Start()
    {
        sensiMulti = 1;
        origin_rotation = transform.localRotation;
    }
    private void Update()
    {
        UpdateSway();
    }
    #endregion
    #region Private Methods
    void UpdateSway()
    {
        //controls
        if (PlayerPrefs.HasKey("Sensi"))
            sensiMulti = PlayerPrefs.GetFloat("Sensi");
        float t_x_mouse = Input.GetAxis("Mouse X")*sensiMulti;
        float t_y_mouse = Input.GetAxis("Mouse Y")*sensiMulti;
        if (!isMine)
        {
            t_x_mouse = 0;
            t_y_mouse = 0;
        }
        //calculate target rotation
        Quaternion t_x_adj = Quaternion.AngleAxis(-intensity * t_x_mouse, Vector3.up);
        Quaternion t_y_adj = Quaternion.AngleAxis(intensity * t_y_mouse, Vector3.right);
        Quaternion target_roation = origin_rotation * t_x_adj * t_y_adj;

        //rotate towards target rotation
        transform.localRotation = Quaternion.Lerp(transform.localRotation, target_roation, Time.deltaTime * smooth);

    }
    #endregion
}

