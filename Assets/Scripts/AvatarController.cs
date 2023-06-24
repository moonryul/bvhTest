using System;
using System.Collections.Generic; // for IEnumerator<T> like List<Transform>
using System.Collections; // IEnumerator used in coroutine.
using System.Linq;
using System.Text;
using System.Threading.Tasks;


//using Unity.Mathematics;

using Python.Runtime;  // This package resides in Assets/Assets/Plugins

//When using Console application in vscode: Use NuGet package manager extension to install pythonnet (=Python.Runtime)
// The message will be: Success! Wrote pythonnet to c:\Users\moon\PythonNetTestvsCode\Assembly-CSharp.csproj. Run dotnet restore to update your project.
//=>   <Reference Include="Python.Runtime">
//        <HintPath>C:\Users\moon\PythonNetTestvsCode\Assets\Assets\Plugins\Python.Runtime.dll</HintPath>
// </Reference>


//Note: running "pip install pythonnet" only installs the ability to load & use CLR types & assemblies from Python.
// To embed PythonNet in a C# app, you actually don't need to install pythonnet on the Python side.


using System.IO;
using UnityEngine;

//MJ: This script component is attached to gameObject "bvhRetargetter", which has other two compomenents attached to it:
// BVHAnimationRetargetter and BVHFrameGetter (in addition to mandatory Transform component)
public class AvatarController : MonoBehaviour
{
    public int frameNo = 0;
    public GameObject skeletonGO = null;
    
    public List<string> jointPaths = new List<string>(); // emtpy one

    public Transform avatarRootTransform;

    public List<Transform> avatarCurrentTransforms; // = new List<Transform>();
    // The transforms for Skeleton gameObject; This is the bvhCurrentTransform set from the current frame of the bvh motion file
                                             //        12 13 14 15
    public int[] jointIndex = {1,2,3,4,5,6,7,8,9,10,11,27,28,29,30}; // Excluding Hip; starting from Spine
    public int   numOfUsedJoints = 15; // inclung the hip rotation, but excluding the hip position, execluding the finger joints
    public int   numOfbvhFileJoints; 
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

                      @"D:\Dropbox\Dropbox\metaverse\gesticulatorUnity",  // the root folder itself  under which demo package resides; demo package has demo.py module
                      @"D:\Dropbox\Dropbox\metaverse\gesticulatorUnity\gesticulator",
                       @"D:\Dropbox\Dropbox\metaverse\gesticulatorUnity\gesticulator\visualization"

            }
        );

        Debug.Log($"\nPythonEngine.PythonPath  2 ****:{PythonEngine.PythonPath}");

        // Python 엔진 초기화
        PythonEngine.Initialize();

        // this.skeletonGO = GameObject.FindGameObjectWithTag("Skeleton");

        if (this.skeletonGO == null)
        {

             Debug.Log(" Skeleton gameObject should have been created and added to the hierarchy by using BVHSkeletonCreator.cs");

             throw new InvalidOperationException("No   Skeleton gameObject is set");
            
        }
        else
        {
            Debug.Log(" bvh Skeleton is already created and has been given Tag 'Skeleton' ");

          

            //IMPORTANT:  Collect the transforms in the skeleton hiearchy into ***a list of transforms***,  this.avatarCurrentTransforms:
            // If you change  this.avatarCurrentTransforms, it affects the hierarchy of    this.skeletonGO , because both reference the same transforms;

            this.avatarRootTransform = this.skeletonGO.transform.GetChild(0);
            this.ParseAvatarRootTransform(this.avatarRootTransform, this.jointPaths, this.avatarCurrentTransforms);
            //MJ:  this.jointPaths and this.avatarCurrentTransforms are set within the above method.
            this.numOfbvhFileJoints = this.avatarCurrentTransforms.Count;
                     

        }


       
        // this.avatarRootTransform = this.gameObject.GetComponent<BVHFrameGetter>().avatarRootTransform;
        // this.avatarCurrentTransforms = this.gameObject.GetComponent<BVHFrameGetter>().avatarCurrentTransforms;

     
        // Define the input to the gesticulator: 
        //ToDo:  For the term project, you need to get the audio from the user and get the text from it using 
        // speechToText API

        string audio_text = "Deep learning is an algorithm inspired by how the human brain works, and as a result it's an algorithm which has no theoretical limitations on what it can do. The more data you give it and the more computation time you give it, the better it gets. The New York Times also showed in this article another extraordinary result of deep learning which I'm going to show you now. It shows that computers can listen and understand.";
        // In the case of plain text:
        // the word timing is estimated using  _estimate_word_timings_bert(self, text, total_duration_frames, tokenizer, bert_model):
        //    This is a convenience functions that enables the model to work with plaintext 
        //    transcriptions in place of a time-annotated JSON file from Google Speech-to-Text.
        
        string audio_file = @"D:\Dropbox\Dropbox\metaverse\gesticulatorUnity\demo\input\jeremy_howard.wav";

        // Start Coroutine: It basically execute a custom Update(), which has a yield statement somewhere
        //IEnumerator _ienumerator = GetSpeech_GenGesture_Display(audio_text, audio_file);
        StartCoroutine( GetSpeech_GenGesture_Display(audio_text, audio_file) );

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

                dynamic motionPythonArray = democs.main1(audio_text: audio_text, audio_file:audio_file);
                //dynamic motionPythonArray = democs.main2(audio_text: audio_text, audio_array:audio_array, sample_rate:sample_rate);
                
                //MJ: just for debugging:
                //yield break;


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

                //         logMessage.AppendFormat("{0} \t", motionPythonArray[i][j]);

                //     }

                //     logMessage.AppendLine();

                // }

                // System.IO.File.WriteAllText("motionGeneratedFromGesticulator.bvh", logMessage.ToString());


                
                Vector3 rootPosition = new Vector3();// the hip position is set  1 m above the ground, the world coord system.
                Vector3 eulerAngles_ij = new Vector3();
                Quaternion rotation_ij = new Quaternion(); //

                // this.avatarCurrentTransforms[ 0 ].localPosition = rootPosition; // The root position of the body is set always the same => This could be changed dynamically by
                //                                                                   // other locomotions.
                // this.avatarCurrentTransforms[ 0 ].localRotation = Quaternion.identity; 

                // int num_of_rows = motionPythonArray.shape[0]; // = 528
                // int num_of_cols = motionPythonArray.shape[1]; // = 45 (The  total number of main joints is 46); The returned 45 means that HIPs joint is fixed and not returned

                for (int i = 0; i < num_of_rows; i++) // i = 0 ~ 527; num_of_rows = num of frames
                {   this.frameNo = i;

                    // Get the position and the rotation of the root joint, Hips, j=0
                    rootPosition.x = (float)motionPythonArray[i][0];
                    rootPosition.y = (float) motionPythonArray[i][1];
                    rootPosition.z = (float) motionPythonArray[i][2]; 

                    this.avatarCurrentTransforms[ 0 ].localPosition = rootPosition; // The root position of the body is set always the same => This could be changed dynamically by
                                                                                  // other locomotions.
                    eulerAngles_ij.z = (float)motionPythonArray[i][ 3 + 0]; // X from ZXY euler angles (in degrees)
                    eulerAngles_ij.x = (float) motionPythonArray[i][ 3 + 1]; // Y from ZXY euler angles (in degrees) 
                    eulerAngles_ij.y = (float) motionPythonArray[i][ 3 + 2]; // Z from ZXY euler angles (in degrees) 
                    //    public static quaternion EulerXZY(float3 xyz) in Unity.Mathematics
                    rotation_ij = this.fromEulerZXY(eulerAngles_ij);

                    this.avatarCurrentTransforms[ 0 ].localRotation = rotation_ij; 

                    for (int j = 1; j < this.numOfbvhFileJoints; j++) //MJ: from Spine joint: j=1; A total of 57 joints
                    //for (int j = 0; j < numOfUsedJoints; j++) // num_of_cols are the num of Euler  angles for the  15 joints; depth first search of the skeleton hierarchy: bvh.boneCount = 57
                                                             // The range of j = the range of joints = 15;               // HumanBodyBones: Hips=0....; LastBone = 55
                    {  // The 0th joint refers to Spine, just above Hips.
                    
                        //  In Unity, the Euler angles follows the convention of ZXY Euler angles,
                        //     where the first angle corresponds to the Z-axis rotation, 
                        //  the second angle corresponds to the X-axis rotation, 
                        // and the third angle corresponds to the Y-axis rotation.

                       

                        // Get the three angles for the jth joint
                        eulerAngles_ij.z = (float)motionPythonArray[i][ 3*(j+1) + 0]; // ZXY euler angles (in degrees)
                        eulerAngles_ij.x = (float) motionPythonArray[i][ 3*(j+1) + 1]; // ZXY euler angles (in degrees) 
                        eulerAngles_ij.y = (float) motionPythonArray[i][ 3*(j+1) + 2]; // ZXY euler angles (in degrees) 

                        rotation_ij = this.fromEulerZXY(eulerAngles_ij); 

                        //public static quaternion EulerZXY(float x, float y, float z): Returns a quaternion constructed by first performing a rotation around the z-axis, then the x-axis and finally the y-axis.

                        //  Update the pose of the avatar to the motion data of the current frame this.frameNo
                        //this.avatarCurrentTransforms[b].localPosition = vector; // 0 ~ 56: a total of 57
                        //this.avatarCurrentTransforms[ this.jointIndex[j] ].localRotation = rotation_ij;

                        this.avatarCurrentTransforms[ j].localRotation = rotation_ij;
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

 // BVH to Unity
    private Quaternion fromEulerZXY(Vector3 euler)
    {
        return Quaternion.AngleAxis(euler.z, Vector3.forward) * Quaternion.AngleAxis(euler.x, Vector3.right) * Quaternion.AngleAxis(euler.y, Vector3.up);
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

    }   //class AvatarController


