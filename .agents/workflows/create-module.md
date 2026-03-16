---
description: Scaffolds a new module following the project's structure guidelines.
---

1.  **Determine Module Path**: Ask the user where the module should be created (e.g., `Assets/Scripts/Infra/[ModuleName]`).
2.  **Verify GUID Preservation**: Remind the agent/user that `.meta` files are critical for GUID preservation if moving folders.
3.  **Create Top-Level Folders**: Create the following directory structure:
    - `[ModulePath]/Contracts`
    - `[ModulePath]/Runtime`
    - `[ModulePath]/Container`
    - `[ModulePath]/Tests`
    - `[ModulePath]/Editor` (Optional)
    - `[ModulePath]/Assets` (Optional)
    - `[ModulePath]/Samples` (Optional)
4.  **Resolve Project Prefix (Deterministic Rule)**:
    - Use `scaffold.SCA0009.root_namespace` from `.editorconfig` if present.
    - Else use the repository's `RootNamespace`/project naming conventions.
    - Else fallback to the current assembly prefix pattern already used in this repository.
5.  **Generate Assembly Definitions**: Create `.asmdef` files:
    - `[ModulePath]/Contracts/[ProjectPrefix].[ModuleName].Contracts.asmdef`
    - `[ModulePath]/Runtime/[ProjectPrefix].[ModuleName].Runtime.asmdef` (Reference Contracts)
    - `[ModulePath]/Container/[ProjectPrefix].[ModuleName].Container.asmdef` (Reference Runtime and Contracts if needed)
    - `[ModulePath]/Tests/[ProjectPrefix].[ModuleName].Tests.asmdef` (Reference Contracts, Runtime, Container, and Test Framework as needed)
6.  **Generate Initial Contracts**:
    - Create a template interface in `Contracts/I[ModuleName].cs`.
    - Create a template installer in `Container/[ModuleName]Installer.cs` (Inheriting from `Installer` and public).
7.  **Create Container**:
    - Create `Container/[ModuleName]Container.cs` (Inheriting from `Container`).
8.  **Documentation Update**:
    - Add or update module documentation under `Docs/` following repository module-doc conventions.
    - Check for circular dependencies.
9.  **Boundary Hygiene (Best Practice)**:
    - Keep cross-module API types in `Contracts` and concrete logic in `Runtime`.
    - Default non-boundary classes to `internal`.
    - Default external dependencies to `<Module>.Contracts`; restrict `<Module>.Runtime` usage to composition roots (for example `App/Bootstrap`) and module-local wiring.
