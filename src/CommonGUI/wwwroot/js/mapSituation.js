const situationMapLayers = {
    "BLUE": [],
    "RED": [],
    "NEUTRAL": [],
}

let leafSituationMap, drawnItems,SPGroup;

async function RenderEditorMap(map, spawnPoints, airbaseData) {
    console.log("SpawnPoints", spawnPoints.length)
    if (leafSituationMap) {
        leafSituationMap.off();
        leafSituationMap.remove();
    }

    try {
        leafSituationMap = L.map('situationMap')
        L.esri.basemapLayer("Imagery").addTo(leafSituationMap);
        L.esri.basemapLayer("ImageryLabels").addTo(leafSituationMap);
        SPGroup = new L.layerGroup()
        L.easyButton('oi oi-dial', function (btn, map) {
            ToggleSPLayer()
        }, `Spawn Points (${spawnPoints.length})`).addTo(leafSituationMap);
    } catch (error) {
        console.warn(error)
    }

    Object.keys(airbaseData).forEach(key => {
        data = airbaseData[key]
        AddIcon(key, data, leafSituationMap, map)
    })

    GetCenterView(map, leafSituationMap)
    DrawMapBounds(map, leafSituationMap)

    drawnItems = new L.FeatureGroup();
    leafSituationMap.addLayer(drawnItems);

    var drawControl = new L.Control.Draw({
        draw: {
            polygon: {
                shapeOptions: {
                    color: 'purple'
                },
                allowIntersection: false,
                drawError: {
                    color: 'orange',
                    timeout: 1000
                },
                showArea: true,
                metric: false,
                repeatMode: false
            },
            polyline: false,
            rectangle: false,
            circle: false,
            circlemarker: false,
            marker: false
        },
        edit: {
            featureGroup: drawnItems
        }
    });
    leafSituationMap.addControl(drawControl);
    leafSituationMap.on('draw:created', function (e) {
        const type = e.layerType,
            layer = e.layer;
        const areaType = document.querySelector('input[name="sideRadio"]:checked').value
        switch (areaType) {
            case "RED":
                layer.options.color = "red"
                break;
            case "BLUE":
                layer.options.color = "blue"
                break;
            case "NEUTRAL":
                layer.options.color = "green"
                break;
            default:
                break;
        }
        situationMapLayers[areaType].push(layer)
        drawnItems.addLayer(layer);
    });
    leafSituationMap.on('draw:deleted', function (e) {
        e.layers.eachLayer(x => {
            situationMapLayers.RED = situationMapLayers.RED.filter(y => y !== x)
            situationMapLayers.BLUE = situationMapLayers.BLUE.filter(y => y !== x)
            situationMapLayers.NEUTRAL = situationMapLayers.NEUTRAL.filter(y => y !== x)
        })
    });

    spawnPoints.forEach(sp => {
        let iconType = "GREEN_AIRBASE"
        switch (sp.bRtype) {
            case "LandSmall":
                iconType = "RED_AIRBASE"
                break;
            case "LandLarge":
                iconType = "BLUE_AIRBASE"
                break;
            default:
                break;
        }
        SPGroup.addLayer(new L.Marker(GetFromMapCoordData(sp.coords, map), {
            title: sp.pt,
            icon: new L.DivIcon({
                html: `<img class="map_point_icon_small" src="_content/CommonGUI/img/nato-icons/${iconType}.svg" alt="${sp.bRtype}"/>`
            }),
        }))
    });


}

function ToggleSPLayer() {

    if (SPGroup._map) {
        SPGroup.remove()
        return
    }
    SPGroup.addTo(leafSituationMap)
}

function SetSituationZones(dataString, map)
{
    const projector = GetDCSMapProjector(map)
    const data = JSON.parse(dataString);
    situationMapLayers.RED = data.redZones.map(zone => SetSituationZone(zone, projector, "red"))
    situationMapLayers.BLUE = data.blueZones.map(zone => SetSituationZone(zone, projector, "blue"))
    situationMapLayers.NEUTRAL = data.noSpawnZones.map(zone => SetSituationZone(zone, projector, "green"))
}

function SetSituationZone(zone, projector, color)
{
    zone = zone.map(x => DCStoLatLong(x, projector).reverse())
    var layer = L.polygon(zone, {
        color: color,
        fillColor: color,
        fillOpacity: 0.2,
    })
    layer.addTo(drawnItems)
    return layer;
}


function CreateCoordsList(layer, map) {
    const projector = GetDCSMapProjector(map)
    const adjustedCoords = layer.map(shape => shape.editing.latlngs[0][0].map(x => {
        const pos2 = PullPosWithinBounds([x.lat, x.lng], map)
        return { lat: pos2[0], lng: pos2[1] }
    }))

    layer.forEach((x, i) => x.setLatLngs(adjustedCoords[i]))

    return adjustedCoords.map(shape => shape.map(x => latLongToDCS([x.lat, x.lng], projector)))
}

function GetSituationCoordinates(map) {

    return {
       redZones: CreateCoordsList(situationMapLayers.RED, map),
       blueZones: CreateCoordsList(situationMapLayers.BLUE, map),
       noSpawnZones: CreateCoordsList(situationMapLayers.NEUTRAL, map) 
    }
}

function ClearMap() {
    situationMapLayers.RED = []
    situationMapLayers.BLUE = []
    situationMapLayers.NEUTRAL = []
    drawnItems.remove()
    drawnItems = new L.FeatureGroup();
}