using System;
using UnityEngine;

namespace Winterdust
{
	/// <summary>This component is added to all debug skeletons created by the BVH class.
	// It's used to draw colored lines between all transforms and their children,
	//  except for the transform that holds this component (unless alsoDrawLinesFromOrigin is true). 
	// The "Hidden/Internal-Colored" shader is used and the lines are drawn using the GL class in OnRenderObject().</summary>
	// Token: 0x02000007 RID: 7
	public class BVHDebugLines : MonoBehaviour
	{
		// Token: 0x06000046 RID: 70 RVA: 0x00002357 File Offset: 0x00000557
		private void Awake()
		{
			if (BVHDebugLines.mat == null)
			{
				BVHDebugLines.mat = new Material(Shader.Find("Hidden/Internal-Colored"));
			}
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00005D58 File Offset: 0x00003F58
		private void OnRenderObject()
		{
			BVHDebugLines.mat.color = this.color; // refer to 	private static Material mat;
			BVHDebugLines.mat.SetInt("_ZTest", this.xray ? 0 : 4);
			BVHDebugLines.mat.SetInt("_ZWrite", this.xray ? 0 : 1);
			BVHDebugLines.mat.SetPass(0);
			GL.PushMatrix();

			// SceneObjects = this.gameObject.GetComponentsInChildren<Transform>().Where(go => go.gameObject != this.gameObject);
			//Transform[] componentsInChildren = base.transform.GetComponentsInChildren<Transform>();
			//Transform[] componentsInChildren = transform.GetComponentsInChildren<Transform>();
			Transform[] componentsInChildren = this.gameObject.transform.GetComponentsInChildren<Transform>();
			// this.gameObject == "Skeleton" GameObject; this.gameObject.transform and its children transforms are set by BVHFrameSetter

			//for (int i = (componentsInChildren[0] == base.transform) ? (this.alsoDrawLinesFromOrigin ? 0 : 1) : 0; i < componentsInChildren.Length; i++)
			
			for (int i = (componentsInChildren[0] == this.gameObject.transform) ? (this.alsoDrawLinesFromOrigin ? 0 : 1) : 0; 
			              i < componentsInChildren.Length; i++)
			{
				for (int j = 0; j < componentsInChildren[i].childCount; j++)
				{
					GL.Begin(1); // GL.Begin(mode); mode = TRIANGLES = 4;  TRIANGLE_STRIP = 5;  QUADS = 7;  LINES = 1;   LINE_STRIP = 2;
					GL.Vertex3(componentsInChildren[i].position.x, componentsInChildren[i].position.y, componentsInChildren[i].position.z);
					GL.Vertex3(componentsInChildren[i].GetChild(j).position.x, componentsInChildren[i].GetChild(j).position.y, componentsInChildren[i].GetChild(j).position.z);
					GL.End();
				}
			}
			GL.PopMatrix();
		}

		// Token: 0x04000021 RID: 33
		private static Material mat;    //  class Material : Object

        /// <summary>The color of all the lines.</summary>
        // Token: 0x04000022 RID: 34
        public Color color = Color.white;     // public struct Color : 

        /// <summary>Should the lines be visible through walls?</summary>
        // Token: 0x04000023 RID: 35
        public bool xray;

		/// <summary>When true lines will be drawn from the "root transform" to all its children as well. The "root transform" is the transform of the GameObject that has this BVHDebugLines component.</summary>
		// Token: 0x04000024 RID: 36
		public bool alsoDrawLinesFromOrigin = true;
	} // public class BVHDebugLines : MonoBehaviour
}
