using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

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
    [Header("Advanced settings")]
    [Tooltip("When this option is enabled, standard Unity humanoid bone names will be mapped to the corresponding bones of the skeleton.")]
    public bool standardBoneNames = true;
    [Tooltip("When this option is disabled, bone names have to match exactly.")]
    public bool flexibleBoneNames = true;

    [Tooltip("This allows you to give a mapping from names in the BVH file to actual bone names. If standard bone names are enabled, the target names (==actual bone names) may also be Unity humanoid bone names. Entries with **empty BVH names** will be ignored.")]
    public FakeDictionary[] bvhToUnityRenamingMapArray = null; // This dictionary may be filled by BVHAnimationLoaderEditor.cs

    [Header("Animation settings")]
    [Tooltip("When this option is set, the animation start playing automatically after being loaded.")]
    public bool autoPlay = false;
    [Tooltip("When this option is set, the animation will be loaded and start playing as soon as the script starts running. This also implies the option above being enabled.")]
    public bool autoStart = false;
    [Header("Animation")]
    [Tooltip("This is the Animation component to which the clip will be added. If left empty, a new Animation component will be added to the target avatar.")]
    public Animation anim;
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
    private Transform bvhRootTransform;
    private string prefix;
    private int frames;
    private Dictionary<string, string> pathToBone;  // the default value of reference variable is null
    private Dictionary<string, string[]> boneToMuscles;  // the default value of reference variable is null
    private Dictionary<string, Transform> UnityBoneToTransformMap; // the default value of reference variable is null
    private Dictionary<string, string> bvhToUnityRenamingMap;  // the default value of reference variable is null

    [Serializable]
    public struct FakeDictionary
    {
        public string bvhName;
        public string targetName;
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

    private string standardName(string name)
    {
        if (!this.flexibleBoneNames)
        {
            return name;
        }
        // get the standard bone names which do not contain space, hyphen, and Capital letters
        name = name.Replace(" ", "");
        name = name.Replace("_", "");
        name = name.ToLower();
        return name;
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

    private Transform getBoneTransformByName(string bvhName, Transform targetTransform, bool first)
    {
        // first = true means that bvhName is the root node
        string bvhNameStandard = standardName(bvhName);


        // if (this.bvhToUnityRenamingMap.ContainsKey(bvhNameStandard))
        // {
        //     bvhNameStandard = standardName(bvhToUnityRenamingMap[bvhNameStandard]);
        // }

        if (first)
        { // check if the bhvNode is the root node of the bvh hiearachy
        // Try it for the case of NOT using Standard Unity Bone Names
            if (standardName(targetTransform.name) == bvhNameStandard)
            { 
                return targetTransform;
            }
        // //  Try it for the case of  using Standard Unity Bone Names    
        //     if (this.UnityBoneToTransformMap.ContainsKey(bvhNameStandard) && this.UnityBoneToTransformMap[bvhNameStandard] == targetTransform)
        //     {
        //         return targetTransform;
        //     }

            // None of the return statements are encountered. Then it means that an error has occurred:
            throw new InvalidOperationException( bvhName + "is supposed to be the " + targetTransform.name + " but IS NOT");
        }
        // first is NOT true:  Try to find bvhName among the children of targetTransform;
       
        for (int i = 0; i < targetTransform.childCount; i++)
        {
            Transform childTransform = targetTransform.GetChild(i);
             //// Try it for the case of NOT using Standard Unity Bone Names
            if (standardName(childTransform.name) == bvhNameStandard) {
               return childTransform;
            }
            // //  Try it for the case of  using Standard Unity Bone Names    
            // if (this.UnityBoneToTransformMap.ContainsKey(bvhNameStandard) && this.UnityBoneToTransformMap[bvhNameStandard] == childTransform)
            // { // targetName is a Unity bone name
            //     return childTransform;
            // }
        }
        // None of the return statements are encountered. Then it means that an error has occurred:
        throw new InvalidOperationException( bvhName + "is supposed to be under/below " + targetTransform.name + " but IS NOT");
    }

    //private void getCurves(string path, BVHParser.BVHBone bvhNode, bool first) {

    //this.getCurves(this.prefix, this.bp.bvhRootNode, this.bvhRootTransform, true) when first called. 
    // //  this.getCurves("Genesis8Male/Hips", "Spine", "Hips"  false)
    private void getCurves(string path, BVHParser.BVHBone bvhNode, Transform targetTransform, bool first) 
    // bvhNode is the current bvh node to which the animation key frames will be assigned
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

        //   Get the transform of "bvhRoot.name"  within the children of "avatarNodeTransform";
        //   Return "avatarNodeTransform" itself or one of its children

        Transform bvhNodeTransform = getBoneTransformByName(bvhNode.name, targetTransform, first);
        // ROOT Hips{ => bvhNode.name
	    //   OFFSET -14.64140 90.27770 -84.91600
        //   CHANNELS 6 Xposition Yposition Zposition Zrotation Xrotation Yrotation => targetTransform
        //    JOINT Spine {
            //}  
      
        if (path != this.prefix)
        { // getCurve() is first called with path == this.prefix;   // this.prefix has the form of "Genesis8Male/Hips"
            path += "/";
        }
        // In our experiment, path = this.prefix when getCurves() is called in the first time, with first = true.
        // In this case, path = 
        //MJ: if (this.bvhRootTransform != this.bvhAnimator.transform || !first)
        if ( !first)
        {
            //path += bvhNodeTransform.name;
            path += targetTransform.name; // path becomes "Genesis8Male/Hips/Spine", bvhNode = "Spine" in the second call of SetCurves()
        }
   
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

        // Call getCurves() recursively.
        // Define the animation curves for each child bone of the root bone.

        foreach (BVHParser.BVHBone child in bvhNode.children)
        {
            //this.getCurves(path, child, bvhNodeTransform, false);
            this.getCurves(path, child, bvhNodeTransform, false); //  this.getCurves("Genesis8Male/Hips", "Spine", "Hips"  false) in the second call
        }

    } // private void getCurves(string path, BVHParser.BVHBone bvhRootNode, Transform rootBoneTransform, bool first)

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
                return root.name; // tye same as target.name
            }
        }
        // The target transform is a child of the root transform
        for (int i = 0; i < root.childCount; i++) // root = "avatar" in the first call of the function
        {
            Transform child = root.GetChild(i);

            if (target.IsChildOf(child))
            {

                if (skipFirst)
                { // skipFirst means skip the first node "Avatar" to find the path to the  "target" transform
                    return getPathBetween(target, child, false, skipLast); // Find the path from child to target, with skipFirst = false
                    // root = "avatar", child =  "Genesis8Male"; target = "hips"; skipFirst = false
                }
                else
                {
                    return root.name + "/" + getPathBetween(target, child, false, skipLast);
                    // root ="Genesis8Male";  target ="hips", child  = "hips";  => getPathBetween(target, child, false, skipLast) =""
                    // return "Genesis8Male/hips" at the end
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

        // // Get the transforms for the bones of the Unity Avatar imported from say Daz3D.
        // if (this.UnityBoneToTransformMap == null)
        // {

        //     if (standardBoneNames)
        //     { // Use the standard Unity Human Bone Names for BVH hierarchy??
        //         Dictionary<Transform, string> transformToBoneMapUnityAvatar; // Transform: string

        //         BVHRecorder.populateBoneMap(out transformToBoneMapUnityAvatar, bvhAnimator);
        //         // switch { transform: boneName} to { boneName: transform}
        //         this.UnityBoneToTransformMap = transformToBoneMapUnityAvatar.ToDictionary(kp => standardName(kp.Value), kp => kp.Key); // switch the order of Transform and string
        //     }
        //     else
        //     {
        //         this.UnityBoneToTransformMap = new Dictionary<string, Transform>(); // create an empty UnityBoneToTransformMap dictionary
        //     }
        // }

        // Use the 

        // Get the map that maps  vbh bone names to unity bone names
        // this.bp.boneList contains the list of bvh bones; this.bp.boneList[i].name is the bvh bone name
        // this.bvhToUnityRenamingMap = new Dictionary<string, string>(); // create an empty dict ={  bvh bone name : unity bone name }

        // // Create an mapping from bvh bone names to the target bone names used in Unity
        // if (this.bvhToUnityRenamingMapArray != null) // if the bvhToUnityBoneName reNaiming map is defined by the user in the inspector
        // {

        //     foreach (FakeDictionary entry in this.bvhToUnityRenamingMapArray)
        //     {

        //         if (entry.bvhName != "" && entry.targetName != "")
        //         {

        //             bvhToUnityRenamingMap.Add(standardName(entry.bvhName), standardName(entry.targetName));
        //         }
        //     }
        // }
        // if this.boneBvhToUnityRenamingMap is not created by the user in Inspector, then BvhToUnityRenamingMap will be null.

        // BvhToUnityRenamingMap == this.boneBvhToUnityRenamingMap

        Queue<Transform> transformsInImportedAvatar = new Queue<Transform>();

        transformsInImportedAvatar.Enqueue(this.bvhAnimator.gameObject.transform); // add the root transform of the avatar to Unity transforms queue
        //  this.bvhAnimator.transform = 'avatar'
        string bvhRootBoneName = standardName(this.bp.bvhRootNode.name); // this.bp.bvhRootNode.name = "Hips"; bvhRootBoneName ='hips'


        // BvhToUnityRenamingMap.ContainsKey(rootBoneTransformNameBvh) is false when
        // this.boneBvhToUnityRenamingMap is not created by the user; 
        // Check if the root bone name from bvh file is mapped to a Unity standard bone name
        // if (bvhToUnityRenamingMap.Count != 0)
        // {
        //     if (bvhToUnityRenamingMap.ContainsKey(bvhRootBoneName))
        //     {
        //         bvhRootBoneName = standardName(bvhToUnityRenamingMap[bvhRootBoneName]); // get the unity root bone name
        //     }

        // }

        // Check if  rootBoneTransformNameUnity to used as the root of the bvh character in Unity corresponds to some bone in the "Avatar" hierarchy imported in the Unity scene, e.g., imported from Daz3D humanoid
        while (transformsInImportedAvatar.Any())
        {

            Transform transformInImportedAvatar = transformsInImportedAvatar.Dequeue(); // get the transform from the queue
            // Transform.name is the name of the game object to which Transform component is attached.
            // if the root bone of bvh hierarchy is the same as the root bone of the Unity avatar character, use this bone as this.rootBoneTransform
            if (standardName(transformInImportedAvatar.name) == bvhRootBoneName)
            {
                this.bvhRootTransform = transformInImportedAvatar;

                break;
            }

            // if (UnityBoneToTransformMap.ContainsKey(bvhRootBoneName) && UnityBoneToTransformMap[bvhRootBoneName] == transformInImportedAvatar)
            // {
            //     this.bvhRootTransform = transformInImportedAvatar;

            //     break;
            // }
            // Otherwise, add the children nodes of  transformInImportedAvatar to the queue, in order to 
            // check if the root node of the bvh hierarchy is equal to  some child node of the Unity avatar;
            // It means that the avatar gameObject has other nodes than the the root of the bvh body.

            for (int i = 0; i < transformInImportedAvatar.childCount; i++)
            {
                transformsInImportedAvatar.Enqueue(transformInImportedAvatar.GetChild(i));
            }
        } // while

        // When the control reaches here, it means that the root bvh bone is NOT found in the Unity avatar;
        // THis is an error.
        // if (this.bvhRootTransform == null) // The following logic is dubious; commented by MJ
        // { // Use 
        //     this.bvhRootTransform = BVHRecorder.getrootBoneTransform(bvhAnimator);
        //     Debug.LogWarning("Using the root Transform of the Unity Avatar \"" + this.bvhRootTransform + "\" as the transform  of the bvh root.");
        // }
        // The rootBoneTransform was not identified so far:
        if (this.bvhRootTransform == null)
        {
           // Debug.LogWarning("The name of the bvh root should be the same as the real root, say 'hips' of the Unity character model");

            throw new InvalidOperationException("The bvh root bone \"" + bp.bvhRootNode.name + "\" not found in the Unity character model");
        }

        this.frames = this.bp.frames;

       

        
      
        //public static string getPathBetween(Transform target, Transform root, bool skipFirst, bool skipLast) 
        // Get the"prefix" path of nodes from root "this.bvhAnimator.transform" to target "this.rootBoneTransform", which is the
        // root node of the bvh hierarchy. This prefix path is not controlled by the bvh motion data.

        // RelativePath for AnimationClip.SetCurve: https://forum.unity.com/threads/help-i-dont-get-it-animationclip-setcurve-relativepath.168050/
        //   public static string getPathBetween(Transform target, Transform root, bool skipFirst, bool skipLast)
        // Get the prefix which consists of bones without the first and the last bone
        this.prefix = getPathBetween(this.bvhRootTransform, this.bvhAnimator.transform, true, true);
        // this.prefix has the form of root.name + "/"
        // this.bvhAnimator.transform is the Transform component attached to this gameObject, where "this gameObject" is
        // the Avatar gameObject to which bvhAnimator component is attached. This is "avatar" in our experiment;
        // The bvh root transform is "Hips", which is under  this.bvhAnimator.transform:
        // Avatar => Genesis8Male => hips => pelvis. This.prefix = "Genesis8Male/Hips"

        // Save the root transform of the Unity avatar
        Vector3 bvhAnimatorPosition = this.bvhAnimator.transform.position;
        Quaternion bvhAnimatorRotation = this.bvhAnimator.transform.rotation;

        // Set the identity transform to the root node of the Unity Avatar
        this.bvhAnimator.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
        this.bvhAnimator.transform.rotation = Quaternion.identity;

        // 	Creates an animation curve from an arbitrary number of keyframes.
        // this.bp.root contains the bvh motion data to used to define Animation frames.
        // this.prefix refers to the real root node of the avatar, whidh is a child of the avatar gameObject to which the AnimationClip
        // component is attached. If the path = this.prefix is "", then the frame data will be applied to the GameObject to which
        // the AnimationClip component is attached. 

        // getCurves sets this.clip.SetCurves

        this.clip = new AnimationClip();
        // Create the animation clip to this.clip by   this.clip.SetCurve(path, typeof(Transform), props[0], new AnimationCurve(keyframes[0]));
        this.getCurves(this.prefix, this.bp.bvhRootNode, this.bvhRootTransform, true); // true: first:  this.bvhRootTransform = "Skeleton/hips"
                                                                                       //  this.bvhRootTransform is the transform of bp.bvhRootNode

        this.getCurves(this.prefix, this.bp.bvhRootNode,true); // true: first

        this.bvhAnimator.transform.position = bvhAnimatorPosition;
        this.bvhAnimator.transform.rotation = bvhAnimatorRotation;

        this.clip.EnsureQuaternionContinuity();

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

        this.clipName = "speechGesture"; //' the string name of the AnimationClip
        if (this.clipName != "")
         {
             this.clip.name = this.clipName;
         }

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
       this.SetClipAtRunTime(this.bvhAnimator, this.clipName, this.clip );
//https://www.telerik.com/blogs/implementing-indexers-in-c#:~:text=To%20declare%20an%20indexer%20for,value%20from%20the%20object%20instance.
        
    } // public void loadAnimation()

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

   void ChangeClipAtRunTime(Animator anim, string currentClipName, AnimationClip clip ){
    //Animator anim = GetComponent<Animator>(); 

    AnimatorOverrideController overrideController =    new AnimatorOverrideController();

    // overriderController has the following indexer:
    // public AnimationClip this[string name] { get; set; }
    // public AnimationClip this[AnimationClip clip] { get; set; }

    AnimatorStateInfo[] layerInfo = new AnimatorStateInfo[anim.layerCount];
    for (int i = 0; i < anim.layerCount; i++)
    {
        layerInfo[i] = anim.GetCurrentAnimatorStateInfo(i);
    }

    overrideController.runtimeAnimatorController = anim.runtimeAnimatorController;

    overrideController[currentClipName] = clip;

    anim.runtimeAnimatorController = overrideController;

    // Force an update: Disable Animator component and then update it via API.?
    // Animator.Update() 와 Monobehaviour.Update() 간의 관계: https://m.blog.naver.com/PostView.naver?isHttpsRedirect=true&blogId=1mi2&logNo=220928872232
     // https://gamedev.stackexchange.com/questions/197869/what-is-animator-updatefloat-deltatime-doing
     // => Animator.Update() is a function that you can call to step the animator forward by the given interval.
    anim.Update(0.0f); // Update(Time.deltaTime): Animation control: https://chowdera.com/2021/08/20210823014846793k.html
    //=>  //  Record each frame
    //        animator.Update( 1.0f / frameRate);
    //=> You can pass the elapsed time by which it updates, and passing zero works as expected - it updates to the first frame of the first animation state.
    // The game logic vs animation logic: https://docs.unity3d.com/Manual/ExecutionOrder.html
    // https://forum.unity.com/threads/forcing-animator-update.381881/#post-3045779
    // Animation time scale: https://www.youtube.com/watch?v=4huKeRgEr4k
    // Push back state
    for (int i = 0; i < anim.layerCount; i++)
    {
        anim.Play(layerInfo[i].fullPathHash, i, layerInfo[i].normalizedTime);
    }
    //currentClipName = clip.name;
  } // void ChangeClip

    public void mapBvhBoneNamesToUnityBoneNames()
    {

        HumanBodyBones[] bones = (HumanBodyBones[])Enum.GetValues(typeof(HumanBodyBones));
        // bvhToUnityRenamingMapArray 
        this.bvhToUnityRenamingMapArray = new BVHAnimationLoader.FakeDictionary[bones.Length - 1];
        for (int i = 0; i < bones.Length - 1; i++)
        {
            if (bones[i] != HumanBodyBones.LastBone)
            {
                this.bvhToUnityRenamingMapArray[i].bvhName = "";
                this.bvhToUnityRenamingMapArray[i].targetName = bones[i].ToString();
            }
        }

    }
    // This function doesn't call any Unity API functions and should be safe to call from another thread
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

        //this.bvhAnimator.Play("BVHBehaviour");  // MJ: Animator.Play(string stateName); play a state stateName; Base Layer.Bounce, e.g.
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

        this.bvhAnimator = this.getbvhAnimator(); // Get Animator component of the virtual human to which this BVHAnimationLoader component is added
        // =>   this.bvhAnimator = this.GetComponent<Animator>();
        
        this.saraAnimator =  this.getSaraAnimator();
        this.saraAnimator.runtimeAnimatorController = this.bvhAnimator.runtimeAnimatorController;
        


    }

    void Update()
    {

        
        if (autoStart)
        {
           

           this.parseFile();

           this.loadAnimation();

           this.bvhAnimator.SetTrigger("ToBvh");

           this.saraAnimator.SetTrigger("ToBvh");

           

        }
    }
}
