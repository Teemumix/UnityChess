using System;
using System.Collections.Generic;
using UnityChess;
using UnityEngine;
using static UnityChess.SquareUtil;

public class BoardManager : MonoBehaviourSingleton<BoardManager>
{
    private readonly GameObject[] allSquaresGO = new GameObject[64];
    private Dictionary<Square, GameObject> positionMap;
    private const float BoardPlaneSideLength = 14f;
    private const float BoardPlaneSideHalfLength = BoardPlaneSideLength * 0.5f;
    private const float BoardHeight = 1.6f;

    private void Awake()
    {
        GameManager.NewGameStartedEvent += OnNewGameStarted;
        GameManager.GameResetToHalfMoveEvent += OnGameResetToHalfMove;
        
        positionMap = new Dictionary<Square, GameObject>(64);
        Transform boardTransform = transform;
        Vector3 boardPosition = boardTransform.position;
        
        for (int file = 1; file <= 8; file++)
        {
            for (int rank = 1; rank <= 8; rank++)
            {
                GameObject squareGO = new GameObject(SquareToString(file, rank))
                {
                    transform = {
                        position = new Vector3(
                            boardPosition.x + FileOrRankToSidePosition(file),
                            boardPosition.y + BoardHeight,
                            boardPosition.z + FileOrRankToSidePosition(rank)
                        ),
                        parent = boardTransform
                    },
                    tag = "Square"
                };

                positionMap.Add(new Square(file, rank), squareGO);
                allSquaresGO[(file - 1) * 8 + (rank - 1)] = squareGO;
            }
        }
    }

    private void OnNewGameStarted()
    {
        ClearBoard();
        foreach ((Square square, Piece piece) in GameManager.Instance.CurrentPieces)
        {
            CreateAndPlacePieceGO(piece, square);
        }
        EnsureOnlyPiecesOfSideAreEnabled(GameManager.Instance.SideToMove);
    }

    private void OnGameResetToHalfMove()
    {
        ClearBoard();
        foreach ((Square square, Piece piece) in GameManager.Instance.CurrentPieces)
        {
            CreateAndPlacePieceGO(piece, square);
        }
        GameManager.Instance.HalfMoveTimeline.TryGetCurrent(out HalfMove latestHalfMove);
        if (latestHalfMove.CausedCheckmate || latestHalfMove.CausedStalemate)
            SetActiveAllPieces(false);
        else
            EnsureOnlyPiecesOfSideAreEnabled(GameManager.Instance.SideToMove);
    }

    public void CastleRook(Square rookPosition, Square endSquare)
    {
        GameObject rookGO = GetPieceGOAtPosition(rookPosition);
        rookGO.transform.parent = GetSquareGOByPosition(endSquare).transform;
        rookGO.transform.localPosition = Vector3.zero;
    }

    public void CreateAndPlacePieceGO(Piece piece, Square position)
    {
        string modelName = $"{piece.Owner} {piece.GetType().Name}";
        GameObject pieceGO = Instantiate(
            Resources.Load("PieceSets/Marble/" + modelName) as GameObject,
            positionMap[position].transform
        );
    }

    public void GetSquareGOsWithinRadius(List<GameObject> squareGOs, Vector3 positionWS, float radius)
    {
        float radiusSqr = radius * radius;
        foreach (GameObject squareGO in allSquaresGO)
        {
            if ((squareGO.transform.position - positionWS).sqrMagnitude < radiusSqr)
                squareGOs.Add(squareGO);
        }
    }

    public void SetActiveAllPieces(bool active)
    {
        VisualPiece[] visualPiece = GetComponentsInChildren<VisualPiece>(true);
        foreach (VisualPiece pieceBehaviour in visualPiece)
            pieceBehaviour.enabled = active;
    }

    public void EnsureOnlyPiecesOfSideAreEnabled(Side side)
    {
        VisualPiece[] visualPiece = GetComponentsInChildren<VisualPiece>(true);
        foreach (VisualPiece pieceBehaviour in visualPiece)
        {
            Piece piece = GameManager.Instance.CurrentBoard[pieceBehaviour.CurrentSquare];
            Side turnSide = NetworkGameController.Instance != null ? NetworkGameController.Instance.CurrentTurn : side;
            pieceBehaviour.enabled = pieceBehaviour.PieceColor == turnSide && GameManager.Instance.HasLegalMoves(piece);
            Debug.Log($"EnsureOnlyPieces: Square: {pieceBehaviour.CurrentSquare}, PieceColor: {pieceBehaviour.PieceColor}, TurnSide: {turnSide}, HasLegalMoves: {GameManager.Instance.HasLegalMoves(piece)}, Enabled: {pieceBehaviour.enabled}");
        }
    }

    public void TryDestroyVisualPiece(Square position)
    {
        VisualPiece visualPiece = positionMap[position].GetComponentInChildren<VisualPiece>();
        if (visualPiece != null)
            DestroyImmediate(visualPiece.gameObject);
    }

    public GameObject GetPieceGOAtPosition(Square position)
    {
        GameObject square = GetSquareGOByPosition(position);
        return square.transform.childCount == 0 ? null : square.transform.GetChild(0).gameObject;
    }

    private static float FileOrRankToSidePosition(int index)
    {
        float t = (index - 1) / 7f;
        return Mathf.Lerp(-BoardPlaneSideHalfLength, BoardPlaneSideHalfLength, t);
    }

    public void ClearBoard()
    {
        VisualPiece[] visualPiece = GetComponentsInChildren<VisualPiece>(true);
        foreach (VisualPiece pieceBehaviour in visualPiece)
        {
            DestroyImmediate(pieceBehaviour.gameObject);
        }
    }

    public GameObject GetSquareGOByPosition(Square position) =>
        Array.Find(allSquaresGO, go => go.name == SquareToString(position));
}