using UnityEngine;
using Unity.Cinemachine;

public class MazeCameraManager : MonoBehaviour
{
    public static MazeCameraManager Instance { get; private set; }
    [SerializeField] private int camHeight = 8;

    [SerializeField] public CinemachineCamera topDownCam;
    [SerializeField] public CinemachineCamera localPlayerCam;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterLocalPlayerCamera(CinemachineCamera cam)
    {
        localPlayerCam = cam;

        // uh if bad things are happening to the camera look here first xd
        float deez = MazeManager.Instance.size * MazeManager.Instance.scale / 2;
        Vector3 pos = new (deez, MazeManager.Instance.scale * camHeight, deez);
        topDownCam.transform.position = pos;
    }



    public void SetToPlayerView()
    {
        if (localPlayerCam == null || topDownCam == null)
        {
            if (localPlayerCam == null) Debug.LogWarning("SetToPlayerView: localPlayerCam is null!");
            if (topDownCam == null) Debug.LogWarning("SetToPlayerView: topDownCam is null!");
            return;
        }

        localPlayerCam.Priority = 20;
        topDownCam.Priority = 10;
    }

    public void SetToTopDownView()
    {
        Debug.Log("accessing cam");
        if (localPlayerCam == null || topDownCam == null)
        {
            if (localPlayerCam == null) Debug.LogWarning("SetToTopDownView: localPlayerCam is null!");
            if (topDownCam == null) Debug.LogWarning("SetToTopDownView: topDownCam is null!");

        }

        topDownCam.Priority = 30; // 30 > 20 to get around localplayer race condition
        localPlayerCam.Priority = 10;
    }

}
