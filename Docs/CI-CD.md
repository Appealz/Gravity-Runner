# Gravity Runner CI/CD (portfolio repository)

## Scope and limitations

This GitHub repository intentionally contains a **public portfolio snapshot**, not a complete Unity project. It has `Script/`, `Docs/`, and `README.md`, but not the full Unity `Assets/`, `Packages/`, and `ProjectSettings/` directories.

**CI** checks:
- C# syntax in `Script/**/*.cs` using the Roslyn parser
- Local file and image links in `README.md`

**CI does not** build the Unity game, resolve Unity or third-party references, verify compatibility with the original Unity/C# version, run Unity EditMode or PlayMode tests, or produce an APK/AAB. A green check means only that these lightweight source checks passed.

**CD** packages the publicly shared source code and screenshots as a ZIP and publishes a **GitHub Release** after the same validation passes on a version tag. It does **not** publish the game to Google Play.

## When the pipeline runs

| Event | Source validation | Portfolio source release |
| --- | --- | --- |
| Pull request targeting `main` | Yes | No |
| Push to `main` | Yes | No |
| Manual run from GitHub Actions | Yes | No |
| Push a `portfolio-v*` tag | Yes | Yes, after successful validation |

See `.github/workflows/portfolio-ci-cd.yml` for the exact workflow.

## Run CI locally

Install the .NET 8 SDK and run the following from the repository root:

```bash
dotnet run --project tools/RepoValidator/RepoValidator.csproj --configuration Release -- .
```

To inspect results on GitHub, open the repository's **Actions** tab and select **Gravity Runner - Portfolio CI/CD**. A failed check lists the script diagnostic or broken README link.

## Publish a source release

After the workflow is merged into `main`, create a version tag from the commit you want to publish:

```bash
git checkout main
git pull
git tag portfolio-v1.0.0
git push origin portfolio-v1.0.0
```

The tag triggers validation. If it passes, the workflow creates a GitHub Release with a `Gravity-Runner-portfolio-v1.0.0.zip` archive of `README.md`, `Docs/`, and `Script/`.

Use a new tag for each release; do not reuse a published release tag.

## To build or deploy the Android game later

Move the **complete buildable Unity project** to an appropriately protected repository, including the matching Unity Editor version, `Assets/` with `.meta` files, `Packages/manifest.json` and package lock, `ProjectSettings/`, required scenes, and build settings. Preserve necessary proprietary asset licenses, but do not commit credentials, keystores, service secrets, or local build folders such as `Library/`.

Then implement a separate Unity-specific workflow:
1. Run Unity EditMode tests and optionally PlayMode tests (a license or activation method may be needed for headless CI).
2. Build Android AAB/APK on a compatible runner with all dependencies and Android modules available.
3. Sign with securely stored credentials; if updating an existing Google Play app, use its existing package identity and approved signing arrangement.
4. Upload the signed artifact for review, then optionally deploy to a Google Play **internal testing** track after configuring access and review gates.

The present workflow intentionally does not attempt these steps without the complete project and required secure configuration.
