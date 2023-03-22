using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayScript : MonoBehaviour
{
    public MainScript mainScript;
    public PlayerNetworkScript playerNetworkScript;
    public UIScript uiScript;

    public bool signedIn;

    public async void initializeRelay()
    {
        if (signedIn) return;

        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        signedIn = true;
    }

    public async void createRelay()
    {
        mainScript.hostMode = true;
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            uiScript.joinCodeOutput.text = $"Join Code: {joinCode}";

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");

            UnityTransport unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            unityTransport.SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();

            unityTransport.OnTransportEvent += (NetworkEvent eventType, ulong clientId, ArraySegment<byte> payload, float receiveTime) =>
            {
                if (eventType.ToString() == "Data") mainScript.gameStarted++;
                if (mainScript.gameStarted == 2) startGame();
                if (eventType.ToString() == "Disconnect")
                {
                    NetworkManager.Singleton.Shutdown();
                    mainScript.destroyBoard();
                    uiScript.resetStartingUI();
                }
            };
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }

        uiScript.waitingForJoinPanel.SetActive(true);
    }

    public async void joinRelay()
    {
        mainScript.hostMode = false;
        string joinCode = uiScript.joinCodeInput.text.ToUpper();
        uiScript.joiningStatus.text = "Joining...";
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            UnityTransport unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            unityTransport.SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();

            unityTransport.OnTransportEvent += (NetworkEvent eventType, ulong clientId, ArraySegment<byte> payload, float receiveTime) =>
            {
                if (eventType.ToString() == "Disconnect")
                {
                    NetworkManager.Singleton.Shutdown();
                    mainScript.destroyBoard();
                    uiScript.resetStartingUI();
                }
            };
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
        uiScript.joiningStatus.text = "Joined!";
    }

    public void startGame(bool randomWhite = false, bool isWhite = false)
    {
        if (randomWhite) isWhite = UnityEngine.Random.Range(0, 2) == 0;
        mainScript.startGame(isWhite, false);
        playerNetworkScript.startGameClientRpc(isWhite, PlayerNetworkScript.clientRpcParamsList[playerNetworkScript.OwnerClientId]);
    }
}