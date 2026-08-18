#nullable enable

using BepInEx;
using BepInEx.Logging;
using Lua;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LuaRoutineExtensions;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid =
        "renegadex.silverpine.luaroutineextensions";
    public const string PluginName = "Lua Routine Extensions";
    public const string PluginVersion = "1.0.0";

    private static readonly FieldInfo? LuaAccessibleFunctionsField =
        typeof(LuaEntity).GetField(
            "luaAccessibleFunctions",
            BindingFlags.NonPublic | BindingFlags.Static);

    private static readonly object WarningLock = new();
    private static readonly HashSet<string> EmittedWarnings =
        new(StringComparer.Ordinal);

    private static ManualLogSource Log = null!;

    private void Awake()
    {
        Log = Logger;

        if (LuaAccessibleFunctionsField?.GetValue(null)
            is not List<LuaAccessibleFunction> registry)
        {
            Logger.LogError(
                "Could not access LuaEntity.luaAccessibleFunctions. " +
                "No Lua routine functions were registered.");
            return;
        }

        int registered = 0;
        foreach (LuaAccessibleFunction function in CreateFunctions())
        {
            if (ContainsFunction(
                    registry,
                    function.tableName,
                    function.luaFunction.Name))
            {
                Logger.LogWarning(
                    $"Skipped existing Lua function " +
                    $"{function.tableName}.{function.luaFunction.Name}.");
                continue;
            }

            registry.Add(function);
            registered++;
        }

        Logger.LogInfo(
            $"Registered {registered} custom-character Lua routine " +
            "functions.");
    }

    private static IEnumerable<LuaAccessibleFunction> CreateFunctions()
    {
        string eventNames = string.Join(
            ", ", Enum.GetNames(typeof(CurrentEvent)));

        yield return new LuaAccessibleFunction(
            "Events",
            new LuaFunction("IsActive", IsEventActive),
            "IsActive(eventName)",
            "eventName is one of: " + eventNames + ".",
            "True when that base-game schedule event is active today. " +
            "Returns false for an invalid name or unavailable game state.");

        yield return new LuaAccessibleFunction(
            "Events",
            new LuaFunction("GetDaysUntil", GetDaysUntilEvent),
            "GetDaysUntil(eventName)",
            "eventName is one of: " + eventNames + ".",
            "The number of days until that base-game schedule event, " +
            "or -1 for an invalid name or unavailable game state.");

        yield return new LuaAccessibleFunction(
            "NPC",
            new LuaFunction("IsVendorActive", IsVendorActive),
            "IsVendorActive(npcName)",
            "npcName is a loaded base or custom NPC's name.",
            "True when the NPC has an active Vendor component.");

        yield return new LuaAccessibleFunction(
            "NPC",
            new LuaFunction(
                "IsBaseRoutineActivity",
                IsBaseRoutineActivity),
            "IsBaseRoutineActivity(npcName, activity)",
            "npcName is a loaded base or custom NPC's name. activity is " +
            "the exact internal activity text to compare.",
            "True when the NPC's underlying non-override routine has the " +
            "specified activity.");

        yield return new LuaAccessibleFunction(
            "NPC",
            new LuaFunction("HasGivenKeys", HasGivenKeys),
            "HasGivenKeys(npcName)",
            "npcName is a loaded base or custom NPC's name.",
            "True when that NPC has given their keys to the player.");

        yield return new LuaAccessibleFunction(
            "NPC",
            new LuaFunction("IsWithinDistance", IsWithinDistance),
            "IsWithinDistance(firstNpcName, secondNpcName, maximumDistance)",
            "The first two inputs are loaded base or custom NPC names. " +
            "maximumDistance is a non-negative number of world tiles.",
            "True when the NPCs' Euclidean tile distance is less than or " +
            "equal to maximumDistance.");
    }

    private static bool ContainsFunction(
        IEnumerable<LuaAccessibleFunction> registry,
        string tableName,
        string functionName)
    {
        foreach (LuaAccessibleFunction existing in registry)
        {
            if (string.Equals(
                    existing.tableName,
                    tableName,
                    StringComparison.Ordinal)
                && string.Equals(
                    existing.luaFunction.Name,
                    functionName,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static ValueTask<int> IsEventActive(
        LuaFunctionExecutionContext context,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        string eventName = context.GetArgument<string>(0);
        bool result = false;

        try
        {
            if (TryParseEvent(eventName, out CurrentEvent currentEvent)
                && CurrentEventsSystem.Instance != null)
            {
                result = CurrentEventsSystem.Instance.IsEventActive(
                    currentEvent);
            }
        }
        catch (Exception exception)
        {
            WarnOnce(
                $"event-active:{eventName}",
                $"Events.IsActive(\"{eventName}\") failed: " +
                exception.Message);
        }

        return new ValueTask<int>(context.Return(result));
    }

    private static ValueTask<int> GetDaysUntilEvent(
        LuaFunctionExecutionContext context,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        string eventName = context.GetArgument<string>(0);
        double result = -1d;

        try
        {
            if (TryParseEvent(eventName, out CurrentEvent currentEvent)
                && CurrentEventsSystem.Instance != null)
            {
                result = CurrentEventsSystem.Instance.GetDaysUntilEvent(
                    currentEvent);
            }
        }
        catch (Exception exception)
        {
            WarnOnce(
                $"event-days:{eventName}",
                $"Events.GetDaysUntil(\"{eventName}\") failed: " +
                exception.Message);
        }

        return new ValueTask<int>(context.Return(result));
    }

    private static ValueTask<int> IsVendorActive(
        LuaFunctionExecutionContext context,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        string npcName = context.GetArgument<string>(0);
        bool result = false;

        try
        {
            if (TryResolveNpc(npcName, out NeuralNPC npc))
                result = npc.IsVendorActive();
        }
        catch (Exception exception)
        {
            WarnNpcQueryFailure("vendor", npcName, exception);
        }

        return new ValueTask<int>(context.Return(result));
    }

    private static ValueTask<int> IsBaseRoutineActivity(
        LuaFunctionExecutionContext context,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        string npcName = context.GetArgument<string>(0);
        string activity = context.GetArgument<string>(1);
        bool result = false;

        try
        {
            if (TryResolveNpc(npcName, out NeuralNPC npc)
                && npc.TryGetComponent(out NPCRoutineExecutor executor))
            {
                result = executor.IsBaseRoutineActivitySpecified(activity);
            }
        }
        catch (Exception exception)
        {
            WarnNpcQueryFailure("base-routine", npcName, exception);
        }

        return new ValueTask<int>(context.Return(result));
    }

    private static ValueTask<int> HasGivenKeys(
        LuaFunctionExecutionContext context,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        string npcName = context.GetArgument<string>(0);
        bool result = false;

        try
        {
            if (TryResolveNpc(npcName, out NeuralNPC npc))
                result = npc.givenKeys;
        }
        catch (Exception exception)
        {
            WarnNpcQueryFailure("keys", npcName, exception);
        }

        return new ValueTask<int>(context.Return(result));
    }

    private static ValueTask<int> IsWithinDistance(
        LuaFunctionExecutionContext context,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        string firstNpcName = context.GetArgument<string>(0);
        string secondNpcName = context.GetArgument<string>(1);
        double maximumDistance = context.GetArgument<double>(2);
        bool result = false;

        try
        {
            if (double.IsNaN(maximumDistance)
                || double.IsInfinity(maximumDistance)
                || maximumDistance < 0d)
            {
                WarnOnce(
                    $"distance-value:{maximumDistance}",
                    "NPC.IsWithinDistance requires a finite, " +
                    "non-negative maximumDistance.");
            }
            else if (TryResolveNpc(firstNpcName, out NeuralNPC firstNpc)
                && TryResolveNpc(secondNpcName, out NeuralNPC secondNpc))
            {
                float distance = Vector2Int.Distance(
                    firstNpc.transform.GetVector2IntPosition(),
                    secondNpc.transform.GetVector2IntPosition());
                result = distance <= maximumDistance;
            }
        }
        catch (Exception exception)
        {
            WarnOnce(
                $"distance:{firstNpcName}:{secondNpcName}",
                $"NPC.IsWithinDistance(\"{firstNpcName}\", " +
                $"\"{secondNpcName}\", ...) failed: {exception.Message}");
        }

        return new ValueTask<int>(context.Return(result));
    }

    private static bool TryParseEvent(
        string eventName,
        out CurrentEvent currentEvent)
    {
        if (Enum.TryParse(
                eventName?.Trim(),
                ignoreCase: true,
                out currentEvent)
            && Enum.IsDefined(typeof(CurrentEvent), currentEvent))
        {
            return true;
        }

        WarnOnce(
            "invalid-event:" + eventName,
            $"Unknown Lua routine event name \"{eventName}\". Expected " +
            string.Join(", ", Enum.GetNames(typeof(CurrentEvent))) + ".");
        currentEvent = default;
        return false;
    }

    private static bool TryResolveNpc(
        string npcName,
        out NeuralNPC npc)
    {
        npc = null!;
        string trimmedName = npcName?.Trim() ?? "";
        if (trimmedName.Length == 0 || NeuralNPC.neuralNPCs == null)
        {
            WarnUnknownNpc(trimmedName);
            return false;
        }

        if (Enum.TryParse(
                trimmedName,
                ignoreCase: true,
                out NPCName parsedName)
            && NeuralNPC.neuralNPCs.TryGetValue(parsedName, out npc)
            && npc != null)
        {
            return true;
        }

        foreach (NeuralNPC candidate in NeuralNPC.neuralNPCs.Values)
        {
            if (candidate != null
                && string.Equals(
                    candidate.GetFinalName(),
                    trimmedName,
                    StringComparison.OrdinalIgnoreCase))
            {
                npc = candidate;
                return true;
            }
        }

        WarnUnknownNpc(trimmedName);
        npc = null!;
        return false;
    }

    private static void WarnUnknownNpc(string npcName)
    {
        WarnOnce(
            "unknown-npc:" + npcName,
            $"Lua routine query could not find a loaded NPC named " +
            $"\"{npcName}\".");
    }

    private static void WarnNpcQueryFailure(
        string query,
        string npcName,
        Exception exception)
    {
        WarnOnce(
            $"npc-query:{query}:{npcName}",
            $"Lua routine NPC query for \"{npcName}\" failed: " +
            exception.Message);
    }

    private static void WarnOnce(string key, string message)
    {
        lock (WarningLock)
        {
            if (!EmittedWarnings.Add(key))
                return;
        }

        if (Log != null)
            Log.LogWarning(message);
    }
}
