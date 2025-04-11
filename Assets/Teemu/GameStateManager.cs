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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private async void Start()
    {
        if (AnalyticsManager.Instance == null)
        {
            Debug.LogError("AnalyticsManager.Instance is null!");
            return;
        }
        await WaitForFirebaseInitialization();
        dbReference = AnalyticsManager.Instance.Database.RootReference.Child("gameStates");
        Debug.Log($"GameStateManager initialized - dbReference: {(dbReference != null ? dbReference.ToString() : "null")}, Rules: Public read/write confirmed");
        Debug.Log($"Firebase App Name: {Firebase.FirebaseApp.DefaultInstance.Name}, Database URL: {AnalyticsManager.Instance.Database.RootReference.ToString()}");

        saveButton.onClick.AddListener(() => SaveGameState());
        loadButton.onClick.AddListener(() => LoadGameState(matchIdInput.text));
    }

    private async Task WaitForFirebaseInitialization()
    {
        while (!AnalyticsManager.Instance.IsInitialized)
        {
            await Task.Delay(100);
        }
        Debug.Log("Firebase initialization confirmed.");
    }

    public void SaveGameState()
    {
        if (!NetworkManager.Singleton.IsServer || dbReference == null)
        {
            Debug.LogWarning("Cannot save: Not server or dbReference is null.");
            return;
        }

        if (!Application.internetReachability.Equals(NetworkReachability.ReachableViaLocalAreaNetwork) && 
            !Application.internetReachability.Equals(NetworkReachability.ReachableViaCarrierDataNetwork))
        {
            Debug.LogError("No internet connection detected! Cannot save to Firebase.");
            return;
        }
        Debug.Log("Internet connection detected.");

        string fen = GameManager.Instance.SerializeGame();
        string matchId = System.Guid.NewGuid().ToString();
        string jsonData = JsonUtility.ToJson(new GameStateData { FEN = fen });
        Debug.Log($"Attempting to save - MatchID: {matchId}, JSON: {jsonData}");

        dbReference.Child(matchId).SetRawJsonValueAsync(jsonData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log($"Game state saved - MatchID: {matchId}, FEN: {fen}");
                    SyncGameStateClientRpc(matchId, fen);
                }
                else
                {
                    string errorMessage = "Failed to save game state: ";
                    if (task.Exception != null)
                    {
                        errorMessage += task.Exception.ToString();
                        if (task.Exception.InnerExceptions.Count > 0)
                        {
                            foreach (var inner in task.Exception.InnerExceptions)
                            {
                                errorMessage += $"\nInner Exception: {inner.Message}";
                            }
                        }
                    }
                    else
                    {
                        errorMessage += "No further details available.";
                    }
                    Debug.LogError(errorMessage);
                }
            });
    }

    public void LoadGameState(string matchId)
    {
        if (!NetworkManager.Singleton.IsServer || dbReference == null)
        {
            Debug.LogWarning("Cannot load: Not server or dbReference is null.");
            return;
        }

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
                Debug.Log($"Game state loaded - MatchID: {matchId}, FEN: {data.FEN}");
            }
            else
            {
                string errorMessage = "Failed to load game state: ";
                if (task.Exception != null)
                {
                    errorMessage += task.Exception.ToString();
                    if (task.Exception.InnerExceptions.Count > 0)
                    {
                        foreach (var inner in task.Exception.InnerExceptions)
                        {
                            errorMessage += $"\nInner Exception: {inner.Message}";
                        }
                    }
                }
                else
                {
                    errorMessage += "No further details available.";
                }
                Debug.LogError(errorMessage);
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
        NetworkGameController.Instance.SyncBoardClientRpc(fen);
    }
}

[System.Serializable]
public class GameStateData
{
    public string FEN;
}