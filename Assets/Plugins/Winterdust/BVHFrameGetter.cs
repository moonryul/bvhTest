using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Winterdust; // for BVH class

using System;

using System.IO;
public class BVHFrameGetter : MonoBehaviour
// This class is Attached to gameObject "Skeleton", which is referred by this.bvhAnimator.gameObject in BVHAnimationRetargetter.cs

{

    // https://winterdust.itch.io/bvhimporterexporter

    public Transform avatarRootTransform; // initialized to null
    public List<string> jointPaths = new List<string>(); // initialized to an empty list of strings
    public List<Transform> avatarTransforms = new List<Transform>();
    // It creates an empty list of Transforms that you can directly use without the need to assign it later.
    // This way, even if GetComponent<BVHFrameGetter>().avatarCurrentTransforms returns null, this.avatarCurrentTransforms will be an empty list (Count will be 0) rather than null.
    // You can start adding or removing Transform elements from the list without encountering null reference exceptions.
    // The transforms for Skeleton gameObject; This is the bvhCurrentTransform set from the current frame of the bvh motion file

 
    public string bvhFileName = ""; // == Assets/Recording_001.bvh
    public BVH bvh;

    public int frameNo = 0;
    public double secondsPerFrame;
    public int frameCount;

    public GameObject skeletonGO = null;



    void Start()
    {

       
        // // Create a Skeleton hiearchy if it  is not yet created. 

       // this.skeletonGO = GameObject.FindGameObjectWithTag("Skeleton");

        if (this.skeletonGO == null)
        {

             Debug.Log(" BVH Skeleton Should have been created by BVHSkeletonCreator component and saved as a Prefab asset");
             return;

            // // (1)  create a skeleton of transforms for the skeleton hierarchy, 
            // // (2) add a BVHDebugLines component to it so it becomes visible like a stick figur [
            // // BVHDebugLines bvhdebugLines = gameObject.AddComponent<BVHDebugLines>() ],
            // // (Lines are simply drawn between the bone heads (and ends))d  
            // // (3) return the skeleton, which is a hierarchy of GameObjects.

            // // Create all gameObjects hiearchy and the components needed to render the gameObjects for bvh.Allbones[].

            // this.skeletonGO = this.bvh.makeDebugSkeleton(animate: false, skeletonGOName: "Skeleton");
            // // => if animate = false, dot not create an animation clip but only the rest pose; 
            // // Create BvhDebugLines component and MeshRenderer component,
            // //  but do not create Animation component:
            // // public GameObject makeDebugSkeleton(bool animate = true, string colorHex = "ffffff", float jointSize = 1f, int frame = -1, bool xray = false, bool includeBoneEnds = true, string skeletonGOName = "Skeleton", bool originLine = false)

            // Debug.Log(" bvh Skeleton is  created");

            // this.avatarRootTransform = this.skeletonGO.transform.GetChild(0); // the Hips joint: The first child of SkeletonGO
            // // Set this.avatarTransforms refer to the children transforms of this.avatarRootTransform (Hip); Hip is the child of
            // // the Skeleton, which is this.bvhAnimator.gameObject.transform, used as the root of the Unity Humanoid avatar
            // // in  this.srcHumanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAnimator.gameObject.transform),
            // // in BVHAnimationRetargetter component. 
       
            // this.ParseAvatarRootTransform(this.avatarRootTransform, this.jointPaths, this.avatarTransforms);
                      

        }
        else
        {
            Debug.Log("In BVHFrameGetter: bvh Skeleton is already created and has been given Tag 'Skeleton' => Perfect ");

            //all gameObjects hiearchy and the components needed to render the gameObjects for bvh.Allbones[] are already available.

            // this.skeletonGO contains the pose of the skeleton obtained from the saved scene.


            //IMPORTANT:  Collect the transforms in the skeleton hiearchy into ***a list of transforms***,  this.avatarCurrentTransforms:
            // If you change  this.avatarCurrentTransforms, it affects the hierarchy of    this.skeletonGO , because both reference the same transforms;

            // this.avatarRootTransform = this.skeletonGO.transform.GetChild(0);
            // this.ParseAvatarRootTransform(this.avatarRootTransform, this.jointPaths, this.avatarTransforms);

            this.avatarRootTransform = this.gameObject.GetComponent<BVHSkeletonCreator>().avatarRootTransform;
            this.avatarTransforms = this.gameObject.GetComponent<BVHSkeletonCreator>().avatarTransforms;


            //MJ:  this.jointPaths and this.avatarCurrentTransforms are set within the above method.

            // MJ:  Load BVH motion data to this.bvh.allBones: No, we assume that when Skeleton is created, its bvh motion is also loaded
            // if (this.bvhFileName == "")
            // {
            //     Debug.Log(" In BVHFrameGetter: bvhFileName should be set in the inspector");
            //     throw new InvalidOperationException(" In BVHFrameGetter: No  Bvh FileName is set.");

            // }

            // //MJ: Note:
            // //public BVH(string pathToBvhFile, double importPercentage = 1.0, bool zUp = false, int calcFDirFromFrame = 0, int calcFDirToFrame = -1, bool ignoreRootBonePositions = false, bool ignoreChildBonePositions = true, bool fixFrameRate = true, bool parseMotionData = true, BVH.ProgressTracker progressTracker = null)

            // //MJ:  During Awake() of BVHFrameGetter Script, parse the bvh file and load the  hierarchy paths and  load motion data to this.bvh.allBones

            this.bvh = new BVH(bvhFileName, parseMotionData: true);
             // Load BVH motion data to this.bvh.allBones
             //  parseMotionData:false => only the skeleton and the rest pose is read;
             // This does not create the Skeleton, which is  created by  this.skeletonGO = this.bvh.makeDebugSkeleton(animate: false, skeletonGOName: "Skeleton");
             // in BVHSkeletonCreator.

            //this.bvh = this.gameObject.GetComponent<BVHSkeletonCreator>().bvh; This did  set  this.bvh.allBones.

            this.frameCount = this.bvh.frameCount;
            this.secondsPerFrame = this.bvh.secondsPerFrame; // 0.05f;
            // // Sets to 20 fps
            Time.fixedDeltaTime = (float)this.secondsPerFrame;

        }


    } // Start()


    // void ParseAvatarTransformRecursive(Transform child, string parentPath, List<string> jointPaths, List<Transform> transforms)
    // {
    //     string jointPath = parentPath.Length == 0 ? child.gameObject.name : parentPath + "/" + child.gameObject.name;
    //     // The empty string's length is zero

    //     jointPaths.Add(jointPath);
    //     transforms.Add(child);

    //     foreach (Transform grandChild in child)
    //     {
    //         ParseAvatarTransformRecursive(grandChild, jointPath, jointPaths, transforms);
    //     }

    //     // Return if child has no children, that is, it is a leaf node.
    // }

    // void ParseAvatarRootTransform(Transform rootTransform, List<string> jointPaths, List<Transform> avatarTransforms)
    // {
    //     jointPaths.Add(""); // The name of the root tranform path is the empty string
    //     avatarTransforms.Add(rootTransform);

    //     foreach (Transform child in rootTransform) // rootTransform class implements IEnuerable interface
    //     {
    //         ParseAvatarTransformRecursive(child, "", jointPaths, avatarTransforms);
    //     }
    // }

    // Update vs FixedUpdate: Update is called once per frame
    // MonoBehaviour.FixedUpdate has the frequency of the physics system; it is called every fixed frame-rate frame. Compute Physics system calculations after FixedUpdate.
    //  0.02 seconds (50 calls per second) is the default time between calls. Use Time.fixedDeltaTime to access this value. 
    //  Alter it by setting it to your preferred value within a script, or, navigate to Edit > Settings > Time > Fixed Timestep and set it there.
    //   The FixedUpdate frequency is more or less than Update. If the application runs at 25 frames per second (fps), 
    //   Unity calls it approximately twice per frame, Alternatively, 100 fps causes approximately two rendering frames with one FixedUpdate.
    //  Control the required frame rate and Fixed Timestep rate from Time settings. Use Application.targetFrameRate to set the frame rate.

    // Application.targetFrameRate: Specifies the frame rate at which Unity tries to render your game.
    // Both Application.targetFrameRate and QualitySettings.vSyncCount let you control your game's frame rate for smoother performance. 
    //targetFrameRate controls the frame rate by specifying the number of frames your game tries to render per second, whereas vSyncCount specifies the number of screen refreshes to allow between frames.


    // On all other platforms, Unity ignores the value of targetFrameRate if you set vSyncCount. When you use vSyncCount, Unity calculates the target frame rate by dividing the platform's default target frame rate by the value of vSyncCount.
    //For example, if the platform's default render rate is 60 fps and vSyncCount is 2, Unity tries to render the game at 30 frames per second.

    //public void GetCurrentFrame(List<Transform> avatarCurrentTransforms)
    // public void GetCurrentFrame()


    void FixedUpdate()

    //MJ:  this.skeletonGO.transform.GetChild(0) refers to this.avatarRootTransform, which is set every frame by BVHFrameGetters.cs
    // set the current position of the skeleton by setting its joints to the angles of the current motion, this.bvh.allBones
    {


        // If the current frame for the human character is from the bvh file, the FixedUpdate() of BVHFrameGetter component is
        // executed to set the current frame; Otherwise, the current frame will be set by the output of  AI gesticulator in AvatarController component.


       
            //this.frameNo = 0; // go to the beginning of the frame
            //return;

            // Update the transforms of skeletonGO bvh.secondsPerFrame


            // We need a formula that computes the two adjacent frameNumbers whose key times include a given time t, using bvh.secondsPerFrame, bvh.frameCount

            // THe position and the rotation of the Hips joint are assumed to be all zeros in our gesticulator experiment.
            // this.bvh.allBones[0].localFramePositions[this.frameNo] = new Vector3(0,0,0);
            // this.bvh.allBones[0].localFrameRotations[this.frameNo] = Quaternion.identity;
            
            Vector3 vector = this.bvh.allBones[0].localFramePositions[this.frameNo];

            Quaternion quaternion = this.bvh.allBones[0].localFrameRotations[this.frameNo]; // frameTIme = 50 ms from gesticulator
                                                                                            // update the pose of the avatar to the motion data of the current frame this.frameNo

            // this.avatarCurrentTransforms[0].localPosition = vector; // 0 ~ 56: a total of 57  ==> this.avatarCurrentTransforms holds the transforms of the Skeleton hierarchy
            // this.avatarCurrentTransforms[0].localRotation = quaternion;

            //MJ: We can get the root vector and quaternion from the motionPythonArray from gesticulator directly, without
            //using this.bvh.allBones[0].localFramePositions[this.frameNo];
            
            // Transform is a reference type, but vector and quaternion are value types.

            this.avatarTransforms[0].localPosition = vector; // 0 ~ 56: a total of 57  ==> this.avatarCurrentTransforms holds the transforms of the Skeleton hierarchy
            this.avatarTransforms[0].localRotation = quaternion;

            for (int b = 1; b < bvh.boneCount; b++) // boneCount= 57 ordered in depth first search of the skeleton hierarchy: bvh.boneCount = 57
                                                    // HumanBodyBones: Hips=0....; LastBone = 55
            {
                //Debug.Log(bvh.preparedAnimationClip.data[n].relativePath);            


                //Vector3 vector = bvh.allBones[b].localFramePositions[this.frameNo];
                // vector =  this.bvh.allBones[b].localRestPosition; // This line can be moved to BVHAnimationRetargetter to improve performance.

                //  We can get the  quaternion of b-th bone from the motionPythonArray from gesticulator directly, without
                //using this.bvh.allBones[b].localFrameRotations[this.frameNo];

                quaternion = this.bvh.allBones[b].localFrameRotations[this.frameNo];

                //  Update the pose of the avatar to the motion data of the current frame this.frameNo
                //this.avatarCurrentTransforms[b].localPosition = vector; //b: 0 ~ 56: a total of 57
                this.avatarTransforms[b].localRotation = quaternion;     // Set the local rotations of each frame  to the correspoding transform in the skeleton hiearchy;
                                                                                // This means the rest pose of the skeleton is important to the final pose



            } //  for each bone

            // Increment frameNo for the next fixed update call
            this.frameNo++;
            // check if the current frame number frameNo exceeds frameCount
            if (this.frameNo == this.bvh.frameCount) // frameNo = 0 ~ 519;  frameCount = 520
            {
                this.frameNo = 0; // go to the beginning of the frame

            }

    }
    //void FixedUpdate()


} // BVHFrameGetter : MonoBehaviour

