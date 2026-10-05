using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem;

[System.Serializable]
public class MonsterDifficultyOverrideEntry : Il2CppSystem.Object
{
	private static readonly System.IntPtr NativeFieldInfoPtr_Tier;

	private static readonly System.IntPtr NativeFieldInfoPtr_Dials;

	private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

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

	public unsafe DifficultyDials Dials
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Dials);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<DifficultyDials>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Dials), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	static MonsterDifficultyOverrideEntry()
	{
		Il2CppClassPointerStore<MonsterDifficultyOverrideEntry>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "MonsterDifficultyOverrideEntry");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<MonsterDifficultyOverrideEntry>.NativeClassPtr);
		NativeFieldInfoPtr_Tier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<MonsterDifficultyOverrideEntry>.NativeClassPtr, "Tier");
		NativeFieldInfoPtr_Dials = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<MonsterDifficultyOverrideEntry>.NativeClassPtr, "Dials");
		NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<MonsterDifficultyOverrideEntry>.NativeClassPtr, 100664164);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 34948, XrefRangeEnd = 34954, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe MonsterDifficultyOverrideEntry()
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<MonsterDifficultyOverrideEntry>.NativeClassPtr))
	{
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	public MonsterDifficultyOverrideEntry(System.IntPtr pointer)
		: base(pointer)
	{
	}
}
