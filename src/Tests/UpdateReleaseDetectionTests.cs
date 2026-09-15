using BriefingRoom4DCS.UpdateService;

namespace BriefingRoom4DCS.Tests;

public sealed class UpdateReleaseDetectionTests
{
    [Fact]
    public void GetReleaseBuildDate_UsesBuildIdFromStableTag()
    {
        var release = new ReleaseInfo
        {
            TagName = "release-2026.08.26-20260826-231046-33022236682-1",
            PublishedAt = new DateTime(2026, 8, 26, 23, 18, 49, DateTimeKind.Utc)
        };

        Assert.Equal(
            new DateTime(2026, 8, 26, 23, 10, 46, DateTimeKind.Utc),
            UpdateManager.GetReleaseBuildDate(release));
    }

    [Fact]
    public void GetReleaseBuildDate_UsesBuildIdFromBetaTag()
    {
        var release = new ReleaseInfo
        {
            TagName = "beta-release-20260803-095816-30803548103-1"
        };

        Assert.Equal(
            new DateTime(2026, 8, 3, 9, 58, 16, DateTimeKind.Utc),
            UpdateManager.GetReleaseBuildDate(release));
    }

    [Fact]
    public void FindLatestNewerRelease_DoesNotOfferCurrentReleaseAgain()
    {
        var currentBuild = new DateTime(2026, 8, 26, 23, 10, 46, DateTimeKind.Utc);
        var currentRelease = new ReleaseInfo
        {
            TagName = "release-2026.08.26-20260826-231046-33022236682-1",
            PublishedAt = currentBuild.AddMinutes(8)
        };

        Assert.Null(UpdateManager.FindLatestNewerRelease([currentRelease], currentBuild));
    }

    [Fact]
    public void FindLatestNewerRelease_IsNullSafeAndSelectsNewest()
    {
        var currentBuild = new DateTime(2026, 8, 26, 23, 10, 46, DateTimeKind.Utc);
        var first = new ReleaseInfo { TagName = "release-2026.08.27-20260827-120000-1-1" };
        var latest = new ReleaseInfo { TagName = "release-2026.08.28-20260828-120000-1-1" };

        Assert.Same(latest, UpdateManager.FindLatestNewerRelease([first, latest], currentBuild));
        Assert.Null(UpdateManager.FindLatestNewerRelease([], currentBuild));
    }

}
