﻿using DynamicData;
using DynamicData.Binding;
using Synthesis.Bethesda.Execution.Settings;
using Newtonsoft.Json;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using System.Threading.Tasks;
using Noggog;

namespace Synthesis.Bethesda.GUI
{
    public class MainVM : ViewModel
    {
        public ConfigurationVM Configuration { get; }

        [Reactive]
        public ViewModel ActivePanel { get; set; }

        public ICommand ConfirmActionCommand { get; }
        public ICommand DiscardActionCommand { get; }

        [Reactive]
        public ConfirmationActionVM? ActiveConfirmation { get; set; }

        // Whether to show red glow in background
        private readonly ObservableAsPropertyHelper<bool> _Hot;
        public bool Hot => _Hot.Value;

        public MainVM()
        {
            Configuration = new ConfigurationVM(this);
            ActivePanel = Configuration;
            DiscardActionCommand = ReactiveCommand.Create(() => ActiveConfirmation = null);
            ConfirmActionCommand = ReactiveCommand.Create(
                () =>
                {
                    if (ActiveConfirmation == null) return;
                    ActiveConfirmation.ToDo();
                    ActiveConfirmation = null;
                });

            _Hot = this.WhenAnyValue(x => x.ActivePanel)
                .Select(x =>
                {
                    if (x is ConfigurationVM config)
                    {
                        return config.WhenAnyFallback(x => x.CurrentRun!.Running, fallback: false);
                    }
                    return Observable.Return(false);
                })
                .Switch()
                .DistinctUntilChanged()
                .ToGuiProperty(this, nameof(Hot));

            Task.Run(() => Mutagen.Bethesda.WarmupAll.Init()).FireAndForget();
        }

        public void Load(SynthesisSettings? settings)
        {
            if (settings == null) return;
            Configuration.Load(settings);
        }

        public SynthesisSettings Save() => Configuration.Save();

        public void Init()
        {
            if (Configuration.Profiles.Count == 0)
            {
                ActivePanel = new NoProfileVM(this.Configuration);
            }
        }
    }
}
