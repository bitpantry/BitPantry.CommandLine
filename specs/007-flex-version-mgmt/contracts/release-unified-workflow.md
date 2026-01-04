# Unified Release Workflow Contract

**File**: `.github/workflows/release-unified.yml`  
**Trigger**: `release-v*` tags (e.g., `release-v20260101-143052`)

## Workflow Structure

```yaml
name: Unified Package Release

on:
  push:
    tags:
      - 'release-v*'

env:
  DOTNET_VERSION: '8.0.x'
  NUGET_API_ENDPOINT: 'https://api.nuget.org/v3-flatcontainer'
  POLL_INTERVAL_SECONDS: 30
  POLL_TIMEOUT_MINUTES: 15

jobs:
  # ═══════════════════════════════════════════════════════════════════════════
  # JOB 1: Publish Core (No Dependencies)
  # ═══════════════════════════════════════════════════════════════════════════
  publish-core:
    runs-on: ubuntu-latest
    outputs:
      published: ${{ steps.detect.outputs.needs_publish }}
      version: ${{ steps.detect.outputs.version }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Detect version
        id: detect
        run: |
          # Extract version from .csproj
          VERSION=$(grep -oP '(?<=<Version>)[^<]+' BitPantry.CommandLine/BitPantry.CommandLine.csproj)
          echo "version=$VERSION" >> $GITHUB_OUTPUT
          
          # Query NuGet for existing versions
          NUGET_VERSIONS=$(curl -s "${{ env.NUGET_API_ENDPOINT }}/bitpantry.commandline/index.json" | jq -r '.versions[]')
          
          if echo "$NUGET_VERSIONS" | grep -q "^$VERSION$"; then
            echo "needs_publish=false" >> $GITHUB_OUTPUT
            echo "::notice::Skipping - version $VERSION already on NuGet"
          else
            echo "needs_publish=true" >> $GITHUB_OUTPUT
            echo "::notice::Publishing version $VERSION"
          fi
      
      - name: Build and Pack
        if: steps.detect.outputs.needs_publish == 'true'
        run: |
          dotnet restore
          dotnet build --configuration Release --no-restore
          dotnet pack BitPantry.CommandLine/BitPantry.CommandLine.csproj \
            --configuration Release --no-build --output ./nupkg \
            -p:UseProjectReferences=false
      
      - name: Publish to NuGet
        if: steps.detect.outputs.needs_publish == 'true'
        env:
          NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
        run: |
          dotnet nuget push ./nupkg/*.nupkg \
            --api-key $NUGET_API_KEY \
            --source https://api.nuget.org/v3/index.json

  # ═══════════════════════════════════════════════════════════════════════════
  # JOB 2: Publish SignalR (Depends on Core)
  # ═══════════════════════════════════════════════════════════════════════════
  publish-signalr:
    runs-on: ubuntu-latest
    needs: [publish-core]
    outputs:
      published: ${{ steps.detect.outputs.needs_publish }}
      version: ${{ steps.detect.outputs.version }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Detect version
        id: detect
        run: |
          VERSION=$(grep -oP '(?<=<Version>)[^<]+' BitPantry.CommandLine.Remote.SignalR/BitPantry.CommandLine.Remote.SignalR.csproj)
          echo "version=$VERSION" >> $GITHUB_OUTPUT
          
          NUGET_VERSIONS=$(curl -s "${{ env.NUGET_API_ENDPOINT }}/bitpantry.commandline.remote.signalr/index.json" | jq -r '.versions[]' 2>/dev/null || echo "")
          
          if echo "$NUGET_VERSIONS" | grep -q "^$VERSION$"; then
            echo "needs_publish=false" >> $GITHUB_OUTPUT
            echo "::notice::Skipping - version $VERSION already on NuGet"
          else
            echo "needs_publish=true" >> $GITHUB_OUTPUT
          fi
      
      - name: Wait for Core on NuGet
        if: steps.detect.outputs.needs_publish == 'true' && needs.publish-core.outputs.published == 'true'
        run: |
          REQUIRED_VERSION="${{ needs.publish-core.outputs.version }}"
          PACKAGE_ID="bitpantry.commandline"
          MAX_ATTEMPTS=$(( ${{ env.POLL_TIMEOUT_MINUTES }} * 60 / ${{ env.POLL_INTERVAL_SECONDS }} ))
          ATTEMPT=0
          
          while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
            VERSIONS=$(curl -s "${{ env.NUGET_API_ENDPOINT }}/$PACKAGE_ID/index.json" | jq -r '.versions[]')
            if echo "$VERSIONS" | grep -q "^$REQUIRED_VERSION$"; then
              echo "✓ Core $REQUIRED_VERSION is available on NuGet"
              exit 0
            fi
            echo "Waiting for Core $REQUIRED_VERSION... (attempt $((ATTEMPT+1))/$MAX_ATTEMPTS)"
            sleep ${{ env.POLL_INTERVAL_SECONDS }}
            ATTEMPT=$((ATTEMPT+1))
          done
          
          echo "::error::Timeout waiting for Core $REQUIRED_VERSION on NuGet"
          exit 1
      
      - name: Build and Pack
        if: steps.detect.outputs.needs_publish == 'true'
        run: |
          dotnet restore
          dotnet build --configuration Release --no-restore
          dotnet pack BitPantry.CommandLine.Remote.SignalR/BitPantry.CommandLine.Remote.SignalR.csproj \
            --configuration Release --no-build --output ./nupkg \
            -p:UseProjectReferences=false
      
      - name: Publish to NuGet
        if: steps.detect.outputs.needs_publish == 'true'
        env:
          NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
        run: |
          dotnet nuget push ./nupkg/*.nupkg \
            --api-key $NUGET_API_KEY \
            --source https://api.nuget.org/v3/index.json

  # ═══════════════════════════════════════════════════════════════════════════
  # JOB 3: Publish Client (Depends on SignalR)
  # ═══════════════════════════════════════════════════════════════════════════
  publish-client:
    runs-on: ubuntu-latest
    needs: [publish-signalr]
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Detect version
        id: detect
        run: |
          VERSION=$(grep -oP '(?<=<Version>)[^<]+' BitPantry.CommandLine.Remote.SignalR.Client/BitPantry.CommandLine.Remote.SignalR.Client.csproj)
          echo "version=$VERSION" >> $GITHUB_OUTPUT
          
          NUGET_VERSIONS=$(curl -s "${{ env.NUGET_API_ENDPOINT }}/bitpantry.commandline.remote.signalr.client/index.json" | jq -r '.versions[]' 2>/dev/null || echo "")
          
          if echo "$NUGET_VERSIONS" | grep -q "^$VERSION$"; then
            echo "needs_publish=false" >> $GITHUB_OUTPUT
            echo "::notice::Skipping - version $VERSION already on NuGet"
          else
            echo "needs_publish=true" >> $GITHUB_OUTPUT
          fi
      
      - name: Wait for SignalR on NuGet
        if: steps.detect.outputs.needs_publish == 'true' && needs.publish-signalr.outputs.published == 'true'
        run: |
          REQUIRED_VERSION="${{ needs.publish-signalr.outputs.version }}"
          PACKAGE_ID="bitpantry.commandline.remote.signalr"
          MAX_ATTEMPTS=$(( ${{ env.POLL_TIMEOUT_MINUTES }} * 60 / ${{ env.POLL_INTERVAL_SECONDS }} ))
          ATTEMPT=0
          
          while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
            VERSIONS=$(curl -s "${{ env.NUGET_API_ENDPOINT }}/$PACKAGE_ID/index.json" | jq -r '.versions[]')
            if echo "$VERSIONS" | grep -q "^$REQUIRED_VERSION$"; then
              echo "✓ SignalR $REQUIRED_VERSION is available on NuGet"
              exit 0
            fi
            echo "Waiting for SignalR $REQUIRED_VERSION... (attempt $((ATTEMPT+1))/$MAX_ATTEMPTS)"
            sleep ${{ env.POLL_INTERVAL_SECONDS }}
            ATTEMPT=$((ATTEMPT+1))
          done
          
          echo "::error::Timeout waiting for SignalR $REQUIRED_VERSION on NuGet"
          exit 1
      
      - name: Build and Pack
        if: steps.detect.outputs.needs_publish == 'true'
        run: |
          dotnet restore
          dotnet build --configuration Release --no-restore
          dotnet pack BitPantry.CommandLine.Remote.SignalR.Client/BitPantry.CommandLine.Remote.SignalR.Client.csproj \
            --configuration Release --no-build --output ./nupkg \
            -p:UseProjectReferences=false
      
      - name: Publish to NuGet
        if: steps.detect.outputs.needs_publish == 'true'
        env:
          NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
        run: |
          dotnet nuget push ./nupkg/*.nupkg \
            --api-key $NUGET_API_KEY \
            --source https://api.nuget.org/v3/index.json

  # ═══════════════════════════════════════════════════════════════════════════
  # JOB 4: Publish Server (Depends on SignalR, parallel with Client)
  # ═══════════════════════════════════════════════════════════════════════════
  publish-server:
    runs-on: ubuntu-latest
    needs: [publish-signalr]
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Detect version
        id: detect
        run: |
          VERSION=$(grep -oP '(?<=<Version>)[^<]+' BitPantry.CommandLine.Remote.SignalR.Server/BitPantry.CommandLine.Remote.SignalR.Server.csproj)
          echo "version=$VERSION" >> $GITHUB_OUTPUT
          
          NUGET_VERSIONS=$(curl -s "${{ env.NUGET_API_ENDPOINT }}/bitpantry.commandline.remote.signalr.server/index.json" | jq -r '.versions[]' 2>/dev/null || echo "")
          
          if echo "$NUGET_VERSIONS" | grep -q "^$VERSION$"; then
            echo "needs_publish=false" >> $GITHUB_OUTPUT
            echo "::notice::Skipping - version $VERSION already on NuGet"
          else
            echo "needs_publish=true" >> $GITHUB_OUTPUT
          fi
      
      - name: Wait for SignalR on NuGet
        if: steps.detect.outputs.needs_publish == 'true' && needs.publish-signalr.outputs.published == 'true'
        run: |
          REQUIRED_VERSION="${{ needs.publish-signalr.outputs.version }}"
          PACKAGE_ID="bitpantry.commandline.remote.signalr"
          MAX_ATTEMPTS=$(( ${{ env.POLL_TIMEOUT_MINUTES }} * 60 / ${{ env.POLL_INTERVAL_SECONDS }} ))
          ATTEMPT=0
          
          while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
            VERSIONS=$(curl -s "${{ env.NUGET_API_ENDPOINT }}/$PACKAGE_ID/index.json" | jq -r '.versions[]')
            if echo "$VERSIONS" | grep -q "^$REQUIRED_VERSION$"; then
              echo "✓ SignalR $REQUIRED_VERSION is available on NuGet"
              exit 0
            fi
            echo "Waiting for SignalR $REQUIRED_VERSION... (attempt $((ATTEMPT+1))/$MAX_ATTEMPTS)"
            sleep ${{ env.POLL_INTERVAL_SECONDS }}
            ATTEMPT=$((ATTEMPT+1))
          done
          
          echo "::error::Timeout waiting for SignalR $REQUIRED_VERSION on NuGet"
          exit 1
      
      - name: Build and Pack
        if: steps.detect.outputs.needs_publish == 'true'
        run: |
          dotnet restore
          dotnet build --configuration Release --no-restore
          dotnet pack BitPantry.CommandLine.Remote.SignalR.Server/BitPantry.CommandLine.Remote.SignalR.Server.csproj \
            --configuration Release --no-build --output ./nupkg \
            -p:UseProjectReferences=false
      
      - name: Publish to NuGet
        if: steps.detect.outputs.needs_publish == 'true'
        env:
          NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
        run: |
          dotnet nuget push ./nupkg/*.nupkg \
            --api-key $NUGET_API_KEY \
            --source https://api.nuget.org/v3/index.json

  # ═══════════════════════════════════════════════════════════════════════════
  # JOB 5: Create GitHub Release (After all publishing completes)
  # ═══════════════════════════════════════════════════════════════════════════
  create-release:
    runs-on: ubuntu-latest
    needs: [publish-core, publish-signalr, publish-client, publish-server]
    steps:
      - uses: actions/checkout@v4
      
      - name: Generate release body
        id: release-body
        run: |
          echo "## Packages Published" > release-notes.md
          echo "" >> release-notes.md
          
          if [ "${{ needs.publish-core.outputs.published }}" == "true" ]; then
            echo "- ✅ **BitPantry.CommandLine** ${{ needs.publish-core.outputs.version }}" >> release-notes.md
          else
            echo "- ⏭️ BitPantry.CommandLine ${{ needs.publish-core.outputs.version }} (skipped - already on NuGet)" >> release-notes.md
          fi
          
          if [ "${{ needs.publish-signalr.outputs.published }}" == "true" ]; then
            echo "- ✅ **BitPantry.CommandLine.Remote.SignalR** ${{ needs.publish-signalr.outputs.version }}" >> release-notes.md
          else
            echo "- ⏭️ BitPantry.CommandLine.Remote.SignalR ${{ needs.publish-signalr.outputs.version }} (skipped)" >> release-notes.md
          fi
          
          # Note: Client/Server outputs would need to be added to those jobs
          echo "" >> release-notes.md
          echo "---" >> release-notes.md
          echo "" >> release-notes.md
      
      - name: Create GitHub Release
        uses: softprops/action-gh-release@v1
        with:
          name: "Release ${{ github.ref_name }}"
          body_path: release-notes.md
          generate_release_notes: true
          append_body: true
```

## Job Dependency Graph

```
publish-core (no deps)
       │
       ▼
publish-signalr (needs: publish-core)
       │
       ├─────────────────┐
       ▼                 ▼
publish-client     publish-server
(needs: signalr)   (needs: signalr)
       │                 │
       └────────┬────────┘
                ▼
        create-release
(needs: core, signalr, client, server)
```

## Version Detection Logic

Each job performs this check:

1. **Extract .csproj version**: `grep -oP '(?<=<Version>)[^<]+' path/to/project.csproj`
2. **Query NuGet**: `curl -s https://api.nuget.org/v3-flatcontainer/{package-id}/index.json`
3. **Compare**: If version exists in NuGet response → skip; otherwise → publish

## NuGet Availability Polling

When a dependent job needs to wait:

| Parameter | Value |
|-----------|-------|
| Endpoint | `https://api.nuget.org/v3-flatcontainer/{package-id}/index.json` |
| Interval | 30 seconds |
| Timeout | 15 minutes (30 attempts) |
| Success | Version found in response `versions` array |
| Failure | Exit 1 with error message |

## Output Variables

| Job | Output | Description |
|-----|--------|-------------|
| publish-core | `published` | `true` if package was published |
| publish-core | `version` | Version number from .csproj |
| publish-signalr | `published` | `true` if package was published |
| publish-signalr | `version` | Version number from .csproj |

## Environment Variables

| Variable | Value | Description |
|----------|-------|-------------|
| `DOTNET_VERSION` | `8.0.x` | .NET SDK version |
| `NUGET_API_ENDPOINT` | `https://api.nuget.org/v3-flatcontainer` | NuGet API base URL |
| `POLL_INTERVAL_SECONDS` | `30` | Seconds between NuGet polls |
| `POLL_TIMEOUT_MINUTES` | `15` | Maximum wait time for dependency |

## Secrets Required

| Secret | Description |
|--------|-------------|
| `NUGET_API_KEY` | NuGet.org API key with push permissions |
