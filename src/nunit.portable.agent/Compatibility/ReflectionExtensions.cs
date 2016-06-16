// ***********************************************************************
// Copyright (c) 2015 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace NUnit.Compatibility
{
    /// <summary>
    /// Provides NUnit specific extensions to aid in Reflection
    /// across multiple frameworks
    /// </summary>
    /// <remarks>
    /// This version of the class allows direct calls on Type on
    /// those platforms that would normally require use of
    /// GetTypeInfo().
    /// </remarks>
    public static class TypeExtensions
    {
        static IList<MemberInfo> GetAllMembers(this Type type)
        {
            List<MemberInfo> members = type.GetTypeInfo().DeclaredMembers.ToList();
            type = type.GetTypeInfo().BaseType;
            if (type != null)
            {
                var baseMembers = type.GetAllMembers();
                members.AddRange(baseMembers.Where(NotPrivate));
            }
            return members;
        }

        static bool NotPrivate(MemberInfo info)
        {
            var pinfo = info as PropertyInfo;
            if (pinfo != null)
                return pinfo.GetMethod.IsPrivate == false;

            var finfo = info as FieldInfo;
            if (finfo != null)
                return finfo.IsPrivate == false;

            var minfo = info as MethodBase;
            if (minfo != null)
                return minfo.IsPrivate == false;

            var einfo = info as EventInfo;
            if (einfo != null)
                return einfo.RaiseMethod.IsPrivate == false;

            return true;
        }

        static IList<MethodInfo> GetAllMethods(this Type type, bool includeBaseStatic = false)
        {
            List<MethodInfo> methods = type.GetTypeInfo().DeclaredMethods.ToList();
            type = type.GetTypeInfo().BaseType;
            if (type != null)
            {
                var baseMethods = type.GetAllMethods(includeBaseStatic)
                    .Where(b => b.IsPublic && (includeBaseStatic || !b.IsStatic) && !methods.Any(m => m.GetRuntimeBaseDefinition() == b));
                methods.AddRange(baseMethods);
            }

            return methods;
        }

        static IEnumerable<PropertyInfo> ApplyBindingFlags(this IEnumerable<PropertyInfo> infos, BindingFlags flags)
        {
            bool pub = flags.HasFlag(BindingFlags.Public);
            bool priv = flags.HasFlag(BindingFlags.NonPublic);
            if (pub && !priv)
                infos = infos.Where(p => (p.GetMethod != null && p.GetMethod.IsPublic) || (p.SetMethod != null && p.SetMethod.IsPublic));
            if (priv && !pub)
                infos = infos.Where(p => (p.GetMethod == null || p.GetMethod.IsPrivate) && (p.SetMethod == null || p.SetMethod.IsPrivate));

            bool stat = flags.HasFlag(BindingFlags.Static);
            bool inst = flags.HasFlag(BindingFlags.Instance);
            if (stat && !inst)
                infos = infos.Where(p => (p.GetMethod != null && p.GetMethod.IsStatic) || (p.SetMethod != null && p.SetMethod.IsStatic));
            else if (inst && !stat)
                infos = infos.Where(p => (p.GetMethod != null && !p.GetMethod.IsStatic) || (p.SetMethod != null && !p.SetMethod.IsStatic));

            return infos;
        }

        static IEnumerable<MethodInfo> ApplyBindingFlags(this IEnumerable<MethodInfo> infos, BindingFlags flags)
        {
            bool pub = flags.HasFlag(BindingFlags.Public);
            bool priv = flags.HasFlag(BindingFlags.NonPublic);
            if (priv && !pub)
                infos = infos.Where(m => m.IsPrivate);
            else if (pub && !priv)
                infos = infos.Where(m => m.IsPublic);

            bool stat = flags.HasFlag(BindingFlags.Static);
            bool inst = flags.HasFlag(BindingFlags.Instance);
            if (stat && !inst)
                infos = infos.Where(m => m.IsStatic);
            else if (inst && !stat)
                infos = infos.Where(m => !m.IsStatic);

            return infos;
        }
    }

    /// <summary>
    /// Extensions to the various MemberInfo derived classes
    /// </summary>
    public static class MemberInfoExtensions
    {
        /// <summary>
        /// Returns an array of custom attributes of the specified type applied to this member
        /// </summary>
        /// <remarks> Portable throws an argument exception if T does not
        /// derive from Attribute. NUnit uses interfaces to find attributes, thus
        /// this method</remarks>
        public static IEnumerable<T> GetAttributes<T>(this MemberInfo info, bool inherit) where T : class
        {
            return GetAttributesImpl<T>(info.GetCustomAttributes(inherit));
        }

        /// <summary>
        /// Returns an array of custom attributes of the specified type applied to this parameter
        /// </summary>
        public static IEnumerable<T> GetAttributes<T>(this ParameterInfo info, bool inherit) where T : class
        {
            return GetAttributesImpl<T>(info.GetCustomAttributes(inherit));
        }

        /// <summary>
        /// Returns an array of custom attributes of the specified type applied to this assembly
        /// </summary>
        public static IEnumerable<T> GetAttributes<T>(this Assembly asm) where T : class
        {
            return GetAttributesImpl<T>(asm.GetCustomAttributes());
        }

        private static IEnumerable<T> GetAttributesImpl<T>(IEnumerable<Attribute> attributes) where T : class
        {
            var attrs = new List<T>();

            attributes.Where(a => typeof(T).IsAssignableFrom(a.GetType()))
                .All(a => { attrs.Add(a as T); return true; });

            return attrs;
        }
    }

    /// <summary>
    /// Extensions for Assembly that are not available in portable
    /// </summary>
    public static class AssemblyExtensions
    {
        /// <summary>
        /// DNX does not have a version of GetCustomAttributes on Assembly that takes an inherit
        /// parameter since it doesn't make sense on Assemblies. This version just ignores the
        /// inherit parameter.
        /// </summary>
        /// <param name="asm">The assembly</param>
        /// <param name="attributeType">The type of attribute you are looking for</param>
        /// <param name="inherit">Ignored</param>
        /// <returns></returns>
        public static object[] GetCustomAttributes(this Assembly asm, Type attributeType, bool inherit)
        {
            return asm.GetCustomAttributes(attributeType).ToArray();
        }
    }

    /// <summary>
    /// Type extensions that apply to all target frameworks
    /// </summary>
    public static class AdditionalTypeExtensions
    {
        /// <summary>
        /// Determines if the given <see cref="Type"/> array is castable/matches the <see cref="ParameterInfo"/> array.
        /// </summary>
        /// <param name="pinfos"></param>
        /// <param name="ptypes"></param>
        /// <returns></returns>
        public static bool ParametersMatch(this ParameterInfo[] pinfos, Type[] ptypes)
        {
            if (pinfos.Length != ptypes.Length)
                return false;

            for (int i = 0; i < pinfos.Length; i++)
            {
                if (!pinfos[i].ParameterType.IsCastableFrom(ptypes[i]))
                    return false;
            }
            return true;
        }

        // §6.1.2 (Implicit numeric conversions) of the specification
        static Dictionary<Type, List<Type>> convertibleValueTypes = new Dictionary<Type, List<Type>>() {
            { typeof(decimal), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char) } },
            { typeof(double), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(float) } },
            { typeof(float), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(float) } },
            { typeof(ulong), new List<Type> { typeof(byte), typeof(ushort), typeof(uint), typeof(char) } },
            { typeof(long), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(char) } },
            { typeof(uint), new List<Type> { typeof(byte), typeof(ushort), typeof(char) } },
            { typeof(int), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(char) } },
            { typeof(ushort), new List<Type> { typeof(byte), typeof(char) } },
            { typeof(short), new List<Type> { typeof(byte) } }
        };

        /// <summary>
        /// Determines if one type can be implicitly converted from another
        /// </summary>
        /// <param name="to"></param>
        /// <param name="from"></param>
        /// <returns></returns>
        public static bool IsCastableFrom(this Type to, Type from)
        {
            if (to.IsAssignableFrom(from))
                return true;

            // Look for the marker that indicates from was null
            if (from == typeof(NUnitNullType) && (to.GetTypeInfo().IsClass || to.FullName.StartsWith("System.Nullable")))
                return true;

            if (convertibleValueTypes.ContainsKey(to) && convertibleValueTypes[to].Contains(from))
                return true;

            return from.GetMethods(BindingFlags.Public | BindingFlags.Static)
                       .Any(m => m.ReturnType == to && m.Name == "op_Implicit");
        }
    }

    /// <summary>
    /// This class is used as a flag when we get a parameter list for a method/constructor, but
    /// we do not know one of the types because null was passed in.
    /// </summary>
    public class NUnitNullType
    {
    }
}
