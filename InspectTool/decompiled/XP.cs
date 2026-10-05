using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppSystem;

public static class XP : Il2CppSystem.Object
{
	private static readonly System.IntPtr NativeMethodInfoPtr_GrantXP_Public_Static_Int32_Damage_Int32_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_IsXPGainBlocked_Public_Static_Boolean_Player_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateXPGained_Public_Static_Int32_Damage_Int32_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculatePartyXP_Private_Static_Int32_Int32_Int32_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_TriggerXPPopUp_Public_Static_Void_Player_Int32_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GrantXPToPlayer_Public_Static_Void_Player_Int32_String_Boolean_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_AddXPToPlayer_Private_Static_Void_Player_Int32_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_AddXPToPet_Private_Static_Void_Player_Int32_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GrantPetXPOwnerSide_Public_Static_Void_Player_Int32_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculatePetXPGain_Private_Static_Int32_LocalPetData_Player_Int32_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_CalculateBonusXP_Public_Static_Int32_Player_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_GetXPFromMonster_Public_Static_Int32_MonsterConfiguration_0;

	static XP()
	{
		Il2CppClassPointerStore<XP>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "XP");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<XP>.NativeClassPtr);
		NativeMethodInfoPtr_GrantXP_Public_Static_Int32_Damage_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<XP>.NativeClassPtr, 100692205);
		NativeMethodInfoPtr_IsXPGainBlocked_Public_Static_Boolean_Player_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<XP>.NativeClassPtr, 100692206);
		NativeMethodInfoPtr_CalculateXPGained_Public_Static_Int32_Damage_Int32_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<XP>.NativeClassPtr, 100692207);
		NativeMethodInfoPtr_CalculatePartyXP_Private_Static_Int32_Int32_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<XP>.NativeClassPtr, 100692208);
		NativeMethodInfoPtr_TriggerXPPopUp_Public_Static_Void_Player_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<XP>.NativeClassPtr, 100692209);
		NativeMethodInfoPtr_GrantXPToPlayer_Public_Static_Void_Player_Int32_String_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<XP>.NativeClassPtr, 100692210);
		NativeMethodInfoPtr_AddXPToPlayer_Private_Static_Void_Player_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<XP>.NativeClassPtr, 100692211);
		NativeMethodInfoPtr_AddXPToPet_Private_Static_Void_Player_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<XP>.NativeClassPtr, 100692212);
		NativeMethodInfoPtr_GrantPetXPOwnerSide_Public_Static_Void_Player_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<XP>.NativeClassPtr, 100692213);
		NativeMethodInfoPtr_CalculatePetXPGain_Private_Static_Int32_LocalPetData_Player_Int32_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<XP>.NativeClassPtr, 100692214);
		NativeMethodInfoPtr_CalculateBonusXP_Public_Static_Int32_Player_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<XP>.NativeClassPtr, 100692215);
		NativeMethodInfoPtr_GetXPFromMonster_Public_Static_Int32_MonsterConfiguration_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<XP>.NativeClassPtr, 100692216);
	}

	[CallerCount(2)]
	[CachedScanResults(RefRangeStart = 474849, RefRangeEnd = 474851, XrefRangeStart = 474820, XrefRangeEnd = 474849, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int GrantXP(Damage damage, int partySplit = 1)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(damage);
		*(int**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &partySplit;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GrantXP_Public_Static_Int32_Damage_Int32_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(3)]
	[CachedScanResults(RefRangeStart = 474860, RefRangeEnd = 474863, XrefRangeStart = 474851, XrefRangeEnd = 474860, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static bool IsXPGainBlocked(Player player)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_IsXPGainBlocked_Public_Static_Boolean_Player_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(bool*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(4)]
	[CachedScanResults(RefRangeStart = 474921, RefRangeEnd = 474925, XrefRangeStart = 474863, XrefRangeEnd = 474921, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int CalculateXPGained(Damage damage, int partySplit = 1, bool raveEquipped = false)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[3];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(damage);
		*(int**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &partySplit;
		*(bool**)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = &raveEquipped;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateXPGained_Public_Static_Int32_Damage_Int32_Boolean_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 474925, XrefRangeEnd = 474936, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int CalculatePartyXP(int xp, int partySplit)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = (nint)(&xp);
		*(int**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &partySplit;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculatePartyXP_Private_Static_Int32_Int32_Int32_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 474945, RefRangeEnd = 474946, XrefRangeStart = 474936, XrefRangeEnd = 474945, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static void TriggerXPPopUp(Player player, int xp)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		*(int**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &xp;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_TriggerXPPopUp_Public_Static_Void_Player_Int32_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(6)]
	[CachedScanResults(RefRangeStart = 474974, RefRangeEnd = 474980, XrefRangeStart = 474946, XrefRangeEnd = 474974, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static void GrantXPToPlayer(Player player, int xp, string source = "Unknown", bool showPopup = true)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[4];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		*(int**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &xp;
		*(System.IntPtr*)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.ManagedStringToIl2Cpp(source);
		*(bool**)((byte*)ptr + checked((nuint)3u * unchecked((nuint)sizeof(System.IntPtr)))) = &showPopup;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GrantXPToPlayer_Public_Static_Void_Player_Int32_String_Boolean_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 475010, RefRangeEnd = 475011, XrefRangeStart = 474980, XrefRangeEnd = 475010, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static void AddXPToPlayer(Player player, int xp)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		*(int**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &xp;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_AddXPToPlayer_Private_Static_Void_Player_Int32_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 475011, XrefRangeEnd = 475026, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static void AddXPToPet(Player player, int xp)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		*(int**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &xp;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_AddXPToPet_Private_Static_Void_Player_Int32_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 475045, RefRangeEnd = 475046, XrefRangeStart = 475026, XrefRangeEnd = 475045, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static void GrantPetXPOwnerSide(Player player, int xp)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[2];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		*(int**)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = &xp;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GrantPetXPOwnerSide_Public_Static_Void_Player_Int32_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppException.RaiseExceptionIfNecessary(exc);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 475071, RefRangeEnd = 475072, XrefRangeStart = 475046, XrefRangeEnd = 475071, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int CalculatePetXPGain(LocalPetData petData, Player player, int xp)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[3];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(petData);
		*(System.IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(System.IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr(player);
		*(int**)((byte*)ptr + checked((nuint)2u * unchecked((nuint)sizeof(System.IntPtr)))) = &xp;
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculatePetXPGain_Private_Static_Int32_LocalPetData_Player_Int32_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 475090, RefRangeEnd = 475091, XrefRangeStart = 475072, XrefRangeEnd = 475090, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int CalculateBonusXP(Player player)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(player);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_CalculateBonusXP_Public_Static_Int32_Player_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 475091, XrefRangeEnd = 475095, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int GetXPFromMonster(MonsterConfiguration monster)
	{
		System.IntPtr* ptr = stackalloc System.IntPtr[1];
		*ptr = IL2CPP.Il2CppObjectBaseToPtr(monster);
		Unsafe.SkipInit(out System.IntPtr exc);
		System.IntPtr obj = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetXPFromMonster_Public_Static_Int32_MonsterConfiguration_0, (System.IntPtr)0, (void**)ptr, ref exc);
		Il2CppException.RaiseExceptionIfNecessary(exc);
		return *(int*)IL2CPP.il2cpp_object_unbox(obj);
	}

	public XP(System.IntPtr pointer)
		: base(pointer)
	{
	}
}
