﻿using ConsoleAppFramework;
using System.Reflection;
using System.Text;
using ZLinq;

namespace FileGen;

public class DropinGen
{
    [Flags]
    enum DropInGenerateTypes
    {
        None = 0,
        Array = 1,
        Span = 2, // Span + ReadOnlySpan
        Memory = 4, // Memory + ReadOnlyMemory
        List = 8,
        Enumerable = 16,
        Collection = Array | Span | Memory | List,
        Everything = Array | Span | Memory | List | Enumerable
    }

    record struct DropInType(string Name, string Replacement, bool IsArray = false);

    [Command("dropin")]
    public void GenerateDropInSource()
    {
        var dropinTypes = new DropInType[]
        {
            new("Array", "FromArray", IsArray:true),
            new("Span", "FromSpan"),
            new("ReadOnlySpan", "FromSpan"),
            new("Memory", "FromMemory"),
            new("ReadOnlyMemory", "FromMemory"),
            new("List", "FromList"),
            new("IEnumerable", "FromEnumerable"),
        };

        foreach (var dropinType in dropinTypes)
        {
            var sb = new StringBuilder();
            sb.AppendLine("""
// <auto-generated />
#pragma warning disable
#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
#if NET8_0_OR_GREATER
using System.Numerics;
#endif
using ZLinq;
using ZLinq.Linq;

internal static partial class ZLinqDropInExtensions
{
""");

            var methodInfos = typeof(ZLinq.ValueEnumerableExtensions).GetMethods();
            foreach (var methodInfo in methodInfos)
            {
                var signature = BuildSignature(methodInfo, dropinType);
                if (signature != null)
                {
                    sb.AppendLine(signature);
                }
            }

            sb.AppendLine("}");

            var code = sb.ToString();

            Console.WriteLine("Generate: " + dropinType.Name);
            File.WriteAllText("DropIn/" + dropinType.Name + ".cs", code);
        }
    }

    string? BuildSignature(MethodInfo methodInfo, DropInType dropInType)
    {
        if (methodInfo.Name is "GetType" or "ToString" or "Equals" or "GetHashCode" or "GetEnumerator")
        {
            return null;
        }

        if (methodInfo.Name.StartsWith("ThenBy"))
        {
            return null;
        }

        if (methodInfo.Name is "Where" && methodInfo.ReturnType.GetGenericArguments().Any(x => x.Name.Contains("SelectWhere")))
        {
            return null;
        }

        if (methodInfo.Name is "Select" && methodInfo.ReturnType.GetGenericArguments().Any(x => x.Name.Contains("WhereSelect")))
        {
            return null;
        }

        // debugging stop condition
        // if (methodInfo.Name is not "Select") continue;

        var returnType = BuildType(methodInfo, methodInfo.ReturnType, dropInType.Replacement) + IsNullableReturnParameter(methodInfo);
        var name = methodInfo.Name;
        var genericsTypes = string.Join(", ", methodInfo.GetGenericArguments().Skip(1).Select(x => x.Name).ToArray());
        var parameters = string.Join(", ", methodInfo.GetParameters().Skip(1).Select(x => $"{BuildType(methodInfo, x.ParameterType, dropInType.Replacement)} {x.Name}").ToArray());
        if (parameters != "") parameters = $", {parameters}";
        var parameterNames = string.Join(", ", methodInfo.GetParameters().Skip(1).Select(x => x.Name).ToArray());
        var sourceType = BuildSourceType(methodInfo, dropInType.Name, dropInType.IsArray);
        var constraints = BuildConstraints(methodInfo);

        var signature = $"public static {returnType} {name}<{genericsTypes}>(this {sourceType} source{parameters}){constraints} => source.AsValueEnumerable().{name}({parameterNames});";

        // quick fix
        if (signature.Contains("RightJoin"))
        {
            signature = signature.Replace("Func<TOuter, TInner, TResult> resultSelector", "Func<TOuter?, TInner, TResult> resultSelector");
        }
        else if (signature.Contains("LeftJoin"))
        {
            signature = signature.Replace("Func<TOuter, TInner, TResult> resultSelector", "Func<TOuter, TInner?, TResult> resultSelector");
        }

        return signature;
    }

    string IsNullableReturnParameter(MethodInfo methodInfo)
    {
        if (methodInfo.Name.EndsWith("OrDefault"))
        {
            // OrDefault and non defaultValue is nullable
            if (!methodInfo.GetParameters().Any(x => x.Name == "defaultValue"))
            {
                return "?";
            }
        }
        if (methodInfo.Name is "Max" or "MaxBy" or "Min" or "MinBy")
        {
            return "?";
        }

        return "";
    }

    string BuildType(MethodInfo methodInfo, Type type, string replacement)
    {
        var sourceGenericTypeName = methodInfo.GetGenericArguments().First(x => !x.Name.Contains("Enumerator")).Name;
        replacement = $"{replacement}<{sourceGenericTypeName}>";

        var sb = new StringBuilder();
        BuildTypeCore(sb, type, replacement);
        return sb.ToString();
    }

    void BuildTypeCore(StringBuilder builder, Type type, string replacement)
    {
        if (!type.IsGenericType)
        {
            if (type.Name is "TEnumerator" or "TEnumerator1")
            {
                builder.Append(replacement);
            }
            else if (type.Name == "Void")
            {
                builder.Append("void");
            }
            else
            {
                builder.Append(type.Name);
            }
            return;
        }

        builder.Append(type.Name, 0, type.Name.Length - 2); // `9 generic types
        builder.Append("<");

        var isFirst = true;
        foreach (var item in type.GenericTypeArguments)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                builder.Append(", ");
            }

            BuildTypeCore(builder, item, replacement);
        }
        builder.Append(">");
    }

    string BuildSourceType(MethodInfo methodInfo, string replacement, bool isArray)
    {
        var sourceGenericTypeName = methodInfo.GetGenericArguments().First(x => !x.Name.Contains("Enumerator")).Name;

        if (methodInfo.Name is "Average" && methodInfo.ReturnType.Name.StartsWith("Nullable"))
        {
            sourceGenericTypeName = "Nullable<" + sourceGenericTypeName + ">";
        }
        else if (methodInfo.Name is "Sum" && methodInfo.ToString()!.Contains("Nullable"))
        {
            sourceGenericTypeName = "Nullable<" + sourceGenericTypeName + ">";
        }
        else if (methodInfo.Name is "ToDictionary" && methodInfo.ToString()!.Contains("KeyValuePair"))
        {
            sourceGenericTypeName = "KeyValuePair<TKey, TValue>";
        }

        if (isArray)
        {
            return sourceGenericTypeName + "[]";
        }
        return $"{replacement}<{sourceGenericTypeName}>";
    }

    string BuildConstraints(MethodInfo methodInfo)
    {
        if (methodInfo.Name is "AggregateBy" or "CountBy" or "ToDictionary")
        {
            return " where TKey : notnull";
        }

        if (methodInfo.Name is "Average" or "Sum" or "SumUnchecked")
        {
            if (methodInfo.GetParameters().Length == 2) // func
            {
                return """

        where TResult : struct
#if NET8_0_OR_GREATER
        , INumber<TResult>
#endif

""";
            }
            else
            {
                return """

        where TSource : struct
#if NET8_0_OR_GREATER
        , INumber<TSource>
#endif

""";
            }
        }

        if (methodInfo.Name is "Concat" or "Except" or "Intersect" or "Union" or "UnionBy" or "SequenceEqual")
        {
            return """

        where TEnumerator2 : struct, IValueEnumerator<TSource>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif

""";
        }

        if (methodInfo.Name is "ExceptBy" or "IntersectBy")
        {
            return """

        where TEnumerator2 : struct, IValueEnumerator<TKey>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif

""";
        }

        if (methodInfo.Name is "GroupJoin" or "Join" or "LeftJoin" or "RightJoin")
        {
            return """

        where TEnumerator2 : struct, IValueEnumerator<TInner>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif

""";
        }

        if (methodInfo.Name is "SelectMany")
        {
            if (methodInfo.GetGenericArguments().Any(x => x.Name == "TCollection"))
            {
                return """

        where TEnumerator2 : struct, IValueEnumerator<TCollection>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif

""";
            }
            else
            {
                return """

        where TEnumerator2 : struct, IValueEnumerator<TResult>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif

""";
            }
        }

        if (methodInfo.Name is "Zip")
        {
            if (methodInfo.GetGenericArguments().Any(x => x.Name == "TThird"))
            {
                return """

        where TEnumerator2 : struct, IValueEnumerator<TSecond>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TEnumerator3 : struct, IValueEnumerator<TThird>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif

""";
            }
            else
            {
                return """

        where TEnumerator2 : struct, IValueEnumerator<TSecond>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif

""";
            }
        }


        return "";
    }
}
