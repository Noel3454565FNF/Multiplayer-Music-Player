using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using Mirror;

public class OnlineGM : NetworkBehaviour
{

    private MusicPlayer MP;
    // Start is called before the first frame update
    void Start()
    {
        if (isLocalPlayer)
        {
            MP = gameObject.GetComponent<MusicPlayer>();
            gameObject.AddComponent<AudioSource>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }




}
