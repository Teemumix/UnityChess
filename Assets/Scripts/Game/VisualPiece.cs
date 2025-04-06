using System.Collections.Generic;
using UnityChess;
using UnityEngine;
using static UnityChess.SquareUtil;
using Unity.Netcode;

public class VisualPiece : MonoBehaviour
{
    public delegate void VisualPieceMovedAction(Square movedPieceInitialSquare, Transform movedPieceTransform, Transform closestBoardSquareTransform, Piece promotionPiece = null);
    public static event VisualPieceMovedAction VisualPieceMoved;

    public Side PieceColor;
    public Square CurrentSquare => StringToSquare(transform.parent.name);

    private const float SquareCollisionRadius = 9f;
    private Camera boardCamera;
    private Vector3 piecePositionSS;
    private Transform thisTransform;
    private List<GameObject> potentialLandingSquares = new List<GameObject>();

    private void Start()
    {
        thisTransform = transform;
        boardCamera = Camera.main;
    }

    public void OnMouseDown()
    {
        if (enabled)
        {
            Debug.Log($"OnMouseDown: Piece at {CurrentSquare}, Color: {PieceColor}, Enabled: {enabled}");
            piecePositionSS = boardCamera.WorldToScreenPoint(transform.position);
        }
    }

    private void OnMouseDrag()
    {
        if (enabled)
        {
            Vector3 nextPiecePositionSS = new Vector3(Input.mousePosition.x, Input.mousePosition.y, piecePositionSS.z);
            thisTransform.position = boardCamera.ScreenToWorldPoint(nextPiecePositionSS);
        }
    }

    public void OnMouseUp()
    {
        if (enabled)
        {
            Debug.Log($"OnMouseUp: NetworkGameController exists: {NetworkGameController.Instance != null}, IsMyTurn: {(NetworkGameController.Instance != null ? NetworkGameController.Instance.IsMyTurn(NetworkManager.Singleton.LocalClientId) : false)}, ClientId: {NetworkManager.Singleton.LocalClientId}");
            if (NetworkGameController.Instance != null && NetworkGameController.Instance.IsMyTurn(NetworkManager.Singleton.LocalClientId))
            {
                potentialLandingSquares.Clear();
                BoardManager.Instance.GetSquareGOsWithinRadius(potentialLandingSquares, thisTransform.position, SquareCollisionRadius);

                if (potentialLandingSquares.Count == 0)
                {
                    thisTransform.position = thisTransform.parent.position;
                    return;
                }

                Transform closestSquareTransform = potentialLandingSquares[0].transform;
                float shortestDistanceFromPieceSquared = (closestSquareTransform.position - thisTransform.position).sqrMagnitude;

                for (int i = 1; i < potentialLandingSquares.Count; i++)
                {
                    GameObject potentialLandingSquare = potentialLandingSquares[i];
                    float distanceFromPieceSquared = (potentialLandingSquare.transform.position - thisTransform.position).sqrMagnitude;

                    if (distanceFromPieceSquared < shortestDistanceFromPieceSquared)
                    {
                        shortestDistanceFromPieceSquared = distanceFromPieceSquared;
                        closestSquareTransform = potentialLandingSquare.transform;
                    }
                }

                ForceNetworkSerializeByMemcpy<NetworkSquare> startSquare = new ForceNetworkSerializeByMemcpy<NetworkSquare>(new NetworkSquare(CurrentSquare));
                ForceNetworkSerializeByMemcpy<NetworkSquare> endSquare = new ForceNetworkSerializeByMemcpy<NetworkSquare>(new NetworkSquare(StringToSquare(closestSquareTransform.name)));

                NetworkGameController.Instance.RequestMoveServerRpc(startSquare, endSquare, NetworkManager.Singleton.LocalClientId);
            }
            else
            {
                thisTransform.position = thisTransform.parent.position;
                Debug.Log("Move rejected: Not my turn or no network controller.");
            }
        }
    }
}