using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


public class AvatarController   : MonoBehaviour
    {   

    public Transform avatarRootTransform;

    public List<Transform> avatarCurrentTransforms = new List<Transform>(); 
    // The transforms for Skeleton gameObject; This is the bvhCurrentTransform set from the current frame of the bvh motion file

    
    void Start() 

        {
          
           // Set PythonDLL property of Runtime class in Python.Runtime package/namespace: Find out how to install Python.Runtime via internet search
            Runtime.PythonDLL =@"C:\Users\moon\AppData\Local\Programs\Python\Python38\python38.dll";
                        
            
            Debug.Log($"\nPythonEngine.PythonPath 0  ****:{PythonEngine.PythonPath}");
            //....\python38.zip;.\DLLs;.\lib; C:\Program Files\Unity\Hub\Editor\2021.3.25f1\Editor
            // => Setting   Runtime.PythonDLL causes the python env and Unity Editor to specified.

            //Set the environment variable PYTHONHOME
           
            string PYTHON_HOME =@"C:\Users\moon\AppData\Local\Programs\Python\Python38";
            
            PythonEngine.PythonHome =  PYTHON_HOME; 
            
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
            // Global Interpreter Lock을 취득
            using (Py.GIL())
            {
        
             // NOTE: set the Api Compatibility Level to .Net 4.x in your Player Settings,
             //  because you  need to use the dynamic keyword.

                dynamic pysys = Py.Import("sys");   // Py.Import() uses  PythonEngine.PythonPath  to import python modules.
                dynamic pySysPath = pysys.path;
                string[] sysPathArray = ( string[]) pySysPath;    // About conversion: https://csharp.hotexamples.com/site/file?hash=0x7a3b7b993fab126a5a205be68df1c82bd87e4de081aa0f5ad36909b54f95e3d7&fullName=&project=pythonnet/pythonnet

                List<string> sysPath = ((string[])pySysPath).ToList<string>();
               
                Debug.Log("\nsys.path:\n");
                Array.ForEach(sysPathArray, element =>  Debug.Log($"{element}\t") );

         
                // All python objects should be declared as dynamic type: https://discoverdot.net/projects/pythonnet

                dynamic os = Py.Import("os");

                dynamic pycwdUnity = os.getcwd();
                string cwdUnity = (string)pycwdUnity;

                Debug.Log($"Initial curr working dir = {cwdUnity}");

              
               
                // Define the input to the gesticulator: 
                //ToDo:  For the term project, you need to get the audio from the user and get the text from it using 
                // speechToText API
                
                string audio_text = "Deep learning is an algorithm inspired by how the human brain works, and as a result it's an algorithm which has no theoretical limitations on what it can do. The more data you give it and the more computation time you give it, the better it gets. The New York Times also showed in this article another extraordinary result of deep learning which I'm going to show you now. It shows that computers can listen and understand.";
                             

              //# audio, sample_rate = librosa.load(audio_filename) # sample_rate = 22050 discrete values  per second; 0.1 s = 1 frame => 2205 discrete time points per frame
                dynamic librosa = Py.Import("librosa");  // import a package

                string audio_file = @"D:\Dropbox\metaverse\gesticulator\demo\input\jeremy_howard.wav";
                dynamic audio_plus_sample_rate = librosa.load(audio_file); // audio is an np.array [ shape=(n,) or (2,n)]
                var audio_array = audio_plus_sample_rate[0];
                                                              
                        
                //https://stackoverflow.com/questions/23143184/how-do-i-check-type-of-dynamic-datatype-at-runtime

                Type t = (audio_array).GetType();

                Debug.Log($"\n\n (audio).GetType():{t}\n");

                int sample_rate = audio_plus_sample_rate[1];
               
                dynamic democs = Py.Import("demo.democs");
                //MJ: passing keyword arguments:  
               // Use mod.func(args, keywordargname: keywordargvalue) to apply keyword arguments.
              
                dynamic motionPythonArray = democs.main(audio_file, audio_text, audio_array:null, sample_rate:-1);

                //MJ: Set motionPythonArray to the root transform of this.gameObject, which is Skeleton.
                this.avatarRootTransform = this.gameObject.transform.GetChild(0); // the Hips joint: The first child of SkeletonGO

                //this.ParseAvatarRootTransform(this.avatarRootTransform, this.jointPaths, this.avatarCurrentTransforms);



                //  this.avatarCurrentTransforms[0].localPosition = vector; // 0 ~ 56: a total of 57  ==> this.avatarCurrentTransforms holds the transforms of the Skeleton hierarchy
                //  this.avatarCurrentTransforms[0].localRotation = quaternion;

                // this.avatarCurrentTransforms[b].localRotation = quaternion;    
                // Set the local rotations of each frame  to the correspoding transform in the skeleton hiearchy;
                // This means the rest pose of the skeleton is important to the final pose



                //Restore the current working directory used in python back to the one used in Unity

                os.chdir(cwdUnity);
                pycwdUnity = os.getcwd();
        
                Debug.Log($"\n\n new os.cwd={cwdUnity}");
                // at run time, motionPythonArray will be of Python.Runtime.PyObject
                // audio_array: null is the same as audio_array=None in python
                Type t2 = motionPythonArray.GetType();

                Debug.Log("\n\n ( motionPythonArray.GetType():{t2}\n");

               // NDarray <==> C# array: https://stackoverflow.com/questions/66866731/numpy-net-getting-values

                // Assuming motionPythonArray is a numpy.ndarray object
                int num_of_rows =motionPythonArray.shape[0];
                int num_of_cols = motionPythonArray.shape[1];
          
                                            
                
                System.Text.StringBuilder logMessage = new System.Text.StringBuilder();

                for (int i = 0; i < num_of_rows; i++) // mum_of_rows == 528: why not 520?
                {
                    logMessage.AppendFormat("Frame={0}: \n",i);// To avoid the repetition of the debug message in Unity, use .LogFormat instead of .Log

                    for (int j = 0; j < num_of_cols; j++) //=24
                    {     
                                          
                        logMessage.AppendFormat("{0} \t", motionPythonArray[i][j]);                      

                    }

                    logMessage.AppendLine();

                }

                System.IO.File.WriteAllText("motionGeneratedFromGesticulator.txt", logMessage.ToString() );

            }    // using GIL( Py.GIL() )

            // python 환경을 종료한다.
            PythonEngine.Shutdown();
            //Console.WriteLine("Press any key...");
            //Console.ReadKey();

        }   //   c void Start())

    }   //class Program


