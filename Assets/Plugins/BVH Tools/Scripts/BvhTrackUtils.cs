using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public static class BvhTrackUtils
{
   
    static readonly Dictionary<BvhJointType, HumanBodyBones> bvhToUnity = new Dictionary<BvhJointType, HumanBodyBones>() // a total of 52
    {
        // Body Joints 4 => 4
        {BvhJointType.Hips,               HumanBodyBones.Hips},   // -1,-1,-1        
        {BvhJointType.Spine,               HumanBodyBones.Spine},  // 2,1,0
         {BvhJointType.Spine1,               HumanBodyBones.Chest}, // 5,4,3
         {BvhJointType.Spine2,               HumanBodyBones.UpperChest}, // 8,7,6     
        
      
// Left Arms 4 => 8
        {BvhJointType.LeftHand,           HumanBodyBones.LeftHand},
        {BvhJointType.LeftForeArm,           HumanBodyBones.LeftLowerArm}, // 43,42
        {BvhJointType.LeftArm,        HumanBodyBones.LeftUpperArm},// 41,40

         {BvhJointType.LeftShoulder,          HumanBodyBones.LeftShoulder}, // x=-1, y=38;z=37



 // Right Arms    4 => 12
        {BvhJointType.RightHand,          HumanBodyBones.RightHand}, // x = -1, y=54, z = 53
        {BvhJointType.RightForeArm,          HumanBodyBones.RightLowerArm}, // x=52, y = -1, z = 51
        {BvhJointType.RightArm,       HumanBodyBones.RightUpperArm},

          {BvhJointType.RightShoulder,         HumanBodyBones.RightShoulder},

          // left legs 4 => 16

        {BvhJointType.LeftToeBase,            HumanBodyBones.LeftToes},
        {BvhJointType.LeftFoot,           HumanBodyBones.LeftFoot},
        {BvhJointType.LeftLeg,            HumanBodyBones.LeftLowerLeg},
        {BvhJointType.LeftUpLeg,             HumanBodyBones.LeftUpperLeg},

        // Right Legs 4  => 20

        {BvhJointType.RightToeBase,           HumanBodyBones.RightToes},
        {BvhJointType.RightFoot,          HumanBodyBones.RightFoot},
        {BvhJointType.RightLeg,           HumanBodyBones.RightLowerLeg},
        {BvhJointType.RightUpLeg,            HumanBodyBones.RightUpperLeg},

        // Head 2 =>  22 

        {BvhJointType.Head,                HumanBodyBones.Head},
        {BvhJointType.Neck,                HumanBodyBones.Neck},

         // Left Fingers:
         // Left Thumb 3 => 25
          {BvhJointType.LeftHandThumb1,            HumanBodyBones.LeftThumbProximal},
          {BvhJointType.LeftHandThumb2,            HumanBodyBones.LeftThumbIntermediate},
          {BvhJointType.LeftHandThumb3,       HumanBodyBones.LeftThumbDistal},


        // Left Index 3 => 28
           {BvhJointType.LeftHandIndex1,            HumanBodyBones.LeftIndexProximal},
          {BvhJointType.LeftHandIndex2,            HumanBodyBones.LeftIndexIntermediate},
          {BvhJointType.LeftHandIndex3,       HumanBodyBones.LeftIndexDistal},

         // Left Middle => 31
        {BvhJointType.LeftHandMiddle1,            HumanBodyBones.LeftMiddleProximal},
          {BvhJointType.LeftHandMiddle2,            HumanBodyBones.LeftMiddleIntermediate},
          {BvhJointType.LeftHandMiddle3,       HumanBodyBones.LeftMiddleDistal},


        // Left Ring => 34
        {BvhJointType.LeftHandRing1,            HumanBodyBones.LeftRingProximal},
          {BvhJointType.LeftHandRing2,            HumanBodyBones.LeftRingIntermediate},
          {BvhJointType.LeftHandRing3,       HumanBodyBones.LeftRingDistal},

           // Left Little => 37
        {BvhJointType.LeftHandLittle1,            HumanBodyBones.LeftLittleProximal},
          {BvhJointType.LeftHandLittle2,            HumanBodyBones.LeftLittleIntermediate},
          {BvhJointType.LeftHandLittle3,       HumanBodyBones.LeftLittleDistal},

           // Right Fingers:
         // Right Thumb => 40
          {BvhJointType.RightHandThumb1,            HumanBodyBones.RightThumbProximal},
          {BvhJointType.RightHandThumb2,            HumanBodyBones.RightThumbIntermediate},
          {BvhJointType.RightHandThumb3,       HumanBodyBones.RightThumbDistal},


        // Left Index => 43
           {BvhJointType.RightHandIndex1,            HumanBodyBones.RightIndexProximal},
          {BvhJointType.RightHandIndex2,            HumanBodyBones.RightIndexIntermediate},
          {BvhJointType.RightHandIndex3,       HumanBodyBones.RightIndexDistal},

         // Left Middle => 46
        {BvhJointType.RightHandMiddle1,            HumanBodyBones.RightMiddleProximal},
          {BvhJointType.RightHandMiddle2,            HumanBodyBones.RightMiddleIntermediate},
          {BvhJointType.RightHandMiddle3,       HumanBodyBones.RightMiddleDistal},


        // Left Ring =>  49
        {BvhJointType.RightHandRing1,            HumanBodyBones.RightRingProximal},
          {BvhJointType.RightHandRing2,            HumanBodyBones.RightRingIntermediate},
          {BvhJointType.RightHandRing3,       HumanBodyBones.RightRingDistal},

           // Left Little => 52
        {BvhJointType.RightHandLittle1,            HumanBodyBones.RightLittleProximal},
          {BvhJointType.RightHandLittle2,            HumanBodyBones.RightLittleIntermediate},
          {BvhJointType.RightHandLittle3,       HumanBodyBones.RightLittleDistal},


        

    }; // a total of 52


    /// <summary>
    /// Returns the appropriate HumanBodyBones  for BvhJointType
    /// </summary>
    /// <param name="bvhTrackJoint">BvhJointType</param>
    /// <returns>HumanBodyBones</returns>
    public static HumanBodyBones ToUnityBones(this BvhJointType bvhTrackJoint) // extension method: ToUnityBones is a method of "this" BvhJointType
    {
        if ( bvhToUnity.ContainsKey(bvhTrackJoint))
            return bvhToUnity[bvhTrackJoint];
        else
            return HumanBodyBones.LastBone; // == 55
    }


} // public static class BvhtrackUtils