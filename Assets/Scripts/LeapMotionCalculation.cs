using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Leap.Unity
{
    public class LeapMotionCalculation : MonoBehaviour
    {
        // -----------------------------------------------------------------------------------
        // Helper function to get name of finger
        // -----------------------------------------------------------------------------------
        public string FingerName(int finger)
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

        // -----------------------------------------------------------------------------------
        // All information for a hand
        // -----------------------------------------------------------------------------------
        class Hand
        {
            public string Name;
            public List<Finger> Fingers;
            public Vector3 WristRotPrev;
            public Vector3 WristRotation;
            public Vector3 WristPosition;

            public float ForearmExtension;
            public Vector3 ForearmPrev;
            public float ForearmRotation;

            public float UpperarmExtension;
            public Vector3 UpperarmPrev;
            public float UpperarmRotation;

            public Hand(string _name)
            {
                Name = _name;
                Fingers = new List<Finger>();

                for (int f = 0; f < 5; ++f)
                {
                    Fingers.Add(new Finger());
                }

                ForearmPrev = Vector3.down;
                UpperarmPrev = Vector3.down;
            }
        }

        // -----------------------------------------------------------------------------------
        // All information for a finger
        // -----------------------------------------------------------------------------------
        class Finger
        {
            public float SideRotation;
            public float BaseRotation;
            public float TotalRotation;
        }

        public LeapProvider provider;

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

        public Dropdown LeapOptions;

        List<Hand> hands;

        private void Start()
        {
            hands = new List<Hand>();

            for(int i = 0; i <= (int)TestHandFactory.TestHandPose.ScreenTop; ++i)
            {
                LeapOptions.options.Add(new Dropdown.OptionData(((TestHandFactory.TestHandPose)i).ToString()));
            }

            int val = (int)provider.editTimePose;
            if (PlayerPrefs.HasKey("leapMode"))
                val = PlayerPrefs.GetInt("leapMode");
            LeapOptions.value = val;


            // -- init hands
            // -- left = 0, right = 1, always
            for(int h = 0; h < 2; ++h)
            {
                string name = "Left";
                if (h == 1)
                {
                    name = "Right";
                }

                hands.Add(new Hand(name));
            }

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

            Height.onValueChanged.AddListener(delegate { SetShoulderHeight(); });
            Width.onValueChanged.AddListener(delegate { SetShoulderWidth(); });
            Depth.onValueChanged.AddListener(delegate { SetShoulderDepth(); });
            LeapOptions.onValueChanged.AddListener(delegate { SetLeapMode(); });
        }

        // ===================================================================================
        // Update values from leap motion and calculate values we need
        // ===================================================================================
        private void FixedUpdate()
        {
            // -- Update hand values
            for (int h = 0; h < provider.CurrentFrame.Hands.Count; ++h)
            {
                Hand hand = hands[0];

                // -- Determine what hand we're using
                Leap.Hand leapHand = provider.CurrentFrame.Hands[h];
                if (leapHand.IsLeft)
                {
                    hand = hands[0];
                }
                else if (leapHand.IsRight)
                {
                    hand = hands[1];
                }
                else
                    continue;

                // -- Calculate base of neck
                Vector3 center = NeckBase.position;
                if (leapHand.IsRight)
                    center = RShoulder.position;
                else if (leapHand.IsLeft)
                    center = LShoulder.position;

                // -- Upperarm extension
                Vector3 elbowPos = center - leapHand.Arm.ElbowPosition.ToVector3();

                // -- debug draw
                if (leapHand.IsLeft)
                    LElbow.position = leapHand.Arm.ElbowPosition.ToVector3();
                else if (leapHand.IsRight)
                    RElbow.position = leapHand.Arm.ElbowPosition.ToVector3();

                Vector3 elbowProj = Vector3.ProjectOnPlane(elbowPos, Vector3.forward);
                hand.UpperarmExtension = (elbowProj.magnitude) / (elbowPos.magnitude);

                // -- Upperarm rotation calculations
                float deltaUpperarm;
                hand.UpperarmPrev.GetAxisFromToRotation(elbowProj, Vector3.forward, out deltaUpperarm);
                hand.UpperarmPrev = elbowProj;
                if(leapHand.IsLeft)
                    hand.UpperarmRotation -= deltaUpperarm;
                else
                    hand.UpperarmRotation += deltaUpperarm;

                // -- Forearm extension calculations
                Vector3 wristPos = leapHand.WristPosition.ToVector3() - center;
                wristPos = wristPos - elbowPos;
                Vector3 wristProj = Vector3.ProjectOnPlane(wristPos, Vector3.forward);
                hand.ForearmExtension = (wristProj.magnitude) / (wristPos.magnitude);

                // -- Forearm rotation calculations
                float forearmRotDelta;
                hand.ForearmPrev.GetAxisFromToRotation(wristProj, Vector3.forward, out forearmRotDelta);
                hand.ForearmPrev = wristProj;
                if (leapHand.IsRight)
                    hand.ForearmRotation -= forearmRotDelta;
                else
                    hand.ForearmRotation += forearmRotDelta;

                // -- Wrist position
                hand.WristPosition = leapHand.WristPosition.ToVector3();

                // -- Wrist rotation calculations
                Vector3 palm = leapHand.PalmNormal.ToVector3();
                //Debug.Log("Palm: " + palm);
                hand.WristRotation = palm;
                hand.WristRotPrev = palm;

                // -- Finger calculations
                for (int f = 0; f < hand.Fingers.Count; ++f)
                {
                    Finger finger = hand.Fingers[f];
                    Leap.Finger leapFinger = leapHand.Fingers[f];

                    if (leapFinger != null)
                    {
                        // -- Spread/wagging calculation
                        Quaternion jointRotation = Quaternion.Inverse(leapFinger.bones[0].Rotation.ToQuaternion())
                                                   * leapFinger.bones[1].Rotation.ToQuaternion();

                        // -- clamp change within 3 degrees
                        finger.SideRotation = finger.SideRotation + Mathf.Clamp(jointRotation.eulerAngles.y - finger.SideRotation, -3, 3);
                        if (finger.SideRotation > 180f)
                        {
                            finger.SideRotation -= 360f;
                        }
                        // -- thumb has far more range of motion, but spread frequently glitches, esp. when making a fist.
                        if(leapFinger.Type != Leap.Finger.FingerType.TYPE_THUMB)
                            finger.SideRotation = Mathf.Clamp(finger.SideRotation, -15, 15); ;

                        // -- Base joint rotation calculation
                        finger.BaseRotation = jointRotation.x;
                        if (finger.BaseRotation > 180f)
                        {
                            finger.BaseRotation -= 360f;
                        }

                        // -- Total finger rotation calculation, just average the rotation of each joint
                        float total = 0;
                        for (int j = 1; j < leapHand.Fingers[f].bones.Length - 1; ++j)
                        {
                            Quaternion jointRot = Quaternion.Inverse(leapFinger.Bone((Bone.BoneType)(j)).Rotation.ToQuaternion())
                                                  * leapFinger.Bone((Bone.BoneType)(j+1)).Rotation.ToQuaternion();
                            total += jointRot.eulerAngles.x;
                        }
                        finger.TotalRotation = total / leapHand.Fingers[f].bones.Length;
                        if (finger.TotalRotation > 50f)
                        {
                            finger.TotalRotation = 0;
                        }
                    }
                }
            }
        }

        // ===================================================================================
        // How many hands are there
        // ===================================================================================
        public int HandCount()
        {
            return hands.Count;
        }

        // ===================================================================================
        // A strign representation of each hand
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
        // The averaged rotation of the joints on one hand
        // ===================================================================================
        public float GetFingerRotation(int hand, int finger)
        {
            return hands[hand].Fingers[finger].TotalRotation;
        }

        // ===================================================================================
        // The side to side rotation of a finger, like when you spread your fingers
        // Returns an angle in degrees. For all fingers except thumb, clamped to -15 to 15
        // ===================================================================================
        public float GetFingerSpread(int hand, int finger)
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
        // Rotation of the forearm from the elbow.
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
        // Rotation of the arm from the shoulder.
        // ===================================================================================
        public float GetUpperarmRotation(int hand)
        {
            float rot = hands[hand].UpperarmRotation;
            if (rot < 0)
                rot = rot + 360;

            
            return rot - 180;
        }

        // ===================================================================================
        // Euler angles of the rotation of the wrist 
        // ===================================================================================
        public Vector3 GetWristRotation(int hand)
        {
            return hands[hand].WristRotation;
        }

        // ===================================================================================
        // World position of wrist
        // ===================================================================================
        public Vector3 GetWristPosition(int hand)
        {
            return hands[hand].WristPosition;
        }

        // ===================================================================================
        // Shoulder height change
        // ===================================================================================
        public void SetShoulderHeight()
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

        public void SetShoulderWidth()
        {
            ShoulderWidth = Width.value;

            RShoulder.position = NeckBase.position + ((ShoulderWidth / 2f) * Vector3.right);
            LShoulder.position = NeckBase.position + ((ShoulderWidth / 2f) * Vector3.left);

            PlayerPrefs.SetFloat("width", ShoulderWidth);
        }

        public void SetShoulderDepth()
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

        public void SetLeapMode()
        {
            PlayerPrefs.SetInt("leapMode", LeapOptions.value);
        }
    }
}
