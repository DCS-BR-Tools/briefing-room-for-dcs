﻿/*
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

using BriefingRoom4DCS.Campaign;
using BriefingRoom4DCS.Mission;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BriefingRoom4DCS.CommandLineTool
{
    public class CommandLine
    {
        private static readonly string LOG_FILE = $"{Application.StartupPath}\\BriefingRoomCommandLineDebugLog.txt";

        private readonly StreamWriter LogWriter;

        private readonly BriefingRoom BriefingRoomGenerator;

        [STAThread]
        private static async void Main(string[] args)
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
            BriefingRoomGenerator = new BriefingRoom(WriteToDebugLog);
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
            string[] templateFiles = (from string arg in args where File.Exists(arg) select arg).ToArray();
            string[] invalidTemplateFiles = (from string arg in args where !File.Exists(arg) select arg).ToArray();

            foreach (string filePath in invalidTemplateFiles)
                WriteToDebugLog($"Template file {filePath} doesn't exist.", LogMessageErrorLevel.Warning);

            if (templateFiles.Length == 0)
            {
                WriteToDebugLog("No valid mission template files given as parameters.", LogMessageErrorLevel.Error);
                WriteToDebugLog("");
                WriteToDebugLog("Command-line format is BriefingRoomCommandLine.exe <MissionTemplate.brt> [<MissionTemplate2.brt> <MissionTemplate3.brt>...]");
                return false;
            }

            foreach (string t in templateFiles)
            {
                if (Path.GetExtension(t).ToLowerInvariant() == ".cbrt") // Template file is a campaign template
                {
                    DCSCampaign campaign = await BriefingRoomGenerator.GenerateCampaignAsync(t);
                    if (campaign == null)
                    {
                        Console.WriteLine($"Failed to generate a campaign from template {Path.GetFileName(t)}");
                        continue;
                    }

                    string campaignDirectory;
                    if (templateFiles.Length == 1) // Single template file provided, use  campaign name as campaign path.
                        campaignDirectory = Path.Combine(Application.StartupPath, RemoveInvalidPathCharacters(campaign.Name));
                    else // Multiple template files provided, use the template name as campaign name so we know from which template campaign was generated.
                        campaignDirectory = Path.Combine(Application.StartupPath, Path.GetFileNameWithoutExtension(t));
                    campaignDirectory = GetUnusedFileName(campaignDirectory);

                    campaign.ExportToDirectory(Application.StartupPath);
                    WriteToDebugLog($"Campaign {Path.GetFileName(campaignDirectory)} exported to directory from template {Path.GetFileName(t)}");
                }
                else // Template file is a mission template
                {
                    DCSMission mission = await BriefingRoomGenerator.GenerateMissionAsync(t);
                    if (mission == null)
                    {
                        Console.WriteLine($"Failed to generate a mission from template {Path.GetFileName(t)}");
                        continue;
                    }

                    string mizFileName;
                    if (templateFiles.Length == 1) // Single template file provided, use "theater + mission name" as file name.
                        mizFileName = Path.Combine(Application.StartupPath, $"{mission.TheaterID} - {RemoveInvalidPathCharacters(mission.Briefing.Name)}.miz");
                    else // Multiple template files provided, use the template name as file name so we know from which template mission was generated.
                        mizFileName = Path.Combine(Application.StartupPath, Path.GetFileNameWithoutExtension(t) + ".miz");
                    mizFileName = GetUnusedFileName(mizFileName);

                    if (!mission.SaveToMizFile(mizFileName))
                    {
                        WriteToDebugLog($"Failed to export .miz file from template {Path.GetFileName(t)}", LogMessageErrorLevel.Warning);
                        continue;
                    }
                    else
                        WriteToDebugLog($"Mission {Path.GetFileName(mizFileName)} exported to .miz file from template {Path.GetFileName(t)}");
                }
            }

            return true;
        }

        private string RemoveInvalidPathCharacters(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "_";
            return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
        }

        private string GetUnusedFileName(string filePath)
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
