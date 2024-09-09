using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SFB;
using Mirror;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Net.NetworkInformation;
using TagLib;

public class MusicPlayer : NetworkBehaviour
{
    public List<string> mp3Files = new List<string>();
    public string selectedDirectory = null;
    public int currentmusic = 0;
    public int clientslistening = 0;
    public string Playing = null;
    public bool canPause;

    public AudioSource mp3AudioSource;
    private OnlineGM OGM;
    public NetworkManager NM;
    public Server serv;

    public Button NextB;
    public Button PrevB;
    public Button Pause;
    public Image MusicImage;
    public Sprite DefaultSprite;

    public string PlayerMode = "null";



    private void Start()
    {
        PlayerMode = "null";
        print(PlayerMode);
        if (GetComponent<OnlineGM>() != null)
        {
            OGM = GetComponent<OnlineGM>();
            serv = GetComponent<Server>();
            mp3AudioSource = GetComponent<AudioSource>();
            NM = GameObject.Find("GameManager").gameObject.GetComponent<NetworkManager>();
            NextB = GameObject.Find("Next").gameObject.GetComponent<Button>();
            PrevB = GameObject.Find("Prev").gameObject.GetComponent<Button>();
            Pause = GameObject.Find("Pause").gameObject.GetComponent<Button>();
            MusicImage = GameObject.Find("Image").gameObject.GetComponent<Image>();

        }

        // Safely adding listeners
        if (NextB != null)
        {
            NextB.onClick.AddListener(() => GetNextMusic(1));
        }
        if (PrevB != null)
        {
            PrevB.onClick.AddListener(() => GetNextMusic(-1));
        }
        if (Pause != null)
        {
            Pause.onClick.AddListener(TogglePause);
        }

        if (isServer && isClient && PlayerMode == "null")
        {
            print("server setup");
            setup();
            PlayerMode = "Server";
            canPause = true;
        }

        if (isClient && PlayerMode == "null")
        {
            print("You are now in Client Mode");
            PlayerMode = "Client";
            canPause = false;
            Pause.GetComponentInChildren<Text>().text = "balai dans ton cul";
            returnmypath();
        }
        
        if (isLocalPlayer == false)
        {
            this.gameObject.SetActive(false);
        }
    }



    [Mirror.Command]
    public void returnmypath()
    {
        callbackformypath(serv.altPath + mp3Files[currentmusic]);
    }

    [ClientRpc]
    public void callbackformypath(string path)
    {
        StartCoroutine(LoadAndPlayMp3(path));
    }

    public void setup()
    {
        var paths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            selectedDirectory = paths[0];
            Debug.Log("Selected Directory: " + selectedDirectory);

            if (Directory.Exists(selectedDirectory))
            {
                // Fetch all mp3 files
                string[] files = Directory.GetFiles(selectedDirectory, "*.mp3");
                mp3Files.Clear();
                mp3Files.AddRange(files);

                Debug.Log($"Found {mp3Files.Count} MP3 files.");

                if (mp3Files.Count > 0)
                {
                    StartCoroutine(LoadAndPlayMp3(mp3Files[currentmusic]));
                    serv.updatefilepath(selectedDirectory);
                    SetupIsDone();
                    print(mp3Files[currentmusic]);
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

    public void SetupIsDone()
    {
        /*        if (NextB != null) NextB.enabled = true;
                if (PrevB != null) PrevB.enabled = true;
                if (Pause != null) Pause.enabled = true;
                if (MusicImage != null) MusicImage.enabled = true;
        */
    }

    [ClientRpc]
    public void GetNextMusic(int newm)
    {
        currentmusic += newm;

        if (currentmusic < 0)
        {
            currentmusic = mp3Files.Count - 1;
        }
        else if (currentmusic >= mp3Files.Count)
        {
            currentmusic = 0;
        }

        StartCoroutine(LoadAndPlayMp3(mp3Files[currentmusic]));
    }

    public void TogglePause()
    {
        if (mp3AudioSource.isPlaying)
        {
            mp3AudioSource.Pause();
            TogglePauseClient(true);
        }
        else
        {
            mp3AudioSource.UnPause();
            TogglePauseClient(false);
        }
    }

    [ClientRpc]
    public void TogglePauseClient(bool pause)
    {
        if (canPause)
        {
            if (pause)
            {
                mp3AudioSource.Pause();
            }
            else
            {
                mp3AudioSource.UnPause();
            }
            print("yelo");
        }
    }

    IEnumerator LoadAndPlayMp3(string path)
    {
        string fullPath = PathForPlatform(path);

        Debug.Log("Loading file from: " + fullPath);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fullPath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error loading audio: " + www.error);
            }
            else
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                mp3AudioSource.clip = audioClip;
                checkClient(fullPath);
                LoadCoverArtWindows(fullPath);
            }
        }
    }

    string PathForPlatform(string path)
    {
        string fullPath = "";

#if UNITY_ANDROID
        fullPath = "file://" + path;
#elif UNITY_IOS
        fullPath = "file://" + path;
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        fullPath = "file:///" + path;
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        fullPath = "file://" + path;
#endif

        return fullPath;
    }

    public void checkClient(string path)
    {
        if (NM.numPlayers <= 1)
        {
            mp3AudioSource.Play();
        }
        else
        {
            clientslistening = NM.numPlayers;
            remoteMusicPlaying(path);
            print("1AA");
        }
    }

    [ClientRpc]
    public void remoteMusicPlaying(string path)
    {
        if (isClient)
        {
            StartCoroutine(courmusicthing(path));
            print("IS CLINET");
        }
        if (isServer)
        {
            mp3AudioSource.Play();
        }
    }

    IEnumerator courmusicthing(string fullpath)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fullpath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Attempting secondary method...");
                UnityWebRequest www2 = UnityWebRequestMultimedia.GetAudioClip(serv.altPath + mp3Files[currentmusic], AudioType.MPEG);
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www2);
                mp3AudioSource.clip = audioClip;
                mp3AudioSource.Play();
                Playing = serv.altPath + mp3Files[currentmusic];
                print(Playing);
                print("ONLINE LOAD");
            }
            else
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                mp3AudioSource.clip = audioClip;
                mp3AudioSource.Play();
                Playing = fullpath;
                print("NORMAL LOAD");
            }
        }
    }


    [ClientRpc]
    public void LoadmusicClientSide()
    {

    }

    IEnumerator LoadMusicClientSideCour()
    {
        yield return null;
    }


#if UNITY_STANDALONE || UNITY_EDITOR
    void LoadCoverArtWindows(string filePath)
    {
        try
        {
            print(mp3Files[currentmusic]);
            print(filePath);
            var file = TagLib.File.Create(mp3Files[currentmusic]);
            var coverArt = file.Tag.Pictures;

            if (coverArt.Length > 0)
            {
                var coverArtBytes = coverArt[0].Data.Data;
                Texture2D coverTexture = new Texture2D(2, 2);
                coverTexture.LoadImage(coverArtBytes);

                Rect rect = new Rect(0, 0, coverTexture.width, coverTexture.height);
                Sprite coverSprite = Sprite.Create(coverTexture, rect, new Vector2(0.5f, 0.5f));
                MusicImage.sprite = coverSprite;
            }
            else
            {
                print("loading default artcover");
                MusicImage.sprite = DefaultSprite;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to load cover art on Windows: " + ex.Message);
        }
    }
#endif
}
