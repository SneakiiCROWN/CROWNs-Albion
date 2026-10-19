# v1.6 controller UX QA — 17 October 2026

Built on Windows with the installed .NET Framework 4.x C# compiler: success, zero warnings/errors. `CROWNsAlbion.exe` is built from the included source. The launcher rebuilds before starting.

Passed: 37 executable C# checks covering L3+R3 debounce/rearm, A edges, held A across window transitions, tap/hold B with no extra back, Y/Start hold gates, D-pad repeat, reset/disconnect and trigger neutral gates, plus native WinForms catalogue load, default protections, exact 25,000 queue entry, amount/queue/research route lifecycle, and native Yes/No confirmation callbacks. These use simulated samples and programmatic UI actions, not a physical controller. See `controller-test-results.txt`.

Passed: 15 inherited Python regression checks, with expectations updated only for the intentionally replaced controller mapping, hold implementation and version number. Package and reference check scripts pass separately. Native form bitmap renders of the main menu and exact amount dialog were visually inspected; fixed header/footer docking, clipped amount label and queue/button overlap. These renders are not gameplay screenshots or high-DPI acceptance.

Preservation: 95 catalogue entries, 3,229 research entries and 244 unlocker candidates retained. `catalog.json`, `references.json`, `unlocker_commands.json` are byte-identical to the supplied v1.5 ZIP. Source from Favourite through dispatch/preparation/status functions is identical to v1.5: cheat compilation, exact validation, execution preflights, risk/co-op gates, script writing/cleanup and queue persistence retain their implementation.

No game scripts were dispatched. No live game, injector, XLiveLessNess, saves or game files were modified for testing. Co-op protection defaults ON. The original ZIP is retained at its original Downloads path; this package includes a focused unified source diff for review.

Not tested: physical Xbox controller, actual XInput timing/hardware reconnect, live Fable focus handoff, exclusive fullscreen, high-DPI/scaled layouts, live cheats, injector or co-op sessions. XLLN compatibility is protected by retaining the existing guards and avoiding hooks, injection, XInputEnable or changes to xlive files; it is not proven by these tests. This standalone menu cannot suppress a game that deliberately polls controller input in the background. Foreground switching depends on Windows and the game. Use windowed/borderless mode for live acceptance.

Next live checks: with co-op protection ON, toggle from foreground Fable with L3+R3; verify no double toggle or stick-click actions, no gameplay behind the focused menu, A opens integer amount picker, X queues 25,000 without dispatch, B cancels to parent, hold B closes all menu windows, and gameplay input/focus returns. Alt-tab and reconnect with A/Y/Start held: no accidental action. Check both co-op PCs with protection ON before considering separate offline cheat testing.
