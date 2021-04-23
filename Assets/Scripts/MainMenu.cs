using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Launcher launcher;
    #region Animation
    public GameObject[] bg_image;
    public GameObject[] temp;
    public bool isConnected;
    public Text please_wait;
    public float pw_time;
    public bool ctrl,stopper,once = false;
    private int loop_c = 0;
    void Start()
    {
        Pause.paused = false;
        Cursor.lockState = CursorLockMode.None;
        Bg_Refresh();
        ctrl = true;
    }
    void FixedUpdate()
    {
        if (ctrl && !isConnected)
        {
            Invoke("Txt_Refresh", pw_time);
            ctrl = false;
        }
        if(isConnected && !once)
        {
            once = true;
            temp[0].SetActive(false);
            temp[1].SetActive(false);
            temp[2].SetActive(true);
        }
    }
    void Bg_Refresh()
    {
        int Index = Random.Range(0, bg_image.Length);
        bg_image[Index].SetActive(true);
    }
    void Txt_Refresh()
    {
        if(isConnected)
            return;
        ctrl = true;
        loop_c += 1;
        loop_c = loop_c % 5;
        switch (loop_c)
        {
            case 0:
                {
                    please_wait.text = "Please wait";
                    break;
                }
            case 1:
                {
                    please_wait.text = "Please wait.";
                    break;
                }
            case 2:
                {
                    please_wait.text = "Please wait..";
                    break;
                }
            case 3:
                {
                    please_wait.text = "Please wait...";
                    break;
                }
            case 4:
                {
                    please_wait.text = "Please wait....";
                    break;
                }
            default:
                {
                    break;
                }
        }

    }
    #endregion
    public void JoinMatch()
    {
        launcher.Join();
    }

    public void CreateMatch()
    {
        launcher.Create();
    }

    public void QuitMatch()
    {
        Application.Quit();
    }
}
