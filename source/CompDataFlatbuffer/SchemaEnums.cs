// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace CommunityToolkit.WinUI.Lottie.CompDataFlatbuffer
{
    /// <summary>
    /// Identifies the node vector that a packed object reference points into.
    /// Must be kept in sync with the ObjectCategory enum in lottie_comp.fbs.
    /// </summary>
    enum ObjectCategory : byte
    {
        Visual = 0,
        Shape = 1,
        Geometry = 2,
        Brush = 3,
        Animation = 4,
        Easing = 5,
        PropertySet = 6,
        Surface = 7,
        Clip = 8,
        Controller = 9,
        Shadow = 10,
        Effect = 11,
        GradientStop = 12,
        ViewBox = 13,
    }

    enum VisualKind : byte
    {
        Container = 0,
        Sprite = 1,
        Shape = 2,
        Layer = 3,
    }

    enum ShapeKind : byte
    {
        Container = 0,
        Sprite = 1,
    }

    enum GeometryKind : byte
    {
        Path = 0,
        Rectangle = 1,
        RoundedRectangle = 2,
        Ellipse = 3,
    }

    enum BrushKind : byte
    {
        Color = 0,
        LinearGradient = 1,
        RadialGradient = 2,
        Surface = 3,
        Mask = 4,
        Effect = 5,
    }

    enum ClipKind : byte
    {
        Inset = 0,
        Geometric = 1,
    }

    enum ShadowKind : byte
    {
        Drop = 0,
    }

    enum SurfaceKind : byte
    {
        VisualSurface = 0,
        LoadedImageFromUri = 1,
        LoadedImageFromStream = 2,
    }

    enum EffectKind : byte
    {
        Composite = 0,
        GaussianBlur = 1,
    }

    enum EasingKind : byte
    {
        Linear = 0,
        CubicBezier = 1,
        Step = 2,
    }

    enum AnimationKind : byte
    {
        Scalar = 0,
        Vector2 = 1,
        Vector3 = 2,
        Vector4 = 3,
        Color = 4,
        Boolean = 5,
        Path = 6,
        Expression = 7,
    }

    enum KeyFrameKind : byte
    {
        Value = 0,
        Expression = 1,
    }

    enum CanvasGeometryKind : byte
    {
        Combination = 0,
        Ellipse = 1,
        Group = 2,
        Path = 3,
        RoundedRectangle = 4,
        TransformedGeometry = 5,
    }

    /// <summary>
    /// The opcodes of the flattened CanvasPathBuilder command stream.
    /// </summary>
    enum PathOp : byte
    {
        /// <summary>Consumes 2 operands: x, y.</summary>
        BeginFigure = 0,

        /// <summary>Consumes 1 operand: the CanvasFigureLoop.</summary>
        EndFigure = 1,

        /// <summary>Consumes 2 operands: x, y.</summary>
        AddLine = 2,

        /// <summary>Consumes 6 operands: cp1.x, cp1.y, cp2.x, cp2.y, end.x, end.y.</summary>
        AddCubicBezier = 3,
    }
}
