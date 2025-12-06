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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Mission
{
    public sealed class DCSCampaign
    {
        internal string CMPFile { get; set; }

        private readonly Dictionary<string, byte[]> MediaFiles;

        public int MissionCount { get { return Missions.Count; } }

        public readonly List<DCSMission> Missions;

        public string Name { get; internal set; }

        public DCSCampaign()
        {
            CMPFile = "";
            MediaFiles = new Dictionary<string, byte[]>(StringComparer.InvariantCultureIgnoreCase);
            Missions = new List<DCSMission>();
            Name = "";
        }

        public async Task ExportToDirectory(IDatabase database, string exportPath)
        {
            // Try to create the directory if it doesn't exist.
            if (!Toolbox.CreateMissingDirectory(exportPath))
                throw new BriefingRoomException(database, Missions[0].LangKey, "FailedToCreateCampaignFolder");



            // Write CMP file
            File.WriteAllText($"{exportPath}Campaign.cmp", CMPFile); // TODO: check .cmp file name

            // Write media files
            foreach (string f in MediaFiles.Keys)
                File.WriteAllBytes($"{exportPath}{f}", MediaFiles[f]);

            // Write missions
            foreach (var mission in Missions)
            {
                await mission.SaveToMizFile(database, $"{exportPath}{Name}-{mission.Briefing.Name}.miz");
            }
                
        }

        public async Task<byte[]> ExportToCompressedByteArray(IDatabase database, CampaignTemplate template)
        {
            Dictionary<string, byte[]> FileEntries = new()
            {
                { "Campaign.cmp", Encoding.UTF8.GetBytes(CMPFile) },
                { "template.cbrt", template.GetIniBytes() }
            };
            
            string baseFileName = Toolbox.RemoveInvalidPathCharacters(this.Name);
            await Imagery.GenerateCampaignImages(database, template, this, baseFileName);

            foreach (string key in MediaFiles.Keys)
            {
                FileEntries.Add(key, MediaFiles[key]);
            }

            for (int i = 0; i < Missions.Count; i++)
                FileEntries.Add($"{i+1}_{Missions[i].Briefing.Name}.miz", await Missions[i].SaveToMizBytes(database));

            return Toolbox.ZipData(database, Missions[0].LangKey, FileEntries);
        }

        public byte[] ExportBriefingsToCompressedByteArray(IDatabase database)
        {
            Dictionary<string, byte[]> FileEntries = new();

            foreach (var mission in Missions)
                FileEntries.Add($"{mission.Briefing.Name}.html", Encoding.UTF8.GetBytes(mission.Briefing.GetBriefingAsHTML(database, mission)));
                

            return Toolbox.ZipData(database,  Missions[0].LangKey, FileEntries);
        }

        internal void AddMission(DCSMission mission)
        {
            if (mission == null) return;
            Missions.Add(mission);
        }

        internal void AddMediaFile(string fileName, byte[] mediaFileBytes)
        {
            if (MediaFiles.ContainsKey(fileName))
                MediaFiles[fileName] = mediaFileBytes;
            else
                MediaFiles.Add(fileName, mediaFileBytes);
        }

    }
}

