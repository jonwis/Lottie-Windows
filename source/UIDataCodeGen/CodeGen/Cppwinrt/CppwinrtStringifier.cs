// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Numerics;
using CommunityToolkit.WinUI.Lottie.WinCompData;
using CommunityToolkit.WinUI.Lottie.WinCompData.Wui;
using Mgcg = CommunityToolkit.WinUI.Lottie.WinCompData.Mgcg;

namespace CommunityToolkit.WinUI.Lottie.UIData.CodeGen.Cppwinrt
{
    /// <summary>
    /// Stringifiers for C++/WinRT syntax.
    /// </summary>
    sealed class CppwinrtStringifier : Stringifier
    {
        public override string CanvasFigureLoop(Mgcg.CanvasFigureLoop value) =>
            value switch
            {
                Mgcg.CanvasFigureLoop.Open => "D2D1_FIGURE_END_OPEN",
                Mgcg.CanvasFigureLoop.Closed => "D2D1_FIGURE_END_CLOSED",
                _ => throw new InvalidOperationException(),
            };

        public override string CanvasGeometryCombine(Mgcg.CanvasGeometryCombine value) =>
            value switch
            {
                Mgcg.CanvasGeometryCombine.Union => "D2D1_COMBINE_MODE_UNION",
                Mgcg.CanvasGeometryCombine.Exclude => "D2D1_COMBINE_MODE_EXCLUDE",
                Mgcg.CanvasGeometryCombine.Intersect => "D2D1_COMBINE_MODE_INTERSECT",
                Mgcg.CanvasGeometryCombine.Xor => "D2D1_COMBINE_MODE_XOR",
                _ => throw new InvalidOperationException(),
            };

        public override string CanvasGeometryFactoryCall(string value) => value;

        public override string Color(Color value) => $"{{ {ColorArgs(value)} }}";

        public string ColorArgs(Color value) => $"{Hex(value.A)}, {Hex(value.R)}, {Hex(value.G)}, {Hex(value.B)}";

        public override string ConstExprField(string type, string name, string value) => $"static constexpr {type} {name}{{ {value} }};";

        public override string ConstVar => "const auto";

        public override string DefaultInitialize => "{ nullptr }";

        public override string Deref => ".";

        public override string Double(double value) =>
            Math.Floor(value) == value
                ? value.ToString("0.0", CultureInfo.InvariantCulture)
                : value.ToString("G15", CultureInfo.InvariantCulture);

        public override string FilledRegionDetermination(Mgcg.CanvasFilledRegionDetermination value) =>
            value switch
            {
                Mgcg.CanvasFilledRegionDetermination.Alternate => "D2D1_FILL_MODE_ALTERNATE",
                Mgcg.CanvasFilledRegionDetermination.Winding => "D2D1_FILL_MODE_WINDING",
                _ => throw new InvalidOperationException(),
            };

        public override string Float(float value) =>
            (Math.Floor(value) == value
                ? value.ToString("0.0", CultureInfo.InvariantCulture)
                : value.ToString("G9", CultureInfo.InvariantCulture)) + "F";

        public override string IListAdd => "Append";

        public override string Int32(int value) => value.ToString();

        public override string Int64(long value) => $"{value}L";

        public override string Matrix3x2(Matrix3x2 value) =>
            $"{{ {Float(value.M11)}, {Float(value.M12)}, {Float(value.M21)}, {Float(value.M22)}, {Float(value.M31)}, {Float(value.M32)} }}";

        public override string Matrix4x4(Matrix4x4 value) =>
            $"{{ {Float(value.M11)}, {Float(value.M12)}, {Float(value.M13)}, {Float(value.M14)}, {Float(value.M21)}, {Float(value.M22)}, {Float(value.M23)}, {Float(value.M24)}, {Float(value.M31)}, {Float(value.M32)}, {Float(value.M33)}, {Float(value.M34)}, {Float(value.M41)}, {Float(value.M42)}, {Float(value.M43)}, {Float(value.M44)} }}";

        public override string Namespace(string value) => value.Replace(".", "::");

        public override string New(string typeName) => typeName switch
        {
            "CompositionPath" => "MakeCompositionPath",
            _ => typeName
        };

        public override string FieldTypeName(string value) => value switch
        {
            "BackEasingFunction" => "CompositionEasingFunction",
            "BounceEasingFunction" => "CompositionEasingFunction",
            "CircleEasingFunction" => "CompositionEasingFunction",
            "CubicBezierEasingFunction" => "CompositionEasingFunction",
            "ElasticEasingFunction" => "CompositionEasingFunction",
            "ExponentialEasingFunction" => "CompositionEasingFunction",
            "LinearEasingFunction" => "CompositionEasingFunction",
            "PowerEasingFunction" => "CompositionEasingFunction",
            "SineEasingFunction" => "CompositionEasingFunction",
            "StepEasingFunction" => "CompositionEasingFunction",

            // "ExpressionAnimation" => "CompositionAnimation",
            "KeyframeAnimation" => "CompositionAnimation",
            "NaturalMotionAnimation" => "CompositionAnimation",
            "BooleanKeyFrameAnimation" => "CompositionAnimation",
            "ColorKeyFrameAnimation" => "CompositionAnimation",
            "PathKeyFrameAnimation" => "CompositionAnimation",
            "QuaternionKeyFrameAnimation" => "CompositionAnimation",
            "ScalarKeyFrameAnimation" => "CompositionAnimation",
            "Vector2KeyFrameAnimation" => "CompositionAnimation",
            "Vector3KeyFrameAnimation" => "CompositionAnimation",
            "Vector4KeyFrameAnimation" => "CompositionAnimation",
            _ => value
        };

        public override string Null => "nullptr";

        public override string PropertyGet(string target, string propertyName) => $"{target}.{propertyName}()";

        public override string PropertySet(string target, string propertyName, string value) =>
            $"{target}.{propertyName}({value})";

        public override string ReferenceTypeName(string value) =>
            value switch
            {
                "CanvasGeometry" => "winrt::com_ptr<CanvasGeometry>",
                "StepEasingFunction" => "CompositionEasingFunction",
                "CubicBezierEasingFunction" => "CompositionEasingFunction",
                "CompositionContainerShape" => "CompositionShape",
                "CompositionSpriteShape" => "CompositionShape",
                "CompositionEllipseGeometry" => "CompositionGeometry",
                "CompositionPathGeometry" => "CompositionGeometry",
                _ => value
            };

        public override string ArgumentTypeName(string value) =>
            value switch
            {
                "CanvasGeometry" => "winrt::com_ptr<CanvasGeometry> const&",
                "CompositionPath" => "CompositionPath",
                _ => $"{value} const&"
            };

        public override string ScopeResolve => "::";

        public override string String(string value) => $"L\"{value}\"";

        public override string TimeSpan(string ticks) => $"TimeSpan{{ {ticks} }}";

        public override string Readonly(string value) => $"{value} const";

        public override string TimeSpan(TimeSpan value) => TimeSpan(Int64(value.Ticks));

        public override string TypeInt64 => "int64_t";

        public override string TypeMatrix3x2 { get; } = "float3x2";

        public override string TypeString => "const wchar_t*";

        public override string TypeVector2 { get; } = "float2";

        public override string TypeVector3 { get; } = "float3";

        public override string TypeVector4 { get; } = "float4";

        public override string Var => "auto";

        public override string VariableInitialization(string value) => $"{{ {value} }}";

        public override string Vector2(Vector2 value)
        {
            if ((value.X == 0) && (value.Y == 0))
            {
                return "f2_zero_zero";
            }
            else if ((value.X == 1) && (value.Y == 1))
            {
                return "f2_one_one";
            }
            else
            {
                return $"{{ {Vector2Args(value)} }}";
            }
        }

        public string Vector2Args(Vector2 value) => $"{Float(value.X)}, {Float(value.Y)}";

        public override string Vector3(Vector3 value) => $"{{ {Vector3Args(value)} }}";

        public string Vector3Args(Vector3 value) => $"{Float(value.X)}, {Float(value.Y)}, {Float(value.Z)}";

        public override string Vector4(Vector4 value) => $"{{ {Vector4Args(value)} }}";

        public string Vector4Args(Vector4 value) => $"{Float(value.X)}, {Float(value.Y)}, {Float(value.Z)}, {Float(value.W)}";

        public string Stringify(bool? value) => value.GetValueOrDefault().ToString();

        public string Stringify(float? value) => Float(value.GetValueOrDefault());

        public string Stringify(Vector2? value) => Vector2(value.GetValueOrDefault());

        public string Stringify(Vector3? value) => Vector3(value.GetValueOrDefault());

        public string Stringify(Vector4? value) => Vector4(value.GetValueOrDefault());

        public string Stringify(CompositionStrokeLineJoin? value) => StrokeLineJoin(value.GetValueOrDefault());

        public string Stringify(CompositionStrokeCap? value) => StrokeCap(value.GetValueOrDefault());

        public string Stringify(Matrix3x2? value) => Matrix3x2(value.GetValueOrDefault());

        public string Stringify(Matrix4x4? value) => Matrix4x4(value.GetValueOrDefault());

        public string Stringify(CompositionMappingMode? value) => MappingMode(value.GetValueOrDefault());

        public string Stringify(CompositionGradientExtendMode? value) => ExtendMode(value.GetValueOrDefault());

        public string Stringify(CompositionDropShadowSourcePolicy? value) => DropShadowSourcePolicy(value.GetValueOrDefault());

        public string Stringify(CompositionColorSpace? value) => ColorSpace(value.GetValueOrDefault());
    }
}
