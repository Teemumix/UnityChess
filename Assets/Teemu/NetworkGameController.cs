using Unity.Netcode;
using UnityChess;
using UnityEngine;

public class NetworkGameController : NetworkBehaviour
{
    public static NetworkGameController Instance { get; private set; }

    private NetworkVariable<Side> currentTurn = new NetworkVariable<Side>(Side.White);
    private NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        Instance = this;

        if (IsServer)
        {
            isGameActive.Value = true;
            GameManager.Instance.StartNewGame();
        }
    }

    public override void OnNetworkDespawn()
    {
        Instance = null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(ForceNetworkSerializeByMemcpy<NetworkSquare> start, ForceNetworkSerializeByMemcpy<NetworkSquare> end, ulong clientId)
    {
        if (!isGameActive.Value) return;

        Side playerSide = clientId == NetworkManager.ServerClientId ? Side.White : Side.Black;
        if (playerSide != currentTurn.Value)
        {
            Debug.Log("Not your turn!");
            return;
        }

        Square startSquare = start.Value.ToSquare();
        Square endSquare = end.Value.ToSquare();

        Movement move = new Movement(startSquare, endSquare);
        if (GameManager.Instance.TryExecuteMove(move))
        {
            currentTurn.Value = currentTurn.Value == Side.White ? Side.Black : Side.White;
            SyncBoardClientRpc();
        }
    }

    [ClientRpc]
    private void SyncBoardClientRpc()
    {
        GameManager.Instance.ResetGameToHalfMoveIndex(GameManager.Instance.LatestHalfMoveIndex);
    }

    public bool IsMyTurn(ulong clientId)
    {
        Side playerSide = clientId == NetworkManager.ServerClientId ? Side.White : Side.Black;
        return playerSide == currentTurn.Value && isGameActive.Value;
    }
}