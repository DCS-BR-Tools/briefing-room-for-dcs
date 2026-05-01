/*
==========================================================================
This file is part of Briefing Room for DCS World, a mission
generator for DCS World, by @akaAgar (https://github.com/akaAgar/briefing-room-for-dcs)

Briefing Room for DCS World is free software: you can redistribute it
and/or modify it under the terms of the GNU General Public License
as published by the Free Software Foundation, either version 3 of
the License, or (at your option) any later version.

Briefing Room for DCS World is distributed in the hope that it will
be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Briefing Room for DCS World. If not, see https://www.gnu.org/licenses/
==========================================================================
*/

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.CommandLineTool
{
    public class CommandLine
    {
        private static readonly string LOG_FILE = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BriefingRoomCommandLineDebugLog.txt");

        private readonly StreamWriter LogWriter;

        [STAThread]
        private static async Task Main(string[] args)
        {
#if DEBUG
            if (args.Length == 0) args = new string[] { "Default.brt" };
#endif

            try
            {
                await new CommandLine().DoCommandLineAsync(args);
            }
            catch (Exception e)
            {
                Console.WriteLine($"CRITICAL ERROR: {e.Message}");
            }
        }

        public CommandLine()
        {
            if (File.Exists(LOG_FILE)) File.Delete(LOG_FILE);
            LogWriter = File.AppendText(LOG_FILE);
            LogWriter.Flush();
        }

        private void WriteToDebugLog(string message, LogMessageErrorLevel errorLevel = LogMessageErrorLevel.Info)
        {
            switch (errorLevel)
            {
                case LogMessageErrorLevel.Error: message = $"ERROR: {message}"; break;
                case LogMessageErrorLevel.Warning: message = $"WARNING: {message}"; break;
            }

            LogWriter.WriteLine(message);
            Console.WriteLine(message);
        }

        public async Task<bool> DoCommandLineAsync(string[] args)
        {
            int? seedOverride = ExtractSeedFlag(ref args);

            string[] templateFiles = (from string arg in args where File.Exists(arg) select arg).ToArray();
            string[] invalidTemplateFiles = (from string arg in args where !File.Exists(arg) && !arg.StartsWith("-") select arg).ToArray();

            foreach (string filePath in invalidTemplateFiles)
                WriteToDebugLog($"Template file {filePath} doesn't exist.", LogMessageErrorLevel.Warning);

            if (templateFiles.Length == 0)
            {
                WriteToDebugLog("No valid mission template files given as parameters.", LogMessageErrorLevel.Error);
                WriteToDebugLog("");
                WriteToDebugLog("Command-line format is BriefingRoomCommandLine.exe [--seed N] <MissionTemplate.brt> [<MissionTemplate2.brt> ...]");
                WriteToDebugLog("  --seed N    Apply random seed N to every template, overriding any seed in the .brt file.");
                return false;
            }

            var briefingRoom = new BriefingRoom(new Database());

            foreach (string t in templateFiles)
            {
                if (Path.GetExtension(t).ToLower() == ".cbrt") // Template file is a campaign template
                {
                    DCSCampaign campaign;
                    if (seedOverride.HasValue)
                    {
                        var campaignTemplate = new CampaignTemplate(briefingRoom, t) { RandomSeed = seedOverride };
                        campaign = briefingRoom.GenerateCampaign(campaignTemplate);
                    }
                    else
                    {
                        campaign = briefingRoom.GenerateCampaign(t);
                    }
                    if (campaign == null)
                    {
                        Console.WriteLine($"Failed to generate a campaign from template {Path.GetFileName(t)}");
                        continue;
                    }

                    string campaignDirectory;
                    if (templateFiles.Length == 1) // Single template file provided, use  campaign name as campaign path.
                        campaignDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RemoveInvalidPathCharacters(campaign.Name));
                    else // Multiple template files provided, use the template name as campaign name so we know from which template campaign was generated.
                        campaignDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileNameWithoutExtension(t));
                    campaignDirectory = GetUnusedFileName(campaignDirectory);

                    await campaign.ExportToDirectory(briefingRoom.Database, AppDomain.CurrentDomain.BaseDirectory);
                    WriteToDebugLog($"Campaign {Path.GetFileName(campaignDirectory)} exported to directory from template {Path.GetFileName(t)}");
                }
                else // Template file is a mission template
                {
                    DCSMission mission;
                    if (seedOverride.HasValue)
                    {
                        var missionTemplate = new MissionTemplate(briefingRoom.Database, t) { RandomSeed = seedOverride };
                        mission = briefingRoom.GenerateMission(missionTemplate);
                    }
                    else
                    {
                        mission = briefingRoom.GenerateMission(t);
                    }
                    if (mission == null)
                    {
                        Console.WriteLine($"Failed to generate a mission from template {Path.GetFileName(t)}");
                        continue;
                    }

                    string mizFileName;
                    if (templateFiles.Length == 1) // Single template file provided, use "theater + mission name" as file name.
                        mizFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{mission.TheaterID} - {RemoveInvalidPathCharacters(mission.Briefing.Name)}.miz");
                    else // Multiple template files provided, use the template name as file name so we know from which template mission was generated.
                        mizFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileNameWithoutExtension(t) + ".miz");
                    mizFileName = GetUnusedFileName(mizFileName);

                    var savedMission = await mission.SaveToMizFile(briefingRoom.Database, mizFileName);
                    if (!savedMission)
                    {
                        WriteToDebugLog($"Failed to export .miz file from template {Path.GetFileName(t)}", LogMessageErrorLevel.Warning);
                        continue;
                    }
                    else
                        WriteToDebugLog($"Mission {Path.GetFileName(mizFileName)} exported to .miz file from template {Path.GetFileName(t)}. Found in {mizFileName}");
                }
            }

            return true;
        }

        private static string RemoveInvalidPathCharacters(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "_";
            return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
        }

        // Extract `--seed N` (or `--seed=N`) from args. Returns null if absent or unparseable.
        // The flag is removed from the args array so the rest of the loop only sees template paths.
        private static int? ExtractSeedFlag(ref string[] args)
        {
            int? seed = null;
            var remaining = new System.Collections.Generic.List<string>(args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a == "--seed" && i + 1 < args.Length && int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int s))
                {
                    seed = s;
                    i++;
                    continue;
                }
                if (a.StartsWith("--seed=") && int.TryParse(a.Substring("--seed=".Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int s2))
                {
                    seed = s2;
                    continue;
                }
                remaining.Add(a);
            }
            args = remaining.ToArray();
            return seed;
        }

        private static string GetUnusedFileName(string filePath)
        {
            if (!File.Exists(filePath)) return filePath; // File doesn't exist, use the desired name

            string newName;
            int extraNameCount = 2;

            do
            {
                newName = Path.Combine(Path.GetDirectoryName(filePath), $"{Path.GetFileNameWithoutExtension(filePath)} ({extraNameCount}){Path.GetExtension(filePath)}");
                extraNameCount++;
            } while (File.Exists(newName));

            return newName;
        }
    }
}
