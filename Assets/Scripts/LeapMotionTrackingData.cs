using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Leap.Unity
{
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

        // -----------------------------------------------------------------------------------
        // All information for a hand
        // -----------------------------------------------------------------------------------
        class Hand
        {
            public string Name;
            public List<Finger> Fingers;
            public Vector3 WristRotPrev;
            public Vector3 WristRotation;
            public Vector3 WristRotationLocal;
            public Vector3 WristPosition;

            public float ForearmExtension;
            public Vector3 ForearmPrev;
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
        class Finger
        {
            public float SideRotation;
            public float TotalRotation;
        }

        public LeapProvider provider;

        public Dropdown LeapOptions;

        List<Hand> hands;

        protected override void Start()
        {
            base.Start();

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
                Hand hand = new Hand(name);

                if (h == 0)
                    hand.ForearmPrev = Vector3.left;
                else
                    hand.ForearmPrev = Vector3.right;
                hands.Add(hand);

            }

            LeapOptions.onValueChanged.AddListener(delegate { SetLeapMode(); });
        }

        // ===================================================================================
        // Update values from leap motion and calculate values we need
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
                }
                else if (leapHand.IsRight)
                {
                    hand = hands[1];
                }
                else
                    continue;

                trackingFound[h] = true;

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
                float rawUpperarmRotation = Vector3.SignedAngle(Vector3.down, elbowProj, Vector3.forward);
                hand.UpperarmRotation = rawUpperarmRotation;
                if (leapHand.IsLeft)
                    hand.UpperarmRotation = -hand.UpperarmRotation;


                Vector3 wristPos = leapHand.StabilizedPalmPosition.ToVector3() - elbowPos - center;
                Vector3 wristProj = Vector3.ProjectOnPlane(wristPos, Vector3.forward);

                // -- Forearm extension calculations
                hand.ForearmExtension = (wristProj.magnitude) / (wristPos.magnitude);

                // -- Forearm rotation calculation
                Vector3 localForearm = Quaternion.Euler(0, 0, -rawUpperarmRotation) * wristProj;
                hand.ForearmRotation = ElbowRotation(hand.ForearmRotation, hand.ForearmPrev, localForearm);
                hand.ForearmPrev = localForearm;

                // -- Wrist position
                hand.WristPosition = HandPostion(leapHand);

                // -- Wrist rotation calculations
                hand.WristRotation = HandRotations(leapHand, center);

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
                Quaternion jointRot = Quaternion.Inverse(leapFinger.Bone((Bone.BoneType)(j)).Rotation.ToQuaternion())
                                      * leapFinger.Bone((Bone.BoneType)(j + 1)).Rotation.ToQuaternion();
                total += jointRot.eulerAngles.x;
            }
            result = total / leapFinger.bones.Length;
            if (result > 50f)
            {
                result = 0;
            }

            // -- clamp to 0 - 1
            if (leapFinger.Type == Leap.Finger.FingerType.TYPE_THUMB)
                result = Map(result, 0, 20, 0, 1);
            else
                result = Map(result, 0, 30, 0, 1);
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

        Vector3 HandRotations(Leap.Hand leapHand, Vector3 center)
        {
            Vector3 result;
            
            Vector3 elbowPos = leapHand.Arm.ElbowPosition.ToVector3() - center;
            Quaternion shoulderRot = Quaternion.FromToRotation(Vector3.down, elbowPos);
            Quaternion elbowRot = Quaternion.FromToRotation(shoulderRot * Vector3.down, leapHand.StabilizedPalmPosition.ToVector3());

            Quaternion palm = leapHand.Rotation.ToQuaternion();
            //palm = Quaternion.Inverse(shoulderRot) * Quaternion.Inverse(elbowRot) * palm;
            result = palm.eulerAngles;
            result = new Vector3((result.x + 180) % 360, (result.y + 180) % 360, (result.z + 180) % 360);

            return result;
        }

        Vector3 HandPostion(Leap.Hand leapHand)
        {
            return leapHand.WristPosition.ToVector3();
        }

        // -- Forearm rotation calculation
        float ElbowRotation(float previous, Vector3 forearmPrevious, Vector3 localForearm)
        {
            float result = previous;

            result += Vector3.SignedAngle(forearmPrevious, localForearm, Vector3.forward);

            return result;
        }

        // -- Helper function to remap values to standard ranges
        static float Map(float x, float x1, float x2, float y1, float y2)
        {
            var m = (y2 - y1) / (x2 - x1);
            var c = y1 - m * x1;

            return m * x + c;
        }

        // ===================================================================================
        // How many hands are there
        // ===================================================================================
        public override int HandCount()
        {
            return hands.Count;
        }

        // ===================================================================================
        // A strign representation of each hand
        // ===================================================================================
        public override string HandName(int hand)
        {
            return hands[hand].Name;
        }

        // ===================================================================================
        // How many fingers are there on one hand
        // ===================================================================================
        public override int FingerCount(int hand)
        {
            return hands[hand].Fingers.Count;
        }

        // ===================================================================================
        // The averaged rotation of the joints on one hand
        // ===================================================================================
        public override float GetFingerRotation(int hand, int finger)
        {
            return hands[hand].Fingers[finger].TotalRotation;
        }

        // ===================================================================================
        // The side to side rotation of a finger, like when you spread your fingers
        // Returns an angle in degrees. For all fingers except thumb, clamped to -1 to 1
        // ===================================================================================
        public override float GetSideToSideRotation(int hand, int finger)
        {
            return hands[hand].Fingers[finger].SideRotation;
        }

        // ===================================================================================
        // How long the forearm is, 1 is long, 0 is short
        // ===================================================================================
        public override float GetForearmExtension(int hand)
        {
            return hands[hand].ForearmExtension;
        }

        // ===================================================================================
        // Rotation of the forearm from the elbow.
        // ===================================================================================
        public override float GetForearmRotation(int hand)
        {
            return hands[hand].ForearmRotation;
        }

        // ===================================================================================
        // How long the upperarm is, 1 is long, 0 is short
        // ===================================================================================
        public override float GetUpperarmExtension(int hand)
        {
            return hands[hand].UpperarmExtension;
        }

        // ===================================================================================
        // Rotation of the arm from the shoulder.
        // ===================================================================================
        public override float GetUpperarmRotation(int hand)
        {
            return hands[hand].UpperarmRotation;
        }

        // ===================================================================================
        // Euler angles of the rotation of the wrist 
        // ===================================================================================
        public override Vector3 GetWristRotation(int hand)
        {
            return hands[hand].WristRotation;
        }

        // ===================================================================================
        // World position of wrist
        // ===================================================================================
        public override Vector3 GetWristPosition(int hand)
        {
            return hands[hand].WristPosition;
        }

        // ===================================================================================
        // Whether hand was tracked this frame
        // ===================================================================================
        public override bool HandTracked(int hand)
        {
            return hands[hand].Found;
        }

        public void SetLeapMode()
        {
            PlayerPrefs.SetInt("leapMode", LeapOptions.value);
        }
    }
}
