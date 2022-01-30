using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// =======================================================================================
// An abstract class that can be used to add a different source for hand tracking data
// =======================================================================================
public abstract class TrackingData : MonoBehaviour
{
    // Helper function to remap values to standard ranges
    static public float Map(float x, float x1, float x2, float y1, float y2)
    {
        var m = (y2 - y1) / (x2 - x1);
        var c = y1 - m * x1;

        return m * x + c;
    }

    // -----------------------------------------------------------------------------------
    // All information for a hand
    // -----------------------------------------------------------------------------------
    protected class Hand
    {
        public string Name;
        public List<Finger> Fingers;
        public Vector3 WristRotPrev;
        public Vector3 WristRotation;
        public Vector3 WristRotationLocal;
        public Vector3 WristPosition;

        public float ForearmExtension;
        public float ForearmRotation;

        public float UpperarmExtension;
        public Vector3 UpperarmPrev;
        public float UpperarmRotation;

        public bool Found;

        public Hand(string _name)
        {
            Name = _name;
            Fingers = new List<Finger>();

            for (int f = 0; f < 5; ++f)
            {
                Fingers.Add(new Finger());
            }

            UpperarmPrev = Vector3.down;
        }
    }

    // -----------------------------------------------------------------------------------
    // All information for a finger
    // -----------------------------------------------------------------------------------
    protected class Finger
    {
        public float SideRotation;
        public float TotalRotation;
    }


    // -- These values are determined by the user via UI
    public float ShoulderWidth;
    public Transform NeckBase;

    public Transform RElbow;
    public Transform LElbow;
    public Transform RShoulder;
    public Transform LShoulder;

    public Slider Height;
    public Slider Width;
    public Slider Depth;

    // -- Hand data for this instance
    protected List<Hand> hands;

    protected virtual void Start()
    {
        // -- load in and update shoulder positions
        Vector3 pos = NeckBase.position;
        if (PlayerPrefs.HasKey("base x") && PlayerPrefs.HasKey("base y") && PlayerPrefs.HasKey("base z"))
            pos = new Vector3(PlayerPrefs.GetFloat("base x"),
                PlayerPrefs.GetFloat("base y"), PlayerPrefs.GetFloat("base z"));
        NeckBase.position = pos;

        ShoulderWidth = Width.value;
        if (PlayerPrefs.HasKey("width"))
            ShoulderWidth = PlayerPrefs.GetFloat("width");

        Height.value = pos.y;
        Width.value = ShoulderWidth;
        Depth.value = pos.z;

        RShoulder.position = NeckBase.position + ((ShoulderWidth / 2f) * Vector3.right);
        LShoulder.position = NeckBase.position + ((ShoulderWidth / 2f) * Vector3.left);

        // -- automatically update values when slider is changed
        Height.onValueChanged.AddListener(delegate { SetShoulderHeight(); });
        Width.onValueChanged.AddListener(delegate { SetShoulderWidth(); });
        Depth.onValueChanged.AddListener(delegate { SetShoulderDepth(); });
    }

    // ===================================================================================
    // Helper function to get name of finger
    // ===================================================================================
    public virtual string FingerName(int finger)
    {
        string fingerTitle = "";
        switch (finger)
        {
            case 0:
                fingerTitle = "Thumb";
                break;
            case 1:
                fingerTitle = "Index";
                break;
            case 2:
                fingerTitle = "Middle";
                break;
            case 3:
                fingerTitle = "Ring";
                break;
            case 4:
                fingerTitle = "Pinky";
                break;
        }
        return fingerTitle;
    }

    // ===================================================================================
    // How many hands are there
    // ===================================================================================
    public int HandCount()
    {
        return hands.Count;
    }

    // ===================================================================================
    // A string representation of each hand
    // ===================================================================================
    public string HandName(int hand)
    {
        return hands[hand].Name;
    }

    // ===================================================================================
    // How many fingers are there on one hand
    // ===================================================================================
    public int FingerCount(int hand)
    {
        return hands[hand].Fingers.Count;
    }

    // ===================================================================================
    // The averaged rotation of the joints on one hand, min 0, max 1 (soft constraints)
    // ===================================================================================
    public float GetFingerRotation(int hand, int finger)
    {
        return hands[hand].Fingers[finger].TotalRotation;
    }

    // ===================================================================================
    // The side to side rotation of a finger, like when you spread your fingers
    // Returns an angle in degrees.
    // For all fingers except thumb: min -1, max 1 (soft constraints)
    // Thumb: min -1, max 1
    // ===================================================================================
    public float GetSideToSideRotation(int hand, int finger)
    {
        return hands[hand].Fingers[finger].SideRotation;
    }

    // ===================================================================================
    // How long the forearm is, 1 is long, 0 is short
    // ===================================================================================
    public float GetForearmExtension(int hand)
    {
        return hands[hand].ForearmExtension;
    }

    // ===================================================================================
    // Rotation of the forearm from the elbow. min -180, max 180
    // ===================================================================================
    public float GetForearmRotation(int hand)
    {
        return hands[hand].ForearmRotation;
    }

    // ===================================================================================
    // How long the upperarm is, 1 is long, 0 is short
    // ===================================================================================
    public float GetUpperarmExtension(int hand)
    {
        return hands[hand].UpperarmExtension;
    }

    // ===================================================================================
    // Rotation of the arm from the shoulder. min -180, max 180
    // ===================================================================================
    public float GetUpperarmRotation(int hand)
    {
        return hands[hand].UpperarmRotation;
    }

    // ===================================================================================
    // Euler angles of the rotation of the wrist. min -1, max 1 for each axis
    // ===================================================================================
    public Vector3 GetWristRotation(int hand)
    {
        return hands[hand].WristRotation;
    }

    // ===================================================================================
    // World position of wrist. min -5, max 5
    // ===================================================================================
    public Vector3 GetWristPosition(int hand)
    {
        return hands[hand].WristPosition;
    }

    // ===================================================================================
    // Whether a hand was found this frame
    // ===================================================================================
    public bool HandTracked(int hand)
    {
        return hands[hand].Found;
    }

    // ===================================================================================
    // Average of the finger values
    // ===================================================================================
    public float HandOpen(int hand)
    {
        float sum = 0f;
        Hand h = hands[hand];
        foreach(Finger f in h.Fingers)
        {
            sum += f.TotalRotation;
        }

        return sum / h.Fingers.Count;
    }

    // ===================================================================================
    // Shoulder position options
    // ===================================================================================
    public virtual void SetShoulderHeight()
    {
        Vector3 pos = NeckBase.position;
        pos.y = Height.value;
        NeckBase.position = pos;

        RShoulder.position = NeckBase.position + ((ShoulderWidth / 2f) * Vector3.right);
        LShoulder.position = NeckBase.position + ((ShoulderWidth / 2f) * Vector3.left);

        PlayerPrefs.SetFloat("base x", NeckBase.position.x);
        PlayerPrefs.SetFloat("base y", NeckBase.position.y);
        PlayerPrefs.SetFloat("base z", NeckBase.position.z);
    }

    public virtual void SetShoulderWidth()
    {
        ShoulderWidth = Width.value;

        RShoulder.position = NeckBase.position + ((ShoulderWidth / 2f) * Vector3.right);
        LShoulder.position = NeckBase.position + ((ShoulderWidth / 2f) * Vector3.left);

        PlayerPrefs.SetFloat("width", ShoulderWidth);
    }

    public virtual void SetShoulderDepth()
    {
        Vector3 pos = NeckBase.position;
        pos.z = Depth.value;
        NeckBase.position = pos;

        RShoulder.position = NeckBase.position + ((ShoulderWidth / 2f) * Vector3.right);
        LShoulder.position = NeckBase.position + ((ShoulderWidth / 2f) * Vector3.left);

        PlayerPrefs.SetFloat("base x", NeckBase.position.x);
        PlayerPrefs.SetFloat("base y", NeckBase.position.y);
        PlayerPrefs.SetFloat("base z", NeckBase.position.z);
    }
}
