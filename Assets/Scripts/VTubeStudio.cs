using UnityEngine;
using System;
using NativeWebSocket;
using SimpleJSON;
using UnityEngine.UI;

public class VTubeStudio : MonoBehaviour
{
    public string URI;
    public Sprite Icon;
    WebSocket webSocket;

    bool connected = false;
    public Button ConnectButton;
    public Text ConnectText;

    string AuthenticationToken;

    // -- temp storing data to be sent
    InjectParameterData injectParameterDataStore;

    private void Start()
    {
        AuthenticationToken = PlayerPrefs.GetString("AuthenticationToken");

        if (!string.IsNullOrEmpty(AuthenticationToken))
            Connect();
    }

    // ============================================================================================
    // Websocket interface
    // ============================================================================================
    void WebsocketOpen()
    {
        Debug.Log("Websocket Open");
        // -- If we already have an auth token, connect immediately, otherwise get one
        if (!string.IsNullOrEmpty(AuthenticationToken))
        {
            webSocket.Send(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new VTubeAuthentication(AuthenticationToken))));
        }
        else
        {
            VTubeMessage req = new VTubeAuthenticationToken(Icon);
            webSocket.Send(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(req)));
        }
    }

    void WebsocketError(string e)
    {
        Debug.LogError(e);
    }

    void WebsocketClose()
    {
        connected = false;
        Debug.Log("Websocket Closed");
        if(ConnectButton)
            ConnectButton.interactable = true;
    }

    void WebsocketResponse(byte[] m)
    {
        string message = System.Text.Encoding.UTF8.GetString(m);
        Debug.Log(message);

        var response = JSON.Parse(message);
        var data = response["data"];

        switch (VTubeData.GetType(response["messageType"].Value))
        {
            case "APIError":
                AuthenticationToken = "";
                Connect();
                break;
            case VTubeData.AuthenticationToken:
                Debug.Log("Authentification Token Recieved");
                AuthenticationToken = data["authenticationToken"];
                PlayerPrefs.SetString(VTubeData.AuthenticationToken, AuthenticationToken);
                PlayerPrefs.Save();
                webSocket.Send(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new VTubeAuthentication(AuthenticationToken))));
                break;

            case VTubeData.Authentication:
                if (data["authenticated"].AsBool)
                {
                    Debug.Log("VTube Studio Authenticated");
                    ConnectText.text = "Successfully Connected";
                    ConnectButton.interactable = false;
                    connected = true;
                }
                else
                {
                    AuthenticationToken = "";
                    Connect();
                }
                break;

            case VTubeData.HotkeysInCurrentModel:
                break;
            case null:
                Debug.LogError(message);
                break;
        }
    }

    public void Connect()
    {
        ConnectToVTubeStudio();
    }

    async void ConnectToVTubeStudio()
    {
        webSocket = new WebSocket(URI);
        webSocket.OnMessage += (bytes) =>
        {
            WebsocketResponse(bytes);
        };

        webSocket.OnOpen += () =>
        {
            WebsocketOpen();
        };

        webSocket.OnError += (e) =>
        {
            WebsocketError(e);
        };

        webSocket.OnClose += (e) =>
        {
            WebsocketClose();
        };

        Debug.Log("Attempting connect");
        await webSocket.Connect();
    }

    private async void OnApplicationQuit()
    {
        connected = false;
        await webSocket.Close();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if(webSocket != null)
            webSocket.DispatchMessageQueue();
#endif
    }

    public bool isConnected()
    {
        return connected;
    }

    public void ParameterCreation(string parameterName, string explanation, int min, int max, int defaultValue)
    {
        if (!connected)
            return;
        VTubeMessage req = new VTubeParameterCreationRequest(parameterName, explanation, min, max, defaultValue);
        webSocket.Send(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(req)));
    }

    public void QueueInjectParameterData(string id, float value, float weight = 1.0f)
    {
        if (!connected)
            return;

        if (injectParameterDataStore == null)
            injectParameterDataStore = new InjectParameterData();

        injectParameterDataStore.AddParameter(id, value, weight);
    }

    public void SendInjectParameterData()
    {
        if (!connected)
            return;
        if (injectParameterDataStore != null)
        {
            Debug.Log(JsonUtility.ToJson(injectParameterDataStore));
            webSocket.Send(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(injectParameterDataStore)));
            injectParameterDataStore = null;
        }
    }
}
