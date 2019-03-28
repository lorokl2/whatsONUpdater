﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Soloplan GmbH">
//  Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI
{
  using System.Collections.Generic;
  using System.Windows;
  using Soloplan.WhatsON.GUI.Config.View;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow
  {
    /// <summary>
    /// The configuration.
    /// </summary>
    private ApplicationConfiguration config;

    private ObservationScheduler scheduler;

    public MainWindow(ObservationScheduler scheduler, ApplicationConfiguration configuration, IList<Subject> initialSubjectState)
    {
      this.InitializeComponent();
      this.scheduler = scheduler;
      this.config = configuration;
      this.mainTreeView.Init(this.scheduler, this.config, initialSubjectState);
    }

    private void OpenConfig(object sender, RoutedEventArgs e)
    {
      var configWindow = new ConfigWindow(this.config);
      configWindow.Owner = this;
      configWindow.ConfigurationApplied += (s, ev) =>
      {
        this.config = ev.Value;

        this.ApplyConfiguration();
      };

      configWindow.ShowDialog();
    }

    private void ApplyConfiguration()
    {
      var schedulerRunning = this.scheduler.Running;
      if (this.scheduler.Running)
      {
        this.scheduler.Stop();
      }

      this.scheduler.UnobserveAll();
      foreach (var subjectConfiguration in this.config.SubjectsConfiguration)
      {
        var subject = PluginsManager.Instance.GetSubject(subjectConfiguration);
        this.scheduler.Observe(subject);
      }

      this.mainTreeView.Update(this.config);

      if (schedulerRunning)
      {
        this.scheduler.Start();
      }
    }
  }
}
