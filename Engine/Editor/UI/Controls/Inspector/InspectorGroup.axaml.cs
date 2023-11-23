﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AP = Avalonia.AvaloniaProperty;

// ReSharper disable UnusedParameter.Local
namespace Friflo.Fliox.Editor.UI.Controls.Inspector;

public partial class InspectorGroup : UserControl
{
    private List<InspectorComponent> components;
    
    public static readonly StyledProperty<string>       GroupProperty   = AP.Register<InspectorGroup, string>       (nameof(Group), "group");
    public static readonly StyledProperty<InputElement> ExpandProperty  = AP.Register<InspectorGroup, InputElement> (nameof(Expand));
    
    public string   Group   { get => GetValue(GroupProperty);   set => SetValue(GroupProperty,  value); }
    public InputElement   Expand {
        get => GetValue(ExpandProperty);
        set => SetValue(ExpandProperty, value);
    }

    public InspectorGroup()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e) {
        base.OnLoaded(e);
        
        PointerEntered += (sender, args) => { Add.Classes.Add("GreenButton"); };
        PointerExited += (sender, args)  => { Add.Classes.Remove("GreenButton"); };
        if (Expand != null) {
            Expand.PointerEntered += (sender, args) => { Add.Classes.Add("GreenButton"); };
            Expand.PointerExited += (sender, args)  => { Add.Classes.Remove("GreenButton"); };
        }
    }

    private void Button_OnClick(object sender, RoutedEventArgs e) {
        components ??= new List<InspectorComponent>();
        EditorUtils.GetControls(Expand, components);
        var expanded = true;
        foreach (var component in components) {
            expanded &= component.Expanded;
        }
        foreach (var component in components) {
            component.Expanded = !expanded;
        }
    }
}
