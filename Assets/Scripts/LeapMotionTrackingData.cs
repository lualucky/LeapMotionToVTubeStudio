using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Leap.Unity;

public class LeapMotionTrackingData : TrackingData
{
// -----------------------------------------------------------------------------------
// Helper function to get name of finger
// -----------------------------------------------------------------------------------
public override string FingerName(int finger)
    {
        string fingerTitle = "";
        switch ((Leap.Finger.FingerType)finger)
        {
            case Leap.Finger.FingerType.TYPE_THUMB:
                fingerTitle = "Thumb";
                break;
            case Leap.Finger.FingerType.TYPE_INDEX:
                fingerTitle = "Index";
                break;
            case Leap.Finger.FingerType.TYPE_MIDDLE:
                fingerTitle = "Middle";
                break;
            case Leap.Finger.FingerType.TYPE_RING:
                fingerTitle = "Ring";
                break;
            case Leap.Finger.FingerType.TYPE_PINKY:
                fingerTitle = "Pinky";
                break;
        }
        return fingerTitle;
    }

    public LeapProvider provider;

    public Dropdown LeapPlacementOptions;

    public Slider PosXSlider;
    public Slider PosYSlider;
    public Slider PosZSlider;

    public Slider RotXSlider;
    public Slider RotYSlider;
    public Slider RotZSlider;

    protected override void Start()
    {
        base.Start();

        hands = new List<Hand>();

        // -- init hands
        // -- left = 0, right = 1, always
        for (int h = 0; h < 2; ++h)
        {
            string name = "Left";
            if (h == 1)
            {
                name = "Right";
            }
            Hand hand = new Hand(name);
            hands.Add(hand);

        }

        for (int i = 0; i <= (int)Leap.TestHandFactory.TestHandPose.ScreenTop; ++i)
        {
            LeapPlacementOptions.options.Add(new Dropdown.OptionData(((Leap.TestHandFactory.TestHandPose)i).ToString()));
        }

        int val = (int)provider.editTimePose;
        if (PlayerPrefs.HasKey("leapMode"))
            val = PlayerPrefs.GetInt("leapMode");
        LeapPlacementOptions.value = val;

        if (PlayerPrefs.HasKey("leapPosX"))
        {
            PosXSlider.value = PlayerPrefs.GetFloat("leapPosX");
            SetLeapPosX();
        }
        if (PlayerPrefs.HasKey("leapPosY"))
        {
            PosYSlider.value = PlayerPrefs.GetFloat("leapPosY");
            SetLeapPosY();
        }
        if (PlayerPrefs.HasKey("leapPosZ"))
        {
            PosZSlider.value = PlayerPrefs.GetFloat("leapPosZ");
            SetLeapPosZ();
        }

        if (PlayerPrefs.HasKey("leapRotX"))
        {
            RotXSlider.value = PlayerPrefs.GetFloat("leapRotX");
            SetLeapRotX();
        }
        if (PlayerPrefs.HasKey("leapRotY"))
        {
            RotYSlider.value = PlayerPrefs.GetFloat("leapRotY");
            SetLeapRotY();
        }
        if (PlayerPrefs.HasKey("leapRotZ"))
        {
            RotZSlider.value = PlayerPrefs.GetFloat("leapRotZ");
            SetLeapRotZ();
        }

        LeapPlacementOptions.onValueChanged.AddListener(delegate { SetLeapMode(); });
        PosXSlider.onValueChanged.AddListener(delegate { SetLeapPosX(); });
        PosYSlider.onValueChanged.AddListener(delegate { SetLeapPosY(); });
        PosZSlider.onValueChanged.AddListener(delegate { SetLeapPosZ(); });
        RotXSlider.onValueChanged.AddListener(delegate { SetLeapRotX(); });
        RotYSlider.onValueChanged.AddListener(delegate { SetLeapRotY(); });
        RotZSlider.onValueChanged.AddListener(delegate { SetLeapRotZ(); });
    }

    // ===================================================================================
    // Update values from leap motion and calculate values we need, and save them in
    // the data members of our parent TrackingData.cs
    // ===================================================================================
    private void FixedUpdate()
    {
        bool[] trackingFound = new bool[] { false, false };

        // -- Update hand values
        for (int h = 0; h < provider.CurrentFrame.Hands.Count; ++h)
        {
            Hand hand = hands[0];

            // -- Determine what hand we're using
            Leap.Hand leapHand = provider.CurrentFrame.Hands[h];
            if (leapHand.IsLeft)
            {
                hand = hands[0];
                trackingFound[0] = true;
            }
            else if (leapHand.IsRight)
            {
                hand = hands[1];
                trackingFound[1] = true;
            }
            else
                continue;

            // -- Calculate base of neck
            Vector3 center = NeckBase.position;
            if (leapHand.IsRight)
                center = RShoulder.position;
            else if (leapHand.IsLeft)
                center = LShoulder.position;

            // -- debug draw
            if (leapHand.IsLeft)
                LElbow.position = leapHand.Arm.ElbowPosition.ToVector3();
            else if (leapHand.IsRight)
                RElbow.position = leapHand.Arm.ElbowPosition.ToVector3();

            // -- Upperarm Vector
            Vector3 elbowPos = leapHand.Arm.ElbowPosition.ToVector3() - center;

            // -- Upperarm Extension
            Vector3 elbowProj = Vector3.ProjectOnPlane(elbowPos, Vector3.forward);
            hand.UpperarmExtension = (elbowProj.magnitude) / (elbowPos.magnitude);

            // -- Upperarm rotation calculations
            hand.UpperarmRotation = Vector3.SignedAngle(Vector3.down, elbowProj, Vector3.forward);
            if (leapHand.IsLeft)
                hand.UpperarmRotation = -hand.UpperarmRotation;

            Vector3 wristPos = leapHand.WristPosition.ToVector3() - elbowPos - center;
            Vector3 wristProj = Vector3.ProjectOnPlane(wristPos, Vector3.forward);

            // -- Forearm extension calculations
            hand.ForearmExtension = (wristProj.magnitude) / (wristPos.magnitude);

            // -- Forearm rotation calculation

            hand.ForearmRotation = ElbowRotation(leapHand, hand.ForearmRotation, wristProj, hand.UpperarmRotation);

            // -- Wrist position
            hand.WristPosition = HandPostion(leapHand);
            if (leapHand.IsLeft)
                hand.WristPosition.x = -hand.WristPosition.x;

            // -- Wrist rotation calculations
            hand.WristRotation = HandRotations(leapHand);
            if (leapHand.IsRight)
            {
                hand.WristRotation.z = -hand.WristRotation.z;
                hand.WristRotation.x = -hand.WristRotation.x;
            }

            // -- Finger calculations
            for (int f = 0; f < hand.Fingers.Count; ++f)
            {
                Finger finger = hand.Fingers[f];
                Leap.Finger leapFinger = leapHand.Fingers[f];

                if (leapFinger != null)
                {
                    finger.SideRotation = FingerSpreadCalc(leapFinger, finger.SideRotation);
                    if (leapHand.IsLeft)
                        finger.SideRotation = -finger.SideRotation;
                    finger.TotalRotation = FingerRotationCalc(leapFinger);
                }
            }
        }

        hands[0].Found = trackingFound[0];
        hands[1].Found = trackingFound[1];
    }

    // -- Total finger rotation calculation, just average the rotation of each joint
    float FingerRotationCalc(Leap.Finger leapFinger)
    {
        float result;

        float total = 0;
        for (int j = 1; j < leapFinger.bones.Length - 1; ++j)
        {
            Quaternion jointRot = Quaternion.Inverse(leapFinger.Bone((Leap.Bone.BoneType)(j)).Rotation.ToQuaternion())
                                    * leapFinger.Bone((Leap.Bone.BoneType)(j + 1)).Rotation.ToQuaternion();
            total += jointRot.eulerAngles.x;
        }
        result = total / leapFinger.bones.Length;
        if (result > 50f)
        {
            result = 0;
        }

        // -- clamp to 0 - 1
        if (leapFinger.Type == Leap.Finger.FingerType.TYPE_THUMB)
            result = Map(result, 0, 20, 1, 0);
        else
            result = Map(result, 0, 30, 1, 0);
        return result;
    }

    // -- Spread/wagging calculation
    float FingerSpreadCalc(Leap.Finger leapFinger, float PreviousSpread)
    {
        float result;

        // -- Spread/wagging calculation
        Quaternion jointRotation = Quaternion.Inverse(leapFinger.bones[0].Rotation.ToQuaternion())
                                    * leapFinger.bones[1].Rotation.ToQuaternion();

        result = jointRotation.eulerAngles.y;
        if (result > 180f)
        {
            result -= 360f;
        }
        // -- thumb has far more range of motion, but spread frequently glitches, esp. when making a fist.
        if (leapFinger.Type == Leap.Finger.FingerType.TYPE_THUMB)
            result = Map(Mathf.Clamp(result, 0, 40), 0, 40, -1, 1);
        else
            result = Map(result, -15, 15, -1, 1);

        return result;
    }

    Vector3 HandRotations(Leap.Hand leapHand)
    {
        Vector3 result = new Vector3();

        Quaternion localHand;
        localHand = leapHand.Rotation.ToQuaternion();
        Vector3 angles = localHand.eulerAngles;
        result.x = ((angles.z + 180) % 360) - 180;
        result.y = ((angles.x + 180) % 360) - 180;
        result.z = ((angles.y + 180) % 360) - 180;

        return result;
    }

    Vector3 HandPostion(Leap.Hand leapHand)
    {
        return leapHand.WristPosition.ToVector3();
    }

    // -- Forearm rotation calculation
    float ElbowRotation(Leap.Hand leapHand, float previousRotation, Vector3 forearm, float shoulderRotation)
    {
        float result;
        if (leapHand.IsLeft)
        {
            // -- I have no idea why this works but it seems to
            result = -(Mathf.Atan2(-forearm.y, -forearm.x) * Mathf.Rad2Deg);
            //result = shoulderRotation - result;
        }
        else
        {

            result = (Mathf.Atan2(forearm.y, forearm.x) * Mathf.Rad2Deg);
            result = result - shoulderRotation;
        }

        return result;
    }

    // ===================================================================================
    // Set Leap Mode
    // ===================================================================================
    public void SetLeapMode()
    {
        PlayerPrefs.SetInt("leapMode", LeapPlacementOptions.value);
    }

    // ===================================================================================
    // Set Leap Motion Location
    // ===================================================================================
    public void SetLeapPosX()
    {
        float v = PosXSlider.value;
        Vector3 pos = provider.gameObject.transform.position;
        pos.x = v;
        provider.gameObject.transform.position = pos;
        PlayerPrefs.SetFloat("leapPosX", v);
    }

    public void SetLeapPosY()
    {
        float v = PosYSlider.value;
        Vector3 pos = provider.gameObject.transform.position;
        pos.y = v;
        provider.gameObject.transform.position = pos;
        PlayerPrefs.SetFloat("leapPosY", v);
    }

    public void SetLeapPosZ()
    {
        float v = PosZSlider.value;
        Vector3 pos = provider.gameObject.transform.position;
        pos.z = v;
        provider.gameObject.transform.position = pos;
        PlayerPrefs.SetFloat("leapPosZ", v);
    }

    public void SetLeapRotX()
    {
        float v = RotXSlider.value;
        Vector3 rot = provider.gameObject.transform.rotation.eulerAngles;
        rot.x = v;
        provider.gameObject.transform.rotation = Quaternion.Euler(rot);
        PlayerPrefs.SetFloat("leapRotX", v);
    }

    public void SetLeapRotY()
    {
        float v = RotYSlider.value;
        Vector3 rot = provider.gameObject.transform.rotation.eulerAngles;
        rot.y = v;
        provider.gameObject.transform.rotation = Quaternion.Euler(rot);
        PlayerPrefs.SetFloat("leapRotY", v);
    }

    public void SetLeapRotZ()
    {
        float v = RotZSlider.value;
        Vector3 rot = provider.gameObject.transform.rotation.eulerAngles;
        rot.z = v;
        provider.gameObject.transform.rotation = Quaternion.Euler(rot);
        PlayerPrefs.SetFloat("leapRotZ", v);
    }
}