using System;
using System.IO;
using System.Collections.Generic; // for IEnumerator<T> like List<Transform>
using System.Collections; // IEnumerator used in coroutine.
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
//using Unity.Mathematics;

using Python.Runtime;  // This package resides in Assets/Assets/Plugins

//When using Console application in vscode: Use NuGet package manager extension to install pythonnet (=Python.Runtime)
// The message will be: Success! Wrote pythonnet to c:\Users\moon\PythonNetTestvsCode\Assembly-CSharp.csproj. Run dotnet restore to update your project.
//=>   <Reference Include="Python.Runtime">
//        <HintPath>C:\Users\moon\PythonNetTestvsCode\Assets\Assets\Plugins\Python.Runtime.dll</HintPath>
// </Reference>


//Note: running "pip install pythonnet" only installs the ability to load & use CLR types & assemblies from Python.
// To embed PythonNet in a C# app, you actually don't need to install pythonnet on the Python side.




//MJ: This script component is attached to gameObject "bvhRetargetter", which has other two compomenents attached to it:
// BVHAnimationRetargetter and BVHFrameGetter (in addition to mandatory Transform component)
public class AvatarController : MonoBehaviour
{
   
    // The transforms for Skeleton gameObject; This is the bvhCurrentTransform set from the current frame of the bvh motion file
                                             //        12 13 14 15
    public int[] jointIndex = {1,2,3,4,5,6,7,8,9,10,11,28,29,30,31}; // Excluding Hip; starting from Spine
    public int   numOfUsedJoints = 15; // inclung the hip rotation, but excluding the hip position, execluding the finger joints

//MJ from bvh2features.py:
//                              1       2        3      4        5      6       7       8
 //('jtsel', JointSelector(['Spine','Spine1','Spine2','Spine3','Neck','Neck1','Head','RightShoulder', 
 //      9                 10        11            12             13        14              15         0th = Hip
 //   'RightArm', 'RightForeArm', 'RightHand', 'LeftShoulder', 'LeftArm', 'LeftForeArm', 'LeftHand'], include_root=True)),


//MJ: from read_bvh.py in gesticulator:
//  main_joints = [     
//       MocapJoints          usedJoints
//         "Hips",  #1             1
//         "Spine",  #2            2
//         "Spine1", #3            3
//         "Spine2", #4            4
//         "Spine3", #5            5
//         "Neck",  #6             6
//         "Neck1",  #7            7
//         "Head",   #8            8
//         "RightShoulder", #9     8
//         "RightArm",  #10        9
//         "RightForeArm",  #11    10
//         "RightHand",  #12       11
        
//         "RightHandThumb1", #13
//         "RightHandThumb2", #14
//         "RightHandThumb3", #15     
//         "RightHandIndex1",  #16
//         "RightHandIndex2",  #17
//         "RightHandIndex3",  #18
//         "RightHandMiddle1", #19
//         "RightHandMiddle2", #20
//         "RightHandMiddle3", #21
//         "RightHandRing1",  #22
//         "RightHandRing2",  #23
//         "RightHandRing3",  #24
//         "RightHandPinky1", #25
//         "RightHandPinky2", #26
//         "RightHandPinky3", #27
        
//         "LeftShoulder",  #28     12
//         "LeftArm",        #29    13
//         "LeftForeArm",  #30      14
//         "LeftHand",      #31     15
//         "LeftHandThumb1", #32
//         "LeftHandThumb2", #33
//         "LeftHandThumb3", #34
//         "LeftHandIndex1", #35
//         "LeftHandIndex2", #36
//         "LeftHandIndex3", #37
//         "LeftHandMiddle1", #38
//         "LeftHandMiddle2", #39
//         "LeftHandMiddle3", #40
//         "LeftHandRing1", #41
//         "LeftHandRing2", #42
//         "LeftHandRing3", #43
//         "LeftHandPinky1", #44
//         "LeftHandPinky2", #45
//         "LeftHandPinky3",  # left hand #46
//     ]



    public Transform avatarRootTransform; // initialized to null
    public List<string> jointPaths = new List<string>(); // initialized to an empty list of strings
    public List<Transform> avatarTransforms = new List<Transform>();
    // It creates an empty list of Transforms that you can directly use without the need to assign it later.
    // This way, even if GetComponent<BVHFrameGetter>().avatarCurrentTransforms returns null, this.avatarCurrentTransforms will be an empty list (Count will be 0) rather than null.
    // You can start adding or removing Transform elements from the list without encountering null reference exceptions.
    // The transforms for Skeleton gameObject; This is the bvhCurrentTransform set from the current frame of the bvh motion file

 
    public string bvhFileName = ""; // == Assets/Recording_001.bvh
    public Winterdust.BVH bvh;
    public double secondsPerFrame;
    public int frameCount;

    public GameObject skeletonGO = null;

    IEnumerator  ienumerator;

     private Quaternion fromEulerZXY(Vector3 euler)
    {   
        return Quaternion.AngleAxis(euler.z, Vector3.forward) * Quaternion.AngleAxis(euler.x, Vector3.right) * Quaternion.AngleAxis(euler.y, Vector3.up);
    }

    private Quaternion fromEulerZXY(float z, float x, float y)
    {   // angles z, z, y are in degrees
        return Quaternion.AngleAxis(z, Vector3.forward) * Quaternion.AngleAxis(x, Vector3.right) * Quaternion.AngleAxis(y, Vector3.up);
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


    void Awake() // Awake() Method of a component is executed even when the component is unchecked in the inspector; 
                 // When it is unchecked, its Start() and Update() methods are executed. 
    {

        // Load BVH motion data to this.bvh.allBones
        if (this.bvhFileName == "")
        {
            Debug.Log(" bvhFileName should be set in the inspector");
            throw new InvalidOperationException("No  Bvh FileName is set.");

        }

        //MJ: Note:
        //public BVH(string pathToBvhFile, double importPercentage = 1.0, bool zUp = false, int calcFDirFromFrame = 0, int calcFDirToFrame = -1, bool ignoreRootBonePositions = false, bool ignoreChildBonePositions = true, bool fixFrameRate = true, bool parseMotionData = true, BVH.ProgressTracker progressTracker = null)

        //MJ:  During Awake() of BVHFrameGetter Script, parse the bvh file and load the  hierarchy paths and  load motion data to this.bvh.allBones

        //VERY IMPORTANT:  when you get the motion file from the gesticulator, store it to this.bvh.allBones. Then the motion will be played.

        this.bvh = new Winterdust.BVH(bvhFileName, parseMotionData: true); // Load BVH motion data to this.bvh.allBones

        this.frameCount = this.bvh.frameCount;
        this.secondsPerFrame = this.bvh.secondsPerFrame; // 0.05f;
        // Sets to 20 fps
        Time.fixedDeltaTime = (float)this.secondsPerFrame;


        // Create a Skeleton hiearchy if it  is not yet created. 

        this.skeletonGO = GameObject.FindGameObjectWithTag("Skeleton");

        if (this.skeletonGO == null)
        {
            // (1)  create a skeleton of transforms for the skeleton hierarchy, 
            // (2) add a BVHDebugLines component to it so it becomes visible like a stick figur [
            // BVHDebugLines bvhdebugLines = gameObject.AddComponent<BVHDebugLines>() ],
            // (Lines are simply drawn between the bone heads (and ends))d  
            // (3) return the skeleton, which is a hierarchy of GameObjects.

            // Create all gameObjects hiearchy and the components needed to render the gameObjects for bvh.Allbones[].

            this.skeletonGO = this.bvh.makeDebugSkeleton(animate: false, skeletonGOName: "Skeleton");
            // => if animate = false, dot not create an animation clip but only the rest pose; 
            // Create BvhDebugLines component and MeshRenderer component,
            //  but do not create Animation component:
            // public GameObject makeDebugSkeleton(bool animate = true, string colorHex = "ffffff", float jointSize = 1f, int frame = -1, bool xray = false, bool includeBoneEnds = true, string skeletonGOName = "Skeleton", bool originLine = false)

            Debug.Log(" bvh Skeleton is  created");

            this.avatarRootTransform = this.skeletonGO.transform.GetChild(0); // the Hips joint: The first child of SkeletonGO

             // Set this.avatarTransforms refer to the children transforms of this.avatarRootTransform (Hip); Hip is the child of
            // the Skeleton, which is this.bvhAnimator.gameObject.transform, which is used as the root of the Unity Humanoid avatar
            // in  this.srcHumanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAnimator.gameObject.transform),
            // in BVHAnimationRetargetter component. 

            // Although the avatar controlled by Animator component has the root in Skeleton node, its transform is not changed at all, so 
            // we set the root of the avatar to its child, Hip; We will change this pose of the  avatar by means of motion data either from bvh file or
            // from motion generator.
       
            this.ParseAvatarRootTransform(this.avatarRootTransform, this.jointPaths, this.avatarTransforms);
           

            Debug.Log("In AvatarController: bvhFile has been read in Awake()");

        }
        else
        {
            Debug.Log("In AvatarController: bvh Skeleton is already created and has been given Tag 'Skeleton' ");

            //all gameObjects hiearchy and the components needed to render the gameObjects for bvh.Allbones[] are already available.

            // this.skeletonGO contains the pose of the skeleton obtained from the saved scene.
            // Set this pose to the allBones data structure of the BVH => no need to do it, because we do:
            //   this.bvh = new BVH(bvhFileName, parseMotionData: true); // Load BVH motion data to this.bvh.allBones always.

            //this.bvh.setBoneTransformsToSkeleton(this.skeletonGO);

            // Note:
            // public void setBoneTransformsToSkeleton(GameObject skeletonGO,  int frame = -1)
            // {
            //     Transform[] componentsInChildren = skeletonGO.GetComponentsInChildren<Transform>(); 

            //     for (int i = 0; i < this.boneCount; i++)
            //     {


            //         this.allBones[i].setLocalPosRot( componentsInChildren[ i+1].transform, ref frame);  
            // 		 // i+1 index: componentsInChildren contains the transform of Skeleton GameObject itself; we exclude it from the avatar hiearchy


            //     }


            // }



            //IMPORTANT:  Collect the transforms in the skeleton hiearchy into ***a list of transforms***,  this.avatarCurrentTransforms:
            // If you change  this.avatarCurrentTransforms, it affects the hierarchy of    this.skeletonGO , because both reference the same transforms;

            this.avatarRootTransform = this.skeletonGO.transform.GetChild(0);
            this.ParseAvatarRootTransform(this.avatarRootTransform, this.jointPaths, this.avatarTransforms);
            //MJ:  this.jointPaths and this.avatarTransforms are set within the above method.


        }
    } // Awake()



    void Start()

    {

        // Set PythonDLL property of Runtime class in Python.Runtime package/namespace: Find out how to install Python.Runtime via internet search
        Runtime.PythonDLL = @"C:\Users\moon\AppData\Local\Programs\Python\Python38\python38.dll";


        Debug.Log($"\nPythonEngine.PythonPath 0  ****:{PythonEngine.PythonPath}");
        //....\python38.zip;.\DLLs;.\lib; C:\Program Files\Unity\Hub\Editor\2021.3.25f1\Editor
        // => Setting   Runtime.PythonDLL causes the python env and Unity Editor to specified.

        //Set the environment variable PYTHONHOME

        string PYTHON_HOME = @"C:\Users\moon\AppData\Local\Programs\Python\Python38";

        PythonEngine.PythonHome = PYTHON_HOME;

        // Set the PythonPath Property of PythonEngine which is defined within Python.Runtime package/namespace        
        PythonEngine.PythonPath = string.Join(

            Path.PathSeparator.ToString(), // Path.PathSeparator.ToString() == ";" 
            new string[] {
                  PythonEngine.PythonPath,
                     Path.Combine(PYTHON_HOME, @"Lib\site-packages"),

                      @"D:\Dropbox\metaverse\gesticulator",  // the root folder itself  under which demo package resides; demo package has demo.py module
                      @"D:\Dropbox\metaverse\gesticulator\gesticulator",
                       @"D:\Dropbox\metaverse\gesticulator\gesticulator\visualization"

            }
        );

        Debug.Log($"\nPythonEngine.PythonPath  2 ****:{PythonEngine.PythonPath}");

        // Python 엔진 초기화
        PythonEngine.Initialize();

     
        // Define the input to the gesticulator: 
        //ToDo:  For the term project, you need to get the audio from the user and get the text from it using 
        // speechToText API

        string audio_text = "Deep learning is an algorithm inspired by how the human brain works, and as a result it's an algorithm which has no theoretical limitations on what it can do. The more data you give it and the more computation time you give it, the better it gets. The New York Times also showed in this article another extraordinary result of deep learning which I'm going to show you now. It shows that computers can listen and understand.";
        // In the case of plain text:
        // the word timing is estimated using  _estimate_word_timings_bert(self, text, total_duration_frames, tokenizer, bert_model):
        //    This is a convenience functions that enables the model to work with plaintext 
        //    transcriptions in place of a time-annotated JSON file from Google Speech-to-Text.
        
        string audio_file = @"D:\Dropbox\metaverse\gesticulator\demo\input\jeremy_howard.wav";

        // Start Coroutine: It basically execute a custom Update(), which has a yield statement somewhere
        this.ienumerator = GetSpeech_GenGesture_Display(audio_text, audio_file);
        StartCoroutine( this.ienumerator );

    } // Start

    // What is a coroutine: A normal function does some stuff, and then returns a value, and then it's done. A coroutine does some stuff, returns a value, but is not necessarily done. Instead, it "yields". When it returns, it sends control back to whatever called it; it's saying "Hey, I'm done with the thing I did; I have more stuff to do, though, so I'll just be waiting here for you to call me."
    // from https://forum.unity.com/threads/why-cant-you-yield-an-ienumerator-directly-in-a-coroutine.359316/

    // IEnumerators weren't invented by Unity. They use IEnumerators to make their Coroutine implementation work,

    // GetSpeech_GenGesture_Display() works as if Upodate()

   
    IEnumerator GetSpeech_GenGesture_Display(string audio_text, string audio_file)
    {
        // Global Interpreter Lock을 취득 ; Indetation: Shift+Alt+F: https://www.grepper.com/answers/31148/visual+studio+code+auto+indent

        using (Py.GIL())
        {         
                // (1) Get utterance, audio_array, from the user via Microphone
                // (2) Get text, audio_text, from the utterance, via SpeechToText API
                // (3) Get the response text from the avatar via DialoGPT, and the audio_array via TextToSpeech API

                //(4) Now Generate a gesture from audio_text and audio_array using Gesticulator network.
                //(5) Display the pose of the avatar for the current frame, and yield.



                // NOTE: set the Api Compatibility Level to .Net 4.x in your Player Settings,
                //  because you  need to use the dynamic keyword.

                // dynamic pysys = Py.Import("sys");   // Py.Import() uses  PythonEngine.PythonPath  to import python modules.
                // dynamic pySysPath = pysys.path;
                // string[] sysPathArray = (string[])pySysPath;    // About conversion: https://csharp.hotexamples.com/site/file?hash=0x7a3b7b993fab126a5a205be68df1c82bd87e4de081aa0f5ad36909b54f95e3d7&fullName=&project=pythonnet/pythonnet

                // List<string> sysPath = ((string[])pySysPath).ToList<string>();

                // Debug.Log("\nsys.path:\n");
                // Array.ForEach(sysPathArray, element => Debug.Log($"{element}\t"));


                // // All python objects should be declared as dynamic type: https://discoverdot.net/projects/pythonnet

                // dynamic os = Py.Import("os");

                // dynamic pycwdUnity = os.getcwd();
                // string cwdUnity = (string)pycwdUnity;

                // Debug.Log($"Initial curr working dir = {cwdUnity}");





                //# audio, sample_rate = librosa.load(audio_filename) # sample_rate = 22050 discrete values  per second; 0.1 s = 1 frame => 2205 discrete time points per frame
                dynamic librosa = Py.Import("librosa");  // import a package


                dynamic audio_plus_sample_rate = librosa.load(audio_file); // audio is an np.array [ shape=(n,) or (2,n)]
                var audio_array = audio_plus_sample_rate[0];


                //https://stackoverflow.com/questions/23143184/how-do-i-check-type-of-dynamic-datatype-at-runtime

                //Type t = (audio_array).GetType();

                //Debug.Log($"\n\n (audio).GetType():{t}\n");
//
                int sample_rate = audio_plus_sample_rate[1];

                dynamic democs = Py.Import("demo.democs");
                //MJ: passing keyword arguments:  
                // Use mod.func(args, keywordargname: keywordargvalue) to apply keyword arguments.

                //dynamic motionPythonArray = democs.main(audio_file:null, audio_text:speech_text, audio_array:audio_array, sample_rate:sample_rate);
                
                //MJ: Test two versions of main: main1 with audi_file and main2 with audio_array:

                //dynamic motionPythonArray = democs.main1(audio_text: audio_text, audio_file:audio_file);
                dynamic motionPythonArray = democs.main2(audio_text: audio_text, audio_array:audio_array, sample_rate:sample_rate);
                
                //  We removed lower-body data, retaining 15 upper-body joints out of the original 69. 
                // Fingers were not modelled due to poor data quality. 15 joints * 3 = 45 angles

                // To obtain semantic information for the speech, we first transcribed the audio recordings 
                // using Google Cloud automatic speech  recognition (ASR), followed by thorough manual review
                //  to correct recognition errors and add punctuation for both the training and test parts of 
                //  the dataset

                // To extract motion features, the motion-capture data was downsampled to 20 fps 
                // and the joint angles were converted to an exponential map representation [16]
                //  relative to a T-pose; this is common in computer animation                
                // gestures were converted from joint angles to 3D joint positions.

                int num_of_rows = motionPythonArray.shape[0]; // = 528
                int num_of_cols = motionPythonArray.shape[1]; // = 45 (The  total number of main joints is 46); The returned 45 means that HIPs joint is fixed and not returned

                // System.Text.StringBuilder logMessage = new System.Text.StringBuilder();

                // for (int i = 0; i < num_of_rows; i++) // mum_of_rows == 528: why not 520?
                // {
                //     logMessage.AppendFormat("Frame={0}: \n", i);// To avoid the repetition of the debug message in Unity, use .LogFormat instead of .Log

                //     for (int j = 0; j < num_of_cols; j++) 
                //     {

                //         logMessage.AppendFormat("{0:f4} \t", (float) motionPythonArray[i][j]);

                //     } // for each Euler angle

                //     logMessage.AppendLine();

                // }

                // System.IO.File.WriteAllText("motionGeneratedFromGesticulatorAudioArray5digits.txt", logMessage.ToString());


                
                // System.Text.StringBuilder logMessageQuat = new System.Text.StringBuilder();
                // for (int i = 0; i < num_of_rows; i++) // i = 0 ~ 527; num_of_rows = num of frames
                // {
                //     for (int j = 0; j < num_of_cols /3; j++) // num_of_cols are the num of Euler  angles for the  15 joints; depth first search of the skeleton hierarchy: bvh.boneCount = 57
                //                                              // The range of j = the range of joints = 15;               // HumanBodyBones: Hips=0....; LastBone = 55
                //     {  // The 0th joint refers to Spine, just above Hips.

                //         //  In Unity, the Euler angles follows the convention of ZXY Euler angles,
                //         //     where the first angle corresponds to the Z-axis rotation, 
                //         //  the second angle corresponds to the X-axis rotation, 
                //         // and the third angle corresponds to the Y-axis rotation.

                //         Vector3 eulerAngles_ij = new Vector3();

                //         // Get the three angles for the jth joint
                //         eulerAngles_ij.z = (float)motionPythonArray[i][ 3*j + 0]; // ZXY euler angles (in degrees)
                //         eulerAngles_ij.x = (float) motionPythonArray[i][ 3*j + 1]; // ZXY euler angles (in degrees) 
                //         eulerAngles_ij.y = (float) motionPythonArray[i][ 3*j + 2]; // ZXY euler angles (in degrees) 

                //         // For testing, generate random euler angles in degrees:
                        
                        
                //         eulerAngles_ij.z = (float)UnityEngine.Random.Range(0, 360);
                //         eulerAngles_ij.x = (float) UnityEngine.Random.Range(0, 360);
                //         eulerAngles_ij.y = (float) UnityEngine.Random.Range(0, 360);
                //         quaternion rotation_ij = quaternion.EulerZXY(eulerAngles_ij.x * Mathf.Deg2Rad, eulerAngles_ij.y * Mathf.Deg2Rad, eulerAngles_ij.z * Mathf.Deg2Rad); // expects radian inputs

                //         //public static quaternion EulerZXY(float x, float y, float z

                //         logMessageQuat.AppendFormat("{0:f4} \t", rotation_ij);
                //         //this.avatarTransforms[ this.jointIndex[j] ].localRotation = rotation_ij;
                //         // Set the local rotations of each frame  to the correspoding transform in the skeleton hiearchy;
                //         // This means the rest pose of the skeleton is important to the final pose

                //         // DIfference between UnityEngine.Quaternion vs Unity.Mathematics.quaternion: 
                //         //  https://forum.unity.com/threads/unity-mathematics-quaternion-euler-vs-unityengine-quaternion-euler.608197/
                //         //Note:  Quaternion.Euler == quaternion.EulerZXY

                //     } //  for each bone j

                //     logMessageQuat.AppendLine();

                  
                // } //   for (i = 0; i++;i < num_of_rows) 

                //  System.IO.File.WriteAllText("motionGeneratedFromGesticulatorQuaternions.txt", logMessageQuat.ToString());



                // this.avatarTransforms should be set carefully, because it is used by 
                //  this.srcHumanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAnimator.gameObject.transform);
                //  and this.srcHumanPoseHandler.GetHumanPose(ref humanPose); 
               // This variable is set  to the list of children transforms of the bvh avatarRoot in Awake() method of the current component, AvatarController;
            
                
                Vector3 rootPosition = new Vector3(0, 1, 0); // the hip position is set  1 m above the ground, the world coord system.

                this.avatarTransforms[ 0 ].localPosition = rootPosition; // The root position of the body is set always the same => This could be changed dynamically by
                                                                                  // other locomotions.
                this.avatarTransforms[ 0 ].localRotation = Quaternion.identity; 

                // int num_of_rows = motionPythonArray.shape[0]; // = 528
                // int num_of_cols = motionPythonArray.shape[1]; // = 45 (The  total number of main joints is 46); The returned 45 means that HIPs joint is fixed and not returned

                for (int i = 0; i < num_of_rows; i++) // i = 0 ~ 527; num_of_rows = num of frames
                {
                    for (int j = 0; j < num_of_cols /3; j++) // num_of_cols are the num of Euler  angles for the  15 joints; depth first search of the skeleton hierarchy: bvh.boneCount = 57
                                                             // The range of j = the range of joints = 15;               // HumanBodyBones: Hips=0....; LastBone = 55
                    {  // The 0th joint refers to Spine, just above Hips.

                        //  In Unity, the Euler angles follows the convention of ZXY Euler angles,
                        //     where the first angle corresponds to the Z-axis rotation, 
                        //  the second angle corresponds to the X-axis rotation, 
                        // and the third angle corresponds to the Y-axis rotation.

                        Vector3 eulerAngles_ij = new Vector3();

                        // Get the three angles for the jth joint
                        eulerAngles_ij.z = (float)motionPythonArray[i][ 3*j + 0]; // ZXY euler angles (in degrees)
                        eulerAngles_ij.x = (float) motionPythonArray[i][ 3*j + 1]; // ZXY euler angles (in degrees) 
                        eulerAngles_ij.y = (float) motionPythonArray[i][ 3*j + 2]; // ZXY euler angles (in degrees) 


                        // eulerAngles_ij.z = (float)UnityEngine.Random.Range(0, 360);
                        // eulerAngles_ij.x = (float) UnityEngine.Random.Range(0, 360);
                        // eulerAngles_ij.y = (float) UnityEngine.Random.Range(0, 360);  

                        //quaternion rotation_ij = quaternion.EulerZXY(eulerAngles_ij.x * Mathf.Deg2Rad, eulerAngles_ij.y * Mathf.Deg2Rad, eulerAngles_ij.z * Mathf.Deg2Rad); // expects radian inputs
                        // private Quaternion fromEulerZXY(float z, float x, float y)
                        // angles z, z, y are in degrees
                        Quaternion rotation_ij =  fromEulerZXY( eulerAngles_ij.z,  eulerAngles_ij.x, eulerAngles_ij.y);
                        //public static quaternion EulerZXY(float x, float y, float z

                        
                        this.avatarTransforms[ this.jointIndex[j] ].localRotation = rotation_ij;
                        // Set the local rotations of each frame  to the correspoding transform in the skeleton hiearchy;
                        // This means the rest pose of the skeleton is important to the final pose

                        // DIfference between UnityEngine.Quaternion vs Unity.Mathematics.quaternion: 
                        //  https://forum.unity.com/threads/unity-mathematics-quaternion-euler-vs-unityengine-quaternion-euler.608197/
                        //Note:  Quaternion.Euler == quaternion.EulerZXY

                    } //  for each bone j

                    yield return null; // yield with null YieldInstruction so that SetMotionPythonToSkeleton  function 
                                       // will be resumed at the location immediately at the next frame.
                                       // When resumed at this location, the next iteration of the  for loop will be executed.
                } //   for (i = 0; i++;i < num_of_rows) 

                yield return StartCoroutine( this.ienumerator );

                // yield return this.ienumerator;
               // At the end of the for loop, the ienumerator returns without yield instruction. 
               // You can yield return yieldInstruction or yield break within a coroutine.

                //Restore the current working directory used in python back to the one used in Unity

                // os.chdir(cwdUnity);
                // pycwdUnity = os.getcwd();

                // Debug.Log($"\n\n new os.cwd={cwdUnity}");
                // // at run time, motionPythonArray will be of Python.Runtime.PyObject
                // // audio_array: null is the same as audio_array=None in python
                // Type t2 = motionPythonArray.GetType();

                // Debug.Log("\n\n ( motionPythonArray.GetType():{t2}\n");

                // // NDarray <==> C# array: https://stackoverflow.com/questions/66866731/numpy-net-getting-values

                // // Assuming motionPythonArray is a numpy.ndarray object
                // int num_of_rows = motionPythonArray.shape[0];
                // int num_of_cols = motionPythonArray.shape[1];



                // System.Text.StringBuilder logMessage = new System.Text.StringBuilder();

                // for (int i = 0; i < num_of_rows; i++) // mum_of_rows == 528: why not 520?
                // {
                //     logMessage.AppendFormat("Frame={0}: \n", i);// To avoid the repetition of the debug message in Unity, use .LogFormat instead of .Log

                //     for (int j = 0; j < num_of_cols; j++) 
                //     {

                //         logMessage.AppendFormat("{0} \t", motionPythonArray[i][j]);

                //     }

                //     logMessage.AppendLine();

                // }

                // System.IO.File.WriteAllText("motionGeneratedFromGesticulator.txt", logMessage.ToString());

                

                // python 환경을 종료한다.
                // PythonEngine.Shutdown();
                //Console.WriteLine("Press any key...");
                //Console.ReadKey();

            }   // using GIL( Py.GIL() ) 

        } // IEnumerator GetSpeech_GenGesture_Display()     


    }   // AvatarController


