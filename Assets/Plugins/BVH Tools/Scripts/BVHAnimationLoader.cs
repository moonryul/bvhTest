using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class BVHAnimationLoader : MonoBehaviour {
    [Header("Loader settings")]
    [Tooltip("This is the target avatar for which the animation should be loaded. **Bone names should be identical to those in the BVH file and unique**(??). All bones should be initialized with zero rotations. This is usually the case for VRM avatars.")]
    public Animator targetAvatar; // the default value of reference variable is null

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
    public FakeDictionary[] boneRenamingMap = null; // This dictionary may be filled by BVHAnimationLoaderEditor.cs

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
    private Transform rootBone;
    private string prefix;
    private int frames;
    private Dictionary<string, string> pathToBone;  // the default value of reference variable is null
    private Dictionary<string, string[]> boneToMuscles;  // the default value of reference variable is null
    private Dictionary<string, Transform> boneToTransformMap; // the default value of reference variable is null
    private Dictionary<string, string> renamingMap;  // the default value of reference variable is null

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


    // private Dictionary<string, Transform> boneToTransformMap;
    // private Dictionary<string, string> renamingMap;

   // Get the transform with name within the hiearchy rooted by transform; Return transform itself or one of its children
    private Transform getBoneByName(string name, Transform transform, bool first) {
        string targetName = standardName(name);
        if (this.renamingMap.ContainsKey(targetName)) {
            targetName = standardName(renamingMap[targetName]);
        }

        if (first) { // check if transform itself is referred to by name.
            if (standardName(transform.name) == targetName) {
                return transform;
            }
            if (this.boneToTransformMap.ContainsKey(targetName) && this.boneToTransformMap[targetName] == transform) {
                return transform;
            }
        }
        // The above two conditions failed to be met:

        for (int i = 0; i < transform.childCount; i++) {
            Transform child = transform.GetChild(i);
            if (standardName(child.name) == targetName) {
                return child;
            }
            if (this.boneToTransformMap.ContainsKey(targetName) && this.boneToTransformMap[targetName] == child) { // targetName is a Unity bone name
                return child;
            }
        }
        // None of the return statements are encountered. Then it means that an error has occurred:
        throw new InvalidOperationException("Could not find bone \"" + name + "\" under bone \"" + transform.name + "\".");
    }

    //    this.getCurves(this.prefix, this.bp.root, this.rootBone, true); // true = first
    private void getCurves(string path, BVHParser.BVHBone bvhRootNode, Transform rootBone, bool first) {
        bool posX = false;
        bool posY = false;
        bool posZ = false;
        bool rotX = false;
        bool rotY = false;
        bool rotZ = false;

        float[][] values = new float[6][];

        Keyframe[][] keyframes = new Keyframe[7][];

        string[] props = new string[7];

//   Get the transform with "bvhRoot.name"  within the hiearchy rooted by "rootBone";
//   Return "rootTBone" itself or one of its children

        Transform bvhRootTransform = getBoneByName(bvhRootNode.name, rootBone, first);

        if (path != this.prefix) {
            path += "/";
        }
        // In our experiment, path = this.prefix
        if (this.rootBone != this.targetAvatar.transform || !first) {
            path += bvhRootTransform.name;
        }
        // In our experiment, this.rootBone == this.targetAvatar.transform

        // This needs to be changed to gather from all channels into two vector3, invert the coordinate system transformation and then make keyframes from it
        for (int channel = 0; channel < 6; channel++) {
            if (!bvhRootNode.channels_bvhBones[channel].enabled) {
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
            values[channel] = bvhRootNode.channels_bvhBones[channel].values; // the animation key frames

            if (rotX && rotY && rotZ && keyframes[6] == null) {
                keyframes[6] = new Keyframe[frames];
                props[6] = "localRotation.w";
            }
        }

        float time = 0f;

        if (posX && posY && posZ) {
            Vector3 offset;
            if (this.blender) {
                offset = new Vector3(-bvhRootNode.offsetX, bvhRootNode.offsetZ, -bvhRootNode.offsetY);
            } else {
                offset = new Vector3(-bvhRootNode.offsetX, bvhRootNode.offsetY, bvhRootNode.offsetZ);
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
                    Vector3 bvhPosition = rootBone.transform.parent.InverseTransformPoint(new Vector3(keyframes[0][i].value, keyframes[1][i].value, keyframes[2][i].value) + targetAvatar.transform.position + offset);
                    keyframes[0][i].value = bvhPosition.x * this.targetAvatar.transform.localScale.x;
                    keyframes[1][i].value = bvhPosition.y * this.targetAvatar.transform.localScale.y;
                    keyframes[2][i].value = bvhPosition.z * this.targetAvatar.transform.localScale.z;
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
            Quaternion oldRotation = rootBone.transform.rotation;

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
                if (first) {
                    rootBone.transform.rotation = new Quaternion(keyframes[3][i].value, keyframes[4][i].value, keyframes[5][i].value, keyframes[6][i].value);
                    keyframes[3][i].value = rootBone.transform.localRotation.x;
                    keyframes[4][i].value = rootBone.transform.localRotation.y;
                    keyframes[5][i].value = rootBone.transform.localRotation.z;
                    keyframes[6][i].value = rootBone.transform.localRotation.w;
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

            rootBone.transform.rotation = oldRotation;

            //public void SetCurve(string relativePath, Type type, string propertyName, AnimationCurve curve);
            // https://docs.unity3d.com/ScriptReference/AnimationClip.SetCurve.html
            // The root node of path is the game object to which AnimationClip component is attached, and
            // the tip of the path is the game object to which Animation Curive will be applied.
            clip.SetCurve(path, typeof(Transform), props[3], new AnimationCurve(keyframes[3])); // Material as well as Transform can be animated
            clip.SetCurve(path, typeof(Transform), props[4], new AnimationCurve(keyframes[4]));
            clip.SetCurve(path, typeof(Transform), props[5], new AnimationCurve(keyframes[5]));
            clip.SetCurve(path, typeof(Transform), props[6], new AnimationCurve(keyframes[6]));
        } // if (rotX && rotY && rotZ) 

        //Define the animation curves for each child bone of the root bone.
        foreach (BVHParser.BVHBone child in bvhRootNode.children) {
            this.getCurves(path, child, bvhRootTransform
            , false); // getCurves() is called recursively.
        }

    } // private void getCurves(string path, BVHParser.BVHBone bvhRootNode, Transform rootBone, bool first)

    public static string getPathBetween(Transform target, Transform root, bool skipFirst, bool skipLast) {
        if (root == target) {
            if (skipLast) {
                return "";
            } else {
                return root.name;
            }
        }
        // root node is not target node
        for (int i = 0; i < root.childCount; i++) {
            Transform child = root.GetChild(i);

            if (target.IsChildOf(child)) {

                if (skipFirst) {
                    return getPathBetween(target, child, false, skipLast);
                } else {
                    return root.name + "/" + getPathBetween(target, child, false, skipLast);
                }
            }
        }

        throw new InvalidOperationException("No path between transforms " + target.name + " and " + root.name + " found.");
    }

    private void getTargetAvatar() {
        if (this.targetAvatar == null) {
            this.targetAvatar = this.GetComponent<Animator>();
        }
        if (this.targetAvatar == null) {
            throw new InvalidOperationException("No target avatar set.");
        }

    }

    // private Dictionary<string, Transform> boneToTransformMap; // null initially
    // private Dictionary<string, string> renamingMap;
	public void loadAnimation() {
        this.getTargetAvatar(); // Get Animator component of the virtual human to which this BVHAnimationLoader component is added
        // =>   this.targetAvatar = this.GetComponent<Animator>();

        if (this.bp == null) {
            throw new InvalidOperationException("No BVH file has been parsed.");
        }

        if (this.boneToTransformMap == null) {

            if (standardBoneNames) {
                Dictionary<Transform, string> transformToBoneMapUnityAvatar; // Transform: string

                BVHRecorder.populateBoneMap(out transformToBoneMapUnityAvatar, targetAvatar);
                // switch { transform: boneName} to { boneName: transform}
                this.boneToTransformMap = transformToBoneMapUnityAvatar.ToDictionary(kp => standardName(kp.Value), kp => kp.Key); // switch the order of Transform and string
            } else {
                this.boneToTransformMap = new Dictionary<string, Transform>(); // create an empty boneToTransformMap dictionary
            }
        }
        // The map that maps  vbh bone names to unity bone names
        renamingMap = new Dictionary<string, string>(); // create an empty dict ={  bvh bone name : unity bone name }

        // Create an mapping from bvh bone names to the target bone names used in Unity
        foreach (FakeDictionary entry in this.boneRenamingMap) {

            if (entry.bvhName != "" && entry.targetName != "") {

                renamingMap.Add(standardName(entry.bvhName), standardName(entry.targetName));
            }
        }
        // if this.boneRenamingMap is not created by the user in Inspector, then renamingMap will be null.

        // renamingMap == this.boneRenamingMap

        Queue<Transform> transformsUnityAvatar = new Queue<Transform>();

        transformsUnityAvatar.Enqueue(targetAvatar.transform); // add the root transform of the avatar to transforms queue

        string rootBoneNameBvh = standardName(this.bp.root.name); // The root bvh bone name
             
        // renamingMap.ContainsKey(rootBoneNameBvh) is false when
        // this.boneRenamingMap is not created by the user; 
        // Check if the root bone name from bvh file is mapped to a Unity standard bone name
        if (renamingMap.ContainsKey(rootBoneNameBvh)) { 
            rootBoneNameBvh = standardName(renamingMap[rootBoneNameBvh]); // get the unity root bone name
        }
        // If there is no unity name mapped to bvh bone name, the bvh bone name will be used. If this name 
        // is not found in the actual avatar used in Unity scene, an error message will be thrown.
        
        while (transformsUnityAvatar.Any()) {

            Transform rootTransformAvatar = transformsUnityAvatar.Dequeue(); // get the transform from the queue
            // Transform.name is the name of the game object to which Transform component is attached.
            // if the root bone of bvh hierarchy is the same as the root bone of the Unity avatar character, use this bone as this.rootbone
            if (standardName(rootTransformAvatar.name) == rootBoneNameBvh) { // check if the transform from the queue is rootBoneNameUnity
                this.rootBone = rootTransformAvatar;
                break;
            }
            //  the root bone of bvh hierarchy is NOT  the same as the root bone of the Unity avatar character;
            // Check if rootBoneNameBvh is euqal to some bone in the Unity avatar, use this bone as this.rootbone
            if (this.boneToTransformMap.ContainsKey( rootBoneNameBvh) && this.boneToTransformMap[ rootBoneNameBvh] == rootTransformAvatar) {
                this.rootBone = rootTransformAvatar;
                break;
            }

            // Otherwise, add the children nodes of the  rootTransformAvatar to the queue, in order to 
            // check if the root node of the bvh hierarchy is equal to  some child node of the Unity avatar;
            // It means that the bvh file contains the motion only for the parts of the avatar body, e.g. the upper body.
            for (int i = 0; i < rootTransformAvatar.childCount; i++) {
                transformsUnityAvatar.Enqueue(rootTransformAvatar.GetChild(i));
            }
        } // while

        // When the control reaches here, it means that the root bvh bone is NOT found in the Unity avatar;
        // THis is an error.
        // if (this.rootBone == null) { // Use 
        //     this.rootBone = BVHRecorder.getRootBone(targetAvatar);
        //     Debug.LogWarning("Using \"" + this.rootBone.name + "\" as the root bone.");
        // }
        // The rootBone was not identified so far:
        if (this.rootBone == null) {
            throw new InvalidOperationException("No root bone \"" + bp.root.name + "\" found." );
        }

        this.frames = this.bp.frames;

        this.clip = new AnimationClip();

        this.clip.name = "BVHClip (" + (clipCount++) + ")"; // clipCount is static
        if (this.clipName != "") {
            this.clip.name = this.clipName;
        }
        this.clip.legacy = true;

        //public static string getPathBetween(Transform target, Transform root, bool skipFirst, bool skipLast) 
        // Get the"prefix" path of nodes from root "this.targetAvatar.transform" to target "this.rootBone", which is the
        // root node of the bvh hierarchy. This prefix path is not controlled by the bvh motion data.

        // RelativePath for AnimationClip.SetCurve: https://forum.unity.com/threads/help-i-dont-get-it-animationclip-setcurve-relativepath.168050/

        this.prefix = getPathBetween(this.rootBone, this.targetAvatar.transform, true, true);

        // Save the root transform of the Unity avatar
        Vector3 targetAvatarPosition = this.targetAvatar.transform.position;
        Quaternion targetAvatarRotation = this.targetAvatar.transform.rotation;

        // Set the identity transform to the root node of the Unity Avatar
        this.targetAvatar.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
        this.targetAvatar.transform.rotation = Quaternion.identity;
        
        // 	Creates an animation curve from an arbitrary number of keyframes.
        // this.bp.root contains the bvh motion data to used to define Animation frames.
        this.getCurves(this.prefix, this.bp.root, this.rootBone, true); // true: first
        
        this.targetAvatar.transform.position = targetAvatarPosition;
        this.targetAvatar.transform.rotation = targetAvatarRotation;
        
        clip.EnsureQuaternionContinuity();
        if (this.anim == null) { // null by default
            this.anim = this.targetAvatar.gameObject.GetComponent<Animation>();
            if (this.anim == null) {
                this.anim = this.targetAvatar.gameObject.AddComponent<Animation>();
            }
        }
        this.anim.AddClip(this.clip, this.clip.name);
        this.anim.clip = this.clip;
        this.anim.playAutomatically = this.autoPlay;

        if (this.autoPlay) {
            this.anim.Play(this.clip.name);
        }
    } // public void loadAnimation()

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
            this.parseFile();
            this.loadAnimation();
        }
    }
}
