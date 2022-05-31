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


public class HumanoidRecorder
{
    float time = 0;
    // public Avatar avatar;

    HumanPoseHandler humanPoseHandler;
    HumanPose humanPose = new HumanPose();

    //    class AnimationCurve:
    //         public Keyframe this[int index] { get; }

    //         //
    //         // Summary:
    //         //     The number of keys in the curve. (Read Only)
    //         public int length { get; }
    //         //
    //         // Summary:
    //         //     All keys defined in the animation curve.
    //         public Keyframe[] keys { get; set; }

    Dictionary<int, AnimationCurve> muscleCurves = new Dictionary<int, AnimationCurve>();
    Dictionary<string, AnimationCurve> rootCurves = new Dictionary<string, AnimationCurve>();

    Vector3 rootOffset;

//} // public class HumanoidRecoder

    //     public struct HumanPose
    // {
    //     //
    //     // Summary:
    //     //     The human body position for that pose.
    //     public Vector3 bodyPosition;
    //     //
    //     // Summary:
    //     //     The human body orientation for that pose.
    //     public Quaternion bodyRotation;
    //     //
    //     // Summary:
    //     //     The array of muscle values for that pose.
    //     public float[] muscles;
    // }


    //   public class Avatar : Object
    //     {
    //         //
    //         // Summary:
    //         //     Return true if this avatar is a valid mecanim avatar. It can be a generic avatar
    //         //     or a human avatar.
    //         public bool isValid { get; }
    //         //
    //         // Summary:
    //         //     Return true if this avatar is a valid human avatar.
    //         public bool isHuman { get; }
    //         //
    //         // Summary:
    //         //     Returns the HumanDescription used to create this Avatar.
    //         public HumanDescription humanDescription { get; }
    //     }

    //  public struct HumanDescription
    //     {
    //         //
    //         // Summary:
    //         //     Mapping between Mecanim bone names and bone names in the rig.
    //         [NativeNameAttribute("m_Human")]
    //         public HumanBone[] human;
    //         //
    //         // Summary:
    //         //     List of bone Transforms to include in the model.
    //         [NativeNameAttribute("m_Skeleton")]
    //         public SkeletonBone[] skeleton;


    // // public HumanoidRecoder(Animator animator, HumanBodyBones[] humanBodyBones)
    public HumanoidRecorder(Animator animator) 
    {
        this.rootOffset = animator.transform.position;


        // public HumanPoseHandler(Avatar avatar, Transform root);
        // Creates a human pose handler from an avatar and the root transform 
        this.humanPoseHandler = new HumanPoseHandler(animator.avatar, animator.transform);

        HumanBone[] humanBones = animator.avatar.humanDescription.human;

        //int bonesLength = this.avatar.humanDescription.skeleton[i].name
        int bonesLength = humanBones.Length; // = 56
                                             //HumanBodyBones.LastBone == 55 

        // Create placeholders for the root keys and muscle keys

        for (int i = 0; i < bonesLength; i++)  // unityBoneType may be 55, Lastbone, which is not a bone.
        {
            for (int dofIndex = 0; dofIndex < 3; dofIndex++)
            {

                //   Obtain the muscle index for a particular bone index and "degree of freedom".
                // Parameters:   
                //  dofIndex:   Number representing a "degree of freedom": 0 for X-Axis, 1 for Y-Axis, 2 for    Z-Axis.

                int eachMuscle = HumanTrait.MuscleFromBone(i, dofIndex);

                if (eachMuscle != -1) // unityBoneType = 55 will not have muscle defined for it.
                                      //this.muscleCurves is a  Dictionary<int, AnimationCurve>();
                    this.muscleCurves.Add(eachMuscle, new AnimationCurve()); // Generic Rig/Animation does not have use muscles
            }
        }

        this.rootCurves.Add("RootT.x", new AnimationCurve());
        this.rootCurves.Add("RootT.y", new AnimationCurve());
        this.rootCurves.Add("RootT.z", new AnimationCurve());

    } // HumanoidRecoder(Animator animators)


    // public class Avatar : Object
    //     {
    //         //
    //         // Summary:
    //         //     Return true if this avatar is a valid mecanim avatar. It can be a generic avatar
    //         //     or a human avatar.
    //         public bool isValid { get; }
    //         //
    //         // Summary:
    //         //     Return true if this avatar is a valid human avatar.
    //         public bool isHuman { get; }
    //         //
    //         // Summary:
    //         //     Returns the HumanDescription used to create this Avatar.
    //         public HumanDescription humanDescription { get; }
    //     }

    //        void Update()
    //         {
    //             if (this.isRecording)
    //                 this.recordable.TakeSnapshot(Time.deltaTime);
    //         }

    public void TakeSnapshot(float deltaTime) // a method of Interface  IRecordable
    {
        time += deltaTime;

        this.humanPoseHandler.GetHumanPose(ref this.humanPose); // https://forum.unity.com/threads/humanpose-issue-continued.484128/

        //     void LateUpdate() {
        //     handler.GetHumanPose(ref humanPose);
        //     humanPose.bodyPosition = humanPose.bodyPosition.y * Vector3.up;
        //     humanPose.bodyRotation = Quaternion.identity;
        //     for (int i = 0; i < muscleIndices.Length; ++i)
        //         humanPose.muscles[muscleIndices[i]] = values[i];
        //     handler.SetHumanPose(ref humanPose);
        // }

        // https://unity928.rssing.com/chan-30531769/article714855.html
        // https://forum.unity.com/threads/how-can-i-animate-a-humanoid-avatar-using-only-a-csv-file-s-o-s.485117/


        // Summary:
        //     Retargetable humanoid pose.
        // public struct HumanPose  ==> this.humanPose.muscles
        // {
        //     //
        //     // Summary:
        //     //     The human body position for that pose.
        //     public Vector3 bodyPosition;
        //     //
        //     // Summary:
        //     //     The human body orientation for that pose.
        //     public Quaternion bodyRotation;
        //     //
        //     // Summary:
        //     //     The array of muscle values for that pose.
        //     public float[] muscles;
        // }


        // fill the key for the current time for  each muscle anim key data
        foreach (KeyValuePair<int, AnimationCurve> muscleCurve in this.muscleCurves)
        {   //  currentPose.muscles[i]
            Keyframe timedKey = new Keyframe(time, this.humanPose.muscles[muscleCurve.Key]); // public float[] muscles;

            muscleCurve.Value.AddKey(timedKey); //   public int AddKey(Keyframe key); => fill muscleCurve data with key
                                       // data.Value refer to AnimationCurve (list), the value part of the dict, to which each key is added
                                       //         //  // Summary:
                                       // //     The number of keys in the curve. (Read Only)
                                       //         public int length { get; }
                                       // //
                                       // // Summary:
                                       // //     All keys defined in the animation curve.
                                       //           public Keyframe[] keys { get; set; }
                                       // //
        }

        Vector3 rootPosition = this.humanPose.bodyPosition - rootOffset;

        this.AddRootKey("RootT.x", rootPosition.x);
        this.AddRootKey("RootT.y", rootPosition.y);
        this.AddRootKey("RootT.z", rootPosition.z);
    } //   public void TakeSnapshot(float deltaTime) // a method of Interface  IRecordable

    void AddRootKey(string property, float value)
    {
        Keyframe key = new Keyframe(time, value);
        this.rootCurves[property].AddKey(key);
    }

    public AnimationClip GetClip() // a method of  Interface IRecordable
    {


        AnimationClip muscleClip = new AnimationClip();

        foreach (KeyValuePair<int, AnimationCurve> muscleCurve in this.muscleCurves)
        {
            muscleClip.SetCurve("", typeof(Animator), HumanTrait.MuscleName[muscleCurve.Key], muscleCurve.Value);

        }


        // https://blog.unity.com/technology/mecanim-humanoids:
        //A "Muscle" is a normalized value [-1,1] that moves a bone for one axis between range [min,max]. 
        //Note that the Muscle normalized value can go below or over [-1,1] to overshoot the range. 
        //The range is not a hard limit, instead it defines the normal motion span for a Muscle.
        // A specific Humanoid Rig can augment or reduce the range of a Muscle Referential to augment or reduce its motion span.
        // The Muscle Space is the set of all Muscle normalized values for the Humanoid Rig. 
        //It is a Normalized Humanoid pose. A range of zero (min= max) for a bone axis means that there is no Muscle for it.
        // For example, the Elbow does not have a muscle for its Y axis, 
        //as it only stretches in and out (Z-Axis) and roll in and out (X-Axis). 
        //In the end, the Muscle Space is composed of at most 47 Muscle values
        // that completely describe a Humanoid body pose.


        // One beautiful thing about Muscle Space, is that it is completely abstracted from its original or any skeleton rig. It can be directly applied to any Humanoid Rig and it always create a believable pose.  
        // Another beautiful thing is how well Muscle Space interpolates. Compare to standard skeleton pose, 
        // Muscle Space will always interpolate naturally between animation key frames, during state machine transition or when mixed in a blend tree.

        // Computation-wise it also performs as the Muscle Space can be treated as a vector of a scalar
        //  that you can linearly interpolate as opposed to quaternions or Euler angles.

        // An approximation of human body and human motion
        // Every new skeleton rig built for a humanoid character or any animation captured will be
        //  an approximation of the human body and human motion. 
        //  No matter how many bones or how good your MOCAP hardware is, the result will be an approximation of the real thing.

        // This is a tough one. Why 2, not 3? or an arbitrary number of spines bones?  
        // Lets discard the latest, it is not about biomedical research.
        //  (Note that you can always use a Generic Rig if you absolutely need this level of precision).
        //  One spine bone is clearly under defined.
        foreach (KeyValuePair<string, AnimationCurve> rootCurve in this.rootCurves)
        {
            muscleClip.SetCurve("", typeof(Animator), rootCurve.Key, rootCurve.Value); // data.key = string, data.Value=AnimationCurve
        }

        // https://extra-ordinary.tv/2020/10/12/animating-hand-poses-in-unity/
        // => Rather than having a transform, position, and rotate to be animated, there is a single number to control how far each joint bends. 
        //The number is effectively the strength of the muscles around that joint.
        //  If relativePath is empty it refers to the game object the animation clip is attached to.
        // This gameObject may be a humanoid avatar
        // If relativePath is empty it refers to the GameObject the Animation/Animator component is attached to.
        // typeof(BlendShapesClip): https://forum.unity.com/threads/add-key-via-c-script-in-custom-clip.597142/

        //               ==>  
        // Keyframe[] keys;
        // keys = new Keyframe[3];
        // keys[0] = new Keyframe(0.0f, 0.0f);
        // keys[1] = new Keyframe(1.1f, 1.5f);
        // keys[2] = new Keyframe(2.0f, 0.0f);
        // curve = new AnimationCurve(keys);

        // var newCustomClip = track.CreateClip<BlendShapesClip>();

        // newCustomClip.displayName = "My New Clip";
        // newCustomClip.duration = 3f;

        // typeof(TimelineClip).GetMethod("AllocateAnimatedParameterCurves", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(newCustomClip,new object[]{});
        // newCustomClip.curves.SetCurve("", typeof(BlendShapesClip), "ShapeWeight", curve);



        return muscleClip;

    } //  public AnimationClip GetClip() // a method of  Interface IRecordable

    //https://gamedev.stackexchange.com/questions/183186/animation-via-animatorcontroller-created-in-script-does-not-play-in-unity


    // Compare the two versions of GetClip: The following is the version of GenericRecorder:
    // public AnimationClip GetClip
    // {
    //     get
    //     {
    //         AnimationClip clip = new AnimationClip(); // an animation clip for the character, the whole subpaths of the character

    //         foreach (ObjectAnimation animation in objectAnimations) // animation for each joint, which is animation.Path
    //         {
    //             foreach (CurveContainer container in animation.Curves) // container for each DOF in animation of the current joint

    //             {
    //                 if (container.Curve.keys.Length > 1)
    //                     clip.SetCurve(animation.Path, typeof(Transform), container.Property, container.Curve);
    //             }
    //         }

    //         return clip;
    //     }
    // }

} // public class HumanoidRecorder

public class BVHAnimationLoader : MonoBehaviour
{

    public Avatar bvhAvatar;

    public Transform bvhAvatarRootTransform;
     HumanPose currentHumanPose = new HumanPose();

    
    List<string> jointPaths = new List<string>(); // emtpy one
    List<Transform> avatarTransforms = new List<Transform>();
    HumanPoseHandler humanPoseHandler; 
    HumanoidRecorder humanoidRecorder;
    NativeArray<float> avatarPose;


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

    void ParseAvatarRootTransform(Transform rootTransform, List<string> jointPaths, List<Transform> transforms)
    {
        jointPaths.Add(""); // root tranform path is the empty string
        transforms.Add(rootTransform);

        foreach (Transform child in rootTransform)
        {
            ParseAvatarTransformRecursive(child, "", jointPaths, transforms);
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


    // private Dictionary<string, Transform> UnityBoneToTransformMap;
    // private Dictionary<string, string> BvhToUnityRenamingMap;

    // Get the transform with name within the hiearchy rooted by transform; Return transform itself or one of its children
    // private Transform getBoneTransformByName(string bvhName, Transform transform, bool first) {
    //     string bvhNameStandard = standardName(bvhName);
    //     string unityBoneName;

    //     if (this.bvhToUnityRenamingMap.ContainsKey(bvhNameStandard)) {
    //         unityBoneName = standardName( bvhToUnityRenamingMap[bvhNameStandard]);
    //     }
    //     else {
    //         // BvhUnityRenamingMap is not defined. 
    //          throw new InvalidOperationException("BvhUnityRenamingMap is not defined");
    //     } 

    //     if (first) { // check if transform itself is referred to by name.
    //         if (standardName(transform.name) == unityBoneName) {
    //             return transform;
    //         }
    //         if (this.UnityBoneToTransformMap.ContainsKey(unityBoneName) && this.UnityBoneToTransformMap[unityBoneName] == transform) {
    //             return transform;
    //         }
    //     }
    //     // The above two conditions failed to be met:

    //     for (int i = 0; i < transform.childCount; i++) {
    //         Transform child = transform.GetChild(i);
    //         if (standardName(child.name) == unityBoneName) {
    //             return child;
    //         }
    //         if (this.UnityBoneToTransformMap.ContainsKey(unityBoneName) && this.UnityBoneToTransformMap[unityBoneName] == child) { // targetName is a Unity bone name
    //             return child;
    //         }
    //     }
    //     // None of the return statements are encountered. Then it means that an error has occurred:
    //     throw new InvalidOperationException("Could not find bone \"" + bvhName + "\" under bone \"" + transform.name + "\".");
    // }

    //private void getCurves(string path, BVHParser.BVHBone bvhNode, bool first) {

    //this.getCurves(this.prefix, this.bp.bvhRootNode, this.bvhRootTransform, true) when first called. 
    // //  this.getCurves("Genesis8Male/Hips", "Spine", "Hips"  false)

    
    private void getCurves(string path, BVHParser.BVHBone bvhNode, Transform targetTransform, bool first) 
    // path is the CURRENT bvh node  which contains the animation key frames; targetTransform is the transform of the node at the path
    // bvhNodeParent is the parent of the node at the path
    {
        // first = true means bvhNode is the root node
        bool posX = false;
        bool posY = false;
        bool posZ = false;
        bool rotX = false;
        bool rotY = false;
        bool rotZ = false;

        float[][] values = new float[6][];

        Keyframe[][] keyframes = new Keyframe[7][];

        string[] props = new string[7];

      
      
        if (path != this.prefix)
        { // getCurve() is first called with path == this.prefix;   // this.prefix has the form of "Genesis8Male/Hips"
            path += "/";
        }
        // In our experiment, path = this.prefix when getCurves() is called for the first time, with first = true.
      
        //MJ: if (this.bvhRootTransform != this.bvhAnimator.transform || !first)
        if ( !first)
        {
            //path += bvhNodeTransform.name;
            path += targetTransform.name; // path becomes "Genesis8Male/Hips/Spine", bvhNode = "Spine" in the second call of SetCurves()
        }
        // else  {  path == this.prefix = Genesis8Male/Hips}
        // This needs to be changed to gather from all channels into two vector3, invert the coordinate system transformation and then make keyframes from it
        // Construct the data structure for frame data for each channel for the current node or joint
        for (int channel = 0; channel < 6; channel++) // 6 or 3 channels
        {
            if (!bvhNode.channels_bvhBones[channel].enabled)
            {
                continue;
            }

            switch (channel)
            {
                case 0:
                    posX = true;
                    props[channel] = "localPosition.x";
                    break;
                case 1:
                    posY = true;
                    props[channel] = "localPosition.y";
                    break;
                case 2:
                    posZ = true;
                    props[channel] = "localPosition.z";
                    break;
                case 3:
                    rotX = true;
                    props[channel] = "localRotation.x";
                    break;
                case 4:
                    rotY = true;
                    props[channel] = "localRotation.y";
                    break;
                case 5:
                    rotZ = true;
                    props[channel] = "localRotation.z";
                    break;
                default:
                    channel = -1;
                    break;
            }
            if (channel == -1)
            {
                continue;
            }

            keyframes[channel] = new Keyframe[frames];

        // public Keyframe(float time, float value, float inTangent, float outTangent, float inWeight, float outWeight);

        //
        // Summary:
        //     The time of the keyframe.
        //public float time { get; set; }
        //
        // Summary:
        //     The value of the curve at keyframe.
        // public float value { get; set; }

            values[channel] = bvhNode.channels_bvhBones[channel].values; // the animation key frames (joint angles for frames)

            if (rotX && rotY && rotZ && keyframes[6] == null)
            {
                keyframes[6] = new Keyframe[frames];
                props[6] = "localRotation.w";
            }
        } // for (int channel = 0; channel < 6; channel++)

        float time = 0f;

        // Get the position data of the current joint/node  for each frame
        if (posX && posY && posZ) // the position value of the joint center
        {
            Vector3 offset; //  used for the root node

                        
            if (this.blender) //  //  the BVH file will be assumed to have the Z axis as up and the Y axis as forward, X rightward as in Blender
            {
                offset = new Vector3(-bvhNode.offsetX, bvhNode.offsetZ, -bvhNode.offsetY); // => Unity frame
            }
            else //  //  the BVH file will be assumed to have the normal BVH convention: Y up; Z backward; X right (OpenGL: right handed)
            {
                offset = new Vector3(-bvhNode.offsetX, bvhNode.offsetY, bvhNode.offsetZ);  // To unity Frame
                // Unity:  Y: up, Z: forward, X = right or Y: up, Z =backward, X left (The above transform follows the second)
            }
            for (int i = 0; i < this.frames; i++)
            {
                time += 1f / this.frameRate;
                keyframes[0][i].time = time;
                keyframes[1][i].time = time;
                keyframes[2][i].time = time;
                if (blender)
                {
                    keyframes[0][i].value = -values[0][i];
                    keyframes[1][i].value = values[2][i];
                    keyframes[2][i].value = -values[1][i];
                }
                else
                {
                    keyframes[0][i].value = -values[0][i]; // From BVH to Unity: the sign of the x coordinate changes because of the frame change from BVH to Unity
                    keyframes[1][i].value = values[1][i];
                    keyframes[2][i].value = values[2][i];
                }
                if (first) // use the offset of the joint to compute the location of the root joint relative to the world space, when it is the root joint.
                {
                    Vector3 bvhPositionLocal = targetTransform.transform.parent.InverseTransformPoint(new Vector3(keyframes[0][i].value, keyframes[1][i].value, keyframes[2][i].value) + bvhAnimator.transform.position + offset);
                    keyframes[0][i].value = bvhPositionLocal.x * this.bvhAnimator.transform.localScale.x;
                    keyframes[1][i].value = bvhPositionLocal.y * this.bvhAnimator.transform.localScale.y;
                    keyframes[2][i].value = bvhPositionLocal.z * this.bvhAnimator.transform.localScale.z;
                }
            }
            if (first) // the first bone
            {   // public AnimationCurve(params Keyframe[] keys);
                this.clip.SetCurve(path, typeof(Transform), props[0], new AnimationCurve(keyframes[0]));
                this.clip.SetCurve(path, typeof(Transform), props[1], new AnimationCurve(keyframes[1]));
                this.clip.SetCurve(path, typeof(Transform), props[2], new AnimationCurve(keyframes[2]));
            }
            else
            {
                Debug.LogWarning("Position information on bones other than the root bone is currently not supported and has been ignored. If you exported this file from Blender, please tick the \"Root Translation Only\" option next time.");
            }
        } // if (posX && posY && posZ) // the position value of the joint center

        time = 0f;
        if (rotX && rotY && rotZ) // the rotatation value of the joint center
        {
            Quaternion oldRotation = targetTransform.transform.rotation;

            for (int i = 0; i < frames; i++)
            {

                Vector3 eulerBVH = new Vector3(wrapAngle(values[3][i]), wrapAngle(values[4][i]), wrapAngle(values[5][i]));
                Quaternion rot = fromEulerZXY(eulerBVH);
                if (blender)
                {
                    keyframes[3][i].value = rot.x;
                    keyframes[4][i].value = -rot.z;
                    keyframes[5][i].value = rot.y;
                    keyframes[6][i].value = rot.w;
                    //rot2 = new Quaternion(rot.x, -rot.z, rot.y, rot.w);
                }
                else
                {
                    keyframes[3][i].value = rot.x;
                    keyframes[4][i].value = -rot.y;
                    keyframes[5][i].value = -rot.z;
                    keyframes[6][i].value = rot.w;
                    //rot2 = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
                }

                if (first)
                { // first == true means that the curve is generated for the bvh root node
                    targetTransform.transform.rotation = new Quaternion(keyframes[3][i].value, keyframes[4][i].value, keyframes[5][i].value, keyframes[6][i].value);

                    // public Quaternion localRotation { get; set; }:  The rotation of the transform relative to the transform rotation of the parent.
                    // position and rotation atrributes are the values relative to the world space.
        
                    keyframes[3][i].value = targetTransform.transform.localRotation.x;
                    keyframes[4][i].value = targetTransform.transform.localRotation.y;
                    keyframes[5][i].value = targetTransform.transform.localRotation.z;
                    keyframes[6][i].value = targetTransform.transform.localRotation.w;
                }
                /*Vector3 euler = rot2.eulerAngles;

                keyframes[3][i].value = wrapAngle(euler.x);
                keyframes[4][i].value = wrapAngle(euler.y);
                keyframes[5][i].value = wrapAngle(euler.z);*/

                time += 1f / this.frameRate;
                keyframes[3][i].time = time;
                keyframes[4][i].time = time;
                keyframes[5][i].time = time;
                keyframes[6][i].time = time;
            } //   for (int i = 0; i < frames; i++) 

            targetTransform.transform.rotation = oldRotation;

            //public void SetCurve(string relativePath, Type type, string propertyName, AnimationCurve curve);
            // https://docs.unity3d.com/ScriptReference/AnimationClip.SetCurve.html
            // The root node of path is the game object to which AnimationClip component is attached, and
            // the tip of the path is the game object to which Animation Curive will be applied.
            this.clip.SetCurve(path, typeof(Transform), props[3], new AnimationCurve(keyframes[3])); // Material as well as Transform can be animated
            this.clip.SetCurve(path, typeof(Transform), props[4], new AnimationCurve(keyframes[4]));
            this.clip.SetCurve(path, typeof(Transform), props[5], new AnimationCurve(keyframes[5]));
            this.clip.SetCurve(path, typeof(Transform), props[6], new AnimationCurve(keyframes[6]));

        } //if (rotX && rotY && rotZ) // the rotatation value of the joint center 


        foreach (BVHParser.BVHBone child in bvhNode.children)
        {
            //this.getCurves(path, child, bvhNodeTransform, false);
            this.getCurves(path, child,targetTransform, false); //  this.getCurves("Genesis8Male/Hips", "Spine", "Hips"  false) in the second call
        }

    } // private void getCurves(string path, BVHParser.BVHBone bvhRootNode, Transform rootBoneTransform, bool first)


// this.SetMusclePoseEachFrame(i,  this.bp.bvhRootNode, this.bvhAvatarRootTransform); // true: first
    void SetMusclePoseEachFrame(int i, BVHParser.BVHBone bvhNode, Transform rootTransform,List<string> jointPaths, List<Transform> transforms ) 
    {
        jointPaths.Add(""); // root tranform path is the empty string
        transforms.Add(rootTransform);

        foreach (Transform child in rootTransform)
        {
            SetMusclePoseEachFrameRecursive(i, bvhNode, child, "", jointPaths, transforms);
        }

    }
 
 private void SetMusclePoseEachFrameRecursive(int i, BVHParser.BVHBone bvhNode, Transform child, string parentPath, 
                                                                                  List<string> jointPaths, List<Transform> transforms) 
    
    {

         string jointPath = parentPath.Length == 0 ? child.gameObject.name : parentPath + "/" + child.gameObject.name;
       
         float[] value = new float[6]; // Values of each DOF in joint

         Keyframe[] keyframe = new Keyframe[7]; // Positions and Quaternions

        // string[] props = new string[7];

      
        // This needs to be changed to gather from all channels into two vector3, invert the coordinate system transformation and then make keyframes from it
        // Construct the data structure for frame data for each channel for the current node or joint


        // Set the property names
        for (int channel = 0; channel < 6; channel++) // 6 or 3 channels
        // 	CHANNELS 6 Xposition Yposition Zposition Zrotation Xrotation Yrotation 
        // 	CHANNELS 3 Zrotation Xrotation Yrotation 
        {
            if (!bvhNode.channels_bvhBones[channel].enabled)
            {
                continue;
            }
           
            value[channel] = bvhNode.channels_bvhBones[channel].values[i]; // the animation key frames (joint angles for frames)

        } // for (int channel = 0; channel < 6; channel++)

        // Get the position data of the current joint/node  for each frame

        //humanPose = new HumanPose();

        if (first) // the position value of the joint center for the ith frame
        {
            Vector3 offset; //  used for the OffSet of each joint; ignored in this program


            if (this.blender) //  //  the BVH file will be assumed to have the Z axis as up and the Y axis as forward, X rightward as in Blender
            {
                offset = new Vector3(-bvhNode.offsetX, bvhNode.offsetZ, -bvhNode.offsetY); // => Unity frame
            }
            else //  //  the BVH file will be assumed to have the normal BVH convention: Y up; Z backward; X right (OpenGL: right handed)
            {
                offset = new Vector3(-bvhNode.offsetX, bvhNode.offsetY, bvhNode.offsetZ);  // To unity Frame
                                                                                           // Unity:  Y: up, Z: forward, X = right or Y: up, Z =backward, X left (The above transform follows the second)
            }

            // set each frame to the current avatar, Sara.

            if (blender)
            {
                keyframe[0].value = -value[0];
                keyframe[1].value = value[2];
                keyframe[2].value = -value[1];
            }
            else
            {
                keyframe[0].value = -value[0]; // From BVH to Unity: the sign of the x coordinate changes because of the frame change from BVH to Unity
                keyframe[1].value = value[1];
                keyframe[2].value = value[2];
            }

            Vector3 bvhPositionLocal = targetTransform.transform.parent.InverseTransformPoint(new Vector3(keyframe[0].value, keyframe[1].value, keyframe[2].value) 
                                                                                                   + animator.transform.position + offset);
            keyframe[0].value = bvhPositionLocal.x * animator.transform.localScale.x;
            keyframe[1].value = bvhPositionLocal.y * animator.transform.localScale.y;
            keyframe[2].value = bvhPositionLocal.z * animator.transform.localScale.z;

            // first == true means that the curve is generated for the bvh root node => get the position and rotation values from elsewhere than Mocap data
            targetTransform.transform.rotation = new Quaternion(keyframe[3].value, keyframe[4].value, keyframe[5].value, keyframe[6].value);

            // public Quaternion localRotation { get; set; }:  The rotation of the transform relative to the transform rotation of the parent.
            // position and rotation atrributes are the values relative to the world space.

            keyframe[3].value = targetTransform.transform.localRotation.x;
            keyframe[4].value = targetTransform.transform.localRotation.y;
            keyframe[5].value = targetTransform.transform.localRotation.z;
            keyframe[6].value = targetTransform.transform.localRotation.w;


            // set current frame, ith, to the avatar, Sara, this.saraAnimator.transform 

            animator.gameObject.transform.localPosition = new Vector3(keyframe[0].value, keyframe[1].value, keyframe[2].value);



            // if (first) // the first bone
            // {   // public AnimationCurve(params Keyframe[] keys);
            //     this.clip.SetCurve(path, typeof(Transform), props[0], new AnimationCurve(keyframes[0]));
            //     this.clip.SetCurve(path, typeof(Transform), props[1], new AnimationCurve(keyframes[1]));
            //     this.clip.SetCurve(path, typeof(Transform), props[2], new AnimationCurve(keyframes[2]));
            // }
            // else
            // {
            //     Debug.LogWarning("Position information on bones other than the root bone is currently not supported and has been ignored. If you exported this file from Blender, please tick the \"Root Translation Only\" option next time.");
            // }

         
           

        }// if (first)



        if (!first) // set the rotatation value of the joint center for the ith frame
        {
            // Quaternion oldRotation = targetTransform.transform.rotation;




            Vector3 eulerBVH = new Vector3(wrapAngle(value[3]), wrapAngle(value[4]), wrapAngle(value[5]));

            Quaternion rot = fromEulerZXY(eulerBVH);

            if (blender)
            {
                keyframe[3].value = rot.x;
                keyframe[4].value = -rot.z;
                keyframe[5].value = rot.y;
                keyframe[6].value = rot.w;
                //rot2 = new Quaternion(rot.x, -rot.z, rot.y, rot.w);
            }
            else
            {
                keyframe[3].value = rot.x;
                keyframe[4].value = -rot.y;
                keyframe[5].value = -rot.z;
                keyframe[6].value = rot.w;
                //rot2 = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
            }


            /*Vector3 euler = rot2.eulerAngles;

            keyframes[3][i].value = wrapAngle(euler.x);
            keyframes[4][i].value = wrapAngle(euler.y);
            keyframes[5][i].value = wrapAngle(euler.z);*/

            animator.gameObject.transform.localRotation = new Quaternion(keyframe[3].value, keyframe[4].value, keyframe[5].value, keyframe[6].value);
            // targetTransform.transform.rotation = oldRotation;

            //public void SetCurve(string relativePath, Type type, string propertyName, AnimationCurve curve);
            // https://docs.unity3d.com/ScriptReference/AnimationClip.SetCurve.html
            // The root node of path is the game object to which AnimationClip component is attached, and
            // the tip of the path is the game object to which Animation Curive will be applied.
            // this.clip.SetCurve(path, typeof(Transform), props[3], new AnimationCurve(keyframes[3])); // Material as well as Transform can be animated
            // this.clip.SetCurve(path, typeof(Transform), props[4], new AnimationCurve(keyframes[4]));
            // this.clip.SetCurve(path, typeof(Transform), props[5], new AnimationCurve(keyframes[5]));
            // this.clip.SetCurve(path, typeof(Transform), props[6], new AnimationCurve(keyframes[6]));

        } // if (!first)

       // Create the muscle animation clip
         float deltaTime =  1f / this.frameRate;

         this.humanoidRecorder.TakeSnapshot( deltaTime ); // save motion at each frame time for each muscle

         
        // Call getCurves() recursively.
        // Define the animation curves for each child bone of the root bone.


        //   Get the transform of "bvhRoot.name"  within the children of "avatarNodeTransform";
        //   Return "avatarNodeTransform" itself or one of its children


        // foreach (Transform child in rootTransform)
        // {
        //     ParseAvatarTransformRecursive(child, "", jointPaths, transforms);
        // }

        foreach (Transform grandChild in child)
        {
            SetMusclePoseEachFrameRecursive(i, grandChild, jointPath);
        }

       

    } // SetMusclePoseEachFrameRecursive()

    // first call: this.prefix = getPathBetween(this.bvhRootTransform, this.bvhAnimator.transform, true, true);
    public static string getPathBetween(Transform target, Transform root, bool skipFirst, bool skipLast) 
     // target ="hips", root = "avatar"  when first called
    {
        if (root == target) // the termining condition for the recursion
        {
            if (skipLast) // true in our experiment
            {
                return "";
            }
            else
            {
                return root.name; // the same as target.name
            }
        }
        // The target transform is a child of the root transform
        for (int i = 0; i < root.childCount; i++) // root = "avatar" in the first call of the function
        {
            Transform child = root.GetChild(i);

            if (target.IsChildOf(child))
            {

                if (skipFirst)
                { // skipFirst means skip the first node "Sara" to find the path to the  "target" transform
                    return getPathBetween(target, child, false, skipLast); // Find the path from child to target, with skipFirst = false
                    // root = "Sara", child =  "Genesis3Female"; target = "hips"; skipFirst = false
                }
                else
                {
                    return root.name + "/" + getPathBetween(target, child, false, skipLast);
                    // root ="Genesis3Female";  target ="hips", child  = "hips";  => getPathBetween(target, child, false, skipLast) =""
                    // return "Genesis3Female/hips" at the end
                }
            }
        }

        throw new InvalidOperationException("No path between transforms " + target.name + " and " + root.name + " found.");
    }

    private Animator  getbvhAnimator()
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
    public void loadAnimation()
    {
        //  this.getbvhAnimator(); // Get Animator component of the virtual human to which this BVHAnimationLoader component is added
        // =>   this.bvhAnimator = this.GetComponent<Animator>();

        // the character is automatically set in the initial pose by playing the clip "temp (3)|temp (3)", which is associated with the 
        // state "InitState"

        //animatorOverrideController = new AnimatorOverrideController(bvhAnimator.runtimeAnimatorController);
        //bvhAnimator.runtimeAnimatorController = animatorOverrideController;


        if (this.bp == null)
        {
            throw new InvalidOperationException("No BVH file has been parsed.");
        }


        this.frames = this.bp.frames;


        // this.bvhAnimator.transform is the Transform component attached to this gameObject, where "this gameObject" is
        // the Avatar gameObject to which bvhAnimator component is attached. This is "avatar" in our experiment;
        // The bvh root transform is "Hips", which is under  this.bvhAnimator.transform, "Skeleton":
        // Sara => Genesis3Female => hips => pelvis. This.prefix = "Genesis3Female/Hips"

        //this.bvhAnimator.transform =  this.bvhAnimator.gameObject.transform, 
        // where  this.bvhAnimator.gameObjectrefers to the gameObject to which this.bvhAnimator is attached
        // this.bvhAvatarRoot refers to the "Hips' joint of the bvh avatar hierarchy, which is NOT the same as the gameObject to which this.bvhAnimator is attached
        ParseAvatarRootTransform(this.bvhAvatarRootTransform, this.jointPaths, this.avatarTransforms);
        // HumanPose currentHumanPose = new HumanPose(); 
        for (int i = 0; i < this.frames; i++)
        {
            this.SetMusclePoseEachFrame(i, this.bp.bvhRootNode, this.bvhAvatarRootTransform); // true: first
                                                                                                                                          // ParseAvatarTransformRecursive(child, "", jointPaths, transforms);
           
        }



        // ParseAvatarRootTransform(this.saraAvatarRoot, this.jointPaths, this.avatarTransforms);


        //this.getCurves(this.prefix, this.bp.bvhRootNode, this.bvhRootTransform, true); // first = true means that this.bp.bvhRootNode is the root node
        //  this.bvhRootTransform is the transform of bp.bvhRootNode
        // Save the humanoid animation curves for each muscle as muscle AnimationClip
        this.clip = this.humanoidRecorder.GetClip();
       
        //this.getCurves(this.prefix, this.bp.bvhRootNode,true); // true: first

        //this.bvhAnimator.transform.position = bvhAnimatorPosition;
        //this.bvhAnimator.transform.rotation = bvhAnimatorRotation;

        //this.clip.EnsureQuaternionContinuity();


        // MJ will use Animator and Animator Controller rather than Animation component in order to use Mechanim Humanoid rather than
        // the old Legacy animation technique;
        // The difference between Animation and Animator: refer to https://forum.unity.com/threads/whats-the-difference-between-animation-and-animator.288962/?msclkid=70fded59c86d11ec9503fa26f247a194
        // The animation component is the legacy animation system which was not updated since 4.0, it doesn't support new feature like Sprite animation or material properties animation.
        //  In those case you have to use the new Animator Component.

        // https://stackoverflow.com/questions/45949738/what-is-the-difference-between-playing-animator-state-and-or-playing-animationcl?msclkid=70fe9844c86d11ecb10d2d137743528e


        //         Basically, the system works this way:

        // First of all, you need an Animator Controller asset. 
        // This Animator is a Finite State Machine (with substates etc.), and every single state can have an Animation Clip (you assign it via script of via Inspector in the Motion field). 
        //  When the FSM enters a specific state, it will play the Animation Clip assigned.
        //  Clearly the Animator, in order to be used by a game object, has to be assigned to that object via the Animator component.

        // Animation Clips are assets which contain the actual animation, they're never assigned as components to game objects,
        // but instead they must be referenced by an Animator to be played, as I said before.

        // Blend trees are used to blend different animations in a seamless way by using linear interpolation. 
        // They're a "special" state of the Animator, which has multiple Animation Clips that you can interpolate to display transitions from a state to another,
        // for example when you need to change the animation among running forward, running left and running right.

        // The argument is very broad, you can start to get into it by reading the official documentation about Animators, Animation Clips and Blend Trees here:

        // https://docs.unity3d.com/Manual/AnimationSection.html
        //  this.bvhAnimator 

        //this.clip.name = "BVHClip (" + (clipCount++) + ")"; // clipCount is static

        this.clip.name = "speechGesture"; //' the string name of the AnimationClip

        

        //Use the Legacy Animation using Animation component
        // this.clip.legacy = true; // MJ

        // if (this.anim == null)
        // { // null by default
        //   //  this.anim = this.bvhAnimator.gameObject.GetComponent<Animation>();
        //     if (this.anim == null)
        //     { //  The animation component is used to play back animations.
        //         this.anim = this.bvhAnimator.gameObject.AddComponent<Animation>();
        //     }
        // }
        // this.anim.AddClip(this.clip, this.clip.name);
        // this.anim.clip = this.clip;
        // this.anim.playAutomatically = this.autoPlay;

        // if (this.autoPlay)
        // {
        //     this.anim.Play(this.clip.name);  // MJ: Animator.Play(string stateName); play a state stateName; Base Layer.Bounce, e.g.
        //                                       // "Entry" => Bounce 
        // }



        // It is expected that the Animator is reset when you change a clip. 
        // Unity needs to save the Animator state before replacing the AnimationClip and set it back after it has been replaced,
        //  but that feature is not implemented in some versions of Unity.
        // https://support.unity.com/hc/en-us/articles/205845885-Animator-state-is-reset-when-AnimationClips-are-replaced-using-an-AnimatorControllerOverride

        //https://answers.unity.com/questions/1319072/how-to-change-animation-clips-of-an-animator-state.html
        //https://docs.unity3d.com/ScriptReference/AnimatorOverrideController.ApplyOverrides.html







        //Sets the animator in playback mode.
        //this.bvhAnimator.StartPlayback();

        // In playback mode, you control the animator by setting a time value. The animator is not updated from game logic. 
        //Use playbackTime to explicitly manipulate the progress of time

        // Animator.StopPlayback: Stops the animator playback mode. When playback stops, the avatar resumes getting control from game logic.
        // NormalizedTime: https://stackoverflow.com/questions/52722206/unity3d-get-animator-controller-current-animation-time?msclkid=aef6aeffce6f11ecb436e23284615f76
        // Controlling animation in script: https://catwolf.org/qs?id=c9aa3b58-3373-4703-aae9-5e8487ae27ec&x=x

        //     Apparently on this site I don't have enough "rep" to comment. Animators should begin playing automatically, 
        //     so the problem is not with how you're "starting" it. Ensure that

        // -The animator component is enabled

        // -The game object is enabled

        // -The state you want to play is the default

        // -The animator isn't transitioning to another state at the beginning

        // Barring that, if you are trying to explicitly tell the animator what state to play, you need to name the state,
        //  not the name of the animation file
        //this.ChangeClipAtRunTime(this.bvhAnimator, this.clipName, this.clip );

        this.SetClipAtRunTime( this.bvhAnimator, this.clip.name, this.clip);

        //https://www.telerik.com/blogs/implementing-indexers-in-c#:~:text=To%20declare%20an%20indexer%20for,value%20from%20the%20object%20instance.

    } // public void loadAnimation(Animator animator)

    void SetClipAtRunTime(Animator animator, string currentClipName, AnimationClip animClip ){
    //Animator anim = GetComponent<Animator>(); 

    animatorOverrideController =    new AnimatorOverrideController( animator.runtimeAnimatorController);
    
    //   public AnimationClip[] animationClips = animator.animatorClips;
    clipOverrides = new AnimationClipOverrides(animatorOverrideController.overridesCount);
    // original clip vs override clip
    animatorOverrideController.GetOverrides(clipOverrides); // get 

     //var anims = new List<KeyValuePair<AnimationClip, AnimationClip>>();

    
     clipOverrides[currentClipName] =  animClip;

     AnimationClip animClipToOverride =  clipOverrides[currentClipName];
     Debug.Log( animClipToOverride );

     animatorOverrideController.ApplyOverrides(clipOverrides);

     animator.runtimeAnimatorController = animatorOverrideController;

     // set the bvh's animatorOverrideController to that of Sara

     //Animator saraAnimator =  this.getSaraAnimator();
     this.saraAnimator.runtimeAnimatorController =   animatorOverrideController;

    // Transite to the new state with the new bvh motion clip
    //this.bvhAnimator.Play("ToBvh");
  } // void SetClipAtRunTime

   void ChangeClipAtRunTime(Animator animator, string currentClipName, AnimationClip clip ){
    //Animator anim = GetComponent<Animator>(); 

    AnimatorOverrideController overrideController =    new AnimatorOverrideController();

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

    public void playAnimation()
    {

        this.bvhAnimator.Play("BVHBehaviour");  // MJ: Animator.Play(string stateName); play a state stateName; Base Layer.Bounce, e.g.
                                              // "Entry" => Bounce        

        // if (this.bp == null)
        // {
        //     throw new InvalidOperationException("No BVH file has been parsed.");
        // }
        // if (anim == null || clip == null)
        // {
        //     this.loadAnimation();
        // }
        // this.anim.Play(this.clip.name);
    }

    public void stopAnimation()
    {

        
        this.bvhAnimator.enabled  = false;
        // if (clip != null)
        // {
        //     if (anim.IsPlaying(clip.name))
        //     {
        //         anim.Stop();
        //     }
        // }
    }

    void Start()
    {
        this.parseFile();


        this.bvhAnimator = this.getbvhAnimator(); // Get Animator component of the virtual human to which this BVHAnimationLoader component is added
        // =>   this.bvhAnimator = this.GetComponent<Animator>();
        
        this.saraAnimator =  this.getSaraAnimator();
        this.saraAnimator.runtimeAnimatorController = this.bvhAnimator.runtimeAnimatorController;

        this.loadAnimation();


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


        // Make ready to record the motion of the bvh file as a humanoid muscle clip
        this.humanoidRecorder = new HumanoidRecorder( this.bvhAnimator);

        // Create humanPoseHandler using the root transform of the avatar
        //this.humanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAnimator.transform);

       // Create humanPoseHandler using jointPaths: A list that defines the avatar joint paths. 
       // Each joint path starts from the node after the root transform and continues down the avatar skeleton hierarchy.
       // The root transform joint path is an empty string.

       // https://docs.unity3d.com/ScriptReference/HumanPoseHandler-ctor.html:
       // ParseAvatarRootTransform(this.bvhAvatarRoot, this.jointPaths, this.avatarTransforms);

       // this.humanPoseHandler = new HumanPoseHandler(this.bvhAvatar, this.jointPaths.ToArray());

       // this.avatarPose = new NativeArray<float>(this.jointPaths.Count * 7, Allocator.Persistent);

    //     for (int i = 0; i < this.jointPaths.Count; ++i)
    //     {
    //         Vector3 position = this.avatarTransforms[i].localPosition;
    //         Quaternion rotation = this.avatarTransforms[i].localRotation;
    //         this.avatarPose[7 * i] = position.x;
    //         this.avatarPose[7 * i + 1] = position.y;
    //         this.avatarPose[7 * i + 2] = position.z;
    //         this.avatarPose[7 * i + 3] = rotation.x;
    //         this.avatarPose[7 * i + 4] = rotation.y;
    //         this.avatarPose[7 * i + 5] = rotation.z;
    //         this.avatarPose[7 * i + 6] = rotation.w;
    //     }


    //    // (1) GetHumanPose:	Computes a human pose from the avatar skeleton bound to humanPoseHandler, stores the pose in the human pose handler, and returns the human pose.
    //    // Converts an avatar pose to a human pose and stores it as the internal human pose inside the human pose handler

    //     (2) this.humanPoseHandler.SetInternalAvatarPose(this.avatarPose);  // If the human pose handler was constructed with a skeleton root transform, this method does nothing.

     //     (3)  HumanPoseHandler.GetInternalHumanPose: Gets the internal human pose stored in the human pose handler
       

        //this.avatarPose.Dispose();
        //this.humanPoseHandler.Dispose();
        
        // this.avatar = this.saraAnimator.avatar;
        // Transform hipTransform = this.avatar.GetBoneTransform(HumanBodyBones.Hips);
        // humanPoseHandler = new HumanPoseHandler(this.avatar, hips.transform);
        // humanPose = new HumanPose();
        // humanPoseHandler.GetHumanPose(ref humanPose);
        


    }

    void Update()
    {

        
        // if (autoStart)
        // {
           

        //    this.parseFile();

        //    this.loadAnimation();

        //    this.bvhAnimator.SetTrigger("ToBvh");

        //    this.saraAnimator.SetTrigger("ToBvh");

           

        // }


          // https://forum.unity.com/threads/mecanim-animationclip-at-runtime.525983/

        //this.setPose(this.prefix, this.bp.bvhRootNode, this.bvhRootTransform, true); // true: first
        //for (int i = 0; i < this.jointPaths.Count; ++i)
    //     {
    //         Vector3 position = this.avatarTransforms[i].localPosition;
    //         Quaternion rotation = this.avatarTransforms[i].localRotation;
    //         this.avatarPose[7 * i] = position.x;
    //         this.avatarPose[7 * i + 1] = position.y;
    //         this.avatarPose[7 * i + 2] = position.z;
    //         this.avatarPose[7 * i + 3] = rotation.x;
    //         this.avatarPose[7 * i + 4] = rotation.y;
    //         this.avatarPose[7 * i + 5] = rotation.z;
    //         this.avatarPose[7 * i + 6] = rotation.w;
    //     }


    //    // GetHumanPose:	Computes a human pose from the avatar skeleton bound to humanPoseHandler, stores the pose in the human pose handler, and returns the human pose.
    //    // Converts an avatar pose to a human pose and stores it as the internal human pose inside the human pose handler

    //    // HumanPoseHandler.GetInternalHumanPose: Gets the internal human pose stored in the human pose handler.
    //     this.humanPoseHandler.SetInternalAvatarPose(this.avatarPose);

    //     HumanPose humanPose = new HumanPose();
    //     this.humanPoseHandler.GetInternalHumanPose( ref humanPose);

        // Read muscles from humanPose and create muscle animation clip

    }
} // public class BVHAnimationLoader : MonoBehaviour
