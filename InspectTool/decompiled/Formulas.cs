using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

public class Formulas : Il2CppSystem.Object
{
	private static readonly System.IntPtr NativeFieldInfoPtr_HealthEffects;

	private static readonly System.IntPtr NativeFieldInfoPtr_EmptyEffects;

	private static readonly System.IntPtr NativeFieldInfoPtr_ConcentrationEffects;

	private static readonly System.IntPtr NativeFieldInfoPtr_StaminaEffects;

	private static readonly System.IntPtr NativeFieldInfoPtr_ResistanceEffects;

	private static readonly System.IntPtr NativeFieldInfoPtr_CritRateEffects;

	private static readonly System.IntPtr NativeFieldInfoPtr_CritDamageEffects;

	private static readonly System.IntPtr NativeFieldInfoPtr_AttackSpeedEffects;

	private static readonly System.IntPtr NativeFieldInfoPtr_PlayerSpeedEffects;

	private static readonly System.IntPtr NativeFieldInfoPtr_MonsterSpeedEffects;

	private static readonly System.IntPtr NativeFieldInfoPtr_EncumbranceEffects;

	private static readonly System.IntPtr NativeFieldInfoPtr_EmptyEffectValues;

	private static readonly System.IntPtr NativeFieldInfoPtr_AttributeCurveDivisor;

	private static readonly System.IntPtr NativeFieldInfoPtr_AttributeCurveExponent;

	private static readonly System.IntPtr NativeFieldInfoPtr_PlayerLevelFlatCost;

	private static readonly System.IntPtr NativeFieldInfoPtr_StunRatchetStep;

	private static readonly System.IntPtr NativeFieldInfoPtr_UnlimitedCarryCapacity;

	private static readonly System.IntPtr NativeMethodInfoPtr_ListHasEffect_Private_Static_Boolean_List_1_EffectValues_Effect_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_AttributesModificationCheck_Private_Static_Attributes_ObjectsCommon_List_1_EffectValues_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_ResolveAttributes_Private_Static_Attributes_ObjectsCommon_Attributes_List_1_EffectValues_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_AddSkillTreeBonuses_Public_Static_Void_ObjectsCommon_byref_Single_byref_Single_Il2CppStructArray_1_Stat_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateAttribute_Public_Static_Single_ObjectsCommon_List_1_Effect_Single_Single_Single_Boolean_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculatePhysicalCombatPower_Public_Static_Single_ObjectsCommon_Attributes_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateMagicCombatPower_Public_Static_Single_ObjectsCommon_Attributes_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateSupportCombatPower_Public_Static_Single_ObjectsCommon_Attributes_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_UpgradeCostForAttribute_Public_Static_Single_Player_Attribute_Nullable_1_Int32_Nullable_1_Int32_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_AttributeCurveCost_Public_Static_Single_Single_Int32_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetStartingAttributeLevel_Public_Static_Int32_Race_Class_Attribute_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetBaseCostForStat_Public_Static_Int32_Int32_Int32_Attribute_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetAttributeLevel_Public_Static_Int32_Player_Attribute_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateMaxHealth_Public_Static_Single_ObjectsCommon_Attributes_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateMaxStunBuildUp_Public_Static_Single_ObjectsCommon_Attributes_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateMaxConcentration_Public_Static_Single_ObjectsCommon_Attributes_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateMaxStamina_Public_Static_Single_ObjectsCommon_Attributes_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateBaseMaxHealth_Public_Static_Single_Player_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateBaseMaxConcentration_Public_Static_Single_Player_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateBaseMaxStamina_Public_Static_Single_Player_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateBaseSpeed_Public_Static_Single_Player_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateBaseAttackSpeed_Public_Static_Single_Player_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateBaseSpellHaste_Public_Static_Single_Player_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateResistance_Public_Static_Single_ObjectsCommon_Attributes_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetMinionStatBonus_Public_Static_Single_ObjectsCommon_Stat_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetGuardEfficiencyMultiplier_Public_Static_Single_ObjectsCommon_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateCriticalRate_Public_Static_Single_ObjectsCommon_Attributes_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateCriticalDamage_Public_Static_Single_ObjectsCommon_Attributes_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateSpellHaste_Public_Static_Single_ObjectsCommon_Attributes_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetCooldownMultiplier_Public_Static_Single_Single_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateSpellCooldown_Public_Static_Single_ObjectsCommon_Single_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateAttackSpeed_Public_Static_Single_ObjectsCommon_Single_Attributes_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateSpeed_Public_Static_Single_ObjectsCommon_Attributes_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculatePlayerSpeed_Public_Static_Single_Player_Attributes_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_SneakCheck_Private_Static_Single_Player_Single_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateMonsterSpeed_Private_Static_Single_ObjectsCommon_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateEncumbrance_Public_Static_Single_ObjectsCommon_Attributes_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_IsEncumbranceEnforced_Public_Static_Boolean_Player_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_ExceedsEncumbrance_Public_Static_Boolean_Player_Single_Single_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_RemainingCarryCapacity_Public_Static_Single_Player_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_EmpoweredHealthBonusPercent_Public_Static_Single_MonsterConfiguration_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_MonsterHPInflation_Public_Static_Int32_ObjectsCommon_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_MonsterStunBuildUpInflation_Public_Static_Int32_ObjectsCommon_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetMonsterDamageScaling_Public_Static_Single_ObjectsCommon_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateGoldDrop_Public_Static_Int32_List_1_EffectValues_Int32_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateHealthRegeneration_Public_Static_Single_Attributes_List_1_EffectValues_ObjectsCommon_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateConcentrationRegeneration_Public_Static_Single_Attributes_List_1_EffectValues_ObjectsCommon_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateConcentrationRegenOnHit_Public_Static_Single_Attributes_List_1_EffectValues_ObjectsCommon_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateStaminaRegeneration_Public_Static_Single_Attributes_List_1_EffectValues_ObjectsCommon_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateSprintingCostPerSecond_Public_Static_Single_Player_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetPercentageOfMax_Public_Static_Int32_Int32_Int32_Single_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetBarSprite_Public_Static_Sprite_Il2CppReferenceArray_1_Sprite_Single_Single_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetBarSprite_Public_Static_Sprite_List_1_Sprite_Int32_Int32_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetBarSpriteReverse_Public_Static_Sprite_Il2CppReferenceArray_1_Sprite_Int32_Int32_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetArchetypeName_Public_Static_String_Race_Class_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetLocalizedArchetypeName_Public_Static_String_Race_Class_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetLocalizedRaceName_Public_Static_String_Race_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetLocalizedClassName_Public_Static_String_Class_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetRaceAttributesBonus_Public_Static_Attributes_Race_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetClassAttributesBonus_Public_Static_Attributes_Class_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetRaceBonusByAttribute_Public_Static_Int32_Race_Attribute_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetClassBonusByStat_Public_Static_Int32_Class_Attribute_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

	public unsafe static List<Effect> HealthEffects
	{
		get
		{
			Unsafe.SkipInit(out System.IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_HealthEffects, &intPtr);
			System.IntPtr intPtr2 = intPtr;
			return (intPtr2 != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<Effect>>(intPtr2) : null;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_HealthEffects, (void*)IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe static List<Effect> EmptyEffects
	{
		get
		{
			Unsafe.SkipInit(out System.IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_EmptyEffects, &intPtr);
			System.IntPtr intPtr2 = intPtr;
			return (intPtr2 != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<Effect>>(intPtr2) : null;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_EmptyEffects, (void*)IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe static List<Effect> ConcentrationEffects
	{
		get
		{
			Unsafe.SkipInit(out System.IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_ConcentrationEffects, &intPtr);
			System.IntPtr intPtr2 = intPtr;
			return (intPtr2 != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<Effect>>(intPtr2) : null;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_ConcentrationEffects, (void*)IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe static List<Effect> StaminaEffects
	{
		get
		{
			Unsafe.SkipInit(out System.IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_StaminaEffects, &intPtr);
			System.IntPtr intPtr2 = intPtr;
			return (intPtr2 != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<Effect>>(intPtr2) : null;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_StaminaEffects, (void*)IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe static List<Effect> ResistanceEffects
	{
		get
		{
			Unsafe.SkipInit(out System.IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_ResistanceEffects, &intPtr);
			System.IntPtr intPtr2 = intPtr;
			return (intPtr2 != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<Effect>>(intPtr2) : null;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_ResistanceEffects, (void*)IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe static List<Effect> CritRateEffects
	{
		get
		{
			Unsafe.SkipInit(out System.IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_CritRateEffects, &intPtr);
			System.IntPtr intPtr2 = intPtr;
			return (intPtr2 != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<Effect>>(intPtr2) : null;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_CritRateEffects, (void*)IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe static List<Effect> CritDamageEffects
	{
		get
		{
			Unsafe.SkipInit(out System.IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_CritDamageEffects, &intPtr);
			System.IntPtr intPtr2 = intPtr;
			return (intPtr2 != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<Effect>>(intPtr2) : null;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_CritDamageEffects, (void*)IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe static List<Effect> AttackSpeedEffects
	{
		get
		{
			Unsafe.SkipInit(out System.IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_AttackSpeedEffects, &intPtr);
			System.IntPtr intPtr2 = intPtr;
			return (intPtr2 != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<Effect>>(intPtr2) : null;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_AttackSpeedEffects, (void*)IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe static List<Effect> PlayerSpeedEffects
	{
		get
		{
			Unsafe.SkipInit(out System.IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_PlayerSpeedEffects, &intPtr);
			System.IntPtr intPtr2 = intPtr;
			return (intPtr2 != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<Effect>>(intPtr2) : null;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_PlayerSpeedEffects, (void*)IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe static List<Effect> MonsterSpeedEffects
	{
		get
		{
			Unsafe.SkipInit(out System.IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_MonsterSpeedEffects, &intPtr);
			System.IntPtr intPtr2 = intPtr;
			return (intPtr2 != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<Effect>>(intPtr2) : null;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_MonsterSpeedEffects, (void*)IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe static List<Effect> EncumbranceEffects
	{
		get
		{
			Unsafe.SkipInit(out System.IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_EncumbranceEffects, &intPtr);
			System.IntPtr intPtr2 = intPtr;
			return (intPtr2 != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<Effect>>(intPtr2) : null;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_EncumbranceEffects, (void*)IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe static List<EffectValues> EmptyEffectValues
	{
		get
		{
			Unsafe.SkipInit(out System.IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_EmptyEffectValues, &intPtr);
			System.IntPtr intPtr2 = intPtr;
			return (intPtr2 != (System.IntPtr)0) ? Il2CppObjectPool.Get<List<EffectValues>>(intPtr2) : null;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_EmptyEffectValues, (void*)IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	public unsafe static float AttributeCurveDivisor
	{
		get
		{
			Unsafe.SkipInit(out float result);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_AttributeCurveDivisor, &result);
			return result;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_AttributeCurveDivisor, &num);
		}
	}

	public unsafe static float AttributeCurveExponent
	{
		get
		{
			Unsafe.SkipInit(out float result);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_AttributeCurveExponent, &result);
			return result;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_AttributeCurveExponent, &num);
		}
	}

	public unsafe static float PlayerLevelFlatCost
	{
		get
		{
			Unsafe.SkipInit(out float result);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_PlayerLevelFlatCost, &result);
			return result;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_PlayerLevelFlatCost, &num);
		}
	}

	public unsafe static float StunRatchetStep
	{
		get
		{
			Unsafe.SkipInit(out float result);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_StunRatchetStep, &result);
			return result;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_StunRatchetStep, &num);
		}
	}

	public unsafe static float UnlimitedCarryCapacity
	{
		get
		{
			Unsafe.SkipInit(out float result);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_UnlimitedCarryCapacity, &result);
			return result;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_UnlimitedCarryCapacity, &num);
		}
	}

	static Formulas()
	{
		Il2CppClassPointerStore<Formulas>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "Formulas");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<Formulas>.NativeClassPtr);
		NativeFieldInfoPtr_HealthEffects = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "HealthEffects");
		NativeFieldInfoPtr_EmptyEffects = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "EmptyEffects");
		NativeFieldInfoPtr_ConcentrationEffects = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "ConcentrationEffects");
		NativeFieldInfoPtr_StaminaEffects = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "StaminaEffects");
		NativeFieldInfoPtr_ResistanceEffects = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "ResistanceEffects");
		NativeFieldInfoPtr_CritRateEffects = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "CritRateEffects");
		NativeFieldInfoPtr_CritDamageEffects = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "CritDamageEffects");
		NativeFieldInfoPtr_AttackSpeedEffects = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "AttackSpeedEffects");
		NativeFieldInfoPtr_PlayerSpeedEffects = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "PlayerSpeedEffects");
		NativeFieldInfoPtr_MonsterSpeedEffects = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "MonsterSpeedEffects");
		NativeFieldInfoPtr_EncumbranceEffects = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "EncumbranceEffects");
		NativeFieldInfoPtr_EmptyEffectValues = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "EmptyEffectValues");
		NativeFieldInfoPtr_AttributeCurveDivisor = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "AttributeCurveDivisor");
		NativeFieldInfoPtr_AttributeCurveExponent = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "AttributeCurveExponent");
		NativeFieldInfoPtr_PlayerLevelFlatCost = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "PlayerLevelFlatCost");
		NativeFieldInfoPtr_StunRatchetStep = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "StunRatchetStep");
		NativeFieldInfoPtr_UnlimitedCarryCapacity = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<Formulas>.NativeClassPtr, "UnlimitedCarryCapacity");
		NativeMethodInfoPtr_ListHasEffect_Private_Static_Boolean_List_1_EffectValues_Effect_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689891);
		NativeMethodInfoPtr_AttributesModificationCheck_Private_Static_Attributes_ObjectsCommon_List_1_EffectValues_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689892);
		NativeMethodInfoPtr_ResolveAttributes_Private_Static_Attributes_ObjectsCommon_Attributes_List_1_EffectValues_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689893);
		NativeMethodInfoPtr_AddSkillTreeBonuses_Public_Static_Void_ObjectsCommon_byref_Single_byref_Single_Il2CppStructArray_1_Stat_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689894);
		NativeMethodInfoPtr_CalculateAttribute_Public_Static_Single_ObjectsCommon_List_1_Effect_Single_Single_Single_Boolean_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689895);
		NativeMethodInfoPtr_CalculatePhysicalCombatPower_Public_Static_Single_ObjectsCommon_Attributes_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689896);
		NativeMethodInfoPtr_CalculateMagicCombatPower_Public_Static_Single_ObjectsCommon_Attributes_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689897);
		NativeMethodInfoPtr_CalculateSupportCombatPower_Public_Static_Single_ObjectsCommon_Attributes_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689898);
		NativeMethodInfoPtr_UpgradeCostForAttribute_Public_Static_Single_Player_Attribute_Nullable_1_Int32_Nullable_1_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689899);
		NativeMethodInfoPtr_AttributeCurveCost_Public_Static_Single_Single_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689900);
		NativeMethodInfoPtr_GetStartingAttributeLevel_Public_Static_Int32_Race_Class_Attribute_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689901);
		NativeMethodInfoPtr_GetBaseCostForStat_Public_Static_Int32_Int32_Int32_Attribute_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689902);
		NativeMethodInfoPtr_GetAttributeLevel_Public_Static_Int32_Player_Attribute_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689903);
		NativeMethodInfoPtr_CalculateMaxHealth_Public_Static_Single_ObjectsCommon_Attributes_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689904);
		NativeMethodInfoPtr_CalculateMaxStunBuildUp_Public_Static_Single_ObjectsCommon_Attributes_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689905);
		NativeMethodInfoPtr_CalculateMaxConcentration_Public_Static_Single_ObjectsCommon_Attributes_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689906);
		NativeMethodInfoPtr_CalculateMaxStamina_Public_Static_Single_ObjectsCommon_Attributes_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689907);
		NativeMethodInfoPtr_CalculateBaseMaxHealth_Public_Static_Single_Player_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689908);
		NativeMethodInfoPtr_CalculateBaseMaxConcentration_Public_Static_Single_Player_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689909);
		NativeMethodInfoPtr_CalculateBaseMaxStamina_Public_Static_Single_Player_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689910);
		NativeMethodInfoPtr_CalculateBaseSpeed_Public_Static_Single_Player_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689911);
		NativeMethodInfoPtr_CalculateBaseAttackSpeed_Public_Static_Single_Player_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689912);
		NativeMethodInfoPtr_CalculateBaseSpellHaste_Public_Static_Single_Player_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689913);
		NativeMethodInfoPtr_CalculateResistance_Public_Static_Single_ObjectsCommon_Attributes_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689914);
		NativeMethodInfoPtr_GetMinionStatBonus_Public_Static_Single_ObjectsCommon_Stat_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689915);
		NativeMethodInfoPtr_GetGuardEfficiencyMultiplier_Public_Static_Single_ObjectsCommon_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689916);
		NativeMethodInfoPtr_CalculateCriticalRate_Public_Static_Single_ObjectsCommon_Attributes_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689917);
		NativeMethodInfoPtr_CalculateCriticalDamage_Public_Static_Single_ObjectsCommon_Attributes_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689918);
		NativeMethodInfoPtr_CalculateSpellHaste_Public_Static_Single_ObjectsCommon_Attributes_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689919);
		NativeMethodInfoPtr_GetCooldownMultiplier_Public_Static_Single_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689920);
		NativeMethodInfoPtr_CalculateSpellCooldown_Public_Static_Single_ObjectsCommon_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689921);
		NativeMethodInfoPtr_CalculateAttackSpeed_Public_Static_Single_ObjectsCommon_Single_Attributes_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689922);
		NativeMethodInfoPtr_CalculateSpeed_Public_Static_Single_ObjectsCommon_Attributes_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689923);
		NativeMethodInfoPtr_CalculatePlayerSpeed_Public_Static_Single_Player_Attributes_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689924);
		NativeMethodInfoPtr_SneakCheck_Private_Static_Single_Player_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689925);
		NativeMethodInfoPtr_CalculateMonsterSpeed_Private_Static_Single_ObjectsCommon_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689926);
		NativeMethodInfoPtr_CalculateEncumbrance_Public_Static_Single_ObjectsCommon_Attributes_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689927);
		NativeMethodInfoPtr_IsEncumbranceEnforced_Public_Static_Boolean_Player_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689928);
		NativeMethodInfoPtr_ExceedsEncumbrance_Public_Static_Boolean_Player_Single_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689929);
		NativeMethodInfoPtr_RemainingCarryCapacity_Public_Static_Single_Player_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689930);
		NativeMethodInfoPtr_EmpoweredHealthBonusPercent_Public_Static_Single_MonsterConfiguration_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689931);
		NativeMethodInfoPtr_MonsterHPInflation_Public_Static_Int32_ObjectsCommon_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689932);
		NativeMethodInfoPtr_MonsterStunBuildUpInflation_Public_Static_Int32_ObjectsCommon_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689933);
		NativeMethodInfoPtr_GetMonsterDamageScaling_Public_Static_Single_ObjectsCommon_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689934);
		NativeMethodInfoPtr_CalculateGoldDrop_Public_Static_Int32_List_1_EffectValues_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689935);
		NativeMethodInfoPtr_CalculateHealthRegeneration_Public_Static_Single_Attributes_List_1_EffectValues_ObjectsCommon_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689936);
		NativeMethodInfoPtr_CalculateConcentrationRegeneration_Public_Static_Single_Attributes_List_1_EffectValues_ObjectsCommon_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689937);
		NativeMethodInfoPtr_CalculateConcentrationRegenOnHit_Public_Static_Single_Attributes_List_1_EffectValues_ObjectsCommon_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689938);
		NativeMethodInfoPtr_CalculateStaminaRegeneration_Public_Static_Single_Attributes_List_1_EffectValues_ObjectsCommon_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689939);
		NativeMethodInfoPtr_CalculateSprintingCostPerSecond_Public_Static_Single_Player_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689940);
		NativeMethodInfoPtr_GetPercentageOfMax_Public_Static_Int32_Int32_Int32_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689941);
		NativeMethodInfoPtr_GetBarSprite_Public_Static_Sprite_Il2CppReferenceArray_1_Sprite_Single_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689942);
		NativeMethodInfoPtr_GetBarSprite_Public_Static_Sprite_List_1_Sprite_Int32_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689943);
		NativeMethodInfoPtr_GetBarSpriteReverse_Public_Static_Sprite_Il2CppReferenceArray_1_Sprite_Int32_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689944);
		NativeMethodInfoPtr_GetArchetypeName_Public_Static_String_Race_Class_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689945);
		NativeMethodInfoPtr_GetLocalizedArchetypeName_Public_Static_String_Race_Class_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689946);
		NativeMethodInfoPtr_GetLocalizedRaceName_Public_Static_String_Race_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689947);
		NativeMethodInfoPtr_GetLocalizedClassName_Public_Static_String_Class_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689948);
		NativeMethodInfoPtr_GetRaceAttributesBonus_Public_Static_Attributes_Race_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689949);
		NativeMethodInfoPtr_GetClassAttributesBonus_Public_Static_Attributes_Class_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689950);
		NativeMethodInfoPtr_GetRaceBonusByAttribute_Public_Static_Int32_Race_Attribute_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689951);
		NativeMethodInfoPtr_GetClassBonusByStat_Public_Static_Int32_Class_Attribute_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689952);
		NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<Formulas>.NativeClassPtr, 100689953);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 435026, RefRangeEnd = 435027, XrefRangeStart = 435022, XrefRangeEnd = 435026, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static bool ListHasEffect(List<EffectValues> effects, Effect target)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(effects);
		*(Effect**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &target;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_ListHasEffect_Private_Static_Boolean_List_1_EffectValues_Effect_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(bool*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(15)]
	[CachedScanResults(RefRangeStart = 435045, RefRangeEnd = 435060, XrefRangeStart = 435027, XrefRangeEnd = 435045, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static Attributes AttributesModificationCheck(ObjectsCommon obj, List<EffectValues> effects)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(effects);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_AttributesModificationCheck_Private_Static_Attributes_ObjectsCommon_List_1_EffectValues_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Attributes>(intPtr) : null;
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 435060, XrefRangeEnd = 435064, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static Attributes ResolveAttributes(ObjectsCommon obj, Attributes attributes, List<EffectValues> effects = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[3];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(effects);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_ResolveAttributes_Private_Static_Attributes_ObjectsCommon_Attributes_List_1_EffectValues_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Attributes>(intPtr) : null;
	}

	[CallerCount(9)]
	[CachedScanResults(RefRangeStart = 435073, RefRangeEnd = 435082, XrefRangeStart = 435064, XrefRangeEnd = 435073, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static void AddSkillTreeBonuses(ObjectsCommon obj, ref float runeFlatBonus, ref float runePercentBonus, [Optional] Il2CppStructArray<Runes.Stat> stats)
	{
		if (stats == null)
		{
			stats = new Il2CppStructArray<Runes.Stat>(0L);
		}
		System.IntPtr* ptr = stackalloc System.IntPtr[4];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(void**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = Unsafe.AsPointer(ref runeFlatBonus);
		*(void**)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = Unsafe.AsPointer(ref runePercentBonus);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)3u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(stats);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_AddSkillTreeBonuses_Public_Static_Void_ObjectsCommon_byref_Single_byref_Single_Il2CppStructArray_1_Stat_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(15)]
	[CachedScanResults(RefRangeStart = 435098, RefRangeEnd = 435113, XrefRangeStart = 435082, XrefRangeEnd = 435098, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateAttribute(ObjectsCommon obj, List<Effect> effects, float baseStat = 0f, float runeFlatBonus = 0f, float runePercentBonus = 0f, bool runeCalcFirst = false, bool tempDebug = false)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[7];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(effects);
		*(float**)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = &baseStat;
		*(float**)((byte*)ptr + checked((nuint)3u * unchecked((nuint)sizeof(System.IntPtr)))) = &runeFlatBonus;
		*(float**)((byte*)ptr + checked((nuint)4u * unchecked((nuint)sizeof(System.IntPtr)))) = &runePercentBonus;
		*(bool**)((byte*)ptr + checked((nuint)5u * unchecked((nuint)sizeof(System.IntPtr)))) = &runeCalcFirst;
		*(bool**)((byte*)ptr + checked((nuint)6u * unchecked((nuint)sizeof(System.IntPtr)))) = &tempDebug;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateAttribute_Public_Static_Single_ObjectsCommon_List_1_Effect_Single_Single_Single_Boolean_Boolean_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(7)]
	[CachedScanResults(RefRangeStart = 435163, RefRangeEnd = 435170, XrefRangeStart = 435113, XrefRangeEnd = 435163, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculatePhysicalCombatPower(ObjectsCommon obj, Attributes attributes = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculatePhysicalCombatPower_Public_Static_Single_ObjectsCommon_Attributes_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(8)]
	[CachedScanResults(RefRangeStart = 435228, RefRangeEnd = 435236, XrefRangeStart = 435170, XrefRangeEnd = 435228, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateMagicCombatPower(ObjectsCommon obj, Attributes attributes = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateMagicCombatPower_Public_Static_Single_ObjectsCommon_Attributes_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 435244, RefRangeEnd = 435245, XrefRangeStart = 435236, XrefRangeEnd = 435244, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateSupportCombatPower(ObjectsCommon obj, Attributes attributes = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateSupportCombatPower_Public_Static_Single_ObjectsCommon_Attributes_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(5)]
	[CachedScanResults(RefRangeStart = 435267, RefRangeEnd = 435272, XrefRangeStart = 435245, XrefRangeEnd = 435267, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float UpgradeCostForAttribute(Player player, Attribute attribute, Il2CppSystem.Nullable<int> attributeLevel = null, Il2CppSystem.Nullable<int> playerLevel = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[4];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		*(Attribute**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &attribute;
		*(System.IntPtr*)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull(attributeLevel));
		*(System.IntPtr*)((byte*)ptr + checked((nuint)3u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull(playerLevel));
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_UpgradeCostForAttribute_Public_Static_Single_Player_Attribute_Nullable_1_Int32_Nullable_1_Int32_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(3)]
	[CachedScanResults(RefRangeStart = 435273, RefRangeEnd = 435276, XrefRangeStart = 435272, XrefRangeEnd = 435273, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float AttributeCurveCost(float baseCost, int pointsAboveStart)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = (nint)(&baseCost);
		*(int**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &pointsAboveStart;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_AttributeCurveCost_Public_Static_Single_Single_Int32_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 435283, RefRangeEnd = 435284, XrefRangeStart = 435276, XrefRangeEnd = 435283, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int GetStartingAttributeLevel(Race race, Class @class, Attribute attribute)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[3];
		*ptr = (nint)(&race);
		*(Class**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &@class;
		*(Attribute**)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = &attribute;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetStartingAttributeLevel_Public_Static_Int32_Race_Class_Attribute_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(3)]
	[CachedScanResults(RefRangeStart = 435293, RefRangeEnd = 435296, XrefRangeStart = 435284, XrefRangeEnd = 435293, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int GetBaseCostForStat(int raceValue, int classValue, Attribute attribute)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[3];
		*ptr = (nint)(&raceValue);
		*(int**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &classValue;
		*(Attribute**)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = &attribute;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetBaseCostForStat_Public_Static_Int32_Int32_Int32_Attribute_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(3)]
	[CachedScanResults(RefRangeStart = 435296, RefRangeEnd = 435299, XrefRangeStart = 435296, XrefRangeEnd = 435296, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int GetAttributeLevel(Player player, Attribute attribute)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		*(Attribute**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &attribute;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetAttributeLevel_Public_Static_Int32_Player_Attribute_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(17)]
	[CachedScanResults(RefRangeStart = 435361, RefRangeEnd = 435378, XrefRangeStart = 435299, XrefRangeEnd = 435361, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateMaxHealth(ObjectsCommon obj, Attributes attributes = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateMaxHealth_Public_Static_Single_ObjectsCommon_Attributes_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(4)]
	[CachedScanResults(RefRangeStart = 435401, RefRangeEnd = 435405, XrefRangeStart = 435378, XrefRangeEnd = 435401, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateMaxStunBuildUp(ObjectsCommon obj, Attributes attributes = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateMaxStunBuildUp_Public_Static_Single_ObjectsCommon_Attributes_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(10)]
	[CachedScanResults(RefRangeStart = 435461, RefRangeEnd = 435471, XrefRangeStart = 435405, XrefRangeEnd = 435461, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateMaxConcentration(ObjectsCommon obj, Attributes attributes = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateMaxConcentration_Public_Static_Single_ObjectsCommon_Attributes_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(10)]
	[CachedScanResults(RefRangeStart = 435540, RefRangeEnd = 435550, XrefRangeStart = 435471, XrefRangeEnd = 435540, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateMaxStamina(ObjectsCommon obj, Attributes attributes = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateMaxStamina_Public_Static_Single_ObjectsCommon_Attributes_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(3)]
	[CachedScanResults(RefRangeStart = 435555, RefRangeEnd = 435558, XrefRangeStart = 435550, XrefRangeEnd = 435555, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateBaseMaxHealth(Player player)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateBaseMaxHealth_Public_Static_Single_Player_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 435563, RefRangeEnd = 435565, XrefRangeStart = 435558, XrefRangeEnd = 435563, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateBaseMaxConcentration(Player player)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateBaseMaxConcentration_Public_Static_Single_Player_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 435570, RefRangeEnd = 435572, XrefRangeStart = 435565, XrefRangeEnd = 435570, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateBaseMaxStamina(Player player)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateBaseMaxStamina_Public_Static_Single_Player_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 435576, RefRangeEnd = 435578, XrefRangeStart = 435572, XrefRangeEnd = 435576, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateBaseSpeed(Player player)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateBaseSpeed_Public_Static_Single_Player_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 435583, RefRangeEnd = 435585, XrefRangeStart = 435578, XrefRangeEnd = 435583, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateBaseAttackSpeed(Player player)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateBaseAttackSpeed_Public_Static_Single_Player_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 435585, RefRangeEnd = 435587, XrefRangeStart = 435585, XrefRangeEnd = 435585, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateBaseSpellHaste(Player player)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateBaseSpellHaste_Public_Static_Single_Player_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 435587, XrefRangeEnd = 435616, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateResistance(ObjectsCommon obj, Attributes attributes = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateResistance_Public_Static_Single_ObjectsCommon_Attributes_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(3)]
	[CachedScanResults(RefRangeStart = 435652, RefRangeEnd = 435655, XrefRangeStart = 435616, XrefRangeEnd = 435652, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float GetMinionStatBonus(ObjectsCommon summoner, Runes.Stat stat)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(summoner);
		*(Runes.Stat**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &stat;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetMinionStatBonus_Public_Static_Single_ObjectsCommon_Stat_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(3)]
	[CachedScanResults(RefRangeStart = 435692, RefRangeEnd = 435695, XrefRangeStart = 435655, XrefRangeEnd = 435692, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float GetGuardEfficiencyMultiplier(ObjectsCommon blocker)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(blocker);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetGuardEfficiencyMultiplier_Public_Static_Single_ObjectsCommon_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(3)]
	[CachedScanResults(RefRangeStart = 435750, RefRangeEnd = 435753, XrefRangeStart = 435695, XrefRangeEnd = 435750, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateCriticalRate(ObjectsCommon obj, Attributes attributes = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateCriticalRate_Public_Static_Single_ObjectsCommon_Attributes_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(3)]
	[CachedScanResults(RefRangeStart = 435795, RefRangeEnd = 435798, XrefRangeStart = 435753, XrefRangeEnd = 435795, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateCriticalDamage(ObjectsCommon obj, Attributes attributes = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateCriticalDamage_Public_Static_Single_ObjectsCommon_Attributes_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(3)]
	[CachedScanResults(RefRangeStart = 435836, RefRangeEnd = 435839, XrefRangeStart = 435798, XrefRangeEnd = 435836, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateSpellHaste(ObjectsCommon obj, Attributes attributes = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateSpellHaste_Public_Static_Single_ObjectsCommon_Attributes_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(0)]
	public unsafe static float GetCooldownMultiplier(float spellHaste)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = (nint)(&spellHaste);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetCooldownMultiplier_Public_Static_Single_Single_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(7)]
	[CachedScanResults(RefRangeStart = 435843, RefRangeEnd = 435850, XrefRangeStart = 435839, XrefRangeEnd = 435843, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateSpellCooldown(ObjectsCommon obj, float CooldownTimer)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(float**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &CooldownTimer;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateSpellCooldown_Public_Static_Single_ObjectsCommon_Single_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(6)]
	[CachedScanResults(RefRangeStart = 435918, RefRangeEnd = 435924, XrefRangeStart = 435850, XrefRangeEnd = 435918, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateAttackSpeed(ObjectsCommon obj, float baseAttackSpeed = 1f, Attributes attributes = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[3];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(float**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &baseAttackSpeed;
		*(System.IntPtr*)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateAttackSpeed_Public_Static_Single_ObjectsCommon_Single_Attributes_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 435941, RefRangeEnd = 435942, XrefRangeStart = 435924, XrefRangeEnd = 435941, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateSpeed(ObjectsCommon obj, Attributes attributes = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateSpeed_Public_Static_Single_ObjectsCommon_Attributes_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 435988, RefRangeEnd = 435989, XrefRangeStart = 435942, XrefRangeEnd = 435988, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculatePlayerSpeed(Player player, Attributes attributes = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculatePlayerSpeed_Public_Static_Single_Player_Attributes_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(0)]
	public unsafe static float SneakCheck(Player player, float speed)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		*(float**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &speed;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_SneakCheck_Private_Static_Single_Player_Single_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 435989, XrefRangeEnd = 436003, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateMonsterSpeed(ObjectsCommon obj)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateMonsterSpeed_Private_Static_Single_ObjectsCommon_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(6)]
	[CachedScanResults(RefRangeStart = 436050, RefRangeEnd = 436056, XrefRangeStart = 436003, XrefRangeEnd = 436050, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateEncumbrance(ObjectsCommon obj, Attributes attributes = null)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateEncumbrance_Public_Static_Single_ObjectsCommon_Attributes_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 436056, XrefRangeEnd = 436060, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static bool IsEncumbranceEnforced(Player player)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_IsEncumbranceEnforced_Public_Static_Boolean_Player_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(bool*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(6)]
	[CachedScanResults(RefRangeStart = 436075, RefRangeEnd = 436081, XrefRangeStart = 436060, XrefRangeEnd = 436075, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static bool ExceedsEncumbrance(Player player, float addedWeight = 0f, float capReduction = 0f)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[3];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		*(float**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &addedWeight;
		*(float**)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = &capReduction;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_ExceedsEncumbrance_Public_Static_Boolean_Player_Single_Single_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(bool*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(5)]
	[CachedScanResults(RefRangeStart = 436092, RefRangeEnd = 436097, XrefRangeStart = 436081, XrefRangeEnd = 436092, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float RemainingCarryCapacity(Player player)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_RemainingCarryCapacity_Public_Static_Single_Player_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 436097, XrefRangeEnd = 436101, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float EmpoweredHealthBonusPercent(MonsterConfiguration config)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(config);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_EmpoweredHealthBonusPercent_Public_Static_Single_MonsterConfiguration_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 436151, RefRangeEnd = 436152, XrefRangeStart = 436101, XrefRangeEnd = 436151, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int MonsterHPInflation(ObjectsCommon obj)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_MonsterHPInflation_Public_Static_Int32_ObjectsCommon_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 436191, RefRangeEnd = 436193, XrefRangeStart = 436152, XrefRangeEnd = 436191, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int MonsterStunBuildUpInflation(ObjectsCommon obj)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(obj);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_MonsterStunBuildUpInflation_Public_Static_Int32_ObjectsCommon_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 436214, RefRangeEnd = 436215, XrefRangeStart = 436193, XrefRangeEnd = 436214, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float GetMonsterDamageScaling(ObjectsCommon attacker)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(attacker);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetMonsterDamageScaling_Public_Static_Single_ObjectsCommon_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 436222, RefRangeEnd = 436223, XrefRangeStart = 436215, XrefRangeEnd = 436222, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int CalculateGoldDrop(List<EffectValues> effects, int baseAmount)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(effects);
		*(int**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &baseAmount;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateGoldDrop_Public_Static_Int32_List_1_EffectValues_Int32_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(3)]
	[CachedScanResults(RefRangeStart = 436289, RefRangeEnd = 436292, XrefRangeStart = 436223, XrefRangeEnd = 436289, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateHealthRegeneration(Attributes attributes, List<EffectValues> effects, ObjectsCommon obj, bool forStatDisplay = false)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[4];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(effects);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(bool**)((byte*)ptr + checked((nuint)3u * unchecked((nuint)sizeof(System.IntPtr)))) = &forStatDisplay;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateHealthRegeneration_Public_Static_Single_Attributes_List_1_EffectValues_ObjectsCommon_Boolean_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(4)]
	[CachedScanResults(RefRangeStart = 436346, RefRangeEnd = 436350, XrefRangeStart = 436292, XrefRangeEnd = 436346, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateConcentrationRegeneration(Attributes attributes, List<EffectValues> effects, ObjectsCommon obj, bool forStatDisplay = false)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[4];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(effects);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(obj);
		*(bool**)((byte*)ptr + checked((nuint)3u * unchecked((nuint)sizeof(System.IntPtr)))) = &forStatDisplay;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateConcentrationRegeneration_Public_Static_Single_Attributes_List_1_EffectValues_ObjectsCommon_Boolean_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(4)]
	[CachedScanResults(RefRangeStart = 436411, RefRangeEnd = 436415, XrefRangeStart = 436350, XrefRangeEnd = 436411, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateConcentrationRegenOnHit(Attributes attributes, List<EffectValues> effects, ObjectsCommon obj)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[3];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(effects);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(obj);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateConcentrationRegenOnHit_Public_Static_Single_Attributes_List_1_EffectValues_ObjectsCommon_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(4)]
	[CachedScanResults(RefRangeStart = 436473, RefRangeEnd = 436477, XrefRangeStart = 436415, XrefRangeEnd = 436473, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateStaminaRegeneration(Attributes attributes, List<EffectValues> effects, ObjectsCommon obj)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[3];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(attributes);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(effects);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(obj);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj2 = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateStaminaRegeneration_Public_Static_Single_Attributes_List_1_EffectValues_ObjectsCommon_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj2);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 436477, RefRangeEnd = 436479, XrefRangeStart = 436477, XrefRangeEnd = 436477, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static float CalculateSprintingCostPerSecond(Player player)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateSprintingCostPerSecond_Public_Static_Single_Player_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(float*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 436480, RefRangeEnd = 436481, XrefRangeStart = 436479, XrefRangeEnd = 436480, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int GetPercentageOfMax(int maxPercent, int size, float maxArraySize = 100f)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[3];
		*ptr = (nint)(&maxPercent);
		*(int**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &size;
		*(float**)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = &maxArraySize;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetPercentageOfMax_Public_Static_Int32_Int32_Int32_Single_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 436481, XrefRangeEnd = 436485, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static Sprite GetBarSprite(Il2CppReferenceArray<Sprite> sprites, float lowNum, float maxNum)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[3];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(sprites);
		*(float**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &lowNum;
		*(float**)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = &maxNum;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetBarSprite_Public_Static_Sprite_Il2CppReferenceArray_1_Sprite_Single_Single_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Sprite>(intPtr) : null;
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 436485, XrefRangeEnd = 436492, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static Sprite GetBarSprite(List<Sprite> sprites, int lowNum, int maxNum)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[3];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(sprites);
		*(int**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &lowNum;
		*(int**)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = &maxNum;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetBarSprite_Public_Static_Sprite_List_1_Sprite_Int32_Int32_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Sprite>(intPtr) : null;
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 436494, RefRangeEnd = 436495, XrefRangeStart = 436492, XrefRangeEnd = 436494, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static Sprite GetBarSpriteReverse(Il2CppReferenceArray<Sprite> sprites, int lowNum, int maxNum)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[3];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(sprites);
		*(int**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &lowNum;
		*(int**)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = &maxNum;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetBarSpriteReverse_Public_Static_Sprite_Il2CppReferenceArray_1_Sprite_Int32_Int32_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Sprite>(intPtr) : null;
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 436495, XrefRangeEnd = 436507, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static string GetArchetypeName(Race race, Class @class)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = (nint)(&race);
		*(Class**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &@class;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr il2CppString = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetArchetypeName_Public_Static_String_Race_Class_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return IL2CPP.Il2CppStringToManaged(il2CppString);
	}

	[CallerCount(7)]
	[CachedScanResults(RefRangeStart = 436555, RefRangeEnd = 436562, XrefRangeStart = 436507, XrefRangeEnd = 436555, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static string GetLocalizedArchetypeName(Race race, Class @class)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = (nint)(&race);
		*(Class**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &@class;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr il2CppString = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetLocalizedArchetypeName_Public_Static_String_Race_Class_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return IL2CPP.Il2CppStringToManaged(il2CppString);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 436593, RefRangeEnd = 436594, XrefRangeStart = 436562, XrefRangeEnd = 436593, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static string GetLocalizedRaceName(Race race)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = (nint)(&race);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr il2CppString = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetLocalizedRaceName_Public_Static_String_Race_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return IL2CPP.Il2CppStringToManaged(il2CppString);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 436625, RefRangeEnd = 436626, XrefRangeStart = 436594, XrefRangeEnd = 436625, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static string GetLocalizedClassName(Class @class)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = (nint)(&@class);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr il2CppString = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetLocalizedClassName_Public_Static_String_Class_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return IL2CPP.Il2CppStringToManaged(il2CppString);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 436638, RefRangeEnd = 436639, XrefRangeStart = 436626, XrefRangeEnd = 436638, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static Attributes GetRaceAttributesBonus(Race race)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = (nint)(&race);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetRaceAttributesBonus_Public_Static_Attributes_Race_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Attributes>(intPtr) : null;
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 436651, RefRangeEnd = 436652, XrefRangeStart = 436639, XrefRangeEnd = 436651, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static Attributes GetClassAttributesBonus(Class @class)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = (nint)(&@class);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetClassAttributesBonus_Public_Static_Attributes_Class_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Attributes>(intPtr) : null;
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 436652, XrefRangeEnd = 436659, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int GetRaceBonusByAttribute(Race race, Attribute attribute)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = (nint)(&race);
		*(Attribute**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &attribute;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetRaceBonusByAttribute_Public_Static_Int32_Race_Attribute_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 436659, XrefRangeEnd = 436666, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int GetClassBonusByStat(Class @class, Attribute attribute)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = (nint)(&@class);
		*(Attribute**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &attribute;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetClassBonusByStat_Public_Static_Int32_Class_Attribute_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(3573)]
	[CachedScanResults(RefRangeStart = 24693, RefRangeEnd = 28266, XrefRangeStart = 24693, XrefRangeEnd = 28266, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe Formulas()
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<Formulas>.NativeClassPtr))
	{
		System.IntPtr* param = null;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull(this), (void**)param, ref exc);
		Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	public static void AddSkillTreeBonuses(ObjectsCommon obj, ref float runeFlatBonus, ref float runePercentBonus, params Runes.Stat[] stats)
	{
		AddSkillTreeBonuses(obj, ref runeFlatBonus, ref runePercentBonus, new Il2CppStructArray<Runes.Stat>(stats));
	}

	public Formulas(System.IntPtr pointer)
		: base(pointer)
	{
	}
}
