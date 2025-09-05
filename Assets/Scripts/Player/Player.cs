using Unity.Netcode;
using Unity.Collections;


public class Player : NetworkBehaviour
{
    // hold player user info
    public NetworkVariable<FixedString64Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Owner);

    public ulong Id => NetworkObjectId;

    private void Update()
    {
        if(!IsOwner) return; // only owner can change targeted combatant
      
    }
    
}