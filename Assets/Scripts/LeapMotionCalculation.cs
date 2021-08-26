using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            public Vector3 WristRotation;
            public float ForearmExtension;
            public float ForearmRotation;
            public float UpperarmExtension;
            public float UpperarmRotation;

            public Hand(string _name)
            {
                Name = _name;
                Fingers = new List<Finger>();

                for (int f = 0; f < 5; ++f)
                {
                    Fingers.Add(new Finger());
                }
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
        public GameObject NeckBase;

        List<Hand> hands;

        private void Start()
        {
            hands = new List<Hand>();

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
                Vector3 center = NeckBase.transform.position;
                if (h == 0)
                    center = center + (Vector3.left * ShoulderWidth);
                else
                    center = center + (Vector3.right * ShoulderWidth);

                // -- Upperarm calculations
                Vector3 elbowPos = leapHand.Arm.ElbowPosition.ToVector3() - center;
                Vector3 elbowProj = Vector3.ProjectOnPlane(elbowPos, Vector3.forward);
                hand.UpperarmExtension = (elbowProj.magnitude) / (elbowPos.magnitude);

                Vector3.down.GetAxisFromToRotation(elbowProj, Vector3.forward, out hand.UpperarmRotation);
                if (hand.UpperarmRotation > 180f)
                {
                    hand.UpperarmRotation -= 360f;
                }

                // -- Forearm calculations
                Vector3 wristPos = leapHand.WristPosition.ToVector3() - center;
                wristPos = wristPos - elbowPos;
                Vector3 wristProj = Vector3.ProjectOnPlane(wristPos, Vector3.forward);
                hand.ForearmExtension = (wristProj.magnitude) / (wristPos.magnitude);

                Vector3.down.GetAxisFromToRotation(wristProj, Vector3.forward, out hand.ForearmRotation);
                if (hand.ForearmRotation > 180f)
                {
                    hand.ForearmRotation -= 360f;
                }

                // -- Wrist rotation calculations
                Quaternion wristRot = leapHand.Rotation.ToQuaternion();
                hand.WristRotation = wristRot.eulerAngles;
                if (hand.WristRotation.x > 180f)
                {
                    hand.WristRotation.x -= 360f;
                }
                if (hand.WristRotation.y > 180f)
                {
                    hand.WristRotation.y -= 360f;
                }
                if (hand.WristRotation.z > 180f)
                {
                    hand.WristRotation.z -= 360f;
                }

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
                        finger.SideRotation = jointRotation.eulerAngles.y;
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
            return hands[hand].UpperarmRotation;
        }

        // ===================================================================================
        // Euler angles of the rotation of the wrist 
        // ===================================================================================
        public Vector3 GetWristRotation(int hand)
        {
            return hands[hand].WristRotation;
        }
    }
}
