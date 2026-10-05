using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace DimraethMinimap;

[BepInPlugin("RenosUtilitiesConfig", "Reno's Utilities", "1.1.3")]
public class Plugin : BasePlugin
{
	public const string PluginGuid = "RenosUtilitiesConfig";

	public const string PluginName = "Reno's Utilities";

	public const string PluginVersion = "1.1.3";

	public static ManualLogSource Logger;

	public static ConfigEntry<float> MapSize;

	public static ConfigEntry<float> Margin;

	public static ConfigEntry<string> Corner;

	public static ConfigEntry<float> Zoom;

	public static ConfigEntry<float> CameraHeight;

	public static ConfigEntry<bool> RotateWithPlayer;

	public static ConfigEntry<string> PlayerObjectName;

	public static ConfigEntry<bool> Mode2D;

	public static ConfigEntry<bool> FallbackToMainCamera;

	public static ConfigEntry<bool> ShowCompass;

	public static ConfigEntry<bool> ShowPlayerHead;

	public static ConfigEntry<KeyCode> ToggleKey;

	public static ConfigEntry<float> HeadSize;

	public static ConfigEntry<float> HeadCenterX;

	public static ConfigEntry<float> HeadCenterY;

	public static ConfigEntry<float> MarkerSize;

	public static ConfigEntry<string> HeadImagePath;

	public static ConfigEntry<float> TargetFps;

	public static ConfigEntry<int> RenderResolution;

	public static void Diag(string msg)
	{
		Logger.LogInfo(msg);
	}

	public override void Load()
	{
		Logger = base.Log;
		MapSize = base.Config.Bind("Minimap", "Size", 220f, "Diameter of the minimap in pixels (at 1080p).");
		Margin = base.Config.Bind("Minimap", "Margin", 20f, "Gap between the minimap and the screen edge.");
		Corner = base.Config.Bind("Minimap", "Corner", "TopLeft", "Where the minimap sits: TopLeft, TopRight, BottomLeft or BottomRight.");
		Zoom = base.Config.Bind("Minimap", "Zoom", 40f, "World units shown from the center to the edge. Smaller = more zoomed in.");
		CameraHeight = base.Config.Bind("Minimap", "CameraHeight", 150f, "Distance of the map camera from the world.");
		RotateWithPlayer = base.Config.Bind("Minimap", "RotateWithPlayer", defaultValue: false, "Rotate the map so the player faces up (3D games only).");
		PlayerObjectName = base.Config.Bind("Minimap", "PlayerObjectName", "", "Exact name of the player GameObject. Leave empty to search for the tag 'Player'.");
		Mode2D = base.Config.Bind("Minimap", "Mode2D", defaultValue: true, "True for 2D games (the world is the X/Y plane, Z is depth). False for 3D games (ground is X/Z).");
		FallbackToMainCamera = base.Config.Bind("Minimap", "FallbackToMainCamera", defaultValue: false, "Follow the main camera when no player is found (debugging). When off, the minimap hides in menus.");
		ShowCompass = base.Config.Bind("Minimap", "ShowCompass", defaultValue: true, "Show N / E / S / W badges on the border (up on screen = north).");
		ShowPlayerHead = base.Config.Bind("Minimap", "ShowPlayerHead", defaultValue: true, "Use the player's head picture as the map marker (2D mode). Falls back to a yellow dot.");
		ToggleKey = base.Config.Bind("Minimap", "ToggleKey", KeyCode.F8, "Keyboard shortcut to show or hide the minimap. Set to None to disable the shortcut.");
		HeadSize = base.Config.Bind("Minimap", "HeadSize", 0.98f, "Size of the head picture. Bigger number = bigger head. (The previous version used 1.5, which was about 35% larger.)");
		HeadCenterX = base.Config.Bind("Minimap", "HeadCenterX", 0.59f, "Where the visible head sits inside the game's picture, left to right (0-1). Used to center it.");
		HeadCenterY = base.Config.Bind("Minimap", "HeadCenterY", 0.35f, "Where the visible head sits inside the game's picture, bottom to top (0-1). Used to center it.");
		MarkerSize = base.Config.Bind("Minimap", "MarkerSize", 0.22f, "Size of the player marker (head) as a fraction of the map size. Raise it if the head looks too small.");
		HeadImagePath = base.Config.Bind("Minimap", "HeadImagePath", "", "End of the UI path of the image to use as the head, e.g. 'PlayerInConvo/Head'. Empty = auto.");
		TargetFps = base.Config.Bind("Performance", "TargetFps", 30f, "Minimap camera refresh rate (FPS). Lower values (e.g. 30) drastically reduce GPU load and eliminate stutter.");
		RenderResolution = base.Config.Bind("Performance", "RenderResolution", 256, "Minimap render texture resolution (128, 256, or 512). 256 is sharp and 4x faster than 512.");
		ClassInjector.RegisterTypeInIl2Cpp<MinimapBehaviour>();
		GameObject gameObject = new GameObject("DimraethMinimap");
		UnityEngine.Object.DontDestroyOnLoad(gameObject);
		gameObject.hideFlags = HideFlags.HideAndDontSave;
		gameObject.AddComponent<MinimapBehaviour>();
		Diag("Reno's Utilities 1.1.3 loaded.");
	}
}
