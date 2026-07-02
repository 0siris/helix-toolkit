// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ViewportCommands.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Windows.Input;

namespace HelixToolkit.Wpf.SharpDX;

public static class ViewportCommands
{
    public static RoutedCommand Zoom { get; } = new();

    public static RoutedCommand ZoomExtents { get; } = new();

    public static RoutedCommand ZoomRectangle { get; } = new();

    public static RoutedCommand Pan { get; } = new();

    public static RoutedCommand Rotate { get; } = new();

    public static RoutedCommand SetTarget { get; } = new();

    public static RoutedCommand Reset { get; } = new();

    public static RoutedCommand ChangeFieldOfView { get; } = new();

    public static RoutedCommand BackView { get; } = new();

    public static RoutedCommand FrontView { get; } = new();

    public static RoutedCommand TopView { get; } = new();

    public static RoutedCommand BottomView { get; } = new();

    public static RoutedCommand LeftView { get; } = new();

    public static RoutedCommand RightView { get; } = new();
}