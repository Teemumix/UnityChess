using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Firebase.Database;
using System.Threading.Tasks;
using Firebase.Extensions;

public class GameStateManager : NetworkBehaviour
{
    public static GameStateManager Instance { get; private set; }
    private DatabaseReference dbReference;

    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private InputField matchIdInput;

    // Set up singleton
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Initialize Firebase and UI buttons
    private async void Start()
    {
        if (AnalyticsManager.Instance == null)
        {
            Debug.LogError("AnalyticsManager.Instance is null!");
            return;
        }
        await WaitForFirebaseInitialization();
        dbReference = AnalyticsManager.Instance.Database.RootReference.Child("gameStates");
        saveButton.onClick.AddListener(() => SaveGameState());
        loadButton.onClick.AddListener(() => LoadGameState(matchIdInput.text));
    }

    // Wait for Firebase to initialize
    private async Task WaitForFirebaseInitialization()
    {
        while (!AnalyticsManager.Instance.IsInitialized)
        {
            await Task.Delay(100);
        }
    }

    // Save current game state to Firebase
    public void SaveGameState()
    {
        if (!NetworkManager.Singleton.IsServer || dbReference == null)
            return;

        if (!Application.internetReachability.Equals(NetworkReachability.ReachableViaLocalAreaNetwork) && 
            !Application.internetReachability.Equals(NetworkReachability.ReachableViaCarrierDataNetwork))
        {
            Debug.LogError("No internet connection detected! Cannot save to Firebase.");
            return;
        }

        string fen = GameManager.Instance.SerializeGame();
        string matchId = System.Guid.NewGuid().ToString();
        string jsonData = JsonUtility.ToJson(new GameStateData { FEN = fen });

        dbReference.Child(matchId).SetRawJsonValueAsync(jsonData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    SyncGameStateClientRpc(matchId, fen);
                }
                else
                {
                    Debug.LogError("Failed to save game state: " + task.Exception?.ToString());
                }
            });
    }

    // Load game state from Firebase
    public void LoadGameState(string matchId)
    {
        if (!NetworkManager.Singleton.IsServer || dbReference == null)
            return;

        if (string.IsNullOrEmpty(matchId))
        {
            Debug.LogError("MatchID is empty!");
            return;
        }

        dbReference.Child(matchId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully && task.Result != null)
            {
                string json = task.Result.GetRawJsonValue();
                GameStateData data = JsonUtility.FromJson<GameStateData>(json);
                GameManager.Instance.LoadGame(data.FEN);
                SyncBoardClientRpc(data.FEN);
            }
            else
            {
                Debug.LogError("Failed to load game state: " + task.Exception?.ToString());
            }
        });
    }

    // Sync game state to clients
    [ClientRpc]
    private void SyncGameStateClientRpc(string matchId, string fen)
    {
        if (!IsServer)
        {
            GameManager.Instance.LoadGame(fen);
        }
    }

    // Sync board state to clients
    [ClientRpc]
    private void SyncBoardClientRpc(string fen)
    {
        NetworkGameController.Instance.SyncBoardClientRpc(fen);
    }
}

[System.Serializable]
public class GameStateData
{
    public string FEN;
}