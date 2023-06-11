
using UnityEngine;
using System.Collections.Generic;

//     public interface IRecordable
//     {
//         void TakeSnapshot(float deltaTime); // ==> time += deltaTime

//         AnimationClip GetClip { get; } // Create an animation clip from the animation data
//     }
public class HumanoidRecorder
{
    float time = 0;

    BvhSkeleton bvhSkeleton = new BvhSkeleton();

    HumanPoseHandler humanPoseHandler;
    public HumanPose humanPose = new HumanPose();

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
    //HumanPose humanPose;

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


    // public HumanoidRecorder(Animator animator, Transform rootTransform, HumanPoseHandler humanPoseHandler)

    public HumanoidRecorder(Animator animator, Transform rootTransform)
    {
        //this.rootOffset = animator.transform.position; // The position of the avatar root, Hips, relative to the world coordinate system.

         this.rootOffset  = rootTransform.position;

        // public HumanPoseHandler(Avatar avatar, Transform root);

        // Creates a human pose handler from an avatar and the root transform 
        
       // this.humanPoseHandler = new HumanPoseHandler(animator.avatar, animator.transform);
       // this.humanPoseHandler =humanPoseHandler; 

        Avatar avatar = animator.avatar;

        string[] muscleNames = HumanTrait.MuscleName; // 95 muscles
        // https://blog.unity.com/technology/mecanim-humanoids:
        // Humanoid Rigs don’t support twist bones, but Mecanim solver let you specify a percentage of twist to be taken out of the parent
        //  and put onto the child of the limb.It is defaulted at 50% and greatly helps to prevent skin deformation problem.
        string[] boneNames = HumanTrait.BoneName; // 55 human bones
        int boneCount = HumanTrait.BoneCount;



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



        // HumanBodyBones

        foreach (HumanBodyBones unityBoneId in bvhSkeleton.GetHumanBodyBones) // unityBoneType may be 55, Lastbone, which is not a bone.
                                                                              //for (int i=0; i < humanBones.Length; i++) // < humanBones.Length = 55, 3 of which (Left Eye, Right Eye, Jaw) do not correspond to bvh bones +>
                                                                              // The effective Human bones are 52. All of these human bones correspond to 52 bvh skeleton bones; 
                                                                              //                                             4 of which do not correspond to human bones => 52 = 56 -4
        {
            for (int dofIndex = 0; dofIndex < 3; dofIndex++) // 52 x 3 = (50 + 2) x 3 = 150 + 6 = 156
            {

                //   Obtain the muscle index for a particular bone index and "degree of freedom".
                // Parameters:   
                //  dofIndex:   Number representing a "degree of freedom": 0 for X-Axis, 1 for Y-Axis, 2 for    Z-Axis.
                // https://forum.unity.com/threads/problem-with-muscle-settings-in-humanoid-configuration.707714/
                int eachMuscle = HumanTrait.MuscleFromBone((int)unityBoneId, dofIndex); // Hips.0,.1,.2 = -1; spine.0 => 2

                if (eachMuscle != -1) // if the muscle is valid
                                      //this.muscleCurves is a  Dictionary<int, AnimationCurve>();
                    this.muscleCurves.Add(eachMuscle, new AnimationCurve()); // Generic Rig/Animation does not have use muscles: muscleCurves has 89 entries
            }
        }

        this.rootCurves.Add("RootT.x", new AnimationCurve());
        this.rootCurves.Add("RootT.y", new AnimationCurve());
        this.rootCurves.Add("RootT.z", new AnimationCurve());
    } // HumanoidRecoder(Animator animator, HumanBodyBones[] humanBodyBones)


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
    // ******************************************************************************
    //        void Update()
    //         {
    //             if (this.isRecording)
    //                 this.recordable.TakeSnapshot(Time.deltaTime);
    //         }

    public void SaveSnapshotAsKeys(HumanPoseHandler  humanPoseHandler, float deltaTime) // a method of Interface  IRecordable
    // public void SaveSnapshotAsKeys(float deltaTime) // a method of Interface  IRecordable
    {
        time += deltaTime;
       // Get the current human pose currently being updated.
       // Trying to read avatar skeleton transforms, but the HumanPoseHandler isn't bound to an avatar skeleton root transform
        // UnityEngine.HumanPoseHandler:GetHumanPose (UnityEngine.HumanPose&)
        //this.humanPoseHandler.GetHumanPose(ref this.humanPose); // https://forum.unity.com/threads/humanpose-issue-continued.484128/: humanPose.muscles[95]

        humanPoseHandler.GetHumanPose(ref this.humanPose);
       // public void GetInternalHumanPose(ref HumanPose humanPose);
        //this.humanPoseHandler.GetInternalHumanPose( ref this.humanPose);

        ////     void LateUpdate() {
        ////     handler.GetHumanPose(ref humanPose);
        ////     humanPose.bodyPosition = humanPose.bodyPosition.y * Vector3.up;
        ////     humanPose.bodyRotation = Quaternion.identity;
        ////     for (int i = 0; i < muscleIndices.Length; ++i)
        ////         humanPose.muscles[muscleIndices[i]] = values[i];
        ////     handler.SetHumanPose(ref humanPose);
        //// 

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



        foreach (KeyValuePair<int, AnimationCurve> data in this.muscleCurves) // muscleCurves[0...89]
        {
           Keyframe key = new Keyframe(time, this.humanPose.muscles[data.Key]);
          // Keyframe key = new Keyframe(time, humanPose.muscles[data.Key]);

            data.Value.AddKey(key); //   public int AddKey(Keyframe key); => fill muscleCurve data with key
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

       Vector3 rootPosition = this.humanPose.bodyPosition - this.rootOffset; // this.rootOffset = animator.transform.position;
       //  this.rootOffset  = rootTransform.position;

        this.AddRootKey("RootT.x", rootPosition.x);
        this.AddRootKey("RootT.y", rootPosition.y);
        this.AddRootKey("RootT.z", rootPosition.z);
    }

    void AddRootKey(string property, float value)
    {
        Keyframe key = new Keyframe(time, value);
        this.rootCurves[property].AddKey(key);
    }

    public AnimationClip GetClip() // a method of  Interface IRecordable
    {
      
        
            AnimationClip clip = new AnimationClip();

            foreach (KeyValuePair<int, AnimationCurve> data in this.muscleCurves)
            {
                clip.SetCurve("", typeof(Animator), HumanTrait.MuscleName[data.Key], data.Value);

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
            foreach (KeyValuePair<string, AnimationCurve> data in this.rootCurves)
            {
                clip.SetCurve("", typeof(Animator), data.Key, data.Value); // data.key = string, data.Value=AnimationCurve
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



            return clip;
        
    } //  public AnimationClip GetClip // a method of  Interface IRecordable

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
