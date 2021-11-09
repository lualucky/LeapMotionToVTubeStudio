using UnityEngine;
using UnityEngine.UI;


// =======================================================================================
// An abstract class that can be used to add a different source for hand tracking data
// =======================================================================================
public abstract class TrackingData : MonoBehaviour
{
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
    public abstract int HandCount();

    // ===================================================================================
    // A string representation of each hand
    // ===================================================================================
    public abstract string HandName(int hand);

    // ===================================================================================
    // How many fingers are there on one hand
    // ===================================================================================
    public abstract int FingerCount(int hand);

    // ===================================================================================
    // The averaged rotation of the joints on one hand, min 0, max 1 (soft constraints)
    // ===================================================================================
    public abstract float GetFingerRotation(int hand, int finger);

    // ===================================================================================
    // The side to side rotation of a finger, like when you spread your fingers
    // Returns an angle in degrees.
    // For all fingers except thumb: min -1, max 1 (soft constraints)
    // Thumb: min -1, max 1
    // ===================================================================================
    public abstract float GetSideToSideRotation(int hand, int finger);

    // ===================================================================================
    // How long the forearm is, 1 is long, 0 is short
    // ===================================================================================
    public abstract float GetForearmExtension(int hand);

    // ===================================================================================
    // Rotation of the forearm from the elbow. min -180, max 180
    // ===================================================================================
    public abstract float GetForearmRotation(int hand);

    // ===================================================================================
    // How long the upperarm is, 1 is long, 0 is short
    // ===================================================================================
    public abstract float GetUpperarmExtension(int hand);

    // ===================================================================================
    // Rotation of the arm from the shoulder. min -180, max 180
    // ===================================================================================
    public abstract float GetUpperarmRotation(int hand);

    // ===================================================================================
    // Euler angles of the rotation of the wrist. min -1, max 1 for each axis
    // ===================================================================================
    public abstract Vector3 GetWristRotation(int hand);

    // ===================================================================================
    // World position of wrist. min -5, max 5
    // ===================================================================================
    public abstract Vector3 GetWristPosition(int hand);

    // ===================================================================================
    // Whether a hand was found this frame
    // ===================================================================================
    public abstract bool HandTracked(int hand);

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
