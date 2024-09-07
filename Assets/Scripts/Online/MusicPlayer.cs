using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SFB;
using Mirror;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MusicPlayer : NetworkBehaviour
{
    public List<string> mp3Files = new List<string>();
    public string selectedDirectory = null;

    public AudioSource mp3AudioSource;
    private OnlineGM OGM;

    public Button NextB;
    public Button PrevB;
    public Button Pause;
    public Image MusicImage;

    private void Start()
    {
        if (gameObject.GetComponent<OnlineGM>() != null)
        {
            OGM = gameObject.GetComponent<OnlineGM>();
            mp3AudioSource = gameObject.GetComponent<AudioSource>();
            setup();
        }
    }

    public void setup()
    {
        var paths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            selectedDirectory = paths[0];
            Debug.Log("Selected Directory: " + selectedDirectory);

            // Check if the directory exists
            if (Directory.Exists(selectedDirectory))
            {
                // Fetch all mp3 files
                string[] files = Directory.GetFiles(selectedDirectory, "*.mp3");

                // Add each file path to the list
                foreach (string file in files)
                {
                    // Replace backslashes with forward slashes
                    string formattedFile = file.Replace("\\", "/");
                    mp3Files.Add(formattedFile);
                    Debug.Log("Found MP3: " + formattedFile);
                }

                // Start playing the first MP3 file as an example
                if (mp3Files.Count > 0)
                {
                    StartCoroutine(LoadAndPlayMp3(mp3Files[0]));
                    SetupIsDone();
                }
            }
            else
            {
                Debug.LogError("Directory not found: " + selectedDirectory);
            }
        }
        else
        {
            Debug.LogWarning("No directory selected");
        }
    }

    // Example method to get all mp3 files
    public List<string> GetMp3Files()
    {
        return mp3Files;
    }

    public void SetupIsDone()
    {
        NextB.enabled = true;
        PrevB.enabled = true;
        Pause.enabled = true;
        MusicImage.enabled = true;
    }

    IEnumerator LoadAndPlayMp3(string path)
    {
        // Prepare the full path for different platforms
        string fullPath = "";

#if UNITY_ANDROID
        fullPath = "file://" + path;  // For Android
#elif UNITY_IOS
        fullPath = "file://" + path;  // For iOS
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        fullPath = "file:///" + path; // On Windows
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        fullPath = "file://" + path;  // For macOS
#endif

        Debug.Log("Loading file from: " + fullPath);

        // Use UnityWebRequest to load the mp3 from the file system
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fullPath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error loading audio: " + www.error);
            }
            else
            {
                // Get the audio clip and assign it to the AudioSource
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                mp3AudioSource.clip = audioClip;
                mp3AudioSource.Play(); // Play the audio
                Debug.Log("Playing: " + fullPath);
            }
        }
    }
}
