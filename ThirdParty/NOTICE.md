# Sources and attribution

## Syobon Action / Cat Mario

- Original game: Chiku (ちく), 2007. Original campaign contains four stages; the random tile mode is separate and is not exposed by this integration.
- Native C# reference: https://github.com/SageChaozu/SyobonActionMono
- Pinned commit: `8a5b5f512fa94d3af1bfdac5aab804db5a7f7ace`. Complete retrieved source, README and GPL-3.0 license are preserved in `SyobonActionMono`.
- `Assets/Scripts/Syobon/SyobonEngine.cs` and `SyobonResources.cs` adapt this reference. The Unity drawing/input/audio adapters, Chinese presentation, CSV map loading and integration changes are dated 2026-10-17. All corresponding local source is provided in this Unity project; no MonoGame executable or emulator is used.
- Atlas colour keys follow the reference Content.mgcb (`153,255,255`); PNGs are losslessly converted to alpha. Audio and artwork are retained from the reference; source licensing does not confer ownership of third-party music or characters.
- Count cross-check: https://en.wikipedia.org/wiki/Syobon_Action describes the first stage followed by three additional stages; the pinned reference README explicitly lists level selection 1, 2, 3 and 4.

The Super Mario Bros. campaigns use Nintendo's game worlds and characters.
This is a local native Unity reconstruction, not an official Nintendo product.

## FullScreenMario reference data

- Original project: Josh Goldberg and the contributors listed in `FullScreenMario/Contributors.md`.
- Retrieved repository: https://github.com/pustladi/FullScreenMario
- Pinned commit: `dbfae9f19562dd3014cf26bcaa462c5589474997`.
- The repository's license text is preserved verbatim in `FullScreenMario/LICENSE`.
- The 32 map descriptions, macro expansion definitions, pixel sprite descriptions and OGG recordings are derived from this reference.
- `Tools/import-reference.cjs` executes the reference map macros offline, converts their output to JSON and lossless PNGs, and retains castle sections and transport connections.
- The reference JavaScript engine is not used at runtime. Unity runs the C# game, collision, rendering and editing tools in `Assets`.
- Source code licensing does not grant ownership of Nintendo's characters, graphics, music or trademarks.

## Original physics / HUD reference

- https://github.com/Xkeeper0/smb1
- Pinned commit: `1cc3c92cbbe8cda44903657f3da332688e806a89`.
- The repository credits doppelganger's annotated disassembly and Nintendo's original game; its README is preserved in `SMB1Reference/README-upstream.md`.
- The 8 KiB CHR data is retained for reproducible extraction of the 8×8 HUD alphabet with `Tools/import-font.py`. No executable NES ROM or emulator is included in the runtime.
- Horizontal speed/friction and vertical jump tables were checked against the annotated SMB1 NTSC listing. The native Unity collision and enemy systems are new implementations, not byte-for-byte execution of the original.

An additional read-only reference during development was SMB-Vanilla at https://github.com/nukep/SMB-Vanilla, commit `20f4e02614412f9dbd407dd4e4fab4ade5eb79da`. Its C implementation is not included or linked into this Unity build.

## Japanese Super Mario Bros. 2 / Lost Levels

- Original game: Nintendo, 1986 (Japanese Famicom Disk System release).
- Annotated disassembly by doppelganger, ca65 adaptation by threecreepio: https://github.com/threecreepio/smb2j-disassembly
- Pinned commit: `9c40114626ecd07f13e16d5e67e217b98482d7af`; original readme and source retained in `SMB2JReference`.
- `Tools/assemble-smb2j-reference.py` builds an offline extraction reference. `Tools/import-smb2j.py` invokes its original area parser with py65, extracts metatile columns, decodes enemy/transport streams, and creates native editable Unity nodes for worlds 1–8. The CHR and metatile palette tables provide the Japanese terrain artwork.
- The Unity game does not load the assembled program, py65, FDS BIOS, or an emulator. Enemy behavior, collision, platforms, and maze gates are native C# adaptations; this conversion is not a claim of complete cycle accuracy.
- The disassembly is provided as-is by its authors; Nintendo's original artwork/program data are not being relicensed as public domain.

## Chinese interface font

- Fusion Pixel Font 12px monospaced Simplified Chinese, release `2026.10.16`: https://github.com/TakWolf/fusion-pixel-font
- Source OTF and all bundled OFL/component notices retained in `FusionPixel`.
- `Tools/import-chinese-font.py` verifies the font cmap and bakes the interface's Chinese characters into point-sampled 12×12 PNG glyphs. Existing original 8×8 Latin letters and digits are retained.
