using UnityEngine;
using Firebase.Storage;
using System.IO;
using System.Threading.Tasks;
using Firebase.Extensions;

public class SkinLoader : MonoBehaviour
{
    public static SkinLoader Instance { get; private set; }

    private FirebaseStorage storage;
    public bool isInitialized = false;

    // Set up singleton and initialize Firebase Storage
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully && task.Result == Firebase.DependencyStatus.Available)
            {
                storage = FirebaseStorage.DefaultInstance;
                isInitialized = true;
            }
            else
            {
                Debug.LogError("Failed to initialize Firebase: " + task.Exception?.Message);
            }
        });
    }

    // Load skin from Firebase or local storage
    public async void LoadSkin(string skinName, string storagePath)
    {
        if (!isInitialized)
            return;

        string localPath = $"{Application.persistentDataPath}/{skinName}.png";
        if (File.Exists(localPath))
        {
            ApplySkin(skinName, localPath);
            return;
        }

        try
        {
            StorageReference skinRef = storage.GetReference(storagePath);
            byte[] data = await skinRef.GetBytesAsync(1024 * 1024);
            File.WriteAllBytes(localPath, data);
            ApplySkin(skinName, localPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to download {skinName}: {e.Message}");
        }
    }

    // Apply downloaded skin to UI
    private void ApplySkin(string skinName, string localPath)
    {
        byte[] bytes = File.ReadAllBytes(localPath);
        Texture2D texture = new Texture2D(2, 2);
        if (!texture.LoadImage(bytes))
            return;

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

        foreach (SkinItemUI item in FindObjectsOfType<SkinItemUI>())
        {
            if (item.nameText.text == skinName)
            {
                item.SetPreviewSprite(sprite);
            }
        }
    }

    // Get sprite for a skin
    public Sprite GetSkinSprite(string skinName)
    {
        string localPath = $"{Application.persistentDataPath}/{skinName}.png";
        if (File.Exists(localPath))
        {
            byte[] bytes = File.ReadAllBytes(localPath);
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(bytes))
            {
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            }
        }
        return null;
    }
}