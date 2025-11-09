using TMPro;
using UnityEngine;

public class JoinCodeUi : MonoBehaviour
{
    public TextMeshProUGUI joinCodeUi;
    void Start()
    {
        if (SceneLifetimeManager.Instance.activeSession == null)
        {
            this.gameObject.SetActive(false);
        }
        else
        {
            joinCodeUi.text = SceneLifetimeManager.Instance.activeSession.Code;
        }
    }
}
