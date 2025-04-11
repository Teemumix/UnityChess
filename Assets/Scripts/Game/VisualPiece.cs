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

    // Set up piece transform and camera
    private void Start()
    {
        thisTransform = transform;
        boardCamera = Camera.main;
    }

    // Start dragging piece
    public void OnMouseDown()
    {
        if (enabled)
        {
            piecePositionSS = boardCamera.WorldToScreenPoint(transform.position);
        }
    }

    // Drag piece with mouse
    private void OnMouseDrag()
    {
        if (enabled)
        {
            Vector3 nextPiecePositionSS = new Vector3(Input.mousePosition.x, Input.mousePosition.y, piecePositionSS.z);
            thisTransform.position = boardCamera.ScreenToWorldPoint(nextPiecePositionSS);
        }
    }

    // Drop piece and request move
    public void OnMouseUp()
    {
        if (enabled && NetworkGameController.Instance != null && NetworkGameController.Instance.IsMyTurn(NetworkManager.Singleton.LocalClientId))
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
        }
    }
}