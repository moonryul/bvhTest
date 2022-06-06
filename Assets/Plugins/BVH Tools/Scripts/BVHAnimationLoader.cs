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


public class BVHAnimationLoader : MonoBehaviour
{
    public enum  AnimType {Legacy, Generic, Humanoid};
    public AnimType animType = AnimType.Generic;
    public Avatar bvhAvatar;

    public Transform bvhAvatarRootTransform;
    HumanPose humanPose;

    
    List<string> jointPaths = new List<string>(); // emtpy one
    List<Transform> avatarTransforms = new List<Transform>();
    HumanPoseHandler humanPoseHandler; 
    HumanoidRecorder humanoidRecorder;
    NativeArray<float> avatarPose;

    GenericRecorder genericRecorder;


   float[][] values = new float[6][]; // the root transform data at keyframe for the current bvh file
   Keyframe[][] keyframes = new Keyframe[7][]; // the joint tansform data at key keyframes for the curernt bvh file

    // Field Initializer vs Setting within Constructors: https://stackoverflow.com/questions/298183/c-sharp-member-variable-initialization-best-practice?msclkid=7fa9d0edc04911ec89a1aa2251ca3533
       
    [Header("Loader settings")]
    [Tooltip("The Animator component for the character; The bone names should be identical to those in the BVH file; All bones should be initialized with zero rotations.")]
    public  Animator bvhAnimator; 
    public   Animator saraAnimator; 
    [Tooltip("This is the path to the BVH file that should be loaded. **Bone offsets** are currently being ignored by this loader.")]
    public string filename;

    [Tooltip("When this option is set, the BVH file will be assumed to have the Z axis as up and the Y axis as forward instead of the normal BVH conventions.")]
    public bool blender = false;
    [Tooltip("When this flag is set, the frame time in the BVH time will be used to determine the frame rate instead of using the one given below.")]
    public bool respectBVHTime = true;
    [Tooltip("If the flag above is disabled, the frame rate given in the BVH file will be overridden by this value.")]
    public float frameRate = 60.0f;
    [Tooltip("This is the name that will be set on the animation clip. Leaving this empty is also okay.")]
    public string clipName;
    
    [Tooltip("This field can be used to read out the the animation clip after being loaded. A new clip will always be created when loading.")]
    public AnimationClip clip;

    //MJ added the following to use Animation Controller for the animation of the character

    private AnimationClip[] m_Animations;

    public Animation animation = null;
    
     protected AnimatorOverrideController animatorOverrideController; 
    
    protected AnimationClipOverrides clipOverrides;
     
        // public AnimationClip this[string name] { get; set; }
        // public AnimationClip this[AnimationClip clip] { get; set; }
    static private int clipCount = 0;
    private BVHParser bp = null;
    //private Transform bvhRootTransform;
    private string prefix;
    private int frames;
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



 void GetbvhTransformsForCurrentFrame(int i, BVHParser.BVHBone rootBvhNode, Transform avatarRootTransform, List<string> jointPaths, List<Transform> bvhTransforms )
    {
       
        // first = true means bvhNode is the root node
        bool posX = false;
        bool posY = false;
        bool posZ = false;
        bool rotX = false;
        bool rotY = false;
        bool rotZ = false;

        // float[][] values = new float[6][];

        // Keyframe[][] keyframes = new Keyframe[7][];


        //Trans oldTrans = new Trans( rootTransform ); // save the rootTransform to oldTrans

        // This needs to be changed to gather from all channels into two vector3, invert the coordinate system transformation and then make keyframes from it
        // Construct the data structure for frame data for each channel for the current node or joint
        for (int channel = 0; channel < 6; channel++) // 6 channels
        {

            this.keyframes[channel] = new Keyframe[this.frames];

            // public Keyframe(float time, float value, float inTangent, float outTangent, float inWeight, float outWeight);


            this.values[channel] = rootBvhNode.channels_bvhBones[channel].values; // the animation key frames of the bvh root node (Hips)
            // Note that  this.keyframes[0][i].value = -this.values[0][i];

        } // for (int channel = 0; channel < 6; channel++)

        this.keyframes[6] = new Keyframe[this.frames]; //  "localRotation.w";


        float time = 0f;


        Vector3 offset; //  used for the root node


        if (this.blender) //  //  the BVH file will be assumed to have the Z axis as up and the Y axis as forward, X rightward as in Blender
        {
            offset = new Vector3(-rootBvhNode.offsetX, rootBvhNode.offsetZ, -rootBvhNode.offsetY); // => Unity frame
        }
        else //  //  the BVH file will be assumed to have the normal BVH convention: Y up; Z backward; X right (OpenGL: right handed)
        {
            offset = new Vector3(-rootBvhNode.offsetX, rootBvhNode.offsetY, rootBvhNode.offsetZ);  // To unity Frame
            // Unity:  Y: up, Z: forward, X = right or Y: up, Z =backward, X left (The above transform follows the second)
        }

        time += 1f / this.frameRate;
        this.keyframes[0][i].time = time;
        this.keyframes[1][i].time = time;
        this.keyframes[2][i].time = time;
        // Change the coordinate system from BVH to Unity
        if (blender)
        {
            this.keyframes[0][i].value = -this.values[0][i];
            this.keyframes[1][i].value = this.values[2][i];
            this.keyframes[2][i].value = -this.values[1][i];
        }
        else
        {
            this.keyframes[0][i].value = -this.values[0][i]; // From BVH to Unity: the sign of the x coordinate changes because of the frame change from BVH to Unity
            this.keyframes[1][i].value = this.values[1][i];
            this.keyframes[2][i].value = this.values[2][i];
        }

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
        Vector3 rootTranslation = new Vector3(keyframes[0][i].value, keyframes[1][i].value, keyframes[2][i].value);

        Vector3 globalRootPos = bvhAnimator.gameObject.transform.position + offset + rootTranslation;
        Vector3 bvhPositionLocal = avatarRootTransform.transform.parent.InverseTransformPoint(  globalRootPos  );

        bvhPositionLocal =  Vector3.Scale(bvhPositionLocal, this.bvhAnimator.gameObject.transform.localScale);

     
        //rootTransform.transform.localPosition = bvhPositionLocal;
       
        
        
        // this.keyframes[0][i].value = bvhPositionLocal.x * this.bvhAnimator.transform.localScale.x;
        // this.keyframes[1][i].value = bvhPositionLocal.y * this.bvhAnimator.transform.localScale.y;
        // this.keyframes[2][i].value = bvhPositionLocal.z * this.bvhAnimator.transform.localScale.z;

        this.keyframes[0][i].value = bvhPositionLocal.x; 
        this.keyframes[1][i].value = bvhPositionLocal.y;
        this.keyframes[2][i].value = bvhPositionLocal.z;

      
        time = 0f;
       
        Vector3 eulerBVH = new Vector3(wrapAngle(values[3][i]), wrapAngle(values[4][i]), wrapAngle(values[5][i]));
        Quaternion rot = fromEulerZXY(eulerBVH); // Get the quaternion for the BVH ZXY Euler angles

        Quaternion rot2;
        // Change the coordinate system from BVH to Unity
        if (blender)
        {
            keyframes[3][i].value = rot.x;
            keyframes[4][i].value = -rot.z;
            keyframes[5][i].value = rot.y;
            keyframes[6][i].value = rot.w;
            rot2 = new Quaternion(rot.x, -rot.z, rot.y, rot.w);
        }
        else
        {
            keyframes[3][i].value = rot.x;
            keyframes[4][i].value = -rot.y;
            keyframes[5][i].value = -rot.z;
            keyframes[6][i].value = rot.w;
            rot2 = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
        }

      
      // Store the new root transform to the current rootTransform, which is used as a temporary variable to store the transform at the current frame i;
      // Changing the rootTransform is OK because when the generated animation clip will be used to play the virtual character.
      // The original pose of the virtual character will be set to the first frame of the animation clip, when the clip is played.
       avatarRootTransform.localPosition =  bvhPositionLocal;
       avatarRootTransform.localRotation = rot2;


        // public Quaternion localRotation { get; set; }:  The rotation of the transform relative to the transform rotation of the parent.
        // position and rotation atrributes are the values relative to the world space.

        // => Change the transform of the root bvh node to the local transform relative the root coordinate system.
        // The original bvh data is considered to be measured relative to the world coordinate system.

        // keyframes[3][i].value = rootTransform.transform.localRotation.x;
        // keyframes[4][i].value = rootTransform.transform.localRotation.y;
        // keyframes[5][i].value = rootTransform.transform.localRotation.z;
        // keyframes[6][i].value = rootTransform.transform.localRotation.w;

        


        /*Vector3 euler = rot2.eulerAngles;

        keyframes[3][i].value = wrapAngle(euler.x);
        keyframes[4][i].value = wrapAngle(euler.y);
        keyframes[5][i].value = wrapAngle(euler.z);*/

        time += 1f / this.frameRate;
        keyframes[3][i].time = time;
        keyframes[4][i].time = time;
        keyframes[5][i].time = time;
        keyframes[6][i].time = time;
                 
        jointPaths.Add(""); // root tranform path is the empty string

        bvhTransforms.Add( avatarRootTransform); // bvhTransforms is the contrainer of transforms in the skeleton path
        // restore the original root bvh node transform
       // rootTransform.transform.rotation = oldRotation;
        // Get the frame data for each child of the root node, recursively.
        foreach (BVHParser.BVHBone child in rootBvhNode.children)
        {
            Transform childTransform = avatarRootTransform.Find(child.name);

            GetbvhTransformsForCurrentFrameRecursive(i, child, childTransform, "", jointPaths, bvhTransforms); // "" refers to the root node of the skeleton path
           // GetbvhTransformsForCurrentFrameRecursive(i, child, "", jointPaths, bvhTrans); // "" refers to the root node of the skeleton path
        }

    } // void GetbvhTransformsForCurrentFrame(i, BVHParser.BVHBone bvhNode, Transform rootTransform, List<string> jointPaths, List<Transform> bvhTransforms )

    private void GetbvhTransformsForCurrentFrameRecursive( int i, BVHParser.BVHBone bvhNode,  Transform bvhNodeTransform, string parentPath,
                                                                                   List<string> jointPaths, List<Transform> bvhTransforms)

    //private void GetbvhTransformsForCurrentFrameRecursive( int i, BVHParser.BVHBone bvhNode,   string parentPath,
//                                                                                  List<string> jointPaths, List<Trans> bvhTrans)
    {

        // first = true means bvhNode is the root node
        bool posX = false;
        bool posY = false;
        bool posZ = false;
        bool rotX = false;
        bool rotY = false;
        bool rotZ = false;

        // float[][] values = new float[6][];

        // Keyframe[][] keyframes = new Keyframe[7][];

       

        // This needs to be changed to gather from all channels into two vector3, invert the coordinate system transformation and then make keyframes from it
        // Construct the data structure for frame data for each channel for the current node or joint
        for (int channel = 3; channel < 6; channel++) // The rotational channels
        {



            this.keyframes[channel] = new Keyframe[frames];

            // public Keyframe(float time, float value, float inTangent, float outTangent, float inWeight, float outWeight);


            this.values[channel] = bvhNode.channels_bvhBones[channel].values; // the animation key frames (joint angles for frames)


        } // for (int channel = 0; channel < 6; channel++)


        this.keyframes[6] = new Keyframe[frames]; //  "localRotation.w";


        float time = 0f;


        time = 0f;
        Quaternion rot2;

        // the rotatation value of the joint center

        // Quaternion oldRotation = bvhNodeTransform.transform.rotation;


        Vector3 eulerBVH = new Vector3(wrapAngle(values[3][i]), wrapAngle(values[4][i]), wrapAngle(values[5][i]));
        Quaternion rot = fromEulerZXY(eulerBVH); // BVH Euler: CHANNELS 3 Zrotation Xrotation Yrotation
                                                 // Change the coordinate system from the standard right hand system (Opengl) to that of Blender or of Unity
        if (blender)
        {
            this.keyframes[3][i].value = rot.x;
            this.keyframes[4][i].value = -rot.z;
            this.keyframes[5][i].value = rot.y;
            this.keyframes[6][i].value = rot.w;
            rot2 = new Quaternion(rot.x, -rot.z, rot.y, rot.w);
        }
        else
        {
            this.keyframes[3][i].value = rot.x;
            this.keyframes[4][i].value = -rot.y;
            this.keyframes[5][i].value = -rot.z;
            this.keyframes[6][i].value = rot.w;
            rot2 = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
        }


        /*Vector3 euler = rot2.eulerAngles;

        keyframes[3][i].value = wrapAngle(euler.x);
        keyframes[4][i].value = wrapAngle(euler.y);
        keyframes[5][i].value = wrapAngle(euler.z);*/

        time += 1f / this.frameRate;
        this.keyframes[3][i].time = time;
        this.keyframes[4][i].time = time;
        this.keyframes[5][i].time = time;
        this.keyframes[6][i].time = time;


        // Change the rotation of bvhNodeTransform

        bvhNodeTransform.localRotation = rot2;

        string jointPath = parentPath.Length == 0 ? bvhNode.name : parentPath + "/" + bvhNode.name;
        jointPaths.Add(jointPath);

        bvhTransforms.Add(bvhNodeTransform); // bvhTransforms is the contrainer of transforms in the skeleton path

        foreach (BVHParser.BVHBone child in bvhNode.children)
        {
            Transform childTransform = bvhNodeTransform.Find(child.name);

            GetbvhTransformsForCurrentFrameRecursive(i, child, childTransform, jointPath, jointPaths, bvhTransforms);

        }



    } //  GetbvhTransformsForCurrentFrameRecursive( int i, BVHParser.BVHBone bvhNode,  string parentPath, 
      //                                         List<string> jointPaths, List<Transform> transforms) 

    private Animator getbvhAnimator()
    {

        if (this.bvhAnimator == null)
        {
            throw new InvalidOperationException("No Bvh Animator  set.");
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

    public void parse(string bvhData)
    {
        if (this.respectBVHTime)
        {
            this.bp = new BVHParser(bvhData);
            this.frameRate = 1f / this.bp.frameTime;
        }
        else
        {
            this.bp = new BVHParser(bvhData, 1f / this.frameRate); // this.bp.channels_bvhBones[].values will store the motion data
        }
    }

    // This function doesn't call any Unity API functions and should be safe to call from another thread
    public void parseFile()
    {
        this.parse(File.ReadAllText(this.filename));
    }


    public void loadAnimation()
    {
      this.Start();
    }





    public void playAnimation()
    {

        if (this.animType == AnimType.Humanoid)
        {
            this.bvhAnimator.Play("bvhPlay");  // MJ: Animator.Play(string stateName); play a state stateName; Base Layer.Bounce, e.g.
                                               // "Entry" => Bounce    

            this.bvhAnimator.Update(0.0f); // Update(Time.deltaTime): Animation control: https://chowdera.com/2021/08/20210823014846793k.html
                                           //=>  //  Record each frame
                                           //        animator.Update( 1.0f / frameRate);
                                           //=> You can pass the elapsed time by which it updates, and passing zero works as expected - **it updates to the first frame of the first animation state**
                                           // The game logic vs animation logic: https://docs.unity3d.com/Manual/ExecutionOrder.html  
        }

        else if (this.animType == AnimType.Legacy)
        {


            this.animation.Play(this.clip.name);
        }
    }

    public void stopAnimation()
    {
        if (this.animType == AnimType.Humanoid)
        {

            this.bvhAnimator.enabled = false;
        }
        else if (this.animType == AnimType.Legacy)
        {

            if (this.animation.IsPlaying(clip.name))
            {
                this.animation.Stop();
            }
        }
    }


    void Start()
    {

        this.parseFile();

        this.bvhAnimator = this.getbvhAnimator(); // Get Animator component of the virtual human to which this BVHAnimationLoader component is added
                                                  // =>   this.bvhAnimator = this.GetComponent<Animator>();

        this.saraAnimator = this.getSaraAnimator();
        this.saraAnimator.runtimeAnimatorController = this.bvhAnimator.runtimeAnimatorController;

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

        // Parse the avatar skeleton path and set the transfrom "container" for each joint in the path 

        float deltaTime = 1f / this.frameRate;
        this.frames = this.bp.frames;

        if (animType == AnimType.Humanoid)
        {
            // in the case of Humanoid animation type, you should use Animator component to control the animation clip
            ParseAvatarRootTransform(this.bvhAvatarRootTransform, this.jointPaths, this.avatarTransforms);

            // Setup for Humanoid Animation: Make ready to record the motion of the bvh file as a humanoid muscle clip
            this.humanoidRecorder = new HumanoidRecorder(this.bvhAnimator);

            // Create humanPoseHandler using the root transform of the avatar
            //this.humanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAnimator.transform);

            // Create humanPoseHandler using jointPaths: A list that defines the avatar joint paths. 
            // Each joint path starts from the node after the root transform and continues down the avatar skeleton hierarchy.
            // The root transform joint path is an empty string.

            // https://docs.unity3d.com/ScriptReference/HumanPoseHandler-ctor.html:
            // Create the set of joint paths, this.jointPaths, and the corresponding transforms, this.avatarTransforms,  from this.bvhAvatarRoot



            this.humanPoseHandler = new HumanPoseHandler(this.bvhAvatar, this.jointPaths.ToArray());

            this.avatarPose = new NativeArray<float>(this.jointPaths.Count * 7, Allocator.Persistent);

            for (int i = 0; i < this.frames; i++)
            {
                // GetbvhTransformsForCurrentFrame(int i, BVHParser.BVHBone rootBvhNode, Transform rootTransform, List<string> jointPaths, List<Transform> bvhTransforms )
                // this.GetbvhTransformsForCurrentFrame(i, this.bp.bvhRootNode, this.bvhAvatarRootTransform, this.jointPaths, this.avatarTransforms);      
                this.GetbvhTransformsForCurrentFrame(i, this.bp.bvhRootNode, this.bvhAvatarRootTransform, this.jointPaths, this.avatarTransforms);                                                                                                                            // ParseAvatarTransformRecursive(child, "", jointPaths, transforms);
                                                                                                                                                                                                                                                        // ParseAvatarTransformRecursive(child, "", jointPaths, transforms);
                for (int j = 0; j < this.jointPaths.Count; ++j)
                {
                    Vector3 position = this.avatarTransforms[j].localPosition;
                    Quaternion rotation = this.avatarTransforms[j].localRotation;

                    this.avatarPose[7 * j] = position.x;
                    this.avatarPose[7 * j + 1] = position.y;
                    this.avatarPose[7 * j + 2] = position.z;
                    this.avatarPose[7 * j + 3] = rotation.x;
                    this.avatarPose[7 * j + 4] = rotation.y;
                    this.avatarPose[7 * j + 5] = rotation.z;
                    this.avatarPose[7 * j + 6] = rotation.w;
                }

                this.humanPoseHandler.SetInternalAvatarPose(this.avatarPose);

                //    // (1) GetHumanPose:	Computes a human pose from the avatar skeleton bound to humanPoseHandler, stores the pose in the human pose handler, and returns the human pose.
                //    // Converts an avatar pose to a human pose and stores it as the internal human pose inside the human pose handler

                //     (2) this.humanPoseHandler.SetInternalAvatarPose(this.avatarPose);  // If the human pose handler was constructed with a skeleton root transform, this method does nothing.

                //     (3)  HumanPoseHandler.GetInternalHumanPose: Gets the internal human pose stored in the human pose handler


                //this.avatarPose.Dispose();
                //this.humanPoseHandler.Dispose();


                // Save the humanoid animation curves for each muscle as muscle AnimationClip
                this.humanoidRecorder.TakeSnapshot(deltaTime);

                this.SetClipAtRunTime(this.bvhAnimator, this.clip.name, this.clip);



            } //    for (int i = 0; i < this.frames; i++)

            this.clip = this.humanoidRecorder.GetClip();
            this.clip.EnsureQuaternionContinuity();
            this.clip.name = "speechGesture"; //' the string name of the AnimationClip
                                              // 
                                              // 
            this.clip.wrapMode = WrapMode.Loop;
            this.SetClipAtRunTime(this.bvhAnimator, this.clip.name, this.clip);

        } //  if (animType == AnimType.Humanoid) 

        else if (animType == AnimType.Generic)
        {
            // in the case of Generic animation type, you should use Animator component to control the animation clip
            ParseAvatarRootTransform(this.bvhAvatarRootTransform, this.jointPaths, this.avatarTransforms);

            //  public GenericRecorder(Transform rootTransform, List<string> jointPaths, Transform[] recordableTransforms )

            this.genericRecorder = new GenericRecorder(this.jointPaths, this.avatarTransforms);


            for (int i = 0; i < this.frames; i++)
            {
                this.GetbvhTransformsForCurrentFrame(i, this.bp.bvhRootNode, this.bvhAvatarRootTransform, this.jointPaths, this.avatarTransforms);

                this.genericRecorder.TakeSnapshot(deltaTime);



            } //    for (int i = 0; i < this.frames; i++)


            this.clip = this.genericRecorder.GetClip();
            this.clip.EnsureQuaternionContinuity();
            this.clip.name = "speechGesture"; //' the string name of the AnimationClip
            this.clip.wrapMode = WrapMode.Loop;
            this.SetClipAtRunTime(this.bvhAnimator, this.clip.name, this.clip);

        }


        else if (animType == AnimType.Legacy)
        {
            // Disalbe Animator component

            this.bvhAnimator.enabled = false;

            // in the case of legacy animation type, you should use Animation component rather than Animator component to control the animation clip
            ParseAvatarRootTransform(this.bvhAvatarRootTransform, this.jointPaths, this.avatarTransforms);

            //  public GenericRecorder(Transform rootTransform, List<string> jointPaths, Transform[] recordableTransforms )

            this.genericRecorder = new GenericRecorder(this.jointPaths, this.avatarTransforms);


            for (int i = 0; i < this.frames; i++)
            {
                this.GetbvhTransformsForCurrentFrame(i, this.bp.bvhRootNode, this.bvhAvatarRootTransform, this.jointPaths, this.avatarTransforms);

                this.genericRecorder.TakeSnapshot(deltaTime);



            } //    for (int i = 0; i < this.frames; i++)


            this.clip = this.genericRecorder.GetClip();
            this.clip.EnsureQuaternionContinuity();
            this.clip.name = "speechGesture"; //' the string name of the AnimationClip

            this.clip.legacy = true; // MJ
            this.clip.wrapMode = WrapMode.Loop;




            this.animation = this.gameObject.GetComponent<Animation>();
            if (this.animation == null)
            {
                this.animation = this.gameObject.AddComponent<Animation>();

               // throw new InvalidOperationException("Animation component should be attached to Skeleton gameObject");

            }

            this.animation.AddClip(this.clip, this.clip.name); // Adds a clip to the animation with name newName
            this.animation.clip = this.clip; // the default animation
            this.animation.playAutomatically = true;
            this.animation.Play(this.clip.name);

        } //  else if (animType == AnimType.Legacy)


        else
        {
            throw new InvalidOperationException("Invalid Anim Type");
        }

    } // Start()


    void Update()
    {        
      

        //   this.bvhAnimator.SetTrigger("ToBvh");

        //   this.saraAnimator.SetTrigger("ToBvh");

              


    }
} // public class BVHAnimationLoader : MonoBehaviour
