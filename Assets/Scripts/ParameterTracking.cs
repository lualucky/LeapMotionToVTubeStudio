using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ParameterTracking : MonoBehaviour
{
    // -----------------------------------------------------------------------------------
    // Holds the parameter information separately from the Hand Rotations, mainly
    // for UI and user settings purposes
    // -----------------------------------------------------------------------------------
    public class Parameter
    {
        public enum paramType
        {
            TotalRotation,
            SideRotation,
            AngleX,
            AngleY,
            AngleZ,
            PositionX,
            PositionY,
            PositionZ,
            ForearmExtension,
            ForearmRotation,
            UpperarmExtension,
            UpperarmRotation,
            Found,
            Open,
            Distance,
        }

        public bool defaultParam;
        public paramType type;
        int hand;
        public int finger;
        public string paramName;
        public string title;
        public float value;

        Queue<float> smoothValues;
        public int smooth = 0;

        public int min;
        public int max;
        public int def;

        public bool mirrored;

        public UILineData UI;

        public Parameter(bool _defaultParam, paramType _type, int _hand, int _default, int _min, int _max, string _title, GameObject _ui, TrackingData Data, int _finger = 0)
        {
            defaultParam = _defaultParam;
            smoothValues = new Queue<float>();
            type = _type;
            hand = _hand;
            finger = _finger;
            title = _title;
            min = _min;
            max = _max;
            value = _default;
            def = _default;
            UI = _ui.GetComponent<UILineData>();

            // -- construct parameter name
            switch (type)
            {
                case paramType.TotalRotation:
                    paramName = "Finger" + "_" + (finger + 1) + "_" + Data.FingerName(finger);
                    break;
                case paramType.SideRotation:
                    paramName = "Finger" + "Spread" + (finger + 1) + Data.FingerName(finger);
                    break;
                default:
                    paramName = type.ToString();
                    break;
            }

            if (DefaultsOnly && !defaultParam)
            {
                UI.EnableToggle.isOn = false;
                UI.gameObject.SetActive(false);
            }

            UI.SetName(title);
            UI.SetParameterName(ParameterName());

            // -- load in offset preference
            if (PlayerPrefs.HasKey(paramName + hand + "Smooth"))
            {
                smooth = PlayerPrefs.GetInt(paramName + hand + "Smooth");
                UI.SetSmooth(smooth);
            }

            UI.RegisterSmoothCallback(UpdateSmooth);
        }

        // -----------------------------------------------------------------------------------
        // Update the value from the Tracking Data
        // -----------------------------------------------------------------------------------
        public float UpdateValue(TrackingData Data)
        {
            // -- parameter is disabled from UI
            if (!UI.Enabled())
                return -Mathf.Infinity;

            // -- get values
            switch(type)
            {
                case paramType.TotalRotation:
                    value = Data.GetFingerRotation(hand, finger);
                    break;
                case paramType.SideRotation:
                    value = Data.GetSideToSideRotation(hand, finger);
                    break;
                case paramType.AngleX:
                    value = Data.GetWristRotation(hand).x;
                    break;
                case paramType.AngleY:
                    value = Data.GetWristRotation(hand).y;
                    break;
                case paramType.AngleZ:
                    value = Data.GetWristRotation(hand).z;
                    break;
                case paramType.PositionX:
                    value = Data.GetWristPosition(hand).x;
                    break;
                case paramType.PositionY:
                    value = Data.GetWristPosition(hand).y;
                    break;
                case paramType.PositionZ:
                    value = Data.GetWristPosition(hand).z;
                    break;
                case paramType.ForearmExtension:
                    value = Data.GetForearmExtension(hand);
                    break;
                case paramType.ForearmRotation:
                    value = Data.GetForearmRotation(hand);
                    break;
                case paramType.UpperarmExtension:
                    value = Data.GetUpperarmExtension(hand);
                    break;
                case paramType.UpperarmRotation:
                    value = Data.GetUpperarmRotation(hand);
                    break;
                case paramType.Found:
                    value = Data.HandTracked(hand) ? 1 : 0;
                    break;
                case paramType.Open:
                    value = Data.HandOpen(hand);
                    break;
                case paramType.Distance:
                    value = Data.HandDistance();
                    break;
            }

            // -- mirror value if mirrored
            if (mirrored)
            {
                switch (type)
                {
                    case paramType.AngleX:
                        value = -value;
                        break;
                }
            }

            // -- make it look good in UI
            if (Mathf.Abs(min) + Mathf.Abs(max) < 10)
                UI.SetValue((int)(value * 1000.0f) / 1000.0f);
            else
                UI.SetValue(Mathf.Round(value));

            // -- clean it up for VTube Studio
            value = (int)(value * 1000.0f) / 1000.0f;

            // -- smooth values over certain given count
            if (smooth != 0 && false)
            {
                smoothValues.Enqueue(value);

                if (smoothValues.Count > smooth)
                    smoothValues.Dequeue();

                float totalValues = 0;
                foreach(float v in smoothValues)
                {
                    totalValues += v;
                }

                value = totalValues / smoothValues.Count;
            }

            return value;
        }

        // -- update the smooth value from UI
        public void UpdateSmooth(string _newSmooth)
        {
            smooth = UI.Smooth();

            PlayerPrefs.SetInt(paramName + hand + "Smooth", smooth);
        }
        
        // -- reverse which side the parameters are being sent to
        public void MirrorMovement(bool mirrorValue)
        {
            mirrored = mirrorValue;
        }

        public string ParameterName()
        {
            string result = "Hand";
            if(defaultParam)
                result = "Hand";

            if (Hand() == 0)
                result += "Left";
            else if (Hand() == 1)
                result += "Right";

            return result + paramName;
        }

        public int Hand() {
            if (hand < 0)
                return hand;

            if (mirrored)
                return hand == 1 ? 0 : 1;
            else
                return hand;
        }

        public int RawHand()
        {
            return hand;
        }

        public bool Enabled()
        {
            return UI.ParamEnabled;
        }
    }

    enum trackingLost
    {
        StayAtPose,
        Default,
        WaitDefault
    }

    TrackingData data;
    VTubeStudio vtube;
    Dictionary<string, Parameter> parameters;
    bool wasConnected = false;

    trackingLost trackingResponse;
    bool[] trackingPrev = new bool[2];
    bool[] tracking = new bool[2];

    static public bool DefaultsOnly = true;

    // -- unique UI settings
    public Toggle MirrorMovementToggle;
    public Dropdown TrackingLostDropdown;
    public GameObject TitleText;
    public Transform Content;

    // -- UI prefabs
    public GameObject UILine;
    public GameObject UILineWithSmooth;

    void Start()
    {
        data = GetComponent<TrackingData>();
        vtube = GetComponent<VTubeStudio>();

        parameters = new Dictionary<string, Parameter>();

        // -- create UI and parameters for all values
        for(int h = 0; h < 2; ++h)
        {
            GameObject title = Instantiate(TitleText, Content.transform);
            title.GetComponent<Text>().text = data.HandName(h) + " Side";

            // -- set up arm parameters
            parameters.Add("Upperarm Extension" + h,
                new Parameter(false, Parameter.paramType.UpperarmExtension, h, 1, 0, 1, "Upperarm Extension", Instantiate(UILine, Content), data));
            parameters.Add("Shoulder Rotation" + h,
                new Parameter(false, Parameter.paramType.UpperarmRotation, h, 0, 0, 90, "Shoulder Rotation", Instantiate(UILine, Content), data));
            parameters.Add("Forearm Extension" + h,
                new Parameter(false, Parameter.paramType.ForearmExtension, h, 1, 0, 1, "Forearm Extension", Instantiate(UILine, Content), data));
            parameters.Add("Elbow Rotation" + h,
                new Parameter(false, Parameter.paramType.ForearmRotation, h, 0, -180, 180, "Elbow Rotation", Instantiate(UILine, Content), data));
            parameters.Add("Hand Local Angle X" + h,
                new Parameter(true, Parameter.paramType.AngleX, h, 0, -180, 180, "Hand Local Angle X", Instantiate(UILine, Content), data));
            parameters.Add("Hand Local Angle Y" + h,
                new Parameter(false, Parameter.paramType.AngleY, h, 0, -180, 180, "Hand Local Angle Y", Instantiate(UILine, Content), data));
            parameters.Add("Hand Local Angle Z" + h,
                new Parameter(true, Parameter.paramType.AngleZ, h, 0, -180, 180, "Hand Local Angle Z", Instantiate(UILine, Content), data));
            parameters.Add("Hand Position X" + h,
                new Parameter(true, Parameter.paramType.PositionX, h, 0, -5, 5, "Hand Position X", Instantiate(UILine, Content), data));
            parameters.Add("Hand Position Y" + h,
                new Parameter(true, Parameter.paramType.PositionY, h, 0, -5, 5, "Hand Position Y", Instantiate(UILine, Content), data));
            parameters.Add("Hand Position Z" + h,
                new Parameter(true, Parameter.paramType.PositionZ, h, 0, -5, 5, "Hand Position Z", Instantiate(UILine, Content), data));
            parameters.Add("Hand Found" + h,
                new Parameter(true, Parameter.paramType.Found, h, 0, 0, 1, "Hand Found", Instantiate(UILine, Content), data));
            parameters.Add("Hand Open" + h,
                new Parameter(true, Parameter.paramType.Open, h, 0, 0, 1, "Hand Open", Instantiate(UILine, Content), data));

            // -- set up finger parameters
            for (int f = 0; f < 5; ++f)
            {
                string fingerTitle = data.FingerName(f);

                parameters.Add(fingerTitle + h,
                    new Parameter(true, Parameter.paramType.TotalRotation, h, 0, 0, 1, fingerTitle, Instantiate(UILine, Content), data, f));

                // -- thumb is a special case because it has more side rotation
                if (f == 0)
                {
                    parameters.Add(fingerTitle + "Side" + h, 
                        new Parameter(false, Parameter.paramType.SideRotation, h, 0, -1, 1, fingerTitle + " Spread", Instantiate(UILine, Content), data, f));
                }
                else
                {
                    parameters.Add(fingerTitle + "Side" + h, 
                        new Parameter(false, Parameter.paramType.SideRotation, h, 0, -1, 1, fingerTitle + " Spread", Instantiate(UILine, Content), data, f));
                }
            }
        }

        parameters.Add("Hand Distance",
                new Parameter(true, Parameter.paramType.Distance, -1, 0, 0, 10, "Hand Distance", Instantiate(UILine, Content), data));

        if (PlayerPrefs.HasKey("MirrorMovement"))
            MirrorMovementToggle.isOn = PlayerPrefs.GetInt("MirrorMovement") > 0;

        if (PlayerPrefs.HasKey("TrackingLostResponse")) {
            TrackingLostDropdown.value = PlayerPrefs.GetInt("TrackingLostResponse");
        }

        MirrorMovement(MirrorMovementToggle.isOn);
        SetTrackingLostResponse(TrackingLostDropdown.value);

        MirrorMovementToggle.onValueChanged.AddListener(MirrorMovement);
        TrackingLostDropdown.onValueChanged.AddListener(SetTrackingLostResponse);
    }

    void MirrorMovement(bool value)
    {
        PlayerPrefs.SetInt("MirrorMovement", value ? 1 : 0);

        foreach (Parameter p in parameters.Values)
        {
            p.MirrorMovement(value);
        }
    }

    public void SetTrackingLostResponse(int value)
    {
        PlayerPrefs.SetInt("TrackingLostResponse", value);
        trackingResponse = (trackingLost)value;
    }

    void FixedUpdate()
    {
        // -- just connected for first time, create parameters for vtube studio
        if(vtube.isConnected() && !wasConnected)
        {
            foreach (Parameter p in parameters.Values)
            {
                if(!p.defaultParam)
                    vtube.ParameterCreation(p.ParameterName(), p.title, p.min, p.max, p.def);
            }
        }
        wasConnected = vtube.isConnected();

        for(int h = 0; h < 2; h++)
        {
            Parameter found = parameters["Hand Found" + h];
            int handidx = found.RawHand();

            if (handidx >= 0)
            {
                found.UpdateValue(data);
                trackingPrev[handidx] = tracking[handidx];
                tracking[handidx] = found.value > 0;
            }
        }

        // -- update and send parameters
        foreach (Parameter p in parameters.Values)
        {
            bool send;
            if(p.RawHand() >= 0)
                send = !(!tracking[p.RawHand()] && trackingResponse != trackingLost.StayAtPose);
            else
                send = !(!(tracking[0] && tracking[1]) && trackingResponse != trackingLost.StayAtPose);

            if (!send && trackingResponse == trackingLost.Default && trackingPrev[p.RawHand()])
                vtube.QueueInjectParameterData(p.ParameterName(), p.def, 0);

            if(send)
            {
                float value = p.UpdateValue(data);

                if (p.Enabled() && vtube.isConnected() && value != -Mathf.Infinity)
                {
                    vtube.QueueInjectParameterData(p.ParameterName(), value);
                }
            }
        }

        if (vtube.isConnected())
            vtube.SendInjectParameterData();
    }
}