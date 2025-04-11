using UnityEngine;
using Firebase;
using Firebase.Analytics;
using Firebase.Database;
using System.Threading.Tasks;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }
    public FirebaseDatabase Database { get; private set; }
    private bool isFirebaseInitialized = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);

        InitializeFirebase();
    }

    private async void InitializeFirebase()
    {
        Debug.Log("Starting Firebase initialization...");
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            // Explicitly set the database URL from google-services.json
            Database = FirebaseDatabase.GetInstance(app, "https://cg63a-a36dc-default-rtdb.firebaseio.com/");
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            isFirebaseInitialized = true;
            Debug.Log($"Firebase initialized successfully. App: {app.Name}, Database URL: {Database.RootReference.ToString()}");
        }
        else
        {
            Debug.LogError("Could not resolve Firebase dependencies: " + dependencyStatus);
        }
    }

    public bool IsInitialized => isFirebaseInitialized;
}