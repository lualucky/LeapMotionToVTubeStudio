using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Leap.Unity
{
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
            }

            public bool defaultParam;
            public paramType type;
            public int hand;
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
            // Update the value from Leap Motion
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
                        value = Data.HandTracked(hand) ? 0 : 1;
                        break;
                }

                // -- mirror value if mirrored
                if (mirrored)
                {
                    switch (type)
                    {
                        case paramType.AngleZ:
                            value = (2 * def) - value;
                            break;
                        case paramType.AngleX:
                        //case paramType.ForearmRotation:
                        case paramType.UpperarmRotation:
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
            public void MirrorMovement()
            {
                mirrored = !mirrored;
            }

            public string ParameterName()
            {
                string result = "LeapHand";
                if(defaultParam)
                    result = "Hand";

                if (hand == 0)
                    result += "Left";
                else
                    result += "Right";

                return result + paramName;
            }
        }

        TrackingData data;
        VTubeStudio vtube;
        List<Parameter> parameters;
        bool wasConnected = false;

        // -- unique UI settings
        public Toggle MirrorMovementToggle;
        public GameObject TitleText;
        public Transform Content;

        // -- UI prefabs
        public GameObject UILine;
        public GameObject UILineWithSmooth;

        void Start()
        {
            data = GetComponent<LeapMotionTrackingData>();
            vtube = GetComponent<VTubeStudio>();

            parameters = new List<Parameter>();

            // -- create UI and parameters for all values
            for(int h = 0; h < 2; ++h)
            {
                GameObject title = Instantiate(TitleText, Content.transform);
                title.GetComponent<Text>().text = data.HandName(h) + " Side";

                // -- set up arm parameters
                parameters.Add(new Parameter(false, Parameter.paramType.UpperarmExtension, h, 1, 0, 1, "Upperarm Extension", Instantiate(UILine, Content), data));
                parameters.Add(new Parameter(false, Parameter.paramType.UpperarmRotation, h, 0, -90, 90, "Shoulder Rotation", Instantiate(UILineWithSmooth, Content), data));
                parameters.Add(new Parameter(false, Parameter.paramType.ForearmExtension, h, 1, 0, 1, "Forearm Extension", Instantiate(UILine, Content), data));
                parameters.Add(new Parameter(false, Parameter.paramType.ForearmRotation, h, 0, -180, 180, "Elbow Rotation", Instantiate(UILineWithSmooth, Content), data));
                parameters.Add(new Parameter(true, Parameter.paramType.AngleX, h, 0, -180, 180, "Hand Angle X", Instantiate(UILineWithSmooth, Content), data));
                parameters.Add(new Parameter(false, Parameter.paramType.AngleY, h, 0, -180, 180, "Hand Angle Y", Instantiate(UILineWithSmooth, Content), data));
                parameters.Add(new Parameter(true, Parameter.paramType.AngleZ, h, 0, -180, 180, "Hand Angle Z", Instantiate(UILineWithSmooth, Content), data));
                parameters.Add(new Parameter(true, Parameter.paramType.PositionX, h, 0, -5, 5, "Hand Position X", Instantiate(UILine, Content), data));
                parameters.Add(new Parameter(true, Parameter.paramType.PositionY, h, 0, -5, 5, "Hand Position Y", Instantiate(UILine, Content), data));
                parameters.Add(new Parameter(true, Parameter.paramType.PositionZ, h, 0, -5, 5, "Hand Position Z", Instantiate(UILine, Content), data));
                parameters.Add(new Parameter(true, Parameter.paramType.Found, h, 0, 0, 1, "Hand Found", Instantiate(UILine, Content), data));

                // -- set up finger parameters
                for (int f = 0; f < 5; ++f)
                {
                    string fingerTitle = data.FingerName(f);

                    parameters.Add(new Parameter(true, Parameter.paramType.TotalRotation, h, 0, 0, 1, fingerTitle, Instantiate(UILine, Content), data, f));

                    // -- thumb is a special case because it has more side rotation
                    if (f == 0)
                    {
                        parameters.Add(new Parameter(false, Parameter.paramType.SideRotation, h, 0, -1, 1, fingerTitle + " Spread", Instantiate(UILine, Content), data, f));
                    }
                    else
                    {
                        parameters.Add(new Parameter(false, Parameter.paramType.SideRotation, h, 0, -1, 1, fingerTitle + " Spread", Instantiate(UILine, Content), data, f));
                    }
                }
            }

            if (PlayerPrefs.HasKey("MirrorMovement"))
                MirrorMovementToggle.isOn = PlayerPrefs.GetInt("MirrorMovement") > 0;

            if (MirrorMovementToggle.isOn)
                MirrorMovement(MirrorMovementToggle.isOn);

            MirrorMovementToggle.onValueChanged.AddListener(MirrorMovement);
        }

        void MirrorMovement(bool value)
        {
            PlayerPrefs.SetInt("MirrorMovement", value ? 1 : 0);

            foreach (Parameter p in parameters)
            {
                p.MirrorMovement();
            }
        }

        void FixedUpdate()
        {
            // -- just connected for first time, create parameters for vtube studio
            if(vtube.isConnected() && !wasConnected)
            {
                foreach (Parameter p in parameters)
                {
                    if(!p.defaultParam)
                        vtube.ParameterCreation(p.ParameterName(), p.title, p.min, p.max, p.def);
                }
            }
            wasConnected = vtube.isConnected();

            // -- update and send parameters
            foreach (Parameter p in parameters)
            {
                float value = p.UpdateValue(data);

                if (vtube.isConnected() && value != -Mathf.Infinity)
                {
                    vtube.QueueInjectParameterData(p.ParameterName(), value);
                }

            }

            if (vtube.isConnected())
                vtube.SendInjectParameterData();
        }
    }
}
