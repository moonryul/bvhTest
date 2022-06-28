
using UnityEngine;

using UnityEditor;
using System.Collections.Generic;




public class GenericRecorder
{
    float time = 0.0f;
    int jointPathsCount;

    List<ObjectAnimation> objectAnimations = new List<ObjectAnimation>();

    // Implicit Implementation of Interface Methods: https://www.tutorialsteacher.com/csharp/csharp-interface#:~:text=In%20C%23%2C%20an%20interface%20can,functionalities%20for%20the%20file%20operations.


    // public GenericRecorder(Transform rootTransform, List<string> jointPaths, Transform[] recordableTransforms )
   // public GenericRecorder(List<string> jointPaths, List<Transform> recordableTransforms)

    public GenericRecorder(List<string> jointPaths) // Create containers for the animation curves of each joint
    {
        // foreach (Transform transform in recordableTransform)
        this.jointPathsCount = jointPaths.Count;
        for (int i = 0; i < this.jointPathsCount; i++)
        {
            //string path = AnimationUtility.CalculateTransformPath(transform, rootTransform);
            string path = jointPaths[i];         

            this.objectAnimations.Add(new ObjectAnimation(path));
        }
    }

    public void SaveSnapshotAsKeys(float deltaTime, List<Transform> bvhTransforms) // defined in IRecordable; TakeSnapShots for all objects in the character
    {
        this.time += deltaTime;

        for (int i = 0; i < this.jointPathsCount; i++)
       // foreach (ObjectAnimation objAnimation in this.objectAnimations)
        {
            this.objectAnimations[i].SaveSnapshotAsKeys(this.time, bvhTransforms[i]);
        }
    }

    public AnimationClip GetClip()
    {

        AnimationClip clip = new AnimationClip(); // an animation clip for the character, the whole subpaths of the character

        foreach (ObjectAnimation objAnimation in this.objectAnimations) // animation for each joint, which is animation.Path
        {
            foreach (CurveContainer container in objAnimation.CurveContainers) // container for each DOF in animation of the current joint

            {
                if (container.Curve.keys.Length > 1)
                    clip.SetCurve( objAnimation.Path, typeof(Transform), container.Property, container.Curve);
            }
        }

        return clip;

    } //   public AnimationClip GetClip()
} // public class GenericRecorder

class ObjectAnimation
{
   // Transform transform;

    public List<CurveContainer> CurveContainers { get; private set; }

    public string Path { get; private set; }

   // public ObjectAnimation(string hierarchyPath, Transform recordableTransform)

     public ObjectAnimation(string hierarchyPath)
    {
        this.Path = hierarchyPath;
        //this.transform = recordableTransform; // reference type

        // check if this.Path is the root node or not
        if (this.Path == "")
        {
            // the root node

            this.CurveContainers = new List<CurveContainer>
            {
                new CurveContainer("localPosition.x"),
                new CurveContainer("localPosition.y"),
                new CurveContainer("localPosition.z"),

                new CurveContainer("localRotation.x"),
                new CurveContainer("localRotation.y"),
                new CurveContainer("localRotation.z"),
                new CurveContainer("localRotation.w")
            };
        }
        else
        {
            this.CurveContainers = new List<CurveContainer>
            {

                new CurveContainer("localRotation.x"),
                new CurveContainer("localRotation.y"),
                new CurveContainer("localRotation.z"),
                new CurveContainer("localRotation.w")
            };
        } //  if (this.Path == "")
    } //  public ObjectAnimation(string hierarchyPath, Transform recordableTransform)

    public void SaveSnapshotAsKeys(float time, Transform transform)
    {

        if (this.Path == "")
        {

            this.CurveContainers[0].AddValue(time, transform.localPosition.x); // this.CurveContainers[0].Property = "localPosition.x"
            this.CurveContainers[1].AddValue(time, transform.localPosition.y); // "localPosition.y"
            this.CurveContainers[2].AddValue(time, transform.localPosition.z);

            this.CurveContainers[3].AddValue(time, transform.localRotation.x);
            this.CurveContainers[4].AddValue(time, transform.localRotation.y);
            this.CurveContainers[5].AddValue(time, transform.localRotation.z);
            this.CurveContainers[6].AddValue(time, transform.localRotation.w); // "localRotation.w"
        }

        else
        {
            this.CurveContainers[0].AddValue(time, transform.localRotation.x);
            this.CurveContainers[1].AddValue(time, transform.localRotation.y);
            this.CurveContainers[2].AddValue(time, transform.localRotation.z);
            this.CurveContainers[3].AddValue(time, transform.localRotation.w); // "localRotation.w" 
        }
    }//public void TakeSnapshot(float time)

} // class ObjectAnimation

class CurveContainer
{
    public string Property { get; private set; }
    public AnimationCurve Curve { get; private set; }

    float? lastValue = null;

    public CurveContainer(string _propertyName)
    {
        this.Curve = new AnimationCurve();
        this.Property = _propertyName;
    }

    public void AddValue(float time, float value)
    {
        if (this.lastValue == null || !Mathf.Approximately((float)lastValue, value))
        {
            Keyframe key = new Keyframe(time, value);
            this.Curve.AddKey(key);
            this.lastValue = value;
        }
    }
} //    class CurveContainer

