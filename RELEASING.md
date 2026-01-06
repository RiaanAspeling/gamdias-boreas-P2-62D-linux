# Releasing BOREAS Linux Driver

This document describes the release process for the BOREAS Linux Driver.

## Versioning Strategy

This project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html):

- **MAJOR** version for incompatible API/configuration changes
- **MINOR** version for new functionality in a backwards-compatible manner
- **PATCH** version for backwards-compatible bug fixes

## Automated Releases (Recommended)

Releases are automated via GitHub Actions. To create a new release:

### 1. Update Version Numbers

Update the version in `P2-62D/Boreas.csproj`:

```xml
<Version>X.Y.Z</Version>
<AssemblyVersion>X.Y.Z.0</AssemblyVersion>
<FileVersion>X.Y.Z.0</FileVersion>
```

### 2. Update Changelog

Add a new section to `CHANGELOG.md`:

```markdown
## [X.Y.Z] - YYYY-MM-DD

### Added
- New features...

### Changed
- Changes to existing functionality...

### Fixed
- Bug fixes...
```

Update the links at the bottom of the changelog:

```markdown
[Unreleased]: https://github.com/RiaanAspeling/gamdias-boreas-P2-62D-linux/compare/vX.Y.Z...HEAD
[X.Y.Z]: https://github.com/RiaanAspeling/gamdias-boreas-P2-62D-linux/releases/tag/vX.Y.Z
```

### 3. Commit Changes

```bash
git add -A
git commit -m "Release vX.Y.Z"
git push origin main
```

### 4. Create and Push Tag

```bash
git tag vX.Y.Z
git push origin vX.Y.Z
```

The GitHub Actions workflow will automatically:
- Build the project for linux-x64
- Create release archives (tar.gz and zip)
- Generate SHA256 checksums
- Create a GitHub Release with all assets
- Extract release notes from CHANGELOG.md

## Manual Release Process

If you need to create a release manually:

### 1. Build the Release

```bash
cd P2-62D
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r linux-x64 --self-contained false -o ../release/boreas-linux-x64
```

### 2. Create Release Archive

```bash
cd release
cp ../install/boreas.service boreas-linux-x64/
cp ../install/99-boreas.rules boreas-linux-x64/
cp ../README.md boreas-linux-x64/
cp ../CHANGELOG.md boreas-linux-x64/

tar -czvf boreas-vX.Y.Z-linux-x64.tar.gz boreas-linux-x64/
zip -r boreas-vX.Y.Z-linux-x64.zip boreas-linux-x64/
```

### 3. Generate Checksums

```bash
sha256sum boreas-vX.Y.Z-linux-x64.tar.gz > checksums.txt
sha256sum boreas-vX.Y.Z-linux-x64.zip >> checksums.txt
```

### 4. Create GitHub Release

1. Go to the repository's Releases page
2. Click "Draft a new release"
3. Choose the tag (create new if needed)
4. Set the release title: `vX.Y.Z`
5. Copy release notes from CHANGELOG.md
6. Upload the archives and checksums.txt
7. Publish the release

## Release Checklist

Before releasing, ensure:

- [ ] All tests pass (if applicable)
- [ ] Version numbers updated in `Boreas.csproj`
- [ ] `CHANGELOG.md` updated with all changes
- [ ] README.md updated if needed
- [ ] Code builds without warnings
- [ ] Installation files are up to date (`install/` directory)

## Release Assets

Each release includes:

| File | Description |
|------|-------------|
| `boreas-vX.Y.Z-linux-x64.tar.gz` | Linux x64 binary (tar archive) |
| `boreas-vX.Y.Z-linux-x64.zip` | Linux x64 binary (zip archive) |
| `checksums.txt` | SHA256 checksums for verification |

## Post-Release

After a release:

1. Announce the release (if applicable)
2. Update any package manager submissions (AUR, etc.)
3. Monitor issues for release-related problems
