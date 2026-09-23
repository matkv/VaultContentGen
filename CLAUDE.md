# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project does

A C# CLI tool (`vaultcontentgen`) that reads an Obsidian vault and generates Hugo site content from it. It scans the vault structure, parses markdown frontmatter (YAML), and maps folders to Hugo content sections.

## Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~ConfigServiceTests"

# Run the CLI
dotnet run --project src/VaultContentGen -- <command>

# Configure paths
dotnet run --project src/VaultContentGen -- config set --vault-path /path/to/vault --hugo-path /path/to/hugo/content

# Scan vault structure
dotnet run --project src/VaultContentGen -- scan

# Generate Hugo content, then clear <site>/public and run `hugo serve` (blocks until hugo exits)
dotnet run --project src/VaultContentGen -- generate

# Generate Hugo content only and exit (no public/ cleanup, no hugo); for automation
dotnet run --project src/VaultContentGen -- generate --no-serve

# Release build (used by the update-matkv-dev skill via bin/Release/net10.0/vaultcontentgen)
dotnet build -c Release src/VaultContentGen
```

`generate` returns a non-zero exit code if the config is missing/invalid or generation fails.

Config is persisted to `~/.config/VaultContentGen/config.json` (or OS equivalent `ApplicationData` path).

## Architecture

The project follows a simple layered structure:

- **`Program.cs`** — wires up `System.CommandLine` with three top-level commands: `config`, `scan` and `generate`
- **`Commands/`** — static command factories; each returns a `Command` object with its subcommands and actions
- **`Config/`** — `AppConfig` (record) holds all settings; `ConfigService` serializes/deserializes it as JSON
- **`Models/`** — immutable records representing the scanned vault: `ObsidianStructure` → `ObsidianSection` (recursive, with `SubSections`) → `ObsidianFile`
- **`Services/VaultScanner`** — walks the vault directory, resolves `ContentType` per folder via `AppConfig.SectionTypes`, and parses YAML frontmatter from each `.md` file

**Content type mapping:** Folders are assigned a `ContentType` (`Standard`, `Book`, `Log`, `Project`) via the `SectionTypes` dictionary in config, keyed by relative path (e.g. `"Books"` → `"Book"`). Folders listed in `IgnoredFolders` are skipped entirely.

**Index files:** A file named `Index.md` in the vault root becomes `ObsidianStructure.RootIndex`; `Index.md` inside any section folder becomes `ObsidianSection.SectionIndex`.

## Key dependencies

- `System.CommandLine` 3.0.0-preview (uses `SetAction` / `ParseResult` API — not the older `Handler` API)
- `YamlDotNet` for frontmatter parsing
- `xunit` for tests (no Moq; tests use real file system with temp directories)
