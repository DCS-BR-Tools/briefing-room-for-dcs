# Plan: Windows SmartScreen Code Signing (Option A — SignPath.io OSS)

## Context
- All 4 executables produced by the EXE flavor build are unsigned -> Windows SmartScreen shows red block
  - BriefingRoom-Desktop.exe (root of publish dir)
  - BriefingRoom-Web.exe (root of publish dir)
  - BriefingRoom-CLI.exe (root of publish dir)
  - Updater.exe (in bin\ subfolder)
- Build runs on windows-latest GitHub Actions runner
- GitHub Organisation: DCS-BR-Tools
- Chosen approach: SignPath.io Community OSS (free for open-source orgs)
- No existing signing config in any workflow

## Approach
SignPath receives a zip artifact containing all 4 exes and signs them in one request using a glob pattern (path: "**/*.exe") in the Artifact Configuration. The signed zip replaces the unsigned one before the GitHub Release is uploaded.

---

## Phase 1 - One-time External Setup (human, ~1-2 weeks for approval)
1. Apply for SignPath Community (OSS) plan at signpath.io/product/open-source using the DCS-BR-Tools GitHub Org
2. In the SignPath dashboard, create an Artifact Configuration named briefing-room-release:
   - Type: zip-file
   - Add a pe-file element with path: "**/*.exe" to match all 4 executables regardless of subfolder
   - Sign with Authenticode SHA-256, including a trusted timestamp
3. Create a Signing Policy (slug: release-signing) linked to the OSS OV certificate
4. Connect the GitHub repo to SignPath via the GitHub App integration (avoids manual API token management)
5. Generate a CI User API token as a fallback if the GitHub App approach is not available for free tier

## Phase 2 - GitHub Repository Secrets
6. Add SIGNPATH_API_TOKEN (CI user token from step 5)
7. Add SIGNPATH_ORGANIZATION_ID (visible in the SignPath dashboard URL slug)
8. Add SIGNPATH_PROJECT_SLUG (e.g. briefing-room-for-dcs)
9. Add SIGNPATH_SIGNING_POLICY_SLUG (e.g. release-signing)

## Phase 3 - Update dotnet-release.yml
Insert 3 new steps AFTER "Zip Release" and BEFORE "Upload Release":

10. Upload unsigned zip as artifact via actions/upload-artifact@v4
    - name: unsigned-release
    - path: BriefingRoom-V0.5.${{ steps.current-date.outputs.formattedTime }}.zip
    - id: upload-unsigned

11. Submit signing request via signpath/github-action-submit-signing-request@v1
    - organization-id: ${{ secrets.SIGNPATH_ORGANIZATION_ID }}
    - project-slug: ${{ secrets.SIGNPATH_PROJECT_SLUG }}
    - signing-policy-slug: ${{ secrets.SIGNPATH_SIGNING_POLICY_SLUG }}
    - artifact-configuration-slug: briefing-room-release
    - github-artifact-id: ${{ steps.upload-unsigned.outputs.artifact-id }}
    - wait-for-completion: true
    - output-artifact-path: BriefingRoom-V0.5.${{ steps.current-date.outputs.formattedTime }}.zip (overwrites unsigned zip)

12. Delete the unsigned artifact so it is not publicly downloadable from the Actions run

## Phase 4 - Update dotnet.yml (beta-release)
13. Identical 3 steps with beta-release-${{ steps.current-time.outputs.formattedTime }}.zip substituted for the zip filename

## Relevant Files
- .github/workflows/dotnet-release.yml - insert between "Zip Release" and "Upload Release"
- .github/workflows/dotnet.yml - insert same steps between "Zip Release" and "Upload Release"

## Verification
1. After workflow run, download the GitHub Release zip
2. Right-click each .exe -> Properties -> Digital Signatures -> confirm valid OV certificate on all 4 files
3. Run BriefingRoom-Desktop.exe on a clean Windows machine -> SmartScreen shows publisher name not "Unknown publisher"
4. Run BriefingRoom-Web.exe similarly

## Decisions
- Sign the zip artifact (produced by existing "Zip Release" step) rather than individual files - avoids unzip+re-zip and works naturally with SignPath zip-file artifact configuration
- Sign all 4 exes via **/*.exe glob in one signing request - Updater.exe in bin\ subfolder is covered
- Unsigned artifact must be deleted after signing to prevent users downloading an unsigned copy
- EV certificates (~$200-400/yr + hardware token) are out of scope; OV via SignPath OSS removes "Unknown publisher" and builds reputation over time
