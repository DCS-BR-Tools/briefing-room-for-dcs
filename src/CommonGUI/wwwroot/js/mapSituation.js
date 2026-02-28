const situationMapLayers = {
  BLUE: [],
  RED: [],
  NEUTRAL: [],
  COMBAT: [],
  FRONTLINE: undefined,
};

let leafSituationMap, situationDrawnItems, SPGroupS, SPGroupM, SPGroupL;

async function RenderEditorMap(map, spawnPoints, airbaseData, landWaterZones) {
  console.log("SpawnPoints", spawnPoints.length);
  if (leafSituationMap) {
    leafSituationMap.off();
    leafSituationMap.remove();
  }

  try {
    leafSituationMap = L.map("situationMap");
    L.esri.basemapLayer("Imagery").addTo(leafSituationMap);
    L.esri.basemapLayer("ImageryLabels").addTo(leafSituationMap);

    situationDrawnItems = new L.FeatureGroup();
    leafSituationMap.addLayer(situationDrawnItems);

    AddDrawControls();
    leafSituationMap.on(
      "draw:created",
      onDrawCreated(situationDrawnItems, situationMapLayers),
    );
    leafSituationMap.on("draw:deleted", onDrawDeleted(situationMapLayers));

    AddSituationLegend(leafSituationMap);

    AddSpawnButtonsToMap(spawnPoints);

    Object.keys(airbaseData).forEach((key) => {
      data = airbaseData[key];
      AddIcon(key, data, leafSituationMap, map);
    });

    GetCenterView(map, leafSituationMap);
    DrawMapBounds(map, leafSituationMap);

    AddLandWaterZones(map, landWaterZones);
    AddSpawnPointToMap(map, spawnPoints);
  } catch (error) {
    console.warn(error);
  }
}

function ToggleLandBounds() {
  if (LangGroup._map) {
    LangGroup.remove();
    return;
  }
  LangGroup.addTo(leafSituationMap);
}

function ToggleSPLayer(size) {
  let SPGroup = SPGroupS;
  if (size === "S") {
    SPGroup = SPGroupS;
  } else if (size === "M") {
    SPGroup = SPGroupM;
  } else if (size === "L") {
    SPGroup = SPGroupL;
  }
  if (SPGroup._map) {
    SPGroup.remove();
    return;
  }
  SPGroup.addTo(leafSituationMap);
}

function SetSituationZones(dataString, map) {
  const projector = GetDCSMapProjector(map);
  const data = JSON.parse(dataString);
  if (data.combatZones) {
    situationMapLayers.COMBAT = data.combatZones.map((zone) =>
      SetZone(zone, projector, situationColors.COMBAT, situationDrawnItems),
    );
  }
  situationMapLayers.RED = data.redZones.map((zone) =>
    SetZone(zone, projector, situationColors.RED, situationDrawnItems),
  );
  situationMapLayers.BLUE = data.blueZones.map((zone) =>
    SetZone(zone, projector, situationColors.BLUE, situationDrawnItems),
  );
  situationMapLayers.NEUTRAL = data.noSpawnZones.map((zone) =>
    SetZone(zone, projector, situationColors.NEUTRAL, situationDrawnItems),
  );
  if (data.frontLine) {
    situationMapLayers.FRONTLINE = SetLine(
      data.frontLine,
      projector,
      situationColors.FRONTLINE,
    );
  }
}

function CreateZoneCoordsList(layer, map) {
  const projector = GetDCSMapProjector(map);
  const adjustedCoords = layer.map((shape) =>
    shape.editing.latlngs[0][0].map((x) => {
      const pos2 = PullPosWithinBounds([x.lat, x.lng], map);
      return { lat: pos2[0], lng: pos2[1] };
    }),
  );

  layer.forEach((x, i) => x.setLatLngs(adjustedCoords[i]));

  return adjustedCoords.map((shape) =>
    shape.map((x) => latLongToDCS([x.lat, x.lng], projector)),
  );
}

function CreateLineCoordsList(layer, map) {
  const projector = GetDCSMapProjector(map);
  if (!layer) {
    return undefined;
  }
  const adjustedCoords = layer.editing.latlngs[0].map((x) => {
    const pos2 = PullPosWithinBounds([x.lat, x.lng], map);
    return { lat: pos2[0], lng: pos2[1] };
  });

  layer.setLatLngs([adjustedCoords]);

  return adjustedCoords.map((x) => latLongToDCS([x.lat, x.lng], projector));
}

function GetSituationCoordinates(map) {
  return {
    redZones: CreateZoneCoordsList(situationMapLayers.RED, map),
    blueZones: CreateZoneCoordsList(situationMapLayers.BLUE, map),
    noSpawnZones: CreateZoneCoordsList(situationMapLayers.NEUTRAL, map),
    combatZones: CreateZoneCoordsList(situationMapLayers.COMBAT, map),
    frontLine: CreateLineCoordsList(situationMapLayers.FRONTLINE, map),
  };
}

function ClearSituationMap() {
  ClearMap(situationMapLayers, situationDrawnItems);
}

function AddDrawControls() {
  var drawControlBlue = new L.Control.Draw({
    draw: {
      ...drawBaseOptions,
      polygon: {
        ...polyBaseOptions,
        shapeOptions: {
          color: situationColors.BLUE,
        },
      },
      polyline: false,
    },
  });
  leafSituationMap.addControl(drawControlBlue);
  var drawControlRed = new L.Control.Draw({
    draw: {
      ...drawBaseOptions,
      polygon: {
        ...polyBaseOptions,
        shapeOptions: {
          color: situationColors.RED,
        },
      },
      polyline: false,
    },
  });
  leafSituationMap.addControl(drawControlRed);
  var drawControlGreen = new L.Control.Draw({
    draw: {
      ...drawBaseOptions,
      polygon: {
        ...polyBaseOptions,
        shapeOptions: {
          color: situationColors.NEUTRAL,
        },
      },
      polyline: false,
    },
  });
  leafSituationMap.addControl(drawControlGreen);
  AddDrawOverrideControls(leafSituationMap, situationDrawnItems);
}

function AddSpawnPointToMap(map, spawnPoints) {
  let totalL = 0;
  let totalM = 0;
  let totalS = 0;

  const addSP = (sp) => {
    let iconType = "GREEN_VEHICLE";
    let SPGroup = SPGroupM;
    let totalType = totalM;
    switch (sp.bRtype) {
      case "LandSmall":
        iconType = "RED_VEHICLE";
        SPGroup = SPGroupS;
        totalS++;
        totalType = totalS;
        break;
      case "LandLarge":
        iconType = "BLUE_VEHICLE";
        SPGroup = SPGroupL;
        totalL++;
        totalType = totalL;
        break;
      default:
        totalM++;
        break;
    }
    if (totalType > 5000) {
      return;
    }
    SPGroup.addLayer(
      new L.Marker(GetFromMapCoordData(sp.coords, map), {
        title: JSON.stringify(sp),
        icon: new L.DivIcon({
          html: `<img class="map_point_icon_small" src="_content/CommonGUI/img/nato-icons/${iconType}.svg" alt="${sp.bRtype}"/>`,
        }),
      }),
    );
  };

  shuffle(spawnPoints);
  spawnPoints.forEach(addSP);
}

function AddLandWaterZones(map, landWaterZones) {
  const waterZones = landWaterZones.item1;
  const waterExclusionZones = landWaterZones.item2;
  const projector = GetDCSMapProjector(map);

  waterZones.forEach((zone) => {
    zone = zone.map((x) => DCStoLatLong(x, projector).reverse());
    var layer = L.polygon(zone, {
      color: situationColors.WATER,
      fillColor: situationColors.WATER,
      fillOpacity: 0.2,
    });
    layer.addTo(LangGroup);
  });

  waterExclusionZones.forEach((zone) => {
    zone = zone.map((x) => DCStoLatLong(x, projector).reverse());
    var layer = L.polygon(zone, {
      color: situationColors.LAND,
      fillColor: situationColors.LAND,
      fillOpacity: 0.2,
    });
    layer.addTo(LangGroup);
  });
}

function AddSpawnButtonsToMap(spawnPoints) {
  SPGroupS = new L.layerGroup();
  SPGroupM = new L.layerGroup();
  SPGroupL = new L.layerGroup();
  LangGroup = new L.layerGroup();
  L.easyButton(
    "oi oi-grid-four-up",
    function (btn, map) {
      ToggleSPLayer("S");
    },
    `Spawn Points Small (${
      spawnPoints.filter((x) => x.bRtype == "LandSmall").length
    })`,
  ).addTo(leafSituationMap);

  L.easyButton(
    "oi oi-grid-three-up",
    function (btn, map) {
      ToggleSPLayer("M");
    },
    `Spawn Points Med (${
      spawnPoints.filter((x) => x.bRtype == "LandMedium").length
    })`,
  ).addTo(leafSituationMap);
  L.easyButton(
    "oi oi-grid-two-up",
    function (btn, map) {
      ToggleSPLayer("L");
    },
    `Spawn Points Large (${
      spawnPoints.filter((x) => x.bRtype == "LandLarge").length
    })`,
  ).addTo(leafSituationMap);
  L.easyButton(
    "oi oi-droplet",
    function (btn, map) {
      ToggleLandBounds();
    },
    "Land Water Zones",
  ).addTo(leafSituationMap);
}

function AddSituationLegend(map) {
  var legend = L.control({ position: "topright" });

  legend.onAdd = function (map) {
    const div = GetCommonLegendDiv();
    div.innerHTML += `<i style="background: ${situationColors.WATER}"></i><span>Water</span><br>`;
    div.innerHTML += `<i style="background: ${situationColors.LAND}"></i><span>Land</span><br>`;
    div.innerHTML += "<span>Spawn Points (sample)</span><br>";
    div.innerHTML +=
      '<i class="icon" style="background-image: url(_content/CommonGUI/img/nato-icons/RED_VEHICLE.svg);background-repeat: no-repeat; background-color: transparent;"></i><span>Small Spawn</span><br>';
    div.innerHTML +=
      '<i class="icon" style="background-image: url(_content/CommonGUI/img/nato-icons/GREEN_VEHICLE.svg);background-repeat: no-repeat; background-color: transparent;"></i><span>Medium Spawn</span><br>';
    div.innerHTML +=
      '<i class="icon" style="background-image: url(_content/CommonGUI/img/nato-icons/BLUE_VEHICLE.svg);background-repeat: no-repeat; background-color: transparent;"></i><span>Large Spawn</span><br>';
    return div;
  };

  legend.addTo(map);
}

