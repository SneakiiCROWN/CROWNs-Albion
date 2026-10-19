# CROWN's ALBION v1.5 — Test scope and release blockers

## Static/offline validation

- Original 95 command templates and 3,229 references preserved byte-for-byte.
- Source-level regression checks for exact amounts, quick presets, focus guards, co-op defaults, queue protections, and sidebar layout, queue-review controls, and hold-to-confirm shortcuts.
- Syntactic delimiter checks for the C# source (NOT a compiler or Windows runtime check).
- Package integrity test, no external executable or third-party multiplayer DLL bundled.

## NOT verified

- C# compiler success on Windows, actual WinForms rendering and key event propagation.
- F8 activation/focus restoration during borderless Fable III; behaviour on a multi-monitor desktop.
- Physical controller polling/navigation, XInput variants, stick clicks or accidental gameplay input.
- Fable III runtime Lua command execution; injector load; duplicate grants; save persistence.
- Any compatibility with XLiveLessNess co-op, business partnerships, shared gold/rent or saves.

## Acceptance sequence on Windows

1. Run `CROWN_DIAGNOSE.cmd`, then `CROWN_ALBION.cmd`; capture exact compiler output on failure.
2. With the game **not running**, test Ctrl+G and the Exact Amount pad: enter 1, 25,000, 100,000, queue, update, review/reorder/remove in Ctrl+Q and clear. Verify no game changes occur.
3. With Fable running borderless, press F8 to show and F8/Esc to hide. Verify focus returns to game. Open an unrelated app and verify F8 cannot steal focus.
4. Opt into controller and test navigation with Fable/menu focused, then with a different app focused; avoid unsafe actions. Verify R3 amount pad, A use, X queue and B cancel.
5. Test `Co-op Protection` blocks a selected gold command, even if it is queued. No injector needed to check the guard.
6. Only if a separate injector genuinely works in an isolated offline copy with a throwaway save, test `test-message`, then a reversible change, then gold in a spare save. Confirm no double grant before touching inventory changes.
7. Never promise or ship 'fully working Menyoo' until above is verified. In co-op specifically, avoid experiments on main saves until the original co-op features have been retested.
