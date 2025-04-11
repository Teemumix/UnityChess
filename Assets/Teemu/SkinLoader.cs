using UnityEngine;
using Firebase.Storage;
using System.IO;
using System.Threading.Tasks;
using Firebase.Extensions;
using System.Collections;

public class SkinLoader : MonoBehaviour
{
    public static SkinLoader Instance { get; private set; }

    private FirebaseStorage storage;
    public bool isInitialized = false;

    // Set up singleton and wait for Firebase initialization
    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    // Start coroutine to initialize after AnalyticsManager
    private void Start()
    {
        StartCoroutine(InitializeAfterFirebase());
    }

    // Wait for Firebase to initialize then set up storage
    private IEnumerator InitializeAfterFirebase()
    {
        yield return new WaitUntil(() => AnalyticsManager.Instance != null && AnalyticsManager.Instance.IsInitialized);
        storage = FirebaseStorage.DefaultInstance;
        isInitialized = true;
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