using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor; // to use AssetDatabase


using Unity.Collections;


//public class Retargeting : MonoBehaviour
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

public class BVHAnimationRetargetter : MonoBehaviour
{
    public enum AnimType { Legacy, Generic, Humanoid };
    public AnimType animType = AnimType.Humanoid;

    //public List<Transform> bvhAvatarCurrentTransforms = new List<Transform>();


    public  HumanPose humanPose;
    BvhSkeleton bvhSkeleton = new BvhSkeleton();

    List<string> jointPaths = new List<string>(); // emtpy one

    HumanPoseHandler srcHumanPoseHandler;
    HumanPoseHandler destHumanPoseHandler;

    //NativeArray<float> avatarPose;

   // List<int> muscleIndecies = new List<int>();

    public GameObject skeletonGO = null; //null by default; skeletonGO is created in BVHFrameGetter; this is sett within Start() method of BVHAnimationRetargetter.

    public Animator bvhAnimator; // should be defined in the inspector
    public Animator saraAnimator; // should be defined in the inspector

    protected AnimatorOverrideController animatorOverrideController;


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


    private Animator getbvhAnimator()
    {

        if (this.bvhAnimator == null)
        {
            throw new InvalidOperationException("No Bvh Animator  set. Set it in Inspector");
          
            //GameObject avatarRootNode = this.skeletonGO.transform .GetChild(0).gameObject; //MJ: avatarRootNode  is "Hips"
            // GameObject avatarRootNode = this.skeletonGO; //MJ: this gameObject is "Skeleton"
            
            // HumanDescription humanDescription = new HumanDescription(); // struct type
            // this.bvhAnimator = this.gameObject.AddComponent<Animator>();

            // // This class allows you to create custom avatars for your animated characters entirely
            // // via script, in a similar way to what goes on behind the Scenes
            // // in the Unity Editor when you create an avatar from the model import inspector.
            // this.bvhAnimator.avatar = AvatarBuilder.BuildHumanAvatar( avatarRootNode, humanDescription);
            // //MJ: confer https://github.com/bkevelham/unity-avatar-generation

        /// Create a HumanDescription out of an avatar GameObject. 
		/// The HumanDescription is what is needed to create an Avatar object
		/// using the AvatarBuilder API. This function takes care of 
		/// creating the HumanDescription by going through the avatar's
		/// hierarchy, defining its T-Pose in the skeleton, and defining
		/// the transform/bone mapping in the HumanBone array. 
		/// </summary>
		/// <param name="avatarRoot">Root of your avatar object</param>
		/// <returns>A HumanDescription which can be fed to the AvatarBuilder API</returns>


            // MJ: error: required human bone "Hips" not found
            //=> UnityEngine.AvatarBuilder:BuildHumanAvatar (UnityEngine.GameObject,UnityEngine.HumanDescription)
            // Verify the bone hierarchy: Make sure that your GameObject has a proper bone hierarchy and that it includes the "Hips" bone. 
            //The "Hips" bone is a required bone for building a human avatar, so it must be present. 


            //AssetDatabase.CreateAsset(animator.avatar, "Assets/Path/To/Save/Avatar.asset");
           // Debug.Log($"Is avatar valid={this.bvhAnimator.avatar.isValid}");

            //return  this.bvhAnimator;
        }

        else
        {
            return this.bvhAnimator;
        }

    }

    private Animator getSaraAnimator()
    {

        if (this.saraAnimator == null)
        {
            throw new InvalidOperationException("No Sara Animator set. Set it in inspector");
        }

        else
        {
            return this.saraAnimator;
        }

    }


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

        //MJ: "this" refers to an object of class BVHAnimationRetargetter 


        // this.gameObject =  bvhRetargetter; It has two components: BVHAnimationRetargetter and bvhFrameGetter

        //this.skeletonGO = this.gameObject.GetComponent<BVHSkeletonCreator>().skeletonGO;

        this.skeletonGO = GameObject.FindGameObjectWithTag("Skeleton");

        if (this.skeletonGO == null)
        {

             Debug.Log(" BVH Skeleton Should have been created by BVHSkeletonCreator component and added to the hierarchy");
             throw new InvalidOperationException(" In BVHFrameGetter:Skeleton is not added to the hierarchy or Is not given Tag 'Skeleton'");

        }   

        if (this.animType == AnimType.Humanoid)
        {


            // Animator components of Skeleton and Sara should be added in the inspector by the user

            this.bvhAnimator = this.getbvhAnimator(); //MJ: this method uses  this.skeletonGO
            // Get Animator component of the virtual human to which this BVHAnimationLoader component is added
            // =>   this.bvhAnimator = this.gameObject.GetComponent<Animator>();

      
            if (!this.bvhAnimator.avatar.isValid)
            {

                throw new InvalidOperationException("this.bvhAnimator.avatar.isValid not true.");

            }


            this.saraAnimator = this.getSaraAnimator();
            // Set up Animator for Sara for retargetting motion from Skeleton to Sara

            if (!this.saraAnimator.avatar.isValid)
            {

                throw new InvalidOperationException("this.saraAnimator.avatar.isValid not true.");

            }

            this.humanPose = new HumanPose();

            this.srcHumanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAnimator.gameObject.transform);
            //MJ: 
            //(1) this.bvhAnimator is the Animator component attached to bvh "Skeleton" gameObject,
            //  which a hierarchy of gameObjects representing the hiearchy defined by the bvh file.

            // (2) this.bvhAnimator.gameObject == SkeletonGO:
            //  this.skeletonGO.transform.GetChild(0) refers to the root (Hip) of the bvh skeleton which is set by
            //  every frame by BVHFrameGetters component's FixedUpdate() or by AvatarController's Coroutine. 

            // (3) this.scrHumanPoseHandler has a reference to this.skeletonGO.transform, the root of the avatar object
            // and thereby the entire hierarchy of transforms;
            // (4) this.bvhAnimator.avatar refers to the definitino of the Unity avatar in relation to the bvh character.


            // (4) The transforms of the avatar hierarchy are changed by the FixedUpdate of BVHFrameGetter script;

            // (5) You can set the human pose defined by the human avatar hierarchy to humanPose by  this.srcHumanPoseHandler.SetHumanPose(ref humanPose), or
            // get the humanPose by this.srcHmanPoseHandler.SetHumanPose(ref humanPose),
            // where  HumanPose humanPose = new HumanPose();

            this.destHumanPoseHandler = new HumanPoseHandler(this.saraAnimator.avatar, this.saraAnimator.gameObject.transform);
            //MJ: this.saraAnimator.gameObject = Sara


            //MJ:  // Muscle name and index lookup for Debugging
            //this.LookUpHumanMuscleIndex();


        } //  if (this.animType == AnimType.Humanoid) 


        else //  // animType == AnimType.Legacy or animType == AnimType.Generic or other cases
        {
            throw new InvalidOperationException("Invalid Anim Type");
        }

    } // Start()

    
    void FixedUpdate()
    {
        if (animType == AnimType.Humanoid)
        {

            //this.srcHumanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAvatarRootTransform);
            //this.destHumanPoseHandler = new HumanPoseHandler(this.saraAnimator.avatar, this.saraAvatarRootTransform);

            //MJ: Compute a human pose from the bvh skeleton (bound to srcHumanPoseHandler), 
            // store the pose in the human pose handler, and return the human pose as the value ref humanPose.
            // The bvh skeleton pose is updated every frame by by BVHFrameGetter script.
            // HumanPose humanPose = new HumanPose();
            this.srcHumanPoseHandler.GetHumanPose(ref this.humanPose);
                        
            // => Computes a human pose* from the **bvh avatar skeleton**, stores the pose in the human pose handler, and returns it to humanPose.

            //MJ: note that:

            // (1) If the HumanPoseHander was constructed from an avatar and a root (which is the case in our code) 
            // the human pose is bound to the transform hierarchy representing the humanoid in the scene;
            // You can get the human pose by this.srcHumanPoseHandler.GetHumanPose(ref humanPose);
            //  
            // (2) If the HumanPoseHander was constructed from an avatar and jointPaths, as follows,
            //           the human pose is not bound to the transform hierarchy; 
            //    In this case, GetHumanPose returns the internally stored human pose as the output.
            // 

            //   this.humanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.jointPaths.ToArray());    
            //   this.jointPaths:  A list that defines the avatar joint paths. 
            //   Each joint path starts from the node after the root transform and continues down the avatar skeleton hierarchy.
            //   The root transform joint path is an empty string.

            // (3) https://docs.unity3d.com/ScriptReference/HumanPoseHandler-ctor.html:
            // Create the set of joint paths, this.jointPaths, and the corresponding transforms, this.avatarTransforms,  from this.bvhAvatarRoot
            // this.avatarPose = new NativeArray<float>(this.jointPaths.Count * 7, Allocator.Persistent);


            //Set the humanPose from the bvh motion to the Sara (destination) HumanPose:
            // humanPose is the common ground between the bvh skeleton and the Sara skeleton.

            this.destHumanPoseHandler.SetHumanPose(ref this.humanPose);



        } //  if (animType == AnimType.Humanoid) 

        else // animType == AnimType.Legacy or animType == AnimType.Generic or other cases
        {
            throw new InvalidOperationException("Invalid Anim Type");
        }


    } //  void Update()

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
