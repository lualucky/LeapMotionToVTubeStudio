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
                ForearmExtension,
                ForearmRotation,
                UpperarmExtension,
                UpperarmRotation,
            }

            public paramType type;
            public int hand;
            public int finger;
            public string paramName;
            public GameObject UI;
            public string title;
            public float value;

            public Parameter(paramType _type, int _hand, int _finger, string _title, GameObject _ui, LeapMotionCalculation Data)
            {
                type = _type;
                hand = _hand;
                finger = _finger;
                title = _title;
                UI = _ui;
                UI.transform.GetChild(1).GetComponent<Text>().text = title;

                paramName = "Param";

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
                    paramName += "L";
                else
                    paramName += "R";
                UI.transform.GetChild(2).GetComponent<InputField>().text = paramName;
            }

            // -----------------------------------------------------------------------------------
            // Update the value from Leap Motion
            // -----------------------------------------------------------------------------------
            public float UpdateValue(LeapMotionCalculation Data)
            {
                switch(type)
                {
                    case paramType.TotalRotation:
                        value = Data.GetFingerRotation(hand, finger);
                        break;
                    case paramType.SideRotation:
                        value = Data.GetFingerSpread(hand, finger);
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

                // -- make it look good in UI
                if(value > 1 || value < -1)
                    UI.transform.GetChild(6).GetComponent<Text>().text = "" + Mathf.Round(value);
                else
                    UI.transform.GetChild(6).GetComponent<Text>().text = "" + (int)(value * 1000.0f) / 1000.0f;

                // -- clean it up for VTube Studio
                value = (int)(value * 1000.0f) / 1000.0f;

                return value;
            }

            // -----------------------------------------------------------------------------------
            // Return the parameter name
            // -----------------------------------------------------------------------------------
            public void UpdateParameterName()
            {
                paramName = UI.transform.GetChild(2).GetComponent<InputField>().text;
            }
        }

        LeapMotionCalculation Data;
        VTubeStudio vtube;

        public GameObject TitleText;
        public GameObject UILine;
        public GameObject Content;

        public GameObject SettingsPanel;

        public List<Parameter> parameters;

        bool wasConnected = false;

        void Start()
        {
            Data = GetComponent<LeapMotionCalculation>();
            vtube = GetComponent<VTubeStudio>();

            parameters = new List<Parameter>();

            // -- Create UI and parameters for all values
            for(int h = 0; h < Data.HandCount(); ++h)
            {
                GameObject title = Instantiate(TitleText, Content.transform);
                title.GetComponent<Text>().text = Data.HandName(h) + " Side";

                parameters.Add(new Parameter(Parameter.paramType.UpperarmExtension, h, 0, "Upperarm Extension", Instantiate(UILine, Content.transform), Data));
                parameters.Add(new Parameter(Parameter.paramType.UpperarmRotation, h, 0, "Shoulder Rotation", Instantiate(UILine, Content.transform), Data));
                parameters.Add(new Parameter(Parameter.paramType.ForearmExtension, h, 0, "Forearm Extension", Instantiate(UILine, Content.transform), Data));
                parameters.Add(new Parameter(Parameter.paramType.ForearmRotation, h, 0, "Elbow Rotation", Instantiate(UILine, Content.transform), Data));
                parameters.Add(new Parameter(Parameter.paramType.WristRotationX, h, 0, "Wrist X", Instantiate(UILine, Content.transform), Data));
                parameters.Add(new Parameter(Parameter.paramType.WristRotationY, h, 0, "Wrist Y", Instantiate(UILine, Content.transform), Data));
                parameters.Add(new Parameter(Parameter.paramType.WristRotationZ, h, 0, "Wrist Z", Instantiate(UILine, Content.transform), Data));

                for (int f = 0; f < Data.FingerCount(h); ++f)
                {
                    string fingerTitle = Data.FingerName(f);

                    parameters.Add(new Parameter(Parameter.paramType.TotalRotation, h, f, fingerTitle + " Rotation", Instantiate(UILine, Content.transform), Data));
                    parameters.Add(new Parameter(Parameter.paramType.SideRotation, h, f, fingerTitle + " Side to Side", Instantiate(UILine, Content.transform), Data));
                }
            }
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            // -- just connected for first time, create parameters for vtube studio
            if(vtube.isConnected() && !wasConnected)
            {
                foreach (Parameter p in parameters)
                {
                    // -- TODO something about this min and max magic numbers
                    switch(p.type)
                    {
                        case (Parameter.paramType.SideRotation):
                            // -- thumb has more range
                            if(p.finger == 0)
                                vtube.ParameterCreation(p.paramName, p.title, -15, 40, 0);
                            else
                                vtube.ParameterCreation(p.paramName, p.title, -15, 15, 0);
                            break;
                        case (Parameter.paramType.TotalRotation):
                            vtube.ParameterCreation(p.paramName, p.title, -5, 30, 0);
                            break;
                        default:
                            vtube.ParameterCreation(p.paramName, p.title, -180, 180, 0);
                            break;
                    }
                }
            }
            wasConnected = vtube.isConnected();

            // -- update and send parameters
            if (SettingsPanel.activeInHierarchy)
            {
                foreach (Parameter p in parameters)
                {
                    float value = p.UpdateValue(Data);

                    if (vtube.isConnected())
                    {
                        vtube.QueueInjectParameterData(p.paramName, value);
                    }

                }

                if (vtube.isConnected())
                    vtube.SendInjectParameterData();
            }
        }
    }
}
