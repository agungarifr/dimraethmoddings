using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;

[System.Serializable]
public class DifficultyDials : Il2CppSystem.Object
{
	private static readonly System.IntPtr NativeFieldInfoPtr_HealthMultiplier;

	private static readonly System.IntPtr NativeFieldInfoPtr_DamageDealtMultiplier;

	private static readonly System.IntPtr NativeFieldInfoPtr_DamageTakenMultiplier;

	private static readonly System.IntPtr NativeFieldInfoPtr_SpeedMultiplier;

	private static readonly System.IntPtr NativeFieldInfoPtr_BonusLevel;

	private static readonly System.IntPtr NativeFieldInfoPtr_CooldownRecoveryMultiplier;

	private static readonly System.IntPtr NativeFieldInfoPtr_CombatPaceMultiplier;

	private static readonly System.IntPtr NativeFieldInfoPtr_TokensPerTurnMultiplier;

	private static readonly System.IntPtr NativeFieldInfoPtr_EmpowermentChanceMultiplier;

	private static readonly System.IntPtr NativeFieldInfoPtr_RespawnTimeMultiplier;

	private static readonly System.IntPtr NativeFieldInfoPtr_XPMultiplier;

	private static readonly System.IntPtr NativeFieldInfoPtr_GoldMultiplier;

	private static readonly System.IntPtr NativeFieldInfoPtr_LootChanceMultiplier;

	private static readonly System.IntPtr NativeFieldInfoPtr_ExtraStartingEffects;

	private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

	public unsafe float HealthMultiplier
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_HealthMultiplier);
			return *(float*)num;
		}
		set
		{
			*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_HealthMultiplier)) = num;
		}
	}

	public unsafe float DamageDealtMultiplier
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_DamageDealtMultiplier);
			return *(float*)num;
		}
		set
		{
			*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_DamageDealtMultiplier)) = num;
		}
	}

	public unsafe float DamageTakenMultiplier
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_DamageTakenMultiplier);
			return *(float*)num;
		}
		set
		{
			*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_DamageTakenMultiplier)) = num;
		}
	}

	public unsafe float SpeedMultiplier
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_SpeedMultiplier);
			return *(float*)num;
		}
		set
		{
			*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_SpeedMultiplier)) = num;
		}
	}

	public unsafe int BonusLevel
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_BonusLevel);
			return *(int*)num;
		}
		set
		{
			*(int*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_BonusLevel)) = num;
		}
	}

	public unsafe float CooldownRecoveryMultiplier
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_CooldownRecoveryMultiplier);
			return *(float*)num;
		}
		set
		{
			*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_CooldownRecoveryMultiplier)) = num;
		}
	}

	public unsafe float CombatPaceMultiplier
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_CombatPaceMultiplier);
			return *(float*)num;
		}
		set
		{
			*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_CombatPaceMultiplier)) = num;
		}
	}

	public unsafe float TokensPerTurnMultiplier
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_TokensPerTurnMultiplier);
			return *(float*)num;
		}
		set
		{
			*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_TokensPerTurnMultiplier)) = num;
		}
	}

	public unsafe float EmpowermentChanceMultiplier
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_EmpowermentChanceMultiplier);
			return *(float*)num;
		}
		set
		{
			*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_EmpowermentChanceMultiplier)) = num;
		}
	}

	public unsafe float RespawnTimeMultiplier
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_RespawnTimeMultiplier);
			return *(float*)num;
		}
		set
		{
			*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_RespawnTimeMultiplier)) = num;
		}
	}

	public unsafe float XPMultiplier
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_XPMultiplier);
			return *(float*)num;
		}
		set
		{
			*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_XPMultiplier)) = num;
		}
	}

	public unsafe float GoldMultiplier
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_GoldMultiplier);
			return *(float*)num;
		}
		set
		{
			*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_GoldMultiplier)) = num;
		}
	}

	public unsafe float LootChanceMultiplier
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_LootChanceMultiplier);
			return *(float*)num;
		}
		set
		{
			*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_LootChanceMultiplier)) = num;
		}
	}

	public unsafe List<MonsterStartingEffect> ExtraStartingEffects
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_ExtraStartingEffects);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<MonsterStartingEffect>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_ExtraStartingEffects), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	static DifficultyDials()
	{
		Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "DifficultyDials");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr);
		NativeFieldInfoPtr_HealthMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr, "HealthMultiplier");
		NativeFieldInfoPtr_DamageDealtMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr, "DamageDealtMultiplier");
		NativeFieldInfoPtr_DamageTakenMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr, "DamageTakenMultiplier");
		NativeFieldInfoPtr_SpeedMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr, "SpeedMultiplier");
		NativeFieldInfoPtr_BonusLevel = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr, "BonusLevel");
		NativeFieldInfoPtr_CooldownRecoveryMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr, "CooldownRecoveryMultiplier");
		NativeFieldInfoPtr_CombatPaceMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr, "CombatPaceMultiplier");
		NativeFieldInfoPtr_TokensPerTurnMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr, "TokensPerTurnMultiplier");
		NativeFieldInfoPtr_EmpowermentChanceMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr, "EmpowermentChanceMultiplier");
		NativeFieldInfoPtr_RespawnTimeMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr, "RespawnTimeMultiplier");
		NativeFieldInfoPtr_XPMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr, "XPMultiplier");
		NativeFieldInfoPtr_GoldMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr, "GoldMultiplier");
		NativeFieldInfoPtr_LootChanceMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr, "LootChanceMultiplier");
		NativeFieldInfoPtr_ExtraStartingEffects = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr, "ExtraStartingEffects");
		NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr, 100664141);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 34624, RefRangeEnd = 34626, XrefRangeStart = 34616, XrefRangeEnd = 34624, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe DifficultyDials()
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<DifficultyDials>.NativeClassPtr))
	{
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	public DifficultyDials(System.IntPtr pointer)
		: base(pointer)
	{
	}
}
