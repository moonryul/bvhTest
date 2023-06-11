using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class TransferAnimation : MonoBehaviour
{
    public Transform HumanoidModelWhoseAnimationWillBeCopiedToThisModel;
    private Animator _OtherAnimator;

    public AnimationClip clip;

    private GameObjectRecorder _Recorder;

    void Start()
    {
        // Create recorder and record the script GameObject.
        _Recorder = new GameObjectRecorder(gameObject);

        // Bind all the Transforms on the GameObject and all its children.
        _Recorder.BindComponentsOfType<Transform>(gameObject, true);

        _OtherAnimator = HumanoidModelWhoseAnimationWillBeCopiedToThisModel.gameObject.GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        //wir können keine animation aufnehmen, während ein animator das modell bewegt
        //also machen wir es so:
        //wir übertragen einfach die animation (siehe unten... über position und rotate) auf ein anderes modell
        //auf diese weise braucht das andere modell keine animation, und wir können die animation aufnehmen
        //wir machen sowohl die animation als auch dieses modell humanoid

        List<Transform> nMyTransforms = new List<Transform>();
        //Helpers.GetAllChildren(this.transform, ref nMyTransforms);

        List<Transform> nHumanoidTransforms = new List<Transform>();
        //Helpers.GetAllChildren(HumanoidModelWhoseAnimationWillBeCopiedToThisModel, ref nHumanoidTransforms);

        //we also need to process the main transform, not only its bones
        Undo.RecordObject(HumanoidModelWhoseAnimationWillBeCopiedToThisModel, "Inspector"); //Bone rotations done by a script are not recorded. Is there a trick to record these bone rotations via script anyways? For example using something like (pseudo-code) this? -> Undo.RecordObject(nTargetTransform, "Inspector");
        transform.localRotation = HumanoidModelWhoseAnimationWillBeCopiedToThisModel.localRotation;
        transform.position = HumanoidModelWhoseAnimationWillBeCopiedToThisModel.position;//auch die position ist wichtig!!! z. B. wenn sich jemand duckt beim zielen!!

        foreach (Transform nHumanoidTransform in nHumanoidTransforms)
        {
            foreach (Transform nMyGenericTransform in nMyTransforms)
            {
                if (nHumanoidTransform.name == nMyGenericTransform.name)
                {
                    Undo.RecordObject(nHumanoidTransform, "Inspector"); //Bone rotations done by a script are not recorded. Is there a trick to record these bone rotations via script anyways? For example using something like (pseudo-code) this? -> Undo.RecordObject(nTargetTransform, "Inspector");
                    nMyGenericTransform.localRotation = nHumanoidTransform.localRotation;
                    nMyGenericTransform.position = nHumanoidTransform.position;//auch die position ist wichtig!!! z. B. wenn sich jemand duckt beim zielen!!

                    break;
                }
            }
        }

        if (_OtherAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && !_OtherAnimator.IsInTransition(0))
        {
            //animation has finished playing
            if (_Recorder.isRecording)
            {
                // Save the recorded session to the clip.
                _Recorder.SaveToClip(clip);
                _Recorder.ResetRecording();

                // gameObject.SetActive(false);

            }
        }
        else
        {
            // Take a snapshot and record all the bindings values for this frame.
            _Recorder.TakeSnapshot(Time.deltaTime);
        }
    }

    public static void GetAllChildren(Transform parent, ref List<Transform> transforms)
    {
        foreach (Transform t in parent)
        {
            transforms.Add(t);
            GetAllChildren(t, ref transforms);
        }
    }
}