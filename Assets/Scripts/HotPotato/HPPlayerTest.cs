using UnityEngine;

public class HPPlayerTest : MonoBehaviour
{
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Monster"))
        {
            Destroy(gameObject);
        }
    }
}
