using Mutagen.Bethesda;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using CommandLine;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using Noggog.Utility;
using Mutagen.Bethesda.Synthesis.CLI;

namespace Synthesis.Bethesda.Execution
{
    public class CliPatcherRun : IPatcherRun
    {
        public string Name { get; }

        private readonly Subject<string> _output = new Subject<string>();
        public IObservable<string> Output => _output;

        private readonly Subject<string> _error = new Subject<string>();
        public IObservable<string> Error => _error;

        public string PathToExecutable;

        public string? PathToExtraData;

        public CliPatcherRun(
            string nickname,
            string pathToExecutable, 
            string? pathToExtra)
        {
            Name = nickname;
            PathToExecutable = pathToExecutable;
            PathToExtraData = pathToExtra;
        }

        public void Dispose()
        {
        }

        public async Task Prep(GameRelease release, CancellationToken? cancel = null)
        {
        }

        public async Task Run(RunSynthesisPatcher settings, CancellationToken? cancel = null)
        {
            if (cancel?.IsCancellationRequested ?? false) return;

            var internalSettings = RunSynthesisMutagenPatcher.Factory(settings);
            internalSettings.ExtraDataFolder = PathToExtraData;

            var args = Parser.Default.FormatCommandLine(internalSettings);
            try
            {
                using ProcessWrapper process = ProcessWrapper.Start(
                    new ProcessStartInfo(PathToExecutable, args)
                    {
                        WorkingDirectory = Path.GetDirectoryName(PathToExecutable)
                    },
                    cancel);
                using var outputSub = process.Output.Subscribe(_output);
                using var errSub = process.Error.Subscribe(_error);
                var result = await process.Start();
                if (result != 0)
                {
                    throw new CliUnsuccessfulRunException(
                        result,
                        $"Process exited in failure: {process.StartInfo.FileName} {internalSettings}");
                }
            }
            catch (Win32Exception ex)
            {
                throw new FileNotFoundException($"Could not find target CLI file: {PathToExecutable}", ex);
            }
        }
    }
}
