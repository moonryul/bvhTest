using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using Unity.Collections;
    
public class AnimationClipOverrides : List<KeyValuePair<AnimationClip, AnimationClip>> // a list class of keyValuePairs
{
    public AnimationClipOverrides(int capacity) : base(capacity) {}

    public AnimationClip this[string name]  // indexer to AnimationClipOverides
    {
        get { return this.Find(x => x.Key.name.Equals(name)).Value; }
         //  T Find(Predicate<T> match); x refers to AnimationClip
         //  x.Key.name is the name of the animation clip
        set
        {
            int index = this.FindIndex(x => x.Key.name.Equals(name)); // x.Key refers to AnimationClip, which has a name as an Object
            if (index != -1)
                this[index] = new KeyValuePair<AnimationClip, AnimationClip>(this[index].Key, value);
                //  List class has its own indexer defined as public T this[int index] { get; set; }
                 // The First AnimationClip refers to the original clip and the second AnimationClip to the overriding clip
        }
    }
}


public class BVHAnimationRetargetter : MonoBehaviour
{
    public enum  AnimType {Legacy, Generic, Humanoid};
    public AnimType animType = AnimType.Generic;
    
    //public Avatar bvhAvatar;

    //public Transform bvhAvatarRootTransform; // to be set in inspector
    //public Transform saraAvatarRootTransform; // to be set in inspector

    public List<Transform> bvhAvatarCurrentTransforms = new List<Transform>();

    
    public GameObject skeletonGO; // the reference to skeletonGO comes from bvhFrameGetter.cs

    HumanPose humanPose = new HumanPose();
    BvhSkeleton bvhSkeleton = new BvhSkeleton();


    List<string> jointPaths = new List<string>(); // emtpy one
    List<Transform> avatarTransforms = new List<Transform>();

    HumanPoseHandler srcHumanPoseHandler;
    HumanPoseHandler destHumanPoseHandler;


    //HumanPoseHandler humanPoseHandler; 
    //HumanoidRecorder humanoidRecorder;
    NativeArray<float> avatarPose;

    List<int>  muscleIndecies = new List<int>(); 


    //GenericRecorder genericRecorder;


   float[][] values = new float[6][]; // the root transform data at keyframe for the current bvh file
   Keyframe[][] keyframes = new Keyframe[7][]; // the joint tansform data at key keyframes for the curernt bvh file

    // Field Initializer vs Setting within Constructors: https://stackoverflow.com/questions/298183/c-sharp-member-variable-initialization-best-practice?msclkid=7fa9d0edc04911ec89a1aa2251ca3533
       
   
    public  Animator bvhAnimator; // should be defined in the inspector
    public   Animator saraAnimator; // should be defined in the inspector
         
    protected AnimatorOverrideController animatorOverrideController; 
    
    protected AnimationClipOverrides clipOverrides;
     
        // public AnimationClip this[string name] { get; set; }
        // public AnimationClip this[AnimationClip clip] { get; set; }
    private string prefix;
    private int frames;
    private float deltaTime;
    private Dictionary<string, string> pathToBone;  // the default value of reference variable is null
    private Dictionary<string, string[]> boneToMuscles;  // the default value of reference variable is null
    

    public class Trans
 {
 
     public Vector3 localPosition;
     public Quaternion localRotation;
     public Vector3 localScale;
 
 // https://answers.unity.com/questions/156698/copy-a-transform.html
     public Trans (Vector3 newPosition, Quaternion newRotation, Vector3 newLocalScale)
     {
         this.localPosition = newPosition;
         this.localRotation = newRotation;
         this.localScale = newLocalScale;
     }
 
     public Trans ()
     {
         this.localPosition = Vector3.zero;
         this.localRotation = Quaternion.identity;
         this.localScale = Vector3.one;
     }
 
     public Trans (Transform transform)
     {
         this.copyFrom (transform);
     }
 
     public void copyFrom (Transform transform)
     {
         this.localPosition = transform.position;
         this.localRotation = transform.rotation;
         this.localScale = transform.localScale;
     }
 
     public void copyTo (Transform transform)
     {
         transform.localPosition = this.localPosition;
         transform.localRotation = this.localRotation;
         transform.localScale = this.localScale;
     }
 
 }

    void ParseAvatarTransformRecursive(Transform child, string parentPath, List<string> jointPaths, List<Transform> transforms)
    {
        string jointPath = parentPath.Length == 0 ? child.gameObject.name : parentPath + "/" + child.gameObject.name;
        // The empty string's length is zero

        jointPaths.Add(jointPath);

        if (transforms != null) 
        {
        transforms.Add(child);
        }

        foreach (Transform grandChild in child)
        {
            ParseAvatarTransformRecursive(grandChild, jointPath, jointPaths, transforms);
        }

        // Return if child has no children, that is, it is a leaf node.
    }

    void ParseAvatarRootTransform(Transform rootTransform, List<string> jointPaths, List<Transform> avatarTransforms)
    {
        jointPaths.Add(""); // The name of the root tranform path is the empty string

        if (avatarTransforms != null)
        {
            avatarTransforms.Add(rootTransform);
        }

        foreach (Transform child in rootTransform) // rootTransform class implements IEnuerable interface
        {
            ParseAvatarTransformRecursive(child, "", jointPaths, avatarTransforms);
        }
    }

    // BVH to Unity
    private Quaternion fromEulerZXY(Vector3 euler)
    {
        return Quaternion.AngleAxis(euler.z, Vector3.forward) * Quaternion.AngleAxis(euler.x, Vector3.right) * Quaternion.AngleAxis(euler.y, Vector3.up);
    }

    private float wrapAngle(float a)
    {
        if (a > 180f)
        {
            return a - 360f;
        }
        if (a < -180f)
        {
            return 360f + a;
        }
        return a;
    }



 void GetbvhTransformsForCurrentFrame(int i, BVHParser.BVHBone rootBvhNode, Transform avatarRootTransform, List<Transform> bvhTransforms )
    {
       //  copy the transform data from rootBvhNode to avatarRootTransform and add it to bvhTransfoms list, 
       //  and do the same for the children of rootBvhNode
        
        Vector3 rootOffset; //  r the root node "Hips" is away from the world cooordinate system by rootOffset initially
        rootOffset = new Vector3(-rootBvhNode.offsetX, rootBvhNode.offsetY, rootBvhNode.offsetZ);  // To unity Frame

        // if (this.blender) //  //  the BVH file will be assumed to have the Z axis as up and the Y axis as forward, X rightward as in Blender
        // {
        //     rootOffset = new Vector3(-rootBvhNode.offsetX, rootBvhNode.offsetZ, -rootBvhNode.offsetY); // => Unity frame
        // }
        // else //  //  the BVH file will be assumed to have the normal BVH convention: Y up; Z backward; X right (OpenGL: right handed)
        // {
        //     rootOffset = new Vector3(-rootBvhNode.offsetX, rootBvhNode.offsetY, rootBvhNode.offsetZ);  // To unity Frame
        //     // Unity:  Y: up, Z: forward, X = right or Y: up, Z =backward, X left (The above transform follows the second)
        // }


        Vector3 rootTranslation; // = new Vector3(keyframes[0][i].value, keyframes[1][i].value, keyframes[2][i].value);

        rootTranslation = new Vector3(-this.values[0][i], this.values[2][i], this.values[1][i]);

        rootTranslation = new Vector3(-rootBvhNode.channels_bvhBones[0].values[i], rootBvhNode.channels_bvhBones[2].values[i],
                                       rootBvhNode.channels_bvhBones[1].values[i]);

        // if (blender)
        // {
        //     // rootBvhNode.channels_bvhBones[channel].values;
        //    // rootTranslation = new Vector3(-this.values[0][i],   this.values[2][i],  -this.values[1][i]);

        //    rootTranslation = new Vector3(-rootBvhNode.channels_bvhBones[0].values[i],   rootBvhNode.channels_bvhBones[2].values[i], 
        //                                     -rootBvhNode.channels_bvhBones[1].values[i]);

        // }
        // else
        // {
        //     // rootTranslation = new Vector3(-this.values[0][i],   this.values[2][i],  this.values[1][i]);

        //        rootTranslation = new Vector3(-rootBvhNode.channels_bvhBones[0].values[i],   rootBvhNode.channels_bvhBones[2].values[i], 
        //                                       rootBvhNode.channels_bvhBones[1].values[i]);


        // }

        //  transform.transform has the same effect as transform.GetComponent(Transform).
        // Correct, just use transform; you would never need to use transform.transform. 
        // It's true that you'd prefer transform over GetComponent(Transform), though the speed difference is small.

        //new Vector3(keyframes[0][i].value, keyframes[1][i].value, keyframes[2][i].value) + bvhAnimator.transform.position + offset 
        // is the position of the root bvh node relative to the world coordinate system
        // => Transform it to the local vector relative to the coordinate system of the root node.

        // vector * vector could be interpreted as dot product or a cross product or the "component wise" product, 
        //so I totally agree with the decision of not implementing a custom * operator to force the developers to call the appropriate method.
        // And it happens that Vector3.Scale is already doing this.

        // ROOT Hips
        //{
        //   OFFSET -14.6414 90.2777 -84.916
        //   CHANNELS 6 Xposition Yposition Zposition Zrotation Xrotation Yrotation
        // Vector3 rootTranslation = new Vector3(keyframes[0][i].value, keyframes[1][i].value, keyframes[2][i].value);

        //    GameObject skeleton;
        //    if (this.animType == AnimType.Legacy) {
        //     skeleton = this.animation.gameObject;
        //    }
        //    else { // Generic or Humanoid Type
        //      skeleton = this.bvhAnimator.gameObject;
        //    }

        //Vector3 globalRootPos = skeleton.transform.position + offset + rootTranslation;
        // 

        Vector3 rootNodePos =  rootOffset; // relative to the world
        Vector3 globalRootPos = rootNodePos  + rootTranslation;
        Vector3 bvhPositionLocal = avatarRootTransform.InverseTransformPoint(  globalRootPos  ); // the parent is null
        //bvhPositionLocal =  Vector3.Scale(bvhPositionLocal, this.bvhAnimator.gameObject.transform.localScale);

        bvhPositionLocal =  Vector3.Scale(bvhPositionLocal,  avatarRootTransform.localScale);

            
        Vector3 eulerBVH = new Vector3(wrapAngle( rootBvhNode.channels_bvhBones[3].values[i]), 
                                        wrapAngle( rootBvhNode.channels_bvhBones[4].values[i]), 
                                         wrapAngle( rootBvhNode.channels_bvhBones[5].values[i] ));
        Quaternion rot = fromEulerZXY(eulerBVH); // Get the quaternion for the BVH ZXY Euler angles

        Quaternion rot2;
        rot2 = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
        // Change the coordinate system from BVH to Unity
        // if (blender)
        // {
        //     // keyframes[3][i].value = rot.x;
        //     // keyframes[4][i].value = -rot.z;
        //     // keyframes[5][i].value = rot.y;
        //     // keyframes[6][i].value = rot.w;
        //     rot2 = new Quaternion(rot.x, -rot.z, rot.y, rot.w);
        // }
        // else
        // {
        //     // keyframes[3][i].value = rot.x;
        //     // keyframes[4][i].value = -rot.y;
        //     // keyframes[5][i].value = -rot.z;
        //     // keyframes[6][i].value = rot.w;
        //     rot2 = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
        // }

      
      // Store the new root transform to the current rootTransform, which is used as a temporary variable to store the transform at the current frame i;
      // Changing the rootTransform is OK because when the generated animation clip will be used to play the virtual character.
      // The original pose of the virtual character will be set to the first frame of the animation clip, when the clip is played.

      
       avatarRootTransform.localPosition =  bvhPositionLocal;
       avatarRootTransform.localRotation = rot2;


        // public Quaternion localRotation { get; set; }:  The rotation of the transform relative to the transform rotation of the parent.
        // position and rotation atrributes are the values relative to the world space.
                 
       // jointPaths.Add(""); // root tranform path is the empty string

        bvhTransforms.Add( avatarRootTransform); // bvhTransforms is the contrainer of transforms in the skeleton path
        // restore the original root bvh node transform
       // rootTransform.transform.rotation = oldRotation;
        // Get the frame data for each child of the root node, recursively.
        foreach (BVHParser.BVHBone child in rootBvhNode.children)
        {
            Transform childTransform = avatarRootTransform.Find(child.name);

            GetbvhTransformsForCurrentFrameRecursive(i, child, childTransform, bvhTransforms);
        
        }

    } // void GetbvhTransformsForCurrentFrame(i, BVHParser.BVHBone bvhNode, Transform rootTransform, List<Transform> bvhTransforms )

    private void GetbvhTransformsForCurrentFrameRecursive( int i, BVHParser.BVHBone bvhNode,  Transform avatarNodeTransform, List<Transform> avatarTransforms)
       {                                                                   
        // copy the transform data from bvhNode to bvhNodeTransform and add it to bvhTransfoms list; We do it only for the rotation part of bvhNodeTransform

        Quaternion rot2;

        // the rotatation value of the joint center

        // Quaternion oldRotation = bvhNodeTransform.transform.rotation;

         Vector3 eulerBVH = new Vector3(wrapAngle( bvhNode.channels_bvhBones[3].values[i]), 
                                        wrapAngle( bvhNode.channels_bvhBones[4].values[i]), 
                                         wrapAngle(bvhNode.channels_bvhBones[5].values[i] ));
        //Vector3 eulerBVH = new Vector3(wrapAngle(values[3][i]), wrapAngle(values[4][i]), wrapAngle(values[5][i]));
        Quaternion rot = fromEulerZXY(eulerBVH); // BVH Euler: CHANNELS 3 Zrotation Xrotation Yrotation
                                                 // Change the coordinate system from the standard right hand system (Opengl) to that of Blender or of Unity
        rot2 = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
        // if (blender)
        // {
        //     // this.keyframes[3][i].value = rot.x;
        //     // this.keyframes[4][i].value = -rot.z;
        //     // this.keyframes[5][i].value = rot.y;
        //     // this.keyframes[6][i].value = rot.w;
        //     rot2 = new Quaternion(rot.x, -rot.z, rot.y, rot.w);
        // }
        // else
        // {
        //     // this.keyframes[3][i].value = rot.x;
        //     // this.keyframes[4][i].value = -rot.y;
        //     // this.keyframes[5][i].value = -rot.z;
        //     // this.keyframes[6][i].value = rot.w;
        //     rot2 = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
        // }

        // Change the rotation of bvhNodeTransform

        avatarNodeTransform.localRotation = rot2; // bvhNodeTransform is a reference while its localRotation is a struct, a value type, bvhNodeTransform.localPosition is preserved

        //string jointPath = parentPath.Length == 0 ? bvhNode.name : parentPath + "/" + bvhNode.name;
        //jointPaths.Add(jointPath);

        avatarTransforms.Add(avatarNodeTransform); // bvhTransforms is the contrainer of transforms in the skeleton path

        foreach (BVHParser.BVHBone bvhChild in bvhNode.children)
        {
            Transform avatarChildTransform = avatarNodeTransform.Find(bvhChild.name);

           
            GetbvhTransformsForCurrentFrameRecursive(i, bvhChild, avatarChildTransform, avatarTransforms);

        }


    } //  GetbvhTransformsForCurrentFrameRecursive( int i, BVHParser.BVHBone bvhNode, Transform bvhNodeTransform, List<Transform> transforms) 

    private Animator getbvhAnimator()
    {

        if (this.bvhAnimator == null)
        {
            throw new InvalidOperationException("No Bvh Animator  set.");
           //this.gameObject.AddComponent
        }

        else
        {
          return this.bvhAnimator;
        }

    }

    private Animator  getSaraAnimator()
    {
       
        if (this.saraAnimator == null)
        {
            throw new InvalidOperationException("No Sara Animator set.");
        }

        else
        {
          return this.saraAnimator;
        }

    }


    // private Dictionary<string, Transform> UnityBoneToTransformMap; // null initially
    // private Dictionary<string, string> BvhToUnityRenamingMap;

    void SetClipAtRunTime(Animator animator, string currentClipName, AnimationClip animClip)
    {
        //Animator anim = GetComponent<Animator>(); 

        animatorOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);

        //   public AnimationClip[] animationClips = animator.animatorClips;
        clipOverrides = new AnimationClipOverrides(animatorOverrideController.overridesCount);
        // original clip vs override clip
        animatorOverrideController.GetOverrides(clipOverrides); // get 

        //var anims = new List<KeyValuePair<AnimationClip, AnimationClip>>();


        clipOverrides[currentClipName] = animClip;

        AnimationClip animClipToOverride = clipOverrides[currentClipName];
        Debug.Log(animClipToOverride);

        animatorOverrideController.ApplyOverrides(clipOverrides);

        animator.runtimeAnimatorController = animatorOverrideController;

        // set the bvh's animatorOverrideController to that of Sara

        //Animator saraAnimator =  this.getSaraAnimator();
        this.saraAnimator.runtimeAnimatorController = animatorOverrideController;

        // Transite to the new state with the new bvh motion clip
        //this.bvhAnimator.Play("ToBvh");
    } // void SetClipAtRunTime

    void ChangeClipAtRunTime(Animator animator, string currentClipName, AnimationClip clip)
    {
        //Animator anim = GetComponent<Animator>(); 

        AnimatorOverrideController overrideController = new AnimatorOverrideController();

        // overriderController has the following indexer:
        // public AnimationClip this[string name] { get; set; }
        // public AnimationClip this[AnimationClip clip] { get; set; }

        AnimatorStateInfo[] layerInfo = new AnimatorStateInfo[animator.layerCount];
        for (int i = 0; i < animator.layerCount; i++)
        {
            layerInfo[i] = animator.GetCurrentAnimatorStateInfo(i);
        }

        overrideController.runtimeAnimatorController = animator.runtimeAnimatorController;

        overrideController[currentClipName] = clip;

        animator.runtimeAnimatorController = overrideController;

        // Force an update: Disable Animator component and then update it via API.?
        // Animator.Update() 와 Monobehaviour.Update() 간의 관계: https://m.blog.naver.com/PostView.naver?isHttpsRedirect=true&blogId=1mi2&logNo=220928872232
        // https://gamedev.stackexchange.com/questions/197869/what-is-animator-updatefloat-deltatime-doing
        // => Animator.Update() is a function that you can call to step the animator forward by the given interval.
        animator.Update(0.0f); // Update(Time.deltaTime): Animation control: https://chowdera.com/2021/08/20210823014846793k.html
                               //=>  //  Record each frame
                               //        animator.Update( 1.0f / frameRate);
                               //=> You can pass the elapsed time by which it updates, and passing zero works as expected - **it updates to the first frame of the first animation state**
                               // The game logic vs animation logic: https://docs.unity3d.com/Manual/ExecutionOrder.html
                               // https://forum.unity.com/threads/forcing-animator-update.381881/#post-3045779
                               // Animation time scale: https://www.youtube.com/watch?v=4huKeRgEr4k
                               // Push back state
        for (int i = 0; i < animator.layerCount; i++)
        {
            animator.Play(layerInfo[i].fullPathHash, i, layerInfo[i].normalizedTime);
        }
        //currentClipName = clip.name;
    } // ChangeClipAtRunTime

    // public void parse(string bvhData)
    // {
    //     if (this.respectBVHTime)
    //     {
    //         this.bp = new BVHParser(bvhData);
    //         this.frameRate = 1f / this.bp.frameTime;
    //     }
    //     else
    //     {
    //         this.bp = new BVHParser(bvhData, 1f / this.frameRate); // this.bp.channels_bvhBones[].values will store the motion data
    //     }
    // }

    // // This function doesn't call any Unity API functions and should be safe to call from another thread
    // public void parseFile()
    // {
    //     this.parse(File.ReadAllText(filename));
    // }


    // public void loadAnimation()
    // {
    //   this.Start();
    // }





   
    // Indexes to muscles
    // 9: Neck Nod Down-Up min: -40 max: 40
    // 10: Neck Tilt Left-Right min: -40 max: 40
    // 11: Neck Turn Left-Right min: -40 max: 40
    // 12: Head Nod Down-Up min: -40 max: 40
    // 13: Head Tilt Left-Right min: -40 max: 40
    // 14: Head Turn Left-Right min: -40 max: 40


    // Muscle name and index lookup (See in Debug Log)
    void LookUpHumanMuscleIndex()
    {
        string[] muscleName = HumanTrait.MuscleName;
        int i = 0;
        while (i < HumanTrait.MuscleCount)
        {
            Debug.Log(i + ": " + muscleName[i] +
                " min: " + HumanTrait.GetMuscleDefaultMin(i) + " max: " + HumanTrait.GetMuscleDefaultMax(i));
            i++;
        }
    }

    




    

    void Start()
    {
       // Parse the avatar skeleton path and set the transfrom "container" for each joint in the path 
       // this.parseFile();   
       //MJ: "this" refers to an object of class BVHAnimationRetargetter 
       
       // this.bvhFrameGetter =  this.gameObject.GetComponent<BVHFrameGetter>(); 
        // this.gameObject = bvhRetargetter; It has two components: BVHAnimationRetargetter and bvhFrameGetter

        //this.skeletonGO = this.bvhFrameGetter.skeletonGO;
        //this.bvhAvatarRootTransform = this.bvhFrameGetter.avatarRootTransform;
        //this.bvhAvatarCurrentTransforms = this.bvhFrameGetter.avatarCurrentTransforms;
        
       

        //this.saraAvatarRootTransform = GameObject.FindGameObjectWithTag("Sara").transform.Find("Genesis3Female").Find("hip");


        //float deltaTime = 1f / this.frameRate;
        //this.deltaTime = (float) this.bvhFrameGetter.secondsPerFrame; 
        //this.frames =  this.bvhFrameGetter.frameCount;

        //Debug.LogFormat(" this.bvhFrameGetter.frameNo = {0}", this.bvhFrameGetter.frameNo);
        //Debug.LogFormat($"this.bvhFrameGetter.frameNo = {this.bvhFrameGetter.frameNo}");


        // public static Avatar BuildHumanAvatar(GameObject go, HumanDescription humanDescription);
        //Avatar Returns the Avatar, you must always always check the avatar is valid before using it with Avatar.isValid.

        // HumanDescription
        // struct in UnityEngine/Implemented in:UnityEngine.AnimationModuleLeave feedback
        // Description
        // Class that holds humanoid avatar parameters to pass to the AvatarBuilder.BuildHumanAvatar function.

        // Properties
        // armStretch	Amount by which the arm's length is allowed to stretch when using IK.
        // feetSpacing	Modification to the minimum distance between the feet of a humanoid model.
        // hasTranslationDoF	True for any human that has a translation Degree of Freedom (DoF). It is set to false by default.
        // human	Mapping between Mecanim bone names and bone names in the rig.
        // legStretch	Amount by which the leg's length is allowed to stretch when using IK.
        // lowerArmTwist	Defines how the lower arm's roll/twisting is distributed between the elbow and wrist joints.
        // lowerLegTwist	Defines how the lower leg's roll/twisting is distributed between the knee and ankle.
        // skeleton	List of bone Transforms to include in the model.
        // upperArmTwist	Defines how the upper arm's roll/twisting is distributed between the shoulder and elbow joints.
        // upperLegTwist	Defines how the upper leg's roll/twisting is distributed between the thigh and knee joints.




        // The legacy Animation Clip "speechGesture" cannot be used in the State "bvhPlay". Legacy AnimationClips are not allowed in Animator Controllers.
        // To use this animation in this Animator Controller, you must reimport in as a Generic or Humanoid animation clip
        if (animType == AnimType.Humanoid)
        {

           
            // Animator components of Skeleton and Sara should be added in the inspector by the user

            this.bvhAnimator = this.getbvhAnimator(); // Get Animator component of the virtual human to which this BVHAnimationLoader component is added
                                                      // =>   this.bvhAnimator = this.GetComponent<Animator>();
            //https://docs.unity3d.com/ScriptReference/AvatarBuilder.BuildHumanAvatar.html
            //https://forum.unity.com/threads/how-to-create-an-avatar-mask-for-custom-gameobject-hierarchy-from-scene.574270/


            // GOOD: https://answers.unity.com/questions/622031/how-to-use-avatarbuilderbuildhumanavatar.html

           
           // this.bvhAnimator.avatar = AvatarBuilder.BuildHumanAvatar(this.skeletonGO, HumanDescription humanDescription);
            if ( !this.bvhAnimator.avatar.isValid) {

                 throw new InvalidOperationException("this.bvhAnimator.avatar.isValid not true.");
                
            }
            

            this.saraAnimator = this.getSaraAnimator(); // Set up Animator for Sara for retargetting motion from Skeleton to Sara

              if ( !this.saraAnimator.avatar.isValid) {

                 throw new InvalidOperationException("this.saraAnimator.avatar.isValid not true.");
                
            }
            // Make both animator components use the same animator controller
            //this.saraAnimator.runtimeAnimatorController = this.bvhAnimator.runtimeAnimatorController; // Make both animators use the same animator controller

            // in the case of Humanoid animation type, you should use Animator component to control the animation clip
            //ParseAvatarRootTransform(this.bvhAvatarRootTransform, this.jointPaths, this.avatarTransforms); // this.jointPaths.Count = 57; 0 ~ 56

            // in the case of Generic animation type, you should use Animator component to control the animation clip
            //  ParseAvatarRootTransform(this.bvhAvatarRootTransform, this.jointPaths, this.avatarTransforms);
            //  ParseAvatarRootTransform(this.bvhAvatarRootTransform, this.jointPaths, this.bvhAvatarCurrentTransforms);


            //this.srcHumanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAvatarRootTransform);
            //this.destHumanPoseHandler = new HumanPoseHandler(this.saraAnimator.avatar, this.saraAvatarRootTransform);

            this.srcHumanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAnimator.gameObject.transform); 
            //MJ: this.bvhAnimator.gameObject=SkeletonGO
            //  this.skeletonGO.transform.GetChild(0) refers to this.avatarRootTransform, which is set every frame by BVHFrameGetters.cs
            
            this.destHumanPoseHandler = new HumanPoseHandler(this.saraAnimator.avatar, this.saraAnimator.gameObject.transform); //MJ: this.saraAnimator.gameObject = Sara
            // this.bvhAnimator is the Animator component attached to "Skeleton" gameObject, which a hierarchy of gameObjects.

            // => this.humanPoseHandler has a reference to this.bvhAnimator.avatar and its root transform (and thereby the entire hierarchy of transforms);
            // You can change the transforms of the human avatar hierarchy (In our code, by BVHFrameGetter script), and this humanPoseHandler will
            // refer to it, by  this.srchumanPoseHandler.GetHumanPose()?



            //MJ:  // Muscle name and index lookup for Debugging
            //this.LookUpHumanMuscleIndex();
           
           

            //// HumanBodyBones
            //// Setup a list of actually used human muscles
            //foreach (HumanBodyBones unityBoneId in bvhSkeleton.GetHumanBodyBones) // unityBoneType may be 55, Lastbone, which is not a bone.
            //                                                                      //for (int i=0; i < humanBones.Length; i++) // < humanBones.Length = 55, 3 of which (Left Eye, Right Eye, Jaw) do not correspond to bvh bones +>
            //                                                                      // The effective Human bones are 52. All of these human bones correspond to 52 bvh skeleton bones; 
            //                                                                      //                                             4 of which do not correspond to human bones => 52 = 56 -4
            //{
            //    for (int dofIndex = 0; dofIndex < 3; dofIndex++) // 52 x 3 = (50 + 2) x 3 = 150 + 6 = 156
            //    {

            //        //   Obtain the muscle index for a particular bone index and "degree of freedom".
            //        // Parameters:   
            //        //  dofIndex:   Number representing a "degree of freedom": 0 for X-Axis, 1 for Y-Axis, 2 for    Z-Axis.
            //        // https://forum.unity.com/threads/problem-with-muscle-settings-in-humanoid-configuration.707714/
            //        int eachMuscle = HumanTrait.MuscleFromBone((int)unityBoneId, dofIndex); // Hips.0,.1,.2 = -1; spine.0 => 2

            //        if (eachMuscle != -1) // if the muscle is valid
            //                              //this.muscleCurves is a  Dictionary<int, AnimationCurve>();
            //            this.muscleIndecies.Add(eachMuscle); // Generic Rig/Animation does not have use muscles: muscleIndecies has 89 entries
            //    }
            //}

            // HumanPose.muscles's size must be equal to HumanTrait.MuscleCount
            // this.humanPose.muscles = new float[ this.muscleIndecies.Count ];    // or (2)
            // this.srcHumanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAvatarRootTransform);
            //this.srcHumanPoseHandler.GetHumanPose(ref this.humanPose);
            //for (int i =0; i < this.humanPose.muscles.Length; i++ )
            //{
            //    Debug.LogFormat(" this.humanPose.muscles (initial):{0} = {1} ", i, this.humanPose.muscles[i]);  // a total of 95 muscles
            //}

            //for (int i = 0; i < this.muscleIndecies.Count; i++) {
            //    Debug.LogFormat("  this.muscleIndecies:{0} ={1} ", i, this.muscleIndecies[i]);     // a total of 89 Human bones are used
            //}




            // // An argument that is passed to a ref or in parameter must be initialized before it is passed.             
            // ref: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/ref
            // This requirement is met by  HumanPose humanPose = new HumanPose();
           // this.srcHumanPoseHandler.GetHumanPose(ref this.humanPose);


            //// reset all muscles to 0:    // Set character in fetus position
            //for (int i = 0; i < this.humanPose.muscles.Length; i++)
            //{
            //    //Debug.Log (humanPose.muscles [i]);
            //    // You can change the value of one of the properties of
            //    // itemRef. The change happens to item in Main as well
            //    this.humanPose.muscles[i] = 0;
            //}


            // Avatar avatar: https://docs.unity3d.com/kr/2022.1/ScriptReference/Avatar.html

            //https://docs.unity3d.com/ScriptReference/HumanPose.html:  Retargetable humanoid pose:
            // Represents a humanoid pose that is completely abstracted from any skeleton rig:
            // Properties:
            //         bodyPosition	The human body position for that pose.
            //         bodyRotation	The human body orientation for that pose.
            //         muscles

            //  the bodyPosition and bodyRotation are the position and rotation of the approximate center of mass of the humanoid.
            //   This is relative to the humanoid root transform and it is normalized: the local position is divided by avatar human scale.

            // SetHumanPose(ref HumanPose humanPose):
            // If the HumanPoseHander was constructed from an avatar and a root, 
            // the human pose is applied to the transform hierarchy representing the humanoid in the scene.
            //  If the HumanPoseHander was constructed from an avatar and jointPaths, the human pose is not bound to a transform hierarchy.
            // Create humanPoseHandler using the root transform of the avatar

            // Other ways of Create humanPoseHandler using jointPaths: 
            // Other ways of setting humanPoseHandler:
            //this.humanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.jointPaths.ToArray());    
            //this.jointPaths:  A list that defines the avatar joint paths. 
            // Each joint path starts from the node after the root transform and continues down the avatar skeleton hierarchy.
            // The root transform joint path is an empty string.

            // https://docs.unity3d.com/ScriptReference/HumanPoseHandler-ctor.html:
            // Create the set of joint paths, this.jointPaths, and the corresponding transforms, this.avatarTransforms,  from this.bvhAvatarRoot

            // this.avatarPose = new NativeArray<float>(this.jointPaths.Count * 7, Allocator.Persistent);


            //string[] muscleNames = HumanTrait.MuscleName; // 95 muscles
            //                                              // https://blog.unity.com/technology/mecanim-humanoids:
            //                                              // Humanoid Rigs don’t support twist bones, but Mecanim solver let you specify a percentage of twist to be taken out of the parent
            //                                              //  and put onto the child of the limb.It is defaulted at 50% and greatly helps to prevent skin deformation problem.
            //string[] boneNames = HumanTrait.BoneName; // 55 human bones
            //int boneCount = HumanTrait.BoneCount;



            //     public struct HumanDescription
            // {
            //     Mapping between Mecanim bone names and bone names in the rig.
            //     [NativeNameAttribute("m_Human")]
            //     public HumanBone[] human;
            //     //
            //     List of bone Transforms to include in the model.
            //     [NativeNameAttribute("m_Skeleton")]
            //     public SkeletonBone[] skeleton;

            //HumanBone[] humanBones = avatar.humanDescription.human; // 0 ~ 51 in Avatar Mapping: 52, while the number of Unity bones is 55 (0 ~ 54)'





        } //  if (animType == AnimType.Humanoid) 

        //else if (animType == AnimType.Generic)
        //{
        //    this.bvhAnimator = this.getbvhAnimator(); // Get Animator component of the virtual human to which this BVHAnimationLoader component is added
        //                                              // =>   this.bvhAnimator = this.GetComponent<Animator>();
            
        //    this.bvhAnimator.enabled =  true; // false; Generic Animation Type uses Animator component

        //    // The legacy Animation Clip "speechGesture" cannot be used in the State "bvhPlay". Legacy AnimationClips are not allowed in Animator Controllers.
        //    // To use this animation in this Animator Controller, you must reimport in as a Generic or Humanoid animation clip

        //    // in the case of Generic animation type, you should use Animator component to control the animation clip
        //    ParseAvatarRootTransform(this.bvhAvatarRootTransform, this.jointPaths, null);

        //    //  public GenericRecorder(Transform rootTransform, List<string> jointPaths, Transform[] recordableTransforms )

        //    this.genericRecorder = new GenericRecorder(this.jointPaths); // create containers for the animation curve of each joint


            
        //    // this.animation = this.gameObject.GetComponent<Animation>();
        //    // if (this.animation == null)
        //    // {
        //    //     this.animation = this.gameObject.AddComponent<Animation>();

        //    //    // throw new InvalidOperationException("Animation component should be attached to Skeleton gameObject");

        //    // }

        //}


        //else if (animType == AnimType.Legacy)
        //{
        //    // Disalbe Animator component

        //    //this.bvhAnimator.enabled = false; When Skeleton prefab is imported as Legacy anim type, Animation component, not Animator component, is automatically added

        //    // use   this.skeletonGO = this.bvhTest.skeletonGO;
        //    //GameObject skeleton = GameObject.FindGameObjectWithTag("Skeleton");
        //    this.bvhAnimation =  this.skeletonGO.GetComponent<Animation>();
        //    if (this.GetComponent<Animation>() == null)
        //    {
        //        //this.animation = this.gameObject.AddComponent<Animation>();

        //        throw new InvalidOperationException("Animation component should be ADDED to Skeleton gameObject");

        //    }

        //    // in the case of legacy animation type, you should use Animation component rather than Animator component to control the animation clip
        //    ParseAvatarRootTransform(this.bvhAvatarRootTransform, this.jointPaths, null);

        //    //  public GenericRecorder(Transform rootTransform, List<string> jointPaths, Transform[] recordableTransforms )

        //    this.genericRecorder = new GenericRecorder(this.jointPaths); //  // create containers for the animation curve of each joint


        //} //  else if (animType == AnimType.Legacy)


        else
        {
            throw new InvalidOperationException("Invalid Anim Type");
        }

    } // Start()

    // the muscle solution is still valid when an AnimatorController is attached to current humanoid character.
    // You just need to switch Update Mode to Animate Physics and put your muscle code in Update().
    //That will make your muscle code(which executes in Update()) executes after the AnimationController effects(which executes in FixedUpdate(),
    // which is called earlier than the Update()).

    //This approach will allow you directly control some muscle, and use Unity IK effect at the same time: 

    void Update()
    {

        //this.bvhFrameGetter.GetCurrentFrame(this.bvhAvatarCurrentTransforms); // Get the current frame from the bvh motion framedata

        // this.bvhTest =  this.gameObject.GetComponent<BVHTest>();
        // this.skeletonGO = this.bvhTest.skeletonGO;

        // //float deltaTime = 1f / this.frameRate;
        //float deltaTime = (float)this.bvhFrameGetter.secondsPerFrame;
        // this.frames =  this.bvhTest.frameCount;
        //  public List<Transform> avatarTransforms = new List<Transform>(); // The transforms for Skeleton gameObject updated inbvhFrameGetter.cs

        // int currFrameNo =  this.bvhTest.frameNo;


        //this.humanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAvatarRootTransform);

       // Debug.LogFormat(" this.bvhFrameGetter.frameNo = {0}", this.bvhFrameGetter.frameNo);
       // Debug.LogFormat($"this.bvhFrameGetter.frameNo = {this.bvhFrameGetter.frameNo}" );

        if (animType == AnimType.Humanoid)
        {

            //this.srcHumanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAvatarRootTransform);
            //this.destHumanPoseHandler = new HumanPoseHandler(this.saraAnimator.avatar, this.saraAvatarRootTransform);

            //MJ: Compute a human pose from the bvh skeleton (bound to srcHumanPoseHandler), 
            // store the pose in the human pose handler, and return the human pose as the value ref humanPose.
            // The bvh skeleton pose is updated every frame by by BVHFrameGetter script.
            HumanPose humanPose = new HumanPose();
            this.srcHumanPoseHandler.GetHumanPose(ref humanPose);

            //MJ: note that:

            
            // this.srcHumanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAnimator.gameObject.transform); //MJ: this.bvhAnimator.gameObject=Skeleton gameObj
            // this.destHumanPoseHandler = new HumanPoseHandler(this.saraAnimator.avatar, this.saraAnimator.gameObject.transform); //MJ: this.saraAnimator.gameObject = Sara


            //Computes a human pose from the avatar skeleton (bound to humanPoseHandler), stores the pose in the human pose handler, and returns the human pose.
            //this.destHumanPoseHandler.GetHumanPose(ref humanPose);
       
            //humanPose.bodyPosition = new Vector3(0, 0, 0);

            //humanPose.bodyRotation = Quaternion.identity;

            ////humanPose.muscles[0] = 0f;

            ////humanPose.muscles[1] = 0f;

            ////humanPose.muscles[2] = 0f;

            ////humanPose.muscles[3] = 0f;

            ////humanPose.muscles[4] = 0f;

            ////humanPose.muscles[5] = 0f;

            ////humanPose.muscles[6] = 0f;

            ////humanPose.muscles[7] = 0f;

            ////humanPose.muscles[8] = 0f;

            ////humanPose.muscles[9] = 0f;

            ////humanPose.muscles[10] = 0f;

            ////humanPose.muscles[11] = 0f;

            ////humanPose.muscles[12] = 0f;

            ////humanPose.muscles[13] = 0f;

            ////humanPose.muscles[14] = 0f;

            ////humanPose.muscles[16] = 0f;

            ////humanPose.muscles[18] = 0f;

            ////humanPose.muscles[19] = 1.028553f;

            ////humanPose.muscles[21] = 0.5912678f;

            ////humanPose.muscles[22] = 0.02391984f;

            ////humanPose.muscles[23] = -0.3350839f;

            ////humanPose.muscles[24] = 1.001714f;

            ////humanPose.muscles[25] = 0.2296576f;

            ////humanPose.muscles[26] = -0.007519196f;

            ////humanPose.muscles[27] = 0.03165575f;

            ////humanPose.muscles[28] = 0f;

            ////humanPose.muscles[29] = 0.5912538f;

            ////humanPose.muscles[30] = 0.02393157f;

            ////humanPose.muscles[31] = -0.3353789f;

            ////humanPose.muscles[32] = 1.0017f;

            ////humanPose.muscles[33] = 0.2298868f;

            ////humanPose.muscles[34] = -0.007534596f;

            ////humanPose.muscles[35] = 0.03168624f;

            ////humanPose.muscles[36] = 0f;

            ////humanPose.muscles[39] = 0.3875203f;

            ////humanPose.muscles[40] = 0.3130022f;

            ////humanPose.muscles[41] = 0.004141518f;

            ////humanPose.muscles[42] = 1.005535f;

            ////humanPose.muscles[43] = 0.0606268f;

            ////humanPose.muscles[44] = -0.0003488492f;

            ////humanPose.muscles[48] = 0.3875171f;

            ////humanPose.muscles[49] = 0.3130058f;

            ////humanPose.muscles[50] = 0.004390163f;

            ////humanPose.muscles[51] = 1.005516f;

            ////humanPose.muscles[52] = 0.06033607f;

            ////humanPose.muscles[53] = -0.0003474732f;

            ////humanPose.muscles[55] = -0.7710331f;

            ////humanPose.muscles[56] = 0.3264397f;

            ////humanPose.muscles[57] = 0.6025394f;

            ////humanPose.muscles[58] = 0.6025394f;

            ////humanPose.muscles[59] = 0.6651618f;

            ////humanPose.muscles[60] = -0.3629383f;

            ////humanPose.muscles[61] = 0.8053817f;

            ////humanPose.muscles[62] = 0.8053818f;

            ////humanPose.muscles[63] = 0.6668495f;

            ////humanPose.muscles[64] = -0.4736939f;

            ////humanPose.muscles[65] = 0.8019281f;

            ////humanPose.muscles[66] = 0.8019281f;

            ////humanPose.muscles[67] = 0.6668813f;

            ////humanPose.muscles[68] = -0.650219f;

            ////humanPose.muscles[69] = 0.8097187f;

            ////humanPose.muscles[70] = 0.8097187f;

            ////humanPose.muscles[71] = 0.6675717f;

            ////humanPose.muscles[72] = -0.4611372f;

            ////humanPose.muscles[73] = 0.811213f;

            ////humanPose.muscles[74] = 0.8112127f;

            ////humanPose.muscles[75] = -0.7712734f;

            ////humanPose.muscles[76] = 0.3237967f;

            ////humanPose.muscles[77] = 0.603806f;

            ////humanPose.muscles[78] = 0.6038052f;

            ////humanPose.muscles[79] = 0.665509f;

            ////humanPose.muscles[80] = -0.3649435f;

            ////humanPose.muscles[81] = 0.8050572f;

            ////humanPose.muscles[82] = 0.8050573f;

            ////humanPose.muscles[83] = 0.6668472f;

            ////humanPose.muscles[84] = -0.4735937f;

            ////humanPose.muscles[85] = 0.8022231f;

            ////humanPose.muscles[86] = 0.8022232f;

            ////humanPose.muscles[87] = 0.6668215f;

            ////humanPose.muscles[88] = -0.6518124f;

            ////humanPose.muscles[89] = 0.8098171f;

            ////humanPose.muscles[90] = 0.8098171f;

            ////humanPose.muscles[91] = 0.6676161f;

            ////humanPose.muscles[92] = -0.4607274f;

            ////humanPose.muscles[93] = 0.811323f;

            ////humanPose.muscles[94] = 0.811323f;


            //for (int i =0; i < humanPose.muscles.Length; i++)
            //{
            //    humanPose.muscles[i] = 0f;
            //}
            

            //Set the humanPose from the bvh motion to the Sara (destination) HumanPose
            this.destHumanPoseHandler.SetHumanPose(ref humanPose);

           //MJ: note that this.saraAnimator Script which is attached to the Sara gameObject will play the current humanPose of the Sara avatar:

            
            // this.srcHumanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAnimator.gameObject.transform); //MJ: this.bvhAnimator.gameObject=Skeleton gameObj
            // this.destHumanPoseHandler = new HumanPoseHandler(this.saraAnimator.avatar, this.saraAnimator.gameObject.transform); //MJ: this.saraAnimator.gameObject = Sara


            // https://forum.unity.com/threads/humanpose-issue-continued.484128/
            // 

            // public void GetHumanPose(ref HumanPose humanPose);
            // humanPose:	The output human pose. In the human pose, the bodyPosition and bodyRotation are the position and rotation of 
            // the approximate center of mass of the humanoid in world space. bodyPosition is normalized: the position is divided by avatar human scale.
            // Description:
            // *Computes a human pose* from the **avatar skeleton**, stores the pose in the human pose handler, and returns the human pose.

            // Here the avatar skeleton refers to this.bvhAvatarCurrentTransforms.

            // If the human pose handler was constructed with jointPaths, it is **not bound to a skeleton transform hierarchy**.
            // In this case, GetHumanPose returns the internally stored human pose as the output.



            // //this.jointPaths = new List<String>();     
            // // this.avatarTransforms = new List<Transform>();

            // // this.GetbvhTransformsForCurrentFrame(this.bvhTest.frameNo, this.bp.bvhRootNode, this.bvhAvatarRootTransform, this.bvhTest.avatarTransforms);    // the count of avatarTransforms= 57                                                                                                                        // ParseAvatarTransformRecursive(child, "", jointPaths, transforms);

            //  this.bvhTest.avatarTransforms holds the current frame updated inbvhFrameGetter's fixedUpdate() 

            // Save the humanoid animation curves for each muscle as muscle AnimationClip from the current humanPose set by
            // this.humanPoseHandler.SetInternalAvatarPose(this.avatarPose);





            // // Get the saved muscle animation clip
            // this.clip = this.humanoidRecorder.GetClip();
            // this.clip.EnsureQuaternionContinuity();
            // this.clip.name = "speechGesture"; //' the string name of the AnimationClip
            //                                   // 
            //                                   // 
            // this.clip.wrapMode = WrapMode.Loop;
            // this.SetClipAtRunTime(this.bvhAnimator, this.clip.name, this.clip);

            //this.humanPose.bodyPosition = this.bvhAvatarCurrentTransforms[0].position;
            //this.humanPose.bodyRotation = this.bvhAvatarCurrentTransforms[0].rotation;

            //humanPose.bodyPosition = this.bvhAvatarCurrentTransforms[0].position;
            //humanPose.bodyRotation = this.bvhAvatarCurrentTransforms[0].rotation;

            // https://github.com/fengkan/RuntimeRetargeting
            // https://github.com/fengkan/RuntimeRetargeting/blob/master/RetargetingHPH.cs

            // https://forum.unity.com/threads/move-humanoid-muscles.379500/
            // https://github.com/NumesSanguis/Basic-Unity-Head-Rotation/blob/master/unity_head_rotation/Assets/Scripts/HeadRotatorMuscle.cs
            // https://stackoverflow.com/questions/48740511/unity-3d-move-humanoid-head-muscles-using-script/48758353#48758353
            //// borrowed from: https://www.csharpcodi.com/vs2/3468/mmd-for-unity/Editor/MMDLoader/Private/AvatarSettingScript.cs/
            ///
            //https://www.reddit.com/r/Unity3D/comments/bja3d3/create_simple_poses/

            //https://stackoverflow.com/questions/48740511/unity-3d-move-humanoid-head-muscles-using-script


            //  Actually the muscle solution is still valid when an AnimatorController is attached to current humanoid character.
            //You just need to switch Update Mode to Animate Physics and put your muscle code in Update().
            //That will make your muscle code(which executes in Update()) executes after the AnimationController effects(which executes in FixedUpdate(),
            //which is called earlier than the Update()).   This approach will allow you directly control some muscle, and use Unity IK effect at the same time

            // https://grovecodeblog.wordpress.com/2013/10/30/combining-physics-and-animation-in-unity/

            //            using System.Collections;
            //            using System.Collections.Generic;
            //            using UnityEngine;

            //public class RetargetingHPH : MonoBehaviour
            //    {
            //        public GameObject src;

            //        HumanPoseHandler m_srcPoseHandler;
            //        HumanPoseHandler m_destPoseHandler;

            //        void Start()
            //        {
            //            m_srcPoseHandler = new HumanPoseHandler(src.GetComponent<Animator>().avatar, src.transform);
            //            m_destPoseHandler = new HumanPoseHandler(GetComponent<Animator>().avatar, transform);
            //        }

            //        void LateUpdate()
            //        {
            //            HumanPose m_humanPose = new HumanPose();

            //            m_srcPoseHandler.GetHumanPose(ref m_humanPose);
            //            m_destPoseHandler.SetHumanPose(ref m_humanPose);
            //        }
            //    }




            //  this.humanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAvatarRootTransform); => this.humanPose corresponds to this.bvhAvatarCurrentTransforms.

            //this.destHumanPoseHandler.SetHumanPose(ref this.humanPose);  // Stores the specified human pose inside the human pose handler.
            // 
            // If the HumanPoseHander was constructed from an avatar and a root, the human pose is applied to the transform hierarchy 
            // representing the humanoid in the scene.   Note: struct HumanPose => HumanPose is a value type, thus ref humanPose
            //If the HumanPoseHander was constructed from an avatar and jointPaths, the human pose is not bound to a transform hierarchy.

            //this.destHumanPoseHandler.SetHumanPose(ref this.humanPose);  // Stores the specified human pose inside the human pose handler.

            //  this.destHumanPoseHandler = new HumanPoseHandler(this.saraAnimator.avatar, this.saraAvatarRootTransform);

            //this.destHumanPoseHandler.SetHumanPose(ref humanPose);  

            //this.humanoidRecorder.SaveSnapshotAsKeys(this.humanPoseHandler, deltaTime);


            //// ParseAvatarTransformRecursive(child, "", jointPaths, transforms);
            //for (int j = 0; j < this.bvhFrameGetter.avatarCurrentTransforms.Count; j++)
            //{
            //    Vector3 position = this.bvhFrameGetter.avatarCurrentTransforms[j].localPosition;
            //    // /// <summary>The bone's positions for each frame. If the bone does not have any positions (only rotations) this array is null. Note: Use myBvh.frameCount to determine the number of frames in an animation, localFramePositions.Length might not be reliable if for example myBvh.removeFrame() has been called.</summary>
            //    // Token: 0x0400000F RID: 15
            //    //public Vector3[] localFramePositions; // for each FRAME

            //    Quaternion rotation = this.bvhFrameGetter.avatarCurrentTransforms[j].localRotation;

            //    //     this.avatarPose[7 * j] = position.x;
            //    //     this.avatarPose[7 * j + 1] = position.y;
            //    //     this.avatarPose[7 * j + 2] = position.z;
            //    //     this.avatarPose[7 * j + 3] = rotation.x;
            //    //     this.avatarPose[7 * j + 4] = rotation.y;
            //    //     this.avatarPose[7 * j + 5] = rotation.z;
            //    //     this.avatarPose[7 * j + 6] = rotation.w;
            //    // }

            //    // this.humanPoseHandler.SetInternalAvatarPose(this.avatarPose);

            //    //    // (1) GetHumanPose:	Computes a human pose from the avatar skeleton (bound to humanPoseHandler), stores the pose in the human pose handler, and returns the human pose.

            //    //    // Converts an avatar pose to a human pose and stores it as the internal human pose inside the human pose handler:
            //    //       (2) this.humanPoseHandler.SetInternalAvatarPose(this.avatarPose);  // If the human pose handler was constructed with a skeleton root transform, this method does nothing.

            //    //     (3)  HumanPoseHandler.GetInternalHumanPose: Gets the internal human pose stored in the human pose handler


            //    //this.avatarPose.Dispose();
            //    //this.humanPoseHandler.Dispose();


            //    // Save the humanoid animation curves for each muscle as muscle AnimationClip from the current humanPose set by
            //    // this.humanPoseHandler.SetInternalAvatarPose(this.avatarPose);

            //    this.humanoidRecorder.SaveSnapshotAsKeys(  this.humanPoseHandler, deltaTime);




            //    // // Get the saved muscle animation clip
            //    // this.clip = this.humanoidRecorder.GetClip();
            //    // this.clip.EnsureQuaternionContinuity();
            //    // this.clip.name = "speechGesture"; //' the string name of the AnimationClip
            //    //                                   // 
            //    //                                   // 
            //    // this.clip.wrapMode = WrapMode.Loop;
            //    // this.SetClipAtRunTime(this.bvhAnimator, this.clip.name, this.clip);

            //    this.humanPose.bodyPosition = this.bvhAvatarCurrentTransforms[0].position;
            //    this.humanPose.bodyRotation = this.bvhAvatarCurrentTransforms[0].rotation;

            //    //  this.humanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAvatarRootTransform); => this.humanPose corresponds to this.bvhAvatarCurrentTransforms.

            //    this.humanPoseHandler.SetHumanPose(ref this.humanPose);  // Stores the specified human pose inside the human pose handler.
            //    // 
            //    // If the HumanPoseHander was constructed from an avatar and a root, the human pose is applied to the transform hierarchy 
            //    // representing the humanoid in the scene.   Note: struct HumanPose => HumanPose is a value type, thus ref humanPose
            //    //If the HumanPoseHander was constructed from an avatar and jointPaths, the human pose is not bound to a transform hierarchy.

            //}

        } //  if (animType == AnimType.Humanoid) 

        else if (animType == AnimType.Generic)
        {
            //this.bvhAnimator = this.getbvhAnimator(); // Get Animator component of the virtual human to which this BVHAnimationLoader component is added
            //                                          // =>   this.bvhAnimator = this.GetComponent<Animator>();

            //this.bvhAnimator.enabled = true; // false; Generic Animation Type uses Animator component

            // The legacy Animation Clip "speechGesture" cannot be used in the State "bvhPlay". Legacy AnimationClips are not allowed in Animator Controllers.
            // To use this animation in this Animator Controller, you must reimport in as a Generic or Humanoid animation clip

            // in the case of Generic animation type, you should use Animator component to control the animation clip
            //ParseAvatarRootTransform(this.bvhAvatarRootTransform, this.jointPaths, null);

            //  public GenericRecorder(Transform rootTransform, List<string> jointPaths, Transform[] recordableTransforms )

            // this.genericRecorder = new GenericRecorder(this.jointPaths);



            //this.jointPaths = new List<String>();     
            //this.avatarTransforms = new List<Transform>();

            //this.GetbvhTransformsForCurrentFrame(i, this.bp.bvhRootNode, this.bvhAvatarRootTransform, this.jointPaths, this.avatarTransforms);
            // this.GetbvhTransformsForCurrentFrame(i, this.bp.bvhRootNode, this.bvhAvatarRootTransform, this.avatarTransforms);

            //  this.bvhTest.avatarTransforms holds the current frame updated inbvhFrameGetter's fixedUpdate()    
            //this.genericRecorder.SaveSnapshotAsKeys(deltaTime, this.bvhFrameGetter.avatarCurrentTransforms);

            // } //    for (int i = 0; i < this.frames; i++)


            // this.clip = this.genericRecorder.GetClip();
            // this.clip.EnsureQuaternionContinuity();
            // this.clip.name = "speechGesture"; //' the string name of the AnimationClip
            // this.clip.wrapMode = WrapMode.Loop;
            // this.SetClipAtRunTime(this.bvhAnimator, this.clip.name, this.clip); // The generic animation cannot be controlled by AnimatorOverrideController


            // this.animation = this.gameObject.GetComponent<Animation>();
            // if (this.animation == null)
            // {
            //     this.animation = this.gameObject.AddComponent<Animation>();

            //    // throw new InvalidOperationException("Animation component should be attached to Skeleton gameObject");

            // }

        }


        else if (animType == AnimType.Legacy)
        {
            // Disalbe Animator component

            //this.bvhAnimator.enabled = false; When Skeleton prefab is imported as Legacy anim type, Animation component, not Animator component, is automatically added


            // GameObject skeleton = GameObject.FindGameObjectWithTag("Skeleton");
            // Animation animation =  skeleton.GetComponent<Animation>();
            // if (this.GetComponent<Animation>() == null)
            // {
            //     //this.animation = this.gameObject.AddComponent<Animation>();

            //     throw new InvalidOperationException("Animation component should be ADDED to Skeleton gameObject");

            // }

            // // in the case of legacy animation type, you should use Animation component rather than Animator component to control the animation clip
            // ParseAvatarRootTransform(this.bvhAvatarRootTransform, this.jointPaths, this.avatarTransforms);

            // //  public GenericRecorder(Transform rootTransform, List<string> jointPaths, Transform[] recordableTransforms )

            // this.genericRecorder = new GenericRecorder(this.jointPaths);



            // this.avatarTransforms = new List<Transform>();
            //this.GetbvhTransformsForCurrentFrame(i, this.bp.bvhRootNode, this.bvhAvatarRootTransform, this.jointPaths, this.avatarTransforms);

            // save the old transform of the root node of the avatar           
            // Quaternion oldRootRotation =  this.bvhAvatarRootTransform.rotation;
            // Vector3    oldRootPosition =   this.bvhAvatarRootTransform.position;
            // this.GetbvhTransformsForCurrentFrame(i, this.bp.bvhRootNode, this.bvhAvatarRootTransform, this.avatarTransforms);


            //  this.bvhTest.avatarTransforms holds the current frame updated inbvhFrameGetter's fixedUpdate() 
            //this.genericRecorder.SaveSnapshotAsKeys(deltaTime, this.bvhFrameGetter.avatarCurrentTransforms);

            // restore  the root node of the avatar    

            // this.bvhAvatarRootTransform.rotation =  oldRootRotation;
            // this.bvhAvatarRootTransform.position =  oldRootPosition ;



            //} //    for (int i = 0; i < this.frames; i++)


            // this.clip = this.genericRecorder.GetClip();
            // this.clip.EnsureQuaternionContinuity();
            // this.clip.name = "speechGesture"; //' the string name of the AnimationClip

            // this.clip.legacy = true; // MJ
            // // The AnimationClip 'speechGesture' used by the Animation component 'bvhLoader' must be marked as Legacy.

            // this.clip.wrapMode = WrapMode.Loop;




            // this.bvhAnimation.AddClip(this.clip, this.clip.name); // Adds a clip to the animation with name newName
            // this.bvhAnimation.clip = this.clip; // the default animation
            // this.bvhAnimation.playAutomatically = true;
            // this.bvhAnimation.Play(this.clip.name);

        } //  else if (animType == AnimType.Legacy)


        else
        {
            throw new InvalidOperationException("Invalid Anim Type");
        }




        //   this.bvhAnimator.SetTrigger("ToBvh");

        //   this.saraAnimator.SetTrigger("ToBvh");




    }

    // Note: OnDestroy will only be called on game objects that have previously been active.
    //
    // OnDestroy occurs when a Scene or game ends.
    // Stopping the Play mode when running from inside the Editor will end the application. 
    // As this end happens an OnDestroy will be executed. 
    //Also, if a Scene is closed and a new Scene is loaded the OnDestroy call will be made.
    // When built as a standalone application OnDestroy calls are made when Scenes end.
    // A Scene ending typically means a new Scene is loaded.



void onDestroty()
{

 //this.avatarPose.Dispose();

 this.srcHumanPoseHandler.Dispose();
 this.destHumanPoseHandler.Dispose();
 }

} // public class BVHAnimationLoader : MonoBehaviour

// public class ExampleClass : MonoBehaviour
// {
//     float leftFootPositionWeight;
//     float leftFootRotationWeight;
//     Transform leftFootObj;

//     private Animator animator;

//     void Start()
//     {
//         animator = GetComponent<Animator>();
//     }

//     void OnAnimatorIK(int layerIndex)
//     {
//         animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootPositionWeight);
//         animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFootRotationWeight);
//         animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFootObj.position);
//         animator.SetIKRotation(AvatarIKGoal.LeftFoot, leftFootObj.rotation);
//     }
// }

