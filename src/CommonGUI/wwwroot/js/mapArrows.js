
function GetArrows(arrLatlngs, color, arrowCount, mapObj) {

  if (typeof arrLatlngs === undefined || arrLatlngs == null ||
    (!arrLatlngs.length) || arrLatlngs.length < 2)
    return [];

  if (typeof arrowCount === 'undefined' || arrowCount == null)
    arrowCount = 1;

  if (typeof color === 'undefined' || color == null)
    color = '';
  else
    color = 'color:' + color;

  var result = [];
  for (var i = 1; i < arrLatlngs.length; i++) {
    var icon = L.divIcon({ className: 'arrow-icon', bgPos: [5, 5], html: '<div style="' + color + ';transform: rotate(' + GetAngle(arrLatlngs[i - 1], arrLatlngs[i], -1).toString() + 'deg)">▶</div>' });
    for (var c = 1; c <= arrowCount; c++) {
      result.push(L.marker(MyMidPoint(arrLatlngs[i], arrLatlngs[i - 1], (c / (arrowCount + 1)), mapObj), { icon: icon }));
    }
  }
  return result;
}

function GetAngle(latLng1, latlng2, coef) {
  var dy = latlng2[0] - latLng1[0];
  var dx = Math.cos(Math.PI / 180 * latLng1[0]) * (latlng2[1] - latLng1[1]);
  var ang = ((Math.atan2(dy, dx) / Math.PI) * 180 * coef);
  return (ang).toFixed(2);
}

function MyMidPoint(latlng1, latlng2, per, mapObj) {
  if (!mapObj)
    throw new Error('map is not defined');

  var halfDist, segDist, dist, p1, p2, ratio,
    points = [];

  p1 = mapObj.project(new L.latLng(latlng1));
  p2 = mapObj.project(new L.latLng(latlng2));
  halfDist = DistanceTo(p1, p2) * per;

  if (halfDist === 0)
    return mapObj.unproject(p1);

  dist = DistanceTo(p1, p2);

  if (dist > halfDist) {
    ratio = (dist - halfDist) / dist;
    var res = mapObj.unproject(new Point(p2.x - ratio * (p2.x - p1.x), p2.y - ratio * (p2.y - p1.y)));
    return [res.lat, res.lng];
  }

}

function DistanceTo(p1, p2) {
  var x = p2.x - p1.x,
    y = p2.y - p1.y;

  return Math.sqrt(x * x + y * y);
}

function ToPoint(x, y, round) {
  if (x instanceof Point) {
    return x;
  }
  if (isArray(x)) {
    return new Point(x[0], x[1]);
  }
  if (x === undefined || x === null) {
    return x;
  }
  if (typeof x === 'object' && 'x' in x && 'y' in x) {
    return new Point(x.x, x.y);
  }
  return new Point(x, y, round);
}

function Point(x, y, round) {
  this.x = (round ? Math.round(x) : x);
  this.y = (round ? Math.round(y) : y);
}