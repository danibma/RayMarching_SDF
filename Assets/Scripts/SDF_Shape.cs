using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDF_Shape : MonoBehaviour
{
	public enum ShapeType { Sphere, Cube, Torus };
	public enum Operation { None, Blend, Cut }

	public ShapeType shapeType;
	public Operation operation;
	public Color colour = Color.white;
	[Range(0.01f, 1)]
	public float blendStrength;

	public Vector3 Position
	{
		get
		{
			return transform.position;
		}
	}

	public Vector3 Scale
	{
		get
		{
			return transform.localScale;
		}
	}
}