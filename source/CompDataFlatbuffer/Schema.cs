// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace CommunityToolkit.WinUI.Lottie.CompDataFlatbuffer
{
    /// <summary>
    /// Field ids, enum values and constants of the lottie_comp.fbs schema.
    /// These must be kept in sync with lottie_comp.fbs; field ids are the
    /// 0-based declaration order of the fields of each table.
    /// </summary>
    static class Schema
    {
        /// <summary>
        /// The version of the schema that this code reads and writes. Bumped for
        /// every additive change to lottie_comp.fbs.
        /// </summary>
        internal const ushort Version = 1;

        /// <summary>
        /// The 4 character identifier stored at offset 4 of every buffer.
        /// </summary>
        internal const string FileIdentifier = "LCMP";

        /// <summary>
        /// The file extension used for serialized compositions.
        /// </summary>
        internal const string FileExtension = ".lcomp";

        /// <summary>
        /// The value of an index or reference field that refers to nothing.
        /// </summary>
        internal const uint NullIndex = 0xFFFFFFFF;

        /// <summary>
        /// The number of bits used to store the index part of an object reference.
        /// </summary>
        internal const int ObjectReferenceIndexBits = 28;

        internal static uint PackObjectReference(ObjectCategory category, int index)
            => ((uint)category << ObjectReferenceIndexBits) | (uint)index;

        internal static ObjectCategory UnpackCategory(uint reference)
            => (ObjectCategory)(reference >> ObjectReferenceIndexBits);

        internal static int UnpackIndex(uint reference)
            => (int)(reference & ((1u << ObjectReferenceIndexBits) - 1));

        internal static class Animator
        {
            internal const int FieldCount = 3;
            internal const int Property = 0;
            internal const int Animation = 1;
            internal const int Controller = 2;
        }

        internal static class CompObj
        {
            internal const int FieldCount = 3;
            internal const int Comment = 0;
            internal const int Animators = 1;
            internal const int Properties = 2;
        }

        internal static class PropertyValue
        {
            internal const int FieldCount = 5;
            internal const int Name = 0;
            internal const int Type = 1;
            internal const int Scalar = 2;
            internal const int Vector = 3;
            internal const int Color = 4;
        }

        internal static class ReferenceParameter
        {
            internal const int FieldCount = 2;
            internal const int Name = 0;
            internal const int Target = 1;
        }

        internal static class SourceParameter
        {
            internal const int FieldCount = 2;
            internal const int Name = 0;
            internal const int Brush = 1;
        }

        internal static class KeyFrame
        {
            internal const int FieldCount = 8;
            internal const int Progress = 0;
            internal const int Easing = 1;
            internal const int Kind = 2;
            internal const int Expression = 3;
            internal const int Scalar = 4;
            internal const int Vector = 5;
            internal const int Color = 6;
            internal const int Path = 7;
        }

        internal static class Visual
        {
            internal const int FieldCount = 18;
            internal const int Base = 0;
            internal const int Kind = 1;
            internal const int BorderMode = 2;
            internal const int CenterPoint = 3;
            internal const int Clip = 4;
            internal const int IsVisible = 5;
            internal const int Offset = 6;
            internal const int Opacity = 7;
            internal const int RotationAngleInDegrees = 8;
            internal const int RotationAxis = 9;
            internal const int Scale = 10;
            internal const int Size = 11;
            internal const int TransformMatrix = 12;
            internal const int Children = 13;
            internal const int Shapes = 14;
            internal const int ViewBox = 15;
            internal const int Brush = 16;
            internal const int Shadow = 17;
        }

        internal static class Shape
        {
            internal const int FieldCount = 20;
            internal const int Base = 0;
            internal const int Kind = 1;
            internal const int CenterPoint = 2;
            internal const int Offset = 3;
            internal const int RotationAngleInDegrees = 4;
            internal const int Scale = 5;
            internal const int TransformMatrix = 6;
            internal const int Shapes = 7;
            internal const int FillBrush = 8;
            internal const int StrokeBrush = 9;
            internal const int Geometry = 10;
            internal const int IsStrokeNonScaling = 11;
            internal const int StrokeDashOffset = 12;
            internal const int StrokeDashArray = 13;
            internal const int StrokeDashCap = 14;
            internal const int StrokeStartCap = 15;
            internal const int StrokeEndCap = 16;
            internal const int StrokeLineJoin = 17;
            internal const int StrokeMiterLimit = 18;
            internal const int StrokeThickness = 19;
        }

        internal static class Geometry
        {
            internal const int FieldCount = 11;
            internal const int Base = 0;
            internal const int Kind = 1;
            internal const int TrimStart = 2;
            internal const int TrimEnd = 3;
            internal const int TrimOffset = 4;
            internal const int Path = 5;
            internal const int Offset = 6;
            internal const int Size = 7;
            internal const int CornerRadius = 8;
            internal const int Center = 9;
            internal const int Radius = 10;
        }

        internal static class Brush
        {
            internal const int FieldCount = 23;
            internal const int Base = 0;
            internal const int Kind = 1;
            internal const int Color = 2;
            internal const int AnchorPoint = 3;
            internal const int CenterPoint = 4;
            internal const int ColorStops = 5;
            internal const int ExtendMode = 6;
            internal const int InterpolationSpace = 7;
            internal const int MappingMode = 8;
            internal const int Offset = 9;
            internal const int RotationAngleInDegrees = 10;
            internal const int Scale = 11;
            internal const int TransformMatrix = 12;
            internal const int StartPoint = 13;
            internal const int EndPoint = 14;
            internal const int EllipseCenter = 15;
            internal const int EllipseRadius = 16;
            internal const int GradientOriginOffset = 17;
            internal const int Surface = 18;
            internal const int Source = 19;
            internal const int Mask = 20;
            internal const int Effect = 21;
            internal const int SourceParameters = 22;
        }

        internal static class GradientStop
        {
            internal const int FieldCount = 3;
            internal const int Base = 0;
            internal const int Color = 1;
            internal const int Offset = 2;
        }

        internal static class ViewBox
        {
            internal const int FieldCount = 2;
            internal const int Base = 0;
            internal const int Size = 1;
        }

        internal static class Clip
        {
            internal const int FieldCount = 9;
            internal const int Base = 0;
            internal const int Kind = 1;
            internal const int CenterPoint = 2;
            internal const int Scale = 3;
            internal const int LeftInset = 4;
            internal const int RightInset = 5;
            internal const int TopInset = 6;
            internal const int BottomInset = 7;
            internal const int Geometry = 8;
        }

        internal static class Shadow
        {
            internal const int FieldCount = 8;
            internal const int Base = 0;
            internal const int Kind = 1;
            internal const int BlurRadius = 2;
            internal const int Color = 3;
            internal const int Mask = 4;
            internal const int Offset = 5;
            internal const int Opacity = 6;
            internal const int SourcePolicy = 7;
        }

        internal static class Surface
        {
            internal const int FieldCount = 7;
            internal const int Base = 0;
            internal const int Kind = 1;
            internal const int SourceVisual = 2;
            internal const int SourceSize = 3;
            internal const int SourceOffset = 4;
            internal const int Uri = 5;
            internal const int Bytes = 6;
        }

        internal static class Effect
        {
            internal const int FieldCount = 5;
            internal const int Base = 0;
            internal const int Kind = 1;
            internal const int Sources = 2;
            internal const int Mode = 3;
            internal const int BlurAmount = 4;
        }

        internal static class Easing
        {
            internal const int FieldCount = 9;
            internal const int Base = 0;
            internal const int Kind = 1;
            internal const int ControlPoint1 = 2;
            internal const int ControlPoint2 = 3;
            internal const int StepCount = 4;
            internal const int InitialStep = 5;
            internal const int FinalStep = 6;
            internal const int IsInitialStepSingleFrame = 7;
            internal const int IsFinalStepSingleFrame = 8;
        }

        internal static class Animation
        {
            internal const int FieldCount = 8;
            internal const int Base = 0;
            internal const int Kind = 1;
            internal const int Target = 2;
            internal const int ReferenceParameters = 3;
            internal const int Expression = 4;
            internal const int DurationTicks = 5;
            internal const int KeyFrames = 6;
            internal const int InterpolationColorSpace = 7;
        }

        internal static class PropertySet
        {
            internal const int FieldCount = 3;
            internal const int Base = 0;
            internal const int Owner = 1;
            internal const int Values = 2;
        }

        internal static class Controller
        {
            internal const int FieldCount = 5;
            internal const int Base = 0;
            internal const int TargetObject = 1;
            internal const int TargetProperty = 2;
            internal const int IsPaused = 3;
            internal const int IsCustom = 4;
        }

        internal static class PropertyBinding
        {
            internal const int FieldCount = 7;
            internal const int Name = 0;
            internal const int DisplayName = 1;
            internal const int ActualType = 2;
            internal const int ExposedType = 3;
            internal const int DefaultScalar = 4;
            internal const int DefaultVector = 5;
            internal const int DefaultColor = 6;
        }

        internal static class Marker
        {
            internal const int FieldCount = 3;
            internal const int Name = 0;
            internal const int Progress = 1;
            internal const int DurationProgress = 2;
        }

        internal static class Metadata
        {
            internal const int FieldCount = 8;
            internal const int Name = 0;
            internal const int Width = 1;
            internal const int Height = 2;
            internal const int DurationTicks = 3;
            internal const int FramesPerSecond = 4;
            internal const int PropertyBindings = 5;
            internal const int Markers = 6;
            internal const int ThemingPropertySet = 7;
        }

        internal static class Composition
        {
            internal const int FieldCount = 21;
            internal const int SchemaVersion = 0;
            internal const int RequiredUapVersion = 1;
            internal const int Metadata = 2;
            internal const int RootVisual = 3;
            internal const int Strings = 4;
            internal const int Visuals = 5;
            internal const int Shapes = 6;
            internal const int Geometries = 7;
            internal const int CanvasGeometries = 8;
            internal const int Brushes = 9;
            internal const int GradientStops = 10;
            internal const int ViewBoxes = 11;
            internal const int Clips = 12;
            internal const int Shadows = 13;
            internal const int Surfaces = 14;
            internal const int Effects = 15;
            internal const int Easings = 16;
            internal const int Animations = 17;
            internal const int PropertySets = 18;
            internal const int Controllers = 19;
            internal const int CustomControllers = 20;
        }

        internal static class CanvasGeometry
        {
            internal const int FieldCount = 16;
            internal const int Kind = 0;
            internal const int A = 1;
            internal const int B = 2;
            internal const int CombineMode = 3;
            internal const int Matrix = 4;
            internal const int Source = 5;
            internal const int Geometries = 6;
            internal const int FillRule = 7;
            internal const int Ops = 8;
            internal const int Operands = 9;
            internal const int X = 10;
            internal const int Y = 11;
            internal const int W = 12;
            internal const int H = 13;
            internal const int RadiusX = 14;
            internal const int RadiusY = 15;
        }
    }
}
