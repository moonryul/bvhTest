using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Winterdust; // for BVH class

using System;

using System.IO;

//The MenuItem attribute turns any static function into a menu command. Only static functions can use the MenuItem attribute.

public class BVHSkeletonCreator: MonoBehaviour

{

    // https://winterdust.itch.io/bvhimporterexporter

    public   Transform avatarRootTransform; // initialized to null
    public  List<string> jointPaths = new List<string>(); // initialized to an empty list of strings
    public   List<Transform> avatarTransforms = new List<Transform>();
    // It creates an empty list of Transforms that you can directly use without the need to assign it later.
    // This way, even if GetComponent<BVHFrameGetter>().avatarCurrentTransforms returns null, this.avatarCurrentTransforms will be an empty list (Count will be 0) rather than null.
    // You can start adding or removing Transform elements from the list without encountering null reference exceptions.
    // The transforms for Skeleton gameObject; This is the bvhCurrentTransform set from the current frame of the bvh motion file

 
    public   string bvhFileName ="";
    public   BVH bvh = null;

    public  GameObject skeletonGO = null;

    void Start()
    {
           // Load BVH motion data to this.bvh.allBones
            if (this.bvhFileName == "")
            {
                Debug.Log(" In Awake(), BVHFrameGetter: bvhFileName should be set in the inspector");
                throw new InvalidOperationException(" In BVHFrameGetter: No  Bvh FileName is set.");

            }

     
    
    
       //MJ: 
            //public BVH(string pathToBvhFile, double importPercentage = 1.0, bool zUp = false, int calcFDirFromFrame = 0, int calcFDirToFrame = -1, bool ignoreRootBonePositions = false, bool ignoreChildBonePositions = true, bool fixFrameRate = true, bool parseMotionData = true, BVH.ProgressTracker progressTracker = null)

            //MJ:  During Awake() of BVHFrameGetter Script, parse the bvh file and load the  hierarchy paths and  load motion data to this.bvh.allBones

            //VERY IMPORTANT:  when you get the motion file from the gesticulator, store it to this.bvh.allBones. Then the motion will be played.

            this.bvh = new BVH(bvhFileName, parseMotionData: false); 
             // when Skeleton is created, its bvh motion is NOT loaded  
             // parseMotionData:false: set the localFrameRotations to identity quaternion and the localFramePositions to null:

             
            //  for (int n = 0; n < this.boneCount; n++)
			// 			{
			// 				this.allBones[n].localFramePositions = null; //MJ: localFramePositions are set to null ?
			// 				for (int num16 = 0; num16 < this.frameCount; num16++)
			// 				{
			// 					this.allBones[n].localFrameRotations[num16] = Quaternion.identity;
			// 				}
			// 			}
          
            // Create the Skeleton from this.bvh:
            this.skeletonGO = bvh.makeDebugSkeleton(animate: false, skeletonGOName: "Skeleton");
            this.skeletonGO.transform.localScale *= 0.03f;
            //MJ: BVH file uses cm unit but Unity uses m unit, so we need to scale the bone lengths of the bvh charater.
            
            // => if animate = false, dot not create an animation clip but only the rest position of the skeleton,
            // this.skeletonGO.transform and its children transforms:
            // Refer to    setLocalPosRot(Transform boneTransform, ref int frame) in BVH.cs
            // Create BvhDebugLines component and MeshRenderer component,
            //  but do not create Animation component:
            // public GameObject makeDebugSkeleton(bool animate = true, string colorHex = "ffffff", float jointSize = 1f, int frame = -1, bool xray = false, bool includeBoneEnds = true, string skeletonGOName = "Skeleton", bool originLine = false)

            Debug.Log(" In Awake, BVHSkeletonCreator, bvh Skeleton is  created");

            this.avatarRootTransform = skeletonGO.transform.GetChild(0);
             // this.avatarRootTransform = the Hips joint: The first child of SkeletonGO
             // Set this.avatarTransforms refer to the children transforms of this.avatarRootTransform (Hip); Hip is the child of
             // the Skeleton, which is this.bvhAnimator.gameObject.transform, used as the root of the Unity Humanoid avatar
             // in  this.srcHumanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAnimator.gameObject.transform),
             // in BVHAnimationRetargetter component. 

            ParseAvatarRootTransform(this.avatarRootTransform, this.jointPaths, this.avatarTransforms);


    } //  void Start()

    static void  ParseAvatarTransformRecursive(Transform child, string parentPath, List<string> jointPaths, List<Transform> transforms)
    {
        string jointPath = parentPath.Length == 0 ? child.gameObject.name : parentPath + "/" + child.gameObject.name;
        // The empty string's length is zero

        jointPaths.Add(jointPath);
        transforms.Add(child);

        foreach (Transform grandChild in child)
        {
            ParseAvatarTransformRecursive(grandChild, jointPath, jointPaths, transforms);
        }

        // Return if child has no children, that is, it is a leaf node.
    }

    static void ParseAvatarRootTransform(Transform rootTransform, List<string> jointPaths, List<Transform> avatarTransforms)
    {
        jointPaths.Add(""); // The name of the root tranform path is the empty string
        avatarTransforms.Add(rootTransform);

        foreach (Transform child in rootTransform) // rootTransform class implements IEnuerable interface
        {
            ParseAvatarTransformRecursive(child, "", jointPaths, avatarTransforms);
        }
    }

}