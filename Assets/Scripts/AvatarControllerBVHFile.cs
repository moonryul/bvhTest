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
public class AvatarControllerBVHFile : MonoBehaviour
{
   
    
     // The joint index from gesticulator:
     //                        0 1 ....          9  10 11 12 13 14
    public int[] jointIndex = {1,2,3,4,5,6,7,8,9,10,11,27,28,29,30}; // The joint index in avatarTransforms
 
    public int   numOfUsedJoints = 15; // excluding and the lower limbs and the finger joints, and the Hips joint

//MJ from bvh2features.py:
//                              0      1        2      3        4      5       6       7
 //('jtsel', JointSelector(['Spine','Spine1','Spine2','Spine3','Neck','Neck1','Head','RightShoulder', 
 //      8                9        10            11             12        13              14        
 //   'RightArm', 'RightForeArm', 'RightHand', 'LeftShoulder', 'LeftArm', 'LeftForeArm', 'LeftHand'], include_root=True)),


//MJ: from read_bvh.py in gesticulator:
//  main_joints = [   #MJ: without the lower limb joints  
//       MocapJoints          usedJoints in gesticulator
//         "Hips",  #0            
//         "Spine",  #1                0
//         "Spine1", #2                1
//         "Spine2", #3                2
//         "Spine3", #4                3
//         "Neck",  #5                 4
//         "Neck1",  #6                5
//         "Head",   #7                6
//         "RightShoulder", #8         7
//         "RightArm",  #9             8
//         "RightForeArm",  #10        9
//         "RightHand",  #11           10
        
//         "RightHandThumb1", #12
//         "RightHandThumb2", #13
//         "RightHandThumb3", #14     
//         "RightHandIndex1",  #15
//         "RightHandIndex2",  #16
//         "RightHandIndex3",  #17
//         "RightHandMiddle1", #18
//         "RightHandMiddle2", #19
//         "RightHandMiddle3", #20
//         "RightHandRing1",  #21
//         "RightHandRing2",  #22
//         "RightHandRing3",  #23
//         "RightHandPinky1", #24
//         "RightHandPinky2", #25
//         "RightHandPinky3", #26
        
//         "LeftShoulder",  #27       11
//         "LeftArm",        #28      12
//         "LeftForeArm",  #29        13
//         "LeftHand",      #30       14
//         "LeftHandThumb1", #31
//         "LeftHandThumb2", #32
//         "LeftHandThumb3", #33
//         "LeftHandIndex1", #34
//         "LeftHandIndex2", #35
//         "LeftHandIndex3", #36
//         "LeftHandMiddle1", #37
//         "LeftHandMiddle2", #38
//         "LeftHandMiddle3", #39
//         "LeftHandRing1", #40
//         "LeftHandRing2", #41
//         "LeftHandRing3", #42
//         "LeftHandPinky1", #43
//         "LeftHandPinky2", #44
//         "LeftHandPinky3", #45
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

        this.skeletonGO = GameObject.FindGameObjectWithTag("Skeleton");

        if (this.skeletonGO == null)
        {

             Debug.Log(" BVH Skeleton Should have been created by BVHSkeletonCreator component and added to the hierarchy");
             
             throw new InvalidOperationException(" In BVHFrameGetter:Skeleton is not added to the hierarchy");
             

            // // (1)  create a skeleton of transforms for the skeleton hierarchy, 
            // // (2) add a BVHDebugLines component to it so it becomes visible like a stick figur [
            // // BVHDebugLines bvhdebugLines = gameObject.AddComponent<BVHDebugLines>() ],
            // // (Lines are simply drawn between the bone heads (and ends))d  
            // // (3) return the skeleton, which is a hierarchy of GameObjects.

            // // Create all gameObjects hiearchy and the components needed to render the gameObjects for bvh.Allbones[].

            // this.skeletonGO = this.bvh.makeDebugSkeleton(animate: false, skeletonGOName: "Skeleton");
            // // => if animate = false, dot not create an animation clip but only the rest pose; 
            // // Create BvhDebugLines component and MeshRenderer component,
            // //  but do not create Animation component:
            // // public GameObject makeDebugSkeleton(bool animate = true, string colorHex = "ffffff", float jointSize = 1f, int frame = -1, bool xray = false, bool includeBoneEnds = true, string skeletonGOName = "Skeleton", bool originLine = false)

            // Debug.Log(" bvh Skeleton is  created");

            // this.avatarRootTransform = this.skeletonGO.transform.GetChild(0); // the Hips joint: The first child of SkeletonGO
            // // Set this.avatarTransforms refer to the children transforms of this.avatarRootTransform (Hip); Hip is the child of
            // // the Skeleton, which is this.bvhAnimator.gameObject.transform, used as the root of the Unity Humanoid avatar
            // // in  this.srcHumanPoseHandler = new HumanPoseHandler(this.bvhAnimator.avatar, this.bvhAnimator.gameObject.transform),
            // // in BVHAnimationRetargetter component. 
       
            // this.ParseAvatarRootTransform(this.avatarRootTransform, this.jointPaths, this.avatarTransforms);
            

           

        }
        else
        {
            Debug.Log("In AvatarController: bvh Skeleton is already created and has been given Tag 'Skeleton' => Perfect ");

            //all gameObjects hiearchy and the components needed to render the gameObjects for bvh.Allbones[] are already available.

            // this.skeletonGO contains the pose of the skeleton obtained from the saved scene.


            //IMPORTANT:  Collect the transforms in the skeleton hiearchy into ***a list of transforms***,  this.avatarCurrentTransforms:
            // If you change  this.avatarCurrentTransforms, it affects the hierarchy of    this.skeletonGO , because both reference the same transforms;

            this.avatarRootTransform = this.skeletonGO.transform.GetChild(0);
            this.ParseAvatarRootTransform(this.avatarRootTransform, this.jointPaths, this.avatarTransforms);

            // this.avatarRootTransform = this.gameObject.GetComponent<BVHSkeletonCreator>().avatarRootTransform;
            // this.avatarTransforms = this.gameObject.GetComponent<BVHSkeletonCreator>().avatarTransforms;


        }
    }


    void Start()

    {

        // Set PythonDLL property of Runtime class in Python.Runtime package/namespace: Find out how to install Python.Runtime via internet search
        Runtime.PythonDLL = @"C:\Users\moon\AppData\Local\Programs\Python\Python38\python38.dll";
        //Runtime.PythonDLL = @"C:\Users\moon\anaconda3\envs\gest_env\python36.dll";
        
        Debug.Log($"\nPythonEngine.PythonPath 0  ****:{PythonEngine.PythonPath}");
        //....\python38.zip;.\DLLs;.\lib; C:\Program Files\Unity\Hub\Editor\2021.3.25f1\Editor
        // => Setting   Runtime.PythonDLL causes the python env and Unity Editor to specified.

        //Set the environment variable PYTHONHOME

        string PYTHON_HOME = @"C:\Users\moon\AppData\Local\Programs\Python\Python38";
        //string PYTHON_HOME = @"C:\Users\moon\anaconda3\envs\gest_env";

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

        // Python 엔진 초기화 => The Unity was killed during this initialization!
        //PythonEngine.Initialize();

     
        // Define the input to the gesticulator: 
        //ToDo:  For the term project, you need to get the audio from the user and get the text from it using 
        // speechToText API

        //string audio_text = "Deep learning is an algorithm inspired by how the human brain works, and as a result it's an algorithm which has no theoretical limitations on what it can do. The more data you give it and the more computation time you give it, the better it gets. The New York Times also showed in this article another extraordinary result of deep learning which I'm going to show you now. It shows that computers can listen and understand.";
        string audio_text = @"D:\Dropbox\Dropbox\metaverse\gesticulatorUnity\demo\input\jeremy_howard.json";
        // In the case of plain text:
        // the word timing is estimated using  _estimate_word_timings_bert(self, text, total_duration_frames, tokenizer, bert_model):
        //    This is a convenience functions that enables the model to work with plaintext 
        //    transcriptions in place of a time-annotated JSON file from Google Speech-to-Text.
        
        string audio_file = @"D:\Dropbox\Dropbox\metaverse\gesticulatorUnity\demo\input\jeremy_howard.wav";

       
        GetSpeech_GenGesture_Display(audio_text, audio_file);

    } // Start

    // What is a coroutine: A normal function does some stuff, and then returns a value, and then it's done. A coroutine does some stuff, returns a value, but is not necessarily done. Instead, it "yields". When it returns, it sends control back to whatever called it; it's saying "Hey, I'm done with the thing I did; I have more stuff to do, though, so I'll just be waiting here for you to call me."
    // from https://forum.unity.com/threads/why-cant-you-yield-an-ienumerator-directly-in-a-coroutine.359316/

    // IEnumerators weren't invented by Unity. They use IEnumerators to make their Coroutine implementation work,

    // GetSpeech_GenGesture_Display() works as if Upodate()

   
    void GetSpeech_GenGesture_Display(string audio_text, string audio_file)
    {
        // Global Interpreter Lock을 취득 ; Indetation: Shift+Alt+F: https://www.grepper.com/answers/31148/visual+studio+code+auto+indent

        using (Py.GIL())
        {        
               
                // NOTE: set the Api Compatibility Level to .Net 4.x in your Player Settings,
                //  because you  need to use the dynamic keyword.

                // dynamic pysys = Py.Import("sys");   // Py.Import() uses  PythonEngine.PythonPath  to import python modules.
                // dynamic pySysPath = pysys.path;
                // string[] sysPathArray = (string[])pySysPath;    // About conversion: https://csharp.hotexamples.com/site/file?hash=0x7a3b7b993fab126a5a205be68df1c82bd87e4de081aa0f5ad36909b54f95e3d7&fullName=&project=pythonnet/pythonnet

                // List<string> sysPath = ((string[])pySysPath).ToList<string>();

                // Debug.Log("\nsys.path:\n");
                // Array.ForEach(sysPathArray, element => Debug.Log($"{element}\t"));

                // // All python objects should be declared as dynamic type: https://discoverdot.net/projects/pythonnet

                dynamic os = Py.Import("os");

                dynamic cwd = os.getcwd();
                string cwdUnity = (string)cwd;

                Debug.Log($"curr working dir = {cwd}");





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


                Debug.Log($"\n call gesticulator to create bvh motion file");
                democs.main1bvh(audio_text: audio_text, audio_file:audio_file);
                //dynamic motionPythonArray = democs.main2bvh(audio_text: audio_text, audio_array:audio_array, sample_rate:sample_rate);
               
                Debug.Log($"\nbvh motion file, gen_motion_python_test.bvh, has been created");
               

            }   // using GIL( Py.GIL() ) 

        } //  GetSpeech_GenGesture_Display()     


               // python 환경을 종료한다.
                //PythonEngine.Shutdown();
                //Console.WriteLine("Press any key...");
                //Console.ReadKey();

    }   // AvatarControllerBVHFile


