using System;
using System.Collections.Generic;
using UnityChess;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviourSingleton<UIManager>
{
    [SerializeField] private GameObject promotionUI = null;
    [SerializeField] public Text resultText = null;
    [SerializeField] private InputField GameStringInputField = null;
    [SerializeField] private Image whiteTurnIndicator = null;
    [SerializeField] private Image blackTurnIndicator = null;
    [SerializeField] private GameObject moveHistoryContentParent = null;
    [SerializeField] private Scrollbar moveHistoryScrollbar = null;
    [SerializeField] private FullMoveUI moveUIPrefab = null;
    [SerializeField] private Text[] boardInfoTexts = null;
    [SerializeField] private Color backgroundColor = new Color(0.39f, 0.39f, 0.39f);
    [SerializeField] private Color textColor = new Color(1f, 0.71f, 0.18f);
    [SerializeField, Range(-0.25f, 0.25f)] private float buttonColorDarkenAmount = 0f;
    [SerializeField, Range(-0.25f, 0.25f)] private float moveHistoryAlternateColorDarkenAmount = 0f;

    private Timeline<FullMoveUI> moveUITimeline;
    private Color buttonColor;

    // Initialize UI components and event listeners
    private void Start()
    {
        GameManager.NewGameStartedEvent += OnNewGameStarted;
        GameManager.GameEndedEvent += OnGameEnded;
        GameManager.MoveExecutedEvent += OnMoveExecuted;
        GameManager.GameResetToHalfMoveEvent += OnGameResetToHalfMove;

        moveUITimeline = new Timeline<FullMoveUI>();
        foreach (Text boardInfoText in boardInfoTexts)
        {
            boardInfoText.color = textColor;
        }

        buttonColor = new Color(
            backgroundColor.r - buttonColorDarkenAmount,
            backgroundColor.g - buttonColorDarkenAmount,
            backgroundColor.b - buttonColorDarkenAmount
        );
    }

    // Reset UI for a new game
    private void OnNewGameStarted()
    {
        UpdateGameStringInputField();
        ValidateIndicators();

        for (int i = 0; i < moveHistoryContentParent.transform.childCount; i++)
        {
            Destroy(moveHistoryContentParent.transform.GetChild(i).gameObject);
        }

        moveUITimeline.Clear();
        resultText.gameObject.SetActive(false);
    }

    // Show game result when game ends
    private void OnGameEnded()
    {
        GameManager.Instance.HalfMoveTimeline.TryGetCurrent(out HalfMove latestHalfMove);
        if (latestHalfMove.CausedCheckmate)
        {
            resultText.text = $"{latestHalfMove.Piece.Owner} Wins!";
        }
        else if (latestHalfMove.CausedStalemate)
        {
            resultText.text = "Draw.";
        }
        resultText.gameObject.SetActive(true);
    }

    // Update UI after a move is made
    private void OnMoveExecuted()
    {
        UpdateGameStringInputField();
        Side sideToMove = GameManager.Instance.SideToMove;
        whiteTurnIndicator.enabled = sideToMove == Side.White;
        blackTurnIndicator.enabled = sideToMove == Side.Black;

        GameManager.Instance.HalfMoveTimeline.TryGetCurrent(out HalfMove lastMove);
        AddMoveToHistory(lastMove, sideToMove.Complement());
    }

    // Sync UI when game resets to a move
    private void OnGameResetToHalfMove()
    {
        UpdateGameStringInputField();
        moveUITimeline.HeadIndex = GameManager.Instance.LatestHalfMoveIndex / 2;
        ValidateIndicators();
    }

    // Show or hide promotion UI
    public void SetActivePromotionUI(bool value) => promotionUI.gameObject.SetActive(value);

    // Handle promotion piece selection
    public void OnElectionButton(int choice) => GameManager.Instance.ElectPiece((ElectedPiece)choice);

    // Reset game to first move
    public void ResetGameToFirstHalfMove() => GameManager.Instance.ResetGameToHalfMoveIndex(0);

    // Reset game to previous move
    public void ResetGameToPreviousHalfMove() => GameManager.Instance.ResetGameToHalfMoveIndex(Math.Max(0, GameManager.Instance.LatestHalfMoveIndex - 1));

    // Reset game to next move
    public void ResetGameToNextHalfMove() => GameManager.Instance.ResetGameToHalfMoveIndex(Math.Min(GameManager.Instance.LatestHalfMoveIndex + 1, GameManager.Instance.HalfMoveTimeline.Count - 1));

    // Reset game to last move
    public void ResetGameToLastHalfMove() => GameManager.Instance.ResetGameToHalfMoveIndex(GameManager.Instance.HalfMoveTimeline.Count - 1);

    // Start a new game
    public void StartNewGame() => GameManager.Instance.StartNewGame();

    // Load game from input field
    public void LoadGame() => GameManager.Instance.LoadGame(GameStringInputField.text);

    // Add move to history UI
    private void AddMoveToHistory(HalfMove latestHalfMove, Side latestTurnSide)
    {
        RemoveAlternateHistory();

        switch (latestTurnSide)
        {
            case Side.Black:
                if (moveUITimeline.HeadIndex == -1)
                {
                    FullMoveUI blackFullMoveUI = Instantiate(moveUIPrefab, moveHistoryContentParent.transform);
                    moveUITimeline.AddNext(blackFullMoveUI);

                    blackFullMoveUI.transform.SetSiblingIndex(GameManager.Instance.FullMoveNumber - 1);
                    blackFullMoveUI.backgroundImage.color = backgroundColor;
                    blackFullMoveUI.whiteMoveButtonImage.color = buttonColor;
                    blackFullMoveUI.blackMoveButtonImage.color = buttonColor;

                    if (blackFullMoveUI.FullMoveNumber % 2 == 0)
                    {
                        blackFullMoveUI.SetAlternateColor(moveHistoryAlternateColorDarkenAmount);
                    }

                    blackFullMoveUI.MoveNumberText.text = $"{blackFullMoveUI.FullMoveNumber}.";
                    blackFullMoveUI.WhiteMoveButton.enabled = false;
                }

                moveUITimeline.TryGetCurrent(out FullMoveUI latestFullMoveUI);
                latestFullMoveUI.BlackMoveText.text = latestHalfMove.ToAlgebraicNotation();
                latestFullMoveUI.BlackMoveButton.enabled = true;
                break;

            case Side.White:
                FullMoveUI whiteFullMoveUI = Instantiate(moveUIPrefab, moveHistoryContentParent.transform);
                whiteFullMoveUI.transform.SetSiblingIndex(GameManager.Instance.FullMoveNumber - 1);
                whiteFullMoveUI.backgroundImage.color = backgroundColor;
                whiteFullMoveUI.whiteMoveButtonImage.color = buttonColor;
                whiteFullMoveUI.blackMoveButtonImage.color = buttonColor;

                if (whiteFullMoveUI.FullMoveNumber % 2 == 0)
                {
                    whiteFullMoveUI.SetAlternateColor(moveHistoryAlternateColorDarkenAmount);
                }

                whiteFullMoveUI.MoveNumberText.text = $"{whiteFullMoveUI.FullMoveNumber}.";
                whiteFullMoveUI.WhiteMoveText.text = latestHalfMove.ToAlgebraicNotation();
                whiteFullMoveUI.BlackMoveText.text = "";
                whiteFullMoveUI.BlackMoveButton.enabled = false;
                whiteFullMoveUI.WhiteMoveButton.enabled = true;

                moveUITimeline.AddNext(whiteFullMoveUI);
                break;
        }

        moveHistoryScrollbar.value = 0;
    }

    // Remove outdated move history entries
    private void RemoveAlternateHistory()
    {
        if (!moveUITimeline.IsUpToDate)
        {
            GameManager.Instance.HalfMoveTimeline.TryGetCurrent(out HalfMove lastHalfMove);
            resultText.gameObject.SetActive(lastHalfMove.CausedCheckmate);
            List<FullMoveUI> divergentFullMoveUIs = moveUITimeline.PopFuture();
            foreach (FullMoveUI divergentFullMoveUI in divergentFullMoveUIs)
            {
                Destroy(divergentFullMoveUI.gameObject);
            }
        }
    }

    // Update turn indicators
    private void ValidateIndicators()
    {
        Side sideToMove = GameManager.Instance.SideToMove;
        whiteTurnIndicator.enabled = sideToMove == Side.White;
        blackTurnIndicator.enabled = sideToMove == Side.Black;
    }

    // Refresh game string in input field
    private void UpdateGameStringInputField() => GameStringInputField.text = GameManager.Instance.SerializeGame();
}