using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Threading.Tasks;
using Noggog;

namespace SynNoMoreEasyEnemies
{
    public class Program
    {
        // Settings
        static Lazy<Settings> LazySettings = new Lazy<Settings>();
        static Settings settings => LazySettings.Value;

        // Initial setup
        public static async Task<int> Main(string[] args) {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetAutogeneratedSettings(
                    nickname: "Settings",
                    path: "settings.json",
                    out LazySettings
                )
                .SetTypicalOpen(GameRelease.SkyrimSE, "NoMoreEasyEnemies.esp")
                .Run(args);
        }

        // Let's get to work!
        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) {

            Console.WriteLine($"***********************");
            Console.WriteLine($"Real work starting now!");
            Console.WriteLine($"***********************");

            // Convert our custom enum setting to proper level
            Level LevelModifierToReplace = settings.LevelModifierToReplace switch
            {
                Settings.LevelSetting.Medium => Level.Medium,
                Settings.LevelSetting.Hard => Level.Hard,
                Settings.LevelSetting.VeryHard => Level.VeryHard,
                _ => throw new NotImplementedException("Somehow you set a invalid Level Modifier.")
            };

            //// Multipliers ////
            // Save existing (old) multipliers
            Dictionary<Level, float> oldMults = new();
            Dictionary<Level, IGameSettingGetter> multGetters = new();

            foreach (var gmst in state.LoadOrder.PriorityOrder.OnlyEnabled().GameSetting().WinningOverrides()) {
                // We only care about game settings that are floats
                if (gmst is not IGameSettingFloatGetter floatGmst) continue;
                if (floatGmst.EditorID == null) continue;
                if (floatGmst.Data == null) continue;

                // Only for our interesting settings
                if (floatGmst.EditorID.Contains("fLeveledActorMultEasy")) {
                    var multEasy = floatGmst.Data;
                    oldMults.Add(Level.Easy, multEasy ?? 0f);
                    multGetters.Add(Level.Easy, gmst);
                    Console.WriteLine($"Old fLeveledActorMultEasy: {multEasy}");
                }
                else if (floatGmst.EditorID.Contains("fLeveledActorMultMedium")) {
                    var multMedium = floatGmst.Data;
                    oldMults.Add(Level.Medium, multMedium ?? 0f);
                    multGetters.Add(Level.Medium, gmst);
                    Console.WriteLine($"Old fLeveledActorMultMedium: {multMedium}");
                }
                else if (floatGmst.EditorID.Contains("fLeveledActorMultHard")) {
                    var multHard = floatGmst.Data;
                    oldMults.Add(Level.Hard, multHard ?? 0f);
                    multGetters.Add(Level.Hard, gmst);
                    Console.WriteLine($"Old fLeveledActorMultHard: {multHard}");
                }
                else if (floatGmst.EditorID.Contains("fLeveledActorMultVeryHard")) {
                    var multVeryHard = floatGmst.Data;
                    oldMults.Add(Level.VeryHard, multVeryHard ?? 0f);
                    multGetters.Add(Level.VeryHard, gmst);
                    Console.WriteLine($"Old fLeveledActorMultVeryHard: {multVeryHard}");
                }
            }

            // Create new multiplier overrides
            switch (LevelModifierToReplace) {
                case Level.Medium: {
                        Console.WriteLine($"Per your settings, we will be replacing the 'Medium' modifier.");
                        ((GameSettingFloat)state.PatchMod.GameSettings.GetOrAddAsOverride(multGetters[Level.Medium])).Data = oldMults[Level.Easy];
                        Console.WriteLine($"New fLeveledActorMultMedium: {oldMults[Level.Easy]}");
                        break;
					}
                case Level.Hard: {
                        Console.WriteLine($"Per your settings, we will be replacing the 'Hard' modifier.");
                        ((GameSettingFloat)state.PatchMod.GameSettings.GetOrAddAsOverride(multGetters[Level.Medium])).Data = oldMults[Level.Easy];
                        Console.WriteLine($"New fLeveledActorMultMedium: {oldMults[Level.Easy]}");
                        ((GameSettingFloat)state.PatchMod.GameSettings.GetOrAddAsOverride(multGetters[Level.Hard])).Data = oldMults[Level.Medium];
                        Console.WriteLine($"New fLeveledActorMultHard: {oldMults[Level.Medium]}");
                        break;
                    }
                case Level.VeryHard: {
                        Console.WriteLine($"Per your settings, we will be replacing the 'VeryHard' modifier.");
                        ((GameSettingFloat)state.PatchMod.GameSettings.GetOrAddAsOverride(multGetters[Level.Medium])).Data = oldMults[Level.Easy];
                        Console.WriteLine($"New fLeveledActorMultMedium: {oldMults[Level.Easy]}");
                        ((GameSettingFloat)state.PatchMod.GameSettings.GetOrAddAsOverride(multGetters[Level.Hard])).Data = oldMults[Level.Medium];
                        Console.WriteLine($"New fLeveledActorMultHard: {oldMults[Level.Medium]}");
                        ((GameSettingFloat)state.PatchMod.GameSettings.GetOrAddAsOverride(multGetters[Level.VeryHard])).Data = oldMults[Level.Hard];
                        Console.WriteLine($"New fLeveledActorMultVeryHard: {oldMults[Level.Hard]}");
                        break;
                    }
                default: throw new NotImplementedException("Somehow you set a invalid Level Modifier.");
            }

            //// Modifiers ////
            // Create a map between the old and new level modifiers
            Dictionary <Level, Level> levelConversion = new();

            // Fill the conversion map
            switch (LevelModifierToReplace) {
                case Level.Medium: { 
                        levelConversion.Add(Level.Easy, Level.Medium); 
                        break; 
                    }
                case Level.Hard: { 
                        levelConversion.Add(Level.Easy, Level.Medium); 
                        levelConversion.Add(Level.Medium, Level.Hard); 
                        break; 
                    }
                case Level.VeryHard: {
                        levelConversion.Add(Level.Easy, Level.Medium);
                        levelConversion.Add(Level.Medium, Level.Hard);
                        levelConversion.Add(Level.Hard, Level.VeryHard);
                        break;
                    }
                default:
                    throw new NotImplementedException("Somehow you set a invalid Level Modifier.");
            }

            Console.WriteLine("Updating Placed NPC records (ACHR) with the new Level Modifiers...");

            int achrCount = 0;

            foreach (var achr in state.LoadOrder.PriorityOrder.OnlyEnabled().PlacedNpc().WinningContextOverrides(state.LinkCache)) {
                // If the ACHR has no base, skip it
                // Not even sure this is possible
                if (achr.Record.Base.IsNull) continue;

                // If the ACHR has no Level Modifier, skip it                
                if (achr.Record.LevelModifier == null) continue;

                // If the Level Modifier is the one we're replacing, delete it
                if (achr.Record.LevelModifier == LevelModifierToReplace) {
                    // Create override
                    IPlacedNpc achrOverride = achr.GetOrAddAsOverride(state.PatchMod);
                    achrOverride.LevelModifier = null;
				}
                else if (levelConversion.ContainsKey(achr.Record.LevelModifier.Value)) {
                    // Create override
                    IPlacedNpc achrOverride = achr.GetOrAddAsOverride(state.PatchMod);
                    achrOverride.LevelModifier = levelConversion[achr.Record.LevelModifier.Value];
                }

                achrCount++;
                if (achrCount % 1000 == 0) {
                    Console.WriteLine($"Processed {achrCount} records.");
                }
            }
            Console.WriteLine($"Processed {achrCount} records.");

            //// Leveled NPC Lists ////
            if (settings.RemoveFlag) {
                Console.WriteLine("Updating Leveled NPC Lists (LVLN) to prefer NPCs closer to the player's level.");

                int lvlnCount = 0;
                foreach (var lvln in state.LoadOrder.PriorityOrder.OnlyEnabled().LeveledNpc().WinningOverrides()) {

                    if (lvln.Flags.HasFlag(LeveledNpc.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer)) {
                        var lvlnOverride = state.PatchMod.LeveledNpcs.GetOrAddAsOverride(lvln);
                        lvlnOverride.Flags = lvlnOverride.Flags.SetFlag(LeveledNpc.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer, false);
                    }

                    lvlnCount++;
                    if (lvlnCount % 100 == 0) {
                        Console.WriteLine($"Processed {lvlnCount} records.");
                    }
                }
                Console.WriteLine($"Processed {lvlnCount} records.");
            }
        }
    }
}
