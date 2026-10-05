using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem;
using Il2CppSystem.Collections;
using Il2CppSystem.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WorldCreationView : MonoBehaviour
{
	[ObfuscatedName("WorldCreationView+<FadeNotReadyToast>d__80")]
	public sealed class _FadeNotReadyToast_d__80 : Il2CppSystem.Object
	{
		private static readonly System.IntPtr NativeFieldInfoPtr___1__state;

		private static readonly System.IntPtr NativeFieldInfoPtr___2__current;

		private static readonly System.IntPtr NativeFieldInfoPtr___4__this;

		private static readonly System.IntPtr NativeFieldInfoPtr_delayBeforeFade;

		private static readonly System.IntPtr NativeFieldInfoPtr__fadeDuration_5__2;

		private static readonly System.IntPtr NativeFieldInfoPtr__elapsedTime_5__3;

		private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_Int32_0;

		private static readonly System.IntPtr NativeMethodInfoPtr_System_IDisposable_Dispose_Private_Virtual_Final_New_Void_0;

		private static readonly System.IntPtr NativeMethodInfoPtr_MoveNext_Private_Virtual_Final_New_Boolean_0;

		private static readonly System.IntPtr NativeMethodInfoPtr_System_Collections_Generic_IEnumerator_System_Object__get_Current_Private_Virtual_Final_New_get_Object_0;

		private static readonly System.IntPtr NativeMethodInfoPtr_System_Collections_IEnumerator_Reset_Private_Virtual_Final_New_Void_0;

		private static readonly System.IntPtr NativeMethodInfoPtr_System_Collections_IEnumerator_get_Current_Private_Virtual_Final_New_get_Object_0;

		public unsafe int __1__state
		{
			get
			{
				nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr___1__state);
				return *(int*)num;
			}
			set
			{
				*(int*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr___1__state)) = num;
			}
		}

		public unsafe Il2CppSystem.Object __2__current
		{
			get
			{
				nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr___2__current);
				System.IntPtr intPtr = *(System.IntPtr*)num;
				return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Object>(intPtr) : null;
			}
			set
			{
				System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
				IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr___2__current), IL2CPP.Il2CppObjectBaseToPtr(obj));
			}
		}

		public unsafe WorldCreationView __4__this
		{
			get
			{
				nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr___4__this);
				System.IntPtr intPtr = *(System.IntPtr*)num;
				return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<WorldCreationView>(intPtr) : null;
			}
			set
			{
				System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
				IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr___4__this), IL2CPP.Il2CppObjectBaseToPtr(obj));
			}
		}

		public unsafe float delayBeforeFade
		{
			get
			{
				nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_delayBeforeFade);
				return *(float*)num;
			}
			set
			{
				*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_delayBeforeFade)) = num;
			}
		}

		public unsafe float _fadeDuration_5__2
		{
			get
			{
				nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__fadeDuration_5__2);
				return *(float*)num;
			}
			set
			{
				*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__fadeDuration_5__2)) = num;
			}
		}

		public unsafe float _elapsedTime_5__3
		{
			get
			{
				nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__elapsedTime_5__3);
				return *(float*)num;
			}
			set
			{
				*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__elapsedTime_5__3)) = num;
			}
		}

		public unsafe virtual Il2CppSystem.Object System.Collections.Generic.IEnumerator<System.Object>.Current
		{
			[CallerCount(16)]
			[CachedScanResults(RefRangeStart = 129, RefRangeEnd = 145, XrefRangeStart = 129, XrefRangeEnd = 145, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
			get
			{
				IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
				System.IntPtr* param = null;
				Unsafe.SkipInit(out System.IntPtr exc);
				System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_System_Collections_Generic_IEnumerator_System_Object__get_Current_Private_Virtual_Final_New_get_Object_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
				Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
				return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Object>(intPtr) : null;
			}
		}

		public unsafe virtual Il2CppSystem.Object System.Collections.IEnumerator.Current
		{
			[CallerCount(16)]
			[CachedScanResults(RefRangeStart = 129, RefRangeEnd = 145, XrefRangeStart = 129, XrefRangeEnd = 145, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
			get
			{
				IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
				System.IntPtr* param = null;
				Unsafe.SkipInit(out System.IntPtr exc);
				System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_System_Collections_IEnumerator_get_Current_Private_Virtual_Final_New_get_Object_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
				Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
				return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Object>(intPtr) : null;
			}
		}

		static _FadeNotReadyToast_d__80()
		{
			Il2CppClassPointerStore<_FadeNotReadyToast_d__80>.NativeClassPtr = IL2CPP.GetIl2CppNestedType(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "<FadeNotReadyToast>d__80");
			IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<_FadeNotReadyToast_d__80>.NativeClassPtr);
			NativeFieldInfoPtr___1__state = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<_FadeNotReadyToast_d__80>.NativeClassPtr, "<>1__state");
			NativeFieldInfoPtr___2__current = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<_FadeNotReadyToast_d__80>.NativeClassPtr, "<>2__current");
			NativeFieldInfoPtr___4__this = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<_FadeNotReadyToast_d__80>.NativeClassPtr, "<>4__this");
			NativeFieldInfoPtr_delayBeforeFade = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<_FadeNotReadyToast_d__80>.NativeClassPtr, "delayBeforeFade");
			NativeFieldInfoPtr__fadeDuration_5__2 = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<_FadeNotReadyToast_d__80>.NativeClassPtr, "<fadeDuration>5__2");
			NativeFieldInfoPtr__elapsedTime_5__3 = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<_FadeNotReadyToast_d__80>.NativeClassPtr, "<elapsedTime>5__3");
			NativeMethodInfoPtr__ctor_Public_Void_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<_FadeNotReadyToast_d__80>.NativeClassPtr, 100696383);
			NativeMethodInfoPtr_System_IDisposable_Dispose_Private_Virtual_Final_New_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<_FadeNotReadyToast_d__80>.NativeClassPtr, 100696384);
			NativeMethodInfoPtr_MoveNext_Private_Virtual_Final_New_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<_FadeNotReadyToast_d__80>.NativeClassPtr, 100696385);
			NativeMethodInfoPtr_System_Collections_Generic_IEnumerator_System_Object__get_Current_Private_Virtual_Final_New_get_Object_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<_FadeNotReadyToast_d__80>.NativeClassPtr, 100696386);
			NativeMethodInfoPtr_System_Collections_IEnumerator_Reset_Private_Virtual_Final_New_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<_FadeNotReadyToast_d__80>.NativeClassPtr, 100696387);
			NativeMethodInfoPtr_System_Collections_IEnumerator_get_Current_Private_Virtual_Final_New_get_Object_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<_FadeNotReadyToast_d__80>.NativeClassPtr, 100696388);
		}

		[CallerCount(395)]
		[CachedScanResults(RefRangeStart = 2277, RefRangeEnd = 2672, XrefRangeStart = 2277, XrefRangeEnd = 2672, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		public unsafe _FadeNotReadyToast_d__80(int <>1__state)
			: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<_FadeNotReadyToast_d__80>.NativeClassPtr))
		{
			System.IntPtr* ptr = stackalloc System.IntPtr[1];
			*ptr = (nint)(&<>1__state);
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_Int32_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		}

		[CallerCount(21436)]
		[CachedScanResults(RefRangeStart = 2672, RefRangeEnd = 24108, XrefRangeStart = 2672, XrefRangeEnd = 24108, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		public unsafe virtual void System_IDisposable_Dispose()
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_System_IDisposable_Dispose_Private_Virtual_Final_New_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		}

		[CallerCount(0)]
		[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532497, XrefRangeEnd = 532498, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		public unsafe virtual bool MoveNext()
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_MoveNext_Private_Virtual_Final_New_Boolean_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return *(bool*)IL2CPP.il2cpp_object_unbox(obj);
		}

		[CallerCount(0)]
		[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532498, XrefRangeEnd = 532503, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		public unsafe virtual void System_Collections_IEnumerator_Reset()
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_System_Collections_IEnumerator_Reset_Private_Virtual_Final_New_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		}

		public _FadeNotReadyToast_d__80(System.IntPtr pointer)
			: base(pointer)
		{
		}
	}

	private static readonly System.IntPtr NativeFieldInfoPtr__worldDifficultySlider;

	private static readonly System.IntPtr NativeFieldInfoPtr__worldNameField;

	private static readonly System.IntPtr NativeFieldInfoPtr__worldNameInputView;

	private static readonly System.IntPtr NativeFieldInfoPtr__notReadyPopUp;

	private static readonly System.IntPtr NativeFieldInfoPtr__worldNameBanner;

	private static readonly System.IntPtr NativeFieldInfoPtr__createButton;

	private static readonly System.IntPtr NativeFieldInfoPtr__nameError;

	private static readonly System.IntPtr NativeFieldInfoPtr__worldConfirmationPopUp;

	private static readonly System.IntPtr NativeFieldInfoPtr__worldPreview;

	private static readonly System.IntPtr NativeFieldInfoPtr__worldDifficulty;

	private static readonly System.IntPtr NativeFieldInfoPtr__newCharactersOnly;

	private static readonly System.IntPtr NativeFieldInfoPtr__personalLoot;

	private static readonly System.IntPtr NativeFieldInfoPtr__confirmButton;

	private static readonly System.IntPtr NativeFieldInfoPtr__cancelButton;

	private static readonly System.IntPtr NativeFieldInfoPtr__backButton;

	private static readonly System.IntPtr NativeFieldInfoPtr__adjustDifficultyHint;

	private static readonly System.IntPtr NativeFieldInfoPtr__defaultState;

	private static readonly System.IntPtr NativeFieldInfoPtr_NameChanged;

	private static readonly System.IntPtr NativeFieldInfoPtr_NameSubmitted;

	private static readonly System.IntPtr NativeFieldInfoPtr_DifficultyChanged;

	private static readonly System.IntPtr NativeFieldInfoPtr_NewCharactersOnlyChanged;

	private static readonly System.IntPtr NativeFieldInfoPtr_CreatePressed;

	private static readonly System.IntPtr NativeFieldInfoPtr_ConfirmPressed;

	private static readonly System.IntPtr NativeFieldInfoPtr_CancelPressed;

	private static readonly System.IntPtr NativeFieldInfoPtr_BackPressed;

	private static readonly System.IntPtr NativeFieldInfoPtr__notReadyText;

	private static readonly System.IntPtr NativeFieldInfoPtr__notReadyCoroutine;

	private static readonly System.IntPtr NativeFieldInfoPtr__newCharactersOnlyGraphics;

	private static readonly System.IntPtr NativeFieldInfoPtr__wired;

	private static readonly System.IntPtr NativeMethodInfoPtr_add_NameChanged_Public_add_Void_Action_1_String_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_remove_NameChanged_Public_rem_Void_Action_1_String_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_add_NameSubmitted_Public_add_Void_Action_1_String_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_remove_NameSubmitted_Public_rem_Void_Action_1_String_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_add_DifficultyChanged_Public_add_Void_Action_1_Single_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_remove_DifficultyChanged_Public_rem_Void_Action_1_Single_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_add_NewCharactersOnlyChanged_Public_add_Void_Action_1_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_remove_NewCharactersOnlyChanged_Public_rem_Void_Action_1_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_add_CreatePressed_Public_add_Void_Action_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_remove_CreatePressed_Public_rem_Void_Action_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_add_ConfirmPressed_Public_add_Void_Action_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_remove_ConfirmPressed_Public_rem_Void_Action_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_add_CancelPressed_Public_add_Void_Action_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_remove_CancelPressed_Public_rem_Void_Action_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_add_BackPressed_Public_add_Void_Action_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_remove_BackPressed_Public_rem_Void_Action_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_WorldName_Public_get_String_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_DifficultyValue_Public_get_Single_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_NewCharactersOnlyOn_Public_get_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_PersonalLootOn_Public_get_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_WorldNameField_Public_get_TMP_InputField_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_WorldNameInputView_Public_get_UITextInputView_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_DifficultySlider_Public_get_Slider_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_NewCharactersOnlyToggle_Public_get_Toggle_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_PersonalLootToggle_Public_get_Toggle_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_ConfirmationPopupRoot_Public_get_GameObject_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_ConfirmButton_Public_get_Button_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_CancelButton_Public_get_Button_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_BackButton_Public_get_Button_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_CreateButton_Public_get_Button_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_SetAdjustDifficultyHintVisible_Public_Void_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_Awake_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_Wire_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_ApplyState_Public_Void_byref_WorldCreationState_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_SetWorldNameWithoutNotify_Public_Void_String_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_SetNewCharactersOnlyWithoutNotify_Public_Void_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_ShowNotReadyMessage_Public_Void_String_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_FadeNotReadyToast_Private_IEnumerator_Single_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetDefaultState_Public_Virtual_Final_New_WorldCreationState_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_SetDefaultState_Public_Void_byref_WorldCreationState_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_IView_WorldCreationState__ApplyState_Private_Virtual_Final_New_Void_byref_WorldCreationState_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_IViewDefaultState_WorldCreationState__SetDefaultState_Private_Virtual_Final_New_Void_byref_WorldCreationState_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__Wire_b__75_0_Private_Void_String_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__Wire_b__75_1_Private_Void_String_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__Wire_b__75_2_Private_Void_Single_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__Wire_b__75_3_Private_Void_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__Wire_b__75_4_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__Wire_b__75_5_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__Wire_b__75_6_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__Wire_b__75_7_Private_Void_0;

	public unsafe Slider _worldDifficultySlider
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__worldDifficultySlider);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Slider>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__worldDifficultySlider), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe TMP_InputField _worldNameField
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__worldNameField);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<TMP_InputField>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__worldNameField), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe UITextInputView _worldNameInputView
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__worldNameInputView);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<UITextInputView>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__worldNameInputView), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe CanvasGroup _notReadyPopUp
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__notReadyPopUp);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<CanvasGroup>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__notReadyPopUp), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Image _worldNameBanner
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__worldNameBanner);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Image>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__worldNameBanner), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Button _createButton
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__createButton);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Button>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__createButton), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe TextMeshProUGUI _nameError
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__nameError);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<TextMeshProUGUI>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__nameError), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe GameObject _worldConfirmationPopUp
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__worldConfirmationPopUp);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<GameObject>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__worldConfirmationPopUp), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Image _worldPreview
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__worldPreview);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Image>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__worldPreview), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Image _worldDifficulty
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__worldDifficulty);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Image>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__worldDifficulty), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Toggle _newCharactersOnly
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__newCharactersOnly);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Toggle>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__newCharactersOnly), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Toggle _personalLoot
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__personalLoot);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Toggle>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__personalLoot), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Button _confirmButton
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__confirmButton);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Button>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__confirmButton), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Button _cancelButton
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__cancelButton);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Button>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__cancelButton), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Button _backButton
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__backButton);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Button>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__backButton), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe UIActionHint _adjustDifficultyHint
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__adjustDifficultyHint);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<UIActionHint>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__adjustDifficultyHint), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe WorldCreationState _defaultState
	{
		get
		{
			nint data = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__defaultState);
			return new WorldCreationState(IL2CPP.il2cpp_value_box(Il2CppClassPointerStore<WorldCreationState>.NativeClassPtr, data));
		}
		set
		{
			// IL cpblk instruction
			Unsafe.CopyBlock((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__defaultState), IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtr(obj)), IL2CPP.il2cpp_class_value_size(Il2CppClassPointerStore<WorldCreationState>.NativeClassPtr, ref *(uint*)null));
		}
	}

	public unsafe Il2CppSystem.Action<string> NameChanged
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NameChanged);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action<string>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NameChanged), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action<string> NameSubmitted
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NameSubmitted);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action<string>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NameSubmitted), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action<float> DifficultyChanged
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_DifficultyChanged);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action<float>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_DifficultyChanged), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action<bool> NewCharactersOnlyChanged
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NewCharactersOnlyChanged);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action<bool>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NewCharactersOnlyChanged), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action CreatePressed
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_CreatePressed);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_CreatePressed), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action ConfirmPressed
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_ConfirmPressed);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_ConfirmPressed), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action CancelPressed
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_CancelPressed);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_CancelPressed), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Il2CppSystem.Action BackPressed
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_BackPressed);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.Action>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_BackPressed), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe TextMeshProUGUI _notReadyText
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__notReadyText);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<TextMeshProUGUI>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__notReadyText), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Coroutine _notReadyCoroutine
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__notReadyCoroutine);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Coroutine>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__notReadyCoroutine), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe List<Il2CppSystem.ValueTuple<Graphic, float>> _newCharactersOnlyGraphics
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__newCharactersOnlyGraphics);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<Il2CppSystem.ValueTuple<Graphic, float>>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__newCharactersOnlyGraphics), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe bool _wired
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__wired);
			return *(bool*)num;
		}
		set
		{
			*(bool*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__wired)) = flag;
		}
	}

	public unsafe string WorldName
	{
		[CallerCount(4)]
		[CachedScanResults(RefRangeStart = 532595, RefRangeEnd = 532599, XrefRangeStart = 532591, XrefRangeEnd = 532595, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr il2CppString = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_WorldName_Public_get_String_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return IL2CPP.Il2CppStringToManaged(il2CppString);
		}
	}

	public unsafe float DifficultyValue
	{
		[CallerCount(3)]
		[CachedScanResults(RefRangeStart = 532603, RefRangeEnd = 532606, XrefRangeStart = 532599, XrefRangeEnd = 532603, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_DifficultyValue_Public_get_Single_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return *(float*)IL2CPP.il2cpp_object_unbox(obj);
		}
	}

	public unsafe bool NewCharactersOnlyOn
	{
		[CallerCount(2)]
		[CachedScanResults(RefRangeStart = 532610, RefRangeEnd = 532612, XrefRangeStart = 532606, XrefRangeEnd = 532610, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_NewCharactersOnlyOn_Public_get_Boolean_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return *(bool*)IL2CPP.il2cpp_object_unbox(obj);
		}
	}

	public unsafe bool PersonalLootOn
	{
		[CallerCount(1)]
		[CachedScanResults(RefRangeStart = 532616, RefRangeEnd = 532617, XrefRangeStart = 532612, XrefRangeEnd = 532616, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_PersonalLootOn_Public_get_Boolean_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return *(bool*)IL2CPP.il2cpp_object_unbox(obj);
		}
	}

	public unsafe TMP_InputField WorldNameField
	{
		[CallerCount(3)]
		[CachedScanResults(RefRangeStart = 188, RefRangeEnd = 191, XrefRangeStart = 188, XrefRangeEnd = 191, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_WorldNameField_Public_get_TMP_InputField_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<TMP_InputField>(intPtr) : null;
		}
	}

	public unsafe UITextInputView WorldNameInputView
	{
		[CallerCount(14)]
		[CachedScanResults(RefRangeStart = 145, RefRangeEnd = 159, XrefRangeStart = 145, XrefRangeEnd = 159, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_WorldNameInputView_Public_get_UITextInputView_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<UITextInputView>(intPtr) : null;
		}
	}

	public unsafe Slider DifficultySlider
	{
		[CallerCount(1)]
		[CachedScanResults(RefRangeStart = 187, RefRangeEnd = 188, XrefRangeStart = 187, XrefRangeEnd = 188, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_DifficultySlider_Public_get_Slider_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Slider>(intPtr) : null;
		}
	}

	public unsafe Toggle NewCharactersOnlyToggle
	{
		[CallerCount(1)]
		[CachedScanResults(RefRangeStart = 427, RefRangeEnd = 428, XrefRangeStart = 427, XrefRangeEnd = 428, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_NewCharactersOnlyToggle_Public_get_Toggle_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Toggle>(intPtr) : null;
		}
	}

	public unsafe Toggle PersonalLootToggle
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_PersonalLootToggle_Public_get_Toggle_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Toggle>(intPtr) : null;
		}
	}

	public unsafe GameObject ConfirmationPopupRoot
	{
		[CallerCount(1)]
		[CachedScanResults(RefRangeStart = 213, RefRangeEnd = 214, XrefRangeStart = 213, XrefRangeEnd = 214, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_ConfirmationPopupRoot_Public_get_GameObject_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<GameObject>(intPtr) : null;
		}
	}

	public unsafe Button ConfirmButton
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_ConfirmButton_Public_get_Button_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Button>(intPtr) : null;
		}
	}

	public unsafe Button CancelButton
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_CancelButton_Public_get_Button_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Button>(intPtr) : null;
		}
	}

	public unsafe Button BackButton
	{
		[CallerCount(6)]
		[CachedScanResults(RefRangeStart = 741, RefRangeEnd = 747, XrefRangeStart = 741, XrefRangeEnd = 747, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_BackButton_Public_get_Button_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Button>(intPtr) : null;
		}
	}

	public unsafe Button CreateButton
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_CreateButton_Public_get_Button_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Button>(intPtr) : null;
		}
	}

	static WorldCreationView()
	{
		Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "WorldCreationView");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr);
		NativeFieldInfoPtr__worldDifficultySlider = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_worldDifficultySlider");
		NativeFieldInfoPtr__worldNameField = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_worldNameField");
		NativeFieldInfoPtr__worldNameInputView = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_worldNameInputView");
		NativeFieldInfoPtr__notReadyPopUp = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_notReadyPopUp");
		NativeFieldInfoPtr__worldNameBanner = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_worldNameBanner");
		NativeFieldInfoPtr__createButton = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_createButton");
		NativeFieldInfoPtr__nameError = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_nameError");
		NativeFieldInfoPtr__worldConfirmationPopUp = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_worldConfirmationPopUp");
		NativeFieldInfoPtr__worldPreview = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_worldPreview");
		NativeFieldInfoPtr__worldDifficulty = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_worldDifficulty");
		NativeFieldInfoPtr__newCharactersOnly = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_newCharactersOnly");
		NativeFieldInfoPtr__personalLoot = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_personalLoot");
		NativeFieldInfoPtr__confirmButton = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_confirmButton");
		NativeFieldInfoPtr__cancelButton = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_cancelButton");
		NativeFieldInfoPtr__backButton = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_backButton");
		NativeFieldInfoPtr__adjustDifficultyHint = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_adjustDifficultyHint");
		NativeFieldInfoPtr__defaultState = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_defaultState");
		NativeFieldInfoPtr_NameChanged = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "NameChanged");
		NativeFieldInfoPtr_NameSubmitted = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "NameSubmitted");
		NativeFieldInfoPtr_DifficultyChanged = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "DifficultyChanged");
		NativeFieldInfoPtr_NewCharactersOnlyChanged = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "NewCharactersOnlyChanged");
		NativeFieldInfoPtr_CreatePressed = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "CreatePressed");
		NativeFieldInfoPtr_ConfirmPressed = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "ConfirmPressed");
		NativeFieldInfoPtr_CancelPressed = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "CancelPressed");
		NativeFieldInfoPtr_BackPressed = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "BackPressed");
		NativeFieldInfoPtr__notReadyText = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_notReadyText");
		NativeFieldInfoPtr__notReadyCoroutine = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_notReadyCoroutine");
		NativeFieldInfoPtr__newCharactersOnlyGraphics = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_newCharactersOnlyGraphics");
		NativeFieldInfoPtr__wired = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, "_wired");
		NativeMethodInfoPtr_add_NameChanged_Public_add_Void_Action_1_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696332);
		NativeMethodInfoPtr_remove_NameChanged_Public_rem_Void_Action_1_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696333);
		NativeMethodInfoPtr_add_NameSubmitted_Public_add_Void_Action_1_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696334);
		NativeMethodInfoPtr_remove_NameSubmitted_Public_rem_Void_Action_1_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696335);
		NativeMethodInfoPtr_add_DifficultyChanged_Public_add_Void_Action_1_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696336);
		NativeMethodInfoPtr_remove_DifficultyChanged_Public_rem_Void_Action_1_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696337);
		NativeMethodInfoPtr_add_NewCharactersOnlyChanged_Public_add_Void_Action_1_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696338);
		NativeMethodInfoPtr_remove_NewCharactersOnlyChanged_Public_rem_Void_Action_1_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696339);
		NativeMethodInfoPtr_add_CreatePressed_Public_add_Void_Action_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696340);
		NativeMethodInfoPtr_remove_CreatePressed_Public_rem_Void_Action_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696341);
		NativeMethodInfoPtr_add_ConfirmPressed_Public_add_Void_Action_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696342);
		NativeMethodInfoPtr_remove_ConfirmPressed_Public_rem_Void_Action_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696343);
		NativeMethodInfoPtr_add_CancelPressed_Public_add_Void_Action_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696344);
		NativeMethodInfoPtr_remove_CancelPressed_Public_rem_Void_Action_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696345);
		NativeMethodInfoPtr_add_BackPressed_Public_add_Void_Action_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696346);
		NativeMethodInfoPtr_remove_BackPressed_Public_rem_Void_Action_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696347);
		NativeMethodInfoPtr_get_WorldName_Public_get_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696348);
		NativeMethodInfoPtr_get_DifficultyValue_Public_get_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696349);
		NativeMethodInfoPtr_get_NewCharactersOnlyOn_Public_get_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696350);
		NativeMethodInfoPtr_get_PersonalLootOn_Public_get_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696351);
		NativeMethodInfoPtr_get_WorldNameField_Public_get_TMP_InputField_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696352);
		NativeMethodInfoPtr_get_WorldNameInputView_Public_get_UITextInputView_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696353);
		NativeMethodInfoPtr_get_DifficultySlider_Public_get_Slider_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696354);
		NativeMethodInfoPtr_get_NewCharactersOnlyToggle_Public_get_Toggle_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696355);
		NativeMethodInfoPtr_get_PersonalLootToggle_Public_get_Toggle_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696356);
		NativeMethodInfoPtr_get_ConfirmationPopupRoot_Public_get_GameObject_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696357);
		NativeMethodInfoPtr_get_ConfirmButton_Public_get_Button_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696358);
		NativeMethodInfoPtr_get_CancelButton_Public_get_Button_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696359);
		NativeMethodInfoPtr_get_BackButton_Public_get_Button_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696360);
		NativeMethodInfoPtr_get_CreateButton_Public_get_Button_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696361);
		NativeMethodInfoPtr_SetAdjustDifficultyHintVisible_Public_Void_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696362);
		NativeMethodInfoPtr_Awake_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696363);
		NativeMethodInfoPtr_Wire_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696364);
		NativeMethodInfoPtr_ApplyState_Public_Void_byref_WorldCreationState_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696365);
		NativeMethodInfoPtr_SetWorldNameWithoutNotify_Public_Void_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696366);
		NativeMethodInfoPtr_SetNewCharactersOnlyWithoutNotify_Public_Void_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696367);
		NativeMethodInfoPtr_ShowNotReadyMessage_Public_Void_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696368);
		NativeMethodInfoPtr_FadeNotReadyToast_Private_IEnumerator_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696369);
		NativeMethodInfoPtr_GetDefaultState_Public_Virtual_Final_New_WorldCreationState_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696370);
		NativeMethodInfoPtr_SetDefaultState_Public_Void_byref_WorldCreationState_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696371);
		NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696372);
		NativeMethodInfoPtr_IView_WorldCreationState__ApplyState_Private_Virtual_Final_New_Void_byref_WorldCreationState_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696373);
		NativeMethodInfoPtr_IViewDefaultState_WorldCreationState__SetDefaultState_Private_Virtual_Final_New_Void_byref_WorldCreationState_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696374);
		NativeMethodInfoPtr__Wire_b__75_0_Private_Void_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696375);
		NativeMethodInfoPtr__Wire_b__75_1_Private_Void_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696376);
		NativeMethodInfoPtr__Wire_b__75_2_Private_Void_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696377);
		NativeMethodInfoPtr__Wire_b__75_3_Private_Void_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696378);
		NativeMethodInfoPtr__Wire_b__75_4_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696379);
		NativeMethodInfoPtr__Wire_b__75_5_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696380);
		NativeMethodInfoPtr__Wire_b__75_6_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696381);
		NativeMethodInfoPtr__Wire_b__75_7_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr, 100696382);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532508, RefRangeEnd = 532509, XrefRangeStart = 532503, XrefRangeEnd = 532508, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void add_NameChanged(Il2CppSystem.Action<string> value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_add_NameChanged_Public_add_Void_Action_1_String_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532514, RefRangeEnd = 532515, XrefRangeStart = 532509, XrefRangeEnd = 532514, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void remove_NameChanged(Il2CppSystem.Action<string> value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_remove_NameChanged_Public_rem_Void_Action_1_String_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532520, RefRangeEnd = 532521, XrefRangeStart = 532515, XrefRangeEnd = 532520, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void add_NameSubmitted(Il2CppSystem.Action<string> value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_add_NameSubmitted_Public_add_Void_Action_1_String_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532526, RefRangeEnd = 532527, XrefRangeStart = 532521, XrefRangeEnd = 532526, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void remove_NameSubmitted(Il2CppSystem.Action<string> value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_remove_NameSubmitted_Public_rem_Void_Action_1_String_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532532, RefRangeEnd = 532533, XrefRangeStart = 532527, XrefRangeEnd = 532532, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void add_DifficultyChanged(Il2CppSystem.Action<float> value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_add_DifficultyChanged_Public_add_Void_Action_1_Single_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532538, RefRangeEnd = 532539, XrefRangeStart = 532533, XrefRangeEnd = 532538, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void remove_DifficultyChanged(Il2CppSystem.Action<float> value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_remove_DifficultyChanged_Public_rem_Void_Action_1_Single_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532544, RefRangeEnd = 532545, XrefRangeStart = 532539, XrefRangeEnd = 532544, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void add_NewCharactersOnlyChanged(Il2CppSystem.Action<bool> value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_add_NewCharactersOnlyChanged_Public_add_Void_Action_1_Boolean_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532550, RefRangeEnd = 532551, XrefRangeStart = 532545, XrefRangeEnd = 532550, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void remove_NewCharactersOnlyChanged(Il2CppSystem.Action<bool> value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_remove_NewCharactersOnlyChanged_Public_rem_Void_Action_1_Boolean_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532555, RefRangeEnd = 532556, XrefRangeStart = 532551, XrefRangeEnd = 532555, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void add_CreatePressed(Il2CppSystem.Action value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_add_CreatePressed_Public_add_Void_Action_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532560, RefRangeEnd = 532561, XrefRangeStart = 532556, XrefRangeEnd = 532560, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void remove_CreatePressed(Il2CppSystem.Action value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_remove_CreatePressed_Public_rem_Void_Action_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532565, RefRangeEnd = 532566, XrefRangeStart = 532561, XrefRangeEnd = 532565, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void add_ConfirmPressed(Il2CppSystem.Action value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_add_ConfirmPressed_Public_add_Void_Action_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532570, RefRangeEnd = 532571, XrefRangeStart = 532566, XrefRangeEnd = 532570, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void remove_ConfirmPressed(Il2CppSystem.Action value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_remove_ConfirmPressed_Public_rem_Void_Action_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532575, RefRangeEnd = 532576, XrefRangeStart = 532571, XrefRangeEnd = 532575, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void add_CancelPressed(Il2CppSystem.Action value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_add_CancelPressed_Public_add_Void_Action_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532580, RefRangeEnd = 532581, XrefRangeStart = 532576, XrefRangeEnd = 532580, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void remove_CancelPressed(Il2CppSystem.Action value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_remove_CancelPressed_Public_rem_Void_Action_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532585, RefRangeEnd = 532586, XrefRangeStart = 532581, XrefRangeEnd = 532585, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void add_BackPressed(Il2CppSystem.Action value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_add_BackPressed_Public_add_Void_Action_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[SpecialName]
	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532590, RefRangeEnd = 532591, XrefRangeStart = 532586, XrefRangeEnd = 532590, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void remove_BackPressed(Il2CppSystem.Action value)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_remove_BackPressed_Public_rem_Void_Action_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 532622, RefRangeEnd = 532624, XrefRangeStart = 532617, XrefRangeEnd = 532622, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void SetAdjustDifficultyHintVisible(bool visible)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = (nint)(&visible);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_SetAdjustDifficultyHintVisible_Public_Void_Boolean_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532624, XrefRangeEnd = 532651, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void Awake()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_Awake_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532732, RefRangeEnd = 532733, XrefRangeStart = 532651, XrefRangeEnd = 532732, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void Wire()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_Wire_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(4)]
	[CachedScanResults(RefRangeStart = 532803, RefRangeEnd = 532807, XrefRangeStart = 532733, XrefRangeEnd = 532803, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void ApplyState([In] ref WorldCreationState state)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull(state));
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_ApplyState_Public_Void_byref_WorldCreationState_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532812, RefRangeEnd = 532813, XrefRangeStart = 532807, XrefRangeEnd = 532812, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void SetWorldNameWithoutNotify(string name)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(name);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_SetWorldNameWithoutNotify_Public_Void_String_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 532818, RefRangeEnd = 532820, XrefRangeStart = 532813, XrefRangeEnd = 532818, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void SetNewCharactersOnlyWithoutNotify(bool isOn)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = (nint)(&isOn);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_SetNewCharactersOnlyWithoutNotify_Public_Void_Boolean_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 532861, RefRangeEnd = 532862, XrefRangeStart = 532820, XrefRangeEnd = 532861, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void ShowNotReadyMessage(string message)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(message);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_ShowNotReadyMessage_Public_Void_String_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532862, XrefRangeEnd = 532867, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe IEnumerator FadeNotReadyToast(float delayBeforeFade)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = (nint)(&delayBeforeFade);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_FadeNotReadyToast_Private_IEnumerator_Single_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<IEnumerator>(intPtr) : null;
	}

	[CallerCount(0)]
	public unsafe virtual WorldCreationState GetDefaultState()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr pointer = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetDefaultState_Public_Virtual_Final_New_WorldCreationState_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return new WorldCreationState(pointer);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532867, XrefRangeEnd = 532869, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void SetDefaultState([In] ref WorldCreationState state)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull(state));
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_SetDefaultState_Public_Void_byref_WorldCreationState_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532869, XrefRangeEnd = 532878, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe WorldCreationView()
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<WorldCreationView>.NativeClassPtr))
	{
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 532878, XrefRangeEnd = 532879, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe virtual void IView_WorldCreationState__ApplyState([In] ref WorldCreationState state)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull(state));
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_IView_WorldCreationState__ApplyState_Private_Virtual_Final_New_Void_byref_WorldCreationState_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 0, XrefRangeEnd = 0, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe virtual void IViewDefaultState_WorldCreationState__SetDefaultState([In] ref WorldCreationState state)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull(state));
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_IViewDefaultState_WorldCreationState__SetDefaultState_Private_Virtual_Final_New_Void_byref_WorldCreationState_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	public unsafe void _Wire_b__75_0(string v)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(v);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__Wire_b__75_0_Private_Void_String_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	public unsafe void _Wire_b__75_1(string v)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(v);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__Wire_b__75_1_Private_Void_String_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	public unsafe void _Wire_b__75_2(float v)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = (nint)(&v);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__Wire_b__75_2_Private_Void_Single_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	public unsafe void _Wire_b__75_3(bool v)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = (nint)(&v);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__Wire_b__75_3_Private_Void_Boolean_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	public unsafe void _Wire_b__75_4()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__Wire_b__75_4_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	public unsafe void _Wire_b__75_5()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__Wire_b__75_5_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	public unsafe void _Wire_b__75_6()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__Wire_b__75_6_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	public unsafe void _Wire_b__75_7()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__Wire_b__75_7_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	public WorldCreationView(System.IntPtr pointer)
		: base(pointer)
	{
	}
}
