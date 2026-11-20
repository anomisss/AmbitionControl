---
phase: 05-release-packaging
plan: 01
status: published
completed: 2026-11-19
requirements: [REL-01]
---

# 05-01 Summary: Release package v0.6.0-alpha.2

**Package:** `dist/AmbitionControl-v0.6.0-alpha.2-ea3233-melonloader.zip` (git-ignored; local only)
- ZIP SHA-256: `67bb8831b6d136d54faeaa7d40e0abe872a4efefb688a9c144010642d0a075a3` (45,080 bytes)
- Contents (verified by extraction): `AmbitionControl.dll` (65,024 bytes), `README.txt`, `CHANGELOG.md`, `SHA256SUMS.txt`
- DLL SHA-256 `f2ef90364160680fc758faa49e75ec2fbed64a4728fd877e3d8265f1c11bbd0a` = the exact binary tested in-game
  (04-02, commit bff5c78); the script refuses any other hash (`-ExpectedSha256`).
- No game assemblies, saves, logs or local paths included.

**Reproduce:** `powershell -ExecutionPolicy Bypass -File packaging\package.ps1 -Version 0.6.0-alpha.2 -ExpectedSha256 f2ef9036...`

**Published (REL-02, owner-approved 2026-11-19):** annotated tag `v0.6.0-alpha.2` → 06c7bce; GitHub pre-release https://github.com/anomisss/AmbitionControl/releases/tag/v0.6.0-alpha.2 with the ZIP as its only asset (45,080 bytes). Asset re-downloaded and SHA-256 matched `67bb8831…`.

**License:** none in the repository; README says so rather than inventing one.
