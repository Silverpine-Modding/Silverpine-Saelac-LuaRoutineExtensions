# Lua Routine Extensions

Lua Routine Extensions adds read-only base-game schedule checks to the Lua
environment used by Silverpine custom characters. The functions are registered
in the game's own Lua function catalog, so they are also included in the
creator-facing Lua documentation and AI routine-generation context.

## Installation

Place `LuaRoutineExtensions.dll` in
`BepInEx/plugins/LuaRoutineExtensions/` and restart Silverpine.

## Event functions

### `Events.IsActive(eventName)`

Returns whether a base-game schedule event is active today.

### `Events.GetDaysUntil(eventName)`

Returns the number of days until a base-game schedule event. It returns `-1`
if the name is invalid or the relevant game state is unavailable.

Supported event names:

- `MirelAtTavern`
- `HaydensDayOff`
- `RosalynVisitingFriend`
- `CelandineVisitingCapital`
- `AldricSleepIn`
- `OrianaMountains`
- `OrsonAtTavern`

Event names are case-insensitive.

## NPC functions

### `NPC.IsVendorActive(npcName)`

Returns whether the named loaded NPC has an active `Vendor` component.

### `NPC.IsBaseRoutineActivity(npcName, activity)`

Returns whether the named NPC's underlying non-override routine has the exact
internal activity text. This follows the base game's own
`NPCRoutineExecutor.IsBaseRoutineActivitySpecified` check.

### `NPC.HasGivenKeys(npcName)`

Returns whether the named NPC has given their keys to the player.

### `NPC.IsWithinDistance(firstNpcName, secondNpcName, maximumDistance)`

Returns whether the loaded NPCs are at most `maximumDistance` world tiles apart
using Euclidean tile distance.

NPC names are case-insensitive and may be base NPC enum names or the final names
of loaded custom NPCs. An unknown or unavailable NPC produces `false` and one
warning in the BepInEx log rather than breaking routine selection.

## Example

```lua
function GetTargetRoutineID(hour)
    if Events.IsActive("HaydensDayOff") then
        return "visit_bakery"
    end

    if hour >= 18 and NPC.IsVendorActive("Aldric") then
        return "shop_at_aldric"
    end

    if NPC.IsWithinDistance("Oriana", "Celandine", 50) then
        return "visit_friend"
    end

    return "silverpine_leisure"
end
```

Every returned custom routine ID must exist in that character's possible
routines. The built-in `silverpine_leisure` ID remains available.

## Credits

Created by **Saelac and ChatGPT**.
