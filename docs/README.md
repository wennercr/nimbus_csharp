# Nimbus Framework – Internal Packaging & Distribution Guide

This repository contains the **Nimbus Framework**, a shared automation library intended for reuse across internal test automation solutions. Nimbus is published as a **private NuGet package** via GitLab’s Package Registry. This document outlines the required configuration, publishing process, and consumer integration steps.

## 1. Overview

Nimbus.Framework is distributed as a **versioned NuGet package** hosted inside GitLab. This enables:

- Centralized maintenance of framework code
- Versioned and controlled releases
- Lightweight test repositories
- Seamless CI/CD integration
- Secure access via GitLab CI tokens

## 2. Framework Packaging Configuration

### 2.1 NuGet Metadata in the .csproj

Add the following to `src/Nimbus.Framework/Nimbus.Framework.csproj`:

```xml
<PropertyGroup>
  <TargetFramework>net9.0</TargetFramework>
  <GeneratePackageOnBuild>false</GeneratePackageOnBuild>

  <PackageId>Nimbus.Framework</PackageId>
  <Version>1.0.0</Version>
  <Authors>Automation Engineering</Authors>
  <Company>Internal</Company>
  <Description>Nimbus automation framework for Selenium-based UI and API utilities.</Description>

  <RepositoryUrl>https://gitlab.com/<group>/nimbus-csharp</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
</PropertyGroup>
```

### 2.2 Local Build & Pack Validation

```bash
dotnet restore
dotnet build src/Nimbus.Framework/Nimbus.Framework.csproj -c Release --no-restore
dotnet pack src/Nimbus.Framework/Nimbus.Framework.csproj -c Release -o nupkg --no-build
```

A `.nupkg` file is created under `src/Nimbus.Framework/nupkg/`.

## 3. Publishing Nimbus via GitLab CI

Add to `.gitlab-ci.yml`:

```yaml
publish_nuget:
  stage: publish
  image: mcr.microsoft.com/dotnet/sdk:9.0
  needs:
    - build

  script:
    - dotnet restore
    - dotnet build src/Nimbus.Framework/Nimbus.Framework.csproj -c Release --no-restore
    - dotnet pack src/Nimbus.Framework/Nimbus.Framework.csproj -c Release -o nupkg --no-build
    - dotnet nuget push nupkg/*.nupkg         --source "$CI_API_V4_URL/projects/$CI_PROJECT_ID/packages/nuget"         --api-key "$CI_JOB_TOKEN"

  only:
    - main
```

## 4. Developer Setup (Local)

```bash
dotnet nuget add source   "https://gitlab.com/api/v4/projects/<PROJECT_ID>/packages/nuget/index.json"   --name gitlab-nimbus   --username private-token   --password <YOUR_PAT>
```

Ensure PAT includes: `read_api`, `read_package_registry`.

## 5. Consuming Nimbus in Test Repositories

### 5.1 Add NuGet.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="gitlab-nimbus" value="https://gitlab.com/api/v4/projects/<PROJECT_ID>/packages/nuget/index.json" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

### 5.2 Add Package Reference

```xml
<ItemGroup>
  <PackageReference Include="Nimbus.Framework" Version="1.0.0" />
</ItemGroup>
```

### 5.3 Test Execution

```bash
dotnet restore
dotnet test
```

## 6. GitLab CI Integration for Consumers

```yaml
test_with_nimbus:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:9.0

  script:
    - dotnet nuget add source         "https://gitlab.com/api/v4/projects/<PROJECT_ID>/packages/nuget/index.json"         --name gitlab-nimbus         --username gitlab-ci-token         --password "$CI_JOB_TOKEN"
    - dotnet restore
    - dotnet test --configuration Release
```

## 7. Release Workflow

1. Update Nimbus.Framework source code
2. Increment `<Version>` in `.csproj`
3. Merge to `main`
4. CI publishes the package
5. Consumer repos update their package reference

## 8. Recommended Repo Structure

```
nimbus-csharp/
  README.md
  docs/
  src/
    Nimbus.Framework/
      Nimbus.Framework.csproj
      nupkg/
```

## 9. Support & Ownership

- Maintainers: Automation Engineering
- Consumers: QA & CI Teams
- Location: GitLab → Project → Deploy → Package Registry
