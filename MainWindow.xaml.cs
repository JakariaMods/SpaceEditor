﻿using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SpaceEditor.Controls;
using SpaceEditor.Data;

namespace SpaceEditor;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    private Settings Settings => Settings.Default;

    public string GamePath
    {
        get => this.Settings.GamePath;
        set
        {
            this.Settings.GamePath = value;
            OnPropertyChanged();
            Reload(default!, default!);
        }
    }

    public int Theme
    {
        get => this.Settings.Theme;
        set
        {
            if (this.Settings.Theme == value)
                return;

            this.Settings.Theme = value;
            OnPropertyChanged();
            OnThemeChanged(value);
        }
    }

    private List<ResourceDictionary> _themes = new();
    private ResourceDictionary _globalTheme;

    public MainWindow()
    {
        this.DataContext = this;
        this.Loaded += Reload;
        InitializeComponent();

        var resources = Application.Current.Resources.MergedDictionaries;
        //Theme index is determined in App.xaml
        for (int i = 0; i < resources.Count - 1; i++)
        {
            _themes.Add(resources[i]);
        }

        _globalTheme = resources.Last();
        OnThemeChanged(this.Settings.Theme);
    }

    private async void Reload(object sender, RoutedEventArgs e)
    {
        var tabs = this.MainTabs.Items;
        while (tabs.Count > 1)
        {
            tabs.RemoveAt(tabs.Count - 1);
        }

        if (string.IsNullOrWhiteSpace(this.GamePath))
        {
            this.InfoText.Text = $"First, please show me where is the Game installed by pressing the '{LocateGameButton.Content.ToString()}' button";
            return;
        }

        this.InfoText.Text = "Loading ...";
        try
        {
            var game = await Task.Run(async () =>
            {
                var game = new GameProxy(this.GamePath);
                _ = await game.InputActions;
                _ = await game.InputIds;
                return game;
            });

            var propertyGridFactoryKey = "CompositePropertyGridControlFactory";
            this.Resources.Remove(propertyGridFactoryKey);
            this.Resources.Add(propertyGridFactoryKey, new CompositePropertyGridControlFactory
            {
                Factories =
                {
                    new InputIdControlsFactory
                    {
                        InputIds = await game.InputIds
                    }
                }
            });

            tabs.Add(new TabItem
            {
                Header = "Key Binds",
                Content = new KeyBindsEditor(game)
            });

            var sb = new StringBuilder();
            sb.AppendLine("Loading finished");
            sb.AppendLine();
            
            sb.AppendLine("Main Assembly:");
            var gameExe = game.MainAssembly;
            sb.AppendLine($"{gameExe.GetName().Name}");
            sb.AppendLine($"{gameExe.GetName().Version}");
            sb.AppendLine($"{gameExe.Location}");

            sb.AppendLine();
            sb.AppendLine("Use Tabs above to access individual features");
            
            this.InfoText.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            this.InfoText.Text = "Exception happened during initial loading:" + Environment.NewLine + ex;
        }
    }

    private void OnLocateGame(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = "Select Space Engineers 2's Directory"
        };

        var found = dialog.ShowDialog(this);
        if (found != true)
            return;

        this.GamePath = dialog.FolderName;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnThemeChanged(int themeIndex)
    {
        var resources = Application.Current.Resources.MergedDictionaries;
        resources.Clear();
        resources.Add(_themes[themeIndex]);
        resources.Add(_globalTheme);
    }
}