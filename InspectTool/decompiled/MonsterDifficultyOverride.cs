using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

public class MonsterDifficultyOverride : ScriptableObject
{
	private static readonly IntPtr NativeFieldInfoPtr_InProduction;

	private static readonly IntPtr NativeFieldInfoPtr_Monsters;

	private static readonly IntPtr NativeFieldInfoPtr_Entries;

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

	public unsafe List<MonsterType> Monsters
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Monsters);
			IntPtr intPtr = *(IntPtr*)num;
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<List<MonsterType>>(intPtr) : null;
		}
		set
		{
			IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Monsters), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe List<MonsterDifficultyOverrideEntry> Entries
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Entries);
			IntPtr intPtr = *(IntPtr*)num;
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<List<MonsterDifficultyOverrideEntry>>(intPtr) : null;
		}
		set
		{
			IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Entries), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	static MonsterDifficultyOverride()
	{
		Il2CppClassPointerStore<MonsterDifficultyOverride>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "MonsterDifficultyOverride");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<MonsterDifficultyOverride>.NativeClassPtr);
		NativeFieldInfoPtr_InProduction = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<MonsterDifficultyOverride>.NativeClassPtr, "InProduction");
		NativeFieldInfoPtr_Monsters = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<MonsterDifficultyOverride>.NativeClassPtr, "Monsters");
		NativeFieldInfoPtr_Entries = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<MonsterDifficultyOverride>.NativeClassPtr, "Entries");
		NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<MonsterDifficultyOverride>.NativeClassPtr, 100664163);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 34933, XrefRangeEnd = 34948, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe MonsterDifficultyOverride()
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<MonsterDifficultyOverride>.NativeClassPtr))
	{
		IntPtr* param = null;
		Unsafe.SkipInit(out IntPtr exc);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	public MonsterDifficultyOverride(IntPtr pointer)
		: base(pointer)
	{
	}
}
