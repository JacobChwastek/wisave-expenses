# Contracts Versioning With Nerdbank.GitVersioning Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace manual contracts package versioning with Nerdbank.GitVersioning so pull requests to `master` publish preview packages and pushes to `master` publish stable packages from the same GitHub Packages feed.

**Architecture:** Store package version policy in a repo-root `version.json`, let NBGV stamp `WiSave.Expenses.Contracts` during build and pack, and split CI into a PR preview workflow plus a push-to-`master` stable publish workflow. Limit version-height bumps to contracts-relevant paths so unrelated repo changes do not advance the contracts package version.

**Tech Stack:** C# / .NET 10, GitHub Actions, GitHub Packages, Nerdbank.GitVersioning 3.9.50

**Spec:** `docs/superpowers/specs/2026-04-14-contracts-versioning-with-nbgv-design.md`

---

### File Map

| File | Action | Responsibility |
| ---- | ------ | -------------- |
| `version.json` | Create | Repo-level NBGV policy for contracts package versioning |
| `src/WiSave.Expenses.Contracts/WiSave.Expenses.Contracts.csproj` | Modify | Remove hardcoded version and add NBGV package reference |
| `.github/workflows/publish-contracts.yml` | Modify | Publish stable contracts packages on pushes to `master` |
| `.github/workflows/pr-contracts-preview.yml` | Create | Publish preview contracts packages for PRs targeting `master` |
| `README.md` | Modify | Document the contracts package versioning and publish behavior |

---

### Task 1: Add Repo-Level NBGV Configuration

**Files:**
- Create: `version.json`

- [ ] **Step 1: Create `version.json`**

Create `version.json` at the repository root:

```json
{
  "$schema": "https://raw.githubusercontent.com/dotnet/Nerdbank.GitVersioning/main/src/NerdBank.GitVersioning/version.schema.json",
  "version": "1.0",
  "publicReleaseRefSpec": [
    "^refs/heads/master$"
  ],
  "pathFilters": [
    "src/WiSave.Expenses.Contracts",
    ".github/workflows/publish-contracts.yml",
    ".github/workflows/pr-contracts-preview.yml",
    "version.json"
  ]
}
```

- [ ] **Step 2: Verify the file is valid JSON**

Run:
```bash
python -m json.tool version.json
```
Expected: the command prints formatted JSON with no parse errors.

- [ ] **Step 3: Commit**

```bash
git add version.json
git commit -m "chore(versioning): add repo-level NBGV configuration"
```

---

### Task 2: Wire NBGV Into Contracts Project

**Files:**
- Modify: `src/WiSave.Expenses.Contracts/WiSave.Expenses.Contracts.csproj`

- [ ] **Step 1: Replace manual versioning in the contracts project**

Update `src/WiSave.Expenses.Contracts/WiSave.Expenses.Contracts.csproj` so the property group and package references look like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <PackageId>WiSave.Expenses.Contracts</PackageId>
    <Description>Shared commands, integration events, and models for the WiSave Expenses microservice</Description>
    <Authors>JacobChwastek</Authors>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <RepositoryUrl>https://github.com/JacobChwastek/wisave-expenses</RepositoryUrl>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MassTransit.Abstractions" Version="8.5.8" />
    <PackageReference Include="Nerdbank.GitVersioning" Version="3.9.50">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

This step intentionally removes:

```xml
<Version>0.1.0</Version>
```

- [ ] **Step 2: Restore the contracts project**

Run:
```bash
dotnet restore src/WiSave.Expenses.Contracts/WiSave.Expenses.Contracts.csproj
```
Expected: restore succeeds and resolves `Nerdbank.GitVersioning` 3.9.50.

- [ ] **Step 3: Pack the contracts project locally**

Run:
```bash
dotnet pack src/WiSave.Expenses.Contracts/WiSave.Expenses.Contracts.csproj -c Release
```
Expected: pack succeeds and produces a `.nupkg` whose filename version is derived from NBGV rather than the removed hardcoded `0.1.0`.

- [ ] **Step 4: Inspect the generated nuspec/package name**

Run:
```bash
find src/WiSave.Expenses.Contracts/bin/Release -name '*.nupkg' | sort
```
Expected: output includes a package named `WiSave.Expenses.Contracts.<computed-version>.nupkg`, where `<computed-version>` is in the `1.0` line.

- [ ] **Step 5: Commit**

```bash
git add src/WiSave.Expenses.Contracts/WiSave.Expenses.Contracts.csproj
git commit -m "chore(contracts): derive package version from NBGV"
```

---

### Task 3: Update Stable Publish Workflow For `master`

**Files:**
- Modify: `.github/workflows/publish-contracts.yml`

- [ ] **Step 1: Replace the existing workflow content**

Update `.github/workflows/publish-contracts.yml` to:

```yaml
name: Publish Contracts NuGet

on:
  push:
    branches: [master]
    paths:
      - 'src/WiSave.Expenses.Contracts/**'
      - '.github/workflows/publish-contracts.yml'
      - '.github/workflows/pr-contracts-preview.yml'
      - 'version.json'

  workflow_dispatch:

env:
  DOTNET_VERSION: '10.0.x'
  PACKAGE_PROJECT: src/WiSave.Expenses.Contracts/WiSave.Expenses.Contracts.csproj

jobs:
  publish:
    runs-on: ubuntu-latest
    permissions:
      packages: write
      contents: read

    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore
        run: dotnet restore ${{ env.PACKAGE_PROJECT }}

      - name: Build
        run: dotnet build ${{ env.PACKAGE_PROJECT }} -c Release --no-restore

      - name: Pack
        run: dotnet pack ${{ env.PACKAGE_PROJECT }} -c Release --no-build -o ./nupkg

      - name: Push to GitHub Packages
        run: dotnet nuget push ./nupkg/*.nupkg --source "https://nuget.pkg.github.com/JacobChwastek/index.json" --api-key ${{ secrets.GITHUB_TOKEN }} --skip-duplicate
```

This keeps the workflow simple: pushes to `master` publish the stable package, and `fetch-depth: 0` makes full git history available for NBGV version calculation.

- [ ] **Step 2: Validate the workflow YAML locally**

Run:
```bash
rg -n "^name:|^  push:|^    branches: \\[master\\]" .github/workflows/publish-contracts.yml
```
Expected: matches include `name: Publish Contracts NuGet`, the `push:` trigger, and `branches: [master]`.

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/publish-contracts.yml
git commit -m "ci(contracts): publish stable contracts packages from master"
```

---

### Task 4: Add PR Preview Publish Workflow

**Files:**
- Create: `.github/workflows/pr-contracts-preview.yml`

- [ ] **Step 1: Create the PR workflow**

Create `.github/workflows/pr-contracts-preview.yml`:

```yaml
name: Publish Contracts Preview NuGet

on:
  pull_request:
    branches: [master]
    paths:
      - 'src/WiSave.Expenses.Contracts/**'
      - '.github/workflows/publish-contracts.yml'
      - '.github/workflows/pr-contracts-preview.yml'
      - 'version.json'

env:
  DOTNET_VERSION: '10.0.x'
  PACKAGE_PROJECT: src/WiSave.Expenses.Contracts/WiSave.Expenses.Contracts.csproj

jobs:
  publish-preview:
    runs-on: ubuntu-latest
    permissions:
      packages: write
      contents: read

    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore
        run: dotnet restore ${{ env.PACKAGE_PROJECT }}

      - name: Build
        run: dotnet build ${{ env.PACKAGE_PROJECT }} -c Release --no-restore

      - name: Pack
        run: dotnet pack ${{ env.PACKAGE_PROJECT }} -c Release --no-build -o ./nupkg

      - name: Push preview package to GitHub Packages
        run: dotnet nuget push ./nupkg/*.nupkg --source "https://nuget.pkg.github.com/JacobChwastek/index.json" --api-key ${{ secrets.GITHUB_TOKEN }} --skip-duplicate
```

Because PR refs are non-public in NBGV, the packed package version should naturally be a preview/non-public version even though the workflow does not pass additional version arguments.

- [ ] **Step 2: Validate the workflow YAML locally**

Run:
```bash
rg -n "^name:|^  pull_request:|^    branches: \\[master\\]" .github/workflows/pr-contracts-preview.yml
```
Expected: matches include `name: Publish Contracts Preview NuGet`, the `pull_request:` trigger, and `branches: [master]`.

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/pr-contracts-preview.yml
git commit -m "ci(contracts): publish preview contracts packages for pull requests"
```

---

### Task 5: Document The New Contracts Package Flow

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Add a contracts versioning section to the README**

Add this section near the existing architecture or development documentation in `README.md`:

```md
## Contracts Package Versioning

`WiSave.Expenses.Contracts` uses `Nerdbank.GitVersioning` for package versions.

- Pull requests targeting `master` publish preview packages to GitHub Packages.
- Pushes to `master` publish stable packages to the same feed.
- The package version is not hardcoded in the contracts `.csproj`; it is derived from git history and `version.json`.

The current contracts version line starts at `1.0`.
```

- [ ] **Step 2: Verify the README mentions the new flow**

Run:
```bash
rg -n "Contracts Package Versioning|Nerdbank.GitVersioning|preview packages|stable packages" README.md
```
Expected: four matching lines in the new section.

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "docs: describe contracts package versioning flow"
```

---

### Task 6: End-To-End Verification

**Files:**
- Verify: `version.json`
- Verify: `src/WiSave.Expenses.Contracts/WiSave.Expenses.Contracts.csproj`
- Verify: `.github/workflows/publish-contracts.yml`
- Verify: `.github/workflows/pr-contracts-preview.yml`
- Verify: `README.md`

- [ ] **Step 1: Build and pack the contracts project after all changes**

Run:
```bash
dotnet pack src/WiSave.Expenses.Contracts/WiSave.Expenses.Contracts.csproj -c Release
```
Expected: pack succeeds and emits a package in the `1.0.x` line.

- [ ] **Step 2: Confirm the generated nuspec/package version is not `0.1.0`**

Run:
```bash
find src/WiSave.Expenses.Contracts -path '*/obj/*' -name '*.nuspec' -o -path '*/bin/*' -name '*.nupkg'
```
Expected: the latest `.nuspec` and `.nupkg` paths reference a computed `1.0` version, not `0.1.0`.

- [ ] **Step 3: Run the solution build**

Run:
```bash
dotnet build WiSave.Expenses.slnx
```
Expected: build succeeds with the new versioning configuration in place.

- [ ] **Step 4: Review changed files**

Run:
```bash
git diff --stat HEAD~6..HEAD
```
Expected: output shows changes limited to `version.json`, the contracts project, the two workflow files, and `README.md`.

- [ ] **Step 5: Final commit**

```bash
git add version.json src/WiSave.Expenses.Contracts/WiSave.Expenses.Contracts.csproj .github/workflows/publish-contracts.yml .github/workflows/pr-contracts-preview.yml README.md
git commit -m "feat(contracts): adopt NBGV-based package versioning"
```
