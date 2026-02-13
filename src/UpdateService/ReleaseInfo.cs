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
using System.Text.Json.Serialization;

namespace BriefingRoom4DCS.UpdateService
{
    /// <summary>
    /// Represents release information from GitHub.
    /// </summary>
    public class ReleaseInfo
    {
        /// <summary>
        /// Release tag name (e.g., "release-260211-120000-123-1").
        /// </summary>
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        /// <summary>
        /// Human-readable release name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Release publication date.
        /// </summary>
        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }

        /// <summary>
        /// URL to the release page.
        /// </summary>
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }

        /// <summary>
        /// Whether this is a prerelease/beta.
        /// </summary>
        [JsonPropertyName("prerelease")]
        public bool IsPrerelease { get; set; }

        /// <summary>
        /// Whether this is a draft release.
        /// </summary>
        [JsonPropertyName("draft")]
        public bool IsDraft { get; set; }

        /// <summary>
        /// Release assets (downloadable files).
        /// </summary>
        [JsonPropertyName("assets")]
        public ReleaseAsset[] Assets { get; set; }
    }

    /// <summary>
    /// Represents a downloadable asset attached to a release.
    /// </summary>
    public class ReleaseAsset
    {
        /// <summary>
        /// Asset file name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Direct download URL.
        /// </summary>
        [JsonPropertyName("browser_download_url")]
        public string DownloadUrl { get; set; }

        /// <summary>
        /// File size in bytes.
        /// </summary>
        [JsonPropertyName("size")]
        public long Size { get; set; }

        /// <summary>
        /// Content type (e.g., "application/zip").
        /// </summary>
        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }
    }
}
