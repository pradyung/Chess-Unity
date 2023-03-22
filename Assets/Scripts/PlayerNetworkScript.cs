using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkScript : NetworkBehaviour
{
    public MainScript mainScript;
    public RelayScript relayScript;

    public static ClientRpcParams[] clientRpcParamsList;

    private void Awake()
    {
        mainScript = GameObject.FindGameObjectWithTag("MainScript").GetComponent<MainScript>();
        relayScript = GameObject.FindGameObjectWithTag("RelayScript").GetComponent<RelayScript>();

        mainScript.playerNetworkScript = this;
        relayScript.playerNetworkScript = this;

        clientRpcParamsList = new ClientRpcParams[]
        {
            new() { Send = new() { TargetClientIds = new ulong[] { 0 } } },
            new() { Send = new() { TargetClientIds = new ulong[] { 1 } } }
        };
    }

    [ClientRpc]
    public void startGameClientRpc(bool isWhite, ClientRpcParams clientRpcParams) => mainScript.startGame(!isWhite, false);

    [ClientRpc]
    public void moveClientRpc(MainScript.MoveData moveData) => mainScript.board.makeMove(new MainScript.Move(moveData, mainScript.board));

    [ServerRpc(RequireOwnership = false)]
    public void moveServerRpc(MainScript.MoveData moveData) => moveClientRpc(moveData);
}