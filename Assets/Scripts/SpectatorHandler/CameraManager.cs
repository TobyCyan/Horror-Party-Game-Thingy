using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;
    public List<PlayerCam> allCameras = new();

    private bool canSwitchCam = false;
    private int currentCameraIndex = 0;
    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void AddCam(PlayerCam cam)
    {
        if (allCameras.Contains(cam)) return;

        allCameras.Add(cam);
        
        if (cam.IsOwner)
        {
            canSwitchCam = false;
            cam.TogglePlayerCam(true);
        }
    }

    public void RemoveCam(PlayerCam cam)
    {
        if (!allCameras.Contains(cam)) return;
        
        allCameras.Remove(cam);

        if (cam.IsOwner)
        {
            canSwitchCam = true;
            switchNext();
        }
    }

    private void Update()
    {
        if (!canSwitchCam) return;
        
        if (Input.GetKeyDown(KeyCode.A))
        {
            switchNext();
        }
            
        if (Input.GetKeyDown(KeyCode.D))
        {
            SwitchPrevious();
        }
    }
    private void switchNext()
    {
        if (allCameras.Count <= 0) return;

        allCameras[currentCameraIndex++].TogglePlayerCam(false);
        
        if (currentCameraIndex >= allCameras.Count) currentCameraIndex = 0;
        allCameras[currentCameraIndex].TogglePlayerCam(true);
        
    }

    private void SwitchPrevious()
    {
        if (allCameras.Count <= 0) return;
        
        allCameras[currentCameraIndex--].TogglePlayerCam(false);

        if (currentCameraIndex < 0) currentCameraIndex = allCameras.Count - 1;
        allCameras[currentCameraIndex].TogglePlayerCam(true);
    }
}
