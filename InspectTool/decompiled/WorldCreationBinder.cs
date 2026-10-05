using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem;

public sealed class WorldCreationBinder : UIPresenterBinder
{
	private static readonly System.IntPtr NativeFieldInfoPtr__view;

	private static readonly System.IntPtr NativeMethodInfoPtr_CreatePresenter_Protected_Virtual_IDisposable_UIContext_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

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

	static WorldCreationBinder()
	{
		Il2CppClassPointerStore<WorldCreationBinder>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "WorldCreationBinder");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<WorldCreationBinder>.NativeClassPtr);
		NativeFieldInfoPtr__view = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationBinder>.NativeClassPtr, "_view");
		NativeMethodInfoPtr_CreatePresenter_Protected_Virtual_IDisposable_UIContext_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationBinder>.NativeClassPtr, 100696276);
		NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<WorldCreationBinder>.NativeClassPtr, 100696277);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 531355, XrefRangeEnd = 531359, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe override Il2CppSystem.IDisposable CreatePresenter(UIContext context)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(context);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CreatePresenter_Protected_Virtual_IDisposable_UIContext_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Il2CppSystem.IDisposable>(intPtr) : null;
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 0, XrefRangeEnd = 0, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe WorldCreationBinder()
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<WorldCreationBinder>.NativeClassPtr))
	{
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	public WorldCreationBinder(System.IntPtr pointer)
		: base(pointer)
	{
	}
}
