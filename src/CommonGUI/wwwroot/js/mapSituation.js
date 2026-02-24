const situationMapLayers = {
  BLUE: [],
  RED: [],
  NEUTRAL: [],
};

let leafSituationMap, drawnItems, SPGroupS, SPGroupM, SPGroupL;

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

 drawnItems = new L.FeatureGroup();
  leafSituationMap.addLayer(drawnItems);

  var drawControl = new L.Control.Draw({
    draw: {
      polygon: {
        shapeOptions: {
          color: "blue",
        },
        allowIntersection: false,
        drawError: {
          color: "orange",
          timeout: 1000,
        },
        showArea: true,
        metric: false,
        repeatMode: false,
      },
      polyline: false,
      rectangle: false,
      circle: false,
      circlemarker: false,
      marker: false,
    },
  });
  leafSituationMap.addControl(drawControl);
  var drawControl = new L.Control.Draw({
    draw: {
      polygon: {
        shapeOptions: {
          color: "red",
        },
        allowIntersection: false,
        drawError: {
          color: "orange",
          timeout: 1000,
        },
        showArea: true,
        metric: false,
        repeatMode: false,
      },
      polyline: false,
      rectangle: false,
      circle: false,
      circlemarker: false,
      marker: false,
    },
  });
  leafSituationMap.addControl(drawControl);
  var drawControl = new L.Control.Draw({
    draw: {
      polygon: {
        shapeOptions: {
          color: "green",
        },
        allowIntersection: false,
        drawError: {
          color: "orange",
          timeout: 1000,
        },
        showArea: true,
        metric: false,
        repeatMode: false,
      },
      polyline: false,
      rectangle: false,
      circle: false,
      circlemarker: false,
      marker: false,
    },
    edit: {
      featureGroup: drawnItems,
    },
  });
  leafSituationMap.addControl(drawControl);
  leafSituationMap.on("draw:created", function (e) {
    const layer = e.layer;
    let areaType;
    switch (layer.options.color) {
      case "red":
        areaType = "RED";
        break;
      case "blue":
        areaType = "BLUE";
        break;
      case "green":
        areaType = "NEUTRAL";
        break;
      default:
        areaType = "NEUTRAL";
        break;
    }
    situationMapLayers[areaType].push(layer);
    drawnItems.addLayer(layer);
  });
  leafSituationMap.on("draw:deleted", function (e) {
    e.layers.eachLayer((x) => {
      situationMapLayers.RED = situationMapLayers.RED.filter((y) => y !== x);
      situationMapLayers.BLUE = situationMapLayers.BLUE.filter((y) => y !== x);
      situationMapLayers.NEUTRAL = situationMapLayers.NEUTRAL.filter(
        (y) => y !== x,
      );
    });
  });

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
  } catch (error) {
    console.warn(error);
  }

  Object.keys(airbaseData).forEach((key) => {
    data = airbaseData[key];
    AddIcon(key, data, leafSituationMap, map);
  });

  GetCenterView(map, leafSituationMap);
  DrawMapBounds(map, leafSituationMap);

 

  const waterZones = landWaterZones.item1;
  const waterExclusionZones = landWaterZones.item2;
  const projector = GetDCSMapProjector(map);

  waterZones.forEach((zone) => {
    zone = zone.map((x) => DCStoLatLong(x, projector).reverse());
    var layer = L.polygon(zone, {
      color: "blue",
      fillColor: "blue",
      fillOpacity: 0.2,
    });
    layer.addTo(LangGroup);
  });

  waterExclusionZones.forEach((zone) => {
    zone = zone.map((x) => DCStoLatLong(x, projector).reverse());
    var layer = L.polygon(zone, {
      color: "yellow",
      fillColor: "yellow",
      fillOpacity: 0.2,
    });
    layer.addTo(LangGroup);
  });

  let totalL = 0;
  let totalM = 0;
  let totalS = 0;

  const addSP = (sp) => {
    let iconType = "GREEN_AIRBASE";
    let SPGroup = SPGroupM;
    let totalType = totalM;
    switch (sp.bRtype) {
      case "LandSmall":
        iconType = "RED_AIRBASE";
        SPGroup = SPGroupS;
        totalS++;
        totalType = totalS;
        break;
      case "LandLarge":
        iconType = "BLUE_AIRBASE";
        SPGroup = SPGroupL;
        totalL++;
        totalType = totalL;
        break;
      default:
        totalM++;
        break;
    }
    if (totalType > 10000) {
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

  const shuffle = (array) => {
    for (let i = array.length - 1; i > 0; i--) {
      let j = Math.floor(Math.random() * (i + 1));
      [array[i], array[j]] = [array[j], array[i]];
    }
  };
  shuffle(spawnPoints);
  spawnPoints.forEach(addSP);
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
  situationMapLayers.RED = data.redZones.map((zone) =>
    SetSituationZone(zone, projector, "red"),
  );
  situationMapLayers.BLUE = data.blueZones.map((zone) =>
    SetSituationZone(zone, projector, "blue"),
  );
  situationMapLayers.NEUTRAL = data.noSpawnZones.map((zone) =>
    SetSituationZone(zone, projector, "green"),
  );
}

function SetSituationZone(zone, projector, color) {
  zone = zone.map((x) => DCStoLatLong(x, projector).reverse());
  var layer = L.polygon(zone, {
    color: color,
    fillColor: color,
    fillOpacity: 0.2,
  });
  layer.addTo(drawnItems);
  return layer;
}

function CreateCoordsList(layer, map) {
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

function GetSituationCoordinates(map) {
  return {
    redZones: CreateCoordsList(situationMapLayers.RED, map),
    blueZones: CreateCoordsList(situationMapLayers.BLUE, map),
    noSpawnZones: CreateCoordsList(situationMapLayers.NEUTRAL, map),
  };
}

function ClearMap() {
  situationMapLayers.RED = [];
  situationMapLayers.BLUE = [];
  situationMapLayers.NEUTRAL = [];
  drawnItems.remove();
  drawnItems = new L.FeatureGroup();
}
