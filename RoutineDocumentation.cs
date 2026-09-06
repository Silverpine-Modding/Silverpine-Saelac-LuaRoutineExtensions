#nullable enable

using System;
using System.Collections.Generic;

namespace LuaRoutineExtensions;

// Sent to the LLM through the game's catalog and LuaEntity.GetDocumentation.
internal static class RoutineDocumentation
{
    internal const string EventActivity =
        "Boolean: true when the event is active for TODAY. It is a day-level " +
        "schedule flag, not the NPC's current location, shop status, or free time. " +
        "False for invalid names or unavailable state. Event meanings:\n" +
        "- HaydensDayOff: Hayden's bakery is CLOSED today. Do not choose bakery " +
        "shopping because this is true. Social visits need a suitable supplied routine.\n" +
        "- RosalynVisitingFriend: Rosalyn visits Isolde in the CAPITAL today; " +
        "this does not mean she is available in her Silverpine shop.\n" +
        "- CelandineVisitingCapital: Celandine travels to the capital today.\n" +
        "- AldricSleepIn: Aldric starts his morning shop routine later today " +
        "(normally 10 instead of 8 in unmodified 1.7.3); he is not asleep all day.\n" +
        "- OrianaMountains: Oriana visits the mountains today.\n" +
        "- MirelAtTavern: Mirel is scheduled to stay at Gareth's tavern tonight.\n" +
        "- OrsonAtTavern: Orson is scheduled to visit/stay at Gareth's tavern today.\n" +
        "Travel and return times still matter. A tavern event does not place its " +
        "NPC there at every hour. For shopping NOW, prefer NPC.IsVendorActive.";

    internal const string EventDays =
        "Number: 0 means the event's scheduled day is today, positive values " +
        "are days remaining, and -1 means invalid/unavailable. Compare explicitly: " +
        "Events.GetDaysUntil(\"HaydensDayOff\") == 1 means tomorrow. Lua treats " +
        "both 0 and -1 as true, so never use this number as a boolean. " +
        "Use Events.IsActive for actual active-today status: conditional event " +
        "suppression can differ from the countdown (for example Mirel/Orson). " +
        "Do not recreate event intervals with day modulo arithmetic.";

    internal const string NpcNameInput =
        "npcName is a quoted, case-insensitive loaded NPC name, e.g. \"Aldric\" " +
        "or a custom NPC's final given name. Use actual supplied names, not " +
        "filenames, surnames, display titles, routine IDs, or a 'self' placeholder.";

    internal const string VendorActivity =
        "Boolean: true only when the named loaded NPC has an active Vendor " +
        "component NOW; false when closed, not a vendor, or unavailable. " +
        "Use this to gate a supplied shopping/selling routine during an errand " +
        "window. A false event flag or an assumed opening hour does not establish " +
        "that a shop is open. This query does not open shops, move NPCs, check " +
        "stock, reserve a visit, or predict availability on arrival.";

    internal const string BaseRoutineActivity =
        "Boolean: exact, case-sensitive comparison with the NPC's underlying " +
        "routine activity, ignoring temporary overrides; false if unavailable. " +
        "The activity argument is NOT a routine ID, visible activity label, " +
        "pathing text, or guessed synonym. Use only a known exact activity; " +
        "otherwise choose another documented check. It does not establish the " +
        "NPC's current location or guarantee they can be visited.";

    internal const string GivenKeys =
        "Boolean: true when this NPC has given keys to the PLAYER; false if " +
        "unavailable. This is not permission for the custom character to enter " +
        "that NPC's house, proof a door is unlocked, or a friendship check.";

    internal const string WithinDistance =
        "Boolean: true when the two loaded NPCs' straight-line tile distance " +
        "is <= maximumDistance; false for missing NPCs or invalid distance. " +
        "Ignores walls, doors, path length and travel feasibility. It does not " +
        "find a meeting place, move either NPC, or prove either is free.";

    internal const string ContextHeader = "--- CONTEXT ---";
    internal const string GuidanceHeader = "--- ROUTINE AUTHORING GUIDANCE (Lua Routine Extensions) ---";

    internal const string AuthoringGuidance = GuidanceHeader + "\n" +
        "1. Output only Lua. Start with function GetTargetRoutineID(hour) and " +
        "finish with end. Put helpers and local variables inside that function; " +
        "no leading comments, markdown fences, imports, or prose. The automatic " +
        "generator checks this format and has a limited output budget.\n" +
        "2. hour is an integer from 0 through 23. Cover every hour, including " +
        "overnight periods, and always return exactly one valid routine-ID " +
        "string. Use only exact IDs listed in CONTEXT, plus silverpine_leisure. " +
        "Never return nil, booleans, numbers, tables, activities, or NPC names. " +
        "Give every supplied ID a sensible reachable branch when requested.\n" +
        "3. This script only SELECTS existing routines. Destinations, movement, " +
        "sleeping, vendor behavior and activity labels come from their saved " +
        "definitions. Returning a shop routine on a day off does not turn it " +
        "into a social visit. Do not invent routines, coordinates, or actions " +
        "to repair a mismatched definition. Use silverpine_leisure as a general " +
        "fallback; it selects base-game leisure/shopping, not necessarily home.\n" +
        "4. Call only functions in AVAILABLE FUNCTIONS and supported standard " +
        "libraries, using Table.Function(...) syntax. The day API is " +
        "World.GetCurrentDay(), never Game.GetCurrentDay(). World day starts at " +
        "1. Compare weekday, season and weather strings with the documented " +
        "capitalization. require, OS, file, network and Unity APIs are unavailable.\n" +
        "5. Put sleep, meals and required work windows before optional errands " +
        "and visits. Use clear hour ranges such as hour >= 9 and hour < 12, " +
        "and hour >= 22 or hour < 6 for overnight sleep. Query only relevant " +
        "events/NPCs inside their activity windows; do not fetch every event. " +
        "Gate shop visits on NPC.IsVendorActive, and use a valid alternative " +
        "when a vendor or other NPC is unavailable.\n" +
        "6. Routine selection may repeat within an hour, and the entire Lua " +
        "chunk is evaluated again each time. Prefer stable daily variety such " +
        "as World.GetCurrentDay() % 3 == 0 for optional errands. Uncached " +
        "math.random() can change the choice on every call. Do not reset " +
        "math.randomseed each call or assume script variables survive saves.\n" +
        "7. Events and NPC are provided by this mod, not vanilla Silverpine. " +
        "For portable scripts, guard optional calls, e.g. if NPC and " +
        "NPC.IsVendorActive and NPC.IsVendorActive(\"Hayden\") then ... end. " +
        "Likewise check Events and Events.IsActive before calling it. Missing " +
        "tables/functions must lead to a valid fallback; a return-type guard " +
        "does not catch errors thrown inside the original function.\n" +
        "8. Before answering, check all hours, return IDs, function/name " +
        "spellings, event meanings, unavailable-NPC fallbacks, and reachable " +
        "branches. Keep the script short and finish every if/function with end.";

    // Exact stock 1.7.3 instruction. Do not rewrite character lore or scripts.
    internal const string StockVarietyInstruction =
        "Make sure to add some variety between days using, for example, " +
        "math.random() < 0.X or Game.GetCurrentDay() % X == 0. " +
        "Answer with just the Lua script.";

    internal const string ImprovedVarietyInstruction =
        "Add stable variety between days where suitable, for example " +
        "World.GetCurrentDay() % 3 == 0 for an optional errand. " +
        "Use only the supplied routine IDs and documented functions. " +
        "Answer with just the complete Lua script, starting with " +
        "function GetTargetRoutineID(hour) and ending with end.";

    internal static string Enrich(string documentation)
    {
        int contextIndex = documentation.IndexOf(ContextHeader, StringComparison.Ordinal);
        if (contextIndex < 0 ||
            documentation.IndexOf("GetTargetRoutineID", contextIndex, StringComparison.Ordinal) < 0 ||
            documentation.IndexOf(GuidanceHeader, StringComparison.Ordinal) >= 0)
            return documentation;

        // Keep the caller's NPC-specific context verbatim; invoke no callback.
        return documentation.Insert(contextIndex, AuthoringGuidance + "\n\n");
    }

    internal static List<string> CorrectGenerationDialog(List<string> dialog)
    {
        List<string>? corrected = null;
        for (int i = 0; i < dialog.Count; i++)
        {
            string prompt = dialog[i];
            if (!prompt.EndsWith(StockVarietyInstruction, StringComparison.Ordinal) ||
                prompt.IndexOf("--- AVAILABLE FUNCTIONS ---", StringComparison.Ordinal) < 0 ||
                prompt.IndexOf("GetTargetRoutineID(hour)", StringComparison.Ordinal) < 0 ||
                prompt.IndexOf("Create a daily routine script for an NPC with the following character description:", StringComparison.Ordinal) < 0)
                continue;

            corrected ??= new List<string>(dialog);
            corrected[i] = prompt.Substring(0, prompt.Length - StockVarietyInstruction.Length) +
                ImprovedVarietyInstruction;
        }
        return corrected ?? dialog;
    }
}
