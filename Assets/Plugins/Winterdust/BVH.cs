using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

using System.Collections.Generic;

namespace Winterdust
{
	/// <summary>BvhImporterExporterDemo 1.1.0 (Winterdust, Sweden). Representation of motion capture in the Biovision Hierarchy format.</summary>
	// Token: 0x02000002 RID: 2
	public class BVH
	{
		/// <summary>Creates a brand new BVH instance, copies all the data from THIS instance over to the new one and returns the new BVH instance. Changes to the old instance will not affect the new one and vice versa. (This is the only non-static method with a BVH return that does NOT return the same instance for chaining purposes.)</summary>
		// Token: 0x06000001 RID: 1 RVA: 0x00002390 File Offset: 0x00000590
		public BVH duplicate()
		{
			BVH bvh = new BVH();
			bvh.pathToBvhFileee = this.pathToBvhFileee;
			bvh.alias = this.alias;
			if (this.allBones != null)
			{
				bvh.allBones = new BVH.BVHBone[this.allBones.Length];
				for (int i = 0; i < this.allBones.Length; i++)
				{
					bvh.allBones[i] = this.allBones[i].duplicate();
				}
			}
			bvh.boneCount = this.boneCount;
			bvh.secondsPerFrame = this.secondsPerFrame;
			bvh.frameCount = this.frameCount;
			bvh.animationRotation = this.animationRotation;
			return bvh;
		}

		// Token: 0x06000002 RID: 2 RVA: 0x000020E0 File Offset: 0x000002E0
		private BVH()
		{
			this.animationRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
		}

		/// <summary>Loads a .bvh file found at the given path using UTF8 encoding. See each parameter description for full information.</summary>
		/// <param name="pathToBvhFile">Path to the .bvh text file. Can be absolute or relative to the program's working directory. Line endings may be marked by a single line feed character or a carriage return character and line feed character together.</param>
		/// <param name="importPercentage">You can reduce import time by reducing the number of keyframes to process. Setting importPercentage to 0.5 will skip every second frame in the .bvh file, which halves the number of frames but doubles the duration each frame is shown. 0.25 will turn 120 FPS into 30 FPS. Set this to a negative value to auto-adjust the importPercentage so that the animation get at most importPercentage*-1 FPS. Example: If importPercentage is -25 and you import a 30 FPS bhv file the importPercentage will auto-adjust to 0.8333 and the animation will end up at 25 FPS (a 15 FPS at will keep all its frames and end up at the original 15 FPS). You can't have a importPercentage of 0 (will become 1). If you don't care about the animation keep this at 1 and instead set parseMotionData to false. Tip: A shaky high FPS .bvh file will not only take less time to import with a lower importPercentage, it might appear smoother too. Tip: Try setting this to -10. Thanks to interpolation in the AnimationClip you might not notice much of a quality drop.</param>
		/// <param name="zUp">Y is up in the BVH format but some programs use Z as up when they create the .bvh file (or change Y to Z when they import a .bvh file and then don't change back upon export). Try setting this to true if your imported skeleton appears to be laying down instead of standing up. Note: This only has meaning during import, once the constructor is done Y is always up in the skeleton.</param>
		/// <param name="calcFDirFromFrame">Used together with calcFDirToFrame to determine which frames to use when calling calculateForwardDirectionOfAnimation() at the end of the constructor. If the default from 0 to -1 (the last frame of the animation) gives a poor forward you can pick two that defines the general direction of the animation better. Example: If the first root bone in your animation goes forward and then returns to a location to the left of the start position it would be better to only use the first half of the animation to calculate its forward direction, since using the first and last frames alone would give the impression of it going to the left.</param>
		/// <param name="calcFDirToFrame">Used together with calcFDirFromFrame to determine which frames to use when calling calculateForwardDirectionOfAnimation() at the end of the constructor. Tip: If you set calcFDirFromFrame and calcFDirToFrame to the same value calculateForwardDirectionOfAnimation() will always return Vector3.forward. Note: If the correct forward can't be calculated you can simply give it to the BVH instance by calling feedKnownForward() or feedKnownRotation() after the constructor returns.</param>
		/// <param name="ignoreRootBonePositions">Ignoring root bone positions will place all root bones at the skeleton origin. This means that if you have more than one root bone they will all have the same position.</param>
		/// <param name="ignoreChildBonePositions">Ignoring child bone positions saves import time since they are usually not moved anyway in most .bvh files (of course even more time is saved if they only have rotations specified in the .bvh file to begin with).</param>
		/// <param name="fixFrameRate">Fixing the frame rate will make for example "Frame Time: 0.0333333" in a .bvh file equal 30.0 FPS instead of 30.00003000003 FPS due to floating point inaccuracy.</param>
		/// <param name="parseMotionData">If you wish to only peek at a .bvh file (to for example only read its frame rate) you can set parseMotionData to false. This will make the constructor very fast, especially for long animations. However, only the rest pose will be available for all frames and calculated forward direction will always be Vector3.forward since nothing moves.</param>
		/// <param name="progressTracker">If you wish to check the status of the importation from a different thread in your program you can give a BVH.ProgressTracker here. Otherwise keep this as null.</param>
		// Token: 0x06000003 RID: 3 RVA: 0x00002438 File Offset: 0x00000638
		public BVH(string pathToBvhFile, double importPercentage = 1.0, bool zUp = false, int calcFDirFromFrame = 0, int calcFDirToFrame = -1, bool ignoreRootBonePositions = false, bool ignoreChildBonePositions = true, bool fixFrameRate = true, bool parseMotionData = true, BVH.ProgressTracker progressTracker = null)
		{
			this.pathToBvhFileee = pathToBvhFile;
			this.innerConstructor(File.ReadAllText(pathToBvhFile, Encoding.UTF8).Split(new char[]
			{
				'\n'
			}),  importPercentage, zUp, calcFDirFromFrame, calcFDirToFrame, ignoreRootBonePositions, ignoreChildBonePositions, fixFrameRate, parseMotionData, progressTracker);
		}

		/// <summary>Loads the given .bvh file content. See each parameter description for full information.</summary>
		/// <param name="bvhFile">The content of the .bvh text file. Each string in the given array should be a line in the text file.</param>
		/// <param name="importPercentage">You can reduce import time by reducing the number of keyframes to process. Setting importPercentage to 0.5 will skip every second frame in the .bvh file, which halves the number of frames but doubles the duration each frame is shown. 0.25 will turn 120 FPS into 30 FPS. Set this to a negative value to auto-adjust the importPercentage so that the animation get at most importPercentage*-1 FPS. Example: If importPercentage is -25 and you import a 30 FPS bhv file the importPercentage will auto-adjust to 0.8333 and the animation will end up at 25 FPS (a 15 FPS at will keep all its frames and end up at the original 15 FPS). You can't have a importPercentage of 0 (will become 1). If you don't care about the animation keep this at 1 and instead set parseMotionData to false. Tip: A shaky high FPS .bvh file will not only take less time to import with a lower importPercentage, it might appear smoother too. Tip: Try setting this to -10. Thanks to interpolation in the AnimationClip you might not notice much of a quality drop.</param>
		/// <param name="zUp">Y is up in the BVH format but some programs use Z as up when they create the .bvh file (or change Y to Z when they import a .bvh file and then don't change back upon export). Try setting this to true if your imported skeleton appears to be laying down instead of standing up. Note: This only has meaning during import, once the constructor is done Y is always up in the skeleton.</param>
		/// <param name="calcFDirFromFrame">Used together with calcFDirToFrame to determine which frames to use when calling calculateForwardDirectionOfAnimation() at the end of the constructor. If the default from 0 to -1 (the last frame of the animation) gives a poor forward you can pick two that defines the general direction of the animation better. Example: If the first root bone in your animation goes forward and then returns to a location to the left of the start position it would be better to only use the first half of the animation to calculate its forward direction, since using the first and last frames alone would give the impression of it going to the left.</param>
		/// <param name="calcFDirToFrame">Used together with calcFDirFromFrame to determine which frames to use when calling calculateForwardDirectionOfAnimation() at the end of the constructor. Tip: If you set calcFDirFromFrame and calcFDirToFrame to the same value calculateForwardDirectionOfAnimation() will always return Vector3.forward. Note: If the correct forward can't be calculated you can simply give it to the BVH instance by calling feedKnownForward() or feedKnownRotation() after the constructor returns.</param>
		/// <param name="ignoreRootBonePositions">Ignoring root bone positions will place all root bones at the skeleton origin. This means that if you have more than one root bone they will all have the same position.</param>
		/// <param name="ignoreChildBonePositions">Ignoring child bone positions saves import time since they are usually not moved anyway in most .bvh files (of course even more time is saved if they only have rotations specified in the .bvh file to begin with).</param>
		/// <param name="fixFrameRate">Fixing the frame rate will make for example "Frame Time: 0.0333333" in a .bvh file equal 30.0 FPS instead of 30.00003000003 FPS due to floating point inaccuracy.</param>
		/// <param name="parseMotionData">If you wish to only peek at a .bvh file (to for example only read its frame rate) you can set parseMotionData to false. This will make the constructor very fast, especially for long animations. However, only the rest pose will be available for all frames and calculated forward direction will always be Vector3.forward since nothing moves.</param>
		/// <param name="progressTracker">If you wish to check the status of the importation from a different thread in your program you can give a BVH.ProgressTracker here. Otherwise keep this as null.</param>
		// Token: 0x06000004 RID: 4 RVA: 0x00002484 File Offset: 0x00000684
		//public BVH(string[] bvhFile, double importPercentage = 1.0, bool zUp = false, int calcFDirFromFrame = 0, int calcFDirToFrame = -1, bool ignoreRootBonePositions = false, bool ignoreChildBonePositions = true, bool fixFrameRate = true, bool parseMotionData = true, BVH.ProgressTracker progressTracker = null)

        public BVH(string[] bvhFile,  double importPercentage = 1.0, bool zUp = false, int calcFDirFromFrame = 0, int calcFDirToFrame = -1, bool ignoreRootBonePositions = false, bool ignoreChildBonePositions = true, bool fixFrameRate = true, bool parseMotionData = true, BVH.ProgressTracker progressTracker = null)
        {
			this.pathToBvhFileee = null;
			// We may use motionPythonArray in place of bvhFile to play the Unity Avatar by the results of gesticulator.

			this.innerConstructor(bvhFile,  importPercentage, zUp, calcFDirFromFrame, calcFDirToFrame, ignoreRootBonePositions, ignoreChildBonePositions, fixFrameRate, parseMotionData, progressTracker);
		}

		// Token: 0x06000005 RID: 5 RVA: 0x000024B8 File Offset: 0x000006B8
		private void innerConstructor(string[] bvhFile,  double importPercentage = 1.0, bool zUp = false, int calcFDirFromFrame = 0, int calcFDirToFrame = -1, bool ignoreRootBonePositions = false, bool ignoreChildBonePositions = true, bool fixFrameRate = true, bool parseMotionData = true, BVH.ProgressTracker progressTracker = null)
		{
			//if (string.Concat(Type.GetType("Winterdust.Internal.BvhImporterExporterDemoL+MOIHA4KSJBDJAYSFGDJHLKX3JOIQYUW9GHBHAKPQKAMZNZOUWIJDGA5318008," + BVH.asmbly)).Length < 10)
			//{
			//	Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			//	int i;
			//	for (i = 0; i < assemblies.Length; i++)
			//	{
			//		if (string.Concat(assemblies[i].GetType("Winterdust.Internal.BvhImporterExporterDemoL+MOIHA4KSJBDJAYSFGDJHLKX3JOIQYUW9GHBHAKPQKAMZNZOUWIJDGA5318008")).Length >= 10)
			//		{
			//			BVH.asmbly = assemblies[i].FullName;
			//			break;
			//		}
			//	}
			//	if (i >= assemblies.Length)
			//	{
			//		Debug.Log("Something's wrong with the licence, please check https://winterdust.itch.io/bvhimporterexporter for a new version.");
			//		return;
			//	}
			//}
			//long num = (long)(TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes + 1440.0);

			//if (TimeZone.CurrentTimeZone.IsDaylightSavingTime(DateTime.Now))
			//{
			//	num += 2138L;
			//}
			//num = num * 2L + 452L;

			//long num2 = (long)DateTime.Now.Year + 1337L;
			//long num3 = (long)DateTime.Now.Month + 12L;
			//num2 *= 2872L * num3;
			//num3 *= 767125L;
			//string text = "bvh" + num2 + num + num3;

			//if (Resources.Load(text) == null)
			//{
			//	Debug.Log("### BvhImporterExporterDemo - The demonstration is not active right now.");
			//	Debug.Log("### To make it work please place an empty text file named \"" + text + "\" in your Resources folder.");
			//	Debug.Log("### Example: Assets/Resources/" + text + ".txt");
			//	Debug.Log("### [IMPORTANT] You have to rename this file every once in a while. You'll get this message again when the time comes.");
			//	Debug.Log("### This is to prevent games from being released with the demo version.");
			//	Debug.Log("### Thanks for trying the demo! Get the full version today and never see this message again!");
			//	return;
			//}
			//long num4 = (long)(TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now.AddMonths(-1)).TotalMinutes + 1440.0);
			//if (TimeZone.CurrentTimeZone.IsDaylightSavingTime(DateTime.Now.AddMonths(-1)))
			//{
			//	num4 += 2138L;
			//}
			//num4 = num4 * 2L + 452L;
			//long num5 = (long)DateTime.Now.AddMonths(-1).Year + 1337L;
			//long num6 = (long)DateTime.Now.AddMonths(-1).Month + 12L;
			//num5 *= 2872L * num6;
			//num6 *= 767125L;
			//string text2 = "bvh" + num5 + num4 + num6;
			//if (text != text2 && Resources.Load(text2) != null)
			//{
			//	Debug.Log("### BvhImporterExporterDemo - The demonstration is not active right now.");
			//	Debug.Log("### Please remove the previous text file to make it work (" + text2 + ").");
			//	return;
			//}
			if (progressTracker != null)
			{
				progressTracker.progress = 0.0;
			}


			this.allBones = new BVH.BVHBone[25]; // gesticulator has 24 bones.

			double num7 = 1.0;

			if (importPercentage == 0.0 || importPercentage > 1.0)
			{
				importPercentage = 1.0;
				num7 = 1.0;
			}
			else if (importPercentage > 0.0)
			{
				num7 = importPercentage;
			}
			bool flag = true;
			int num8 = 0;
			int num9 = 0;
			bool flag2 = false;

			int num10 = -1;

			double num11 = 1.0;
			int num12 = 0;
            //string s = null;
            //string s2 = null;

            string Frames = null;
            string FrameTime  = null;
            int num13 = 0;
			float[] array = null;

			int num14 = 0;

			for (int j = 0; j < bvhFile.Length; j++) //string[] bvhFile = input string
			{
				num14 += bvhFile[j].Length;
				string text3 = bvhFile[j].Replace("\t", " ").Trim();
				while (text3.IndexOf("  ") != -1)
				{
					text3 = text3.Replace("  ", " ");
				}
				if (flag)
				{
					if (text3 == "{")
					{
						num8++;
					}
					else if (text3 == "}")
					{
						if (num8 == num9)
						{
							num9--;
							num10 = this.allBones[num10].parentBoneIndex;
						}
						else
						{
							flag2 = false;
						}
						num8--;
					}
					else if (text3.StartsWith("JOINT ") || text3.StartsWith("ROOT "))
					{
						if (this.allBones.Length == this.boneCount)
						{
							BVH.BVHBone[] array2 = new BVH.BVHBone[this.boneCount + 25];

							for (int k = 0; k < this.boneCount; k++)
							{
								array2[k] = this.allBones[k];
							}
							this.allBones = array2;
						}
						string text4 = text3.Substring(text3.IndexOf(' ') + 1);
						if (num9 == 0)
						{
							this.allBones[this.boneCount].relativePath = text4;
							this.allBones[this.boneCount].parentBoneIndex = -1;
						}
						else
						{
							this.allBones[this.boneCount].relativePath = this.allBones[num10].relativePath + "/" + text4;
							this.allBones[this.boneCount].parentBoneIndex = num10;
						}

						num10 = this.boneCount;

						this.boneCount++;
						num9++;
					}
					else if (text3.StartsWith("OFFSET "))
					{
						if (flag2)
						{
							this.allBones[num10].defineEndPosition(ref text3, ref zUp);
						}
						else if (!ignoreRootBonePositions || this.allBones[num10].parentBoneIndex != -1)
						{
							this.allBones[num10].defineLocalRestPosition(ref text3, ref zUp);
						}
					}
					else if (text3.StartsWith("CHANNELS "))
					{
						if (text3.Contains("position") && ((this.allBones[num10].parentBoneIndex == -1 && !ignoreRootBonePositions) || (this.allBones[num10].parentBoneIndex != -1 && !ignoreRootBonePositions)))
						{
							this.allBones[num10].localFramePositions = new Vector3[0];
						}
						num13 += this.allBones[num10].defineChannels(ref text3);
					}
					else if (text3.StartsWith("End Site"))
					{
						flag2 = true;
					}
					else if (!(text3 == "HIERARCHY") && text3 == "MOTION")
					{
						flag = false;
					}
				}
				else if (text3[0] == 'F')
				{
					if (text3.StartsWith("Frames: "))
					{
						Frames = text3.Substring(8);   //  public String Substring(int startIndex); = Frames: 520 => 520
                    }
					else if (text3.StartsWith("Frame Time: "))
					{
						FrameTime = text3.Substring(12);
					}
				}
				else
				{
					if (num12 == 0)
					{
						this.secondsPerFrame = double.Parse(FrameTime);
						if (importPercentage < 0.0)
						{
							double num15 = 1.0 / this.secondsPerFrame;
							if (num15 > importPercentage * -1.0)
							{
								num7 = importPercentage * -1.0 / num15;
							}
						}
						this.secondsPerFrame *= 1.0 / num7;
						if (fixFrameRate)
						{
							this.secondsPerFrame = 1.0 / this.secondsPerFrame;
							this.secondsPerFrame = Math.Round(this.secondsPerFrame * 1000.0) / 1000.0;
							this.secondsPerFrame = 1.0 / this.secondsPerFrame;
						}

						this.frameCount = int.Parse(Frames);

						for (int l = 0; l < this.frameCount; l++)
						{
							if (num11 >= 1.0)
							{
								num11 -= 1.0;
								num12++;
								if (num12 == this.frameCount)
								{
									break;
								}
							}

							num11 += num7;
						}

						this.frameCount = num12;
						num11 = 1.0;
						num12 = 0;
						for (int m = 0; m < this.boneCount; m++)
						{
							if (this.allBones[m].localFramePositions != null)
							{
								this.allBones[m].localFramePositions = new Vector3[this.frameCount];
							}
							this.allBones[m].localFrameRotations = new Quaternion[this.frameCount];
						}
						array = new float[num13];
					}
					if (!parseMotionData)  // if parseMotionData = false:
					{
						for (int n = 0; n < this.boneCount; n++)
						{
							this.allBones[n].localFramePositions = null;
							for (int num16 = 0; num16 < this.frameCount; num16++)
							{
								this.allBones[n].localFrameRotations[num16] = Quaternion.identity;
							}
						}
						break;    // break out of 	the parsing loop for (int j = 0; j < bvhFile.Length; j++) //string[] bvhFile = input string
                    }


                    if (num11 >= 1.0)
					{
						num11 -= 1.0;
						string[] array3 = text3.Split(new char[]
						{
							' '
						});
						for (int num17 = 0; num17 < num13; num17++)
						{
							array[num17] = float.Parse(array3[num17]);
						}
						int num18 = 0;
						for (int num19 = 0; num19 < this.boneCount; num19++)
						{
							this.allBones[num19].feedFrame(ref num18, ref array, ref num12, ref zUp);
						}
						num12++;
						if (progressTracker != null)
						{
							progressTracker.progress = (double)num12 / (double)this.frameCount * 0.9999;
						}
						if (num12 == this.frameCount)
						{
							break;
						}
					}
					num11 += num7;
				}
			}
			if (this.pathToBvhFileee == null)
			{
				this.alias = string.Concat(new object[]
				{
					"bvh_",
					bvhFile.Length,
					"_",
					num14
				});
			}
			else
			{
				this.alias = "/" + this.pathToBvhFileee.Replace("\\", "/");
				this.alias = this.alias.Substring(this.alias.LastIndexOf("/") + 1);
				if (this.alias.Contains("."))
				{
					this.alias = this.alias.Substring(0, this.alias.LastIndexOf("."));
				}
			}
			this.animationRotation = Quaternion.LookRotation(this.calculateForwardDirectionOfAnimation(calcFDirFromFrame, calcFDirToFrame), Vector3.up);
			if (progressTracker != null)
			{
				progressTracker.progress = 1.0;
			}
		}

		/// <summary>(This is typically called right after the constructor returns and then never called again.) Changes the definition of what should be considered the animation's forward direction. This method does nothing on its own except change the internal animationRotation Quaternion to the value represented by the given forward, which affects future calls to setAnimationRotation(), rotateAnimationBy(), align() and normalize(). (Returns this BVH instead of void, for chaining.) Note: This is a shortcut for myBvh.feedKnownRotation(Quaternion.LookRotation(knownForwardDirectionOfAnimation.normalized, Vector3.up));</summary>
		// Token: 0x06000006 RID: 6 RVA: 0x000020FD File Offset: 0x000002FD
		public BVH feedKnownForward(Vector3 knownForwardDirectionOfAnimation)
		{
			return this.feedKnownRotation(Quaternion.LookRotation(knownForwardDirectionOfAnimation.normalized, Vector3.up));
		}

		/// <summary>(This is typically called right after the constructor returns and then never called again.) Changes the definition of what should be considered the animation's rotation. This method does nothing on its own except change the internal animationRotation Quaternion to the given value, which affects future calls to setAnimationRotation(), rotateAnimationBy(), align() and normalize(). (Returns this BVH instead of void, for chaining.) Tip: You can create knownRotationOfAnimation with euler angles via Quaternion.Euler(degRotX, degRotY, degRotZ)</summary>
		// Token: 0x06000007 RID: 7 RVA: 0x00002116 File Offset: 0x00000316
		public BVH feedKnownRotation(Quaternion knownRotationOfAnimation)
		{
			this.animationRotation = knownRotationOfAnimation;
			return this;
		}

		/// <summary>Multiplies the localRestPosition, endPosition and localFramePositions of all bones by the given amount. (Returns this BVH instead of void, for chaining.) Note: This changes the positions without storing their original values, myBvh.scale(2).scale(1) will not return the skeleton to its original scale. Remember that you can scale the transform of the skeletonGO returned by makeSkeleton()/makeDebugSkeleton() as well, without needing to modify the BVH instance.</summary>
		// Token: 0x06000008 RID: 8 RVA: 0x00002F10 File Offset: 0x00001110
		public BVH scale(float amount)
		{
			for (int i = 0; i < this.allBones.Length; i++)
			{
				BVH.BVHBone[] array = this.allBones;
				int num = i;
				array[num].localRestPosition = array[num].localRestPosition * amount;
				BVH.BVHBone[] array2 = this.allBones;
				int num2 = i;
				array2[num2].endPosition = array2[num2].endPosition * amount;
				if (this.allBones[i].localFramePositions != null)
				{
					for (int j = 0; j < this.frameCount; j++)
					{
						this.allBones[i].localFramePositions[j] *= amount;
					}
				}
			}
			return this;
		}

		/// <summary>Returns a string in this format: BVH "file name minus extension" (# bones, # frames, # FPS, #.# sec).</summary>
		// Token: 0x06000009 RID: 9 RVA: 0x00002FC8 File Offset: 0x000011C8
		public override string ToString()
		{
			return string.Concat(new object[]
			{
				"BVH \"",
				this.alias,
				"\" (",
				this.boneCount,
				" bones, ",
				this.frameCount,
				" frames, ",
				this.getFPS(),
				" FPS, ",
				this.getDurationSec(),
				" sec)."
			});
		}

		/// <summary>Returns how many seconds long the whole animation is. Note: This is a shortcut for myBvh.secondsPerFrame*myBvh.frameCount</summary>
		// Token: 0x0600000A RID: 10 RVA: 0x00002120 File Offset: 0x00000320
		public double getDurationSec()
		{
			return this.secondsPerFrame * (double)this.frameCount;
		}

		/// <summary>Returns the frame rate of the animation in terms of frames per second instead of vice versa. Note: This is a shortcut for 1/myBvh.secondsPerFrame</summary>
		// Token: 0x0600000B RID: 11 RVA: 0x00002130 File Offset: 0x00000330
		public double getFPS()
		{
			return 1.0 / this.secondsPerFrame;
		}

		/// <summary>Sets the frame rate of the animation to the given frames per second, a higher number will speed up the animation. (Returns this BVH instead of void, for chaining.) Note: This is a shortcut for myBvh.secondsPerFrame=1/framesPerSecond; Also: Lowering frame rate doesn't remove any frame data, if your goal is to produce less heavy AnimationClips it's better to reduce importPercentage in the constructor.</summary>
		// Token: 0x0600000C RID: 12 RVA: 0x00002142 File Offset: 0x00000342
		public BVH setFPS(double framesPerSecond)
		{
			this.secondsPerFrame = 1.0 / framesPerSecond;
			return this;
		}

		/// <summary>Makes a skeleton from the current state of the bones. See each parameter description for full information.</summary>
		/// <returns>The skeletonGO, which is a hierarchy of GameObjects.</returns>
		/// <param name="frame">Use frame -1 to put the skeleton into its rest pose. 0 puts the skeleton into the first frame of the animation, myBvh.boneCount-1 equals the last frame. Note: Some .bvh files will place an improved rest pose in the first frame.</param>
		/// <param name="includeBoneEnds">A "bone end" is not a bone itself but the end position of for example a head or finger bone. They have the name of their parent (the actual bone) plus "End" added as a suffix. These have no animation data themselves but can be used to for example put a hat on top of a head.</param>
		/// <param name="skeletonGOName">This simply decides the name of the returned GameObject. You can rename it later as usual with skeletonGO.name if you want to, though renaming a skeletonGO could break compatibility with AnimationClips if pathToSkeletonGO was not empty when calling makeAnimationClip().</param>
		/// <param name="animate">Calls the static BVH.animateSkeleton(returningSkeletonGO, myBvh.makeAnimationClip(), 1); if animate is true, animating the skeleton with an AnimationCLip created from the default parameters of makeAnimationClip().</param>
		// Token: 0x0600000D RID: 13 RVA: 0x00003054 File Offset: 0x00001254
		public GameObject makeSkeleton(int frame = -1, bool includeBoneEnds = true, string skeletonGOName = "Skeleton", bool animate = false)
		{
			GameObject gameObject = new GameObject(skeletonGOName);

			for (int i = 0; i < this.boneCount; i++)
			{
				if (this.allBones[i].parentBoneIndex == -1) // ParentBoneIndex =-1 means that this bone is a root bone. Usually there is only one root bone.
				                                 		    // The root bone's parent is gameObject.
				{  // >Makes a GameObject that represents this bone, complete with child GameObjects that represents the bone's children. 
					this.allBones[i].makeGO(ref frame, ref includeBoneEnds, ref this.allBones, i).transform.parent = gameObject.transform;
				}
			}
			if (animate)
			{
				BVH.animateSkeleton(gameObject, this.makeAnimationClip(0, -1, false, "", WrapMode.Loop, true, false, false), 1f);
				//=> 	public static Animation animateSkeleton(GameObject skeletonGO, AnimationClip clip, float blendTimeSec = 1f)
				//=>    gameObject( skeletonGO ).AddComponent<Animation>();
			}
			return gameObject;
		}




        //Added by Moon Jung; skeleton Hierarchy, skeletonGO,  already exists: Set the bones' locations/rotations are set to the current transforms of the
        // Skeleton's hiearchy. 
        public void setBoneTransformsToSkeleton(GameObject skeletonGO,  int frame = -1)
        {
            Transform[] componentsInChildren = skeletonGO.GetComponentsInChildren<Transform>(); 
			//in Unity C# scripts, when you call GetComponentsInChildren<Transform>(), the order of elements 
			//in the resulting array componentsInChildren follows a depth-first order of the hierarchy in the skeletonGO GameObject.

			// the resulting array componentsInChildren will contain the Transform component of the parent GameObject (skeletonGO)
			//  as the first element, followed by the Transform components of its children and their descendants in a depth-first order.
			          
            for (int i = 0; i < this.boneCount; i++)
            {

              
                this.allBones[i].setLocalPosRot( componentsInChildren[ i+1].transform, ref frame);  
				 // i+1 index: componentsInChildren contains the transform of Skeleton GameObject itself as the first element; we exclude it from the avatar hiearchy,
				 // because the root of the avatar, Hips, is a child to the Skeleton.


            }
            
            
        }

        //Note for makeSkeleton defined above:
		
        //public void setLocalPosRot(Transform boneTransform, ref int frame)
        //{
        //    if (frame == -1)
        //    {
        //        boneTransform.localPosition = this.localRestPosition;
        //        boneTransform.localRotation = Quaternion.identity;
        //        return;
        //    }
        //    boneTransform.localPosition = ((this.localFramePositions != null) ? this.localFramePositions[frame] : this.localRestPosition);
        //    boneTransform.localRotation = this.localFrameRotations[frame];
        //}

        /// <summary>Rearranges an existing skeleton to the given frame (changes both positions and rotations). 
		// Use frame -1 to put the skeleton into its rest pose. Does not touch GameObjects representing bone ends.	
		//	 (Returns this BVH instead of void, for chaining.)</summary>
        // Token: 0x0600000E RID: 14 RVA: 0x000030DC File Offset: 0x000012DC
        public BVH moveSkeleton(GameObject skeletonGO, int frame)
		{
			Transform transform = skeletonGO.transform;
			for (int i = 0; i < this.boneCount; i++)
			{
				Transform transform2 = transform.Find(this.allBones[i].relativePath);
				if (transform2 != null)
				{
					this.allBones[i].setLocalPosRot(transform2, ref frame);
				}
			}
			return this;
		}

		/// <summary>Makes a skeleton from the current state of the bones and adds a BVHDebugLines component to it so it becomes visible like a stick figure. See each parameter description for full information. Note: Lines are simply drawn between the bone heads (and ends). Some programs visualize skeletons by drawing from the bone head to their own tail and then to the child's head. So the skeleton might appear to be different here but it is just visualized differently. You can use calculateTail() on a BVHBone in the allBones[] array if you ever need a bone's tail (all bones have a tail, even if they don't have a endPosition set).</summary>
		/// <returns>The skeletonGO, which is a hierarchy of GameObjects.</returns>
		/// <param name="animate">Calls the static BVH.animateSkeleton(returningSkeletonGO, myBvh.makeAnimationClip(), 1); if animate is true, animating the skeleton with an AnimationCLip created from the default parameters of makeAnimationClip().</param>
		/// <param name="colorHex">You can change the line (and mesh) color from the default white by specifying a HTML hex color.</param>
		/// <param name="jointSize">If jointSize is not 0 a mesh will be rendered on every bone joint and any bone ends. The square surface faces the transform's up and the triangle surface the transform's forward. You can give a negative jointSize to make the surfaces face the skeleton's up/forward instead, which is always Vector3.up and Vector3.forward. Note: The skeleton's forward shouldn't be confused with the animation's forward. The animation can be rotated but the skeleton's rotation is always considered to be Quaternion.identity (though you can of course rotate/scale/reposition the transform of a created skeletonGO freely if you want to).</param>
		/// <param name="frame">Use frame -1 to put the skeleton into its rest pose. 0 puts the skeleton into the first frame of the animation, myBvh.boneCount-1 equals the last frame.</param>
		/// <param name="xray">Set this to true if you want to be able to see the lines even if they are behind something else.</param>
		/// <param name="includeBoneEnds">A "bone end" is not a bone itself but the end position of for example a head or finger bone. They have the name of their parent (the actual bone) plus "End" added as a suffix. These have no animation data themselves but can be used to for example put a hat on top of a head.</param>
		/// <param name="skeletonGOName">This simply decides the name of the returned GameObject. You can rename it later as usual with skeletonGO.name if you want to, though renaming a skeletonGO could break compatibility with AnimationClips if pathToSkeletonGO was not empty when calling makeAnimationClip().</param>
		/// <param name="originLine">Set this to true if you want a line to be drawn from the skeleton origin to the root bone(s).</param>
		// Token: 0x0600000F RID: 15 RVA: 0x00003138 File Offset: 0x00001338
		public GameObject makeDebugSkeleton(bool animate = true, string colorHex = "ffffff", float jointSize = 1f, int frame = -1, bool xray = false, bool includeBoneEnds = true, string skeletonGOName = "Skeleton", bool originLine = false)
		{
			GameObject gameObject = this.makeSkeleton(frame, includeBoneEnds, skeletonGOName, animate);
			// => 	GameObject gameObject = new GameObject(skeletonGOName);
			Color color;
			ColorUtility.TryParseHtmlString("#" + colorHex.Replace("#", "").Replace("0x", ""), out color);
			if (jointSize != 0f)
			{
				bool flag = false;
				if (jointSize < 0f)
				{
					jointSize *= -1f;
					flag = true;
				}
				Material material = new Material(Shader.Find("Legacy Shaders/Diffuse"));
				material.color = color;
				Transform[] componentsInChildren = gameObject.GetComponentsInChildren<Transform>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					if (componentsInChildren[i] != gameObject.transform)
					{
						Vector3[] array = new Vector3[]
						{
							new Vector3(-jointSize / 8f, jointSize / 2f, -jointSize / 2f),
							new Vector3(-jointSize, jointSize, -jointSize * 2f),
							new Vector3(jointSize, jointSize, -jointSize * 2f),
							new Vector3(jointSize / 8f, jointSize / 2f, -jointSize / 2f),
							new Vector3(0f, -jointSize, 0f),
							new Vector3(jointSize, jointSize, 0f),
							new Vector3(-jointSize, jointSize, 0f),
							new Vector3(0f, -jointSize, 0f)
						};
						if (flag && componentsInChildren[i].rotation != Quaternion.identity)
						{
							Quaternion rotation = Quaternion.Inverse(componentsInChildren[i].rotation) * Quaternion.identity;
							for (int j = 0; j < array.Length; j++)
							{
								array[j] = rotation * array[j];
							}
						}
						Mesh mesh = new Mesh();
						mesh.name = "BvhMesh";
						mesh.vertices = array;
						mesh.triangles = BVH.debugMeshFaces;
						mesh.RecalculateNormals();
						componentsInChildren[i].gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
						componentsInChildren[i].gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
					}
				}
			}
			BVHDebugLines bvhdebugLines = gameObject.AddComponent<BVHDebugLines>();

			bvhdebugLines.color = color;
			bvhdebugLines.xray = xray;
			bvhdebugLines.alsoDrawLinesFromOrigin = originLine;
			return gameObject;
        }   //public GameObject makeDebugSkeleton(bool animate = true, string colorHex = "ffffff", float jointSize = 1f, int frame = -1, bool xray = false, bool includeBoneEnds = true, string skeletonGOName = "Skeleton", bool originLine = false)
		


        /// <summary>Makes several debug skeletons that together visualize the whole animation at once (or a part of the animation if you change fromFrame/toFrame). The stick figures shift from green to yellow to red, where green is the beginning and red is the end. Xray will make the skeletons visible through walls.</summary>
        // Token: 0x06000010 RID: 16 RVA: 0x0000339C File Offset: 0x0000159C
        public GameObject makeDebugSkeletons(int fromFrame = 0, int toFrame = -1, bool includeBoneEnds = true, bool xray = false, string containerGOName = "SKELETONS")
		{
			this.fixFromFrameAndToFrame(ref fromFrame, ref toFrame);
			GameObject gameObject = new GameObject(containerGOName);
			float num = (float)(toFrame - fromFrame + 1);
			for (int i = fromFrame; i <= toFrame; i++)
			{
				GameObject gameObject2 = this.makeDebugSkeleton(false, "ffffff", 0f, i, xray, includeBoneEnds, "Skeleton Frame " + i, false);
				gameObject2.transform.parent = gameObject.transform;
				BVHDebugLines[] componentsInChildren = gameObject2.GetComponentsInChildren<BVHDebugLines>();
				for (int j = 0; j < componentsInChildren.Length; j++)
				{
					if (i == fromFrame)
					{
						componentsInChildren[j].color = Color.green;
					}
					else if (num == 3f && i == fromFrame + 1)
					{
						componentsInChildren[j].color = Color.yellow;
					}
					else
					{
						float num2 = (float)(i - fromFrame + 1) / num;
						if (num2 < 0.5f)
						{
							componentsInChildren[j].color = new Color(num2 * 2f, 1f, 0f);
						}
						else
						{
							componentsInChildren[j].color = new Color(1f, 1f - (num2 - 0.5f) * 2f, 0f);
						}
					}
				}
			}
			return gameObject;
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00002156 File Offset: 0x00000356
		private void fixFromFrameAndToFrame(ref int fromFrame, ref int toFrame)
		{
			if (fromFrame < 0)
			{
				fromFrame = this.frameCount + fromFrame;
			}
			if (toFrame < 0)
			{
				toFrame = this.frameCount + toFrame;
			}
		}

		/// <summary>Shifts all localFramePositions and localFrameRotations on all bones. If -1 is given the last frame becomes the first frame. If 1 is given the second frame becomes the first frame. (Returns this BVH instead of void, for chaining.)</summary>
		// Token: 0x06000012 RID: 18 RVA: 0x000034C0 File Offset: 0x000016C0
		public BVH shiftAnimation(int frameThatWillBecomeFrame0)
		{
			int num = frameThatWillBecomeFrame0 * -1;
			this.fixFromFrameAndToFrame(ref frameThatWillBecomeFrame0, ref num);
			if (num > 0)
			{
				Vector3[] array = new Vector3[this.frameCount + num];
				Quaternion[] array2 = new Quaternion[this.frameCount + num];
				for (int i = 0; i < this.boneCount; i++)
				{
					if (this.allBones[i].localFramePositions != null)
					{
						Array.Copy(this.allBones[i].localFramePositions, 0, array, num, this.frameCount);
						Array.Copy(array, this.frameCount, this.allBones[i].localFramePositions, 0, num);
						Array.Copy(array, num, this.allBones[i].localFramePositions, num, this.frameCount - num);
					}
					Array.Copy(this.allBones[i].localFrameRotations, 0, array2, num, this.frameCount);
					Array.Copy(array2, this.frameCount, this.allBones[i].localFrameRotations, 0, num);
					Array.Copy(array2, num, this.allBones[i].localFrameRotations, num, this.frameCount - num);
				}
			}
			return this;
		}

		/// <summary>Loops through the bones and returns the first one with the given relative path. Example of a relativePath to the bone named Head: "Hips/ToSpine/Spine/Spine1/Neck/Head". If alsoCheckJustName is true you can just have the bone's name as relativePath and the first bone by that name is returned. The returned integer is the index in the bones[] struct array of this BVH instance. -1 is returned if no match is made.</summary>
		// Token: 0x06000013 RID: 19 RVA: 0x000035E8 File Offset: 0x000017E8
		public int getBoneIndex(string relativePath, bool alsoCheckJustName = false)
		{
			if (alsoCheckJustName)
			{
				for (int i = 0; i < this.boneCount; i++)
				{
					if (this.allBones[i].relativePath == relativePath || this.allBones[i].getName() == relativePath)
					{
						return i;
					}
				}
			}
			else
			{
				for (int j = 0; j < this.boneCount; j++)
				{
					if (this.allBones[j].relativePath == relativePath)
					{
						return j;
					}
				}
			}
			return -1;
		}

		/// <summary>Creates an AnimationClip from the current state of the BVH instance. At least 2 frames are required to make a healthy AnimationClip. See each parameter description for full information. Note: This method must be called from Unity's main thread, use prepareAnimationClip() when calling from a different thread. Tip: You can make less heavy AnimationClips by reducing importPercentage in the constructor. Since keyframes are interpolated it's usually not easy to notice any difference in animation quality and as a bonus the import time is reduced. Try setting importPercentage in the constructor to -10 (meaning "auto-adjust to 10 FPS") and increase it only if needed.</summary>
		/// <returns>The animation clip, ready to be used by an Animation component (legacy) or in an Animator (mecanim).</returns>
		/// <param name="fromFrame">Keep 0 to start the animation from the first frame. If the first frame is a T-pose you can put 1 here to skip it.</param>
		/// <param name="toFrame">Keep -1 to end the animation on the last frame. Note: Reverse animations are possible (from -1 to 0 will reverse the whole thing).</param>
		/// <param name="addExtraLoopKeyframe">When true a copy of the first keyframe is added to the end of the animation for seamless looping. Usually not needed if the .bvh file was created for seamless looping in the first place.</param>
		/// <param name="pathToSkeletonGO">If the Animation (or Animator) component that is supposed to use this AnimationCLip is not on the skeletonGO itself but on a GameObject that contains the skeleton GameObject you can add the path here. Example: You have a GameObject hierarchy "Level/MyModel/MySkeleton/Hips/etc..." ("Level" has no parent). The skeletonGO is "MySkeleton" but the Animation component is on "MyModel". pathToSkeletonGO should then be "MySkeleton". Had the hierarchy been "Level/MyModel/MyContainer/MySkeleton/Hips/etc..." the correct pathToSkeletonGO would be "MyContainer/MySkeleton".</param>
		/// <param name="wrapMode">If you want to make the animation play back and forth you can give WrapMode.PingPong here instead of the default WrapMode.Loop. This simply sets the "wrapMode" property of the AnimationClip.</param>
		/// <param name="legacy">If you want to use the AnimationClip with the Animator component (via a AnimatorOverrideController for example) you can turn off legacy here to make it compatible with the Mecanim system. This simply sets the "legacy" property of the AnimationClip.</param>
		/// <param name="keyRestPositions">If the bone doesn't have any localFramePositions it is usually a good idea to only key the bone's rotation. That way the AnimationClip can be used on any skeletonGO and the length of each bone (the distance between the parent Transforms and their child Transforms) doesn't matter since they will just rotate. But if you need the positions to always be keyed the option is available here.</param>
		/// <param name="keyEndPositions">It's usually not a good idea to key the end positions of bones since they are not real joints and their Transform should just always go along with the movement of its parent. But if you want to key the end positions the option is available here. Note: End rotations are never keyed since they should always be Quaternion.identity.</param>
		// Token: 0x06000014 RID: 20 RVA: 0x0000366C File Offset: 0x0000186C
		public AnimationClip makeAnimationClip(int fromFrame = 0, int toFrame = -1, bool addExtraLoopKeyframe = false, string pathToSkeletonGO = "", WrapMode wrapMode = WrapMode.Loop, bool legacy = true, bool keyRestPositions = false, bool keyEndPositions = false)
		{
			return this.prepareAnimationClip(fromFrame, toFrame, addExtraLoopKeyframe, pathToSkeletonGO, wrapMode, legacy, keyRestPositions, keyEndPositions, null).make();
		}

		/// <summary>Creates a PreparedAnimationClip that you can call make() on to make an actual AnimationClip. The idea is that you can call this method from a secondary thread to prepare the clip during your game's loading phase and then later make() it on the main thread (a new AnimationClip may only be created on Unity's main thread, including setting its curves). If you wish to check the status of the preparation from a different thread in your program you can give a BVH.ProgressTracker here. See the description of makeAnimationClip() for full information about the method and its parameters.</summary>
		// Token: 0x06000015 RID: 21 RVA: 0x00003694 File Offset: 0x00001894
		public BVH.PreparedAnimationClip prepareAnimationClip(int fromFrame = 0, int toFrame = -1, bool addExtraLoopKeyframe = false, string pathToSkeletonGO = "", WrapMode wrapMode = WrapMode.Loop, bool legacy = true, bool keyRestPositions = false, bool keyEndPositions = false, BVH.ProgressTracker progressTracker = null)
		{
			if (progressTracker != null)
			{
				progressTracker.progress = 0.0;
			}
			this.fixFromFrameAndToFrame(ref fromFrame, ref toFrame);
			if (pathToSkeletonGO == null)
			{
				pathToSkeletonGO = "";
			}
			else if (pathToSkeletonGO.Length > 0)
			{
				pathToSkeletonGO = ("OK18JIJ4AKH1GB2JHD45UI8U" + pathToSkeletonGO).Replace("\\", "/").Replace("//", "/").Replace("OK18JIJ4AKH1GB2JHD45UI8U/", "").Replace("OK18JIJ4AKH1GB2JHD45UI8U", "");
				if (!pathToSkeletonGO.EndsWith("/"))
				{
					pathToSkeletonGO += "/";
				}
			}
			bool flag = fromFrame > toFrame;
			if (flag)
			{
				int num = fromFrame;
				fromFrame = toFrame;
				toFrame = num;
			}
			BVH.PreparedAnimationClip preparedAnimationClip = new BVH.PreparedAnimationClip();
			preparedAnimationClip.name = this.alias;
			preparedAnimationClip.legacy = legacy;
			preparedAnimationClip.wrapMode = wrapMode;
			preparedAnimationClip.frameRate = (float)this.getFPS();
			if (keyEndPositions)
			{
				int num2 = 0;
				for (int i = 0; i < this.boneCount; i++)
				{
					if (this.allBones[i].endPosition.sqrMagnitude != 0f)
					{
						num2++;
					}
				}
				preparedAnimationClip.data = new BVH.PreparedAnimationClip.CurveBlock[this.boneCount + num2];
			}
			else
			{
				preparedAnimationClip.data = new BVH.PreparedAnimationClip.CurveBlock[this.boneCount];
			}
			int num3 = toFrame - fromFrame + (addExtraLoopKeyframe ? 2 : 1);
			Keyframe[] array = new Keyframe[2];
			array[1].time = (float)(this.secondsPerFrame * (double)(num3 - 1));
			Keyframe[][][] array2 = new Keyframe[7][][];
			for (int j = 0; j < 7; j++)
			{
				array2[j] = new Keyframe[this.boneCount][];
				for (int k = 0; k < this.boneCount; k++)
				{
					array2[j][k] = new Keyframe[num3];
					for (int l = 0; l < num3; l++)
					{
						array2[j][k][l].time = (float)(this.secondsPerFrame * (double)l);
					}
				}
				if (progressTracker != null)
				{
					progressTracker.progress = ((double)j + 1.0) / 7.0 * 0.05;
				}
			}
			for (int m = 0; m < num3; m++)
			{
				int num4 = (addExtraLoopKeyframe && m == num3 - 1) ? 0 : m;
				if (flag)
				{
					num4 = toFrame - num4;
				}
				else
				{
					num4 = fromFrame + num4;
				}
				for (int n = 0; n < this.boneCount; n++)
				{
					if (this.allBones[n].localFramePositions != null)
					{
						Vector3 vector = this.allBones[n].localFramePositions[num4];
						array2[0][n][m].value = vector.x;
						array2[1][n][m].value = vector.y;
						array2[2][n][m].value = vector.z;
					}
					Quaternion quaternion = this.allBones[n].localFrameRotations[num4];
					array2[3][n][m].value = quaternion.x;
					array2[4][n][m].value = quaternion.y;
					array2[5][n][m].value = quaternion.z;
					array2[6][n][m].value = quaternion.w;
				}
				if (progressTracker != null)
				{
					progressTracker.progress = 0.05 + ((double)m + 1.0) / (double)num3 * 0.05;
				}
			}
			for (int num5 = 0; num5 < this.boneCount; num5++)
			{
				preparedAnimationClip.data[num5].relativePath = pathToSkeletonGO + this.allBones[num5].relativePath;
				if (this.allBones[num5].localFramePositions != null)
				{
					preparedAnimationClip.data[num5].posX = this.prepCurve(ref array2[0][num5]);
					preparedAnimationClip.data[num5].posY = this.prepCurve(ref array2[1][num5]);
					preparedAnimationClip.data[num5].posZ = this.prepCurve(ref array2[2][num5]);
				}
				else if (keyRestPositions)
				{
					array[0].value = this.allBones[num5].localRestPosition.x;
					array[1].value = this.allBones[num5].localRestPosition.x;
					preparedAnimationClip.data[num5].posX = this.prepCurve(ref array);
					array[0].value = this.allBones[num5].localRestPosition.y;
					array[1].value = this.allBones[num5].localRestPosition.y;
					preparedAnimationClip.data[num5].posY = this.prepCurve(ref array);
					array[0].value = this.allBones[num5].localRestPosition.z;
					array[1].value = this.allBones[num5].localRestPosition.z;
					preparedAnimationClip.data[num5].posZ = this.prepCurve(ref array);
				}
				preparedAnimationClip.data[num5].rotX = this.prepCurve(ref array2[3][num5]);
				preparedAnimationClip.data[num5].rotY = this.prepCurve(ref array2[4][num5]);
				preparedAnimationClip.data[num5].rotZ = this.prepCurve(ref array2[5][num5]);
				preparedAnimationClip.data[num5].rotW = this.prepCurve(ref array2[6][num5]);
				if (progressTracker != null)
				{
					progressTracker.progress = 0.1 + ((double)num5 + 1.0) / (double)this.boneCount * 0.8999;
				}
			}
			if (keyEndPositions)
			{
				int num6 = 0;
				for (int num7 = 0; num7 < this.boneCount; num7++)
				{
					if (this.allBones[num7].endPosition.sqrMagnitude != 0f)
					{
						preparedAnimationClip.data[this.boneCount + num6].relativePath = string.Concat(new string[]
						{
							pathToSkeletonGO,
							this.allBones[num7].relativePath,
							"/",
							this.allBones[num7].getName(),
							"End"
						});
						array[0].value = this.allBones[num7].endPosition.x;
						array[1].value = this.allBones[num7].endPosition.x;
						preparedAnimationClip.data[this.boneCount + num6].posX = this.prepCurve(ref array);
						array[0].value = this.allBones[num7].endPosition.y;
						array[1].value = this.allBones[num7].endPosition.y;
						preparedAnimationClip.data[this.boneCount + num6].posY = this.prepCurve(ref array);
						array[0].value = this.allBones[num7].endPosition.z;
						array[1].value = this.allBones[num7].endPosition.z;
						preparedAnimationClip.data[this.boneCount + num6].posZ = this.prepCurve(ref array);
						num6++;
					}
				}
			}
			if (progressTracker != null)
			{
				progressTracker.progress = 1.0;
			}
			return preparedAnimationClip;
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00003EE4 File Offset: 0x000020E4
		private AnimationCurve prepCurve(ref Keyframe[] keyframes)
		{
			AnimationCurve animationCurve = new AnimationCurve(keyframes);
			for (int i = 0; i < keyframes.Length; i++)
			{
				animationCurve.SmoothTangents(i, 0f);
			}
			return animationCurve;
		}

		/// <summary>Creates a brand new instance of BVH that is completely empty and returns it, in case you want to build your own BVH from scratch. (This method gives access to a private BVH constructor that takes no parameters, all makeEmpty() does is "return new BVH();".)</summary>
		// Token: 0x06000017 RID: 23 RVA: 0x00002178 File Offset: 0x00000378
		public static BVH makeEmpty()
		{
			return new BVH();
		}

		/// <summary>Convenience method for adding an Animation component to the given skeletonGO, adding the given AnimationClip to the Animation and then playing it. If the skeletonGO already has an Animation component the given AnimationClip is added to its list of clips and is then played, with a crossover time in seconds from any old animation. Note: This method will maintain max three clips in the list. Old clips are replaced as needed. Add clips manually to the returned Animation component instead of calling animateSkeleton() again if you wish to have more control.</summary>
		// Token: 0x06000018 RID: 24 RVA: 0x00003F18 File Offset: 0x00002118
		public static Animation animateSkeleton(GameObject skeletonGO, AnimationClip clip, float blendTimeSec = 1f)
		{
			Animation animation = skeletonGO.GetComponent<Animation>();
			if (animation == null)
			{
				animation = skeletonGO.AddComponent<Animation>();
			}
			if (animation.clip == null)
			{
				animation.clip = clip;
				animation.AddClip(animation.clip, animation.clip.name);
				animation.Play();
			}
			else if (animation.GetClipCount() == 1)
			{
				animation.AddClip(clip, "AAA");
				animation.Blend(animation.clip.name, 0f, blendTimeSec);
				animation.Blend("AAA", 1f, blendTimeSec);
			}
			else if (animation.GetClipCount() == 2)
			{
				animation.AddClip(clip, "BBB");
				animation.Blend("AAA", 0f, blendTimeSec);
				animation.Blend("BBB", 1f, blendTimeSec);
			}
			else
			{
				bool flag = true;
				foreach (object obj in animation)
				{
					AnimationState animationState = (AnimationState)obj;
					if (animationState.clip.name != "")
					{
						if (!animation.IsPlaying(animationState.clip.name) || animationState.weight == 0f || !flag)
						{
							if (animationState.clip.name == "AAA")
							{
								animation.AddClip(clip, "AAA");
								animation.Blend("BBB", 0f, blendTimeSec);
								animation.Blend("AAA", 1f, blendTimeSec);
								break;
							}
							animation.AddClip(clip, "BBB");
							animation.Blend("AAA", 0f, blendTimeSec);
							animation.Blend("BBB", 1f, blendTimeSec);
							break;
						}
						else
						{
							flag = false;
						}
					}
				}
			}
			return animation;
		}

		/// <summary>Makes it so that the first root bone's rest position and animation origin is at [0,?,0] of the skeleton, where ? is the original value of its position on the up axis (Y). I recommend calling this AFTER any rotation methods, including align(), since the rest pose usually isn't repositioned by rotateAnimationBy() unless alsoAffectRestPose is true. (Returns this BVH instead of void, for chaining.) Note: This is a shortcut for myBvh.setAnimationOrigin(Vector3.zero, true, false, true, false);</summary>
		// Token: 0x06000019 RID: 25 RVA: 0x0000217F File Offset: 0x0000037F
		public BVH centerXZ()
		{
			return this.setAnimationOrigin(Vector3.zero, true, false, true, false);
		}

		/// <summary>Makes it so that the first root bone's rest position and animation origin is at [0,0,0] of the skeleton, unless you set to keep any of XYZ. I recommend calling this AFTER any rotation methods, including align(), since the rest pose usually isn't repositioned by rotateAnimationBy() unless alsoAffectRestPose is true. (Returns this BVH instead of void, for chaining.) Note: This is a shortcut for myBvh.setAnimationOrigin(Vector3.zero, true, keepOldX, keepOldY, keepOldZ);</summary>
		// Token: 0x0600001A RID: 26 RVA: 0x00002190 File Offset: 0x00000390
		public BVH center(bool keepOldX = false, bool keepOldY = false, bool keepOldZ = false)
		{
			return this.setAnimationOrigin(Vector3.zero, true, keepOldX, keepOldY, keepOldZ);
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000040F4 File Offset: 0x000022F4
		private void makeLocalFramePositionsArray(int boneIndex)
		{
			this.allBones[boneIndex].localFramePositions = new Vector3[this.frameCount];
			for (int i = 0; i < this.frameCount; i++)
			{
				this.allBones[boneIndex].localFramePositions[i] = this.allBones[boneIndex].localRestPosition;
			}
		}

		/// <summary>Repositions the whole animation by moving the first root bone on all frames. Your given position becomes the origin. Any other root bones are also moved to keep their offset relative to the first root bone's new locations. In effect this repositions the whole skeleton on all frames. I recommend calling this AFTER any rotation methods, including align(), since the rest pose usually isn't repositioned by rotateAnimationBy() unless alsoAffectRestPose is true. (Returns this BVH instead of void, for chaining.)</summary>
		// Token: 0x0600001C RID: 28 RVA: 0x00004158 File Offset: 0x00002358
		public BVH setAnimationOrigin(Vector3 localPosition, bool alsoMoveRestPositions = true, bool keepOldX = false, bool keepOldY = false, bool keepOldZ = false)
		{
			if (alsoMoveRestPositions)
			{
				this.repositionFirstRootBone(localPosition, -1, keepOldX, keepOldY, keepOldX);
			}
			if (this.allBones[0].localFramePositions == null)
			{
				this.makeLocalFramePositionsArray(0);
			}
			Vector3 vector = this.allBones[0].localFramePositions[0];
			this.repositionFirstRootBone(localPosition, 0, keepOldX, keepOldY, keepOldX);
			vector -= this.allBones[0].localFramePositions[0];
			for (int i = 1; i < this.frameCount; i++)
			{
				this.repositionFirstRootBone(this.allBones[0].localFramePositions[i] - vector, i, keepOldX, keepOldY, keepOldX);
			}
			return this;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x0000420C File Offset: 0x0000240C
		private void repositionFirstRootBone(Vector3 localPosition, int frame, bool keepOldX, bool keepOldY, bool keepOldZ)
		{
			Vector3 b;
			if (frame == -1)
			{
				if (keepOldX)
				{
					localPosition.x = this.allBones[0].localRestPosition.x;
				}
				if (keepOldY)
				{
					localPosition.y = this.allBones[0].localRestPosition.y;
				}
				if (keepOldZ)
				{
					localPosition.z = this.allBones[0].localRestPosition.z;
				}
				b = this.allBones[0].localRestPosition - localPosition;
				this.allBones[0].localRestPosition = localPosition;
			}
			else
			{
				if (this.allBones[0].localFramePositions == null)
				{
					this.makeLocalFramePositionsArray(0);
				}
				if (keepOldX)
				{
					localPosition.x = this.allBones[0].localFramePositions[frame].x;
				}
				if (keepOldY)
				{
					localPosition.y = this.allBones[0].localFramePositions[frame].y;
				}
				if (keepOldZ)
				{
					localPosition.z = this.allBones[0].localFramePositions[frame].z;
				}
				b = this.allBones[0].localFramePositions[frame] - localPosition;
				this.allBones[0].localFramePositions[frame] = localPosition;
			}
			for (int i = 1; i < this.allBones.Length; i++)
			{
				if (this.allBones[i].parentBoneIndex == -1)
				{
					if (frame == -1)
					{
						BVH.BVHBone[] array = this.allBones;
						int num = i;
						array[num].localRestPosition = array[num].localRestPosition - b;
					}
					else
					{
						if (this.allBones[i].localFramePositions == null)
						{
							this.makeLocalFramePositionsArray(i);
						}
						this.allBones[i].localFramePositions[frame] -= b;
					}
				}
			}
		}

		/// <summary>Returns the forward direction calculated from the position of the first root bone at the given frames (the default from 0 to -1 means the first and last frames are used). Vector3.forward is returned if the root bone never moves or if fromFrame is the same as toFrame. This method is used once at the end of the constructor to determine the animation's rotation. Note: The returned Vector3 from this method might not be the same as the returned Vector3 from getAnimationForward().</summary>
		// Token: 0x0600001E RID: 30 RVA: 0x0000440C File Offset: 0x0000260C
		public Vector3 calculateForwardDirectionOfAnimation(int fromFrame = 0, int toFrame = -1)
		{
			this.fixFromFrameAndToFrame(ref fromFrame, ref toFrame);
			if (this.allBones[0].localFramePositions == null || fromFrame == toFrame)
			{
				return Vector3.forward;
			}
			Vector3 normalized = (this.allBones[0].localFramePositions[toFrame] - this.allBones[0].localFramePositions[fromFrame]).normalized;
			if (normalized.magnitude >= 0.5f)
			{
				return normalized;
			}
			return Vector3.forward;
		}

		/// <summary>Rotates the whole animation so its new rotation becomes the given one. If you want to just change the definition of what should be considered the animation's rotation use feedKnownRotation() instead. (Returns this BVH instead of void, for chaining.) Note: This is a shortcut for myBvh.rotateAnimationBy(Quaternion.Inverse(myBvh.getAnimationRotation()) * newRotation, false, false);</summary>
		// Token: 0x0600001F RID: 31 RVA: 0x000021A1 File Offset: 0x000003A1
		public BVH setAnimationRotation(Quaternion newRotation)
		{
			return this.rotateAnimationBy(Quaternion.Inverse(this.animationRotation) * newRotation, false, false);
		}

		/// <summary>Returns the current rotation of the Animation. It is decided in the constructor by calculateForwardDirectionOfAnimation(). The rotation can change by calling setAnimationRotation(), rotateAnimationBy(), align() or normalize(). It can also be redefined via feedKnownForward() or feedKnownRotation(), those two does not change anything in the BVH but simply sets the animationRotation directly - redefining what should be considered up/down/left/right.</summary>
		// Token: 0x06000020 RID: 32 RVA: 0x000021BC File Offset: 0x000003BC
		public Quaternion getAnimationRotation()
		{
			return this.animationRotation;
		}

		/// <summary>The forward direction of the animation. Note: This is a shortcut for myBvh.getAnimationRotation()*Vector3.forward</summary>
		// Token: 0x06000021 RID: 33 RVA: 0x000021C4 File Offset: 0x000003C4
		public Vector3 getAnimationForward()
		{
			return this.animationRotation * Vector3.forward;
		}

		/// <summary>The right direction of the animation. Note: This is a shortcut for myBvh.getAnimationRotation()*Vector3.right</summary>
		// Token: 0x06000022 RID: 34 RVA: 0x000021D6 File Offset: 0x000003D6
		public Vector3 getAnimationRight()
		{
			return this.animationRotation * Vector3.right;
		}

		/// <summary>The up direction of the animation. Note: This is a shortcut for myBvh.getAnimationRotation()*Vector3.up</summary>
		// Token: 0x06000023 RID: 35 RVA: 0x000021E8 File Offset: 0x000003E8
		public Vector3 getAnimationUp()
		{
			return this.animationRotation * Vector3.up;
		}

		/// <summary>Restricts all root bones from moving forward/back from their rest positions while still being able to move in the relative up/down/left/right directions. (Returns this BVH instead of void, for chaining.) Note: This is a shortcut for myBvh.flattenAnimation(myBvh.getAnimationForward(), 0, -1);</summary>
		// Token: 0x06000024 RID: 36 RVA: 0x000021FA File Offset: 0x000003FA
		public BVH flattenAnimationForward()
		{
			return this.flattenAnimation(this.getAnimationForward(), 0, -1);
		}

		/// <summary>Restricts all root bones from moving left/right from their rest positions while still being able to move in the relative forward/back/up/down directions. (Returns this BVH instead of void, for chaining.) Note: This is a shortcut for myBvh.flattenAnimation(myBvh.getAnimationRight(), 0, -1);</summary>
		// Token: 0x06000025 RID: 37 RVA: 0x0000220A File Offset: 0x0000040A
		public BVH flattenAnimationRight()
		{
			return this.flattenAnimation(this.getAnimationRight(), 0, -1);
		}

		/// <summary>Restricts all root bones from moving up/down from their rest positions while still being able to move in the relative forward/back/left/right directions. (Returns this BVH instead of void, for chaining.) Note: This is a shortcut for myBvh.flattenAnimation(myBvh.getAnimationUp(), 0, -1);</summary>
		// Token: 0x06000026 RID: 38 RVA: 0x0000221A File Offset: 0x0000041A
		public BVH flattenAnimationUp()
		{
			return this.flattenAnimation(this.getAnimationUp(), 0, -1);
		}

		/// <summary>Restricts all root bones from moving from their rest positions on the given axis. Affects all frames unless you change fromFrame/toFrame. (Returns this BVH instead of void, for chaining.) Tip: Giving a direction is okay, movement will just be restricted in the reverse direction as well. You can multiply a Quaternion with the animation's forward to get the desired axis: myBvh.flattenAnimation(myQuaternion*myBvh.getAnimationForward(), 0, -1);</summary>
		// Token: 0x06000027 RID: 39 RVA: 0x00004494 File Offset: 0x00002694
		public BVH flattenAnimation(Vector3 forbiddenAxis, int fromFrame = 0, int toFrame = -1)
		{
			this.fixFromFrameAndToFrame(ref fromFrame, ref toFrame);
			for (int i = 0; i < this.boneCount; i++)
			{
				if (this.allBones[i].parentBoneIndex == -1 && this.allBones[i].localFramePositions != null)
				{
					for (int j = fromFrame; j <= toFrame; j++)
					{
						Vector3 vector = this.allBones[i].localFramePositions[j] - this.allBones[i].localRestPosition;
						float d = vector.x * forbiddenAxis.x + vector.y * forbiddenAxis.y + vector.z * forbiddenAxis.z;
						this.allBones[i].localFramePositions[j] = this.allBones[i].localFramePositions[j] - d * forbiddenAxis;
					}
				}
			}
			return this;
		}

		/// <summary>Restricts all root bones from moving from their rest positions on X/Y/Z. Affects all frames unless you change fromFrame/toFrame. (Returns this BVH instead of void, for chaining.)</summary>
		// Token: 0x06000028 RID: 40 RVA: 0x00004594 File Offset: 0x00002794
		public BVH flattenAnimation(bool allowX, bool allowY, bool allowZ, int fromFrame = 0, int toFrame = -1)
		{
			this.fixFromFrameAndToFrame(ref fromFrame, ref toFrame);
			for (int i = 0; i < this.boneCount; i++)
			{
				if (this.allBones[i].parentBoneIndex == -1 && this.allBones[i].localFramePositions != null)
				{
					for (int j = fromFrame; j <= toFrame; j++)
					{
						if (!allowX)
						{
							this.allBones[i].localFramePositions[j].x = this.allBones[i].localRestPosition.x;
						}
						if (!allowY)
						{
							this.allBones[i].localFramePositions[j].y = this.allBones[i].localRestPosition.y;
						}
						if (!allowZ)
						{
							this.allBones[i].localFramePositions[j].z = this.allBones[i].localRestPosition.z;
						}
					}
				}
			}
			return this;
		}

		/// <summary>Makes this BVH's animation more compatible with other BVH animations. Depends heavily on the animation having a constant direction. If the animation doesn't "go anywhere" (stays pretty much in the same place, for example someone standing still) it might be best to set flattenForward to false. If the first root bone does not move a lot between the first and last frame, or if it doesn't move in a somewhat straight line, you probably need to call feedKnownForward() or feedKnownRotation() after the BVH constructor returns. Just setting both calcFDirFromFrame and calcFDirToFrame to -1 in the constructor works well in many cases though. (Returns this BVH instead of void, for chaining.) Note: This is a shortcut for myBvh.align().center().flattenAnimationForward(); (or just myBvh.align().center(); of if flattenForward is false).</summary>
		// Token: 0x06000029 RID: 41 RVA: 0x0000222A File Offset: 0x0000042A
		public BVH normalize(bool flattenForward = true)
		{
			if (flattenForward)
			{
				return this.align().center(false, false, false).flattenAnimationForward();
			}
			return this.align().center(false, false, false);
		}

		/// <summary>Rotates the whole animation so its forward direction becomes Vector3.forward. (Returns this BVH instead of void, for chaining.) Note: This is a shortcut for myBvh.rotateAnimationBy(Quaternion.Inverse(myBvh.getAnimationRotation()) * Quaternion.LookRotation(Vector3.forward, Vector3.up), false, false);</summary>
		// Token: 0x0600002A RID: 42 RVA: 0x00002251 File Offset: 0x00000451
		public BVH align()
		{
			return this.rotateAnimationBy(Quaternion.Inverse(this.animationRotation) * Quaternion.LookRotation(Vector3.forward, Vector3.up), false, false);
		}

		/// <summary>Rotates the whole animation so it gets the given forward direction. (Returns this BVH instead of void, for chaining.) Note: This is a shortcut for myBvh.rotateAnimationBy(Quaternion.Inverse(myBvh.getAnimationRotation()) * Quaternion.LookRotation(newForwardDirection, Vector3.up), false, false);</summary>
		// Token: 0x0600002B RID: 43 RVA: 0x0000227A File Offset: 0x0000047A
		public BVH align(Vector3 newForwardDirection)
		{
			return this.rotateAnimationBy(Quaternion.Inverse(this.animationRotation) * Quaternion.LookRotation(newForwardDirection, Vector3.up), false, false);
		}

		/// <summary>Rotates the whole animation on all frames by the given amount (not frame -1 unless alsoAffectRestPose is true). Rotating bones in their rest pose is tricky because each bone only have a Vector3 stored for it in the BVH format, no Quaternion. Same with end positions. So frame -1 can really only be repositioned, not rotated. And if the rest positions change all bones will have to store their positions for all their frames from now on, not just rotations as is usually the case. Rotating the rest pose will also always makes bone ends incorrect for either frame -1 or all other frames because no rotational or positional data about the ending can be stored for each frame (since it's not an actual bone). Set alsoAffectRestPoseBoneEnds to true to have bone ends correct in the rest pose, set it to false to have bone ends correct in the animation. If alsoAffectRestPose is false it doesn't matter, then this method becomes pretty light because it only rotates the root bones on all their frames except -1, using the skeleton origin as pivot point. (Returns this BVH instead of void, for chaining.) Remember that you can rotate the transform of the skeletonGO returned by makeSkeleton()/makeDebugSkeleton() as well, without needing to modify the BVH instance. Tip: You can use Quaternion.Euler(degX, degY, degZ); to create a rotation.</summary>
		// Token: 0x0600002C RID: 44 RVA: 0x000046A4 File Offset: 0x000028A4
		public BVH rotateAnimationBy(Quaternion amount, bool alsoAffectRestPose = false, bool alsoAffectRestPoseBoneEnds = false)
		{
			Matrix4x4 lhs = Matrix4x4.TRS(Vector3.zero, amount, Vector3.one);
			this.animationRotation *= amount;
			if (alsoAffectRestPose)
			{
				for (int i = 0; i < this.boneCount; i++)
				{
					if (this.allBones[i].localFramePositions == null)
					{
						this.makeLocalFramePositionsArray(i);
					}
				}
			}
			for (int j = 0; j < this.frameCount; j++)
			{
				for (int k = 0; k < this.boneCount; k++)
				{
					if (this.allBones[k].parentBoneIndex == -1)
					{
						Matrix4x4 matrix4x = lhs * this.allBones[k].getWorldMatrix(ref this.allBones, j);
						Vector3 vector = new Vector3(matrix4x.m03, matrix4x.m13, matrix4x.m23);
						if (alsoAffectRestPose || vector != this.allBones[k].localRestPosition)
						{
							if (this.allBones[k].localFramePositions == null)
							{
								this.makeLocalFramePositionsArray(k);
							}
							this.allBones[k].localFramePositions[j] = vector;
						}
						this.allBones[k].localFrameRotations[j] = Quaternion.LookRotation(new Vector3(matrix4x.m02, matrix4x.m12, matrix4x.m22), new Vector3(matrix4x.m01, matrix4x.m11, matrix4x.m21));
					}
				}
			}
			if (alsoAffectRestPose)
			{
				Vector3[] array = new Vector3[this.boneCount];
				Vector3[] array2 = new Vector3[this.boneCount];
				for (int l = 0; l < this.boneCount; l++)
				{
					Matrix4x4 worldMatrix = this.allBones[l].getWorldMatrix(ref this.allBones, -1);
					Matrix4x4 matrix4x2 = lhs * worldMatrix;
					array[l] = new Vector3(matrix4x2.m03, matrix4x2.m13, matrix4x2.m23);
					if (alsoAffectRestPoseBoneEnds && this.allBones[l].endPosition.sqrMagnitude != 0f)
					{
						worldMatrix.m03 += this.allBones[l].endPosition.x;
						worldMatrix.m13 += this.allBones[l].endPosition.y;
						worldMatrix.m23 += this.allBones[l].endPosition.z;
						matrix4x2 = lhs * worldMatrix;
						array2[l] = new Vector3(matrix4x2.m03, matrix4x2.m13, matrix4x2.m23);
					}
				}
				for (int m = 0; m < this.boneCount; m++)
				{
					if (this.allBones[m].parentBoneIndex == -1)
					{
						this.allBones[m].localRestPosition = array[m];
					}
					else
					{
						this.allBones[m].localRestPosition = array[m] - array[this.allBones[m].parentBoneIndex];
					}
					if (alsoAffectRestPoseBoneEnds && this.allBones[m].endPosition.sqrMagnitude != 0f)
					{
						this.allBones[m].endPosition = array2[m] - array[m];
					}
				}
			}
			return this;
		}

		/// <summary>Removes a frame from the animation (the default 0 removes the first frame). The given "f" works like a "fromFrame", meaning that -1 does NOT mean the rest pose, it means the last frame. Remember that if you want to keep the first frame in the BVH instance but skip it in the AnimationClip you can call myBvh.makeAnimationClip(1). (Returns this BVH instead of void, for chaining.) Note: This is a shortcut for myBvh.removeFrames(f, f);</summary>
		// Token: 0x0600002D RID: 45 RVA: 0x0000229F File Offset: 0x0000049F
		public BVH removeFrame(int f = 0)
		{
			return this.removeFrames(f, f);
		}

		/// <summary>Removes one or more frames from the animation. From 0 to 9 would remove the first 10 frames. From -10 to -1 would remove the last 10 frames. From 0 to -1 would clear everything, causing makeAnimationClip() to stop working. (Returns this BVH instead of void, for chaining.)</summary>
		// Token: 0x0600002E RID: 46 RVA: 0x00004A34 File Offset: 0x00002C34
		public BVH removeFrames(int fromFrame, int toFrame)
		{
			this.fixFromFrameAndToFrame(ref fromFrame, ref toFrame);
			for (int i = toFrame; i >= fromFrame; i--)
			{
				if (this.frameCount > 0)
				{
					for (int j = 0; j < this.boneCount; j++)
					{
						for (int k = i + 1; k < this.frameCount; k++)
						{
							this.allBones[j].localFrameRotations[k - 1] = this.allBones[j].localFrameRotations[k];
							if (this.allBones[j].localFramePositions != null)
							{
								this.allBones[j].localFramePositions[k - 1] = this.allBones[j].localFramePositions[k];
							}
						}
					}
					this.frameCount--;
				}
			}
			return this;
		}

		/// <summary>Removes all frames except the ones you specify. From 10 to -11 would remove the first 10 frames and the last 10 frames. From 0 to 0 would leave only one frame left, causing makeAnimationClip() to not produce a healthy AnimationClip. (Returns this BVH instead of void, for chaining.)</summary>
		// Token: 0x0600002F RID: 47 RVA: 0x000022A9 File Offset: 0x000004A9
		public BVH removeFramesExcept(int fromFrame, int toFrame)
		{
			this.fixFromFrameAndToFrame(ref fromFrame, ref toFrame);
			if (toFrame < this.frameCount - 1)
			{
				this.removeFrames(toFrame + 1, this.frameCount - 1);
			}
			if (fromFrame > 0)
			{
				this.removeFrames(0, fromFrame - 1);
			}
			return this;
		}

		/// <summary>Puts together a brand new .bvh file from the current state of the bones and saves it. See each parameter description for full information. (Returns this BVH instead of void, for chaining.) Note 1: The rotation order in the CHANNELS are always written as "Zrotation Xrotation Yrotation". Note 2: Bones that should get a "End Site"-block but has a endPosition equal to Vector3.zero will get "0 0.5 0" as its End Site OFFSET. Note 3: The "Frame Time" gets max 7 digits to the right of "." with no rounding. Other floats may have any number of digits to the right of ".". Note 4: A number may not have any "." at all (zero is "0", not "0.0") but there is always at least one digit on each side of "." if it's there (".123" format is not used). Note 5: Degrees goes from -180.0 to 179.999...</summary>
		/// <returns>This BVH instance, for possible chaining of method calls on the same line.</returns>
		/// <param name="pathToNewBvhFile">Path pointing to a location inside your computer's filesystem where the ASCII text file will end up. Note: Will always write to "X.#.tmp" and then move it into place upon successful flushing (X is the given pathToNewBvhFile and # is a 8 digit random number).</param>
		/// <param name="overwrite">This method throws an IOException if the file already exists and overwrite is false.</param>
		/// <param name="useTabs">If false two spaces will be used instead of a tab in the HIERARCHY section of the .bvh file.</param>
		// Token: 0x06000030 RID: 48 RVA: 0x000022E2 File Offset: 0x000004E2
		public BVH writeToDisk(string pathToNewBvhFile, bool overwrite = false, bool useTabs = true)
		{
			Debug.Log("### Sorry, writeToDisk() isn't available in the demo.");
			return this;
		}

		/// <summary>Returns the names of all bones, in the order they are listed in the allBones[] array.</summary>
		// Token: 0x06000031 RID: 49 RVA: 0x00004B18 File Offset: 0x00002D18
		public string[] getBoneNames()
		{
			string[] array = new string[this.boneCount];
			for (int i = 0; i < this.boneCount; i++)
			{
				array[i] = this.allBones[i].getName();
			}
			return array;
		}

		/// <summary>Sets the names of all bones, in the order they are listed in the allBones[] array. Assumes that boneNames.Length==boneCount. (Returns this BVH instead of void, for chaining.) Tip: Use getBoneNames() to get a compatible array, change the name(s) you want to change and then give the modified array to setBoneNames().</summary>
		// Token: 0x06000032 RID: 50 RVA: 0x00004B58 File Offset: 0x00002D58
		public BVH setBoneNames(string[] boneNames)
		{
			for (int i = 0; i < this.boneCount; i++)
			{
				if (this.allBones[i].parentBoneIndex == -1)
				{
					this.allBones[i].relativePath = boneNames[i];
				}
				else
				{
					this.allBones[i].relativePath = this.allBones[this.allBones[i].parentBoneIndex].relativePath + "/" + boneNames[i];
				}
			}
			return this;
		}

		/// <summary>Replaces bone name [i] with bone name [i+1]. Letter case is ignored, otherwise the name must match exact to be replaced. Assumes that boneNamePairs.Length%2==0, in other words the given array should be divisible by two. If allowDuplicateBoneNames is false an IOException will be thrown if two bones would end up with the same name, otherwise its not regarded as an issue and is ignored. (Returns this BVH instead of void, for chaining.) Example: myBvh.replaceBoneNames(new string[] {"lclavicle", "LeftShoulder", "neck", "Head", "head", "Neck"}); Here the bone named "LClavicle" will be renamed to "LeftShoulder" and the bones "Neck" and "Head" will swap their names (notice that the already changed "Neck" will not get changed back even though it matches "Head" afterwards). The strings in lower-case are the case-insensitive strings that will be compared to the already existing bone names.</summary>
		// Token: 0x06000033 RID: 51 RVA: 0x00004BE0 File Offset: 0x00002DE0
		public BVH replaceBoneNames(string[] boneNamePairs, bool allowDuplicateBoneNames = false)
		{
			string[] boneNames = this.getBoneNames();
			string[] array = new string[this.boneCount];
			for (int i = 0; i < this.boneCount; i++)
			{
				array[i] = boneNames[i].ToUpperInvariant();
			}
			for (int j = 0; j < boneNamePairs.Length; j += 2)
			{
				string b = boneNamePairs[j].ToUpperInvariant();
				for (int k = 0; k < this.boneCount; k++)
				{
					if (array[k] == b)
					{
						boneNames[k] = "\rT8M2P\n" + boneNamePairs[j + 1];
					}
				}
			}
			for (int l = 0; l < this.boneCount; l++)
			{
				boneNames[l] = boneNames[l].Replace("\rT8M2P\n", "");
			}
			if (!allowDuplicateBoneNames)
			{
				for (int m = 0; m < this.boneCount; m++)
				{
					string b2 = boneNames[m].ToUpperInvariant();
					for (int n = 0; n < this.boneCount; n++)
					{
						if (m != n && boneNames[n].ToUpperInvariant() == b2)
						{
							throw new IOException("Bone name \"" + boneNames[n] + "\" would exist more than once after replaceBoneNames() returns and allowDuplicateBoneNames is false!");
						}
					}
				}
			}
			this.setBoneNames(boneNames);
			return this;
		}

		// Token: 0x04000001 RID: 1
		private static readonly int[] debugMeshFaces = new int[]
		{
			0,
			1,
			2,
			0,
			2,
			3,
			3,
			2,
			5,
			3,
			5,
			4,
			5,
			2,
			1,
			5,
			1,
			6,
			3,
			4,
			7,
			3,
			7,
			0,
			0,
			7,
			6,
			0,
			6,
			1,
			4,
			5,
			6,
			4,
			6,
			7
		};

		// Token: 0x04000002 RID: 2
		private static string asmbly = "Assembly-CSharp";

		/// <summary>This build: BvhImporterExporterDemo 1.1.0 (Winterdust, Sweden)</summary>
		// Token: 0x04000003 RID: 3
		public const string VERSION = "BvhImporterExporterDemo 1.1.0 (Winterdust, Sweden)";

		/// <summary>The path that was given to the constructor accepting "pathToBvhFile". Is null if the other constructor was used.</summary>
		// Token: 0x04000004 RID: 4
		public string pathToBvhFileee;

		/// <summary>The alias of this BVH instance. If the constructor accepting "pathToBvhFile" was used then the alias is the file name, excluding extension. If the other constructor was used it is simply set to bvh_X_Y, where X is bvhFile.Length and Y is the sum of all string lengths inside the bvhFile array. Any new AnimationClip created from this BVH instance will get its name from this string, you are free to change the alias to something else if you want to.</summary>
		// Token: 0x04000005 RID: 5
		public string alias;

		/// <summary>All bones and their data. The first (and often only) root bone is always at index 0. A child bone will always have a higher index than its parent (if you modify the array manually make sure to follow this rule). Important: bones.Length can't be trusted, use boneCount instead.</summary>
		// Token: 0x04000006 RID: 6
		public BVH.BVHBone[] allBones; // this.BVH.BVHBone[]

		/// <summary>The actual number of bones. Can be lower or equal to bones.Length.</summary>
		// Token: 0x04000007 RID: 7
		public int boneCount;

		/// <summary>Frame rate of the animation. You can change this before calling makeAnimationClip(), a higher number will slow down the animation. Use getFPS() and setFPS() if that measurement is more comfortable. Note: Stored as a double for better accuracy during calculations but any created animation will still have float time values. Also: Lowering frame rate doesn't remove any frame data, if your goal is to produce less heavy AnimationClips it's better to reduce importPercentage in the constructor.</summary>
		// Token: 0x04000008 RID: 8
		public double secondsPerFrame;

		/// <summary>Number of frames in the animation. The first frame is "0" so if you want to access the last frame of the animation you should use "frameCount-1". There is also an extra "frame" outside the animation, frame "-1", which is the rest pose of the skeleton where no bone has any rotation. This "rest pose frame" is NOT counted into frameCount.</summary>
		// Token: 0x04000009 RID: 9
		public int frameCount;

		// Token: 0x0400000A RID: 10
		private Quaternion animationRotation;

		/// <summary>Representation of a bone in the .bvh file. Together they make up a skeleton. Usually there is only one root bone but more are allowed in the format. Note: The X values of positions are flipped here compared to how they were stored in the .bvh file (e.g. -100 becomes 100). If myBvh.writeToDisk() is used they are flipped back in the new .bvh file.</summary>
		// Token: 0x02000003 RID: 3
		public struct BVHBone
		{
			/// <summary>Creates a brand new BVHBone instance, copies all the data from THIS instance over to the new one and returns the new BVHBone instance. Changes to the old instance will not affect the new one and vice versa.</summary>
			// Token: 0x06000035 RID: 53 RVA: 0x00004D08 File Offset: 0x00002F08
			public BVH.BVHBone duplicate()
			{
				BVH.BVHBone bvhbone = default(BVH.BVHBone);
				bvhbone.relativePath = this.relativePath;
				bvhbone.parentBoneIndex = this.parentBoneIndex;
				bvhbone.localRestPosition = this.localRestPosition;
				bvhbone.endPosition = this.endPosition;
				if (this.localFramePositions != null)
				{
					bvhbone.localFramePositions = new Vector3[this.localFramePositions.Length];
					for (int i = 0; i < this.localFramePositions.Length; i++)
					{
						bvhbone.localFramePositions[i] = this.localFramePositions[i];
					}
				}
				if (this.localFrameRotations != null)
				{
					bvhbone.localFrameRotations = new Quaternion[this.localFrameRotations.Length];
					for (int j = 0; j < this.localFrameRotations.Length; j++)
					{
						bvhbone.localFrameRotations[j] = this.localFrameRotations[j];
					}
				}
				bvhbone.channels = this.channels;
				return bvhbone;
			}

			/// <summary>Returns the representation of this bone's position and rotation in the (skeleton) world space.</summary>
			// Token: 0x06000036 RID: 54 RVA: 0x00004DF0 File Offset: 0x00002FF0
			public Matrix4x4 getWorldMatrix(ref BVH.BVHBone[] allBones, int frame)
			{
				if (frame == -1)
				{
					Matrix4x4 matrix4x = Matrix4x4.TRS(this.localRestPosition, Quaternion.identity, Vector3.one);
					if (this.parentBoneIndex == -1)
					{
						return matrix4x;
					}
					return allBones[this.parentBoneIndex].getWorldMatrix(ref allBones, frame) * matrix4x;
				}
				else
				{
					Matrix4x4 matrix4x2 = Matrix4x4.TRS((this.localFramePositions == null) ? this.localRestPosition : this.localFramePositions[frame], this.localFrameRotations[frame], Vector3.one);
					if (this.parentBoneIndex == -1)
					{
						return matrix4x2;
					}
					return allBones[this.parentBoneIndex].getWorldMatrix(ref allBones, frame) * matrix4x2;
				}
			}

			/// <summary>Defines the "channels"-field of this BVHBone using bitwise operations. Input is the whole "CHANNELS"-line in the .bvh file. Three bits per "slot", 7 slots used. Meaning of values in the first six slot (bits 1-18 counted from the right): int1=Xrotation, int2=Yrotation, int3=Zrotation, int4=Xposition, int5=Yposition, int6=Zposition. Slot seven (bits 19-21 counted from the right) contains an integer that equals the number of channels (how many of the first six slots that are used). Return value is the number of channels used by this bone.</summary>
			// Token: 0x06000037 RID: 55 RVA: 0x00004E98 File Offset: 0x00003098
			public int defineChannels(ref string line)
			{
				string[] array = line.Split(new char[]
				{
					' '
				});
				this.channels |= array.Length - 2 << 18;
				for (int i = 2; i < array.Length; i++)
				{
					if (array[i] == "Xrotation")
					{
						this.channels |= 1 << 3 * (i - 2);
					}
					else if (array[i] == "Yrotation")
					{
						this.channels |= 2 << 3 * (i - 2);
					}
					else if (array[i] == "Zrotation")
					{
						this.channels |= 3 << 3 * (i - 2);
					}
					else if (array[i] == "Xposition")
					{
						this.channels |= 4 << 3 * (i - 2);
					}
					else if (array[i] == "Yposition")
					{
						this.channels |= 5 << 3 * (i - 2);
					}
					else if (array[i] == "Zposition")
					{
						this.channels |= 6 << 3 * (i - 2);
					}
				}
				return array.Length - 2;
			}

			/// <summary>Defines the "localRestPosition"-field of this BVHBone.
			// Input is the whole "OFFSET"-line in the .bvh file (the one above the "CHANNELS"-line).</summary>
			// Token: 0x06000038 RID: 56 RVA: 0x00004FDC File Offset: 0x000031DC

			//MJ: called by this.allBones[num10].defineLocalRestPosition(ref text3, ref zUp);
			public void defineLocalRestPosition(ref string line, ref bool zUp) // Set the offset vector in Unity coordinate system
			{
				string[] array = line.Split(new char[]
				{
					' '
				});
				this.localRestPosition.x = float.Parse(array[1]) * -1f;
				if (zUp)
				{
					this.localRestPosition.z = float.Parse(array[2]) * -1f;
					this.localRestPosition.y = float.Parse(array[3]);
					return;
				}
				this.localRestPosition.y = float.Parse(array[2]);
				this.localRestPosition.z = float.Parse(array[3]);
			}

			///// <summary>Defines the "endPosition"-field of this BVHBone. 
			// Input is the whole "OFFSET"-line in the .bvh file (the one inside the "End Site"-block).</summary>
			// Token: 0x06000039 RID: 57 RVA: 0x0000506C File Offset: 0x0000326C

			//MJ:  called by this.allBones[num10].defineEndPosition(ref text3, ref zUp);
			public void defineEndPosition(ref string line, ref bool zUp)
			{
				string[] array = line.Split(new char[]
				{
					' '
				});
				this.endPosition.x = float.Parse(array[1]) * -1f;
				if (zUp)
				{
					this.endPosition.z = float.Parse(array[2]) * -1f;
					this.endPosition.y = float.Parse(array[3]);
					return;
				}
				this.endPosition.y = float.Parse(array[2]);
				this.endPosition.z = float.Parse(array[3]);
			}

			/// <summary>Returns the bone's tail, calculated from its children. The position is counted from localRestPosition (from this bone's origin - NOT the parent's origin). If the bone has one child the child's localRestPosition is returned. If the bone has more than one child the average of all the children's localRestPosition is returned. If the bone has no child and it has been given an end position the endPosition is returned. Otherwise "new Vector3(0, 0.5f, 0);" is returned (half of Vector3.up). Note: Vector3.zero can be returned if the bone's child has offset 0,0,0 (meaning the same position as its parent).</summary>
			// Token: 0x0600003A RID: 58 RVA: 0x000050FC File Offset: 0x000032FC
			public Vector3 calculateTail(ref BVH.BVHBone[] allBones, int myOwnBoneIndex)
			{
				int[] array = this.findChildBoneIndexes(ref allBones, myOwnBoneIndex);
				if (array.Length == 1)
				{
					return allBones[array[0]].localRestPosition;
				}
				if (array.Length != 0)
				{
					Vector3 a = Vector3.zero;
					for (int i = 0; i < array.Length; i++)
					{
						a += allBones[array[i]].localRestPosition;
					}
					return a / (float)array.Length;
				}
				if (this.endPosition.sqrMagnitude != 0f)
				{
					return this.endPosition;
				}
				return new Vector3(0f, 0.5f, 0f);
			}

			/// <summary>Used to parse the motion data (this is not called if parseMotionData is false in the constructor). The frameData[] array is one of the lines containing only floats inside the MOTION section of the .bvh file.</summary>
			// Token: 0x0600003B RID: 59 RVA: 0x00005190 File Offset: 0x00003390
			public void feedFrame(ref int frameDataIndex, ref float[] frameData, ref int frameNumber, ref bool zUp)
			{
				Quaternion quaternion = Quaternion.identity;
				if (zUp)
				{
					if (this.channels == 786571)
					{
						if (this.localFramePositions != null)
						{
							this.localFramePositions[frameNumber].x = this.localRestPosition.x;
							this.localFramePositions[frameNumber].y = this.localRestPosition.y;
							this.localFramePositions[frameNumber].z = this.localRestPosition.z;
						}
						quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex], Vector3.up);
						quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex + 1], Vector3.right);
						quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex + 2], Vector3.back);
						frameDataIndex += 3;
					}
					else if (this.channels == 1644460)
					{
						if (this.localFramePositions != null)
						{
							this.localFramePositions[frameNumber].x = frameData[frameDataIndex] * -1f;
							this.localFramePositions[frameNumber].z = frameData[frameDataIndex + 1] * -1f;
							this.localFramePositions[frameNumber].y = frameData[frameDataIndex + 2];
						}
						quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex + 3], Vector3.up);
						quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex + 4], Vector3.right);
						quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex + 5], Vector3.back);
						frameDataIndex += 6;
					}
					else
					{
						if (this.localFramePositions != null)
						{
							this.localFramePositions[frameNumber].x = this.localRestPosition.x;
							this.localFramePositions[frameNumber].y = this.localRestPosition.y;
							this.localFramePositions[frameNumber].z = this.localRestPosition.z;
						}
						int num = (this.channels & 1835008) >> 18;
						for (int i = 0; i < num; i++)
						{
							int num2 = (this.channels & 7 << 3 * i) >> 3 * i;
							if (num2 == 1)
							{
								quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex], Vector3.right);
							}
							else if (num2 == 2)
							{
								quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex], Vector3.back);
							}
							else if (num2 == 3)
							{
								quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex], Vector3.up);
							}
							else if (this.localFramePositions != null)
							{
								if (num2 == 4)
								{
									this.localFramePositions[frameNumber].x = frameData[frameDataIndex] * -1f;
								}
								else if (num2 == 5)
								{
									this.localFramePositions[frameNumber].z = frameData[frameDataIndex] * -1f;
								}
								else if (num2 == 6)
								{
									this.localFramePositions[frameNumber].y = frameData[frameDataIndex];
								}
							}
							frameDataIndex++;
						}
					}
				}
				else if (this.channels == 786571)
				{
					if (this.localFramePositions != null)
					{
						this.localFramePositions[frameNumber].x = this.localRestPosition.x;
						this.localFramePositions[frameNumber].y = this.localRestPosition.y;
						this.localFramePositions[frameNumber].z = this.localRestPosition.z;
					}
					quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex], Vector3.forward);
					quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex + 1], Vector3.right);
					quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex + 2], Vector3.up);
					frameDataIndex += 3;
				}
				else if (this.channels == 1644460)
				{
					if (this.localFramePositions != null)
					{
						this.localFramePositions[frameNumber].x = frameData[frameDataIndex] * -1f;
						this.localFramePositions[frameNumber].y = frameData[frameDataIndex + 1];
						this.localFramePositions[frameNumber].z = frameData[frameDataIndex + 2];
					}
					quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex + 3], Vector3.forward);
					quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex + 4], Vector3.right);
					quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex + 5], Vector3.up);
					frameDataIndex += 6;
				}
				else
				{
					if (this.localFramePositions != null)
					{
						this.localFramePositions[frameNumber].x = this.localRestPosition.x;
						this.localFramePositions[frameNumber].y = this.localRestPosition.y;
						this.localFramePositions[frameNumber].z = this.localRestPosition.z;
					}
					int num3 = (this.channels & 1835008) >> 18;
					for (int j = 0; j < num3; j++)
					{
						int num4 = (this.channels & 7 << 3 * j) >> 3 * j;
						if (num4 == 1)
						{
							quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex], Vector3.right);
						}
						else if (num4 == 2)
						{
							quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex], Vector3.up);
						}
						else if (num4 == 3)
						{
							quaternion *= Quaternion.AngleAxis(frameData[frameDataIndex], Vector3.forward);
						}
						else if (this.localFramePositions != null)
						{
							if (num4 == 4)
							{
								this.localFramePositions[frameNumber].x = frameData[frameDataIndex] * -1f;
							}
							else if (num4 == 5)
							{
								this.localFramePositions[frameNumber].y = frameData[frameDataIndex];
							}
							else if (num4 == 6)
							{
								this.localFramePositions[frameNumber].z = frameData[frameDataIndex];
							}
						}
						frameDataIndex++;
					}
				}
				float x = (2f * quaternion.x * quaternion.y - 2f * quaternion.w * quaternion.z) * -1f;
				float x2 = (2f * quaternion.x * quaternion.z + 2f * quaternion.w * quaternion.y) * -1f;
				float y = 1f - 2f * quaternion.x * quaternion.x - 2f * quaternion.z * quaternion.z;
				float y2 = 2f * quaternion.y * quaternion.z - 2f * quaternion.w * quaternion.x;
				float z = 2f * quaternion.y * quaternion.z + 2f * quaternion.w * quaternion.x;
				float z2 = 1f - 2f * quaternion.x * quaternion.x - 2f * quaternion.y * quaternion.y;
				this.localFrameRotations[frameNumber] = Quaternion.LookRotation(new Vector3(x2, y2, z2), new Vector3(x, y, z));
			}

			/// <summary>Makes a GameObject that represents this bone, complete with child GameObjects that represents the bone's children. 
            /// When a skeletonGO is created the BVH instance calls makeGO() on all its root bones and makes the returned GameObjects a child of the skeletonGO
            /// (if there is only one root bone the skeletonGO will only have one child itself).</summary>
			// Token: 0x0600003C RID: 60 RVA: 0x000058B4 File Offset: 0x00003AB4
			public GameObject makeGO(ref int frame, ref bool includeBoneEnds, ref BVH.BVHBone[] allBones, int myOwnBoneIndex)
			{
				GameObject gameObject = new GameObject( this.getName() ); // this refers to allBones[array[i]]

				int[] array = this.findChildBoneIndexes(ref allBones, myOwnBoneIndex);

				for (int i = 0; i < array.Length; i++) // The parent of i-th bone is gameObject
				{
					allBones[array[i]].makeGO(ref frame, ref includeBoneEnds, ref allBones, array[i]).transform.parent = gameObject.transform;
                    // allBones[array[i]].setLocalPosRot(gameObject.transform, ref frame); // this within  setLocalPosRot() refers to allBones[array[i]]
                }

                this.setLocalPosRot(gameObject.transform, ref frame);

            


                if (includeBoneEnds && this.endPosition.sqrMagnitude != 0f)
				{
					GameObject gameObject2 = new GameObject(this.getName() + "End");
					gameObject2.transform.parent = gameObject.transform;
					gameObject2.transform.localPosition = this.endPosition;
				}
				return gameObject;
			}


            public void makeGO(ref int frame, Transform transform)
            {
                this.setLocalPosRot(transform, ref frame);
             
            }

            /// <summary>Sets localPosition/localRotation of the given bone transform (in Unity Scene hieararchy) to the bone's local position/rotation at the given frame. If frame -1 is specified the bone's rest pose is used. The rest rotation is always Quaternion.identity. If frame is &gt;=0 and the bone doesn't have any localFramePositions (the array is null) its localRestPosition is used.</summary>
            // Token: 0x06000040 RID: 64 RVA: 0x00005A24 File Offset: 0x00003C24
            public void setLocalPosRot(Transform boneTransform, ref int frame)
            {
                if (frame == -1)
                {
                    boneTransform.localPosition = this.localRestPosition; // this refers to the current  BVHBone
                    boneTransform.localRotation = Quaternion.identity;
                    return;
                }
                boneTransform.localPosition = ((this.localFramePositions != null) ? this.localFramePositions[frame] : this.localRestPosition);
                boneTransform.localRotation = this.localFrameRotations[frame];
				//the bone's local position/rotation at the given frame "frame"
            }   // public void setLocalPosRot(Transform boneTransform, ref int frame)



            /// <summary>Returns the end of this bone's relativePath; the actual name of the bone.</summary>
            // Token: 0x0600003D RID: 61 RVA: 0x00002312 File Offset: 0x00000512
            public string getName() // method of BVHBone struct
			{
				if (this.parentBoneIndex == -1)
				{
					return this.relativePath;
				}
				return this.relativePath.Substring(this.relativePath.LastIndexOf('/') + 1);
			}

			/// <summary>Goes through the given array and returns the index of the first BVHBone that has the same relativePath as this BVHBone (meaning it has found itself, as long as all bones have unique relativePaths as they should).</summary>
			// Token: 0x0600003E RID: 62 RVA: 0x00005964 File Offset: 0x00003B64
			public int findMyOwnBoneIndex(ref BVH.BVHBone[] allBones)
			{
				for (int i = 0; i < allBones.Length; i++)
				{
					if (allBones[i].relativePath == this.relativePath)
					{
						return i;
					}
				}
				return -1;
			}

			/// <summary>Goes through the given array and returns the indexes of all the child bones of this bone (will check their parentBoneIndex against the given myOwnBoneIndex). If this bone doesn't have any children an empty array is returned.</summary>
			// Token: 0x0600003F RID: 63 RVA: 0x000059A0 File Offset: 0x00003BA0
			public int[] findChildBoneIndexes(ref BVH.BVHBone[] allBones, int myOwnBoneIndex)
			{
				int[] array = null;
				int num = 0;
				while (num < allBones.Length && allBones[num].relativePath != null)
				{
					if (allBones[num].parentBoneIndex == myOwnBoneIndex)
					{
						if (array == null)
						{
							array = new int[]
							{
								num
							};
						}
						else
						{
							int[] array2 = new int[array.Length + 1];
							for (int i = 0; i < array2.Length - 1; i++)
							{
								array2[i] = array[i];
							}
							array2[array2.Length - 1] = num;
							array = array2;
						}
					}
					num++;
				}
				if (array == null)
				{
					return new int[0];
				}
				return array;
			}

			

			/// <summary>The bone's place in the skeleton. It's name is the last part of this string, you can use getName() to extract it. Important: If you ever "remove a bone" by reducing myBvh.boneCount (meaning the bone data is still in the allBones[] array) you need to set its relativePath to null! Otherwise findChildBoneIndexes() won't know the bone was removed. Example relativePath: Hips/LowerBack/Spine/Spine1/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftFingerBase/LeftHandIndex1</summary>
			// Token: 0x0400000B RID: 11
			public string relativePath;

			/// <summary>The index in myBvh.allBones that contains this bone's parent. Is -1 if this bone is a root bone.</summary>
			// Token: 0x0400000C RID: 12
			public int parentBoneIndex;

			/// <summary>The bone's beginning (head), counted from the parent's localRestPosition (the parent's origin - or the skeleton origin if there is no parent).</summary>
			// Token: 0x0400000D RID: 13
			public Vector3 localRestPosition;

			/// <summary>The bone's given end, counted from localRestPosition (from this bone's origin - NOT the parent's origin). Is Vector3.zero (has 0 magnitude/sqrMagnitude) if no end has been specified. Bones without any child bones usually have a endPosition instead. Tip: Use calculateTail() if you need to get a bone's end even if it didn't get one specified.</summary>
			// Token: 0x0400000E RID: 14
			public Vector3 endPosition;

			/// <summary>The bone's positions for each frame. If the bone does not have any positions (only rotations) this array is null. Note: Use myBvh.frameCount to determine the number of frames in an animation, localFramePositions.Length might not be reliable if for example myBvh.removeFrame() has been called.</summary>
			// Token: 0x0400000F RID: 15
			public Vector3[] localFramePositions;

			/// <summary>The bone's rotations for each frame. This array is never null. Note: Use myBvh.frameCount to determine the number of frames in an animation, localFrameRotations.Length might not be reliable if for example myBvh.removeFrame() has been called.</summary>
			// Token: 0x04000010 RID: 16
			public Quaternion[] localFrameRotations;

			/// <summary>Representation of the bone's channels in the .bvh file (the order of its frame data). Created and read using bitwise operations. See summary for defineChannels() for more info. Example: 786571 translates to "Zrotation Xrotation Yrotation" and 1644460 to "Xposition Yposition Zposition Zrotation Xrotation Yrotation" (any order is supported but these two are the most common ones and .bvh files using them gets a minor speed boost during import).</summary>
			// Token: 0x04000011 RID: 17
			public int channels;

		}   // class BVH.BVHBone

		/// <summary>Create an instance of this class and give it to the BVH constructor or myBvh.prepareAnimationClip() if you want to be able to check the progress from a different thread.</summary>
		// Token: 0x02000004 RID: 4
		public class ProgressTracker
		{
			/// <summary>Returns a formatted version of percentLoaded. Example: If percentLoaded is 0.5 this will return "50" when decimals is 0 and "50.00" when decimals is 2. Change minimumIntegers if you want a minimum length on the left side of the dot, "050.0000" would be returned if decimals are 4 and minimumIntegers is 3.</summary>
			// Token: 0x06000041 RID: 65 RVA: 0x00005A84 File Offset: 0x00003C84
			public string getPercentage(int decimals = 0, int minimumIntegers = 1)
			{
				double num = this.progress * 100.0;
				double num2 = 1.0;
				for (int i = 0; i < decimals; i++)
				{
					num2 *= 10.0;
				}
				num = Math.Floor(num * num2) / num2;
				string text = string.Concat(num).Replace(",", ".");
				if (!text.Contains("."))
				{
					text += ".0";
				}
				while (text.IndexOf(".") < minimumIntegers)
				{
					text = "0" + text;
				}
				if (decimals == 0 && text.Contains("."))
				{
					text = text.Substring(0, text.IndexOf("."));
				}
				else
				{
					while (text.Length - text.IndexOf(".") - 1 < decimals)
					{
						text += "0";
					}
				}
				return text;
			}

			/// <summary>The BVH class writes to this double while working. Is always reset to 0 when work starts. When this is 1.0 the work has finished.</summary>
			// Token: 0x04000012 RID: 18
			public double progress;
		}

		/// <summary>An AnimationClip, just not in existence yet. This can be created by any thread and then finished by Unity's main thread via the make() call. You can call make() several times to make several AnimationClips from the same mould, feel free to change stuff in-between. Note: This is used by both myBvh.prepareAnimationClip() and myBvh.makeAnimationClip().</summary>
		// Token: 0x02000005 RID: 5
		public class PreparedAnimationClip
		{
			/// <summary>Creates a brand new AnimationClip from the current fields in this class and returns it. This should only be called from Unity's main thread. EnsureQuaternionContinuity() is called on the clip right before it's returned.</summary>
			// Token: 0x06000043 RID: 67 RVA: 0x00005B68 File Offset: 0x00003D68
			public AnimationClip make()
			{
				AnimationClip animationClip = new AnimationClip();
				animationClip.name = this.name;
				animationClip.legacy = this.legacy;
				animationClip.wrapMode = this.wrapMode;
				animationClip.frameRate = this.frameRate;
				for (int i = 0; i < this.data.Length; i++)
				{
					if (this.data[i].posX != null)
					{
						animationClip.SetCurve(this.data[i].relativePath, BVH.PreparedAnimationClip.typeOfTransform, "localPosition.x", this.data[i].posX);
						animationClip.SetCurve(this.data[i].relativePath, BVH.PreparedAnimationClip.typeOfTransform, "localPosition.y", this.data[i].posY);
						animationClip.SetCurve(this.data[i].relativePath, BVH.PreparedAnimationClip.typeOfTransform, "localPosition.z", this.data[i].posZ);
					}
					if (this.data[i].rotX != null)
					{
						animationClip.SetCurve(this.data[i].relativePath, BVH.PreparedAnimationClip.typeOfTransform, "localRotation.x", this.data[i].rotX);
						animationClip.SetCurve(this.data[i].relativePath, BVH.PreparedAnimationClip.typeOfTransform, "localRotation.y", this.data[i].rotY);
						animationClip.SetCurve(this.data[i].relativePath, BVH.PreparedAnimationClip.typeOfTransform, "localRotation.z", this.data[i].rotZ);
						animationClip.SetCurve(this.data[i].relativePath, BVH.PreparedAnimationClip.typeOfTransform, "localRotation.w", this.data[i].rotW);
					}
				}
				animationClip.EnsureQuaternionContinuity();
				return animationClip;
			}

			// Token: 0x04000013 RID: 19
			private static readonly Type typeOfTransform = typeof(Transform);

			/// <summary>The AnimationClip will get this name.</summary>
			// Token: 0x04000014 RID: 20
			public string name;

			/// <summary>When false the AnimationClip will work with Mecanim (Animator component), otherwise it will be legacy (Animation component).</summary>
			// Token: 0x04000015 RID: 21
			public bool legacy;

			/// <summary>The AnimationClip will have this wrapMode.</summary>
			// Token: 0x04000016 RID: 22
			public WrapMode wrapMode;

			/// <summary>Animation speed in frames per second used by the AnimationClip.</summary>
			// Token: 0x04000017 RID: 23
			public float frameRate;

			/// <summary>The AnimationClip will be fed these during creation via its SetCurve() method. The class type of the animated component is always typeof(Transform).</summary>
			// Token: 0x04000018 RID: 24
			public BVH.PreparedAnimationClip.CurveBlock[] data;

			/// <summary>Contains all the curves that will animate a GameObject.</summary>
			// Token: 0x02000006 RID: 6
			public struct CurveBlock
			{
				/// <summary>Path pointing to the GameObject that will have its transform animated. Example: "Hips". Another example: "Hips/ToSpine/Spine/Spine1/Neck/Head".</summary>
				// Token: 0x04000019 RID: 25
				public string relativePath;

				/// <summary>Unless null this curve will animate the localPosition.x property of the GameObject's transform (part of Vector3).</summary>
				// Token: 0x0400001A RID: 26
				public AnimationCurve posX;

				/// <summary>Unless posX is null this curve will animate the localPosition.y property of the GameObject's transform (part of Vector3).</summary>
				// Token: 0x0400001B RID: 27
				public AnimationCurve posY;

				/// <summary>Unless posX is null this curve will animate the localPosition.z property of the GameObject's transform (part of Vector3).</summary>
				// Token: 0x0400001C RID: 28
				public AnimationCurve posZ;

				/// <summary>Unless null this curve will animate the localRotation.x property of the GameObject's transform (part of Quaternion).</summary>
				// Token: 0x0400001D RID: 29
				public AnimationCurve rotX;

				/// <summary>Unless rotX is null this curve will animate the localRotation.y property of the GameObject's transform (part of Quaternion).</summary>
				// Token: 0x0400001E RID: 30
				public AnimationCurve rotY;

				/// <summary>Unless rotX is null this curve will animate the localRotation.z property of the GameObject's transform (part of Quaternion).</summary>
				// Token: 0x0400001F RID: 31
				public AnimationCurve rotZ;

				/// <summary>Unless rotX is null this curve will animate the localRotation.w property of the GameObject's transform (part of Quaternion).</summary>
				// Token: 0x04000020 RID: 32
				public AnimationCurve rotW;
			}//public struct CurveBlock
		}//public class PreparedAnimationClip
	} // class BVH
}
