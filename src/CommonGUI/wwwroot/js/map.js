const waypointColors = ["Cyan", "orange", "Chartreuse", "Magenta", "DeepPink", "Gold"]
let mapGroups = {}
let leafMap, leafHintMap, hintMarkerMap, hintTarget


let hintPositions = {}
let hintMarkers = {}

function ToggleLayer(id) {
    group = mapGroups[id]
    if (group._map) {
        group.remove()
        return
    }
    group.addTo(leafMap)
}


function GetFromMapCoordData(pos, map) {
    return DCStoLatLong(pos, GetDCSMapProjector(map)).reverse()
}

function GetFromMapCoordDataXY(pos, map) {
    const postArry = GetFromMapCoordData(pos, map)
    return { x: postArry[0], y: postArry[1] }
}


const GetBounds = (map) => {
    dataset = MapBoundaries[map]
    return [
        [dataset[0].lat, dataset[0].lon],
        [dataset[1].lat, dataset[1].lon],
        [dataset[2].lat, dataset[2].lon],
        [dataset[3].lat, dataset[3].lon],
    ]
}

function GetSquareBounds(map) {
    const bounds = GetBounds(map)
    const xArr = bounds.map(bound => bound[0])
    const yArr = bounds.map(bound => bound[1])
    return [[Math.min(...xArr), Math.min(...yArr)], [Math.max(...xArr), Math.max(...yArr)]]
}

function PullPosWithinBounds(pos, map) {
    const bounds = GetSquareBounds(map)
    return [Math.max(Math.min(pos[0], bounds[1][0]), bounds[0][0]), Math.max(Math.min(pos[1], bounds[1][1]), bounds[0][1])]
}

function GetCenterView(map, leafMap) {
    const bounds = GetSquareBounds(map)
    leafMap.setView([(bounds[0][0] + bounds[1][0]) / 2, (bounds[0][1] + bounds[1][1]) / 2], 6.5);
}

function DrawMapBounds(map, leafMap) {
    const bounds = GetBounds(map)
    bounds.push(bounds[0])
    L.geodesic(bounds, {
        color: 'Yellow',
        weight: 2,
        fillOpacity: 0.0
    }).addTo(leafMap);
}

async function SetHintPositions(positionsDict, map) {
    hintPositions = {}
    Object.keys(positionsDict).forEach(key => {
        if (positionsDict[key][0] === 0 && positionsDict[key][1] === 0) return
        hintPositions[key] = GetFromMapCoordDataXY(positionsDict[key], map)
    })
}


async function RenderHintMap(map, hintKey, mapData) {
    //Show hint map
    if (hintKey) {
        var bsOffcanvas = new bootstrap.Offcanvas(document.getElementById('offcanvasBottom'))
        bsOffcanvas.show()
    }
    if (map != hintMarkerMap) {
        hintMarkers = {}
    }
    if (leafHintMap) {
        leafHintMap.off();
        leafHintMap.remove();
    }

    try {
        leafHintMap = L.map('hintMap')
        L.esri.basemapLayer("Imagery").addTo(leafHintMap);
        L.esri.basemapLayer("ImageryLabels").addTo(leafHintMap);
    } catch (error) {
        console.warn(error)
    }
    Object.keys(hintPositions).forEach(key => {
        createHintMarker(key, map)
    })
    Object.keys(mapData).forEach(key => {
        data = mapData[key]
        if (data.length == 1) {
            AddIcon(key, data, leafHintMap, map)
        } else {
            AddZone(key, data, leafHintMap, map)
        }
    })
    leafHintMap.setView([0, 0], 6.5);
    GetCenterView(map, leafHintMap)
    DrawMapBounds(map, leafHintMap)
    if (hintKey) {
        await PrepClickHint(hintKey, map)
    }
}

function PrepClickHint(hintKey, map) {
    leafHintMap.once('click', function (e) {
        hintMarkerMap = map
        boundedPos = PullPosWithinBounds([e.latlng.lat, e.latlng.lng], map)
        hintPositions[hintKey] = { x: boundedPos[0], y: boundedPos[1] }
        if (hintKey in hintMarkers) {
            hintMarkers[hintKey].setLatLng([hintPositions[hintKey].x, hintPositions[hintKey].y])
        } else {
            createHintMarker(hintKey, map)
        }
    });
}



function createHintMarker(key, map) {
    var marker = new L.marker([hintPositions[key].x, hintPositions[key].y], {
        icon: new L.DivIcon({
            className: 'my-div-icon',
            html: `<img class="map_point_icon" src="_content/CommonGUI/img/nato-icons/${GetNatoIcon(key, false)}.svg" alt="${key}"/>` +
                `<span class="map_point_icon map_point_icon_text">${key.slice(key.indexOf('_') + 1).replaceAll("_", " ")}</span>`
        }),
        draggable: 'true'
    })
    hintMarkers[key] = marker
    marker.addTo(leafHintMap);
    hintMarkers[key].on('dragend', async function (event) {
        var marker = event.target;
        var position = marker.getLatLng();
        boundedPos = PullPosWithinBounds([position.lat, position.lng], map)
        hintPositions[key] = { x: boundedPos[0], y: boundedPos[1] }
        marker.setLatLng([hintPositions[key].x, hintPositions[key].y])
    });
    hintMarkers[key].on('dblclick', async function (event) {
        var marker = event.target;
        marker.remove()
        delete hintPositions[key]
        delete hintMarkers[key]
        PrepClickHint(key, map)
    });
}

function RemoveAllHintMarkers() {
    Object.values(hintMarkers).forEach(x => x.remove())
    hintPositions = {}
    hintMarkers = {}
}

function GetHintPoints(map) {
    const data = {}
    Object.keys(hintPositions).forEach(hintKey => {
        data[hintKey] = latLongToDCS([hintPositions[hintKey].x, hintPositions[hintKey].y], GetDCSMapProjector(map))
    });
    return data
}



async function RenderMap(mapData, map, inverted) {
    if (leafMap) {
        leafMap.remove();
        leafMap.off();
    }

    try {
        mapGroups = {
            "SAMs": new L.layerGroup(),
            "GroundForces": new L.layerGroup(),
        }
        leafMap = L.map('map')
        L.esri.basemapLayer("Imagery").addTo(leafMap);
        L.esri.basemapLayer("ImageryLabels").addTo(leafMap);
        addButtons()
    } catch (error) {
        console.warn(error)
    }

    Object.keys(mapData).forEach(key => {
        if (key.includes('ISLAND')) {
            return
        }
        data = mapData[key]
        if (data.length == 1) {
            AddIcon(key, data, leafMap, map, inverted)
        } else if (key.includes("ROUTE_")) {
            AddWaypoints(data, leafMap, map)
        } else if (key.includes("FRONTLINE")) {
            AddLine(key, data, leafMap, map)
        } else {
            AddZone(key, data, leafMap, map)
        }
    })
    if (Object.keys(mapData).includes("AIRBASE_HOME"))
        leafMap.setView(GetFromMapCoordData(mapData["AIRBASE_HOME"][0].reverse(), map), 6.4);
    else {
        await GetCenterView(map, leafMap)
    }
    DrawMapBounds(map, leafMap)
    new ResizeObserver(() => leafMap.invalidateSize()).observe(document.querySelector(".generator-preview"))
}

function addButtons() {
    L.easyButton('oi oi-audio', function (btn, map) {
        ToggleLayer("SAMs")
    }, 'SAMs').addTo(leafMap);
    L.easyButton('oi oi-dial', function (btn, map) {
        ToggleLayer("GroundForces")
    }, 'Ground Forces').addTo(leafMap);
}

function AddIcon(key, data, map, mapName, inverted) {
    if (key.startsWith("UNIT")) {
        AddUnit(key, data, map, mapName, inverted)
    } else if (key.includes("WAYPOINT_")) {
        AddWaypoint(key, data, map, mapName)
    } else {
        let postfix = ""
        if(key.includes("NAME_"))
        {
            postfix = ` - ${key.split("NAME_", 2)[1]}`
        }
        new L.Marker(GetFromMapCoordData(data[0], mapName), {
            title: GetTitle(key) + postfix,
            icon: new L.DivIcon({
                html: `<img class="map_point_icon" src="_content/CommonGUI/img/nato-icons/${GetNatoIcon(key, inverted)}.svg" alt="${key}"/>`
            }),
            zIndexOffset: key == "AIRBASE_HOME" || key.includes("OBJECTIVE_AREA") ? 200 : 100
        }).addTo(map)
    }
}

function AddWaypoint(key, data, map, mapName) {
    const title = key.replace("WAYPOINT_", "")
    const coords = GetFromMapCoordData(data[0], mapName)
    new L.circleMarker(coords, {
        title: title,
        radius: 10,
        weight: 5,
        color: `Gold`,
        fillOpacity: 0.0,
        zIndexOffset: 100
    }).addTo(map)
    new L.Marker(coords, {
        title: title,
        icon: new L.DivIcon({
            html: `<p class="map-label">${title}</p>`
        })
    }).addTo(map)
}

function AddUnit(key, data, map, mapName, inverted) {
    const coords = GetFromMapCoordData(data[0], mapName)
    group = mapGroups[GetGroup(key)]
    group.addLayer(new L.Marker(coords, {
        title: GetTitle(key),
        icon: new L.DivIcon({
            html: `<img class="map_point_icon map_unit"src="_content/CommonGUI/img/nato-icons/${GetNatoIcon(key, inverted)}.svg" alt="${key}"/>`
        })
    }))
    const range = GetRange(key);
    if (range > 0) {
        group.addLayer(new L.Circle(coords, {
            radius: range,
            color: GetColour(key, inverted),
            fillColor: GetColour(key, inverted),
            fillOpacity: 0.25,
            zIndexOffset: 99999999999
        }))
    }
}

function AddWaypoints(data, map, mapName) {
    let color = waypointColors[Math.floor(Math.random() * waypointColors.length)];
    let coords = data.map(x => GetFromMapCoordData(x, mapName))
    new L.polyline(coords, {
        color: color,
        weight: 2,
        opacity: 1,
        smoothFactor: 2
    }).addTo(map);
    L.featureGroup(GetArrows(coords, color, 1, map)).addTo(map);
}

function AddZone(key, data, map, mapName) {
    let coords = data.map(x => GetFromMapCoordData(x, mapName))
    L.polygon(coords, {
        color: GetColour(key),
        fillColor: GetColour(key),
        fillOpacity: 0.2,
    }).addTo(map);
}

function AddLine(key, data, map, mapName) {
    let coords = data.map(x => GetFromMapCoordData(x, mapName))
    group = mapGroups["GroundForces"]
    group.addLayer(L.polyline(coords, {
        color: GetColour(key),
        weight: 5,    
        dashArray: '8, 8', 
        dashOffset: '0'
    }))
}

function GetGroup(id) {
    switch (true) {
        case id.includes("SAM") || id.includes("EWR"):
            return "SAMs";
        default:
            return "GroundForces";
    }
}

function GetRange(id) {
    switch (true) {
        case id.includes("EWR"):
            return 60 * 1852;
        case id.includes("Long"):
            return 40 * 1852;
        case id.includes("Medium"):
            return 20 * 1852;
        default:
            return 0
    }
}

function GetTitle(id) {
    switch (true) {
        case id.includes("PLAYER"):
            return "Player Flight";
        case id.includes("AIRBASE"):
            return "Airbase"
        case id.includes("OBJECTIVE"):
            return "Objective"
        case id.includes("FOB"):
            return 'FOB'
        case id.includes("CARRIER"):
            return 'Carrier'
        case id.includes("SUPPLY"):
            return 'Supply Base'
        case id.includes("EWR"):
            return 'Early Warning Radar'
        case id.includes("Cargo"):
            return 'Cargo Storage'
        case id.includes("SAM"):
            switch (true) {
                case id.includes("Long"):
                    return "SAM Site Long Range"
                case id.includes("Medium"):
                    return "SAM Site Medium Range"
                case id.includes("Short"):
                    return "SAM Site Short Range"
            }
        case id.includes("Infantry"):
            return "Infantry"
        case id.includes("Vehicle"):
            switch (true) {
                case id.includes("APC"):
                    return "APC"
                case id.includes("AAA"):
                    return "AAA"
                case id.includes("Artillery"):
                    return "Artillery"
                case id.includes("MBT"):
                    return "MBT"
                case id.includes("Missile"):
                    return "Missile Launcher"
                case id.includes("Transport"):
                    return "Transport"
                default:
                    console.warn(`Vehicle Type Unknown - ${id}`)
                    return `Vehicle Type Unknown - ${id}`
            }
        case id.includes("Ship"):
            switch (true) {
                case id.includes("Cruiser"):
                    return "Cruiser"
                case id.includes("Frigate"):
                    return "Frigate"
                case id.includes("Submarine"):
                    return "Submarine"
                default:
                    return "Ship"
            }
        case id.includes("Static"):
            switch (true) {
                case id.includes("Military"):
                    return "Military Facility"
                case id.includes("Production"):
                    return "Production Facility"
                case id.includes("Offshore"):
                    return "Offshore Facility"
                default:
                    return "Facility"
            }
    }
}

function GetColour(id, inverted) {
    switch (true) {
        case id.includes("Enemy"):
            return inverted ? '#5555bb' : '#bb0000'
        case id.includes("Ally"):
            return inverted ? '#bb0000' : '#5555bb'
        case id.includes("RED"):
            return '#ff000055'
        case id.includes("BLUE"):
            return '#0000ff55'
        case id.includes('WATER'):
            return '#00000000'
        case id.includes('NOSPAWN'):
            return '#50eb5d55'
        case id.includes('ISLAND'):
            return '#d4eb5088'
        case id.includes('FRONTLINE'):
            return '#BF40BFDD'
        case id.includes("AIRBASE"):
            return '#ffffff'
        case id.includes("OBJECTIVE"):
            return '#eba134'
        case id.includes("FOB"):
            return '#b0b0b0'
        case id.includes("CARRIER"):
            return '#919191'
        default:
            return '#ffffff'
    }
}


function GetNatoIcon(id, invert = false) {
    let prefix = invert ? "RED_" : "BLUE_"
    switch (true) {
        case id.includes("Enemy"):
            prefix = invert ? "BLUE_" : "RED_"
            break
        case id.includes("Neutral"):
            prefix = "GREEN_"
            break
    }
    switch (true) {
        case id.includes("PLAYER"):
            return prefix + "PLAYER";
        case id.includes("AIRBASE"):
            return prefix + "AIRBASE"
        case id.includes("OBJECTIVE"):
            return "OBJECTIVE"
        case id.includes("FOB"):
            return prefix + 'FOB'
        case id.includes("CARRIER"):
            return prefix + 'CARRIER'
        case id.includes("SUPPLY"):
            return prefix + 'SUPPLY'
        case id.includes("EWR"):
            return prefix + 'EWR'
        case id.includes("Cargo"):
            return prefix + 'CARGO'
        case id.includes("SAM"):
            switch (true) {
                case id.includes("Long"):
                    return prefix + "SAM_LONG"
                case id.includes("Medium"):
                    return prefix + "SAM_MID"
                case id.includes("Short"):
                    return prefix + "SAM_SM"
            }
        case id.includes("Infantry"):
            return prefix + "VEHICLE_INFANTRY"
        case id.includes("Vehicle"):
            switch (true) {
                case id.includes("APC"):
                    return prefix + "VEHICLE_APC"
                case id.includes("AAA"):
                    return prefix + "VEHICLE_AAA"
                case id.includes("Artillery"):
                    return prefix + "VEHICLE_ARTILLERY"
                case id.includes("MBT"):
                    return prefix + "VEHICLE_MBT"
                case id.includes("Missile"):
                    return prefix + "VEHICLE_MISSILE"
                case id.includes("Transport"):
                    return prefix + "VEHICLE_TRANSPORT"
                default:
                    return prefix + "VEHICLE"
            }
        case id.includes("Ship"):
            switch (true) {
                case id.includes("Cruiser"):
                    return prefix + "SHIP_CRUISER"
                case id.includes("Frigate"):
                    return prefix + "SHIP_FRIGATE"
                case id.includes("Submarine"):
                    return prefix + "SHIP_SUBMARINE"
                default:
                    return prefix + "SHIP"
            }
        case id.includes("Static"):
            switch (true) {
                case id.includes("Military"):
                    return prefix + "STATIC_MILITARY"
                case id.includes("Production"):
                    return prefix + "STATIC_PRODUCTION"
                case id.includes("Offshore"):
                    return prefix + "STATIC_OFFSHORE"
                default:
                    return prefix + "STATIC"
            }
    }
}
