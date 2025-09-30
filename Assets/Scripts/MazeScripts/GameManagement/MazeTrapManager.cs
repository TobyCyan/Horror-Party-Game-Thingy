using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
public class MazeTrapManager : MonoBehaviour
{
    public static MazeTrapManager Instance;

    [SerializeField] private LayerMask gridLayer;
    [SerializeField] public GameObject[] trapPrefabs;

    private int selectedTrapIndex = -1;
    private bool active = false;

    private int cost;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        enabled = false; // start disabled
    }

    public void EnablePlacing(bool value)
    {
        active = value;
        enabled = value;
    }

    public void SelectTrap(int index)
    {
        selectedTrapIndex = index;
    }

    void Update()
    {
        if (!active || selectedTrapIndex < 0)
            return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, gridLayer))
            {
                Debug.Log("Hit " + hit.collider.gameObject);
                PlaceTrap(hit.collider.gameObject);
            }
        }
    }

    void PlaceTrap(GameObject cell)
    {
        // on client just handle add delete trap
        Debug.Log("Pretending to place trap " + selectedTrapIndex.ToString());
        // should send message to server that a trap was placed? or just instantiate over rpc?
    }
    
    void CancelTrap()
    {

    }

    void FinaliseTraps()
    {
        //sync information to server?
    }

}
