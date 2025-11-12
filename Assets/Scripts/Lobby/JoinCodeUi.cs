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
            string code = SceneLifetimeManager.Instance.activeSession.Code;
            joinCodeUi.text = code;
            GUIUtility.systemCopyBuffer = code;
        }
    }
}
