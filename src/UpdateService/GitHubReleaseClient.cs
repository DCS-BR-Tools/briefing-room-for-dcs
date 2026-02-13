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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BriefingRoom4DCS.UpdateService
{
    /// <summary>
    /// Client for fetching release information from GitHub API.
    /// </summary>
    public class GitHubReleaseClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly UpdateOptions _options;
        private bool _disposed;

        public GitHubReleaseClient(UpdateOptions options = null)
        {
            _options = options ?? new UpdateOptions();
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("BriefingRoom", BriefingRoom.VERSION));
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        }


        /// <summary>
        /// Gets all releases from GitHub.
        /// </summary>
        public async Task<ReleaseInfo[]> GetReleasesAsync(CancellationToken cancellationToken = default)
        {
            var url = $"https://api.github.com/repos/{_options.GitHubOwner}/{_options.GitHubRepo}/releases";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var releases = JsonSerializer.Deserialize<ReleaseInfo[]>(json);


            return releases ?? Array.Empty<ReleaseInfo>();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}
