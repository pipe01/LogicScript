using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace LogicScript.Compiling
{
    /// <summary>
    /// Sigil accesses the GetParameters() method and ReturnParameter property when emitting a Call to a method.
    /// These members throw an exception when called on a MethodBuilder before the containing type has been finalized
    /// and created, which we need to do when compiling code that calls functions it defines.
    /// 
    /// However, since we do know the parameters of the method ahead of time (they all receive and return ulong's),
    /// we can create a proxy type that overrides these two members to return the information we know while everything
    /// else is delegated through to the original MethodInfo (in our case MethodBuilder).
    /// </summary>
    internal sealed class MethodInfoProxy(MethodInfo inner, Type returnType, Type[] parameters) : MethodInfo
    {
        private class SimpleParameterInfo(Type type) : ParameterInfo
        {
            public override Type ParameterType => type;
        }

        public override ParameterInfo[] GetParameters() => [.. parameters.Select(t => new SimpleParameterInfo(t))];
        public override ParameterInfo ReturnParameter => new SimpleParameterInfo(returnType);

        // Everything below this line is proxied directly to `inner`
        // =========================================================

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => inner.ReturnTypeCustomAttributes;

        public override MethodAttributes Attributes => inner.Attributes;

        public override RuntimeMethodHandle MethodHandle => inner.MethodHandle;

        public override Type DeclaringType => inner.DeclaringType;

        public override string Name => inner.Name;

        public override Type ReflectedType => inner.ReflectedType;

        public override CallingConventions CallingConvention => inner.CallingConvention;

        public override bool ContainsGenericParameters => inner.ContainsGenericParameters;

        public override Delegate CreateDelegate(Type delegateType) => inner.CreateDelegate(delegateType);

        public override Delegate CreateDelegate(Type delegateType, object target) => inner.CreateDelegate(delegateType, target);

        public override IEnumerable<CustomAttributeData> CustomAttributes => inner.CustomAttributes;

        public override bool Equals(object obj) => inner.Equals(obj);

        public override IList<CustomAttributeData> GetCustomAttributesData() => inner.GetCustomAttributesData();

        public override Type[] GetGenericArguments() => inner.GetGenericArguments();

        public override MethodInfo GetGenericMethodDefinition() => inner.GetGenericMethodDefinition();

        public override int GetHashCode() => inner.GetHashCode();

        public override MethodBody GetMethodBody() => inner.GetMethodBody();

        public override bool HasSameMetadataDefinitionAs(MemberInfo other) => inner.HasSameMetadataDefinitionAs(other);

        public override bool IsConstructedGenericMethod => inner.IsConstructedGenericMethod;

        public override bool IsGenericMethod => inner.IsGenericMethod;

        public override bool IsGenericMethodDefinition => inner.IsGenericMethodDefinition;

        public override bool IsSecurityCritical => inner.IsSecurityCritical;

        public override bool IsSecuritySafeCritical => inner.IsSecuritySafeCritical;

        public override bool IsSecurityTransparent => inner.IsSecurityTransparent;

        public override MethodInfo MakeGenericMethod(params Type[] typeArguments) => inner.MakeGenericMethod(typeArguments);

        public override MemberTypes MemberType => inner.MemberType;

        public override int MetadataToken => inner.MetadataToken;

        public override MethodImplAttributes MethodImplementationFlags => inner.MethodImplementationFlags;

        public override Module Module => inner.Module;

        public override Type ReturnType => inner.ReturnType;

        public override string ToString() => inner.ToString();

        public override MethodInfo GetBaseDefinition() => inner.GetBaseDefinition();

        public override object[] GetCustomAttributes(bool inherit) => inner.GetCustomAttributes(inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => inner.GetCustomAttributes(attributeType, inherit);

        public override MethodImplAttributes GetMethodImplementationFlags() => inner.GetMethodImplementationFlags();

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => inner.Invoke(obj, invokeAttr, binder, parameters, culture);

        public override bool IsDefined(Type attributeType, bool inherit) => inner.IsDefined(attributeType, inherit);
    }
}