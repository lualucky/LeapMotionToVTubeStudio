using System.Collections.Generic;
using UnityEngine;
using System;
using NativeWebSocket;
using SimpleJSON;
using UnityEngine.UI;

public class VTubeStudio : MonoBehaviour
{

    public static VTubeStudio Instance;

    public string URIBase;
    string URI;
    public Sprite Icon;
    WebSocket webSocket;

    bool connected = false;
    public Button ConnectButton;
    public Text ConnectText;
    public InputField PortInput;

    string AuthenticationToken = "";

    // -- temp storing data to be sent
    VTubeInjectParameterData injectParameterDataStore;

    Dictionary<string, VTubeParameterValueData> TrackingInputParameters;
    Dictionary<string, VTubeParameterValueData> TrackingOutputParameters;

    bool trackingOutputParams;

    public delegate void OnVTubeStudioConnected();
    public OnVTubeStudioConnected onVTubeStudioConnected;

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
        if (ConnectText)
        {
            ConnectText.color = Color.red;
            ConnectText.text = "Can't find VTS. Check API settings in VTS.";
        }
    }
    void WebsocketClose()
    {
        connected = false;
        Debug.Log("Websocket Closed");
        if (ConnectButton)
            ConnectButton.interactable = true;
        if(ConnectText)
        {
            ConnectText.color = Color.black;
            ConnectText.text = "Not Connected";
        }
    }
    void WebsocketResponse(byte[] m)
    {
        string message = System.Text.Encoding.UTF8.GetString(m);
        //Debug.Log(message);

        MessageResponse(message);
    }
    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (webSocket != null)
            webSocket.DispatchMessageQueue();
#endif
    }

    // ============================================================================================
    // VTube Studio
    // ============================================================================================
    private void Start()
    {
        // -- singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(gameObject);

        // -- tracking
        TrackingInputParameters = new Dictionary<string, VTubeParameterValueData>();
        TrackingOutputParameters = new Dictionary<string, VTubeParameterValueData>();

        // -- get saved port number and set up delegate
        if(PlayerPrefs.HasKey("PortNumber"))
            PortInput.text = "" + PlayerPrefs.GetInt("PortNumber");
        URI = URIBase + PortInput.text;
        PortInput.onEndEdit.AddListener(PortNumberField);

        // -- set up auth if exists
        onVTubeStudioConnected = OnConnect;
        if (PlayerPrefs.HasKey("AuthenticationToken"))
            AuthenticationToken = PlayerPrefs.GetString("AuthenticationToken");

        if (!string.IsNullOrEmpty(AuthenticationToken))
            Connect();
    }

    public void Connect()
    {
        ConnectToVTubeStudio();
    }

    async void ConnectToVTubeStudio()
    {
        if (ConnectButton)
            ConnectButton.interactable = false;
        if (ConnectText)
        {
            ConnectText.color = Color.black;
            ConnectText.text = "Not Connected";
        }

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

        await webSocket.Connect();
    }

    async void ReconnectToVTubeStudio()
    {
        await webSocket.Close();
        AuthenticationToken = "";
        Connect();
    }

    void OnConnect()
    {
       
    }

    private async void OnDestroy()
    {
        connected = false;
        if(webSocket != null)
            await webSocket.Close();
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


    // ============================================================================================
    // VTube Studio helper functions
    // ============================================================================================
    // Set or reset the authentification token
    void SetAuthToken(string authToken = "")
    {
        AuthenticationToken = authToken;
        PlayerPrefs.SetString("AuthenticationToken", AuthenticationToken);
        PlayerPrefs.Save();
    }

    // delegate for when the port number field has changed
    void PortNumberField(string portNumber)
    {
        URI = URIBase + PortInput.text;
        PlayerPrefs.SetInt("PortNumber", int.Parse(PortInput.text));
        PlayerPrefs.Save();
    }

    void SendVTubeMessage(VTubeMessage req)
    {
        if (!connected)
            return;

        webSocket.Send(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(req)));
    }

    void MessageResponse(string message)
    {
        var response = JSON.Parse(message);
        var data = response["data"];

        switch (VTubeData.GetType(response["messageType"].Value))
        {
            case "APIError":
                Debug.Log("VTube Studio Error: " + message);
                int ErrorID = int.Parse(data["errorID"]);
                switch ((VTubeStudioErrorID)ErrorID)
                {
                    case VTubeStudioErrorID.TokenRequestDenied:
                        Debug.Log("Request denied");
                        connected = false;
                        webSocket.Close();
                        break;
                    default:
                        break;
                }
                break;
            case VTubeData.AuthenticationToken:
                Debug.Log("Authentification Token Recieved");
                SetAuthToken(data["authenticationToken"]);
                webSocket.Send(System.Text.Encoding.UTF8.GetBytes(
                    JsonUtility.ToJson(new VTubeAuthentication(AuthenticationToken))));
                break;

            case VTubeData.Authentication:
                if (data["authenticated"].AsBool)
                {
                    Debug.Log("VTube Studio Authenticated");
                    connected = true;
                    onVTubeStudioConnected();
                    if (ConnectText)
                    {
                        ConnectText.text = "Successfully Connected";
                    }
                    if (ConnectButton)
                    {
                        ConnectButton.interactable = false;
                    }
                }
                else
                {
                    ConnectButton.interactable = true;
                    SetAuthToken();
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


    // ============================================================================================
    // Public functions
    // ============================================================================================
    public bool isConnected()
    {
        return connected;
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
