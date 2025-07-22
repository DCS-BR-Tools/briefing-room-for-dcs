function __genOrderedIndex(t)
    local orderedIndex = {}
    for key in pairs(t) do
        table.insert(orderedIndex, key)
    end
    table.sort(orderedIndex)
    return orderedIndex
end

function orderedNext(t, state)
    local key = nil
    if state == nil then
        t.__orderedIndex = __genOrderedIndex(t)
        key = t.__orderedIndex[1]
    else
        for i = 1, table.getn(t.__orderedIndex) do
            if t.__orderedIndex[i] == state then
                key = t.__orderedIndex[i + 1]
            end
        end
    end

    if key then
        return key, t[key]
    end

    t.__orderedIndex = nil
    return
end

function orderedPairs(t)
    return orderedNext, t, nil
end

function mysplit(inputstr, sep)
    if sep == nil then
        sep = '%s'
    end
    local t = {}
    for str in string.gmatch(inputstr, '([^' .. sep .. ']+)')
    do
        table.insert(t, str)
    end
    return t
end

unitTypeToFamilies = {
    ["KAMAZ Truck"] = { "VehicleSupply" },
    ["GAZ-66"] = { "VehicleSupply" },
    ["Tor 9A331"] = { "VehicleSAMShort", "VehicleSAMShortIR", "VehicleAAA", "VehicleAAAStatic", "InfantryMANPADS" },
    ["Roland ADS"] = { "VehicleSAMShort", "VehicleSAMShortIR", "VehicleAAA", "VehicleAAAStatic", "InfantryMANPADS" },
    ["2S6 Tunguska"] = { "VehicleSAMShort", "VehicleSAMShortIR", "VehicleAAA", "VehicleAAAStatic", "InfantryMANPADS" },
    ["Vulcan"] = { "VehicleSAMShort", "VehicleSAMShortIR", "VehicleAAA", "VehicleAAAStatic", "InfantryMANPADS" },
    ["M1097 Avenger"] = { "VehicleSAMShort", "VehicleSAMShortIR", "VehicleAAA", "VehicleAAAStatic", "InfantryMANPADS" },
    ["S-300PS 5P85D ln"] = { "VehicleSAMLauncher" },
    ["S-300PS 5P85C ln"] = { "VehicleSAMLauncher" },
    ["S-300PS 40B6MD sr"] = { "VehicleSAMsr" },
    ["S-300PS 40B6M tr"] = { "VehicleSAMtr" },
    ["S-300PS 64H6E sr"] = { "VehicleSAMsr", "VehicleEWR", "VehicleSAMtr" },
    ["Patriot str"] = { "VehicleSAMsr", "VehicleSAMtr" },
    ["Hawk pcp"] = { "VehicleSAMCmd" },
    ["Hawk cwar"] = {"VehicleSAMcmd"},
    ["S-300PS 54K6 cp"] = { "VehicleSAMCmd" },
    ["Patriot cp"] = { "VehicleSAMCmd" },
    ["Patriot AMG"] = { "VehicleSAMCmd" },
    ["Patriot ECS"] = { "VehicleSAMCmd" },
    ["Patriot EPP"] = { "VehicleSAMCmd" },
    
    ["Roland Radar"] = { "VehicleEWR" },
    ["1L13 EWR"] = { "VehicleEWR" },
    ["Infantry AK"] = { "Infantry" },
    ["BTR-80"] = { "VehicleAPC" },
    ["Leopard1A3"] = { "VehicleMBT" },
    ["T-55"] = { "VehicleMBT" },
    ["Scud_B"] = { "VehicleMissile" },
    ["L118_Unit"] = { "VehicleArtillery" },
    ["MTLB"] = { "VehicleTransport" },
    -- temp
    ["Hummer"] = { "VehicleTransport" },
    ["M978 HEMTT Tanker"] = { "VehicleTransport" },
    ["Ural-375 PBU"] = { "VehicleTransport" },
    ["ZiL-131 APA-80"] = { "VehicleTransport" },
    ["ATMZ-5"] = { "VehicleTransport" },
    ["ATZ-10"] = { "VehicleTransport" },
    ["Ural-4320-31"] = { "VehicleSupply" },
    ["ZIL-131 KUNG"] = { "VehicleSupply" },
    ["Ural-4320T"] = { "VehicleSupply" },
    ["M 818"] = { "VehicleSupply" },
    ["Ural-375"] = { "VehicleSupply" },
    ["Ural-4320 APA-5D"] = { "VehicleTransport" },
    ["Land_Rover_101_FC"] = { "VehicleTransport" },
    ["Land_Rover_109_S3"] = { "VehicleTransport" },
    ["snr s-125 tr"] = { "VehicleSAMtr" },
    ["SNR_75V"] = { "VehicleSAMtr" },
    ["rapier_fsa_optical_tracker_unit"] = { "VehicleSAMtr" },
    ["Hawk tr"] = { "VehicleSAMtr" },
    ["S_75M_Volhov"] = { "VehicleSAMLauncher" },
    ["5p73 s-125 ln"] = { "VehicleSAMLauncher" },
    ["S-200_Launcher"] = { "VehicleSAMLauncher" },
    ["Kub 2P25 ln"] = { "VehicleSAMLauncher" },
    ["rapier_fsa_launcher"] = { "VehicleSAMLauncher" },
    ["Hawk ln"] = { "VehicleSAMLauncher" },
    ["Patriot ln"] = { "VehicleSAMLauncher" },

    ["p-19 s-125 sr"] = { "VehicleSAMsr" },
    ["RPC_5N62V"] = { "VehicleSAMsr" },
    ["RLS_19J6"] = { "VehicleSAMsr" },
    ["Kub 1S91 str"] = { "VehicleSAMsr" },
    ["rapier_fsa_blindfire_radar"] = { "VehicleSAMsr" },
    ["Hawk sr"] = { "VehicleSAMsr" },


    default = { "UNKNOWN" }
}

function switchFamilies(x, cases)
    return cases[x] or cases.default
end