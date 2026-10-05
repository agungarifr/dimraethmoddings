using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Cpp2ILInjected;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Token(Token = "0x2000B92")]
public class WorldSelection : MonoBehaviour, IUIActionReceiver
{
	[CompilerGenerated]
	[Token(Token = "0x2000B97")]
	private sealed class <FadeOutNotReadyPopup>d__92 : IEnumerator<object>, IEnumerator, IDisposable
	{
		[Token(Token = "0x4005858")]
		[FieldOffset(Offset = "0x10")]
		private int <>1__state;

		[Token(Token = "0x4005859")]
		[FieldOffset(Offset = "0x18")]
		private object <>2__current;

		[Token(Token = "0x400585A")]
		[FieldOffset(Offset = "0x20")]
		public WorldSelection <>4__this;

		[Token(Token = "0x400585B")]
		[FieldOffset(Offset = "0x28")]
		public float delayBeforeFade;

		[Token(Token = "0x400585C")]
		[FieldOffset(Offset = "0x2C")]
		private float <elapsed>5__2;

		[Token(Token = "0x17000A0C")]
		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			[Token(Token = "0x6005E93")]
			[Address(RVA = "0x6B1E50", Offset = "0x6B0E50", Length = "0x5")]
			get
			{
				return null;
			}
		}

		[Token(Token = "0x17000A0D")]
		object IEnumerator.Current
		{
			[DebuggerHidden]
			[Token(Token = "0x6005E95")]
			[Address(RVA = "0x6B1E50", Offset = "0x6B0E50", Length = "0x5")]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		[Token(Token = "0x6005E90")]
		[Address(RVA = "0x6B1EA0", Offset = "0x6B0EA0", Length = "0x24")]
		public <FadeOutNotReadyPopup>d__92(int <>1__state)
		{
		}

		[DebuggerHidden]
		[Token(Token = "0x6005E91")]
		[Address(RVA = "0x6AAC60", Offset = "0x6A9C60", Length = "0x3")]
		void IDisposable.Dispose()
		{
		}

		[Token(Token = "0x6005E92")]
		[Address(RVA = "0xF41510", Offset = "0xF40510", Length = "0x1F4")]
		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		[Token(Token = "0x6005E94")]
		[Address(RVA = "0xF41710", Offset = "0xF40710", Length = "0x3E")]
		void IEnumerator.Reset()
		{
		}
	}

	[CompilerGenerated]
	[Token(Token = "0x2000B98")]
	private sealed class <InitializeWorldSelectionScreenCoroutine>d__64 : IEnumerator<object>, IEnumerator, IDisposable
	{
		[Token(Token = "0x400585D")]
		[FieldOffset(Offset = "0x10")]
		private int <>1__state;

		[Token(Token = "0x400585E")]
		[FieldOffset(Offset = "0x18")]
		private object <>2__current;

		[Token(Token = "0x400585F")]
		[FieldOffset(Offset = "0x20")]
		public WorldSelection <>4__this;

		[Token(Token = "0x4005860")]
		[FieldOffset(Offset = "0x28")]
		private int <worldIndex>5__2;

		[Token(Token = "0x4005861")]
		[FieldOffset(Offset = "0x2C")]
		private bool <hasWorldEntry>5__3;

		[Token(Token = "0x17000A0E")]
		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			[Token(Token = "0x6005E99")]
			[Address(RVA = "0x6B1E50", Offset = "0x6B0E50", Length = "0x5")]
			get
			{
				return null;
			}
		}

		[Token(Token = "0x17000A0F")]
		object IEnumerator.Current
		{
			[DebuggerHidden]
			[Token(Token = "0x6005E9B")]
			[Address(RVA = "0x6B1E50", Offset = "0x6B0E50", Length = "0x5")]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		[Token(Token = "0x6005E96")]
		[Address(RVA = "0x6B1EA0", Offset = "0x6B0EA0", Length = "0x24")]
		public <InitializeWorldSelectionScreenCoroutine>d__64(int <>1__state)
		{
		}

		[DebuggerHidden]
		[Token(Token = "0x6005E97")]
		[Address(RVA = "0x6AAC60", Offset = "0x6A9C60", Length = "0x3")]
		void IDisposable.Dispose()
		{
		}

		[Token(Token = "0x6005E98")]
		[Address(RVA = "0xF41AB0", Offset = "0xF40AB0", Length = "0x5C5")]
		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		[Token(Token = "0x6005E9A")]
		[Address(RVA = "0xF42080", Offset = "0xF41080", Length = "0x3E")]
		void IEnumerator.Reset()
		{
		}
	}

	[Token(Token = "0x4005823")]
	public static WorldSelection Singleton;

	[SerializeField]
	[Token(Token = "0x4005824")]
	[FieldOffset(Offset = "0x20")]
	private GameObject _worldSavePrefab;

	[SerializeField]
	[Token(Token = "0x4005825")]
	[FieldOffset(Offset = "0x28")]
	private GameObject _emptyWorldSavePrefab;

	[SerializeField]
	[Token(Token = "0x4005826")]
	[FieldOffset(Offset = "0x30")]
	private GameObject _content;

	[SerializeField]
	[Token(Token = "0x4005827")]
	[FieldOffset(Offset = "0x38")]
	private Toggle _singeplayer;

	[SerializeField]
	[Token(Token = "0x4005828")]
	[FieldOffset(Offset = "0x40")]
	private Toggle _multiplayer;

	[SerializeField]
	[Token(Token = "0x4005829")]
	[FieldOffset(Offset = "0x48")]
	private Button _delete;

	[SerializeField]
	[Token(Token = "0x400582A")]
	[FieldOffset(Offset = "0x50")]
	private Image _backgroundDifficultyPreview;

	[SerializeField]
	[Token(Token = "0x400582B")]
	[FieldOffset(Offset = "0x58")]
	private Image _worldDifficultyPreview;

	[SerializeField]
	[Token(Token = "0x400582C")]
	[FieldOffset(Offset = "0x60")]
	private TextMeshProUGUI _worldNamePreview;

	[SerializeField]
	[Token(Token = "0x400582D")]
	[FieldOffset(Offset = "0x68")]
	private TextMeshProUGUI _worldAgePreview;

	[SerializeField]
	[Token(Token = "0x400582E")]
	[FieldOffset(Offset = "0x70")]
	private TextMeshProUGUI _worldDifficultyText;

	[SerializeField]
	[Token(Token = "0x400582F")]
	[FieldOffset(Offset = "0x78")]
	private TextMeshProUGUI _newCharacterOnlyText;

	[SerializeField]
	[Token(Token = "0x4005830")]
	[FieldOffset(Offset = "0x80")]
	private GameObject _worldDeleteConfirmation;

	[SerializeField]
	[Token(Token = "0x4005831")]
	[FieldOffset(Offset = "0x88")]
	private GameObject _checkboxes;

	[SerializeField]
	[Token(Token = "0x4005832")]
	[FieldOffset(Offset = "0x90")]
	private Toggle _friendlyFire;

	[SerializeField]
	[Token(Token = "0x4005833")]
	[FieldOffset(Offset = "0x98")]
	private Toggle _newCharactersOnly;

	[SerializeField]
	[Token(Token = "0x4005834")]
	[FieldOffset(Offset = "0xA0")]
	private Toggle _personalLoot;

	[SerializeField]
	[Token(Token = "0x4005835")]
	[FieldOffset(Offset = "0xA8")]
	private CanvasGroup _notReadyPopUp;

	[SerializeField]
	[Token(Token = "0x4005836")]
	[FieldOffset(Offset = "0xB0")]
	private TextMeshProUGUI _errorText;

	[Header("Page Wiring")]
	[Tooltip("Next button; advances the flow through LoadManager.WorldSelectionNext.")]
	[SerializeField]
	[Token(Token = "0x4005837")]
	[FieldOffset(Offset = "0xB8")]
	private Button _nextButton;

	[Tooltip("Back button; runs the same mode-dependent Back routing as gamepad B / Escape.")]
	[SerializeField]
	[Token(Token = "0x4005838")]
	[FieldOffset(Offset = "0xC0")]
	private Button _backButton;

	[Tooltip("Create World button; opens the WorldCreation page.")]
	[SerializeField]
	[Token(Token = "0x4005839")]
	[FieldOffset(Offset = "0xC8")]
	private Button _createWorldButton;

	[Tooltip("Join button; opens the MultiplayerLobby screen.")]
	[SerializeField]
	[Token(Token = "0x400583A")]
	[FieldOffset(Offset = "0xD0")]
	private Button _joinLobbyButton;

	[Tooltip("Cancel button inside the delete-world confirmation popup; controller focus lands here while the popup is open so the destructive Confirm is never the default.")]
	[SerializeField]
	[Token(Token = "0x400583B")]
	[FieldOffset(Offset = "0xD8")]
	private Button _cancelDeletionButton;

	[Token(Token = "0x400583C")]
	[FieldOffset(Offset = "0xE0")]
	private List<WorldSaveFile> _worldSaves;

	[Token(Token = "0x400583D")]
	[FieldOffset(Offset = "0xE8")]
	public WorldSaveFile CurrentWorldFile;

	[Token(Token = "0x400583E")]
	[FieldOffset(Offset = "0xF0")]
	private bool _isWorldSelectable;

	[Token(Token = "0x400583F")]
	[FieldOffset(Offset = "0xF8")]
	private Coroutine _notReadyCoroutine;

	[Token(Token = "0x4005840")]
	[FieldOffset(Offset = "0x100")]
	private UIFocusScope _focusScope;

	[Token(Token = "0x4005841")]
	[FieldOffset(Offset = "0x108")]
	private UINode _node;

	[Token(Token = "0x4005842")]
	[FieldOffset(Offset = "0x110")]
	private MainMenuTabNavigator _tabNavigator;

	[Token(Token = "0x4005843")]
	[FieldOffset(Offset = "0x118")]
	private UIFocusCoordinator _tabNavigatorCoordinator;

	[Token(Token = "0x4005844")]
	[FieldOffset(Offset = "0x120")]
	private UIFocusCoordinator _selectionTrackedCoordinator;

	[Token(Token = "0x4005845")]
	[FieldOffset(Offset = "0x128")]
	private GameObject _prePopupSelection;

	[Token(Token = "0x4005846")]
	[FieldOffset(Offset = "0x130")]
	private int _pendingDeletionIndex;

	[Token(Token = "0x4005847")]
	[FieldOffset(Offset = "0x138")]
	private string _entryWorldName;

	[Token(Token = "0x4005848")]
	[FieldOffset(Offset = "0x140")]
	private string _entryWorldPref;

	[Token(Token = "0x4005849")]
	[FieldOffset(Offset = "0x148")]
	private string _entryAllowedHashesPref;

	[Token(Token = "0x400584A")]
	[FieldOffset(Offset = "0x150")]
	private bool _hasEntrySnapshot;

	[Token(Token = "0x400584B")]
	private const int SlotDifficultyImageChild = 1;

	[Token(Token = "0x400584C")]
	private const int SlotWorldIconChild = 2;

	[Token(Token = "0x400584D")]
	private const int SlotLockBackgroundChild = 3;

	[Token(Token = "0x400584E")]
	private const int SlotWorldNameChild = 4;

	[Token(Token = "0x400584F")]
	private const int SlotSelectedFrameChild = 5;

	[Token(Token = "0x17000A0A")]
	private bool IsDeleteConfirmationOpen
	{
		[Token(Token = "0x6005E5B")]
		[Address(RVA = "0xF4EE70", Offset = "0xF4DE70", Length = "0x324")]
		get
		{
			return false;
		}
	}

	[Token(Token = "0x17000A0B")]
	private bool CanAdvancePage
	{
		[Token(Token = "0x6005E5C")]
		[Address(RVA = "0xF4EE50", Offset = "0xF4DE50", Length = "0x15")]
		get
		{
			return false;
		}
	}

	[Token(Token = "0x6005E4E")]
	[Address(RVA = "0xF49620", Offset = "0xF48620", Length = "0x776")]
	private void Awake()
	{
	}

	[Token(Token = "0x6005E4F")]
	[Address(RVA = "0xF4BF30", Offset = "0xF4AF30", Length = "0x170")]
	private void OnDestroy()
	{
	}

	[Token(Token = "0x6005E50")]
	[Address(RVA = "0xF4EBE0", Offset = "0xF4DBE0", Length = "0x24C")]
	private void WireTabNavigator()
	{
	}

	[Token(Token = "0x6005E51")]
	[Address(RVA = "0xF4DCC0", Offset = "0xF4CCC0", Length = "0x140")]
	private Selectable SelectedRowOr(Selectable fallback)
	{
		return null;
	}

	[Token(Token = "0x6005E52")]
	[Address(RVA = "0xF4E930", Offset = "0xF4D930", Length = "0x15E")]
	private void Update()
	{
	}

	[Token(Token = "0x6005E53")]
	[Address(RVA = "0xF4C190", Offset = "0xF4B190", Length = "0x12D")]
	private void OnEnable()
	{
	}

	[Token(Token = "0x6005E54")]
	[Address(RVA = "0xF4C0A0", Offset = "0xF4B0A0", Length = "0xE5")]
	private void OnDisable()
	{
	}

	[Token(Token = "0x6005E55")]
	[Address(RVA = "0xF49DA0", Offset = "0xF48DA0", Length = "0x56")]
	private void Back()
	{
	}

	[Token(Token = "0x6005E56")]
	[Address(RVA = "0xF4CC30", Offset = "0xF4BC30", Length = "0x299")]
	private void RestoreEntryWorldCommit()
	{
	}

	[Token(Token = "0x6005E57")]
	[Address(RVA = "0xF4CED0", Offset = "0xF4BED0", Length = "0xA1")]
	private void RestoreEntryWorldPrefs()
	{
	}

	[Token(Token = "0x6005E58")]
	[Address(RVA = "0xF4C2C0", Offset = "0xF4B2C0", Length = "0xA1")]
	private void RegisterAction(UINode node, UIActionId action)
	{
	}

	[Token(Token = "0x6005E59")]
	[Address(RVA = "0xF49E00", Offset = "0xF48E00", Length = "0x36F")]
	public bool CanHandle(in UIActionContext ctx)
	{
		return false;
	}

	[Token(Token = "0x6005E5A")]
	[Address(RVA = "0xF4B4A0", Offset = "0xF4A4A0", Length = "0x605")]
	public UIActionResult Handle(in UIActionContext ctx)
	{
		return default(UIActionResult);
	}

	[Token(Token = "0x6005E5D")]
	[Address(RVA = "0xF495A0", Offset = "0xF485A0", Length = "0x74")]
	private bool AdvanceOrExplainRefusal()
	{
		return false;
	}

	[Token(Token = "0x6005E5E")]
	[Address(RVA = "0xF4BDF0", Offset = "0xF4ADF0", Length = "0x71")]
	public void InitializeWorldSelectionScreen()
	{
	}

	[IteratorStateMachine(typeof(<InitializeWorldSelectionScreenCoroutine>d__64))]
	[Token(Token = "0x6005E5F")]
	[Address(RVA = "0xF4BD80", Offset = "0xF4AD80", Length = "0x67")]
	private IEnumerator InitializeWorldSelectionScreenCoroutine()
	{
		return null;
	}

	[Token(Token = "0x6005E60")]
	[Address(RVA = "0xF4B080", Offset = "0xF4A080", Length = "0x13C")]
	private int FindPreferredWorldIndex()
	{
		return 0;
	}

	[Token(Token = "0x6005E61")]
	[Address(RVA = "0xF4A390", Offset = "0xF49390", Length = "0x197")]
	private void CreateEmptySavesUntilContentFilled()
	{
	}

	[Token(Token = "0x6005E62")]
	[Address(RVA = "0xF4E2E0", Offset = "0xF4D2E0", Length = "0x141")]
	public void TrySelectWorldByName(string worldName)
	{
	}

	[Token(Token = "0x6005E63")]
	[Address(RVA = "0xF4ADA0", Offset = "0xF49DA0", Length = "0x2DF")]
	private int FindLastPlayedWorldIndex()
	{
		return 0;
	}

	[Token(Token = "0x6005E64")]
	[Address(RVA = "0xF4C370", Offset = "0xF4B370", Length = "0x8BE")]
	private void ReloadAllWorldSlots()
	{
	}

	[Token(Token = "0x6005E65")]
	[Address(RVA = "0xF4AAF0", Offset = "0xF49AF0", Length = "0x226")]
	private void EmptyWorldPreviewPage(bool value)
	{
	}

	[Token(Token = "0x6005E66")]
	[Address(RVA = "0xF4E600", Offset = "0xF4D600", Length = "0x241")]
	private void UnhighlightCurrentWorld()
	{
	}

	[Token(Token = "0x6005E67")]
	[Address(RVA = "0xF4A740", Offset = "0xF49740", Length = "0x235")]
	private void DeleteExistingWorldSelectionSlots()
	{
	}

	[Token(Token = "0x6005E68")]
	[Address(RVA = "0xF4E850", Offset = "0xF4D850", Length = "0xDA")]
	private void UpdateWorldSlotInt(Button button, int x)
	{
	}

	[Token(Token = "0x6005E69")]
	[Address(RVA = "0xF4A1B0", Offset = "0xF491B0", Length = "0x3E")]
	public void ChangePersonalLootToggle()
	{
	}

	[Token(Token = "0x6005E6A")]
	[Address(RVA = "0xF4DEF0", Offset = "0xF4CEF0", Length = "0x2E")]
	public void SetMultiplayer()
	{
	}

	[Token(Token = "0x6005E6B")]
	[Address(RVA = "0xF4EA90", Offset = "0xF4DA90", Length = "0x148")]
	private void WireSelectionTracking()
	{
	}

	[Token(Token = "0x6005E6C")]
	[Address(RVA = "0xF4BE70", Offset = "0xF4AE70", Length = "0xB8")]
	private void OnCoordinatorSelectionChanged(GameObject selected)
	{
	}

	[Token(Token = "0x6005E6D")]
	[Address(RVA = "0xF4D260", Offset = "0xF4C260", Length = "0x156")]
	private int RowIndexOf(GameObject go)
	{
		return 0;
	}

	[Token(Token = "0x6005E6E")]
	[Address(RVA = "0xF4D3C0", Offset = "0xF4C3C0", Length = "0x8F7")]
	public void SelectWorld(int x)
	{
	}

	[Token(Token = "0x6005E6F")]
	[Address(RVA = "0xF4BAB0", Offset = "0xF4AAB0", Length = "0x2AA")]
	private void HighlightCurrentWorld(int x)
	{
	}

	[Token(Token = "0x6005E70")]
	[Address(RVA = "0xF4B320", Offset = "0xF4A320", Length = "0x77")]
	private Sprite GetWorldDifficultyHighlightedSprite(WorldSaveFile file)
	{
		return null;
	}

	[Token(Token = "0x6005E71")]
	[Address(RVA = "0xF4A1F0", Offset = "0xF491F0", Length = "0x19D")]
	public void ConfirmWorldDeletion()
	{
	}

	[Token(Token = "0x6005E72")]
	[Address(RVA = "0xF4A170", Offset = "0xF49170", Length = "0x33")]
	public void CancelWorldDeletion()
	{
	}

	[Token(Token = "0x6005E73")]
	[Address(RVA = "0xF4CF80", Offset = "0xF4BF80", Length = "0x2DB")]
	private void RestorePrePopupFocus()
	{
	}

	[Token(Token = "0x6005E74")]
	[Address(RVA = "0xF4DE10", Offset = "0xF4CE10", Length = "0xD2")]
	private GameObject SelectedWorldSlot()
	{
		return null;
	}

	[Token(Token = "0x6005E75")]
	[Address(RVA = "0x8F4F30", Offset = "0x8F3F30", Length = "0x8")]
	public List<WorldSaveFile> ReturnWorldSaves()
	{
		return null;
	}

	[Token(Token = "0x6005E76")]
	[Address(RVA = "0xF4A530", Offset = "0xF49530", Length = "0x20A")]
	public void DeleteCurrentlySelectedWorld()
	{
	}

	[Token(Token = "0x6005E77")]
	[Address(RVA = "0xF4B3A0", Offset = "0xF4A3A0", Length = "0x77")]
	public static Sprite GetWorldDifficultySprite(WorldSaveFile file)
	{
		return null;
	}

	[Token(Token = "0x6005E78")]
	[Address(RVA = "0xF4B420", Offset = "0xF4A420", Length = "0x77")]
	public static Sprite GetWorldDifficultyWorld(WorldSaveFile file)
	{
		return null;
	}

	[Token(Token = "0x6005E79")]
	[Address(RVA = "0xF4DF20", Offset = "0xF4CF20", Length = "0x3B1")]
	private void ShowNewCharactersOnlyPopup()
	{
	}

	[Token(Token = "0x6005E7A")]
	[Address(RVA = "0xF4B1C0", Offset = "0xF4A1C0", Length = "0x15B")]
	private string GetLocalizedError(string key, string fallback)
	{
		return null;
	}

	[IteratorStateMachine(typeof(<FadeOutNotReadyPopup>d__92))]
	[Token(Token = "0x6005E7B")]
	[Address(RVA = "0xF4AD20", Offset = "0xF49D20", Length = "0x79")]
	private IEnumerator FadeOutNotReadyPopup(float delayBeforeFade)
	{
		return null;
	}

	[Token(Token = "0x6005E7C")]
	[Address(RVA = "0xF4A980", Offset = "0xF49980", Length = "0x165")]
	private bool DoesCharacterFailNewPlayersOnlyCheck(WorldSaveFile worldFile)
	{
		return false;
	}

	[Token(Token = "0x6005E7D")]
	[Address(RVA = "0xF4EE30", Offset = "0xF4DE30", Length = "0x18")]
	public WorldSelection()
	{
	}

	[Token(Token = "0x6005E7E")]
	[Address(RVA = "0xF4BD60", Offset = "0xF4AD60", Length = "0x8")]
	bool IUIActionReceiver.CanHandle(in UIActionContext ctx)
	{
		return false;
	}

	[Token(Token = "0x6005E7F")]
	[Address(RVA = "0xF4BD70", Offset = "0xF4AD70", Length = "0x8")]
	UIActionResult IUIActionReceiver.Handle(in UIActionContext ctx)
	{
		return default(UIActionResult);
	}
}
