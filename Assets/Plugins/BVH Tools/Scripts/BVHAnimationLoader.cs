using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class BVHAnimationLoader : MonoBehaviour {
    [Header("Loader settings")]
    [Tooltip("This is the target avatar for which the animation should be loaded. **Bone names should be identical to those in the BVH file and unique**(??). All bones should be initialized with zero rotations. This is usually the case for VRM avatars.")]
    public Animator targetAnimator; // the default value of reference variable is null

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
    public struct FakeDictionary {
        public string bvhName;
        public string targetName;
    }

    // BVH to Unity
    private Quaternion fromEulerZXY(Vector3 euler) {
        return Quaternion.AngleAxis(euler.z, Vector3.forward) * Quaternion.AngleAxis(euler.x, Vector3.right) * Quaternion.AngleAxis(euler.y, Vector3.up);
    }

    private float wrapAngle(float a) {
        if (a > 180f) {
            return a - 360f;
        }
        if (a < -180f) {
            return 360f + a;
        }
        return a;
    }

    private string standardName(string name) {
        if (!this.flexibleBoneNames) {
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

    private Transform getBoneTransformByName(string bvhName, Transform avatarNodeTransform, bool first) {
         // first = true means that bvhName is the root node
        string bvhNameStandard = standardName(bvhName);
        string unityBoneName;

        if (this.bvhToUnityRenamingMap.ContainsKey(bvhNameStandard)) {
            unityBoneName = standardName( bvhToUnityRenamingMap[bvhNameStandard]);
        }
        else {
            // BvhUnityRenamingMap is not defined. 
             throw new InvalidOperationException("BvhUnityRenamingMap is not defined");
        } 

        if (first) { // check if the bhvNode is the root node of the hiearachy
           // if (standardName(avatarNodeTransform.name) == unityBoneName) {
           //     return avatarNodeTransform;
           // }
            if (this.UnityBoneToTransformMap.ContainsKey(unityBoneName) && this.UnityBoneToTransformMap[unityBoneName] == avatarNodeTransform) {
                return avatarNodeTransform;
            }
        }
        // The above two conditions failed to be met: Try to find bvhName among the children of avatarNodeTransform

        for (int i = 0; i < avatarNodeTransform.childCount; i++) {
            Transform child = avatarNodeTransform.GetChild(i);
            //if (standardName(child.name) == unityBoneName) {
            //    return child;
            //}
            if (this.UnityBoneToTransformMap.ContainsKey(unityBoneName) && this.UnityBoneToTransformMap[unityBoneName] == child) { // targetName is a Unity bone name
                return child;
            }
        }
        // None of the return statements are encountered. Then it means that an error has occurred:
        throw new InvalidOperationException("Could not find BVH bone \"" + bvhName + "\" under/below AVATAR bone \"" + avatarNodeTransform.name + "\".");
    }

    //private void getCurves(string path, BVHParser.BVHBone bvhNode, bool first) {

    //this.getCurves(this.prefix, this.bp.bvhRootNode, this.bvhRootTransform, true) when first called. 
    private void getCurves(string path, BVHParser.BVHBone bvhNode, Transform avatarNodeTransform, bool first) { 
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

        Transform bvhNodeTransform = getBoneTransformByName(bvhNode.name, avatarNodeTransform, first);

        if (path != this.prefix) { // getCurve() is first called with path == this.prefix;   // this.prefix has the form of root.name + "/"
            path += "/";
        }
        // In our experiment, path = this.prefix
        if (this.bvhRootTransform != this.targetAnimator.transform || !first) {
            path += bvhNodeTransform.name;
        }
        // In our experiment, this.rootBoneTransform == this.targetAnimator.transform

        // This needs to be changed to gather from all channels into two vector3, invert the coordinate system transformation and then make keyframes from it
        for (int channel = 0; channel < 6; channel++) {
            if (!bvhNode.channels_bvhBones[channel].enabled) {
                continue;
            }

            switch (channel) {
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
            if (channel == -1) {
                continue;
            }

            keyframes[channel] = new Keyframe[frames];
            values[channel] = bvhNode.channels_bvhBones[channel].values; // the animation key frames

            if (rotX && rotY && rotZ && keyframes[6] == null) {
                keyframes[6] = new Keyframe[frames];
                props[6] = "localRotation.w";
            }
        }

        float time = 0f;

        if (posX && posY && posZ) {
            Vector3 offset;
            if (this.blender) {
                offset = new Vector3(-bvhNode.offsetX, bvhNode.offsetZ, -bvhNode.offsetY);
            } else {
                offset = new Vector3(-bvhNode.offsetX, bvhNode.offsetY, bvhNode.offsetZ);
            }
            for (int i = 0; i < this.frames; i++) {
                time += 1f / this.frameRate;
                keyframes[0][i].time = time;
                keyframes[1][i].time = time;
                keyframes[2][i].time = time;
                if (blender) {
                    keyframes[0][i].value = -values[0][i];
                    keyframes[1][i].value = values[2][i];
                    keyframes[2][i].value = -values[1][i];
                } else {
                    keyframes[0][i].value = -values[0][i];
                    keyframes[1][i].value = values[1][i];
                    keyframes[2][i].value = values[2][i];
                }
                if (first) {
                    Vector3 bvhPosition = avatarNodeTransform.transform.parent.InverseTransformPoint(new Vector3(keyframes[0][i].value, keyframes[1][i].value, keyframes[2][i].value) + targetAnimator.transform.position + offset);
                    keyframes[0][i].value = bvhPosition.x * this.targetAnimator.transform.localScale.x;
                    keyframes[1][i].value = bvhPosition.y * this.targetAnimator.transform.localScale.y;
                    keyframes[2][i].value = bvhPosition.z * this.targetAnimator.transform.localScale.z;
                }
            }
            if (first) {
                this.clip.SetCurve(path, typeof(Transform), props[0], new AnimationCurve(keyframes[0]));
                this.clip.SetCurve(path, typeof(Transform), props[1], new AnimationCurve(keyframes[1]));
                this.clip.SetCurve(path, typeof(Transform), props[2], new AnimationCurve(keyframes[2]));
            } else {
                Debug.LogWarning("Position information on bones other than the root bone is currently not supported and has been ignored. If you exported this file from Blender, please tick the \"Root Translation Only\" option next time.");
            }
        }

        time = 0f;
        if (rotX && rotY && rotZ) {
            Quaternion oldRotation = avatarNodeTransform.transform.rotation;

            for (int i = 0; i < frames; i++) {

                Vector3 eulerBVH = new Vector3(wrapAngle(values[3][i]), wrapAngle(values[4][i]), wrapAngle(values[5][i]));
                Quaternion rot = fromEulerZXY(eulerBVH);
                if (blender) {
                    keyframes[3][i].value = rot.x;
                    keyframes[4][i].value = -rot.z;
                    keyframes[5][i].value = rot.y;
                    keyframes[6][i].value = rot.w;
                    //rot2 = new Quaternion(rot.x, -rot.z, rot.y, rot.w);
                } else {
                    keyframes[3][i].value = rot.x;
                    keyframes[4][i].value = -rot.y;
                    keyframes[5][i].value = -rot.z;
                    keyframes[6][i].value = rot.w;
                    //rot2 = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
                }
                if (first) { // first == true means that avatarNodeTransform is the root node
                    avatarNodeTransform.transform.rotation = new Quaternion(keyframes[3][i].value, keyframes[4][i].value, keyframes[5][i].value, keyframes[6][i].value);
                    keyframes[3][i].value = avatarNodeTransform.transform.localRotation.x;
                    keyframes[4][i].value = avatarNodeTransform.transform.localRotation.y;
                    keyframes[5][i].value = avatarNodeTransform.transform.localRotation.z;
                    keyframes[6][i].value = avatarNodeTransform.transform.localRotation.w;
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

            avatarNodeTransform.transform.rotation = oldRotation;

            //public void SetCurve(string relativePath, Type type, string propertyName, AnimationCurve curve);
            // https://docs.unity3d.com/ScriptReference/AnimationClip.SetCurve.html
            // The root node of path is the game object to which AnimationClip component is attached, and
            // the tip of the path is the game object to which Animation Curive will be applied.
            clip.SetCurve(path, typeof(Transform), props[3], new AnimationCurve(keyframes[3])); // Material as well as Transform can be animated
            clip.SetCurve(path, typeof(Transform), props[4], new AnimationCurve(keyframes[4]));
            clip.SetCurve(path, typeof(Transform), props[5], new AnimationCurve(keyframes[5]));
            clip.SetCurve(path, typeof(Transform), props[6], new AnimationCurve(keyframes[6]));
        } // if (rotX && rotY && rotZ) 

        // Call getCurves() recursively.
        // Define the animation curves for each child bone of the root bone.

        foreach (BVHParser.BVHBone child in bvhNode.children) {
            this.getCurves(path, child,bvhNodeTransform,false);
        }

    } // private void getCurves(string path, BVHParser.BVHBone bvhRootNode, Transform rootBoneTransform, bool first)

 // first call: this.prefix = getPathBetween(this.bvhRootTransform, this.targetAnimator.transform, true, true);
    public static string getPathBetween(Transform target, Transform root, bool skipFirst, bool skipLast) {
        if (root == target) {
            if (skipLast) {
                return "";
            } else {
                return root.name; // tye same as target.name
            }
        }
        // The target transform is a child of the root transform
        for (int i = 0; i < root.childCount; i++) {
            Transform child = root.GetChild(i);

            if (target.IsChildOf(child)) {

                if (skipFirst) { // skipFirst means skip "Genesis8Male" under "Avatar"
                    return getPathBetween(target, child, false, skipLast); // Find the path from child to target, with skipFirst = false
                    // root = avatar, target = hip; skipFirst = false
                } else {
                    return root.name + "/" + getPathBetween(target, child, false, skipLast);
                     // root = Genesis8Male;   target = hip, child  = hip;  getPathBetween(target, child, false, skipLast) =""
                }
            }
        }

        throw new InvalidOperationException("No path between transforms " + target.name + " and " + root.name + " found.");
    }

    private void getTargetAnimator() {
        if (this.targetAnimator == null) {
            this.targetAnimator = this.GetComponent<Animator>();
             // In our seeting, Animator component is not added to bvh to which BVHAnimationLoader component (this) is added.
             //So, this.targetAnimator should be set in the inspector.
        }
                                                                
        if (this.targetAnimator == null) {
            throw new InvalidOperationException("No target avatar set.");
        }

    }

    // private Dictionary<string, Transform> UnityBoneToTransformMap; // null initially
    // private Dictionary<string, string> BvhToUnityRenamingMap;
	public void loadAnimation() {
        this.getTargetAnimator(); // Get Animator component of the virtual human to which this BVHAnimationLoader component is added
        // =>   this.targetAnimator = this.GetComponent<Animator>();

        if (this.bp == null) {
            throw new InvalidOperationException("No BVH file has been parsed.");
        }

        // Get the transforms for the bones of the Unity Avatar imported from say Daz3D.
        if (this.UnityBoneToTransformMap == null) {

            if (standardBoneNames) {
                Dictionary<Transform, string> transformToBoneMapUnityAvatar; // Transform: string

                BVHRecorder.populateBoneMap(out transformToBoneMapUnityAvatar, targetAnimator);
                // switch { transform: boneName} to { boneName: transform}
                this.UnityBoneToTransformMap = transformToBoneMapUnityAvatar.ToDictionary(kp => standardName(kp.Value), kp => kp.Key); // switch the order of Transform and string
            } else {
                this.UnityBoneToTransformMap = new Dictionary<string, Transform>(); // create an empty UnityBoneToTransformMap dictionary
            }
        }

       // Use the 
       
        // Get the map that maps  vbh bone names to unity bone names
        // this.bp.boneList contains the list of bvh bones; this.bp.boneList[i].name is the bvh bone name
        this.bvhToUnityRenamingMap = new Dictionary<string, string>(); // create an empty dict ={  bvh bone name : unity bone name }

        // Create an mapping from bvh bone names to the target bone names used in Unity
        foreach (FakeDictionary entry in this.bvhToUnityRenamingMapArray) {

            if (entry.bvhName != "" && entry.targetName != "") {

                bvhToUnityRenamingMap.Add(standardName(entry.bvhName), standardName(entry.targetName));
            }
        }
        // if this.boneBvhToUnityRenamingMap is not created by the user in Inspector, then BvhToUnityRenamingMap will be null.

        // BvhToUnityRenamingMap == this.boneBvhToUnityRenamingMap

        Queue<Transform> transformsInImportedAvatar = new Queue<Transform>();

        transformsInImportedAvatar.Enqueue(this.targetAnimator.transform); // add the root transform of the avatar to Unity transforms queue

        string bvhRootBoneBvhName = standardName(this.bp.bvhRootNode.name); // The root bvh bone name = "Hips"
        //MJ: 
        string bvhRootBoneInUnityName;

        // BvhToUnityRenamingMap.ContainsKey(rootBoneTransformNameBvh) is false when
        // this.boneBvhToUnityRenamingMap is not created by the user; 
        // Check if the root bone name from bvh file is mapped to a Unity standard bone name
        if (bvhToUnityRenamingMap.ContainsKey( bvhRootBoneBvhName )) { 
            bvhRootBoneInUnityName = standardName(bvhToUnityRenamingMap[ bvhRootBoneBvhName]); // get the unity root bone name
        }
        else {
        // If there is no unity name mapped to bvh bone name, this is an error:
           throw new InvalidOperationException("The BVH bone names to Unity Bone Names Mapping is not defined; It should be defined in Inspector" );
        }
        
        // Check if  rootBoneTransformNameUnity to used as the root of the bvh character in Unity corresponds to some bone in the "Avatar" hierarchy imported in the Unity scene, e.g., imported from Daz3D humanoid
        while (transformsInImportedAvatar.Any()) {

            Transform transformInImportedAvatar = transformsInImportedAvatar.Dequeue(); // get the transform from the queue
            // Transform.name is the name of the game object to which Transform component is attached.
            // if the root bone of bvh hierarchy is the same as the root bone of the Unity avatar character, use this bone as this.rootBoneTransform
            if (standardName( transformInImportedAvatar.name) ==  bvhRootBoneInUnityName) { // check if the transform from the queue is rootBoneTransformNameUnity

                this.bvhRootTransform = transformInImportedAvatar;
                break;
            }
           
            // Otherwise, add the children nodes of the  rootTransformAvatar to the queue, in order to 
            // check if the root node of the bvh hierarchy is equal to  some child node of the Unity avatar;
            // It means that the bvh file contains the motion only for the parts of the avatar body, e.g. the upper body.
            for (int i = 0; i < transformInImportedAvatar.childCount; i++) {
                transformsInImportedAvatar.Enqueue( transformInImportedAvatar.GetChild(i));
            }
        } // while

        // When the control reaches here, it means that the root bvh bone is NOT found in the Unity avatar;
        // THis is an error.
        // if (this.rootBoneTransform == null) { // Use 
        //     this.rootBoneTransform = BVHRecorder.getrootBoneTransform(targetAnimator);
        //     Debug.LogWarning("Using \"" + this.rootBoneTransform.name + "\" as the root bone.");
        // }
        // The rootBoneTransform was not identified so far:
        if (this.bvhRootTransform == null) {
            throw new InvalidOperationException("No root bone \"" + bp.bvhRootNode.name + "\" found." );
        }

        this.frames = this.bp.frames;

        this.clip = new AnimationClip();

        this.clip.name = "BVHClip (" + (clipCount++) + ")"; // clipCount is static
        if (this.clipName != "") {
            this.clip.name = this.clipName;
        }
        this.clip.legacy = true;

        //public static string getPathBetween(Transform target, Transform root, bool skipFirst, bool skipLast) 
        // Get the"prefix" path of nodes from root "this.targetAnimator.transform" to target "this.rootBoneTransform", which is the
        // root node of the bvh hierarchy. This prefix path is not controlled by the bvh motion data.

        // RelativePath for AnimationClip.SetCurve: https://forum.unity.com/threads/help-i-dont-get-it-animationclip-setcurve-relativepath.168050/
        //   public static string getPathBetween(Transform target, Transform root, bool skipFirst, bool skipLast)
        // Get the prefix which consists of bones without the first and the last bone
        this.prefix = getPathBetween(this.bvhRootTransform, this.targetAnimator.transform, true, true);
        // this.prefix has the form of root.name + "/"
         // this.targetAnimator.transform is the Transform component attached to this gameObject, where "this gameObject" is
         // the Avatar gameObject to which targetAnimator component is attached. This is not "hip"; hip is under this.targetAnimator.transform:
         // Avatar => Genesis8Male => hip => pelvis. This.prefix = Genesis8Male

        // Save the root transform of the Unity avatar
        Vector3 targetAnimatorPosition = this.targetAnimator.transform.position;
        Quaternion targetAnimatorRotation = this.targetAnimator.transform.rotation;

        // Set the identity transform to the root node of the Unity Avatar
        this.targetAnimator.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
        this.targetAnimator.transform.rotation = Quaternion.identity;
        
        // 	Creates an animation curve from an arbitrary number of keyframes.
        // this.bp.root contains the bvh motion data to used to define Animation frames.
        this.getCurves(this.prefix, this.bp.bvhRootNode, this.bvhRootTransform, true); // true: first
        //  this.bvhRootTransform is the transform of bp.bvhRootNode

        //this.getCurves(this.prefix, this.bp.bvhRootNode,true); // true: first
        
        this.targetAnimator.transform.position = targetAnimatorPosition;
        this.targetAnimator.transform.rotation = targetAnimatorRotation;
        
        clip.EnsureQuaternionContinuity();
        if (this.anim == null) { // null by default
            this.anim = this.targetAnimator.gameObject.GetComponent<Animation>();
            if (this.anim == null) {
                this.anim = this.targetAnimator.gameObject.AddComponent<Animation>();
            }
        }
        this.anim.AddClip(this.clip, this.clip.name);
        this.anim.clip = this.clip;
        this.anim.playAutomatically = this.autoPlay;

        if (this.autoPlay) {
            this.anim.Play(this.clip.name);
        }
    } // public void loadAnimation()

    public void mapBvhBoneNamesToUnityBoneNames() {

        HumanBodyBones[] bones = (HumanBodyBones[])Enum.GetValues(typeof(HumanBodyBones));
            // bvhToUnityRenamingMapArray 
            this.bvhToUnityRenamingMapArray = new BVHAnimationLoader.FakeDictionary[bones.Length - 1];
            for (int i = 0; i < bones.Length - 1; i++) {
                if (bones[i] != HumanBodyBones.LastBone) {
                    this.bvhToUnityRenamingMapArray[i].bvhName = "";
                    this.bvhToUnityRenamingMapArray[i].targetName = bones[i].ToString();
                }
            }

    }
    // This function doesn't call any Unity API functions and should be safe to call from another thread
    public void parse(string bvhData) {
        if (this.respectBVHTime) {
            this.bp = new BVHParser(bvhData);
            this.frameRate = 1f / this.bp.frameTime;
        } else {
            this.bp = new BVHParser(bvhData, 1f / this.frameRate); // this.bp.channels_bvhBones[].values will store the motion data
        }
    }

    // This function doesn't call any Unity API functions and should be safe to call from another thread
    public void parseFile() {
        this.parse(File.ReadAllText(this.filename));
    }

    public void playAnimation() {
        if (this.bp == null) {
            throw new InvalidOperationException("No BVH file has been parsed.");
        }
        if (anim == null || clip == null) {
            this.loadAnimation();
        }
        this.anim.Play(this.clip.name);
    }

    public void stopAnimation() {
        if (clip != null) {
            if (anim.IsPlaying(clip.name)) {
                anim.Stop();
            }
        }
    }

    void Start () {
        if (autoStart) {
            autoPlay = true;

            //this.mapBvhBoneNamesToUnityBoneNames(); 

            this.parseFile();      

            this.loadAnimation();
        }
    }
}
