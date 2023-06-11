using UnityEngine;
using System.Linq;
using System.Collections.Generic;



public class BvhSkeleton
{


    public List<BvhJointType> bvhJoints = new List<BvhJointType>() {

        BvhJointType.Hips,    BvhJointType.Spine,  BvhJointType.Spine1, BvhJointType.Spine2, BvhJointType.Spine3,

        BvhJointType.LeftHand,  BvhJointType.LeftForeArm,    BvhJointType.LeftArm,  BvhJointType.LeftShoulder,
        BvhJointType.RightHand,BvhJointType.RightForeArm, BvhJointType.RightArm,  BvhJointType.RightShoulder,

        BvhJointType.LeftToeBase, BvhJointType.LeftForeFoot, BvhJointType.LeftFoot, BvhJointType.LeftLeg, BvhJointType.LeftUpLeg,
        BvhJointType.RightToeBase,    BvhJointType.RightForeFoot,  BvhJointType.RightFoot, BvhJointType.RightLeg, BvhJointType.RightUpLeg,
        BvhJointType.Head, BvhJointType.Neck, BvhJointType.Neck1,


          BvhJointType.LeftHandThumb1,BvhJointType.LeftHandThumb2,  BvhJointType.LeftHandThumb3,
       
           BvhJointType.LeftHandIndex1,      BvhJointType.LeftHandIndex2,
          BvhJointType.LeftHandIndex3,BvhJointType.LeftHandMiddle1, BvhJointType.LeftHandMiddle2, BvhJointType.LeftHandMiddle3,
          BvhJointType.LeftHandRing1,  BvhJointType.LeftHandRing2,   BvhJointType.LeftHandRing3,
         BvhJointType.LeftHandLittle1,BvhJointType.LeftHandLittle2,  BvhJointType.LeftHandLittle3,   BvhJointType.RightHandThumb1,
         BvhJointType.RightHandThumb2,    BvhJointType.RightHandThumb3,


           BvhJointType.RightHandIndex1,BvhJointType.RightHandIndex2,  BvhJointType.RightHandIndex3,
        BvhJointType.RightHandMiddle1, BvhJointType.RightHandMiddle2, BvhJointType.RightHandMiddle3,

        BvhJointType.RightHandRing1, BvhJointType.RightHandRing2,  BvhJointType.RightHandRing3,

        BvhJointType.RightHandLittle1,  BvhJointType.RightHandLittle2,  BvhJointType.RightHandLittle3,


};




    //void BvhSkeleton()
    //{

    // Fill bvhJoints
    // foreach (SimpleJoint x in this.bvhJoints)
    // {
    //     HumanBodyBones unityBoneId = x.nuitrackJoint.ToUnityBones(); // The Unity Bone corresponding to the bvhtrackJoint
    //     // unityBoneType will be 55 when there is  no unity bone corresponding to the  bvhtrack bone
    //     // Returns Transform mapped to this human bone id, unityBoneId

    //     Transform boneTransform = this.animator.GetBoneTransform(unityBoneId); // c Transform GetBoneTransform(HumanBodyBones humanBoneId);

    //     x.UnityBoneTransform = boneTransform;
    //     x.UnityOffset = boneTransform.rotation; // Quaternion

    // }
    //}

    // .ToUnityBones() is the extension method of  item.nuitrackJoint, which belongs to type JointType 

    //  public static HumanBodyBones ToUnityBones(this JointType nuitrackJoint)
    // {
    //     if (nuitrackToUnity.ContainsKey(nuitrackJoint))
    //         return nuitrackToUnity[nuitrackJoint];
    //     else
    //         return HumanBodyBones.LastBone;
    // }


    public HumanBodyBones[] GetHumanBodyBones
    {
        get
        {
            return this.bvhJoints.Select(x => x.ToUnityBones()).ToArray();
            // BvhJointType.ToUnityBones() is defined in public static class BvhTrackUtils
        }
    }

} // public class BvhSkeleton


public class  BvhJoint
{
    public string bvhJointName;
    public BvhJointType bvhTrackJoint;
     // = BvhJointType.None; //  nuitrack.JointType.None == 0

     BvhJoint( string jointName,  BvhJointType jointType)
     {
        this.bvhJointName = jointName;
        this.bvhTrackJoint = jointType;
     }

   // public Quaternion UnityOffset { get; set; }

   // public Transform UnityBoneTransform { get; set; }
}


public enum BvhJointType // a total of 56 => only 52 are used, since they correspond to Unity Humanbones
    { 
        Hips = 0, Spine  =1, 
         Spine1=2, Spine2=3,   Spine3 = 4,      

        LeftHand = 5, LeftForeArm=6, LeftArm=7,LeftShoulder=8,
 
        RightHand=9, RightForeArm = 10, RightArm=11, RightShoulder=12,
        LeftToeBase=13,      LeftForeFoot = 14, LeftFoot=15, LeftLeg=16, LeftUpLeg = 17,    

       RightToeBase=18,   RightForeFoot=19, RightFoot=20, RightLeg=21,RightUpLeg=22, 
       
       Head=23, Neck1 = 24, Neck = 25,
       
       LeftHandThumb1 = 26,  LeftHandThumb2 =27,LeftHandThumb3 = 28,
      
          LeftHandIndex1 = 29, LeftHandIndex2=30, LeftHandIndex3=31,
          LeftHandMiddle1=32, LeftHandMiddle2=33,  LeftHandMiddle3 = 34,
          LeftHandRing1 =  35,   LeftHandRing2 = 36, LeftHandRing3=37,
          
        LeftHandLittle1 = 38, LeftHandLittle2 = 39, LeftHandLittle3 = 40,
    
         RightHandThumb1 = 41,RightHandThumb2=42,RightHandThumb3=43,
          RightHandIndex1=44,     RightHandIndex2=45,RightHandIndex3=46,

     
        RightHandMiddle1=47,RightHandMiddle2=48,RightHandMiddle3=49,
     
        RightHandRing1 = 50, RightHandRing2 = 51, RightHandRing3  = 52,         
        RightHandLittle1 = 53, RightHandLittle2 = 54, RightHandLittle3 = 55,
        //pCube4 = 56
    }


// namespace nuitrack
// {
//     public enum JointType
//     {
//         None = 0,
//         Head = 1,
//         Neck = 2,
//         Torso = 3,
//         Waist = 4,
//         LeftCollar = 5,
//         LeftShoulder = 6,
//         LeftElbow = 7,
//         LeftWrist = 8,
//         LeftHand = 9,
//         LeftFingertip = 10,
//         RightCollar = 11,
//         RightShoulder = 12,
//         RightElbow = 13,
//         RightWrist = 14,
//         RightHand = 15,
//         RightFingertip = 16,
//         LeftHip = 17,
//         LeftKnee = 18,
//         LeftAnkle = 19,
//         LeftFoot = 20,
//         RightHip = 21,
//         RightKnee = 22,
//         RightAnkle = 23,
//         RightFoot = 24
//     }
// }