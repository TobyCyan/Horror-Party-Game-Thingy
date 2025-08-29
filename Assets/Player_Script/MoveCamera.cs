using UnityEngine;

public class MoveCamera : MonoBehaviour
{

    public Transform cameraPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = cameraPosition.position;

    }
}
