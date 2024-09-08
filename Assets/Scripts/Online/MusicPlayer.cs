using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SFB;
using Mirror;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Linq;
using Unity.VisualScripting;

public class MusicPlayer : NetworkBehaviour
{
    public List<string> mp3Files = new List<string>();
    public string selectedDirectory = null;
    public int currentmusic = 0;
    public int clientslistening = 0;
    public string Playing = null;

    public AudioSource mp3AudioSource;
    private OnlineGM OGM;
    private NetworkManager NM;
    private Server serv;

    public Button NextB;
    public Button PrevB;
    public Button Pause;
    public Image MusicImage;

    private void Start()
    {
        if (gameObject.GetComponent<OnlineGM>() != null && gameObject.GetComponent<NetworkManager>() != null)
        {
            OGM = gameObject.GetComponent<OnlineGM>();
            NM = gameObject.GetComponent<NetworkManager>();
            serv = gameObject.GetComponent<Server>();
            mp3AudioSource = gameObject.GetComponent<AudioSource>();
            setup();
        }
        NextB.onClick.AddListener(delegate{ GetNextMusic(1); });
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
                    StartCoroutine(LoadAndPlayMp3(mp3Files[currentmusic]));
                    serv.updatefilepath(selectedDirectory);
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

    public void GetNextMusic(int newm)
    {
        int test = currentmusic + newm;
        if (test > 0 || test < mp3Files.Count)
        {
            StartCoroutine(LoadAndPlayMp3(mp3Files[currentmusic] + newm));
        }
        if (test < 0)
        {
            StartCoroutine(LoadAndPlayMp3(mp3Files[mp3Files.Count]));
        }
        if (test > mp3Files.Count)
        {
            StartCoroutine(LoadAndPlayMp3(mp3Files[0]));
        }
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
                checkClient();
                //mp3AudioSource.Play(); // Play the audio
                //Debug.Log("Playing: " + fullPath);
                Playing = fullPath;
            }
        }
    }


    
    public void checkClient()
    {
        if (NM.numPlayers == 0)
        {
            mp3AudioSource.Play(); // Play the audio
        }
        else if (NM.numPlayers < 0)
        {
            clientslistening = NM.numPlayers;
            checkmusicexist();
        }
    }

    [ClientRpc]
    public void checkmusicexist()
    {
        if (isClient)
        {

        }
    }
}
