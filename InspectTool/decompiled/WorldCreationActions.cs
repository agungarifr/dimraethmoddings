using System;
using Il2CppInterop.Runtime;
using Il2CppSystem;

public static class WorldCreationActions : Il2CppSystem.Object
{
	private static readonly System.IntPtr NativeFieldInfoPtr_EditWorldName;

	public unsafe static UIActionId EditWorldName
	{
		get
		{
			System.IntPtr intPtr = (nint)stackalloc byte[(int)(uint)IL2CPP.il2cpp_class_value_size(Il2CppClassPointerStore<UIActionId>.NativeClassPtr, ref *(uint*)null)];
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_EditWorldName, (void*)intPtr);
			return new UIActionId(IL2CPP.il2cpp_value_box(Il2CppClassPointerStore<UIActionId>.NativeClassPtr, intPtr));
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_EditWorldName, (void*)IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull(obj)));
		}
	}

	static WorldCreationActions()
	{
		Il2CppClassPointerStore<WorldCreationActions>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "WorldCreationActions");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<WorldCreationActions>.NativeClassPtr);
		NativeFieldInfoPtr_EditWorldName = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WorldCreationActions>.NativeClassPtr, "EditWorldName");
	}

	public WorldCreationActions(System.IntPtr pointer)
		: base(pointer)
	{
	}
}
