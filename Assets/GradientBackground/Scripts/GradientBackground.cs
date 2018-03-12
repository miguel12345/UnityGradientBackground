using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MF
{

	public enum GradientType
	{
		Horizontal,
		Vertical,
		Radial,
		RadialAspectFitWidth,
		RadialAspectFitHeight,
	}

	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	public class GradientBackground : MonoBehaviour
	{
		/// <summary>
		/// The Gradient colors and times. 
		/// 
		/// Only 6 colors are allowed. If more than 6 color are set, only the first 6 will be used
		/// </summary>
		[SerializeField]
		public Gradient Gradient = new Gradient();
		
		/// <summary>
		/// The type of gradient.
		/// 
		/// The difference between Radial, RadialAspectFitWidth, and RadialAspectFitHeight is that
		/// Radial fits the screen exactly, and doesn't form a perfect circle.
		/// RadialAspectFitWidth guarantees a perfect circle and uses the screen width as the maxium radial distance
		/// RadialAspectFitWidth guarantees a perfect circle and uses the screen height as the maxium radial distance
		/// </summary>
		public GradientType GradientType;
		
		/// <summary>
		/// Should invert the default direction?
		/// By default, the horizontal gradient goes from left to right, the vertical from bottom to top
		/// and the radial from the origin center outwards
		/// </summary>
		public bool InvertDirection;
		
		/// <summary>
		/// The origin position x coordinate for the radial background in Viewport space, between 0.0 and 1.0
		/// </summary>
		[Range(0f,1f)]
		public float RadialOriginX = 0.5f;
		
		/// <summary>
		/// The origin position y coordinate for the radial background in Viewport space, between 0.0 and 1.0
		/// </summary>
		[Range(0f,1f)]
		public float RadialOriginY = 0.5f;

		/// <summary>
		/// Should bake the background gradient texture and use it instead?
		/// 
		/// By choosing to use a baked render texture, the shader runs much faster, but it will use more memory.
		/// If you are planning to animate the background gradient, set this to false, run the entire animation,
		/// and set it to true once the animation has finished.  
		/// </summary>
		public bool UseBakedRenderTexture;

		private Camera _ownCamera;
		private CommandBuffer _commandBuffer;
		private Mesh _fullScreenMesh;
		
		[SerializeField]
		private Material _backgroundGradientMaterial;

		private int _shaderGradientColorsPropertyId;
		private int _shaderGradientTimesPropertyId;
		private int _shaderGradientColorsSizePropertyId;
		private int _shaderScreenRatioWidthDividedByHeightPropertyId;
		private int _shaderScreenRatioHeightDividedByWidthPropertyId;
		private int _shaderGradientOriginPropertyId;
		private int _shaderInvertDirectionPropertyId;

		private const int MaximumColors = 6;
		private readonly Color[] _colorArray = new Color[MaximumColors];
		private readonly float[] _timeArray = new float[MaximumColors];
		private CameraClearFlags _originalCameraClearFlags;

		[NonSerialized] private RenderTexture _backgroundBakeRenderTexture;
		[NonSerialized] private bool _awakeCalled;
		[NonSerialized] private bool _isDirty;
		[NonSerialized] private CommandBuffer _bakeRenderTextureCommandBuffer;
		[SerializeField]
		private Material _backgroundRenderBakedTextureMaterial;
		
		[SerializeField,HideInInspector]
		private bool _resetCalled;


		/// <summary>
		/// Set the gradient as dirty, forcing a material update and ,if applicable, a rebaking.
		/// This method should be called after changing any of the public fields.
		/// </summary>
		public void SetDirty()
		{
			_isDirty = true;
		}


		private void RebakeRenderTexture()
		{
			if (_backgroundBakeRenderTexture == null){
				_backgroundBakeRenderTexture = new RenderTexture(Screen.width, Screen.height, 0, GetRenderTextureFormat());
			}

			if (_bakeRenderTextureCommandBuffer == null) 
				CreateBakeRenderTextureCommandBuffer();

			_backgroundRenderBakedTextureMaterial.mainTexture = _backgroundBakeRenderTexture;

			Graphics.ExecuteCommandBuffer(_bakeRenderTextureCommandBuffer);
		}
		
		private static RenderTextureFormat GetRenderTextureFormat()
		{
				return RenderTextureFormat.ARGB32;
		}

		private void CreateBakeRenderTextureCommandBuffer()
		{
			_bakeRenderTextureCommandBuffer = new CommandBuffer {name = "Bake Render Texture"};
			_bakeRenderTextureCommandBuffer.SetRenderTarget(new RenderTargetIdentifier(_backgroundBakeRenderTexture));
			_bakeRenderTextureCommandBuffer.ClearRenderTarget(true,true,Color.clear);
			_bakeRenderTextureCommandBuffer.DrawMesh(_fullScreenMesh, Matrix4x4.identity, _backgroundGradientMaterial,0,0);
		}
		
		private void UpdateMaterial()
		{
			var colorKeys = Gradient.colorKeys;

			if (colorKeys.Length > MaximumColors)
			{
				Debug.LogWarning("Gradient has more than 4 colors. Only the first 4 will be used...");
			}

			var effectiveColorArraySize = Math.Min(MaximumColors, colorKeys.Length);
			int i;
			for (i = 0; i < effectiveColorArraySize; i++)
			{
				_colorArray[i] = colorKeys[i].color;
				_timeArray[i] = colorKeys[i].time;

			}

			for (; i < MaximumColors; i++)
			{
				_colorArray[i] = _colorArray[i - 1];
				_timeArray[i] = 1.0f;

			}

			_backgroundGradientMaterial.SetColorArray(_shaderGradientColorsPropertyId, _colorArray);
			_backgroundGradientMaterial.SetFloatArray(_shaderGradientTimesPropertyId, _timeArray);
			_backgroundGradientMaterial.SetFloat(_shaderGradientColorsSizePropertyId, effectiveColorArraySize);
			_backgroundGradientMaterial.SetFloat(_shaderScreenRatioWidthDividedByHeightPropertyId, _ownCamera.aspect);
			_backgroundGradientMaterial.SetFloat(_shaderScreenRatioHeightDividedByWidthPropertyId, 1.0f / _ownCamera.aspect);
			_backgroundGradientMaterial.SetVector(_shaderGradientOriginPropertyId, new Vector2(RadialOriginX,RadialOriginY));
			_backgroundGradientMaterial.SetFloat(_shaderInvertDirectionPropertyId,InvertDirection?1.0f:0.0f);
			
			var shaderKeywords = _backgroundGradientMaterial.shaderKeywords;
			foreach (var t in shaderKeywords)
			{
				_backgroundGradientMaterial.DisableKeyword(t);
			}
			_backgroundGradientMaterial.EnableKeyword(GetKeywodForGradientType(GradientType));

		}
		
		private void Awake()
		{
			_awakeCalled = true;
			_ownCamera = GetComponent<Camera>();
			_shaderGradientColorsPropertyId = Shader.PropertyToID("_GradientColors");
			_shaderGradientTimesPropertyId = Shader.PropertyToID("_GradientTimes");
			_shaderGradientColorsSizePropertyId = Shader.PropertyToID("_GradientColorsSize");
			_shaderScreenRatioWidthDividedByHeightPropertyId = Shader.PropertyToID("_ScreenRatioWidthDividedByHeight");
			_shaderScreenRatioHeightDividedByWidthPropertyId = Shader.PropertyToID("_ScreenRatioHeightDividedByWidth");
			_shaderGradientOriginPropertyId = Shader.PropertyToID("_GradientOrigin");
			_shaderInvertDirectionPropertyId = Shader.PropertyToID("_InvertDirection");

			_commandBuffer = new CommandBuffer {name = "Background gradient"};
			_fullScreenMesh = CreateQuadMesh();
			
			if (_backgroundGradientMaterial == null || _backgroundRenderBakedTextureMaterial == null)
			{
				var gradientBackgroundShader = Shader.Find("Hidden/GradientBackground");
				_backgroundGradientMaterial = new Material(gradientBackgroundShader);			
				_backgroundRenderBakedTextureMaterial = new Material(gradientBackgroundShader);
			}
			
			UpdateCommandBufferCommands();
			InitializeScreenSize();
		}

		private void UpdateCommandBufferCommands()
		{
			_commandBuffer.Clear();

			if (UseBakedRenderTexture)
			{
				_commandBuffer.DrawMesh(_fullScreenMesh, Matrix4x4.identity, _backgroundRenderBakedTextureMaterial,0,1);
			}
			else
			{
				_commandBuffer.DrawMesh(_fullScreenMesh, Matrix4x4.identity, _backgroundGradientMaterial,0,0);
			}
		}

		private static string GetKeywodForGradientType(GradientType gradientType)
		{
			switch (gradientType)
			{
				case GradientType.Horizontal:
					return "HORIZONTAL_GRADIENT";
				case GradientType.Vertical:
					return "VERTICAL_GRADIENT";
				case GradientType.Radial:
					return "RADIAL_FIT_GRADIENT";
				case GradientType.RadialAspectFitWidth:
					return "RADIAL_ASPECT_WIDTH_GRADIENT";
				case GradientType.RadialAspectFitHeight:
					return "RADIAL_ASPECT_HEIGHT_GRADIENT";
				default:
					throw new ArgumentOutOfRangeException("gradientType", gradientType, null);
			}
		}

		private static Mesh CreateQuadMesh()
		{
			var quadMesh = new Mesh();
			quadMesh.SetVertices(new List<Vector3>
			{
				new Vector3(-1f, -1f, 0f),
				new Vector3(1f, -1f, 0f),
				new Vector3(1f, 1f, 0f),
				new Vector3(-1f, 1f, 0f)
			});

			quadMesh.triangles = new[]
			{
				0, 1, 2, 2, 3, 0
			};

			quadMesh.uv = new[]
			{
				new Vector2(0f, 0f),
				new Vector2(1f, 0f),
				new Vector2(1f, 1f),
				new Vector2(0f, 1f)
			};

			return quadMesh;
		}

		private void OnValidate()
		{
			SetDirty();
		}

		#region WebGLHack

		/**
		Webgl bake hack: Graphics.ExecuteCommandBuffer doesn't seem to work if called in the
		first frame, so that's why we force a call to SetDirty() (and, consequently, another Graphics.ExecuteCommandBuffer)  
		in the second frame */
		private bool _forcedDirtyInSecondFrame = false;

		private void ApplyWebglBakedRenderTextureHack()
		{
			if (Application.isPlaying)
			{
				if (!_forcedDirtyInSecondFrame && Time.frameCount > 1)
				{
					_forcedDirtyInSecondFrame = true;
					SetDirty();
				}
			}
		}

		#endregion
		

		private void Update()
		{
			#if UNITY_EDITOR
			if (!UnityEditor.EditorApplication.isPlaying) SetDirty();
			#endif

			if (UseBakedRenderTexture)
			{
				ApplyWebglBakedRenderTextureHack();
			}

			if (!_isDirty && UseBakedRenderTexture && DidScreenSizeChange())
			{
				_backgroundBakeRenderTexture.Release();
				_backgroundBakeRenderTexture = null;

				_bakeRenderTextureCommandBuffer.Release();
				_bakeRenderTextureCommandBuffer = null;
				
				SetDirty();
			}

			if (_isDirty)
			{
				UpdateCommandBufferCommands();
				UpdateMaterial();
				if (UseBakedRenderTexture)
				{
					RebakeRenderTexture();
				}

				_isDirty = false;
			}
		}


		private Vector2 _currentScreenSize = Vector2.zero;

		private void InitializeScreenSize()
		{
			_currentScreenSize = new Vector2(Screen.width, Screen.height);
		}

		private bool DidScreenSizeChange()
		{
			var screenSize = new Vector2(Screen.width, Screen.height);

			if (!screenSize.Equals(_currentScreenSize))
			{
				_currentScreenSize = screenSize;
				return true;
			}
			return false;
		}

		private void OnEnable()
		{
			if (!_awakeCalled)
				Awake(); //Safeguard for https://issuetracker.unity3d.com/issues/awake-and-start-not-called-before-update-when-assembly-is-reloaded-for-executeineditmode-scripts

			if (!_resetCalled)
				Reset();
			
			_originalCameraClearFlags = _ownCamera.clearFlags;
			_ownCamera.clearFlags = CameraClearFlags.Color;

			SetDirty();
			_ownCamera.AddCommandBuffer(CameraEvent.AfterImageEffectsOpaque, _commandBuffer);
		}

		private void OnDisable()
		{
			_ownCamera.RemoveCommandBuffer(CameraEvent.AfterImageEffectsOpaque, _commandBuffer);
			_ownCamera.clearFlags = _originalCameraClearFlags;
		}

		private void Reset()
		{
			//Tricks and hacks to ensure that the Reset method is called, even when adding the component at runtime
			if (_resetCalled) return;
			
			_resetCalled = true;
			
			//Setup a cool-looking gradient by default
			GradientType = GradientType.RadialAspectFitWidth;
			GradientColorKey colorKeyOne = new GradientColorKey(new Color(0.9f,0.9f,0.9f,1.0f),0f);
			GradientColorKey colorKeyTwo = new GradientColorKey(new Color(0.7f,0.7f,0.7f,1.0f),1f);
			Gradient.colorKeys = new [] {colorKeyOne,colorKeyTwo};
		}
	}
}