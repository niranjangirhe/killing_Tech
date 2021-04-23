using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Pause : MonoBehaviourPunCallbacks
{
    public static bool paused = false;
    private bool disconnecting = false;
    public Slider slider;
    public Text txt;
    public void  TogglePause()
    {
        if (disconnecting)
            return;
        paused = !paused;
        transform.GetChild(0).gameObject.SetActive(paused);
        Cursor.lockState = (paused) ? CursorLockMode.None : CursorLockMode.Confined;
        Cursor.visible = paused;
    }
    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(0);
    }
    public void Quit()
    {
        disconnecting = true;
        PhotonNetwork.LeaveRoom();
        Cursor.lockState = CursorLockMode.None;
    }
    private void Update()
    {
        txt.text = slider.value.ToString("F");
    }
    public void Setter()
    {
        PlayerPrefs.SetFloat("Sensi", slider.value);
    }
}
