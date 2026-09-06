function GetTargetRoutineID(hour)
    if hour >= 22 or hour < 6 then
        return "sleep"
    end

    if (hour >= 7 and hour < 8) or (hour >= 12 and hour < 13)
        or (hour >= 18 and hour < 19) then
        return "eating"
    end

    local day = World.GetCurrentDay()

    if hour >= 9 and hour < 11 and day % 3 == 0 then
        if NPC and NPC.IsVendorActive and NPC.IsVendorActive("Hayden") then
            return "shop_at_bakery"
        end
    end

    if hour >= 14 and hour < 16 and day % 5 == 0 then
        if NPC and NPC.IsVendorActive and NPC.IsVendorActive("Rosalyn") then
            return "shop_at_herbalist"
        end
    end

    if hour >= 8 and hour < 18 then
        return "working"
    end

    if hour >= 19 and hour < 22 and Events and Events.IsActive then
        if Events.IsActive("MirelAtTavern") or Events.IsActive("OrsonAtTavern") then
            return "drink_at_tavern"
        end
    end

    return "silverpine_leisure"
end
