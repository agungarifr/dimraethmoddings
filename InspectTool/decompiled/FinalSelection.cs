using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Cpp2ILInjected;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Token(Token = "0x2000B5A")]
public class FinalSelection : MonoBehaviour, IUIActionReceiver
{
	[CompilerGenerated]
	[Token(Token = "0x2000B5D")]
	private sealed class <FadeOutNotReadyPopup>d__135 : IEnumerator<object>, IEnumerator, IDisposable
	{
		[Token(Token = "0x400569E")]
		[FieldOffset(Offset = "0x10")]
		private int <>1__state;

		[Token(Token = "0x400569F")]
		[FieldOffset(Offset = "0x18")]
		private object <>2__current;

		[Token(Token = "0x40056A0")]
		[FieldOffset(Offset = "0x20")]
		public FinalSelection <>4__this;

		[Token(Token = "0x40056A1")]
		[FieldOffset(Offset = "0x28")]
		public float delayBeforeFade;

		[Token(Token = "0x40056A2")]
		[FieldOffset(Offset = "0x2C")]
		private float <elapsed>5__2;

		[Token(Token = "0x170009EB")]
		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			[Token(Token = "0x6005D10")]
			[Address(RVA = "0x6B1E50", Offset = "0x6B0E50", Length = "0x5")]
			get
			{
				return null;
			}
		}

		[Token(Token = "0x170009EC")]
		object IEnumerator.Current
		{
			[DebuggerHidden]
			[Token(Token = "0x6005D12")]
			[Address(RVA = "0x6B1E50", Offset = "0x6B0E50", Length = "0x5")]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		[Token(Token = "0x6005D0D")]
		[Address(RVA = "0x6B1EA0", Offset = "0x6B0EA0", Length = "0x24")]
		public <FadeOutNotReadyPopup>d__135(int <>1__state)
		{
		}

		[DebuggerHidden]
		[Token(Token = "0x6005D0E")]
		[Address(RVA = "0x6AAC60", Offset = "0x6A9C60", Length = "0x3")]
		void IDisposable.Dispose()
		{
		}

		[Token(Token = "0x6005D0F")]
		[Address(RVA = "0xF288D0", Offset = "0xF278D0", Length = "0x1F4")]
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
		[Token(Token = "0x6005D11")]
		[Address(RVA = "0xF28AD0", Offset = "0xF27AD0", Length = "0x3E")]
		void IEnumerator.Reset()
		{
		}
	}

	[CompilerGenerated]
	[Token(Token = "0x2000B5E")]
	private sealed class <SetInitialControllerSelection>d__103 : IEnumerator<object>, IEnumerator, IDisposable
	{
		[Token(Token = "0x40056A3")]
		[FieldOffset(Offset = "0x10")]
		private int <>1__state;

		[Token(Token = "0x40056A4")]
		[FieldOffset(Offset = "0x18")]
		private object <>2__current;

		[Token(Token = "0x40056A5")]
		[FieldOffset(Offset = "0x20")]
		public FinalSelection <>4__this;

		[Token(Token = "0x170009ED")]
		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			[Token(Token = "0x6005D16")]
			[Address(RVA = "0x6B1E50", Offset = "0x6B0E50", Length = "0x5")]
			get
			{
				return null;
			}
		}

		[Token(Token = "0x170009EE")]
		object IEnumerator.Current
		{
			[DebuggerHidden]
			[Token(Token = "0x6005D18")]
			[Address(RVA = "0x6B1E50", Offset = "0x6B0E50", Length = "0x5")]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		[Token(Token = "0x6005D13")]
		[Address(RVA = "0x6B1EA0", Offset = "0x6B0EA0", Length = "0x24")]
		public <SetInitialControllerSelection>d__103(int <>1__state)
		{
		}

		[DebuggerHidden]
		[Token(Token = "0x6005D14")]
		[Address(RVA = "0x6AAC60", Offset = "0x6A9C60", Length = "0x3")]
		void IDisposable.Dispose()
		{
		}

		[Token(Token = "0x6005D15")]
		[Address(RVA = "0xF293A0", Offset = "0xF283A0", Length = "0x62")]
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
		[Token(Token = "0x6005D17")]
		[Address(RVA = "0xF29410", Offset = "0xF28410", Length = "0x3E")]
		void IEnumerator.Reset()
		{
		}
	}

	[Token(Token = "0x4005646")]
	public static FinalSelection Singleton;

	[Header("World Panels")]
	[SerializeField]
	[Token(Token = "0x4005647")]
	[FieldOffset(Offset = "0x20")]
	private GameObject _simpleWorld;

	[SerializeField]
	[Token(Token = "0x4005648")]
	[FieldOffset(Offset = "0x28")]
	private GameObject _worldInfo;

	[SerializeField]
	[Token(Token = "0x4005649")]
	[FieldOffset(Offset = "0x30")]
	private GameObject _changeWorldBtn;

	[SerializeField]
	[Token(Token = "0x400564A")]
	[FieldOffset(Offset = "0x38")]
	private GameObject _note;

	[SerializeField]
	[Token(Token = "0x400564B")]
	[FieldOffset(Offset = "0x40")]
	private GameObject _multiplayerHost;

	[SerializeField]
	[Token(Token = "0x400564C")]
	[FieldOffset(Offset = "0x48")]
	private GameObject _multiplayerJoin;

	[Header("Simple World (New World Creation)")]
	[SerializeField]
	[Token(Token = "0x400564D")]
	[FieldOffset(Offset = "0x50")]
	private TMP_InputField _simpleWorldNameInput;

	[SerializeField]
	[Token(Token = "0x400564E")]
	[FieldOffset(Offset = "0x58")]
	private TextMeshProUGUI _simpleWorldNameError;

	[SerializeField]
	[Token(Token = "0x400564F")]
	[FieldOffset(Offset = "0x60")]
	private Button _startButton;

	[Tooltip("Difficulty slider on the simple (new world) panel. Whole values 0-2 map to Easy, Moderate and Hard; the new world is created with whatever it shows.")]
	[SerializeField]
	[Token(Token = "0x4005650")]
	[FieldOffset(Offset = "0x68")]
	private Slider _simpleWorldDifficultySlider;

	[Tooltip("Adjust-difficulty hint in the simple panel's controller tutorial. Authored with Manual visibility, so this screen shows it only while the difficulty slider is the focused control.")]
	[SerializeField]
	[Token(Token = "0x4005651")]
	[FieldOffset(Offset = "0x70")]
	private UIActionHint _adjustDifficultyHint;

	[Header("Character Preview")]
	[SerializeField]
	[Token(Token = "0x4005652")]
	[FieldOffset(Offset = "0x78")]
	private Image _bannerPreview;

	[SerializeField]
	[Token(Token = "0x4005653")]
	[FieldOffset(Offset = "0x80")]
	private List<RacePreviewEntry> _characterPreviews;

	[SerializeField]
	[Token(Token = "0x4005654")]
	[FieldOffset(Offset = "0x88")]
	private TextMeshProUGUI _characterNamePreview;

	[SerializeField]
	[Token(Token = "0x4005655")]
	[FieldOffset(Offset = "0x90")]
	private TextMeshProUGUI _characterLevelPreview;

	[SerializeField]
	[Token(Token = "0x4005656")]
	[FieldOffset(Offset = "0x98")]
	private TextMeshProUGUI _characterHoursPreview;

	[SerializeField]
	[Token(Token = "0x4005657")]
	[FieldOffset(Offset = "0xA0")]
	private TextMeshProUGUI _characterDifficultyPreview;

	[SerializeField]
	[Token(Token = "0x4005658")]
	[FieldOffset(Offset = "0xA8")]
	private TextMeshProUGUI _characterArchetype;

	[SerializeField]
	[Token(Token = "0x4005659")]
	[FieldOffset(Offset = "0xB0")]
	private TextMeshProUGUI _characterScene;

	[SerializeField]
	[Token(Token = "0x400565A")]
	[FieldOffset(Offset = "0xB8")]
	private TextMeshProUGUI _memory;

	[SerializeField]
	[Token(Token = "0x400565B")]
	[FieldOffset(Offset = "0xC0")]
	private TextMeshProUGUI _intelligence;

	[SerializeField]
	[Token(Token = "0x400565C")]
	[FieldOffset(Offset = "0xC8")]
	private TextMeshProUGUI _physique;

	[SerializeField]
	[Token(Token = "0x400565D")]
	[FieldOffset(Offset = "0xD0")]
	private TextMeshProUGUI _strength;

	[SerializeField]
	[Token(Token = "0x400565E")]
	[FieldOffset(Offset = "0xD8")]
	private TextMeshProUGUI _adventure;

	[SerializeField]
	[Token(Token = "0x400565F")]
	[FieldOffset(Offset = "0xE0")]
	private TextMeshProUGUI _charisma;

	[SerializeField]
	[Token(Token = "0x4005660")]
	[FieldOffset(Offset = "0xE8")]
	private TextMeshProUGUI _agility;

	[SerializeField]
	[Token(Token = "0x4005661")]
	[FieldOffset(Offset = "0xF0")]
	private TextMeshProUGUI _energy;

	[Header("World Info")]
	[SerializeField]
	[Token(Token = "0x4005662")]
	[FieldOffset(Offset = "0xF8")]
	private GameObject _worldName;

	[SerializeField]
	[Token(Token = "0x4005663")]
	[FieldOffset(Offset = "0x100")]
	private Image _backgroundDifficultyPreview;

	[SerializeField]
	[Token(Token = "0x4005664")]
	[FieldOffset(Offset = "0x108")]
	private Image _worldDifficultyPreview;

	[SerializeField]
	[Token(Token = "0x4005665")]
	[FieldOffset(Offset = "0x110")]
	private TextMeshProUGUI _lobbyCount;

	[SerializeField]
	[Token(Token = "0x4005666")]
	[FieldOffset(Offset = "0x118")]
	private TMP_InputField _passwordInput;

	[SerializeField]
	[Token(Token = "0x4005667")]
	[FieldOffset(Offset = "0x120")]
	private TMP_InputField _lobbyNoteInput;

	[SerializeField]
	[Token(Token = "0x4005668")]
	[FieldOffset(Offset = "0x128")]
	private TextMeshProUGUI _worldNamePreview;

	[SerializeField]
	[Token(Token = "0x4005669")]
	[FieldOffset(Offset = "0x130")]
	private List<TextMeshProUGUI> _worldAgeTexts;

	[SerializeField]
	[Token(Token = "0x400566A")]
	[FieldOffset(Offset = "0x138")]
	private List<TextMeshProUGUI> _worldDifficultyTexts;

	[SerializeField]
	[Token(Token = "0x400566B")]
	[FieldOffset(Offset = "0x140")]
	private List<TextMeshProUGUI> _worldTypeTexts;

	[SerializeField]
	[Token(Token = "0x400566C")]
	[FieldOffset(Offset = "0x148")]
	private Toggle _friendlyFire;

	[SerializeField]
	[Token(Token = "0x400566D")]
	[FieldOffset(Offset = "0x150")]
	private Toggle _newCharactersOnly;

	[Tooltip("Personal Loot switch on the multiplayer-host lobby-options row. Unlike the two above it is EDITABLE, not a read-only display: it writes the world's own PersonalLoot flag, which StartGame then persists.")]
	[SerializeField]
	[Token(Token = "0x400566E")]
	[FieldOffset(Offset = "0x158")]
	private Toggle _personalLoot;

	[SerializeField]
	[Token(Token = "0x400566F")]
	[FieldOffset(Offset = "0x160")]
	public Toggle LobbyOpen;

	[SerializeField]
	[Token(Token = "0x4005670")]
	[FieldOffset(Offset = "0x168")]
	public Toggle FriendsOnly;

	[SerializeField]
	[Token(Token = "0x4005671")]
	[FieldOffset(Offset = "0x170")]
	public Toggle InviteOnly;

	[SerializeField]
	[Token(Token = "0x4005672")]
	[FieldOffset(Offset = "0x178")]
	public Button _decreaseButton;

	[SerializeField]
	[Token(Token = "0x4005673")]
	[FieldOffset(Offset = "0x180")]
	public Button _increaseButton;

	[SerializeField]
	[Token(Token = "0x4005674")]
	[FieldOffset(Offset = "0x188")]
	public TextMeshProUGUI _maxPlayersDisplay;

	[SerializeField]
	[Token(Token = "0x4005675")]
	[FieldOffset(Offset = "0x190")]
	public GameObject _maxPlayersWarning;

	[SerializeField]
	[Token(Token = "0x4005676")]
	[FieldOffset(Offset = "0x198")]
	public TextMeshProUGUI _maxPlayersForServer;

	[Tooltip("Selectable on the multiplayer-host MaxPlayers control; the EditPlayerLimit action (gamepad button west) moves selection here, and left/right adjusts the player limit while it is selected.")]
	[SerializeField]
	[Token(Token = "0x4005677")]
	[FieldOffset(Offset = "0x1A0")]
	private Selectable _maxPlayersSelectable;

	[Tooltip("Adjust-player-limit hint on the multiplayer-host panel. Authored with Manual visibility, so this screen shows it only while the MaxPlayers control is the focused control.")]
	[SerializeField]
	[Token(Token = "0x4005678")]
	[FieldOffset(Offset = "0x1A8")]
	private UIActionHint _adjustPlayerLimitHint;

	[SerializeField]
	[Token(Token = "0x4005679")]
	[FieldOffset(Offset = "0x1B0")]
	private CanvasGroup _notReadyPopUp;

	[SerializeField]
	[Token(Token = "0x400567A")]
	[FieldOffset(Offset = "0x1B8")]
	private TextMeshProUGUI _errorText;

	[Header("Page Wiring")]
	[Tooltip("Back button; runs the same host/client-dependent Back routing as gamepad B / Escape.")]
	[SerializeField]
	[Token(Token = "0x400567B")]
	[FieldOffset(Offset = "0x1C0")]
	private Button _backButton;

	[Tooltip("Change World button on the world info panel; returns to WorldSelection.")]
	[SerializeField]
	[Token(Token = "0x400567C")]
	[FieldOffset(Offset = "0x1C8")]
	private Button _changeWorldButton;

	[Tooltip("Change World button on the simple (new world) panel; returns to WorldSelection.")]
	[SerializeField]
	[Token(Token = "0x400567D")]
	[FieldOffset(Offset = "0x1D0")]
	private Button _simpleChangeWorldButton;

	[Tooltip("Multiplayer-host panel button returning to WorldSelection.")]
	[SerializeField]
	[Token(Token = "0x400567E")]
	[FieldOffset(Offset = "0x1D8")]
	private Button _backToWorldSelectButton;

	[Tooltip("Multiplayer-join panel button returning to the MultiplayerLobby.")]
	[SerializeField]
	[Token(Token = "0x400567F")]
	[FieldOffset(Offset = "0x1E0")]
	private Button _backToLobbySelectButton;

	[Tooltip("Toggles mirroring the world's Personal Loot / Hardcore option; each forwards to WorldSelection.ChangePersonalLootToggle.")]
	[SerializeField]
	[Token(Token = "0x4005680")]
	[FieldOffset(Offset = "0x1E8")]
	private List<Toggle> _personalLootMirrorToggles;

	[Tooltip("Text-input widgets on this screen's fields: the simple-world name, every lobby note copy and the multiplayer password. Each is bound to a text-input presenter so a live edit owns all input and the field only enters edit mode on Submit or a pointer click.")]
	[SerializeField]
	[Token(Token = "0x4005681")]
	[FieldOffset(Offset = "0x1F0")]
	private List<UITextInputView> _textInputViews;

	[Token(Token = "0x4005682")]
	[FieldOffset(Offset = "0x1F8")]
	private Coroutine _notReadyCoroutine;

	[Token(Token = "0x4005683")]
	[FieldOffset(Offset = "0x200")]
	private PlayerSaveFile _playerFile;

	[Token(Token = "0x4005684")]
	[FieldOffset(Offset = "0x208")]
	private WorldSaveFile _worldFile;

	[Token(Token = "0x4005685")]
	[FieldOffset(Offset = "0x210")]
	private int _localPlayers;

	[Token(Token = "0x4005686")]
	[FieldOffset(Offset = "0x214")]
	private bool _isNewWorldMode;

	[Token(Token = "0x4005687")]
	[FieldOffset(Offset = "0x215")]
	private bool _userChangedWorld;

	[Token(Token = "0x4005688")]
	[FieldOffset(Offset = "0x218")]
	private string _worldChoiceOwner;

	[Token(Token = "0x4005689")]
	[FieldOffset(Offset = "0x220")]
	private bool _privacyListenersWired;

	[Token(Token = "0x400568A")]
	[FieldOffset(Offset = "0x221")]
	private bool _personalLootListenerWired;

	[Token(Token = "0x400568B")]
	[FieldOffset(Offset = "0x222")]
	private bool _personalLootDirty;

	[Token(Token = "0x400568C")]
	[FieldOffset(Offset = "0x223")]
	private bool _difficultyListenerWired;

	[Token(Token = "0x400568D")]
	[FieldOffset(Offset = "0x224")]
	private Difficulty _selectedDifficulty;

	[Token(Token = "0x400568E")]
	[FieldOffset(Offset = "0x228")]
	private UIFocusScope _focusScope;

	[Token(Token = "0x400568F")]
	[FieldOffset(Offset = "0x230")]
	private UINode _node;

	[Token(Token = "0x4005690")]
	[FieldOffset(Offset = "0x238")]
	private MainMenuTabNavigator _tabNavigator;

	[Token(Token = "0x4005691")]
	[FieldOffset(Offset = "0x240")]
	private UIFocusCoordinator _tabNavigatorCoordinator;

	[Token(Token = "0x4005692")]
	[FieldOffset(Offset = "0x248")]
	private readonly List<UITextInputPresenter> _textInputPresenters;

	[Token(Token = "0x4005693")]
	[FieldOffset(Offset = "0x250")]
	private Selectable _maxPlayersReturnSelectable;

	[Token(Token = "0x4005694")]
	[FieldOffset(Offset = "0x258")]
	private GameObject _lastCoordinatorSelection;

	[Token(Token = "0x4005695")]
	[FieldOffset(Offset = "0x260")]
	private UIFocusCoordinator _selectionTrackedCoordinator;

	[Token(Token = "0x4005696")]
	private const int MinSeats = 2;

	[Token(Token = "0x4005697")]
	public const string PrefLastMaxPlayers = "FinalSelection_LastMaxPlayers";

	[Token(Token = "0x4005698")]
	public const string PrefLastLobbyType = "FinalSelection_LastLobbyType";

	[Token(Token = "0x4005699")]
	public const string PrefLastSessionMultiplayer = "FinalSelection_LastSessionMultiplayer";

	[Token(Token = "0x6005CCB")]
	[Address(RVA = "0xF0BFF0", Offset = "0xF0AFF0", Length = "0xC5E")]
	private void Awake()
	{
	}

	[Token(Token = "0x6005CCC")]
	[Address(RVA = "0xF108A0", Offset = "0xF0F8A0", Length = "0x2AE")]
	private void OnDestroy()
	{
	}

	[Token(Token = "0x6005CCD")]
	[Address(RVA = "0xF0CC50", Offset = "0xF0BC50", Length = "0x46")]
	private void Back()
	{
	}

	[Token(Token = "0x6005CCE")]
	[Address(RVA = "0xF0CED0", Offset = "0xF0BED0", Length = "0x7A")]
	private void ChangeWorld()
	{
	}

	[Token(Token = "0x6005CCF")]
	[Address(RVA = "0xF10BF0", Offset = "0xF0FBF0", Length = "0x46")]
	private void OnPersonalLootMirrorChanged(bool _)
	{
	}

	[Token(Token = "0x6005CD0")]
	[Address(RVA = "0xF14160", Offset = "0xF13160", Length = "0xF7")]
	private void WirePersonalLootToggle()
	{
	}

	[Token(Token = "0x6005CD1")]
	[Address(RVA = "0xF10B90", Offset = "0xF0FB90", Length = "0x53")]
	private void OnPersonalLootChanged(bool value)
	{
	}

	[Token(Token = "0x6005CD2")]
	[Address(RVA = "0xF14740", Offset = "0xF13740", Length = "0x334")]
	private void WireTextInputPresenters()
	{
	}

	[Token(Token = "0x6005CD3")]
	[Address(RVA = "0xF14360", Offset = "0xF13360", Length = "0x148")]
	private void WireSelectionTracking()
	{
	}

	[Token(Token = "0x6005CD4")]
	[Address(RVA = "0xF144B0", Offset = "0xF134B0", Length = "0x28A")]
	private void WireTabNavigator()
	{
	}

	[Token(Token = "0x6005CD5")]
	[Address(RVA = "0xF110D0", Offset = "0xF100D0", Length = "0x1EB")]
	private Selectable PanelEnd(bool top)
	{
		return null;
	}

	[Token(Token = "0x6005CD6")]
	[Address(RVA = "0xF10730", Offset = "0xF0F730", Length = "0x16F")]
	private void OnCoordinatorSelectionChanged(GameObject selected)
	{
	}

	[Token(Token = "0x6005CD7")]
	[Address(RVA = "0xF104B0", Offset = "0xF0F4B0", Length = "0x125")]
	private bool IsMaxPlayersFocused()
	{
		return false;
	}

	[Token(Token = "0x6005CD8")]
	[Address(RVA = "0xF113C0", Offset = "0xF103C0", Length = "0x24F")]
	private Button ResolveChangeWorldButton()
	{
		return null;
	}

	[Token(Token = "0x6005CD9")]
	[Address(RVA = "0xF0CCA0", Offset = "0xF0BCA0", Length = "0x222")]
	public bool CanHandle(in UIActionContext ctx)
	{
		return false;
	}

	[Token(Token = "0x6005CDA")]
	[Address(RVA = "0xF0F440", Offset = "0xF0E440", Length = "0x515")]
	public UIActionResult Handle(in UIActionContext ctx)
	{
		return default(UIActionResult);
	}

	[Token(Token = "0x6005CDB")]
	[Address(RVA = "0xF10720", Offset = "0xF0F720", Length = "0x8")]
	public void NotifyUserChangedWorld()
	{
	}

	[Token(Token = "0x6005CDC")]
	[Address(RVA = "0xF11370", Offset = "0xF10370", Length = "0x4E")]
	private void RearmWorldChoiceForCharacter(string characterName)
	{
	}

	[Token(Token = "0x6005CDD")]
	[Address(RVA = "0xF11610", Offset = "0xF10610", Length = "0x46")]
	private bool ResolveNewWorldMode(bool isNewCharacter, bool isMultiplayer)
	{
		return false;
	}

	[Token(Token = "0x6005CDE")]
	[Address(RVA = "0xF0FA00", Offset = "0xF0EA00", Length = "0x80F")]
	public void InitializeFinalSelection()
	{
	}

	[Token(Token = "0x6005CDF")]
	[Address(RVA = "0xF0BCC0", Offset = "0xF0ACC0", Length = "0x32A")]
	private void ApplyWorldTypeLabel(bool isMultiplayer)
	{
	}

	[Token(Token = "0x6005CE0")]
	[Address(RVA = "0xF0EE50", Offset = "0xF0DE50", Length = "0x481")]
	private Selectable FirstControllerTarget()
	{
		return null;
	}

	[Token(Token = "0x6005CE1")]
	[Address(RVA = "0xF105E0", Offset = "0xF0F5E0", Length = "0x87")]
	private static bool IsTargetable(Selectable s)
	{
		return false;
	}

	[IteratorStateMachine(typeof(<SetInitialControllerSelection>d__103))]
	[Token(Token = "0x6005CE2")]
	[Address(RVA = "0xF11AA0", Offset = "0xF10AA0", Length = "0x67")]
	private IEnumerator SetInitialControllerSelection()
	{
		return null;
	}

	[Token(Token = "0x6005CE3")]
	[Address(RVA = "0xF10B80", Offset = "0xF0FB80", Length = "0x7")]
	private void OnEnable()
	{
	}

	[Token(Token = "0x6005CE4")]
	[Address(RVA = "0xF117E0", Offset = "0xF107E0", Length = "0x194")]
	private void SeedInitialControllerSelection()
	{
	}

	[Token(Token = "0x6005CE5")]
	[Address(RVA = "0xF11EA0", Offset = "0xF10EA0", Length = "0x5B0")]
	private void SetupPanels()
	{
	}

	[Token(Token = "0x6005CE6")]
	[Address(RVA = "0xF12460", Offset = "0xF11460", Length = "0xB8")]
	private void SetupSimpleWorld()
	{
	}

	[Token(Token = "0x6005CE7")]
	[Address(RVA = "0xF139B0", Offset = "0xF129B0", Length = "0x391")]
	private void Update()
	{
	}

	[Token(Token = "0x6005CE8")]
	[Address(RVA = "0xF10F20", Offset = "0xF0FF20", Length = "0x1A1")]
	public void OnSimpleWorldNameUpdate()
	{
	}

	[Token(Token = "0x6005CE9")]
	[Address(RVA = "0xF10670", Offset = "0xF0F670", Length = "0xAF")]
	private string NormalizedWorldName()
	{
		return null;
	}

	[Token(Token = "0x6005CEA")]
	[Address(RVA = "0xF13D50", Offset = "0xF12D50", Length = "0x30D")]
	private (bool, string) ValidateSimpleWorldName()
	{
		return default((bool, string));
	}

	[Token(Token = "0x6005CEB")]
	[Address(RVA = "0xF13920", Offset = "0xF12920", Length = "0x84")]
	public void UpdateLobbyCount(int lobbyCount, int maxMembers)
	{
	}

	[Token(Token = "0x6005CEC")]
	[Address(RVA = "0xF0F980", Offset = "0xF0E980", Length = "0x78")]
	public void IncreaseMaxPlayers()
	{
	}

	[Token(Token = "0x6005CED")]
	[Address(RVA = "0xF0CF50", Offset = "0xF0BF50", Length = "0x17")]
	public void DecreaseMaxPlayers()
	{
	}

	[Token(Token = "0x6005CEE")]
	[Address(RVA = "0xF14260", Offset = "0xF13260", Length = "0xF7")]
	private void WirePrivacyToggleListeners()
	{
	}

	[Token(Token = "0x6005CEF")]
	[Address(RVA = "0xF14060", Offset = "0xF13060", Length = "0xF1")]
	private void WireDifficultySlider()
	{
	}

	[Token(Token = "0x6005CF0")]
	[Address(RVA = "0xF112C0", Offset = "0xF102C0", Length = "0xAE")]
	private Difficulty ReadSliderDifficulty()
	{
		return default(Difficulty);
	}

	[Token(Token = "0x6005CF1")]
	[Address(RVA = "0xF10B50", Offset = "0xF0FB50", Length = "0x22")]
	private void OnDifficultySliderChanged(float _)
	{
	}

	[Token(Token = "0x6005CF2")]
	[Address(RVA = "0xF11980", Offset = "0xF10980", Length = "0x85")]
	private void SetAdjustDifficultyHintVisible(bool visible)
	{
	}

	[Token(Token = "0x6005CF3")]
	[Address(RVA = "0xF11A10", Offset = "0xF10A10", Length = "0x8B")]
	private void SetAdjustPlayerLimitHintVisible(bool visible)
	{
	}

	[Token(Token = "0x6005CF4")]
	[Address(RVA = "0xF11B10", Offset = "0xF10B10", Length = "0x29C")]
	private void SetSelectedDifficulty(Difficulty difficulty)
	{
	}

	[Token(Token = "0x6005CF5")]
	[Address(RVA = "0xF10C40", Offset = "0xF0FC40", Length = "0x2D8")]
	private void OnPlayerCountChanged()
	{
	}

	[Token(Token = "0x6005CF6")]
	[Address(RVA = "0xF12520", Offset = "0xF11520", Length = "0x2E7")]
	private static void ShowCharacterPreview(GameObject previewObject, Race race, Gender gender, AppearanceColors colors)
	{
	}

	[Token(Token = "0x6005CF7")]
	[Address(RVA = "0xF0CFF0", Offset = "0xF0BFF0", Length = "0xB83")]
	private void FillInPlayerValues()
	{
	}

	[Token(Token = "0x6005CF8")]
	[Address(RVA = "0xF0DB80", Offset = "0xF0CB80", Length = "0x12C0")]
	private void FillInWorldValues()
	{
	}

	[Token(Token = "0x6005CF9")]
	[Address(RVA = "0xF11660", Offset = "0xF10660", Length = "0x174")]
	private void RestoreLobbyPrivacyChoice()
	{
	}

	[Token(Token = "0x6005CFA")]
	[Address(RVA = "0xF11DB0", Offset = "0xF10DB0", Length = "0xE5")]
	private void SetSteamLobbyOptions()
	{
	}

	[Token(Token = "0x6005CFB")]
	[Address(RVA = "0xF12BB0", Offset = "0xF11BB0", Length = "0xD3C")]
	public void StartGame()
	{
	}

	[Token(Token = "0x6005CFC")]
	[Address(RVA = "0xF12810", Offset = "0xF11810", Length = "0x39F")]
	private void ShowNewCharactersOnlyPopup()
	{
	}

	[Token(Token = "0x6005CFD")]
	[Address(RVA = "0xF0F2E0", Offset = "0xF0E2E0", Length = "0x15B")]
	private string GetLocalizedError(string key, string fallback)
	{
		return null;
	}

	[IteratorStateMachine(typeof(<FadeOutNotReadyPopup>d__135))]
	[Token(Token = "0x6005CFE")]
	[Address(RVA = "0xF0CF70", Offset = "0xF0BF70", Length = "0x79")]
	private IEnumerator FadeOutNotReadyPopup(float delayBeforeFade)
	{
		return null;
	}

	[Token(Token = "0x6005CFF")]
	[Address(RVA = "0xF10390", Offset = "0xF0F390", Length = "0x111")]
	private bool IsCharacterAllowedForWorld()
	{
		return false;
	}

	[Token(Token = "0x6005D00")]
	[Address(RVA = "0xF10210", Offset = "0xF0F210", Length = "0x17E")]
	private bool IsCharacterAllowedForServer()
	{
		return false;
	}

	[Token(Token = "0x6005D01")]
	[Address(RVA = "0xF14A80", Offset = "0xF13A80", Length = "0x211")]
	public FinalSelection()
	{
	}

	[Token(Token = "0x6005D02")]
	[Address(RVA = "0xF0F960", Offset = "0xF0E960", Length = "0x8")]
	bool IUIActionReceiver.CanHandle(in UIActionContext ctx)
	{
		return false;
	}

	[Token(Token = "0x6005D03")]
	[Address(RVA = "0xF0F970", Offset = "0xF0E970", Length = "0x8")]
	UIActionResult IUIActionReceiver.Handle(in UIActionContext ctx)
	{
		return default(UIActionResult);
	}
}
