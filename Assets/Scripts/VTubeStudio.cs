using System.Collections.Generic;
using UnityEngine;
using System;
using NativeWebSocket;
using SimpleJSON;
using UnityEngine.UI;

public class VTubeStudio : MonoBehaviour
{

    public static VTubeStudio Instance;

    public string URI;
    public Sprite Icon;
    WebSocket webSocket;

    bool connected = false;
    public Button ConnectButton;
    public Text ConnectText;

    string AuthenticationToken;

    // -- temp storing data to be sent
    VTubeInjectParameterData injectParameterDataStore;

    Dictionary<string, VTubeParameterValueData> TrackingInputParameters;
    Dictionary<string, VTubeParameterValueData> TrackingOutputParameters;

    bool trackingOutputParams;

    public delegate void OnVTubeStudioConnected();
    public OnVTubeStudioConnected onVTubeStudioConnected;

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(gameObject);

        onVTubeStudioConnected += Connected;
        AuthenticationToken = PlayerPrefs.GetString("AuthenticationToken");

        TrackingInputParameters = new Dictionary<string, VTubeParameterValueData>();
        TrackingOutputParameters = new Dictionary<string, VTubeParameterValueData>();

        if (!string.IsNullOrEmpty(AuthenticationToken))
            Connect();
        else
            ConnectToVTubeStudio();
    }

    private void Connected()
    {

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
        if (ConnectButton)
            ConnectButton.interactable = true;
    }

    void WebsocketResponse(byte[] m)
    {
        string message = System.Text.Encoding.UTF8.GetString(m);

        var response = JSON.Parse(message);
        var data = response["data"];

        //Debug.Log(message);

        switch (VTubeData.GetType(response["messageType"].Value))
        {
            case "APIError":
                Debug.Log("VTube Studio Error: " + message);
                /*AuthenticationToken = "";
                Connect();*/
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
                    connected = true;
                    onVTubeStudioConnected();
                    ConnectText.text = "Successfully Connected";	
                    ConnectButton.interactable = false;
                }
                else
                {
                    AuthenticationToken = "";
                    Connect();
                }
                break;

            case VTubeData.HotkeysInCurrentModel:
                break;
            case VTubeData.ParameterValue:
                string parameterName = data["name"];
                if (TrackingInputParameters.ContainsKey(parameterName))
                {
                    VTubeParameterValueData d = TrackingInputParameters[parameterName];
                    d = JsonUtility.FromJson<VTubeParameterValueData>(data);
                }
                break;
            case VTubeData.Live2DParameterList:
                Dictionary<string, VTubeParameterValueData> temp = new Dictionary<string, VTubeParameterValueData>();

                for (int i = 0; i < data["parameters"].Count; ++i)
                {
                    VTubeParameterValueData d = JsonUtility.FromJson<VTubeParameterValueData>(data["parameters"][i]);
                    temp.Add(d.name, d);
                }

                TrackingOutputParameters = temp;
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
        if (webSocket != null)
            webSocket.DispatchMessageQueue();
#endif
    }

    public bool isConnected()
    {
        return connected;
    }

    private void FixedUpdate()
    {
        foreach (string p in TrackingInputParameters.Keys)
        {
            SendVTubeMessage(new VTubeParameterValueRequest(p));
        }

        if (trackingOutputParams)
        {
            SendVTubeMessage(new VTubeLive2DParameterListRequest());
        }
    }

    private void SendVTubeMessage(VTubeMessage req)
    {
        if (!connected)
            return;

        webSocket.Send(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(req)));
    }

    public void ParameterCreation(string parameterName, string explanation, int min, int max, int defaultValue)
    {
        VTubeMessage req = new VTubeParameterCreationRequest(parameterName, explanation, min, max, defaultValue);
        SendVTubeMessage(req);
    }

    public void QueueInjectParameterData(string id, float value, float weight = 1.0f)
    {
        if (!connected)
            return;

        if (injectParameterDataStore == null)
            injectParameterDataStore = new VTubeInjectParameterData();

        injectParameterDataStore.AddParameter(id, value, weight);
    }

    public void SendInjectParameterData()
    {
        if (!connected)
            return;
        if (injectParameterDataStore != null)
        {
            webSocket.Send(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(injectParameterDataStore)));
            injectParameterDataStore = null;
        }
    }

    public void TintAllArtMesh(Color color)
    {
        VTubeColorTintRequest req = new VTubeColorTintRequest(color, null, null, null, null, null, true);
        SendVTubeMessage(req);
    }

    public void TintRGBArtMeshTag(string tag)
    {
        List<string> tags = new List<string>();
        tags.Add(tag);

        VTubeColorTintRequest req = new VTubeColorTintRequest(null, null, null, tags, null);
        SendVTubeMessage(req);
    }

    public void TintArtMeshTag(Color color, List<string> tag)
    {
        VTubeColorTintRequest req = new VTubeColorTintRequest(color, null, null, null, tag, null);
        SendVTubeMessage(req);
    }

    public void StartTrackingParameter(string parameterName)
    {
        TrackingInputParameters.Add(parameterName, new VTubeParameterValueData(parameterName));
    }

    public VTubeParameterValueData TrackingParameterValue(string parameterName)
    {
        if (TrackingInputParameters.ContainsKey(parameterName))
            return TrackingInputParameters[parameterName];
        if (TrackingOutputParameters.ContainsKey(parameterName))
            return TrackingOutputParameters[parameterName];

        return new VTubeParameterValueData(parameterName);
    }

    public void StartTrackingLive2DParameters()
    {
        trackingOutputParams = true;
    }
}
