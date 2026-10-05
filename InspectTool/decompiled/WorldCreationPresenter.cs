using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

public sealed class WorldCreationPresenter : Il2CppSystem.Object
{
	[ObfuscatedName("WorldCreationPresenter+<>c__DisplayClass32_0")]
	public sealed class __c__DisplayClass32_0 : Il2CppSystem.Object
	{
		private static readonly System.IntPtr NativeFieldInfoPtr_name;

		private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

		private static readonly System.IntPtr NativeMethodInfoPtr__ValidWorldNameCheck_b__0_Internal_Boolean_WorldSaveFile_0;

		public unsafe string name
		{
			get
			{
				nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_name);
				return IL2CPP.Il2CppStringToManaged(*(System.IntPtr*)num);
			}
			set
			{
				System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
				IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_name), IL2CPP.ManagedStringToIl2Cpp(str));
			}
		}

		static __c__DisplayClass32_0()
		{
			Il2CppClassPointerStore<__c__DisplayClass32_0>.NativeClassPtr = IL2CPP.GetIl2CppNestedType(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "<>c__DisplayClass32_0");
			IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<__c__DisplayClass32_0>.NativeClassPtr);
			NativeFieldInfoPtr_name = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<__c__DisplayClass32_0>.NativeClassPtr, "name");
			NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<__c__DisplayClass32_0>.NativeClassPtr, 100696330);
			NativeMethodInfoPtr__ValidWorldNameCheck_b__0_Internal_Boolean_WorldSaveFile_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<__c__DisplayClass32_0>.NativeClassPtr, 100696331);
		}

		[CallerCount(3573)]
		[CachedScanResults(RefRangeStart = 24693, RefRangeEnd = 28266, XrefRangeStart = 24693, XrefRangeEnd = 28266, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		public unsafe __c__DisplayClass32_0()
			: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<__c__DisplayClass32_0>.NativeClassPtr))
		{
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		}

		[CallerCount(0)]
		[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 531605, XrefRangeEnd = 531610, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		public unsafe bool _ValidWorldNameCheck_b__0(WorldSaveFile f)
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* ptr = stackalloc System.IntPtr[1];
			*ptr = IL2CPP.Il2CppObjectBaseToPtr(f);
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ValidWorldNameCheck_b__0_Internal_Boolean_WorldSaveFile_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return *(bool*)IL2CPP.il2cpp_object_unbox(obj);
		}

		public __c__DisplayClass32_0(System.IntPtr pointer)
			: base(pointer)
		{
		}
	}

	private static readonly System.IntPtr NativeFieldInfoPtr__view;

	private static readonly System.IntPtr NativeFieldInfoPtr__node;

	private static readonly System.IntPtr NativeFieldInfoPtr__focusCoordinator;

	private static readonly System.IntPtr NativeFieldInfoPtr__focusScope;

	private static readonly System.IntPtr NativeFieldInfoPtr__navigator;

	private static readonly System.IntPtr NativeFieldInfoPtr__nameInputPresenter;

	private static readonly System.IntPtr NativeFieldInfoPtr__onNameChanged;

	private static readonly System.IntPtr NativeFieldInfoPtr__onNameSubmitted;

	private static readonly System.IntPtr NativeFieldInfoPtr__onDifficultyChanged;

	private static readonly System.IntPtr NativeFieldInfoPtr__onNewCharactersOnlyChanged;

	private static readonly System.IntPtr NativeFieldInfoPtr__onCreate;

	private static readonly System.IntPtr NativeFieldInfoPtr__onConfirm;

	private static readonly System.IntPtr NativeFieldInfoPtr__onCancel;

	private static readonly System.IntPtr NativeFieldInfoPtr__onBack;

	private static readonly System.IntPtr NativeFieldInfoPtr__onSelectionChanged;

	private static readonly System.IntPtr NativeFieldInfoPtr__currentDifficulty;

	private static readonly System.IntPtr NativeFieldInfoPtr__newCharactersOnlyBlocked;

	private static readonly System.IntPtr NativeFieldInfoPtr__confirmationVisible;

	private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_WorldCreationView_UIContext_UINode_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_CurrentDifficultySelection_Public_get_Difficulty_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_Repaint_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_OnDifficultyChanged_Private_Void_Single_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_OnNewCharactersOnlyChanged_Private_Void_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_IsNewCharactersOnlyBlocked_Private_Static_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_OnCreatePressed_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_OnCancelPressed_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_OnConfirmPressed_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CreateNewWorld_Private_Void_String_Difficulty_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CreateDefaultWorld_Public_Static_Void_String_Difficulty_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_PopulateNewWeathers_Private_Static_Dictionary_2_Kingdom_Weathers_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_NormalizedWorldName_Private_String_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_ValidWorldNameCheck_Public_Static_ValueTuple_2_Boolean_String_String_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetLocalizedError_Private_Static_String_String_String_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_SetupControllerNavigation_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CanHandle_Public_Boolean_byref_UIActionContext_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_Handle_Public_UIActionResult_byref_UIActionContext_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_PopupOpen_Private_get_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_FocusConfirmationConfirm_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_FocusDifficultySlider_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_OnSelectionChanged_Private_Void_GameObject_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_FocusWorldNameForEdit_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_EnsureNameInputPresenter_Private_Void_UIContext_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_Dispose_Public_Virtual_Final_New_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_IUIActionReceiver_CanHandle_Private_Virtual_Final_New_Boolean_byref_UIActionContext_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_IUIActionReceiver_Handle_Private_Virtual_Final_New_UIActionResult_byref_UIActionContext_0;

	private static readonly System.IntPtr NativeMethodInfoPtr___ctor_b__18_0_Private_Void_String_0;

	private static readonly System.IntPtr NativeMethodInfoPtr___ctor_b__18_1_Private_Void_String_0;

	private static readonly System.IntPtr NativeMethodInfoPtr___ctor_b__18_2_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__EnsureNameInputPresenter_b__43_0_Private_Void_String_0;

	public unsafe WorldCreationView _view
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__view);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<WorldCreationView>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__view), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe UINode _node
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__node);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<UINode>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__node), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe UIFocusCoordinator _focusCoordinator
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__focusCoordinator);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<UIFocusCoordinator>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__focusCoordinator), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe UIFocusScope _focusScope
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__focusScope);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<UIFocusScope>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__focusScope), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe WorldCreationNavigator _navigator
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__navigator);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<WorldCreationNavigator>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__navigator), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe UITextInputPresenter _nameInputPresenter
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__nameInputPresenter);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<UITextInputPresenter>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__nameInputPresenter), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action<string> _onNameChanged
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onNameChanged);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action<string>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onNameChanged), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action<string> _onNameSubmitted
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onNameSubmitted);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action<string>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onNameSubmitted), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action<float> _onDifficultyChanged
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onDifficultyChanged);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action<float>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onDifficultyChanged), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action<bool> _onNewCharactersOnlyChanged
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onNewCharactersOnlyChanged);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action<bool>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onNewCharactersOnlyChanged), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action _onCreate
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onCreate);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onCreate), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action _onConfirm
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onConfirm);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onConfirm), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action _onCancel
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onCancel);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onCancel), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action _onBack
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onBack);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onBack), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action<GameObject> _onSelectionChanged
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onSelectionChanged);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action<GameObject>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__onSelectionChanged), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Difficulty _currentDifficulty
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__currentDifficulty);
			return *(Difficulty*)num;
		}
		set
		{
			*(Difficulty*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__currentDifficulty)) = difficulty;
		}
	}

	public unsafe bool _newCharactersOnlyBlocked
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__newCharactersOnlyBlocked);
			return *(bool*)num;
		}
		set
		{
			*(bool*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__newCharactersOnlyBlocked)) = flag;
		}
	}

	public unsafe bool _confirmationVisible
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__confirmationVisible);
			return *(bool*)num;
		}
		set
		{
			*(bool*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__confirmationVisible)) = flag;
		}
	}

	public unsafe Difficulty CurrentDifficultySelection
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_CurrentDifficultySelection_Public_get_Difficulty_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return *(Difficulty*)IL2CPP.il2cpp_object_unbox(obj);
		}
	}

	public unsafe bool PopupOpen
	{
		[CallerCount(0)]
		[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532416, XrefRangeEnd = 532420, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_PopupOpen_Private_get_Boolean_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return *(bool*)IL2CPP.il2cpp_object_unbox(obj);
		}
	}

	static WorldCreationPresenter()
	{
		Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "WorldCreationPresenter");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr);
		NativeFieldInfoPtr__view = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_view");
		NativeFieldInfoPtr__node = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_node");
		NativeFieldInfoPtr__focusCoordinator = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_focusCoordinator");
		NativeFieldInfoPtr__focusScope = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_focusScope");
		NativeFieldInfoPtr__navigator = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_navigator");
		NativeFieldInfoPtr__nameInputPresenter = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_nameInputPresenter");
		NativeFieldInfoPtr__onNameChanged = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_onNameChanged");
		NativeFieldInfoPtr__onNameSubmitted = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_onNameSubmitted");
		NativeFieldInfoPtr__onDifficultyChanged = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_onDifficultyChanged");
		NativeFieldInfoPtr__onNewCharactersOnlyChanged = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_onNewCharactersOnlyChanged");
		NativeFieldInfoPtr__onCreate = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_onCreate");
		NativeFieldInfoPtr__onConfirm = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_onConfirm");
		NativeFieldInfoPtr__onCancel = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_onCancel");
		NativeFieldInfoPtr__onBack = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_onBack");
		NativeFieldInfoPtr__onSelectionChanged = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_onSelectionChanged");
		NativeFieldInfoPtr__currentDifficulty = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_currentDifficulty");
		NativeFieldInfoPtr__newCharactersOnlyBlocked = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_newCharactersOnlyBlocked");
		NativeFieldInfoPtr__confirmationVisible = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, "_confirmationVisible");
		NativeMethodInfoPtr__ctor_Public_Void_WorldCreationView_UIContext_UINode_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696299);
		NativeMethodInfoPtr_get_CurrentDifficultySelection_Public_get_Difficulty_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696300);
		NativeMethodInfoPtr_Repaint_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696301);
		NativeMethodInfoPtr_OnDifficultyChanged_Private_Void_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696302);
		NativeMethodInfoPtr_OnNewCharactersOnlyChanged_Private_Void_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696303);
		NativeMethodInfoPtr_IsNewCharactersOnlyBlocked_Private_Static_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696304);
		NativeMethodInfoPtr_OnCreatePressed_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696305);
		NativeMethodInfoPtr_OnCancelPressed_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696306);
		NativeMethodInfoPtr_OnConfirmPressed_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696307);
		NativeMethodInfoPtr_CreateNewWorld_Private_Void_String_Difficulty_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696308);
		NativeMethodInfoPtr_CreateDefaultWorld_Public_Static_Void_String_Difficulty_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696309);
		NativeMethodInfoPtr_PopulateNewWeathers_Private_Static_Dictionary_2_Kingdom_Weathers_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696310);
		NativeMethodInfoPtr_NormalizedWorldName_Private_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696311);
		NativeMethodInfoPtr_ValidWorldNameCheck_Public_Static_ValueTuple_2_Boolean_String_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696312);
		NativeMethodInfoPtr_GetLocalizedError_Private_Static_String_String_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696313);
		NativeMethodInfoPtr_SetupControllerNavigation_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696314);
		NativeMethodInfoPtr_CanHandle_Public_Boolean_byref_UIActionContext_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696315);
		NativeMethodInfoPtr_Handle_Public_UIActionResult_byref_UIActionContext_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696316);
		NativeMethodInfoPtr_get_PopupOpen_Private_get_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696317);
		NativeMethodInfoPtr_FocusConfirmationConfirm_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696318);
		NativeMethodInfoPtr_FocusDifficultySlider_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696319);
		NativeMethodInfoPtr_OnSelectionChanged_Private_Void_GameObject_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696320);
		NativeMethodInfoPtr_FocusWorldNameForEdit_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696321);
		NativeMethodInfoPtr_EnsureNameInputPresenter_Private_Void_UIContext_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696322);
		NativeMethodInfoPtr_Dispose_Public_Virtual_Final_New_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696323);
		NativeMethodInfoPtr_IUIActionReceiver_CanHandle_Private_Virtual_Final_New_Boolean_byref_UIActionContext_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696324);
		NativeMethodInfoPtr_IUIActionReceiver_Handle_Private_Virtual_Final_New_UIActionResult_byref_UIActionContext_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696325);
		NativeMethodInfoPtr___ctor_b__18_0_Private_Void_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696326);
		NativeMethodInfoPtr___ctor_b__18_1_Private_Void_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696327);
		NativeMethodInfoPtr___ctor_b__18_2_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696328);
		NativeMethodInfoPtr__EnsureNameInputPresenter_b__43_0_Private_Void_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr, 100696329);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 531737, RefRangeEnd = 531738, XrefRangeStart = 531610, XrefRangeEnd = 531737, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe WorldCreationPresenter(WorldCreationView view, UIContext context, UINode node)
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<WorldCreationPresenter>.NativeClassPtr))
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[3];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(view);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(context);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(node);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_WorldCreationView_UIContext_UINode_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(6)]
	[CachedScanResults(RefRangeStart = 531744, RefRangeEnd = 531750, XrefRangeStart = 531738, XrefRangeEnd = 531744, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void Repaint()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_Repaint_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 531750, XrefRangeEnd = 531751, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void OnDifficultyChanged(float value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = (nint)(&value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_OnDifficultyChanged_Private_Void_Single_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 531751, XrefRangeEnd = 531775, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void OnNewCharactersOnlyChanged(bool isOn)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = (nint)(&isOn);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_OnNewCharactersOnlyChanged_Private_Void_Boolean_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 531775, XrefRangeEnd = 531786, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static bool IsNewCharactersOnlyBlocked()
	{
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_IsNewCharactersOnlyBlocked_Private_Static_Boolean_0, (System.IntPtr)0, (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(bool*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 531786, XrefRangeEnd = 531797, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void OnCreatePressed()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_OnCreatePressed_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 531797, XrefRangeEnd = 531798, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void OnCancelPressed()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_OnCancelPressed_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 531798, XrefRangeEnd = 531835, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void OnConfirmPressed()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_OnConfirmPressed_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532018, RefRangeEnd = 532019, XrefRangeStart = 531835, XrefRangeEnd = 532018, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void CreateNewWorld(string worldName, Difficulty difficulty)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(worldName);
		*(Difficulty**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &difficulty;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CreateNewWorld_Private_Void_String_Difficulty_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532019, XrefRangeEnd = 532199, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static void CreateDefaultWorld(string worldName, Difficulty difficulty)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(worldName);
		*(Difficulty**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &difficulty;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CreateDefaultWorld_Public_Static_Void_String_Difficulty_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 532260, RefRangeEnd = 532262, XrefRangeStart = 532199, XrefRangeEnd = 532260, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static Dictionary<Kingdom, Weathers> PopulateNewWeathers()
	{
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_PopulateNewWeathers_Private_Static_Dictionary_2_Kingdom_Weathers_0, (System.IntPtr)0, (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Dictionary<Kingdom, Weathers>>(intPtr) : null;
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532262, XrefRangeEnd = 532274, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe string NormalizedWorldName()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr il2CppString = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_NormalizedWorldName_Private_String_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return IL2CPP.Il2CppStringToManaged(il2CppString);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 532314, RefRangeEnd = 532316, XrefRangeStart = 532274, XrefRangeEnd = 532314, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static Il2CppSystem.ValueTuple<bool, string> ValidWorldNameCheck(string rawName)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(rawName);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr pointer = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_ValidWorldNameCheck_Public_Static_ValueTuple_2_Boolean_String_String_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return new Il2CppSystem.ValueTuple<bool, string>(pointer);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532316, XrefRangeEnd = 532334, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static string GetLocalizedError(string key, string fallback)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(key);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.ManagedStringToIl2Cpp(fallback);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr il2CppString = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetLocalizedError_Private_Static_String_String_String_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return IL2CPP.Il2CppStringToManaged(il2CppString);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532397, RefRangeEnd = 532398, XrefRangeStart = 532334, XrefRangeEnd = 532397, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void SetupControllerNavigation()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_SetupControllerNavigation_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(3)]
	[CachedScanResults(RefRangeStart = 532403, RefRangeEnd = 532406, XrefRangeStart = 532398, XrefRangeEnd = 532403, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe bool CanHandle([In] ref UIActionContext ctx)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull(ctx));
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CanHandle_Public_Boolean_byref_UIActionContext_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(bool*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532406, XrefRangeEnd = 532416, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe UIActionResult Handle([In] ref UIActionContext ctx)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull(ctx));
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_Handle_Public_UIActionResult_byref_UIActionContext_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(UIActionResult*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532420, XrefRangeEnd = 532428, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void FocusConfirmationConfirm()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_FocusConfirmationConfirm_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 532436, RefRangeEnd = 532438, XrefRangeStart = 532428, XrefRangeEnd = 532436, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void FocusDifficultySlider()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_FocusDifficultySlider_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532438, XrefRangeEnd = 532449, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void OnSelectionChanged(GameObject selected)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(selected);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_OnSelectionChanged_Private_Void_GameObject_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532449, XrefRangeEnd = 532458, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void FocusWorldNameForEdit()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_FocusWorldNameForEdit_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532458, XrefRangeEnd = 532478, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void EnsureNameInputPresenter(UIContext context)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(context);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_EnsureNameInputPresenter_Private_Void_UIContext_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532478, XrefRangeEnd = 532493, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe virtual void Dispose()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_Dispose_Public_Virtual_Final_New_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532493, XrefRangeEnd = 532494, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe virtual bool IUIActionReceiver_CanHandle([In] ref UIActionContext ctx)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull(ctx));
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_IUIActionReceiver_CanHandle_Private_Virtual_Final_New_Boolean_byref_UIActionContext_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(bool*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 0, XrefRangeEnd = 0, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe virtual UIActionResult IUIActionReceiver_Handle([In] ref UIActionContext ctx)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull(ctx));
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_IUIActionReceiver_Handle_Private_Virtual_Final_New_UIActionResult_byref_UIActionContext_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(UIActionResult*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532494, XrefRangeEnd = 532495, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void __ctor_b__18_0(string _)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(_);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr___ctor_b__18_0_Private_Void_String_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532495, XrefRangeEnd = 532496, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void __ctor_b__18_1(string _)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(_);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr___ctor_b__18_1_Private_Void_String_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532496, XrefRangeEnd = 532497, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void __ctor_b__18_2()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr___ctor_b__18_2_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 0, XrefRangeEnd = 0, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void _EnsureNameInputPresenter_b__43_0(string _)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(_);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__EnsureNameInputPresenter_b__43_0_Private_Void_String_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	public WorldCreationPresenter(System.IntPtr pointer)
		: base(pointer)
	{
	}
}
