#nullable enable

using System.Collections.Generic;
using HarmonyLib;

namespace LuaRoutineExtensions;

[HarmonyPatch(typeof(LuaEntity), nameof(LuaEntity.GetDocumentation))]
internal static class RoutineDocumentationPatch
{
    private static void Postfix(ref string __result)
    {
        __result = RoutineDocumentation.Enrich(__result);
    }
}

[HarmonyPatch(typeof(InferenceServerSetupHandler), nameof(InferenceServerSetupHandler.GenerateSimpleLowTemp))]
internal static class RoutineGenerationPromptPatch
{
    private static void Prefix(ref List<string> dialog)
    {
        dialog = RoutineDocumentation.CorrectGenerationDialog(dialog);
    }
}
