using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class CustomisableSkillUi : SkillUi
{
    [SerializeField] private TextMeshProUGUI keyBindText;
    [SerializeField] private KeyCode defaultKeyBind = KeyCode.Q;
    [SerializeField] private Image skillIcon;
    [SerializeField] private Sprite defaultIcon;

    protected override void Awake()
    {
        base.Awake();
        if (!PlayerManager.Instance.localPlayer)
        {
            PlayerManager.OnLocalPlayerSet += BindLocalPlayer;
            PlayerManager.OnPlayerRemoved += UnbindLocalPlayer;
        }
        ClearUi();
    }

    protected override void Start()
    {
        base.Start();
        skillIcon.sprite = defaultIcon;
    }

    protected virtual void OnDestroy()
    {
        PlayerManager.OnLocalPlayerSet -= BindLocalPlayer;
        PlayerManager.OnPlayerRemoved -= UnbindLocalPlayer;
    }

    private void BindLocalPlayer(Player player)
    {
        if (player == null)
        {
            Debug.LogWarning("Player is null in CustomisableSkillUi.");
            return;
        }

        if (NetworkManager.Singleton.LocalClientId != player.clientId)
        {
            Debug.LogWarning("Attempted to bind non-local player to CustomisableSkillUi.");
            return;
        }

        if (player.TryGetComponent<PlayerPickup>(out var pickup))
        {
            pickup.OnTrapPickUp += SetUi;
        }

        if (player.TryGetComponent<TrapPlacer>(out var trapPlacer))
        {
            trapPlacer.OnTrapPlaced += ClearUi;
        }
    }

    private void UnbindLocalPlayer(Player player)
    {
        if (player == null)
        {
            Debug.LogWarning("Player is null in CustomisableSkillUi.");
            return;
        }

        if (NetworkManager.Singleton.LocalClientId != player.clientId)
        {
            Debug.LogWarning("Attempted to bind non-local player to CustomisableSkillUi.");
            return;
        }

        if (!player.TryGetComponent<PlayerPickup>(out var pickup))
        {
            pickup.OnTrapPickUp -= SetUi;
        }

        if (!player.TryGetComponent<TrapPlacer>(out var trapPlacer))
        {
            trapPlacer.OnTrapPlaced -= ClearUi;
        }
    }

    private void ClearUi()
    {
        SetKeyBind(defaultKeyBind.ToString());
        SetSkillIcon(null);
    }

    protected virtual void SetUi()
    {
        SetKeyBind("Hold E + LMB");
        SetSkillIcon(defaultIcon);
    }

    public void SetKeyBind(string keyBind)
    {
        keyBindText.text = keyBind;
    }

    public void SetSkillIcon(Sprite sprite)
    {
        if (sprite == null)
        {
            skillIcon.color = Color.clear;
            return;
        }
        skillIcon.color = Color.white;
        skillIcon.sprite = sprite;
    }
}
