﻿using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Editor.UI;

public partial class ExplorerPanel : UserControl, IEditorControl
{
    public Editor Editor { get; private set; }
    
    public ExplorerPanel()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e) {
        base.OnLoaded(e);
        Editor = this.GetEditor();
        Editor.OnReady += () => {
        };
    }

    public void OnButtonClick(object sender, RoutedEventArgs routedEventArgs)
    {
        Console.WriteLine("Click");
    }
}