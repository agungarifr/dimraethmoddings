using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using Unity.Netcode;

public class DifficultyManager : NetworkBehaviour
{
	public class ResolvedDials : Il2CppSystem.Object
	{
		private static readonly System.IntPtr NativeFieldInfoPtr_Identity;

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

		private static readonly System.IntPtr NativeMethodInfoPtr_From_Public_Static_ResolvedDials_DifficultyDials_0;

		private static readonly System.IntPtr NativeMethodInfoPtr_Layer_Public_ResolvedDials_DifficultyDials_0;

		private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

		public unsafe static ResolvedDials Identity
		{
			get
			{
				Unsafe.SkipInit(out System.IntPtr intPtr);
				IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_Identity, &intPtr);
				System.IntPtr intPtr2 = intPtr;
				return (intPtr2 != (System.IntPtr)0) ? Il2CppObjectPool.Get<ResolvedDials>(intPtr2) : null;
			}
			set
			{
				IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_Identity, (void*)IL2CPP.Il2CppObjectBaseToPtr(obj));
			}
		}

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

		static ResolvedDials()
		{
			Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr = IL2CPP.GetIl2CppNestedType(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, "ResolvedDials");
			IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr);
			NativeFieldInfoPtr_Identity = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, "Identity");
			NativeFieldInfoPtr_HealthMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, "HealthMultiplier");
			NativeFieldInfoPtr_DamageDealtMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, "DamageDealtMultiplier");
			NativeFieldInfoPtr_DamageTakenMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, "DamageTakenMultiplier");
			NativeFieldInfoPtr_SpeedMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, "SpeedMultiplier");
			NativeFieldInfoPtr_BonusLevel = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, "BonusLevel");
			NativeFieldInfoPtr_CooldownRecoveryMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, "CooldownRecoveryMultiplier");
			NativeFieldInfoPtr_CombatPaceMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, "CombatPaceMultiplier");
			NativeFieldInfoPtr_TokensPerTurnMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, "TokensPerTurnMultiplier");
			NativeFieldInfoPtr_EmpowermentChanceMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, "EmpowermentChanceMultiplier");
			NativeFieldInfoPtr_RespawnTimeMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, "RespawnTimeMultiplier");
			NativeFieldInfoPtr_XPMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, "XPMultiplier");
			NativeFieldInfoPtr_GoldMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, "GoldMultiplier");
			NativeFieldInfoPtr_LootChanceMultiplier = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, "LootChanceMultiplier");
			NativeFieldInfoPtr_ExtraStartingEffects = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, "ExtraStartingEffects");
			NativeMethodInfoPtr_From_Public_Static_ResolvedDials_DifficultyDials_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, 100683074);
			NativeMethodInfoPtr_Layer_Public_ResolvedDials_DifficultyDials_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, 100683075);
			NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr, 100683076);
		}

		[CallerCount(0)]
		[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 293184, XrefRangeEnd = 293188, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		public unsafe static ResolvedDials From(DifficultyDials dials)
		{
			System.IntPtr* ptr = stackalloc System.IntPtr[1];
			*ptr = IL2CPP.Il2CppObjectBaseToPtr(dials);
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_From_Public_Static_ResolvedDials_DifficultyDials_0, (System.IntPtr)0, (void**)ptr, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<ResolvedDials>(intPtr) : null;
		}

		[CallerCount(2)]
		[CachedScanResults(RefRangeStart = 293209, RefRangeEnd = 293211, XrefRangeStart = 293188, XrefRangeEnd = 293209, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		public unsafe ResolvedDials Layer(DifficultyDials dials)
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* ptr = stackalloc System.IntPtr[1];
			*ptr = IL2CPP.Il2CppObjectBaseToPtr(dials);
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_Layer_Public_ResolvedDials_DifficultyDials_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<ResolvedDials>(intPtr) : null;
		}

		[CallerCount(0)]
		[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 293211, XrefRangeEnd = 293212, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		public unsafe ResolvedDials()
			: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<ResolvedDials>.NativeClassPtr))
		{
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		}

		public ResolvedDials(System.IntPtr pointer)
			: base(pointer)
		{
		}
	}

	private static readonly System.IntPtr NativeFieldInfoPtr__Singleton_k__BackingField;

	private static readonly System.IntPtr NativeFieldInfoPtr_Definitions;

	private static readonly System.IntPtr NativeFieldInfoPtr_Overrides;

	private static readonly System.IntPtr NativeFieldInfoPtr__difficultySync;

	private static readonly System.IntPtr NativeFieldInfoPtr__resolvedByType;

	private static readonly System.IntPtr NativeFieldInfoPtr__baseResolved;

	private static readonly System.IntPtr NativeFieldInfoPtr__cachedDifficulty;

	private static readonly System.IntPtr NativeFieldInfoPtr__cacheBuilt;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_Singleton_Public_Static_get_DifficultyManager_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_set_Singleton_Private_Static_set_Void_DifficultyManager_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_ActiveDifficulty_Public_get_Difficulty_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_Awake_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_OnNetworkSpawn_Public_Virtual_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_OnDestroy_Public_Virtual_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_Update_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetMonsterHealthMultiplier_Public_Static_Single_Monster_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetMonsterBonusLevel_Public_Static_Int32_MonsterConfiguration_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetExtraStartingEffects_Public_Static_List_1_MonsterStartingEffect_MonsterConfiguration_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetEmpowermentChanceMultiplier_Public_Static_Single_MonsterConfiguration_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetRespawnTimeMultiplier_Public_Static_Single_MonsterConfiguration_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetXPMultiplier_Public_Static_Single_MonsterConfiguration_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetGoldMultiplier_Public_Static_Single_MonsterConfiguration_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetLootChanceMultiplier_Public_Static_Single_MonsterConfiguration_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetMonsterDamageDealtMultiplier_Public_Static_Single_Monster_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetMonsterDamageTakenMultiplier_Public_Static_Single_Monster_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetCooldownRecoveryMultiplier_Public_Static_Single_Monster_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetCombatPaceMultiplier_Public_Static_Single_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetTokensPerTurnMultiplier_Public_Static_Single_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetMonsterSpeedMultiplier_Public_Static_Single_ObjectsCommon_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_DialsFor_Private_Static_ResolvedDials_MonsterConfiguration_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_DialsForHostile_Private_Static_ResolvedDials_Monster_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_ResolveFor_Private_ResolvedDials_MonsterConfiguration_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_EnsureCache_Private_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_BuildResolved_Private_ResolvedDials_MonsterType_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr___initializeVariables_Protected_Virtual_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr___initializeRpcs_Protected_Virtual_Void_0;

	private static readonly System.IntPtr NativeMethodInfoPtr___getTypeName_FamOrAssem_Virtual_String_0;

	public unsafe static DifficultyManager _Singleton_k__BackingField
	{
		get
		{
			Unsafe.SkipInit(out System.IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr__Singleton_k__BackingField, &intPtr);
			System.IntPtr intPtr2 = intPtr;
			return (intPtr2 != (System.IntPtr)0) ? Il2CppObjectPool.Get<DifficultyManager>(intPtr2) : null;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr__Singleton_k__BackingField, (void*)IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe List<DifficultyDefinition> Definitions
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Definitions);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<DifficultyDefinition>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Definitions), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe List<MonsterDifficultyOverride> Overrides
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Overrides);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<MonsterDifficultyOverride>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Overrides), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe NetworkVariable<int> _difficultySync
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__difficultySync);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<NetworkVariable<int>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__difficultySync), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Dictionary<MonsterType, ResolvedDials> _resolvedByType
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__resolvedByType);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Dictionary<MonsterType, ResolvedDials>>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__resolvedByType), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe ResolvedDials _baseResolved
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__baseResolved);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<ResolvedDials>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__baseResolved), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe Difficulty _cachedDifficulty
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__cachedDifficulty);
			return *(Difficulty*)num;
		}
		set
		{
			*(Difficulty*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__cachedDifficulty)) = difficulty;
		}
	}

	public unsafe bool _cacheBuilt
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__cacheBuilt);
			return *(bool*)num;
		}
		set
		{
			*(bool*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__cacheBuilt)) = flag;
		}
	}

	public unsafe static DifficultyManager Singleton
	{
		[CallerCount(0)]
		[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 293212, XrefRangeEnd = 293214, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_Singleton_Public_Static_get_DifficultyManager_0, (System.IntPtr)0, (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<DifficultyManager>(intPtr) : null;
		}
		[CallerCount(0)]
		[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 293214, XrefRangeEnd = 293218, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		set
		{
			System.IntPtr* ptr = stackalloc System.IntPtr[1];
			*ptr = IL2CPP.Il2CppObjectBaseToPtr(value);
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_set_Singleton_Private_Static_set_Void_DifficultyManager_0, (System.IntPtr)0, (void**)ptr, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		}
	}

	public unsafe Difficulty ActiveDifficulty
	{
		[CallerCount(0)]
		[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 293218, XrefRangeEnd = 293246, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			System.IntPtr* param = null;
			Unsafe.SkipInit(out System.IntPtr exc);
			System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_ActiveDifficulty_Public_get_Difficulty_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
			Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
			return *(Difficulty*)IL2CPP.il2cpp_object_unbox(obj);
		}
	}

	static DifficultyManager()
	{
		Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "DifficultyManager");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr);
		NativeFieldInfoPtr__Singleton_k__BackingField = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, "<Singleton>k__BackingField");
		NativeFieldInfoPtr_Definitions = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, "Definitions");
		NativeFieldInfoPtr_Overrides = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, "Overrides");
		NativeFieldInfoPtr__difficultySync = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, "_difficultySync");
		NativeFieldInfoPtr__resolvedByType = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, "_resolvedByType");
		NativeFieldInfoPtr__baseResolved = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, "_baseResolved");
		NativeFieldInfoPtr__cachedDifficulty = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, "_cachedDifficulty");
		NativeFieldInfoPtr__cacheBuilt = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, "_cacheBuilt");
		NativeMethodInfoPtr_get_Singleton_Public_Static_get_DifficultyManager_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683044);
		NativeMethodInfoPtr_set_Singleton_Private_Static_set_Void_DifficultyManager_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683045);
		NativeMethodInfoPtr_get_ActiveDifficulty_Public_get_Difficulty_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683046);
		NativeMethodInfoPtr_Awake_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683047);
		NativeMethodInfoPtr_OnNetworkSpawn_Public_Virtual_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683048);
		NativeMethodInfoPtr_OnDestroy_Public_Virtual_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683049);
		NativeMethodInfoPtr_Update_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683050);
		NativeMethodInfoPtr_GetMonsterHealthMultiplier_Public_Static_Single_Monster_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683051);
		NativeMethodInfoPtr_GetMonsterBonusLevel_Public_Static_Int32_MonsterConfiguration_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683052);
		NativeMethodInfoPtr_GetExtraStartingEffects_Public_Static_List_1_MonsterStartingEffect_MonsterConfiguration_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683053);
		NativeMethodInfoPtr_GetEmpowermentChanceMultiplier_Public_Static_Single_MonsterConfiguration_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683054);
		NativeMethodInfoPtr_GetRespawnTimeMultiplier_Public_Static_Single_MonsterConfiguration_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683055);
		NativeMethodInfoPtr_GetXPMultiplier_Public_Static_Single_MonsterConfiguration_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683056);
		NativeMethodInfoPtr_GetGoldMultiplier_Public_Static_Single_MonsterConfiguration_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683057);
		NativeMethodInfoPtr_GetLootChanceMultiplier_Public_Static_Single_MonsterConfiguration_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683058);
		NativeMethodInfoPtr_GetMonsterDamageDealtMultiplier_Public_Static_Single_Monster_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683059);
		NativeMethodInfoPtr_GetMonsterDamageTakenMultiplier_Public_Static_Single_Monster_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683060);
		NativeMethodInfoPtr_GetCooldownRecoveryMultiplier_Public_Static_Single_Monster_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683061);
		NativeMethodInfoPtr_GetCombatPaceMultiplier_Public_Static_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683062);
		NativeMethodInfoPtr_GetTokensPerTurnMultiplier_Public_Static_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683063);
		NativeMethodInfoPtr_GetMonsterSpeedMultiplier_Public_Static_Single_ObjectsCommon_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683064);
		NativeMethodInfoPtr_DialsFor_Private_Static_ResolvedDials_MonsterConfiguration_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683065);
		NativeMethodInfoPtr_DialsForHostile_Private_Static_ResolvedDials_Monster_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683066);
		NativeMethodInfoPtr_ResolveFor_Private_ResolvedDials_MonsterConfiguration_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683067);
		NativeMethodInfoPtr_EnsureCache_Private_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683068);
		NativeMethodInfoPtr_BuildResolved_Private_ResolvedDials_MonsterType_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683069);
		NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683070);
		NativeMethodInfoPtr___initializeVariables_Protected_Virtual_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683071);
		NativeMethodInfoPtr___initializeRpcs_Protected_Virtual_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683072);
		NativeMethodInfoPtr___getTypeName_FamOrAssem_Virtual_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr, 100683073);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 293246, XrefRangeEnd = 293265, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void Awake()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_Awake_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 293265, XrefRangeEnd = 293276, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe override void OnNetworkSpawn()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr(this), NativeMethodInfoPtr_OnNetworkSpawn_Public_Virtual_Void_0), IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 293276, XrefRangeEnd = 293287, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe override void OnDestroy()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr(this), NativeMethodInfoPtr_OnDestroy_Public_Virtual_Void_0), IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 293287, XrefRangeEnd = 293300, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void Update()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_Update_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 293301, RefRangeEnd = 293302, XrefRangeStart = 293300, XrefRangeEnd = 293301, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float GetMonsterHealthMultiplier(Monster monster)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(monster);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetMonsterHealthMultiplier_Public_Static_Single_Monster_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 293303, RefRangeEnd = 293305, XrefRangeStart = 293302, XrefRangeEnd = 293303, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int GetMonsterBonusLevel(MonsterConfiguration config)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(config);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetMonsterBonusLevel_Public_Static_Int32_MonsterConfiguration_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 293306, RefRangeEnd = 293307, XrefRangeStart = 293305, XrefRangeEnd = 293306, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static List<MonsterStartingEffect> GetExtraStartingEffects(MonsterConfiguration config)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(config);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetExtraStartingEffects_Public_Static_List_1_MonsterStartingEffect_MonsterConfiguration_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<MonsterStartingEffect>>(intPtr) : null;
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 293307, XrefRangeEnd = 293308, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float GetEmpowermentChanceMultiplier(MonsterConfiguration config)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(config);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetEmpowermentChanceMultiplier_Public_Static_Single_MonsterConfiguration_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 293309, RefRangeEnd = 293310, XrefRangeStart = 293308, XrefRangeEnd = 293309, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float GetRespawnTimeMultiplier(MonsterConfiguration config)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(config);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetRespawnTimeMultiplier_Public_Static_Single_MonsterConfiguration_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 293311, RefRangeEnd = 293313, XrefRangeStart = 293310, XrefRangeEnd = 293311, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float GetXPMultiplier(MonsterConfiguration config)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(config);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetXPMultiplier_Public_Static_Single_MonsterConfiguration_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 293314, RefRangeEnd = 293315, XrefRangeStart = 293313, XrefRangeEnd = 293314, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float GetGoldMultiplier(MonsterConfiguration config)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(config);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetGoldMultiplier_Public_Static_Single_MonsterConfiguration_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 293316, RefRangeEnd = 293317, XrefRangeStart = 293315, XrefRangeEnd = 293316, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float GetLootChanceMultiplier(MonsterConfiguration config)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(config);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetLootChanceMultiplier_Public_Static_Single_MonsterConfiguration_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 293318, RefRangeEnd = 293319, XrefRangeStart = 293317, XrefRangeEnd = 293318, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float GetMonsterDamageDealtMultiplier(Monster monster)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(monster);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetMonsterDamageDealtMultiplier_Public_Static_Single_Monster_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 293320, RefRangeEnd = 293321, XrefRangeStart = 293319, XrefRangeEnd = 293320, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float GetMonsterDamageTakenMultiplier(Monster monster)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(monster);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetMonsterDamageTakenMultiplier_Public_Static_Single_Monster_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 293322, RefRangeEnd = 293323, XrefRangeStart = 293321, XrefRangeEnd = 293322, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float GetCooldownRecoveryMultiplier(Monster monster)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(monster);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetCooldownRecoveryMultiplier_Public_Static_Single_Monster_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 293329, RefRangeEnd = 293331, XrefRangeStart = 293323, XrefRangeEnd = 293329, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float GetCombatPaceMultiplier()
	{
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetCombatPaceMultiplier_Public_Static_Single_0, (System.IntPtr)0, (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 293337, RefRangeEnd = 293338, XrefRangeStart = 293331, XrefRangeEnd = 293337, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float GetTokensPerTurnMultiplier()
	{
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetTokensPerTurnMultiplier_Public_Static_Single_0, (System.IntPtr)0, (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 293341, RefRangeEnd = 293343, XrefRangeStart = 293338, XrefRangeEnd = 293341, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float GetMonsterSpeedMultiplier(ObjectsCommon obj)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetMonsterSpeedMultiplier_Public_Static_Single_ObjectsCommon_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(8)]
	[CachedScanResults(RefRangeStart = 293353, RefRangeEnd = 293361, XrefRangeStart = 293343, XrefRangeEnd = 293353, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static ResolvedDials DialsFor(MonsterConfiguration config)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(config);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_DialsFor_Private_Static_ResolvedDials_MonsterConfiguration_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<ResolvedDials>(intPtr) : null;
	}

	[CallerCount(5)]
	[CachedScanResults(RefRangeStart = 293378, RefRangeEnd = 293383, XrefRangeStart = 293361, XrefRangeEnd = 293378, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static ResolvedDials DialsForHostile(Monster monster)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(monster);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_DialsForHostile_Private_Static_ResolvedDials_Monster_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<ResolvedDials>(intPtr) : null;
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 293395, RefRangeEnd = 293396, XrefRangeStart = 293383, XrefRangeEnd = 293395, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe ResolvedDials ResolveFor(MonsterConfiguration config)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(config);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_ResolveFor_Private_ResolvedDials_MonsterConfiguration_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<ResolvedDials>(intPtr) : null;
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 293464, RefRangeEnd = 293465, XrefRangeStart = 293396, XrefRangeEnd = 293464, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void EnsureCache()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_EnsureCache_Private_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 293501, RefRangeEnd = 293502, XrefRangeStart = 293465, XrefRangeEnd = 293501, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe ResolvedDials BuildResolved(MonsterType type)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = (nint)(&type);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_BuildResolved_Private_ResolvedDials_MonsterType_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<ResolvedDials>(intPtr) : null;
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 293502, XrefRangeEnd = 293534, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe DifficultyManager()
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<DifficultyManager>.NativeClassPtr))
	{
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 293534, XrefRangeEnd = 293552, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe override void __initializeVariables()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr(this), NativeMethodInfoPtr___initializeVariables_Protected_Virtual_Void_0), IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(3573)]
	[CachedScanResults(RefRangeStart = 24693, RefRangeEnd = 28266, XrefRangeStart = 24693, XrefRangeEnd = 28266, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe override void __initializeRpcs()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr(this), NativeMethodInfoPtr___initializeRpcs_Protected_Virtual_Void_0), IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 293552, XrefRangeEnd = 293554, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe override string __getTypeName()
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr il2CppString = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr(this), NativeMethodInfoPtr___getTypeName_FamOrAssem_Virtual_String_0), IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return IL2CPP.Il2CppStringToManaged(il2CppString);
	}

	public DifficultyManager(System.IntPtr pointer)
		: base(pointer)
	{
	}
}
