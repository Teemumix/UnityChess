using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Firebase.Database;
using System.Threading.Tasks;

public class GameStateManager : NetworkBehaviour
{
    public static GameStateManager Instance { get; private set; }
    private DatabaseReference dbReference;

    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private InputField matchIdInput;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (AnalyticsManager.Instance != null && AnalyticsManager.Instance.IsInitialized)
        {
            dbReference = AnalyticsManager.Instance.Database.RootReference.Child("gameStates");
        }

        saveButton.onClick.AddListener(() => GameStateManager.Instance.SaveGameState());
        loadButton.onClick.AddListener(() => GameStateManager.Instance.LoadGameState(matchIdInput.text));
    }

    public void SaveGameState()
    {
        if (!NetworkManager.Singleton.IsServer || dbReference == null) return;

        string fen = GameManager.Instance.SerializeGame();
        string matchId = System.Guid.NewGuid().ToString();
        dbReference.Child(matchId).SetRawJsonValueAsync(JsonUtility.ToJson(new GameStateData { FEN = fen }))
            .ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log($"Game state saved - MatchID: {matchId}");
                    SyncGameStateClientRpc(matchId, fen);
                }
                else
                {
                    Debug.LogError("Failed to save game state: " + task.Exception?.Message);
                }
            });
    }

    public void LoadGameState(string matchId)
    {
        if (!NetworkManager.Singleton.IsServer || dbReference == null) return;

        dbReference.Child(matchId).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && task.Result != null)
            {
                string json = task.Result.GetRawJsonValue();
                GameStateData data = JsonUtility.FromJson<GameStateData>(json);
                GameManager.Instance.LoadGame(data.FEN);
                SyncBoardClientRpc(data.FEN);
                Debug.Log($"Game state loaded - MatchID: {matchId}");
            }
            else
            {
                Debug.LogError("Failed to load game state: " + task.Exception?.Message);
            }
        });
    }

    [ClientRpc]
    private void SyncGameStateClientRpc(string matchId, string fen)
    {
        if (!IsServer)
        {
            GameManager.Instance.LoadGame(fen);
            Debug.Log($"Client synced game state - MatchID: {matchId}");
        }
    }

    [ClientRpc]
    private void SyncBoardClientRpc(string fen)
    {
        NetworkGameController.Instance.SyncBoardClientRpc(fen); // Reuse existing sync method
    }
}

[System.Serializable]
public class GameStateData
{
    public string FEN;
}