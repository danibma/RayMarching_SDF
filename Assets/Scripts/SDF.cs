using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class SDF : MonoBehaviour
{
	public ComputeShader raymarchingShader;

	RenderTexture target;
	Camera cam;
	Light lightSource;
	ComputeBuffer shapesBuffer;

	private static readonly int Source = Shader.PropertyToID("_Source");
	private static readonly int Dest = Shader.PropertyToID("_Dest");
	private static readonly int Shapes = Shader.PropertyToID("_Shapes");
	private static readonly int ShapesNum = Shader.PropertyToID("_ShapesNum");
	private static readonly int LightPos = Shader.PropertyToID("_LightPos");
	private static readonly int CameraToWorld = Shader.PropertyToID("_CameraToWorld");
	private static readonly int CameraInverseProj = Shader.PropertyToID
		("_CameraInverseProjection");
	private static readonly int Time = Shader.PropertyToID("_Time");


	private void Init()
	{
		cam = Camera.current;
		lightSource = FindObjectOfType<Light>();
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		Init();
		CreateRenderTexture();
		CreateScene();

		raymarchingShader.SetTexture(0, Source, source);
		raymarchingShader.SetTexture(0, Dest, target);
		raymarchingShader.SetBuffer(0, Shapes, shapesBuffer);
		raymarchingShader.SetInt(ShapesNum, shapesBuffer.count);

		raymarchingShader.SetVector(LightPos, lightSource.transform.position);
		raymarchingShader.SetMatrix(CameraToWorld, cam.cameraToWorldMatrix);
		raymarchingShader.SetMatrix(CameraInverseProj, cam.projectionMatrix.inverse);

		raymarchingShader.SetFloat(Time, (float)EditorApplication.timeSinceStartup);

		raymarchingShader.GetKernelThreadGroupSizes(0, out var threadNumX, out var threadNumY, out _);
		int threadGroupsX = Mathf.CeilToInt(cam.pixelWidth / threadNumX);
		int threadGroupsY = Mathf.CeilToInt(cam.pixelHeight / threadNumY);
		raymarchingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

		Graphics.Blit(target, destination);

		shapesBuffer.Dispose();

		// Force the scene view to update
		SceneView.RepaintAll();
	}

	private void CreateRenderTexture()
	{
		if (target == null || target.width != cam.pixelWidth || target.height != cam.pixelHeight)
		{
			if (target != null)
			{
				target.Release();
			}
			target = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			target.enableRandomWrite = true;
			target.Create();
		}
	}

	private void CreateScene()
	{
		List<SDF_Shape> allShapes = new List<SDF_Shape>(FindObjectsOfType<SDF_Shape>());
		var orderedShapes = allShapes.OrderBy(shape => shape.operation).ToList();

		ShapeData[] shapeData = new ShapeData[allShapes.Count];
		for (int i = 0; i < allShapes.Count; ++i)
		{
			var shape = orderedShapes[i];
			shapeData[i] = new ShapeData()
			{
				position = shape.Position,
				scale = shape.Scale,
				colour = new Vector3(shape.colour.r, shape.colour.g, shape.colour.b),
				blendStrength = shape.blendStrength,
				shapeType = (int)shape.shapeType,
				operation = (int)shape.operation,
				isAnimated = shape.isAnimated ? 1 : 0,
				animateSphereRadius = shape.animateSphereRadius ? 1 : 0,
			};
		}

		shapesBuffer = new ComputeBuffer(shapeData.Length, ShapeData.GetSize());
		shapesBuffer.SetData(shapeData);
	}

	// This has to be equal to the struct in the hlsl file
	struct ShapeData
	{
		public Vector3 position;
		public Vector3 scale;
		public Vector3 colour;
		public float blendStrength;
		public int shapeType;
		public int operation;

		public int isAnimated;
		public int animateSphereRadius;

		public static int GetSize()
		{
			return sizeof(float) * 10 + sizeof(int) * 4;
		}
	}
}
