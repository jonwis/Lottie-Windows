// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using CommunityToolkit.WinUI.Lottie.LottieData;
using CommunityToolkit.WinUI.Lottie.UIData.CodeGen;
using CommunityToolkit.WinUI.Lottie.WinCompData;
using CommunityToolkit.WinUI.Lottie.WinCompData.MetaData;
using CommunityToolkit.WinUI.Lottie.WinCompData.Mgc;
using CommunityToolkit.WinUI.Lottie.WinCompData.Mgce;
using CommunityToolkit.WinUI.Lottie.WinCompData.Mgcg;
using CommunityToolkit.WinUI.Lottie.WinCompData.Wg;
using CommunityToolkit.WinUI.Lottie.WinUIXamlMediaData;
using Microsoft.VisualBasic;
using Expr = CommunityToolkit.WinUI.Lottie.WinCompData.Expressions;
using Mgce = CommunityToolkit.WinUI.Lottie.WinCompData.Mgce;
using Sn = System.Numerics;
using Wmd = CommunityToolkit.WinUI.Lottie.WinUIXamlMediaData;
using Wui = CommunityToolkit.WinUI.Lottie.WinCompData.Wui;

namespace CommunityToolkit.WinUI.Lottie.UIData.CodeGen.Cppwinrt
{
    /// <summary>
    /// Generates code for use by CppWinrt.
    /// </summary>
#if PUBLIC_UIDataCodeGen
    public
#else
    internal
#endif
    partial class CppwinrtInstantiatorGenerator
    {
        class CppWinrtVisualGenerator : AnimatedVisualGenerator
        {
            class FieldWriter
            {
                readonly CodeBuilder _builder;
                readonly CppwinrtStringifier _s = new CppwinrtStringifier();
                readonly List<string> _fields = new List<string>();

                internal FieldWriter(CodeBuilder builder)
                {
                    _builder = builder;
                }

                internal void Write(object? o, string name)
                {
                    if (o is not null)
                    {
                        _builder.WriteLine($"{_s.Stringify((dynamic)o)}, // {name}");
                        _fields.Add(name);
                    }
                    else
                    {
                        _builder.WriteLine($"{{ /* unset */ }}, // {name}");
                    }
                }

                internal string Fields { get => string.Join(" | ", _fields); }
            }

            readonly CppwinrtStringifier _s = new CppwinrtStringifier();
            readonly CppwinrtInstantiatorGenerator _generator;

            internal CppWinrtVisualGenerator(
                CppwinrtInstantiatorGenerator generator,
                InstantiatorGeneratorBase owner,
                CompositionObject graphRoot,
                uint requiredUapVersion,
                bool isPartOfMultiVersionSource,
                CodegenConfiguration configuration)
                : base(owner, graphRoot, requiredUapVersion, isPartOfMultiVersionSource, configuration)
            {
                _generator = generator;
            }

            protected override void InitializeCompositionAnimation(
                CodeBuilder builder,
                CompositionAnimation obj,
                ObjectData node,
                IEnumerable<KeyValuePair<string, string>> parameters)
            {
                InitializeCompositionObject(builder, obj, node);
                WriteSetPropertyStatementDefaultIsNullOrWhitespace(builder, nameof(obj.Target), obj.Target);

                foreach (var parameter in parameters)
                {
                    builder.WriteLine($"result.SetReferenceParameter({String(parameter.Key)}, invoke_func_or_field({parameter.Value}));");
                }
            }

            protected override void InitializeCompositionObject(CodeBuilder builder, CompositionObject obj, ObjectData node, string localName = "result")
            {
                if (Owner.SetCommentProperties)
                {
                    WriteSetPropertyStatementDefaultIsNullOrWhitespace(builder, nameof(obj.Comment), obj.Comment, localName);
                }

                var propertySet = obj.Properties;

                if (propertySet.Names.Count > 0)
                {
                    builder.WriteLine($"constexpr static const propset_value props[] =");
                    builder.OpenScope();

                    foreach (var (name, type) in propertySet.Names)
                    {
                        string propValue = Owner.PropertySetValueInitializer(propertySet, name, type);

                        switch (type)
                        {
                            case PropertySetValueType.Color:
                                builder.WriteLine($"{{ L\"{name}\", Color {{ {propValue} }} }},");
                                break;

                            case PropertySetValueType.Scalar:
                                builder.WriteLine($"{{ L\"{name}\", float {{ {propValue} }} }},");
                                break;

                            case PropertySetValueType.Vector2:
                                builder.WriteLine($"{{ L\"{name}\", float2 {propValue} }},");
                                break;

                            case PropertySetValueType.Vector3:
                                builder.WriteLine($"{{ L\"{name}\", float3 {propValue} }},");
                                break;

                            case PropertySetValueType.Vector4:
                                builder.WriteLine($"{{ L\"{name}\", float4 {propValue} }},");
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                    }

                    builder.CloseScopeWithSemicolon();
                    builder.WriteLine($"ApplyProperties({localName}, props, _countof(props));");
                }
            }

            protected override void InitializeCompositionGradientBrush(CodeBuilder builder, CompositionGradientBrush obj, ObjectData node)
            {
                InitializeCompositionObject(builder, obj, node);

                if (obj.ColorStops.Any())
                {
                    builder.WriteLine("constexpr static const func_or_field<CompositionColorGradientStop> colorStops[] =");
                    builder.OpenScope();
                    foreach (var colorStop in obj.ColorStops)
                    {
                        builder.WriteLine($"{CallFactoryFromFor(node, colorStop)},");
                    }

                    builder.CloseScopeWithSemicolon();
                    builder.WriteLine("constexpr static const uint32_t colorStopCount = _countof(colorStops);");
                }
                else
                {
                    builder.WriteLine("constexpr static const func_or_field<CompositionColorGradientStop> *colorStops = nullptr;");
                    builder.WriteLine("constexpr static const uint32_t colorStopCount = 0;");
                }

                builder.WriteLine("constexpr static const GradientBrushConfig config =");
                builder.OpenScope();
                var writer = new FieldWriter(builder);
                writer.Write(obj.AnchorPoint, "GradientBrushConfig::ConfigFlags::AnchorPoint");
                writer.Write(obj.CenterPoint, "GradientBrushConfig::ConfigFlags::CenterPoint");
                writer.Write(obj.ExtendMode, "GradientBrushConfig::ConfigFlags::ExtendMode");
                writer.Write(obj.InterpolationSpace, "GradientBrushConfig::ConfigFlags::InterpolationSpace");
                writer.Write(obj.MappingMode, "GradientBrushConfig::ConfigFlags::MappingMode");
                writer.Write(obj.Offset, "GradientBrushConfig::ConfigFlags::Offset");
                writer.Write(obj.RotationAngleInDegrees, "GradientBrushConfig::ConfigFlags::RotationAngleInDegrees");
                writer.Write(obj.Scale, "GradientBrushConfig::ConfigFlags::Scale");
                writer.Write(obj.TransformMatrix, "GradientBrushConfig::ConfigFlags::TransformMatrix");
                builder.WriteLine("colorStops, colorStopCount,");
                builder.WriteLine(writer.Fields);
                builder.CloseScopeWithSemicolon();
                builder.WriteLine("ApplyGradientBrushConfig(result, config);");
            }

            protected string ToFuncOrFieldFromFor(string s) => (s.Replace("()", string.Empty) + "Id").Replace("IdId", "Id");

            protected override string CallFactoryFromFor(ObjectData callerNode, ObjectData calleeNode)
            {
                return ToFuncOrFieldFromFor(base.CallFactoryFromFor(callerNode, calleeNode!));
            }

            protected override string CallFactoryFromFor(ObjectData callerNode, CompositionObject? obj) =>
                obj is null
                ? "{ /* unset */ }"
                : ToFuncOrFieldFromFor(base.CallFactoryFromFor(callerNode, NodeFor(obj)));

            protected override string CallCreateCompositionPath(ObjectData node, IGeometrySource2D source)
            {
                return CallFactoryFromFor(node, ((CompositionPath)node.Object).Source);
            }

            protected override bool GenerateCompositionPathGeometryFactory(CodeBuilder builder, CompositionPathGeometry obj, ObjectData node)
            {
                var path = obj.Path is null ? null : ObjectPath(obj.Path);
                var createPathText = path is null ? string.Empty : CallFactoryFromFor(node, path);
                var createPathGeometryText = $"MakePathGeometry({createPathText})";

                WriteObjectFactoryStart(builder, node);
                WriteCreateAssignment(builder, node, createPathGeometryText);
                InitializeCompositionGeometry(builder, obj, node);
                WriteCompositionObjectFactoryEnd(builder, obj, node);

                return true;
            }

            protected override bool GenerateCompositionVisualSurfaceFactory(CodeBuilder builder, CompositionVisualSurface obj, ObjectData node)
            {
                WriteObjectFactoryStart(builder, node);
                WriteCreateAssignment(builder, node, $"_c.CreateVisualSurface()");
                InitializeCompositionObject(builder, obj, node);

                var writer = new FieldWriter(builder);
                builder.WriteLine("constexpr static const VisualSurfaceConfig props = ");
                builder.OpenScope();
                builder.WriteLine($"{CallFactoryFromFor(node, obj.SourceVisual)},");
                writer.Write(obj.SourceSize, "VisualSurfaceConfig::ConfigFlags::SourceSize");
                writer.Write(obj.SourceOffset, "VisualSurfaceConfig::ConfigFlags::SourceOffset");
                builder.WriteLine(writer.Fields);
                builder.CloseScopeWithSemicolon();

                builder.WriteLine("ApplyVisualSurfaceConfig(result, props);");

                WriteCompositionObjectFactoryEnd(builder, obj, node);
                return true;
            }

            protected override bool GenerateCompositionEffectBrushFactory(CodeBuilder builder, CompositionEffectBrush obj, ObjectData node)
            {
                var sources = obj.GetEffectFactory().Effect.Sources;

                WriteObjectFactoryStart(builder, node);

                if (sources.Any())
                {
                    builder.WriteLine($"constexpr static const CompositionBrushProps::SourceParameter params[] =");
                    builder.OpenScope();

                    foreach (var source in sources)
                    {
                        builder.WriteLine($"{{ {String(source.Name)}, {CallFactoryFromFor(node, obj.GetSourceParameter(source.Name))} }},");
                    }

                    builder.CloseScopeWithSemicolon();
                }

                builder.WriteLine($"constexpr static const CompositionBrushProps props =");
                builder.OpenScope();
                builder.WriteLine($"{CallFactoryFromFor(node, obj.GetEffectFactory())},");
                builder.WriteLine(sources.Any() ? "params, _countof(params)," : "nullptr, 0,");
                builder.CloseScopeWithSemicolon();

                WriteCreateAssignment(builder, node, $"MakeEffectBrush(props)");
                InitializeCompositionBrush(builder, obj, node);
                WriteCompositionObjectFactoryEnd(builder, obj, node);

                return true;
            }

            protected override void InitializeContainerVisual(CodeBuilder builder, ContainerVisual obj, ObjectData node)
            {
                InitializeVisual(builder, obj, node);

                if (obj.Children.Any())
                {
                    builder.WriteLine("constexpr static const func_or_field<Visual> visuals[] =");
                    builder.OpenScope();
                    foreach (var child in obj.Children)
                    {
                        WriteShortDescriptionComment(builder, child);
                        builder.WriteLine($"{CallFactoryFromFor(node, child)},");
                    }

                    builder.CloseScopeWithSemicolon();
                    builder.WriteLine($"ApplyContainerVisuals(result, visuals, _countof(visuals));");
                }
            }

            protected override void InitializeVisual(CodeBuilder builder, Visual obj, ObjectData node)
            {
                InitializeCompositionObject(builder, obj, node);

                var subBuilder = new CodeBuilder();
                string nameOfTransform = "nullptr";
                if (obj.TransformMatrix.HasValue)
                {
                    subBuilder.WriteLine($"constexpr static const float4x4 transform = {Matrix4x4(obj.TransformMatrix.Value)};");
                    nameOfTransform = "&transform";
                }

                subBuilder.WriteLine("constexpr static const VisualConfig visProps =");
                subBuilder.OpenScope();

                var writer = new FieldWriter(subBuilder);

                if (obj.BorderMode.HasValue && obj.BorderMode != CompositionBorderMode.Inherit)
                {
                    writer.Write(obj.BorderMode, "VisualConfigFlags::BorderMode");
                }
                else
                {
                    subBuilder.WriteLine("{ /* unset */ }, /* BorderMode */");
                }

                writer.Write(obj.CenterPoint, "VisualConfigFlags::CenterPoint");
                subBuilder.WriteLine(obj.Clip != null ? $"{CallFactoryFromFor(node, obj.Clip)}," : "{ /* clip unset */ },");
                writer.Write(obj.IsVisible, "VisualConfigFlags::IsVisible");
                writer.Write(obj.Offset, "VisualConfigFlags::Offset");
                writer.Write(obj.Opacity, "VisualConfigFlags::Opacity");
                writer.Write(obj.RotationAngleInDegrees, "VisualConfigFlags::RotationAngleInDegrees");
                writer.Write(obj.RotationAxis, "VisualConfigFlags::RotationAxis");
                writer.Write(obj.Scale, "VisualConfigFlags::Scale");
                writer.Write(obj.Size, "VisualConfigFlags::Size");
                subBuilder.WriteLine($"{nameOfTransform},");
                subBuilder.WriteLine(writer.Fields);
                subBuilder.CloseScopeWithSemicolon();
                subBuilder.WriteLine("ApplyVisualConfig(result, visProps);");

                if (writer.Fields.Any() || nameOfTransform != "nullptr")
                {
                    builder.WriteCodeBuilder(subBuilder);
                }
            }

            protected override void InitializeCompositionGeometry(CodeBuilder builder, CompositionGeometry obj, ObjectData node)
            {
                InitializeCompositionObject(builder, obj, node);

                var subBuilder = new CodeBuilder();
                subBuilder.WriteLine("constexpr static const GeometryConfig geometryConfig =");
                subBuilder.OpenScope();
                var writer = new FieldWriter(subBuilder);
                writer.Write(obj.TrimEnd, "GeometryConfig::ConfigFlags::TrimEnd");
                writer.Write(obj.TrimStart, "GeometryConfig::ConfigFlags::TrimStart");
                writer.Write(obj.TrimOffset, "GeometryConfig::ConfigFlags::TrimOffset");
                subBuilder.WriteLine(writer.Fields);
                subBuilder.CloseScopeWithSemicolon();
                subBuilder.WriteLine("ApplyGeometryConfig(result, geometryConfig);");

                if (writer.Fields.Any())
                {
                    builder.WriteCodeBuilder(subBuilder);
                }
            }

            protected override bool GenerateCompositionEllipseGeometryFactory(CodeBuilder builder, CompositionEllipseGeometry obj, ObjectData node)
            {
                WriteObjectFactoryStart(builder, node);

                builder.WriteLine($"constexpr static const EllipseConfig center_radius = {{ {_s.Vector2(obj.Center)}, {_s.Vector2(obj.Radius)} }};");
                WriteCreateAssignment(builder, node, $"CreateEllipseGeometry(center_radius)");
                InitializeCompositionGeometry(builder, obj, node);
                WriteCompositionObjectFactoryEnd(builder, obj, node);
                return true;
            }

            protected override bool GenerateSpriteShapeFactory(CodeBuilder builder, CompositionSpriteShape obj, ObjectData node)
            {
                builder.WriteComment(node.LongComment);
                builder.WriteLine($"CompositionShape {node.Name}()");
                builder.OpenScope();

                if (obj.StrokeDashArray.Any())
                {
                    builder.WriteLine($"constexpr static const float dashes[] = {{ {string.Join(", ", obj.StrokeDashArray.Select(_ => _s.Stringify(_)))} }};");
                    builder.WriteLine("constexpr uint32_t dashCount = _countof(dashes);");
                }

                // Write all the constant properties
                builder.WriteLine("constexpr static const SpriteShapeProperties props =");
                builder.OpenScope();
                var writer = new FieldWriter(builder);

                writer.Write(obj.TransformMatrix, "SpriteFields::Transformation");
                writer.Write(obj.StrokeDashCap, "SpriteFields::StrokeDashCap");
                writer.Write(obj.StrokeDashOffset, "SpriteFields::StrokeDashOffset");
                writer.Write(obj.StrokeStartCap, "SpriteFields::StrokeStartCap");
                writer.Write(obj.StrokeEndCap, "SpriteFields::StrokeEndCap");
                writer.Write(obj.StrokeLineJoin, "SpriteFields::StrokeLineJoin");
                writer.Write(obj.StrokeMiterLimit, "SpriteFields::StrokeMiterLimit");
                writer.Write(obj.StrokeThickness, "SpriteFields::StrokeThickness");
                writer.Write(obj.IsStrokeNonScaling, "SpriteFields::IsStrokeNonScaling");
                writer.Write(obj.CenterPoint, "SpriteFields::CenterPoint");
                writer.Write(obj.Offset, "SpriteFields::Offset");
                writer.Write(obj.RotationAngleInDegrees, "SpriteFields::RotationInDegrees");
                writer.Write(obj.Scale, "SpriteFields::Scale");

                if (obj.StrokeDashArray.Any())
                {
                    builder.WriteLine("dashes, dashCount,");
                }
                else
                {
                    builder.WriteLine("nullptr, 0,");
                }

                builder.WriteLine((obj.Geometry != null) ? $"{CallFactoryFromFor(node, obj.Geometry)}," : "func_or_field<CompositionGeometry> { /* no geometry */ },");
                builder.WriteLine((obj.FillBrush != null) ? $"{CallFactoryFromFor(node, obj.FillBrush)}," : "func_or_field<CompositionBrush> { /* no fill */ },");
                builder.WriteLine((obj.StrokeBrush != null) ? $"{CallFactoryFromFor(node, obj.StrokeBrush)}," : "func_or_field<CompositionBrush> { /* no stroke */ },");

                builder.WriteLine(writer.Fields);
                builder.CloseScopeWithSemicolon();

                // Write the call to the maker method
                WriteCreateAssignment(builder, node, "MakeAndApplyProperties(props)");

                WriteCompositionObjectStartAnimations(builder, obj, node);

                builder.WriteLine("return result;");
                builder.CloseScope();
                builder.WriteLine();

                return true;
            }

            protected override bool GenerateSpriteVisualFactory(CodeBuilder builder, SpriteVisual obj, ObjectData node)
            {
                WriteObjectFactoryStart(builder, node);

                builder.WriteLine("constexpr static const SpriteVisualConfig props =");
                builder.OpenScope();
                builder.WriteLine($"{CallFactoryFromFor(node, obj.Brush)},");
                builder.WriteLine($"{CallFactoryFromFor(node, obj.Shadow)},");
                builder.CloseScopeWithSemicolon();
                WriteCreateAssignment(builder, node, "CreateSpriteVisual(props)");

                InitializeContainerVisual(builder, obj, node);

                WriteCompositionObjectFactoryEnd(builder, obj, node);
                return true;
            }

            protected override bool GenerateVector2KeyFrameAnimationFactory(CodeBuilder builder, Vector2KeyFrameAnimation obj, ObjectData node)
            {
                WriteObjectFactoryStart(builder, node);
                var keyFrames = obj.KeyFrames;
                var firstKeyFrame = keyFrames.First();
                string durationAmount = "nullptr";

                if (obj.Duration == Owner.CompositionDuration)
                {
                    durationAmount = "&c_duration";
                }

                builder.WriteLine("constexpr static const KeyFrameStep<float2> steps[] =");
                builder.OpenScope();
                foreach (var kf in keyFrames)
                {
                    WriteFrameNumberComment(builder, kf.Progress);

                    switch (kf.Type)
                    {
                        case KeyFrameType.Expression:
                            var expressionKeyFrame = (KeyFrameAnimation<Vector2, Expr.Vector2>.ExpressionKeyFrame)kf;
                            builder.WriteLine($"{{ {Float(kf.Progress)}, {String(expressionKeyFrame.Expression)}, {CallFactoryFromFor(node, kf.Easing)} }},");
                            break;
                        case KeyFrameType.Value:
                            var valueKeyFrame = (KeyFrameAnimation<Vector2, Expr.Vector2>.ValueKeyFrame)kf;
                            builder.WriteLine($"{{ {Float(kf.Progress)}, float2 {Vector2(valueKeyFrame.Value)}, {CallFactoryFromFor(node, kf.Easing)} }},");
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }

                builder.CloseScopeWithSemicolon();

                WriteCreateAssignment(builder, node, $"ConfigureAnimationKeyFrames({durationAmount}, steps, _countof(steps))");
                InitializeCompositionAnimation(builder, obj, node);
                WriteCompositionObjectFactoryEnd(builder, obj, node);
                return true;
            }

            protected override bool GenerateVector3KeyFrameAnimationFactory(CodeBuilder builder, Vector3KeyFrameAnimation obj, ObjectData node)
            {
                WriteObjectFactoryStart(builder, node);
                var keyFrames = obj.KeyFrames;
                var firstKeyFrame = keyFrames.First();
                string durationAmount = "nullptr";

                if (obj.Duration == Owner.CompositionDuration)
                {
                    durationAmount = "&c_duration";
                }

                builder.WriteLine("constexpr static const KeyFrameStep<float3> steps[] =");
                builder.OpenScope();
                foreach (var kf in keyFrames)
                {
                    WriteFrameNumberComment(builder, kf.Progress);

                    switch (kf.Type)
                    {
                        case KeyFrameType.Expression:
                            var expressionKeyFrame = (KeyFrameAnimation<Vector3, Expr.Vector3>.ExpressionKeyFrame)kf;
                            builder.WriteLine($"{{ {Float(kf.Progress)}, {String(expressionKeyFrame.Expression)}, {CallFactoryFromFor(node, kf.Easing)} }},");
                            break;
                        case KeyFrameType.Value:
                            var valueKeyFrame = (KeyFrameAnimation<Vector3, Expr.Vector3>.ValueKeyFrame)kf;
                            builder.WriteLine($"{{ {Float(kf.Progress)}, float3 {Vector3(valueKeyFrame.Value)}, {CallFactoryFromFor(node, kf.Easing)} }},");
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }

                builder.CloseScopeWithSemicolon();

                WriteCreateAssignment(builder, node, $"ConfigureAnimationKeyFrames({durationAmount}, steps, _countof(steps))");
                InitializeCompositionAnimation(builder, obj, node);
                WriteCompositionObjectFactoryEnd(builder, obj, node);
                return true;
            }

            protected override bool GenerateVector4KeyFrameAnimationFactory(CodeBuilder builder, Vector4KeyFrameAnimation obj, ObjectData node)
            {
                WriteObjectFactoryStart(builder, node);
                var keyFrames = obj.KeyFrames;
                var firstKeyFrame = keyFrames.First();
                string durationAmount = "nullptr";

                if (obj.Duration == Owner.CompositionDuration)
                {
                    durationAmount = "&c_duration";
                }

                builder.WriteLine("constexpr static const KeyFrameStep<float4> steps[] =");
                builder.OpenScope();
                foreach (var kf in keyFrames)
                {
                    WriteFrameNumberComment(builder, kf.Progress);

                    switch (kf.Type)
                    {
                        case KeyFrameType.Expression:
                            var expressionKeyFrame = (KeyFrameAnimation<Vector4, Expr.Vector4>.ExpressionKeyFrame)kf;
                            builder.WriteLine($"{{ {Float(kf.Progress)}, {String(expressionKeyFrame.Expression)}, {CallFactoryFromFor(node, kf.Easing)} }},");
                            break;
                        case KeyFrameType.Value:
                            var valueKeyFrame = (KeyFrameAnimation<Vector4, Expr.Vector4>.ValueKeyFrame)kf;
                            builder.WriteLine($"{{ {Float(kf.Progress)}, float4 {Vector4(valueKeyFrame.Value)}, {CallFactoryFromFor(node, kf.Easing)} }},");
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }

                builder.CloseScopeWithSemicolon();

                WriteCreateAssignment(builder, node, $"ConfigureAnimationKeyFrames({durationAmount}, steps, _countof(steps))");
                InitializeCompositionAnimation(builder, obj, node);
                WriteCompositionObjectFactoryEnd(builder, obj, node);
                return true;
            }

            protected override bool GenerateScalarKeyFrameAnimationFactory(CodeBuilder builder, ScalarKeyFrameAnimation obj, ObjectData node)
            {
                WriteObjectFactoryStart(builder, node);
                var keyFrames = obj.KeyFrames;
                var firstKeyFrame = keyFrames.First();
                string durationAmount = "nullptr";

                if (obj.Duration == Owner.CompositionDuration)
                {
                    durationAmount = "&c_duration";
                }

                builder.WriteLine("constexpr static const KeyFrameStep<float> steps[] =");
                builder.OpenScope();
                foreach (var kf in keyFrames)
                {
                    WriteFrameNumberComment(builder, kf.Progress);

                    switch (kf.Type)
                    {
                        case KeyFrameType.Expression:
                            var expressionKeyFrame = (KeyFrameAnimation<float, Expr.Scalar>.ExpressionKeyFrame)kf;
                            builder.WriteLine($"{{ {Float(kf.Progress)}, {String(expressionKeyFrame.Expression)}, {CallFactoryFromFor(node, kf.Easing)} }},");
                            break;
                        case KeyFrameType.Value:
                            var valueKeyFrame = (KeyFrameAnimation<float, Expr.Scalar>.ValueKeyFrame)kf;
                            builder.WriteLine($"{{ {Float(kf.Progress)}, {Float(valueKeyFrame.Value)}, {CallFactoryFromFor(node, kf.Easing)} }},");
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }

                builder.CloseScopeWithSemicolon();

                WriteCreateAssignment(builder, node, $"ConfigureAnimationKeyFrames({durationAmount}, steps, _countof(steps))");
                InitializeCompositionAnimation(builder, obj, node);
                WriteCompositionObjectFactoryEnd(builder, obj, node);
                return true;
            }

            protected override bool GenerateColorKeyFrameAnimationFactory(CodeBuilder builder, ColorKeyFrameAnimation obj, ObjectData node)
            {
                WriteObjectFactoryStart(builder, node);
                var keyFrames = obj.KeyFrames;
                var firstKeyFrame = keyFrames.First();
                string durationAmount = "nullptr";

                if (obj.Duration == Owner.CompositionDuration)
                {
                    durationAmount = "&c_duration";
                }

                builder.WriteLine("constexpr static const KeyFrameStep<Color> steps[] =");
                builder.OpenScope();
                foreach (var kf in keyFrames)
                {
                    WriteFrameNumberComment(builder, kf.Progress);

                    switch (kf.Type)
                    {
                        case KeyFrameType.Expression:
                            var expressionKeyFrame = (KeyFrameAnimation<Wui.Color, Expr.Color>.ExpressionKeyFrame)kf;
                            builder.WriteLine($"{{ {Float(kf.Progress)}, {String(expressionKeyFrame.Expression)}, {CallFactoryFromFor(node, kf.Easing)} }},");
                            break;
                        case KeyFrameType.Value:
                            var valueKeyFrame = (KeyFrameAnimation<Wui.Color, Expr.Color>.ValueKeyFrame)kf;
                            builder.WriteLine($"{{ {Float(kf.Progress)}, Color {Color(valueKeyFrame.Value)}, {CallFactoryFromFor(node, kf.Easing)} }},");
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }

                builder.CloseScopeWithSemicolon();

                WriteCreateAssignment(builder, node, $"ConfigureAnimationKeyFrames({durationAmount}, steps, _countof(steps))");
                InitializeCompositionAnimation(builder, obj, node);
                WriteCompositionObjectFactoryEnd(builder, obj, node);
                return true;
            }

            protected override bool GeneratePathKeyFrameAnimationFactory(CodeBuilder builder, PathKeyFrameAnimation obj, ObjectData node)
            {
                WriteObjectFactoryStart(builder, node);
                var keyFrames = obj.KeyFrames;
                var firstKeyFrame = keyFrames.First();
                string durationAmount = "nullptr";

                if (obj.Duration == Owner.CompositionDuration)
                {
                    durationAmount = "&c_duration";
                }

                builder.WriteLine("constexpr static const KeyFrameStep<func_or_field<CompositionPath>> steps[] =");
                builder.OpenScope();
                foreach (var kf in keyFrames)
                {
                    WriteFrameNumberComment(builder, kf.Progress);
                    var valueKeyFrame = (PathKeyFrameAnimation.ValueKeyFrame)kf;
                    builder.WriteLine($"{{ {Float(kf.Progress)}, func_or_field<CompositionPath> {{ {CallFactoryFromFor(node, valueKeyFrame.Value)} }}, {CallFactoryFromFor(node, kf.Easing)} }},");

                    switch (kf.Type)
                    {
                        case KeyFrameType.Expression:
                            var expressionKeyFrame = (KeyFrameAnimation<Wui.Color, Expr.Color>.ExpressionKeyFrame)kf;
                            builder.WriteLine($"{{ {Float(kf.Progress)}, {String(expressionKeyFrame.Expression)}, {CallFactoryFromFor(node, kf.Easing)} }},");
                            break;
                        case KeyFrameType.Value:
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }

                builder.CloseScopeWithSemicolon();

                WriteCreateAssignment(builder, node, $"ConfigureAnimationKeyFrames({durationAmount}, steps, _countof(steps))");
                InitializeCompositionAnimation(builder, obj, node);
                WriteCompositionObjectFactoryEnd(builder, obj, node);
                return true;
            }

            protected override bool GenerateBooleanKeyFrameAnimationFactory(CodeBuilder builder, BooleanKeyFrameAnimation obj, ObjectData node)
            {
                WriteObjectFactoryStart(builder, node);
                var keyFrames = obj.KeyFrames;
                var firstKeyFrame = keyFrames.First();
                string durationAmount = "nullptr";

                if (obj.Duration == Owner.CompositionDuration)
                {
                    durationAmount = "&c_duration";
                }

                builder.WriteLine("constexpr static const KeyFrameStep<bool> steps[] =");
                builder.OpenScope();
                foreach (var kf in keyFrames)
                {
                    WriteFrameNumberComment(builder, kf.Progress);

                    switch (kf.Type)
                    {
                        case KeyFrameType.Expression:
                            var expressionKeyFrame = (KeyFrameAnimation<bool, Expr.Boolean>.ExpressionKeyFrame)kf;
                            builder.WriteLine($"{{ {Float(kf.Progress)}, {String(expressionKeyFrame.Expression)} }},");
                            break;
                        case KeyFrameType.Value:
                            var valueKeyFrame = (KeyFrameAnimation<bool, Expr.Boolean>.ValueKeyFrame)kf;
                            builder.WriteLine($"{{ {Float(kf.Progress)}, {Bool(valueKeyFrame.Value)} }},");
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }

                builder.CloseScopeWithSemicolon();

                WriteCreateAssignment(builder, node, $"ConfigureAnimationKeyFrames({durationAmount}, steps, _countof(steps))");
                InitializeCompositionAnimation(builder, obj, node);
                WriteCompositionObjectFactoryEnd(builder, obj, node);
                return true;
            }

            protected override bool GenerateCompositionPathFactory(CodeBuilder builder, CompositionPath obj, ObjectData node)
            {
                WriteObjectFactoryStart(builder, node);
                var canvasGeometry = GetObjectData((CanvasGeometry)obj.Source);
                WriteCreateAssignment(builder, node, $"MakeCompositionPath({canvasGeometry.FactoryCall()})");
                WriteObjectFactoryEnd(builder);
                return true;
            }

            protected override bool GenerateCompositionSurfaceBrushFactory(CodeBuilder builder, CompositionSurfaceBrush obj, ObjectData node)
            {
                var surfaceNode = obj.Surface switch
                {
                    CompositionObject compositionObject => NodeFor(compositionObject),
                    Wmd.LoadedImageSurface loadedImageSurface => NodeFor(loadedImageSurface),
                    _ => null,
                };

                // Create the code that initializes the Surface.
                var surfaceInitializationText = obj.Surface switch
                {
                    CompositionObject compositionObject => CallFactoryFromFor(node, compositionObject),
                    Wmd.LoadedImageSurface _ => surfaceNode!.FieldName!,
                    null => string.Empty,
                    _ => throw new InvalidOperationException(),
                };

                var isReachableFromSurfaceNode = node.IsReachableFrom(surfaceNode);

                WriteObjectFactoryStart(builder, node);
                WriteCreateAssignment(builder, node, $"MakeSurfaceBrush({surfaceInitializationText}, {Bool(isReachableFromSurfaceNode)})");
                InitializeCompositionBrush(builder, obj, node);
                WriteCompositionObjectFactoryEnd(builder, obj, node);

                return true;
            }

            protected override void WriteDefaultFields(CodeBuilder builder, bool themed)
            {
                if (themed)
                {
                    Owner.WriteDefaultInitializedField(builder, Readonly(_s.ReferenceTypeName("CompositionPropertySet")), ThemePropertiesFieldName);
                }
            }

            protected override void WritePopulateShapesCollection(CodeBuilder builder, IList<CompositionShape> shapes, ObjectData node)
            {
                if (shapes.Any())
                {
                    builder.WriteLine("constexpr static const func_or_field<CompositionShape> shapes[] =");
                    builder.OpenScope();

                    foreach (var shape in shapes)
                    {
                        WriteShortDescriptionComment(builder, shape);
                        builder.WriteLine($"{CallFactoryFromFor(node, shape)},");
                    }

                    builder.CloseScopeWithSemicolon();
                    builder.WriteLine($"AddCompositionShapes(result, shapes, _countof(shapes));");
                }
            }

            protected override void ConfigureAnimationController(CodeBuilder builder, string localName, ref bool controllerVariableAdded, CompositionObject.Animator animator)
            {
                // If the animation has a controller, get the controller, optionally pause it, and recurse to start the animations
                // on the controller.
                if (animator.Controller is not null)
                {
                    var controller = animator.Controller;

                    builder.OpenScope();
                    builder.WriteLine($"auto controller = GetAnimationController({localName}, {String(animator.AnimatedProperty)}, {Bool(controller.IsPaused)});");

                    // Recurse to start animations on the controller.
                    StartAnimations(builder, controller, NodeFor(controller), "controller");
                    builder.CloseScope();
                }
            }

            readonly string _dotProperties = ".Properties()";

            string RemapTargetName(string target)
            {
                if (target.EndsWith(_dotProperties) && !target.StartsWith("result."))
                {
                    return $"invoke_func_or_field({target.Replace(_dotProperties, string.Empty)}).Properties()";
                }
                else
                {
                    return target;
                }
            }

            protected override void WriteAnimationStart(CodeBuilder builder, string targetName, string propertyName, string animationFactoryCall)
            {
                builder.WriteLine($"StartAnimation({RemapTargetName(targetName)}, {propertyName}, {animationFactoryCall});");
            }

            protected override void WriteDestroyAnimation(CodeBuilder builder, string localName, string propertyName)
            {
                builder.WriteLine($"StopAnimation({RemapTargetName(localName)}, L\"{propertyName}\");");
            }

            protected override void WriteProgressBoundAnimationBuild(CodeBuilder builder, string name, string property, string animationFactory, string expressionFactory)
            {
                builder.OpenScope();
                builder.WriteLine($"constexpr static const BoundAnimation anim = {{ {property}, {animationFactory}, {expressionFactory} }};");
                builder.WriteLine($"StartProgressBoundAnimation({RemapTargetName(name)}, anim);");
                builder.CloseScope();
            }

            protected override void EnsureStartProgressBoundAnimationWritten(CodeBuilder builder)
            {
                // Do nothing, we have our own
            }

            protected override void EnsureBindPropertyWritten(CodeBuilder builder)
            {
                // Do nothing, we have our own
            }

            protected override string CallCreateCubicBezierEasingFunction(CubicBezierEasingFunction obj)
            {
                return $"CreateCubicBezierEasingFunction({_generator.GetCubicBezierId(obj.ControlPoint1, obj.ControlPoint2)})";
            }

            protected override string ReferencePropertySetName() => "result.Properties()";

            protected override void WriteOptimizedFieldRead(CodeBuilder builder, ObjectData node)
            {
                // do nothing; our fields are aleady cached
            }

            protected override void WriteFields(CodeBuilder builder)
            {
                foreach (var g in Nodes.GroupBy(_ => _s.ReferenceTypeName(_.TypeName)))
                {
                    var storedType = g.Key;
                    var shortName = storedType;
                    if (shortName == "winrt::com_ptr<CanvasGeometry>")
                    {
                        shortName = "CanvasGeometry";
                    }

                    var idType = shortName + "FieldId";
                    var fields = g.Where(_ => _.RequiresReadonlyStorage || _.RequiresStorage);
                    var nonFields = g.Where(_ => !(_.RequiresReadonlyStorage || _.RequiresStorage));
                    int i = 1;
                    foreach (var k in fields)
                    {
                        builder.WriteLine($"constexpr static const func_or_field<{storedType}> {k.FieldName}Id {{ {i++} }};");
                    }

                    foreach (var k in g)
                    {
                        builder.WriteLine($"constexpr static const func_or_field<{storedType}> {k.Name}Id {{ {i++} }};");
                    }

                    builder.WriteLine($"__declspec(noinline) {g.Key} call_method(func_or_field<{storedType}> const& id) override");
                    builder.OpenScope();
                    builder.WriteLine("switch (id.id)");
                    builder.OpenScope();

                    foreach (var m in g)
                    {
                        builder.WriteLine($"case {m.Name}Id.id: return {m.Name}(); break;");
                    }

                    builder.WriteLine("default: throw std::invalid_argument(\"oops\");");
                    builder.CloseScope();
                    builder.CloseScope();
                    builder.WriteLine();

                    if (fields.Any())
                    {
                        _generator.AddConstructorLine($"m_{shortName}Storage.resize({fields.Count()}, nullptr);");
                    }
                }
            }

            protected override string? FieldReadExpression(ObjectData node)
            {
                if (node.FieldName == null)
                {
                    return null;
                }

                var typeName = _s.ReferenceTypeName(node.TypeName);
                return $"{node.FieldName}Id";
            }

            protected override string FieldWriteExpression(ObjectData node, string value)
            {
                var typeName = _s.ReferenceTypeName(node.TypeName);
                return $"store_field({node.FieldName}Id, {value})";
            }

            protected override bool GenerateCompositionEffectFactory(CodeBuilder builder, CompositionEffectFactory obj, ObjectData node)
            {
                WriteObjectFactoryStart(builder, node);

                if (obj.Effect.Type == GraphicsEffectType.CompositeEffect)
                {
                    var effect = (CompositeEffect)obj.Effect;

                    if (effect.Sources.Any())
                    {
                        builder.WriteLine("constexpr static const wchar_t* effects[] =");
                        builder.OpenScope();

                        foreach (var source in effect.Sources)
                        {
                            builder.WriteLine($"L\"{source.Name}\",");
                        }

                        builder.CloseScopeWithSemicolon();
                        builder.WriteLine("constexpr static const int effectCount = _countof(effects);");
                    }
                    else
                    {
                        builder.WriteLine("constexpr static const wchar_t* effects = nullptr;");
                        builder.WriteLine("constexpr static const int effectCount = 0;");
                    }

                    WriteCreateAssignment(builder, node, $"CompositeEffect::Make(_c, {_s.CanvasCompositeMode(effect.Mode)}, effects, effectCount)");
                }
                else if (obj.Effect.Type == GraphicsEffectType.GaussianBlurEffect)
                {
                    var effect = (Mgce.GaussianBlurEffect)obj.Effect;
                    WriteCreateAssignment(builder, node, $"GaussianBlurEffect::Make(_c, {Float(effect.BlurAmount)}, L\"{effect.Sources.First().Name}\")");
                }
                else
                {
                    throw new InvalidOperationException();
                }

                WriteCompositionObjectFactoryEnd(builder, obj, node);

                return true;
            }
        }
    }
}