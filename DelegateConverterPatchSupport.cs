using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Il2CppException = Il2CppInterop.Runtime.Il2CppException;

namespace HousePartyPlugin
{
    public static class DelegateConverterPatchSupport
    {
        /// <summary>
        /// only call on Il2CppSystem.Type
        /// </summary>
        /// <param name="type">Il2CppSystem.Type object</param>
        /// <param name="predicate">Methodinfo where it searches for one with the same name and arguments</param>
        /// <returns></returns>
        public static unsafe object GetMethodFix(this object type, MethodInfo predicate)
        {
            if (type.GetType().ToString() != DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.Type")!.ToString())
            {
                throw new ArgumentException("Type mismatch in GetMethodFix extension method");
            }
            var pointerGetter = DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.Il2CppObjectBase")!.GetMethod("get_Pointer", BindingFlags.Instance | BindingFlags.NonPublic)!;
            IntPtr pointer = (IntPtr)pointerGetter.Invoke(type, null)!;
            var nativeTypeClass = IL2CPP.il2cpp_object_get_class(pointer);
            var nativeGetMethod = IL2CPP.il2cpp_class_get_method_from_name(nativeTypeClass, "GetMethod", 2);
            var exception = IntPtr.Zero;
            int flags = (int)(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

            var parameters = stackalloc IntPtr[2];
            *parameters = IL2CPP.ManagedStringToIl2Cpp(predicate.Name);
            *(parameters + sizeof(IntPtr)) = (IntPtr)(&flags);
            var nativeMethodInfoObject = IL2CPP.il2cpp_runtime_invoke(nativeGetMethod, pointer, (void**)parameters, ref exception);
            Il2CppException.RaiseExceptionIfNecessary(exception);
            if (nativeMethodInfoObject != IntPtr.Zero)
            {
                var methodInfo = DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.Reflection.MethodInfo")!;
                var objectPoolGet = typeof(Il2CppObjectPool).GetMethod(nameof(Il2CppObjectPool.Get))!;
                var objectPoolGetmethodInfo = objectPoolGet.MakeGenericMethod(new[] { methodInfo });
                return objectPoolGetmethodInfo.Invoke(null, new object[] { nativeMethodInfoObject })!;
            }
            else
                throw new MissingMethodException("method was not found");
        }

        /// <summary>
        /// only call on Il2CppSystem.Type
        /// </summary>
        /// <param name="type">Il2CppSystem.Type object</param>
        /// <returns></returns>
        public static unsafe string FullNameFix(this object type)
        {
            if (type.GetType().ToString() != DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.Type")!.ToString())
            {
                throw new ArgumentException("Type mismatch in FullNameFix extension method");
            }
            var get__impl = DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.Type")!.GetMethod("get__impl", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var value = DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.RuntimeTypeHandle")!.GetField("value")!;
            return Marshal.PtrToStringAnsi(IL2CPP.il2cpp_type_get_name((IntPtr)value.GetValue(get__impl.Invoke(type, null)!)!))!;
        }
    }
}