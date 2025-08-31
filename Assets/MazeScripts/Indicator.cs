using UnityEngine;
using System.Collections;
public class Indicator : MonoBehaviour
{
    [SerializeField]
    GameObject i;

    private void Start()
    {
        i.SetActive(false);
    }
    public void Flash()
    {
        i.SetActive(true);
        if (i != null)
            StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        
        yield return new WaitForSeconds(1f);
        i.SetActive(false);
    }
}
