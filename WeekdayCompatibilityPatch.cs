#nullable enable

using HarmonyLib;
using Lua;

namespace LuaRoutineExtensions;

// Runtime-only alias: do not add this to luaAccessibleFunctions, which the
// game also uses to build creator documentation and AI generation prompts.
[HarmonyPatch(typeof(LuaEntity), "Start")]
internal static class WeekdayCompatibilityPatch
{
    private static void Postfix(LuaState ___luaState)
    {
        if (___luaState.Environment["World"].TryRead<LuaTable>(out var world)
            && world["GetWeekday"].Equals(LuaValue.Nil)
            && world["GetCurrentDayOfWeek"].TryRead<LuaFunction>(out var weekday))
        {
            // Reuse the actual stock callback so results and behavior match.
            // Preserve an existing alias if another mod/game version adds one.
            world["GetWeekday"] = weekday;
        }
    }
}
