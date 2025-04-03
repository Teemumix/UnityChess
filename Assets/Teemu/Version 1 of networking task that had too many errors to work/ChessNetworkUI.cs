using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChessNetworkUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI turnIndicator;
    [SerializeField] private TextMeshProUGUI latencyText;

    private void Start()
    {
        // Set default IP (localhost)
        ipInput.text = "127.0.0.1";
        
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    private void OnHostClicked()
    {
        ChessNetworkManager.Instance.StartHost();
        statusText.text = "Hosting game...";
        ToggleConnectionUI(false);
    }

    private void OnJoinClicked()
    {
        ChessNetworkManager.Instance.SetIPAddress(ipInput.text);
        ChessNetworkManager.Instance.StartClient();
        statusText.text = "Joining game...";
        ToggleConnectionUI(false);
    }

    private void ToggleConnectionUI(bool show)
    {
        hostButton.gameObject.SetActive(show);
        joinButton.gameObject.SetActive(show);
        ipInput.gameObject.SetActive(show);
    }

    private void Update()
    {
        if (ChessNetworkManager.Instance == null) return;

        UpdateGameStatus();
        UpdateTurnIndicator();
        UpdateLatencyDisplay();
    }

    private void UpdateGameStatus()
    {
        if (ChessNetworkManager.Instance.IsGameStarted)
        {
            statusText.text = ChessNetworkManager.Instance.IsWhitePlayer ? "You are WHITE" : "You are BLACK";
        }
        
        if (ChessNetworkManager.Instance.IsGameOver)
        {
            statusText.text = $"Game Over: {ChessNetworkManager.Instance.GameResult}";
        }
    }

    private void UpdateTurnIndicator()
    {
        if (!ChessNetworkManager.Instance.IsGameStarted) return;

        if (ChessNetworkManager.Instance.CurrentPlayerTurn == 0)
        {
            turnIndicator.text = "WHITE's turn";
            turnIndicator.color = Color.white;
        }
        else
        {
            turnIndicator.text = "BLACK's turn";
            turnIndicator.color = Color.black;
        }
    }

    private void UpdateLatencyDisplay()
    {
        latencyText.text = $"Latency: {ChessNetworkManager.Instance.AverageLatency:0.0}ms";
    }
}