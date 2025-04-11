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
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            Database = FirebaseDatabase.DefaultInstance;
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            isFirebaseInitialized = true;
            Debug.Log("Firebase initialized successfully.");
        }
        else
        {
            Debug.LogError("Could not resolve Firebase dependencies: " + dependencyStatus);
        }
    }

    public bool IsInitialized => isFirebaseInitialized;
}