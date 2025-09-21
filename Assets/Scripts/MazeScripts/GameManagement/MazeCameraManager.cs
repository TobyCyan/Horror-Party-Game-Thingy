using UnityEngine;
using Unity.Cinemachine;

public class MazeCameraManager : MonoBehaviour
{
    public static MazeCameraManager Instance { get; private set; }

    [SerializeField] private CinemachineCamera topDownCam;
    [SerializeField] private CinemachineCamera localPlayerCam;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        SetToPlayerView();


    }

    public void RegisterLocalPlayerCamera(CinemachineCamera cam)
    {
        localPlayerCam = cam;

        // uh if bad things are happening to the camera look here first xd
        float deez = MazeManager.Instance.size * MazeManager.Instance.scale / 2;
        Vector3 pos = new (deez, 200f, deez);
        topDownCam.transform.position = pos;
    }



    public void SetToPlayerView()
    {
        if (localPlayerCam == null || topDownCam == null) return;
        localPlayerCam.Priority = 20;
        topDownCam.Priority = 10;
    }

    public void SetToTopDownView()
    {
        if (localPlayerCam == null || topDownCam == null) return;
        topDownCam.Priority = 20;
        localPlayerCam.Priority = 10;
    }
}
