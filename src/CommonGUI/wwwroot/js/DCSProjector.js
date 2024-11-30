// <script src="https://cdnjs.cloudflare.com/ajax/libs/proj4js/2.8.1/proj4.js" integrity="sha512-aIqYhZWmKBmfxEcdqUuqln8wMYFvGUKw4sqfjNcCoEQgx2iZF26+ikx0kz35uHjwpejMeHCcfHUpCtkohi2raw==" crossorigin="anonymous" referrerpolicy="no-referrer"></script>

// Values taken from https://github.com/pydcs/dcs all credit goes to their team
const config = {
  Syria: {
    central_meridian: 39,
    false_easting: 282801.00000003993,
    false_northing: -3879865.9999999935,
    scale_factor: 0.9996,
  },
  SinaiMap: {
    central_meridian: 33,
    false_easting: 169222,
    false_northing: -3325313,
    scale_factor: 0.9996,
  },
  Caucasus: {
    central_meridian: 33,
    false_easting: -99516.9999999732,
    false_northing: -4998114.999999984,
    scale_factor: 0.9996,
  },
  Falklands: {
    central_meridian: -57,
    false_easting: 147639.99999997593,
    false_northing: 5815417.000000032,
    scale_factor: 0.9996,
  },
  MarianaIslands: {
    central_meridian: 147,
    false_easting: 238417.99999989968,
    false_northing: -1491840.000000048,
    scale_factor: 0.9996,
  },
  Nevada: {
    central_meridian: -117,
    false_easting: -193996.80999964548,
    false_northing: -4410028.063999966,
    scale_factor: 0.9996,
  },
  PersianGulf: {
    central_meridian: 57,
    false_easting: 75755.99999999645,
    false_northing: -2894933.0000000377,
    scale_factor: 0.9996,
  },
  Normandy: {
    central_meridian: -3,
    false_easting: -195526.00000000204,
    false_northing: -5484812.999999951,
    scale_factor: 0.9996,
  },
  TheChannel: {
    central_meridian: 3,
    false_easting: 99376.00000000288,
    false_northing: -5636889.00000001,
    scale_factor: 0.9996,
  },
  Kola: {
    central_meridian: 21,
    false_easting: -62702,
    false_northing: -7543625,
    scale_factor: 0.9996,
  },
  Afghanistan: {
    central_meridian: 63,
    false_easting: -300149.9999999864,
    false_northing: -3759657.000000049,
    scale_factor: 0.9996,
  },
};

function GetDCSMapProjector(mapName) {
  const mapConfig = config[mapName];
  return proj4(
    `+proj=tmerc +lat_0=0 +lon_0=${mapConfig.central_meridian} +k_0=${mapConfig.scale_factor} +x_0=${mapConfig.false_easting} +y_0=${mapConfig.false_northing} +towgs84=0,0,0,0,0,0,0 +units=m +vunits=m +ellps=WGS84 +no_defs +axis=neu`
  );
}

function DCStoLatLong(pos, projector) {
  return projector.inverse(pos.reverse());
}

function latLongToDCS(pos, projector) {
  return projector.forward(pos.reverse()).reverse();
}

function CheckConversion() {
  const projector = GetDCSMapProjector("Caucasus");
  const InputIrl = [41.64597937610111, 41.64779663085938]; //BATUMI
  const dcs = latLongToDCS(InputIrl, projector);
  const irl = DCStoLatLong(dcs, projector);
  const accuracy = 1e-14;
  console.log(InputIrl, dcs, irl);
  const diffX = Math.abs(InputIrl[0] - irl[0]);
  const diffY = Math.abs(InputIrl[1] - irl[1]);
  console.log(diffX, diffY);
  if (diffX < accuracy && diffY < accuracy) console.log("Check Pass");
}
