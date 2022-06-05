
using UnityEngine;

using UnityEditor;
using System.Collections.Generic;




public class GenericRecorder
{
    float time = 0.0f;

    List<ObjectAnimation> objectAnimations = new List<ObjectAnimation>();

    // Implicit Implementation of Interface Methods: https://www.tutorialsteacher.com/csharp/csharp-interface#:~:text=In%20C%23%2C%20an%20interface%20can,functionalities%20for%20the%20file%20operations.


   // public GenericRecorder(Transform rootTransform, List<string> jointPaths, Transform[] recordableTransforms )
   public GenericRecorder( List<string> jointPaths, List<Transform> recordableTransforms )
    {
       // foreach (Transform transform in recordableTransform)
       for (int i=0; i < jointPaths.Count; i++)
        {
            //string path = AnimationUtility.CalculateTransformPath(transform, rootTransform);
            string path = jointPaths[i];

            Transform transform = recordableTransforms[i];

            this.objectAnimations.Add(new ObjectAnimation(path, transform));
        }
    }

    public void TakeSnapshot(float deltaTime) // defined in IRecordable; TakeSnapShots for all objects in the character
    {
        this.time += deltaTime;

        foreach (ObjectAnimation objAnimation in this.objectAnimations)
        {
            objAnimation.TakeSnapshot(this.time);
        }
    }

    public AnimationClip GetClip
    {
        get
        {
            AnimationClip clip = new AnimationClip(); // an animation clip for the character, the whole subpaths of the character

            foreach (ObjectAnimation animation in this.objectAnimations) // animation for each joint, which is animation.Path
            {
                foreach (CurveContainer container in animation.CurveContainers) // container for each DOF in animation of the current joint

                {
                    if (container.Curve.keys.Length > 1)
                        clip.SetCurve(animation.Path, typeof(Transform), container.Property, container.Curve);
                }
            }

            return clip;
        }
    }
}

class ObjectAnimation
{
    Transform transform;

    public List<CurveContainer> CurveContainers { get; private set; }

    public string Path { get; private set; }

    public ObjectAnimation(string hierarchyPath, Transform recordableTransform)
    {
        this.Path = hierarchyPath;
        this.transform = recordableTransform;

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

    public void TakeSnapshot(float time)
    {
        this.CurveContainers[0].AddValue(time, this.transform.localPosition.x); // this.CurveContainers[0].Property = "localPosition.x"
        this.CurveContainers[1].AddValue(time, this.transform.localPosition.y); // "localPosition.y"
        this.CurveContainers[2].AddValue(time, this.transform.localPosition.z);

        this.CurveContainers[3].AddValue(time, this.transform.localRotation.x);
        this.CurveContainers[4].AddValue(time, this.transform.localRotation.y);
        this.CurveContainers[5].AddValue(time, this.transform.localRotation.z);
        this.CurveContainers[6].AddValue(time, this.transform.localRotation.w); // "localRotation.w"
    }
}

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
}
