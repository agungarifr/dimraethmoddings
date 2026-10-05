using System;
using UnityEngine;
using UnityEngine.UI;

namespace DimraethMinimap;

public class MinimapBehaviour(IntPtr ptr) : MonoBehaviour(ptr)
{
	private const int UiLayer = 5;

	private const int CanvasSortOrder = 100;

	private const string DialoguePanelPath = "PlayerUIRoot/NPCDialogueUI/NPCTalk";

	private Transform _player;

	private Camera _cam;

	private RenderTexture _rt;

	private GameObject _holder;

	private RectTransform _marker;

	private Image _headImage;

	private Image _outline1;

	private Image _outline2;

	private Image _outline3;

	private Image _outline4;

	private Sprite _defaultMarkerSprite;

	private GameObject _dialoguePanel;

	private Image _headSource;

	private Sprite _lastHeadSprite;

	private Color _lastHeadColor;

	private Vector2 _lastHeadShift;

	private float _lastHeadScale;

	private bool _headVisualInitialized;

	private Sprite _cachedHeadSprite;

	private Color _cachedHeadColor = Color.white;

	private float _nextSearchTime;

	private float _nextHeadSearch;

	private float _nextDialogueSearch;

	private int _rebuilds;

	private float _nextRebuildTime;

	private bool _errorLogged;

	private bool _isQuitting;

	private bool _mapEnabled = true;

	private float _timeSinceLastRender;

	private float _lastZoom = -1f;

	private float _lastHeight = -1f;

	public void Start()
	{
		try
		{
			BuildCamera();
			BuildUi();
			Plugin.Diag("Minimap built.");
		}
		catch (Exception ex)
		{
			Plugin.Diag("Failed to build minimap: " + ex);
		}
	}

	public void OnApplicationQuit()
	{
		_isQuitting = true;
	}

	public void OnDestroy()
	{
		if (_cam != null)
		{
			_cam.targetTexture = null;
		}
		if (_rt != null)
		{
			_rt.Release();
			UnityEngine.Object.Destroy(_rt);
			_rt = null;
		}
	}

	public void LateUpdate()
	{
		if (_isQuitting)
		{
			return;
		}
		if (Plugin.ToggleKey.Value != KeyCode.None && Input.GetKeyDown(Plugin.ToggleKey.Value))
		{
			_mapEnabled = !_mapEnabled;
			Plugin.Diag("Minimap " + (_mapEnabled ? "enabled" : "disabled") + ".");
		}
		if (!_mapEnabled)
		{
			if (_holder != null && _holder.activeSelf)
			{
				_holder.SetActive(value: false);
			}
			return;
		}
		if (_cam == null || _holder == null)
		{
			if (_rebuilds >= 50 || !(Time.unscaledTime >= _nextRebuildTime))
			{
				return;
			}
			_rebuilds++;
			_nextRebuildTime = Time.unscaledTime + 2f;
			Plugin.Diag("Minimap parts were destroyed. Rebuilding, attempt " + _rebuilds);
			try
			{
				RebuildParts();
				return;
			}
			catch (Exception ex)
			{
				Plugin.Diag("Rebuild failed: " + ex);
				return;
			}
		}
		try
		{
			Transform player = _player;
			if (player == null && Time.unscaledTime >= _nextSearchTime)
			{
				_nextSearchTime = Time.unscaledTime + 1f;
				_player = FindPlayer();
				if (_player != null)
				{
					Plugin.Diag("Minimap tracking player: " + _player.name);
					player = _player;
					_headSource = null;
					_nextHeadSearch = 0f;
					_headVisualInitialized = false;
				}
			}
			if (player == null && Plugin.FallbackToMainCamera.Value)
			{
				Camera main = Camera.main;
				if (main != null)
				{
					player = main.transform;
				}
			}
			if (player == null)
			{
				if (_holder.activeSelf)
				{
					_holder.SetActive(value: false);
				}
				return;
			}
			if (IsDialogueOpen())
			{
				if (_holder.activeSelf)
				{
					_holder.SetActive(value: false);
				}
				return;
			}
			if (!_holder.activeSelf)
			{
				_holder.SetActive(value: true);
			}
			if (Plugin.Mode2D.Value && Plugin.ShowPlayerHead.Value)
			{
				UpdateHead();
			}
			Vector3 position = player.position;
			if (Plugin.Mode2D.Value)
			{
				_cam.transform.position = new Vector3(position.x, position.y, position.z - Plugin.CameraHeight.Value);
				_cam.transform.rotation = Quaternion.identity;
				_marker.localEulerAngles = Vector3.zero;
			}
			else
			{
				float y = player.eulerAngles.y;
				float y2 = (Plugin.RotateWithPlayer.Value ? y : 0f);
				_cam.transform.position = new Vector3(position.x, position.y + Plugin.CameraHeight.Value, position.z);
				_cam.transform.rotation = Quaternion.Euler(90f, y2, 0f);
				float z = (Plugin.RotateWithPlayer.Value ? 0f : (0f - y));
				_marker.localEulerAngles = new Vector3(0f, 0f, z);
			}
			if (_lastZoom != Plugin.Zoom.Value)
			{
				_lastZoom = Plugin.Zoom.Value;
				_cam.orthographicSize = _lastZoom;
			}
			if (_lastHeight != Plugin.CameraHeight.Value)
			{
				_lastHeight = Plugin.CameraHeight.Value;
				_cam.farClipPlane = _lastHeight * 2f;
			}
			_timeSinceLastRender += Time.unscaledDeltaTime;
			float fps = Plugin.TargetFps != null ? Mathf.Clamp(Plugin.TargetFps.Value, 5f, 60f) : 30f;
			float renderInterval = 1f / fps;
			if (_timeSinceLastRender >= renderInterval)
			{
				_timeSinceLastRender = 0f;
				_cam.Render();
			}
		}
		catch (Exception ex2)
		{
			if (!_errorLogged)
			{
				_errorLogged = true;
				Plugin.Diag("LateUpdate error: " + ex2);
			}
		}
	}

	private Transform FindPlayer()
	{
		GameObject gameObject = null;
		string value = Plugin.PlayerObjectName.Value;
		if (!string.IsNullOrWhiteSpace(value))
		{
			gameObject = GameObject.Find(value);
		}
		if (gameObject == null)
		{
			try
			{
				gameObject = GameObject.FindWithTag("Player");
			}
			catch (Exception)
			{
			}
		}
		if (!(gameObject != null))
		{
			return null;
		}
		return gameObject.transform;
	}

	private void UpdateHead()
	{
		if (_headImage == null)
		{
			return;
		}
		if (_headSource == null && Time.unscaledTime >= _nextHeadSearch)
		{
			_nextHeadSearch = Time.unscaledTime + 3f;
			_headSource = FindHeadImage();
			if (_headSource != null)
			{
				Plugin.Diag("Using head picture from: " + GetPath(_headSource.transform) + " (sprite " + ((_headSource.sprite != null) ? _headSource.sprite.name : "null") + ")");
			}
		}
		if (_headSource != null && _headSource.sprite != null)
		{
			_cachedHeadSprite = _headSource.sprite;
			_cachedHeadColor = _headSource.color;
		}
		bool num = _cachedHeadSprite != null;
		Sprite sprite = (num ? _cachedHeadSprite : _defaultMarkerSprite);
		float num2 = Plugin.MapSize.Value * Plugin.MarkerSize.Value;
		float num3 = (num ? Plugin.HeadSize.Value : 0.45f);
		Vector2 vector = (num ? new Vector2((0.5f - Plugin.HeadCenterX.Value) * num3 * num2, (0.5f - Plugin.HeadCenterY.Value) * num3 * num2) : Vector2.zero);
		Color color = new Color(0.03f, 0.02f, 0.02f, 0.9f);
		Color color2 = (num ? _cachedHeadColor : Color.white);
		if (!_headVisualInitialized || !(_lastHeadSprite == sprite) || _lastHeadScale != num3 || !(_lastHeadShift == vector) || !(_lastHeadColor == color2))
		{
			Draw.ApplyLayer(_outline1, sprite, num3, vector + new Vector2(1.4f, 0f), color);
			Draw.ApplyLayer(_outline2, sprite, num3, vector + new Vector2(-1.4f, 0f), color);
			Draw.ApplyLayer(_outline3, sprite, num3, vector + new Vector2(0f, 1.4f), color);
			Draw.ApplyLayer(_outline4, sprite, num3, vector + new Vector2(0f, -1.4f), color);
			Draw.ApplyLayer(_headImage, sprite, num3, vector, color2);
			_lastHeadSprite = sprite;
			_lastHeadScale = num3;
			_lastHeadShift = vector;
			_lastHeadColor = color2;
			_headVisualInitialized = true;
		}
	}

	private Image FindHeadImage()
	{
		string text = (Plugin.HeadImagePath.Value ?? "").Trim();
		GameObject gameObject = GameObject.Find((text.Length > 0) ? text : "PlayerUIRoot/NPCDialogueUI/NPCTalk/PlayerIllustrations/PlayerInConvo/Head");
		if (!(gameObject != null))
		{
			return null;
		}
		return gameObject.GetComponent<Image>();
	}

	private bool IsDialogueOpen()
	{
		if (_dialoguePanel == null && Time.unscaledTime >= _nextDialogueSearch)
		{
			_nextDialogueSearch = Time.unscaledTime + 1f;
			_dialoguePanel = GameObject.Find("PlayerUIRoot/NPCDialogueUI/NPCTalk");
		}
		if (_dialoguePanel != null)
		{
			return _dialoguePanel.activeInHierarchy;
		}
		return false;
	}

	private static string GetPath(Transform t)
	{
		string text = t.name;
		while (t.parent != null)
		{
			t = t.parent;
			text = t.name + "/" + text;
		}
		return text;
	}

	private void RebuildParts()
	{
		if (_cam != null)
		{
			_cam.targetTexture = null;
		}
		if (_rt != null)
		{
			_rt.Release();
			UnityEngine.Object.Destroy(_rt);
		}
		for (int num = base.transform.childCount - 1; num >= 0; num--)
		{
			UnityEngine.Object.Destroy(base.transform.GetChild(num).gameObject);
		}
		_cam = null;
		_holder = null;
		_marker = null;
		_dialoguePanel = null;
		_headImage = null;
		_outline1 = null;
		_outline2 = null;
		_outline3 = null;
		_outline4 = null;
		_headVisualInitialized = false;
		_rt = null;
		BuildCamera();
		BuildUi();
		Plugin.Diag("Rebuild finished.");
	}

	private void BuildCamera()
	{
		GameObject gameObject = new GameObject("MinimapCamera");
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		_cam = gameObject.AddComponent<Camera>();
		_cam.orthographic = true;
		_cam.clearFlags = CameraClearFlags.Color;
		_cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
		_cam.cullingMask = -33;
		_cam.nearClipPlane = 0.1f;
		_cam.depth = -50f;

		try
		{
			var camData = gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
			if (camData != null)
			{
				camData.renderShadows = false;
				camData.renderPostProcessing = false;
				camData.requiresColorOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off;
				camData.requiresDepthOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off;
				camData.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;
				camData.dithering = false;
				camData.stopNaN = false;
			}
		}
		catch (Exception ex)
		{
			Plugin.Diag("UniversalAdditionalCameraData init notice: " + ex.Message);
		}

		int res = Mathf.Clamp(Plugin.RenderResolution != null ? Plugin.RenderResolution.Value : 256, 128, 512);
		_rt = new RenderTexture(res, res, 16);
		_rt.useMipMap = false;
		_rt.autoGenerateMips = false;
		_rt.filterMode = FilterMode.Bilinear;
		_cam.targetTexture = _rt;
		_cam.enabled = false;
	}

	private void BuildUi()
	{
		float value = Plugin.MapSize.Value;
		GameObject gameObject = new GameObject("MinimapCanvas");
		gameObject.layer = 5;
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		Canvas canvas = gameObject.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 100;
		CanvasScaler canvasScaler = gameObject.AddComponent<CanvasScaler>();
		canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
		canvasScaler.matchWidthOrHeight = 0.5f;
		string text = (Plugin.Corner.Value ?? "TopLeft").ToLowerInvariant();
		bool flag = text.Contains("right");
		bool flag2 = text.Contains("bottom");
		float value2 = Plugin.Margin.Value;
		RectTransform rectTransform = NewUi("MinimapHolder", gameObject.transform);
		Vector2 vector = (rectTransform.pivot = new Vector2(flag ? 1f : 0f, flag2 ? 0f : 1f));
		Vector2 anchorMin = (rectTransform.anchorMax = vector);
		rectTransform.anchorMin = anchorMin;
		rectTransform.sizeDelta = new Vector2(value, value);
		rectTransform.anchoredPosition = new Vector2(flag ? (0f - value2) : value2, flag2 ? value2 : (0f - value2));
		_holder = rectTransform.gameObject;
		RectTransform rectTransform2 = NewUi("CircleMask", rectTransform);
		Stretch(rectTransform2);
		Image image = rectTransform2.gameObject.AddComponent<Image>();
		image.sprite = Draw.CircleMask();
		image.raycastTarget = false;
		rectTransform2.gameObject.AddComponent<Mask>().showMaskGraphic = false;
		RectTransform rectTransform3 = NewUi("Backdrop", rectTransform2);
		Stretch(rectTransform3);
		Image image2 = rectTransform3.gameObject.AddComponent<Image>();
		image2.color = new Color(0.05f, 0.06f, 0.08f, 1f);
		image2.raycastTarget = false;
		RectTransform rectTransform4 = NewUi("MapView", rectTransform2);
		Stretch(rectTransform4);
		RawImage rawImage = rectTransform4.gameObject.AddComponent<RawImage>();
		rawImage.texture = _rt;
		rawImage.raycastTarget = false;
		_defaultMarkerSprite = (Plugin.Mode2D.Value ? Draw.DefaultCircle() : Draw.DefaultArrow());
		float num = value * Plugin.MarkerSize.Value;
		_marker = NewUi("PlayerMarker", rectTransform2);
		RectTransform marker = _marker;
		RectTransform marker2 = _marker;
		vector = (_marker.pivot = new Vector2(0.5f, 0.5f));
		anchorMin = (marker2.anchorMax = vector);
		marker.anchorMin = anchorMin;
		_marker.sizeDelta = new Vector2(num, num);
		_marker.anchoredPosition = Vector2.zero;
		_outline1 = NewMarkerLayer("OutlineA");
		_outline2 = NewMarkerLayer("OutlineB");
		_outline3 = NewMarkerLayer("OutlineC");
		_outline4 = NewMarkerLayer("OutlineD");
		_headImage = NewMarkerLayer("PlayerIcon");
		RectTransform rectTransform5 = NewUi("Border", rectTransform);
		Stretch(rectTransform5);
		Image image3 = rectTransform5.gameObject.AddComponent<Image>();
		image3.sprite = Draw.WoodBorder(512);
		image3.raycastTarget = false;
		bool flag3 = !Plugin.Mode2D.Value && Plugin.RotateWithPlayer.Value;
		if (Plugin.ShowCompass.Value && !flag3)
		{
			BuildCompass(rectTransform, value);
		}
		_holder.SetActive(value: false);
	}

	private Image NewMarkerLayer(string layerName)
	{
		RectTransform rectTransform = NewUi(layerName, _marker);
		Stretch(rectTransform);
		Image image = rectTransform.gameObject.AddComponent<Image>();
		image.sprite = _defaultMarkerSprite;
		image.preserveAspect = true;
		image.raycastTarget = false;
		return image;
	}

	private void BuildCompass(RectTransform holder, float size)
	{
		string text = "NESW";
		Vector2[] array = new Vector2[4]
		{
			new Vector2(0f, 1f),
			new Vector2(1f, 0f),
			new Vector2(0f, -1f),
			new Vector2(-1f, 0f)
		};
		float num = size * 0.14f;
		float num2 = size * 0.5f * 0.9425f;
		Sprite sprite = Draw.CompassBadge();
		for (int i = 0; i < 4; i++)
		{
			char ch = text[i];
			RectTransform rectTransform = NewUi("Compass" + ch, holder);
			Vector2 vector = (rectTransform.pivot = new Vector2(0.5f, 0.5f));
			Vector2 anchorMin = (rectTransform.anchorMax = vector);
			rectTransform.anchorMin = anchorMin;
			rectTransform.sizeDelta = new Vector2(num, num);
			rectTransform.anchoredPosition = new Vector2(array[i].x * num2, array[i].y * num2);
			Image image = rectTransform.gameObject.AddComponent<Image>();
			image.sprite = sprite;
			image.raycastTarget = false;
			RectTransform rectTransform2 = NewUi("Letter", rectTransform);
			rectTransform2.anchorMin = new Vector2(0.2f, 0.2f);
			rectTransform2.anchorMax = new Vector2(0.8f, 0.8f);
			rectTransform2.offsetMin = Vector2.zero;
			rectTransform2.offsetMax = Vector2.zero;
			Image image2 = rectTransform2.gameObject.AddComponent<Image>();
			image2.sprite = Draw.Letter(ch);
			image2.color = ((i == 0) ? Draw.NorthRed : Draw.Cream);
			image2.raycastTarget = false;
		}
	}

	private static RectTransform NewUi(string name, Transform parent)
	{
		RectTransform rectTransform = new GameObject(name)
		{
			layer = 5
		}.AddComponent<RectTransform>();
		rectTransform.SetParent(parent, worldPositionStays: false);
		return rectTransform;
	}

	private static void Stretch(RectTransform rt)
	{
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;
	}
}
