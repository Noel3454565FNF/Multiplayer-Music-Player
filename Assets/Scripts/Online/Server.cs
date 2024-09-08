using Mirror;
using System;
using System.IO;
using System.Net;
using System.Threading;
using UnityEngine;

public class Server : NetworkBehaviour
{
    private HttpListener listener;
    private Thread serverThread;
    public string hostedFilesPath;
    public string basePath = null; // Directory to serve files from
    private int port = 8080;

    void Start()
    {
        serverThread = new Thread(StartServer);
        serverThread.Start();
    }

    public void updatefilepath(string filepath)
    {
        basePath = filepath;
    }
    void StartServer()
    {
        listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        Debug.Log($"File server running at http://localhost:{port}/");

        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            ProcessRequest(context);
        }
    }

    void ProcessRequest(HttpListenerContext context)
    {
        string filename = context.Request.Url.LocalPath.TrimStart('/');
        string filePath = Path.Combine(basePath, filename);

        if (File.Exists(filePath))
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            context.Response.ContentType = GetMimeType(filePath);
            context.Response.ContentLength64 = fileBytes.Length;
            context.Response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            using (StreamWriter writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.Write("File not found");
            }
        }
        context.Response.OutputStream.Close();
    }

    void OnApplicationQuit()
    {
        listener?.Stop();
        serverThread?.Abort();
    }

    string GetMimeType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        switch (extension)
        {
            case ".html": return "text/html";
            case ".css": return "text/css";
            case ".js": return "application/javascript";
            case ".jpg": return "image/jpeg";
            case ".png": return "image/png";
            case ".gif": return "image/gif";
            case ".json": return "application/json";
            default: return "application/octet-stream";
        }
    }
}
