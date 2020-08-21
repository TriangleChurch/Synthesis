﻿using CommandLine;
using Mutagen.Bethesda.Internals;
using Mutagen.Bethesda.Synthesis.Internal;
using Noggog;
using Synthesis.Bethesda;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wabbajack.Common;

namespace Mutagen.Bethesda.Synthesis
{
    /// <summary>
    /// Bootstrapper API for creating a Mutagen-based patch from CLI arguments or PatcherRunSettings.<br />
    /// Note that you do not have to use these systems to be Synthesis compliant.  This system serves
    /// as a quick bootstrapper for some of the typical setup tasks and informational queries.
    /// </summary>
    public class SynthesisPipeline
    {
        // We want to have this be a static singleton instance, as this allows us to 
        // eventually move the convenience functions out of this library, but still
        // latch on with the same API via extension functions.

        public static readonly SynthesisPipeline Instance = new SynthesisPipeline();

        public delegate void PatcherFunction<TMod, TModGetter>(SynthesisState<TMod, TModGetter> state)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter;

        public delegate Task AsyncPatcherFunction<TMod, TModGetter>(SynthesisState<TMod, TModGetter> state)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter;

        #region Patch
        /// <summary>
        /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
        /// </summary>
        /// <typeparam name="TMod">Setter mod interface</typeparam>
        /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
        /// <param name="args">Main command line args</param>
        /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
        /// <param name="userPreferences">Any custom user preferences</param>
        /// <returns>Int error code of the operation</returns>
        public async Task<int> Patch<TMod, TModGetter>(
            string[] args,
            AsyncPatcherFunction<TMod, TModGetter> patcher,
            UserPreferences? userPreferences = null)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter
        {
            return await Parser.Default.ParseArguments(args, typeof(RunSynthesisPatcher))
                .MapResult(
                    async (RunSynthesisPatcher settings) =>
                    {
                        try
                        {
                            System.Console.WriteLine(settings.ToString());
                            await Patch(
                                settings,
                                patcher,
                                userPreferences);
                        }
                        catch (Exception ex)
                        {
                            System.Console.Error.WriteLine(ex);
                            return -1;
                        }
                        return 0;
                    },
                    async _ =>
                    {
                        var prefs = userPreferences ?? new UserPreferences();
                        if (prefs.ActionsForEmptyArgs == null) return -1;
                        try
                        {
                            await Patch(
                                GetDefaultRun(prefs.ActionsForEmptyArgs.IdentifyingModKey, prefs.ActionsForEmptyArgs.TargetRelease),
                                patcher,
                                prefs);
                        }
                        catch (Exception ex)
                        {
                            System.Console.Error.WriteLine(ex);
                            return -1;
                        }
                        return 0;
                    });
        }

        /// <summary>
        /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
        /// </summary>
        /// <typeparam name="TMod">Setter mod interface</typeparam>
        /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
        /// <param name="args">Main command line args</param>
        /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
        /// <param name="userPreferences">Any custom user preferences</param>
        /// <returns>Int error code of the operation</returns>
        public int Patch<TMod, TModGetter>(
            string[] args,
            PatcherFunction<TMod, TModGetter> patcher,
            UserPreferences? userPreferences = null)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter
        {
            return Parser.Default.ParseArguments(args, typeof(RunSynthesisPatcher))
                .MapResult(
                    (RunSynthesisPatcher settings) =>
                    {
                        try
                        {
                            System.Console.WriteLine(settings.ToString());
                            Patch(
                                settings,
                                patcher,
                                userPreferences);
                        }
                        catch (Exception ex)
                        {
                            System.Console.Error.WriteLine(ex);
                            return -1;
                        }
                        return 0;
                    },
                    _ =>
                    {
                        var prefs = userPreferences ?? new UserPreferences();
                        if (prefs.ActionsForEmptyArgs == null) return -1;
                        try
                        {
                            Patch(
                                GetDefaultRun(prefs.ActionsForEmptyArgs.IdentifyingModKey, prefs.ActionsForEmptyArgs.TargetRelease),
                                patcher,
                                prefs);
                        }
                        catch (Exception ex)
                        {
                            System.Console.Error.WriteLine(ex);
                            return -1;
                        }
                        return 0;
                    });
        }

        /// <summary>
        /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
        /// </summary>
        /// <typeparam name="TMod">Setter mod interface</typeparam>
        /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
        /// <param name="settings">Patcher run settings</param>
        /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
        /// <param name="userPreferences">Any custom user preferences</param>
        public async Task Patch<TMod, TModGetter>(
            RunSynthesisPatcher settings,
            AsyncPatcherFunction<TMod, TModGetter> patcher,
            UserPreferences? userPreferences = null)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter
        {
            WarmupAll.Init();
            var state = Utility.ToState<TMod, TModGetter>(settings, userPreferences ?? new UserPreferences());
            System.Console.WriteLine("Running patch.");
            await patcher(state).ConfigureAwait(false);
            System.Console.WriteLine($"Writing to output: {settings.OutputPath}");
            state.PatchMod.WriteToBinaryParallel(path: settings.OutputPath, param: GetWriteParams(state.LoadOrder.Select(i => i.Key)));
        }

        /// <summary>
        /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
        /// </summary>
        /// <typeparam name="TMod">Setter mod interface</typeparam>
        /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
        /// <param name="settings">Patcher run settings</param>
        /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
        /// <param name="userPreferences">Any custom user preferences</param>
        public void Patch<TMod, TModGetter>(
            RunSynthesisPatcher settings,
            PatcherFunction<TMod, TModGetter> patcher,
            UserPreferences? userPreferences = null)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter
        {
            WarmupAll.Init();
            var state = Utility.ToState<TMod, TModGetter>(settings, userPreferences ?? new UserPreferences());
            System.Console.WriteLine("Running patch.");
            patcher(state);
            System.Console.WriteLine($"Writing to output: {settings.OutputPath}");
            state.PatchMod.WriteToBinaryParallel(path: settings.OutputPath, param: GetWriteParams(state.LoadOrder.Select(i => i.Key)));
        }
        #endregion

        private BinaryWriteParameters GetWriteParams(IEnumerable<ModKey> loadOrder)
        {
            return new BinaryWriteParameters()
            {
                ModKey = BinaryWriteParameters.ModKeyOption.NoCheck,
                MastersListOrdering = new BinaryWriteParameters.MastersListOrderingByLoadOrder(loadOrder),
            };
        }

        public IEnumerable<LoadOrderListing> GetLoadOrder(
            RunSynthesisPatcher settings,
            UserPreferences? userPrefs = null,
            bool throwOnMissingMods = true)
        {
            return GetLoadOrder(
                release: settings.GameRelease,
                loadOrderFilePath: settings.LoadOrderFilePath,
                dataFolderPath: settings.DataFolderPath,
                userPrefs: userPrefs,
                throwOnMissingMods: throwOnMissingMods);
        }

        public IEnumerable<LoadOrderListing> GetLoadOrder(
            GameRelease release,
            string loadOrderFilePath,
            string dataFolderPath,
            UserPreferences? userPrefs = null,
            bool throwOnMissingMods = true)
        {
            var loadOrderListing = LoadOrder.FromPath(loadOrderFilePath, release, dataFolderPath);
            if (userPrefs?.InclusionMods != null)
            {
                var inclusions = userPrefs.InclusionMods.ToHashSet();
                loadOrderListing = loadOrderListing
                    .Where(m => inclusions.Contains(m.ModKey));
            }
            if (userPrefs?.ExclusionMods != null)
            {
                var exclusions = userPrefs.ExclusionMods.ToHashSet();
                loadOrderListing = loadOrderListing
                    .Where(m => !exclusions.Contains(m.ModKey));
            }
            return loadOrderListing;
        }

        public static RunSynthesisPatcher GetDefaultRun(ModKey modKey, GameRelease release)
        {
            var dataPath = Path.Combine(release.ToWjGame().MetaData().GameLocation().ToString(), "Data");
            if (!LoadOrder.TryGetPluginsFile(release, out var path))
            {
                throw new FileNotFoundException("Could not locate load order automatically.");
            }
            return new RunSynthesisPatcher()
            {
                DataFolderPath = dataPath,
                SourcePath = null,
                OutputPath = Path.Combine(dataPath, modKey.FileName),
                GameRelease = release,
                LoadOrderFilePath = path.Path
            };
        }
    }
}
