using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using UnityEngine;

public class DifficultyDefinition : ScriptableObject
{
	private static readonly IntPtr NativeFieldInfoPtr_InProduction;

	private static readonly IntPtr NativeFieldInfoPtr_Tier;

	private static readonly IntPtr NativeFieldInfoPtr_Description;

	private static readonly IntPtr NativeFieldInfoPtr_Dials;

	private static readonly IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

	public unsafe bool InProduction
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_InProduction);
			return *(bool*)num;
		}
		set
		{
			*(bool*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_InProduction)) = flag;
		}
	}

	public unsafe Difficulty Tier
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Tier);
			return *(Difficulty*)num;
		}
		set
		{
			*(Difficulty*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Tier)) = difficulty;
		}
	}

	public unsafe string Description
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Description);
			return IL2CPP.Il2CppStringToManaged(*(IntPtr*)num);
		}
		set
		{
			IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Description), IL2CPP.ManagedStringToIl2Cpp(str));
		}
	}

	public unsafe DifficultyDials Dials
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Dials);
			IntPtr intPtr = *(IntPtr*)num;
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<DifficultyDials>(intPtr) : null;
		}
		set
		{
			IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Dials), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	static DifficultyDefinition()
	{
		Il2CppClassPointerStore<DifficultyDefinition>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "DifficultyDefinition");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<DifficultyDefinition>.NativeClassPtr);
		NativeFieldInfoPtr_InProduction = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDefinition>.NativeClassPtr, "InProduction");
		NativeFieldInfoPtr_Tier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDefinition>.NativeClassPtr, "Tier");
		NativeFieldInfoPtr_Description = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDefinition>.NativeClassPtr, "Description");
		NativeFieldInfoPtr_Dials = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDefinition>.NativeClassPtr, "Dials");
		NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyDefinition>.NativeClassPtr, 100664142);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 34626, XrefRangeEnd = 34632, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe DifficultyDefinition()
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<DifficultyDefinition>.NativeClassPtr))
	{
		IntPtr* param = null;
		Unsafe.SkipInit(out IntPtr exc);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	public DifficultyDefinition(IntPtr pointer)
		: base(pointer)
	{
	}
}
