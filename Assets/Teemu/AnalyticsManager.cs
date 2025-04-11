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
    public bool IsInitialized => isFirebaseInitialized;

    // Set up singleton and start Firebase initialization
    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    // Initialize Firebase services
    private async void InitializeFirebase()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            Database = FirebaseDatabase.GetInstance(app, "https://cg63a-a36dc-default-rtdb.firebaseio.com/");
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            isFirebaseInitialized = true;
        }
        else
        {
            Debug.LogError("Could not resolve Firebase dependencies: " + dependencyStatus);
        }
    }
}