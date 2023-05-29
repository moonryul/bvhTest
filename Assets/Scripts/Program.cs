using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Python.Runtime;  // This package resides in Assets/Assets/Plugins

 //refer: Success! Wrote pythonnet to c:\Users\moon\PythonNetTestvsCode\Assembly-CSharp.csproj. Run dotnet restore to update your project.
        //=>   <Reference Include="Python.Runtime">
          //        <HintPath>C:\Users\moon\PythonNetTestvsCode\Assets\Assets\Plugins\Python.Runtime.dll</HintPath>
          // </Reference>
          

//https://stackoverflow.com/questions/69339974/python-net-pythonengine-initialize-crashes-application-without-throwing-except
//Note that running "pip install pythonnet" only installs the ability to load & use CLR types & assemblies from Python.

//    To embed PythonNet in a C# app, you actually don't need to install pythonnet on the Python side.


using System.IO;
using UnityEngine;


public class Program   : MonoBehaviour
    {   
    
    void Start() 

        {
           //Debug.Log($"\nPythonEngine.PythonPath -1  ****:{PythonEngine.PythonPath}")  => The specified procedure could not be found error;
           // Set PythonDLL property of Runtime class in Python.Runtime package/namespace
            Runtime.PythonDLL =@"C:\Users\moon\AppData\Local\Programs\Python\Python38\python38.dll";
                        
            
            Debug.Log($"\nPythonEngine.PythonPath 0  ****:{PythonEngine.PythonPath}");
            //=> :C:\Users\moon\anaconda3\envs\gest_env\python38.zip;.\DLLs;.\lib; C:\Program Files\Unity\Hub\Editor\2021.3.25f1\Editor
            // => Setting   Runtime.PythonDLL causes the python virtual env and Unity Editor path to be defined.

            //Set the environment variables, PATH, PYTHONHOME, PYTHONPATH
            //(1) Set the path to the virtual environment called "gest_env"
            string PYTHON_HOME =@"C:\Users\moon\AppData\Local\Programs\Python\Python38";
            
            PythonEngine.PythonHome =  PYTHON_HOME; //(5) Set PythonHome property of PhythonEngine class in Python.Runtime namespace
            // Why do we set PythonHome property here?
            // The Environment.SetEnvironmentVariable method is a general-purpose method provided by the .NET Framework
            //  to modify environment variables. It can be used with any environment variable, including "PYTHONHOME."
            //  The PythonEngine.PythonHome property, on the other hand, is specific to a particular Python engine implementation, 
            //  such as IronPython or Python.NET.
            //  By setting both the environment variable and the Python engine property, the code ensures compatibility and 
            // sets the "PYTHONHOME" value for both the general environment (accessible via Environment.GetEnvironmentVariable) 
            // and the specific Python engine (accessible via PythonEngine.PythonHome).

                    
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
        
             //  you  need to set the Api Compatibility Level to .Net 4.x in your Player Settings, because you  need to use the dynamic keyword.

                dynamic pysys = Py.Import("sys");   // It uses  PythonEngine.PythonPath 
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

              

               
                // (2) Test: the new version where the input audio and text are prepared within the csharp code
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
                //MJ: In C#, when calling a Python function using PythonNet, you don't need to explicitly define the audio_array variable before passing it as a keyword argument. 
               // Use mod.func(args, Py.kw("keywordargname", keywordargvalue)) or mod.func(args, keywordargname: keywordargvalue) to apply keyword arguments.
                // from https://github.com/pythonnet/pythonnet

                dynamic motionPythonArray = democs.main(audio_file, audio_text, audio_array:null, sample_rate:-1);

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
          
              // The following code snippet manually copies the data from the NumPy array to the C#  multidimensional array
              // without creating a new copy of the data.
                // unsafe // To use unsafe code, you need to compile with /unsafe option
                // {
                //     fixed (float* dataPtr = &motion1DArray[0])
                //     {
                //         float* sourcePtr = (float*)np.AsArray<float>( motionPythonArray.ravel() );
                //         //The ravel() function in NumPy returns a 1D array that contains all the elements of the original array in a contiguous order,
                //         // without making a copy of the data.

                //         // The error occurs because you cannot directly cast a dynamic object to a pointer type like float*. 
                //         // The dynamic keyword in C# allows for late binding and dynamic dispatch, 
                //         // but it does not provide a direct way to work with pointers.
                //         // If you want to access the elements of the NumPy array in an unsafe context and copy them into
                //         //  a C# 2D array using pointer operations, 
                //         // you need to convert the motionPythonArray to a C# array before working with pointers
                       
                //         int length = motion1DArray.Length;

                //         for (int i = 0; i < length; i++)
                //         {
                //             dataPtr[i] = sourcePtr[i];
                //         }
                //     }
                // }
   
                           
              //float[,] motionArrayFloat = (float[,]) motionPythonArray; // motionPythonArray is  a list of lists        
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


