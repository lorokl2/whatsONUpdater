﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Soloplan GmbH">
//  Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Media.Animation;
  using MaterialDesignThemes.Wpf;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.GUI.Config.View;
  using Soloplan.WhatsON.GUI.Config.ViewModel;
  using Soloplan.WhatsON.GUI.Config.Wizard;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : INotifyPropertyChanged
  {
    private readonly IList<Connector> initialConnectorState;

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    private ApplicationConfiguration config;

    /// <summary>
    /// The scheduler used for observing connectors.
    /// </summary>
    private ObservationScheduler scheduler;

    /// <summary>
    /// App settings.
    /// </summary>
    private MainWindowSettigns settings;

    private bool initialized;

    /// <summary>
    /// Backing field for <see cref="ConfigurationModifiedFromTree"/>.
    /// </summary>
    private bool configurationModifiedFromTree;

    /// <summary>
    /// Occurs when configuration was applied.
    /// </summary>
    public event EventHandler<ValueEventArgs<ApplicationConfiguration>> ConfigurationApplied;

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="scheduler">The scheduler observing build servers.</param>
    /// <param name="configuration">App configuration.</param>
    /// <param name="initialConnectorState">The initial state of observed connectors.</param>
    public MainWindow(ObservationScheduler scheduler, ApplicationConfiguration configuration, IList<Connector> initialConnectorState)
    {
      this.InitializeComponent();
      this.scheduler = scheduler;
      this.config = configuration;
      this.initialConnectorState = initialConnectorState;
      this.ShowInTaskbar = this.config.ShowInTaskbar;
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ShowInTaskbar)));
      this.Topmost = this.config.AlwaysOnTop;
      this.DataContext = this;
      this.mainTreeView.ConfigurationChanged += this.MainTreeViewOnConfigurationChanged;
      this.mainTreeView.EditItem += this.EditTreeItem;
    }

    public bool IsTreeInitialized
    {
      get => this.initialized;
      set
      {
        this.initialized = value;
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsTreeInitialized)));
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether configuration was modified from tree view.
    /// </summary>
    public bool ConfigurationModifiedFromTree
    {
      get => this.configurationModifiedFromTree;
      set
      {
        this.configurationModifiedFromTree = value;
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ConfigurationModifiedFromTree)));
      }
    }

    public MainWindowSettigns GetVisualSettigns()
    {
      this.settings.TreeListSettings = this.mainTreeView.GetTreeListSettings();
      this.settings.MainWindowDimensions = new WindowSettings().Parse(this);

      return this.settings;
    }

    public void ApplyVisualSettings(MainWindowSettigns visualSettings)
    {
      this.settings = visualSettings ?? new MainWindowSettigns();

      if (this.settings.MainWindowDimensions != null)
      {
        this.settings.MainWindowDimensions.Apply(this);
      }
      else
      {
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
      }
    }

    /// <summary>
    /// Applies configuration to main window.
    /// </summary>
    /// <param name="configuration">New configuration.</param>
    public void ApplyConfiguration(ApplicationConfiguration configuration)
    {
      this.config = configuration;
      this.mainTreeView.Update(this.config);
      this.ShowInTaskbar = this.config.ShowInTaskbar;
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ShowInTaskbar)));
      this.Topmost = this.config.AlwaysOnTop;
    }

    public void FinishDrawing()
    {
      this.mainTreeView.Init(this.scheduler, this.config, this.initialConnectorState);
      this.mainTreeView.ApplyTreeListSettings(this.settings.TreeListSettings);
    }

    /// <summary>
    /// Focuses the node connected with <paramref name="connector>.
    /// </summary>
    /// <param name="connector">Connector which should be focused.</param>
    public void FocusConnector(Connector connector)
    {
      this.mainTreeView.FocusItem(connector);
    }

    private void OpenConfig(object sender, EventArgs e)
    {
      if (this.ConfigurationModifiedFromTree)
      {
        return;
      }

      var configWindow = new ConfigWindow(this.config);
      configWindow.Owner = this;
      configWindow.ConfigurationApplied += (s, ev) =>
      {
        this.ConfigurationApplied?.Invoke(this, ev);
      };

      if (this.settings.ConfigDialogSettings != null)
      {
        this.settings.ConfigDialogSettings.Apply(configWindow);
      }
      else
      {
        configWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
      }

      configWindow.Closing += (s, ev) =>
      {
        this.settings.ConfigDialogSettings = new WindowSettings().Parse(configWindow);
      };

      configWindow.ShowDialog();
    }

    /// <summary>
    /// Handles the Click event of the add new connector button control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void NewConnectorClick(object sender, RoutedEventArgs e)
    {
      var wizardController = new WizardController(this);
      var result = false;
      try
      {
        result = wizardController.Start(this.config);
      }
      finally
      {
        if (result)
        {
          this.ConfigurationApplied?.Invoke(this, new ValueEventArgs<ApplicationConfiguration>(this.config));
        }
      }
    }

    private void MainTreeViewOnConfigurationChanged(object sender, EventArgs e)
    {
      var showAnimation = !this.ConfigurationModifiedFromTree;
      this.ConfigurationModifiedFromTree = true;
      if (showAnimation)
      {
        if (this.FindResource("showStoryBoard") is Storyboard sb)
        {
          this.BeginStoryboard(sb);
        }
      }
    }

    private void ResetClick(object sender, RoutedEventArgs e)
    {
      this.HideChangesPanel();
      this.ConfigurationApplied?.Invoke(this, new ValueEventArgs<ApplicationConfiguration>(this.config));
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
      this.HideChangesPanel();

      this.mainTreeView.WriteToConfiguration(this.config);
      SerializationHelper.SaveConfiguration(this.config);
      this.ConfigurationApplied?.Invoke(this, new ValueEventArgs<ApplicationConfiguration>(this.config));
    }

    private void HideChangesPanel()
    {
      if (this.FindResource("hideStorBoard") is Storyboard sb)
      {
        void ChangePanelVisibility(object sender, EventArgs eventArgs)
        {
          this.ConfigurationModifiedFromTree = false;
          sb.Completed -= ChangePanelVisibility;
        }

        sb.Completed += ChangePanelVisibility;

        this.BeginStoryboard(sb);
      }
      else
      {
        this.ConfigurationModifiedFromTree = false;
      }
    }

    /// <summary>
    /// Handles creation of new group.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">Event args.</param>
    private async void NewGroupClick(object sender, RoutedEventArgs e)
    {
      var model = new GroupViewModel
      {
        AlreadyUsedNames = this.mainTreeView.GetGroupNames(),
      };

      var editGroupDialog = new EditGroupNameDialog(model);
      var result = await this.ShowDialogOnPageHost(editGroupDialog);
      if (result)
      {
        this.mainTreeView.CreateGroup(model.Name);
      }
    }

    /// <summary>
    /// Handles editing of tree item.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">Event args.</param>
    private async void EditTreeItem(object sender, ValueEventArgs<Common.ConnectorTreeView.TreeItemViewModel> e)
    {
      if (e.Value is ConnectorGroupViewModel groupTreeViewModel)
      {
        var model = new GroupViewModel
        {
          Name = groupTreeViewModel.GroupName,
          AlreadyUsedNames = this.mainTreeView.GetGroupNames().Where(grpName => grpName != groupTreeViewModel.GroupName).ToList(),
        };
        var editGroupDialog = new EditGroupNameDialog(model);
        var result = await this.ShowDialogOnPageHost(editGroupDialog);
        if (result && groupTreeViewModel.GroupName != model.Name)
        {
          groupTreeViewModel.GroupName = model.Name;
          this.ConfigurationModifiedFromTree = true;
        }
      }
    }

    private async Task<bool> ShowDialogOnPageHost(UserControl dialog)
    {
      var tmpResult = await DialogHost.Show(dialog, "MainWindowPageHost");
      if (tmpResult is bool result)
      {
        return result;
      }

      return false;
    }
  }
}
