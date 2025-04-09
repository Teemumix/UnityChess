using UnityEngine;
using Firebase.Storage;
using System.IO;
using System.Threading.Tasks;
using Firebase.Extensions; // Add this for MainThread callbacks

public class SkinLoader : MonoBehaviour
{
    public static SkinLoader Instance { get; private set; }

    private FirebaseStorage storage;
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Initialize Firebase
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully && task.Result == Firebase.DependencyStatus.Available)
            {
                storage = FirebaseStorage.DefaultInstance;
                isInitialized = true;
                Debug.Log("Firebase Storage initialized successfully.");
            }
            else
            {
                Debug.LogError("Failed to initialize Firebase: " + task.Exception?.Message);
            }
        });
    }

    public async void LoadSkin(string skinName, string storagePath)
    {
        if (!isInitialized)
        {
            Debug.LogError("Firebase Storage not initialized yet. Cannot load skin.");
            return;
        }

        string localPath = $"{Application.persistentDataPath}/{skinName}.png";
        if (File.Exists(localPath))
        {
            ApplySkin(skinName, localPath);
            return;
        }

        try
        {
            StorageReference skinRef = storage.GetReference(storagePath);
            byte[] data = await skinRef.GetBytesAsync(1024 * 1024); // 1MB max
            File.WriteAllBytes(localPath, data);
            Debug.Log($"Downloaded {skinName} to {localPath}");
            ApplySkin(skinName, localPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to download {skinName}: {e.Message}");
        }
    }

    private void ApplySkin(string skinName, string localPath)
    {
        byte[] bytes = File.ReadAllBytes(localPath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

        // Update UI with downloaded skin
        foreach (SkinItemUI item in FindObjectsOfType<SkinItemUI>())
        {
            if (item.nameText.text == skinName)
            {
                item.previewImage.sprite = sprite;
            }
        }

        // Notify network of skin change
        //NetworkPlayerSkinManager.Instance.SetPlayerSkin(skinName, sprite);
    }
}