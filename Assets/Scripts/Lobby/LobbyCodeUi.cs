using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyCodeUi : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI codeText;
    void Start()
    {
        if (HasAuthority)
        {
            codeText.text = MySessionManager.Instance.GetJoinCode();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}