# Lua Routine Extensions

**Version 1.1.0 — Silverpine 1.7.3**

Adds six read-only schedule/NPC checks to custom-character Lua, with documentation
designed to help the game's LLM generate valid, sensible routines. Both the Lua
editor's **copy documentation** button and automatic routine generation receive
the expanded function descriptions and routine-authoring guidance.

## Installation and upgrading

Requires BepInEx 5. Place `LuaRoutineExtensions.dll` in
`BepInEx/plugins/LuaRoutineExtensions/` and restart Silverpine. Replace the older
DLL in that folder; keep only one installed copy. Harmony is supplied by BepInEx.
Modding Tools, Runtime NPC Editor, and Vendor Inventory Manager are not required.

Existing scripts keep working with the same six functions. Updating documentation
does **not** regenerate or edit saved character scripts or possible routines.
Use the updated copied documentation when requesting a new script, or regenerate
through your usual character-editor workflow after restarting. Merely loading a
character with an existing nonempty script does not regenerate it.

When sharing a character that calls `Events` or `NPC` unconditionally, include
this mod as a requirement. In vanilla Silverpine those tables are absent, causing
an `attempt to index a nil value` Lua error. The example below demonstrates
guards and valid fallback routines so optional queries can be skipped when the
mod is absent. A guard that only checks the final return type cannot catch an
error thrown while evaluating the routine function.

## What reaches the LLM

The game uses `LuaEntity.GetDocumentation` to build the function catalog for both
automatic generation and clipboard documentation. This version:

- Explains each event's actual schedule meaning, including closed shops and
  capital visits, and distinguishes daily plans from current availability.
- Documents return types, invalid/unavailable results, exact activity matching,
  player-specific keys, and straight-line distance limitations.
- Adds a routine-only guidance section covering valid IDs, all 24 hours,
  fallbacks, optional-mod guards, and stable daily variety.
- Corrects the stock automatic-generation prompt's invalid
  `Game.GetCurrentDay()` example to `World.GetCurrentDay()` and removes its
  unresolved `0.X`/`X` placeholders. This narrowly matches the stock routine
  prompt; other generation requests and character descriptions are preserved.

The guidance is embedded in the DLL; installing this README alone has no effect
on generation. Model output still needs review. Better documentation cannot fix
incorrect destinations, activity labels, or arguments stored in a character's
possible routines.

## Events

### `Events.IsActive(eventName)` → boolean

True means the event is active **today**, not that an NPC is at a location right
now or available to meet. Invalid event names and unavailable state return false.
Names are quoted strings and case-insensitive.

| Event | Meaning and correct use |
| --- | --- |
| `HaydensDayOff` | Hayden's bakery is **closed** today. Do not schedule bakery shopping because this is true. A social visit requires a suitable existing routine. |
| `RosalynVisitingFriend` | Rosalyn visits Isolde in the **capital**. This is not a flag that her Silverpine shop is open. |
| `CelandineVisitingCapital` | Celandine travels to the capital that day. |
| `AldricSleepIn` | Aldric starts his morning shop routine later (10 instead of 8 in unmodified 1.7.3); he is not asleep all day. Check vendor activity for shopping. |
| `OrianaMountains` | Oriana visits the mountains that day. |
| `MirelAtTavern` | Mirel is scheduled to stay at Gareth's tavern that night. |
| `OrsonAtTavern` | Orson is scheduled to visit/stay at Gareth's tavern that day. |

Travel and return times still matter. A tavern event can motivate an evening
tavern routine, but does not prove the named NPC has arrived. Use
`NPC.IsVendorActive` for shop availability now rather than guessed opening hours
or the absence of a day-off event.

### `Events.GetDaysUntil(eventName)` → number

- `0`: the event's scheduled day is today.
- Positive: days remaining; `== 1` means tomorrow.
- `-1`: invalid name or unavailable state.

Compare explicitly: `Events.GetDaysUntil("HaydensDayOff") == 1`.
Lua treats **both 0 and -1 as true**, so `if Events.GetDaysUntil(...) then` is
incorrect. For active-today status use `Events.IsActive`: conditional event
suppression, such as Mirel/Orson scheduling, can differ from the countdown.
Do not reconstruct base event intervals with modulo arithmetic.

## NPC queries

Pass actual quoted base NPC enum names or loaded custom NPC final names, such as
`"Aldric"`. Name comparisons are case-insensitive. Do not use SILC filenames,
family surnames, titles, routine IDs, or an invented `"self"` name. Unknown or
unavailable NPCs return false and produce a warning once per unknown name.

### `NPC.IsVendorActive(npcName)` → boolean

Checks whether the loaded NPC has an active Vendor component **now**. False
means closed, not a vendor, or unavailable. It does not open the shop, verify
stock, move anyone, reserve a visit, or predict availability on arrival.

### `NPC.IsBaseRoutineActivity(npcName, activity)` → boolean

Compares the underlying non-override routine's exact internal `activity` text.
The activity comparison is **case-sensitive**. An ID like `"shop_at_aldric"`,
visible activity label, pathing text, or guessed synonym is not interchangeable
with the internal activity. Use this only when the exact activity is known.
Temporary overrides and travel can make the visible/current behavior different.

### `NPC.HasGivenKeys(npcName)` → boolean

Checks whether the NPC gave keys to the **player**. This does not grant the custom
NPC entry permission, prove a door is unlocked, or measure friendship.

### `NPC.IsWithinDistance(firstNpcName, secondNpcName, maximumDistance)` → boolean

Compares straight-line tile distance with a finite, non-negative maximum using
`distance <= maximumDistance`. Missing NPCs and invalid distances return false.
Walls, doors, path length, and travel feasibility are ignored. This neither
finds a meeting place nor checks that either NPC is free.

All six queries are read-only. Wrong argument types/counts can still raise Lua
argument errors; use the documented strings/numbers.

## Routine generation rules

1. Output only Lua, starting with `function GetTargetRoutineID(hour)` and ending
   with `end`. Automatic generation rejects leading prose, comments, fences, or
   top-level helpers. Put any helpers and local variables inside the function.
2. `hour` is an integer from 0 through 23. Cover every hour and always return a
   string matching an existing possible-routine ID or `"silverpine_leisure"`.
   Use every supplied ID in a sensible reachable branch when requested.
3. Returning an ID selects its saved destination, activities, and arguments.
   It does not create routines or change a shop visit into a social call.
   `silverpine_leisure` selects base leisure/shopping; it does not mean home.
4. Use only documented functions with `Table.Function(...)` syntax. The day
   function is **`World.GetCurrentDay()`**, starting at 1. Match the documented
   weekday, weather, and season capitalization. OS, file, network, Unity, and
   `require` APIs are unavailable.
5. Prioritize sleep, meals, and work, then add optional errands in specific hour
   windows. Query only relevant events/NPCs. When a check fails, return another
   available routine instead of assuming the destination is accessible.
6. Selection can repeat within an hour, and the game re-evaluates the entire
   script chunk each call. Prefer stable choices like `day % 3 == 0` for optional
   errands. Repeated `math.random()` calls can change activities within one hour;
   resetting `math.randomseed` on each call is not a solution. Script variables
   are not saved between sessions or save switching.
7. For portability, guard optional `Events`/`NPC` table and function access and
   use valid fallbacks. Base-game `World` functions remain available.

## Complete example

[Examples/daily-routine.lua](Examples/daily-routine.lua) demonstrates stable
errand days, open-vendor checks, evening event use, complete hourly coverage,
and optional-mod guards. It expects these **predefined** custom routine IDs:

`sleep`, `eating`, `working`, `shop_at_bakery`, `shop_at_herbalist`, `drink_at_tavern`.

Adapt those IDs to the character's actual list. Do not paste the example into a
character lacking them. Its bakery and herbalist routines must target those
shops, and its sleep routine must have the correct destination and sleep argument.

For example, the bakery branch uses current vendor activity rather than the
day-off event:

```lua
if hour >= 9 and hour < 11 and day % 3 == 0 then
    if NPC and NPC.IsVendorActive and NPC.IsVendorActive("Hayden") then
        return "shop_at_bakery"
    end
end
```

If Hayden is closed, unavailable, or the extension is absent, execution continues
to the example's work/leisure fallback. `day` comes from `World.GetCurrentDay()`.

## Changelog

### 1.1.0

- Expanded the documentation embedded in all six Lua function registrations.
- Added routine-authoring guidance to routine documentation, preserving the
  caller's context and avoiding duplicate sections.
- Corrected the stock automatic generation prompt's day API and variety example.
- Replaced the incorrect day-off bakery README example with a complete guarded
  schedule in `Examples/daily-routine.lua`.
- Kept all six Lua query signatures and runtime behavior compatible with 1.0.0.

## Credits

Created by **Saelac and ChatGPT**.
