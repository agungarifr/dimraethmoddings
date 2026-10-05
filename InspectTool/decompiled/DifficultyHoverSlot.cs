using System;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem;
using UnityEngine;

[System.Serializable]
public sealed class DifficultyHoverSlot : Il2CppSystem.ValueType
{
	private static readonly System.IntPtr NativeFieldInfoPtr_Difficulty;

	private static readonly System.IntPtr NativeFieldInfoPtr_Sprite;

	public unsafe Difficulty Difficulty
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Difficulty);
			return *(Difficulty*)num;
		}
		set
		{
			*(Difficulty*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Difficulty)) = difficulty;
		}
	}

	public unsafe Sprite Sprite
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Sprite);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<Sprite>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_Sprite), IL2CPP.Il2CppObjectBaseToPtr(obj));
		}
	}

	static DifficultyHoverSlot()
	{
		Il2CppClassPointerStore<DifficultyHoverSlot>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "DifficultyHoverSlot");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<DifficultyHoverSlot>.NativeClassPtr);
		NativeFieldInfoPtr_Difficulty = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyHoverSlot>.NativeClassPtr, "Difficulty");
		NativeFieldInfoPtr_Sprite = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DifficultyHoverSlot>.NativeClassPtr, "Sprite");
	}

	public DifficultyHoverSlot(System.IntPtr pointer)
		: base(pointer)
	{
	}

	public DifficultyHoverSlot()
		: base(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<DifficultyHoverSlot>.NativeClassPtr))
	{
	}
}
