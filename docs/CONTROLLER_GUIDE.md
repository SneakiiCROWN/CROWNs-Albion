# Controller guide — CROWN’s ALBION v1.6.1

Xbox navigation is ON for new settings. An existing explicit OFF preference is respected. First XInput controller only; Windows 10/11. Release buttons after a window/focus change before navigating.

| Control | Action |
|---|---|
| L3 + R3 together | Toggle entire menu; both must release before another toggle. Single stick clicks do nothing. |
| D-pad Up / Down | Select feature; hold for repeat. |
| D-pad Left / Right | Previous / next category. |
| A | Select: open exact amount for integer features; queue other selected features. No direct execution. |
| LB / RB | Adjust current value; LT ×10 / RT ×100. |
| X | Favourite. |
| Back / View | Review queue. |
| Hold Y / Start, 0.65 seconds | Attempt selected / queue execution through all existing safety checks. |
| Tap B | Cancel child dialog; in main clear search, return to All, then hide. Tap resolves on release. |
| Hold B, 0.65 seconds | Close all menu windows and attempt to return focus to Fable. |

Exact amount: LB/RB ±1; Up/Down ±1,000; Left/Right choose quick presets. A uses value and returns, X uses + queues, B cancels. Typed entry, all original presets, slider and amount limits remain. A chosen amount alone never executes gameplay.

Queue review: Up/Down select; A loads the queued value into the main editor; X removes; hold Y moves up; hold Start moves down. B returns. As before, updating an existing queued entry after editing uses its mouse context menu; the controller can remove and requeue instead.

Research library: Up/Down select; Left/Right category; A queues an eligible experimental unlocker; X copies identifier; hold Y copies Lua. B returns. All existing research safety checks remain.

Native Yes/No prompts: A activates the focused confirm/decline button; D-pad moves focus; B declines/cancels. Hold B or L3+R3 cancels the prompt and closes the menu. Folder/file pickers retain normal Windows keyboard/mouse controls.

F8 and keyboard/mouse controls remain. Co-op protection defaults ON. Controller navigation runs only in foreground ALBION windows; the toggle also works from foreground Fable. Other applications are left alone. No global controller interception is installed. Physical-controller and live co-op acceptance remain pending; see QA_v1_6_1.md.

All popups now share explicit ownership. Escape/B cancels the active popup; Enter/A confirms. Hold B closes nested popups before hiding. With Fable closed, F8 or the tray reopens the menu. Native open/save/folder picker confirmation/cancellation callbacks are regression-tested; normal Windows keyboard/mouse controls remain.
