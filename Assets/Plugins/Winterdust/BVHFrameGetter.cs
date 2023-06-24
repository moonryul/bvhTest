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

    public Transform avatarRootTransform; // defined in inspector
    public List<string> jointPaths = new List<string>(); // emtpy one
    public List<Transform> avatarCurrentTransforms = new List<Transform>();
    // The transforms for Skeleton gameObject; This is the bvhCurrentTransform set from the current frame of the bvh motion file

 
    public string bvhFileName = ""; // == Assets/Recording_001.bvh
    public BVH bvh;

    public int frameNo = 0;
    public double secondsPerFrame;
    public int frameCount;

    public GameObject skeletonGO = null;



    void Start()
    {
            // Create a Skeleton hiearchy if it  is not yet created. 

        this.skeletonGO = GameObject.FindGameObjectWithTag("Skeleton");

        if (this.skeletonGO == null)
        {

             Debug.Log(" Skeleton gameObject should have been created and added to the hierarchy by using BVHSkeletonCreator.cs");

             throw new InvalidOperationException("No   Skeleton gameObject is set");
            
        }
        else
        {
            Debug.Log(" bvh Skeleton is already created and has been given Tag 'Skeleton' ");

          

            //IMPORTANT:  Collect the transforms in the skeleton hiearchy into ***a list of transforms***,  this.avatarCurrentTransforms:
            // If you change  this.avatarCurrentTransforms, it affects the hierarchy of    this.skeletonGO , because both reference the same transforms;

            this.avatarRootTransform = this.skeletonGO.transform.GetChild(0);
            this.ParseAvatarRootTransform(this.avatarRootTransform, this.jointPaths, this.avatarCurrentTransforms);
            //MJ:  this.jointPaths and this.avatarCurrentTransforms are set within the above method.

            //Set the current transforms of the avatar to the initial pose,which is T-pose.
            

        }


        // Load BVH motion data to this.bvh.allBones
        if (this.bvhFileName == "")
        {
            Debug.Log(" bvhFileName should be set in the inspector");
            throw new InvalidOperationException("No  Bvh FileName is set.");

        }

        //MJ: Note:
        //public BVH(string pathToBvhFile, double importPercentage = 1.0, bool zUp = false, int calcFDirFromFrame = 0, int calcFDirToFrame = -1, bool ignoreRootBonePositions = false, bool ignoreChildBonePositions = true, bool fixFrameRate = true, bool parseMotionData = true, BVH.ProgressTracker progressTracker = null)

        this.bvh = new BVH(bvhFileName, parseMotionData: true); // Load BVH motion data to this.bvh.allBones

        this.frameCount = this.bvh.frameCount;
        this.secondsPerFrame = this.bvh.secondsPerFrame; // 0.05f;
        // Sets to 20 fps
        Time.fixedDeltaTime = (float)this.secondsPerFrame; 
        

       
        // BvhImporterExporter is delivered as a .dll file, everything is well documented with detailed XMLDOC descriptions.
        //  A .xml file is included, when placed next to the .dll the documentation can be seen from inside your script editor.

        // Most of the heavy work (actually importing the .bvh file) can be executed from a different thread if you want to make a preloader for your game. 
        // BvhImporterExporter has been optimized so it's blazingly fast, able to import thousands of animation frames in a very short time.

        // Get the transform sequence from Skeleton gameObject

        //this.avatarRootTransform = this.skeletonGO.transform.GetChild(0); // the Hips joint: The first child of SkeletonGO

        //this.ParseAvatarRootTransform(this.avatarRootTransform, this.jointPaths, this.avatarCurrentTransforms);

        // Debug.Log(" bvhFile has been read in Awake() of BVHFrameGetter");
        // => this.avatarCurrentTransforms holds the transforms of the skeleton hiearchy
    } // Awake()



    void ParseAvatarTransformRecursive(Transform child, string parentPath, List<string> jointPaths, List<Transform> transforms)
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

    void ParseAvatarRootTransform(Transform rootTransform, List<string> jointPaths, List<Transform> avatarTransforms)
    {
        jointPaths.Add(""); // The name of the root tranform path is the empty string
        avatarTransforms.Add(rootTransform);

        foreach (Transform child in rootTransform) // rootTransform class implements IEnuerable interface
        {
            ParseAvatarTransformRecursive(child, "", jointPaths, avatarTransforms);
        }
    }

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

        Vector3 vector = this.bvh.allBones[0].localFramePositions[this.frameNo];

        Quaternion quaternion = this.bvh.allBones[0].localFrameRotations[this.frameNo]; // frameTIme = 50 ms from gesticulator
                                                                                        // update the pose of the avatar to the motion data of the current frame this.frameNo

        // this.avatarCurrentTransforms[0].localPosition = vector; // 0 ~ 56: a total of 57  ==> this.avatarCurrentTransforms holds the transforms of the Skeleton hierarchy
        // this.avatarCurrentTransforms[0].localRotation = quaternion;

        //MJ: We can get the root vector and quaternion from the motionPythonArray from gesticulator directly, without
        //using this.bvh.allBones[0].localFramePositions[this.frameNo];

        this.avatarCurrentTransforms[0].localPosition = vector; // 0 ~ 56: a total of 57  ==> this.avatarCurrentTransforms holds the transforms of the Skeleton hierarchy
        this.avatarCurrentTransforms[0].localRotation = quaternion;

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
            this.avatarCurrentTransforms[b].localRotation = quaternion;     // Set the local rotations of each frame  to the correspoding transform in the skeleton hiearchy;
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


} // BVHTest class

