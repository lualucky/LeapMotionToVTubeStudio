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
                WristRotationX,
                WristRotationY,
                WristRotationZ,
                WristPositionX,
                WristPositionY,
                WristPositionZ,
                ForearmExtension,
                ForearmRotation,
                UpperarmExtension,
                UpperarmRotation,
            }

            public paramType type;
            public int hand;
            public int finger;
            public string paramName;
            public string title;
            public float value;
            public float offset = 0;
            public int min;
            public int max;
            public int def;

            public bool mirrored;
            public char paramSide;

            public UILineData UI;

            public Parameter(paramType _type, int _hand, int _finger, int _min, int _max, string _title, GameObject _ui, TrackingData Data, int _default = 0)
            {
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
                paramName = "";

                switch (type)
                {
                    case paramType.TotalRotation:
                        paramName += "Hand" + Data.FingerName(finger);
                        break;
                    case paramType.SideRotation:
                        paramName += "Hand" + Data.FingerName(finger) + "Spread";
                        break;
                    case paramType.WristRotationX:
                        paramName += "HandRotationX";
                        break;
                    case paramType.WristRotationY:
                        paramName += "HandRotationY";
                        break;
                    case paramType.WristRotationZ:
                        paramName += "HandRotationZ";
                        break;
                    case paramType.WristPositionX:
                        paramName += "HandPositionX";
                        break;
                    case paramType.WristPositionY:
                        paramName += "HandPositionY";
                        break;
                    case paramType.WristPositionZ:
                        paramName += "HandPositionZ";
                        break;
                    case paramType.ForearmExtension:
                        paramName += "ForearmExtension";
                        break;
                    case paramType.ForearmRotation:
                        paramName += "ElbowRotation";
                        break;
                    case paramType.UpperarmExtension:
                        paramName += "UpperarmExtension";
                        break;
                    case paramType.UpperarmRotation:
                        paramName += "ShoulderRotation";
                        break;
                }
                if (hand == 0)
                    paramSide = 'L';
                else
                    paramSide = 'R';

                UI.SetName(title);
                UI.SetParameterName(paramName + paramSide);
                UI.RegisterOffsetCallback(UpdateOffset);

                // -- load in offset preference
                if (PlayerPrefs.HasKey(paramName + paramSide + "Offset"))
                {
                    offset = PlayerPrefs.GetFloat(paramName + paramSide + "Offset");
                    UI.SetOffset(offset);
                }
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
                    case paramType.WristRotationX:
                        value = Data.GetWristRotation(hand).x;
                        break;
                    case paramType.WristRotationY:
                        value = Data.GetWristRotation(hand).y;
                        break;
                    case paramType.WristRotationZ:
                        value = Data.GetWristRotation(hand).z;
                        break;
                    case paramType.WristPositionX:
                        value = Data.GetWristPosition(hand).x;
                        break;
                    case paramType.WristPositionY:
                        value = Data.GetWristPosition(hand).y;
                        break;
                    case paramType.WristPositionZ:
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
                }

                // -- apply offset
                value += offset;

                // -- mirror value if mirrored
                if(mirrored)
                {
                    switch (type)
                    {
                        case paramType.SideRotation:
                        case paramType.WristRotationX:
                        case paramType.WristRotationZ:
                        case paramType.WristPositionX:
                        case paramType.WristPositionZ:
                        case paramType.ForearmRotation:
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

                return value;
            }

            // -- update the offset value from UI
            public void UpdateOffset(string _newOffset)
            {
                offset = UI.Offset();
                char side = paramSide;
                if(mirrored)
                {
                    if (side == 'L')
                        side = 'R';
                    else
                        side = 'L';
                }

                PlayerPrefs.SetFloat(paramName + side + "Offset", offset);
            }
        
            // -- reverse which side the parameters are being sent to
            public void MirrorMovement()
            {
                if (paramSide == 'L')
                {
                    paramSide= 'R';
                }
                else
                {
                    paramSide = 'L';
                }

                mirrored = !mirrored;
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
        public GameObject UILineWithOffset;

        void Start()
        {
            data = GetComponent<LeapMotionTrackingData>();
            vtube = GetComponent<VTubeStudio>();

            parameters = new List<Parameter>();

            // -- create UI and parameters for all values
            for(int h = 0; h < data.HandCount(); ++h)
            {
                GameObject title = Instantiate(TitleText, Content.transform);
                title.GetComponent<Text>().text = data.HandName(h) + " Side";

                // -- set up arm parameters
                parameters.Add(new Parameter(Parameter.paramType.UpperarmExtension, h, 0, 0, 1, "Upperarm Extension", Instantiate(UILine, Content), data, 1));
                parameters.Add(new Parameter(Parameter.paramType.UpperarmRotation, h, 0, -180, 180, "Shoulder Rotation", Instantiate(UILineWithOffset, Content), data));
                parameters.Add(new Parameter(Parameter.paramType.ForearmExtension, h, 0, 0, 1, "Forearm Extension", Instantiate(UILine, Content), data, 1));
                parameters.Add(new Parameter(Parameter.paramType.ForearmRotation, h, 0, -180, 180, "Elbow Rotation", Instantiate(UILineWithOffset, Content), data));
                parameters.Add(new Parameter(Parameter.paramType.WristRotationX, h, 0, -1, 1, "Wrist Rotation X", Instantiate(UILine, Content), data));
                parameters.Add(new Parameter(Parameter.paramType.WristRotationY, h, 0, -1, 1, "Wrist Rotation Y", Instantiate(UILine, Content), data));
                parameters.Add(new Parameter(Parameter.paramType.WristRotationZ, h, 0, -1, 1, "Wrist Rotation Z", Instantiate(UILine, Content), data));
                parameters.Add(new Parameter(Parameter.paramType.WristPositionX, h, 0, -5, 5, "Wrist Position X", Instantiate(UILine, Content), data));
                parameters.Add(new Parameter(Parameter.paramType.WristPositionY, h, 0, -5, 5, "Wrist Position Y", Instantiate(UILine, Content), data));
                parameters.Add(new Parameter(Parameter.paramType.WristPositionZ, h, 0, -5, 5, "Wrist Position Z", Instantiate(UILine, Content), data));

                // -- set up finger parameters
                for (int f = 0; f < data.FingerCount(h); ++f)
                {
                    string fingerTitle = data.FingerName(f);

                    parameters.Add(new Parameter(Parameter.paramType.TotalRotation, h, f, -5, 30, fingerTitle + " Rotation", Instantiate(UILine, Content), data));

                    // -- thumb is a special case because it has more side rotation
                    if (f == 0)
                    {
                        parameters.Add(new Parameter(Parameter.paramType.SideRotation, h, f, -15, 40, fingerTitle + " Side to Side", Instantiate(UILine, Content), data));
                    }
                    else
                    {
                        parameters.Add(new Parameter(Parameter.paramType.SideRotation, h, f, -15, 15, fingerTitle + " Side to Side", Instantiate(UILine, Content), data));
                    }
                }
            }

            if (MirrorMovementToggle.isOn)
                MirrorMovement(MirrorMovementToggle.isOn);

            MirrorMovementToggle.onValueChanged.AddListener(MirrorMovement);
        }

        void MirrorMovement(bool value)
        {
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
                    vtube.ParameterCreation(p.paramName + p.paramSide, p.title, p.min, p.max, p.def);
                }
            }
            wasConnected = vtube.isConnected();

            // -- update and send parameters
            foreach (Parameter p in parameters)
            {
                float value = p.UpdateValue(data);

                if (vtube.isConnected() && value != -Mathf.Infinity)
                {
                    vtube.QueueInjectParameterData(p.paramName + p.paramSide, value);
                }

            }

            if (vtube.isConnected())
                vtube.SendInjectParameterData();
        }
    }
}
