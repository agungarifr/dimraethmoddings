using System;
using Il2CppInterop.Runtime;
using Il2CppSystem;

[System.Serializable]
public sealed class WorldCreationState : Il2CppSystem.ValueType
{
	private static readonly System.IntPtr NativeFieldInfoPtr_DifficultyIndex;

	private static readonly System.IntPtr NativeFieldInfoPtr_NameValid;

	private static readonly System.IntPtr NativeFieldInfoPtr_NameError;

	private static readonly System.IntPtr NativeFieldInfoPtr_ConfirmationVisible;

	private static readonly System.IntPtr NativeFieldInfoPtr_NewCharactersOnlyBlocked;

	public unsafe int DifficultyIndex
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_DifficultyIndex);
			return *(int*)num;
		}
		set
		{
			*(int*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_DifficultyIndex)) = num;
		}
	}

	public unsafe bool NameValid
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NameValid);
			return *(bool*)num;
		}
		set
		{
			*(bool*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NameValid)) = flag;
		}
	}

	public unsafe string NameError
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NameError);
			return IL2CPP.Il2CppStringToManaged(*(System.IntPtr*)num);
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NameError), IL2CPP.ManagedStringToIl2Cpp(str));
		}
	}

	public unsafe bool ConfirmationVisible
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_ConfirmationVisible);
			return *(bool*)num;
		}
		set
		{
			*(bool*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_ConfirmationVisible)) = flag;
		}
	}

	public unsafe bool NewCharactersOnlyBlocked
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NewCharactersOnlyBlocked);
			return *(bool*)num;
		}
		set
		{
			*(bool*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull(this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_NewCharactersOnlyBlocked)) = flag;
		}
	}

	static WorldCreationState()
	{
		Il2CppClassPointerStore<WorldCreationState>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "WorldCreationState");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<WorldCreationState>.NativeClassPtr);
		NativeFieldInfoPtr_DifficultyIndex = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationState>.NativeClassPtr, "DifficultyIndex");
		NativeFieldInfoPtr_NameValid = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationState>.NativeClassPtr, "NameValid");
		NativeFieldInfoPtr_NameError = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationState>.NativeClassPtr, "NameError");
		NativeFieldInfoPtr_ConfirmationVisible = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationState>.NativeClassPtr, "ConfirmationVisible");
		NativeFieldInfoPtr_NewCharactersOnlyBlocked = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationState>.NativeClassPtr, "NewCharactersOnlyBlocked");
	}

	public WorldCreationState(System.IntPtr pointer)
		: base(pointer)
	{
	}

	public WorldCreationState()
		: base(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<WorldCreationState>.NativeClassPtr))
	{
	}
}
