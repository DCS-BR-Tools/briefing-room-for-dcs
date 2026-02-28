const shuffle = (array) => {
  for (let i = array.length - 1; i > 0; i--) {
    let j = Math.floor(Math.random() * (i + 1));
    [array[i], array[j]] = [array[j], array[i]];
  }
};

const situationColors = {
  RED: "red",
  BLUE: "blue",
  NEUTRAL: "green",
  COMBAT: "orange",
  FRONTLINE: "#BF40BF",
  WATER: "cyan",
  LAND: "yellow",
};


const drawBaseOptions = {
  rectangle: false,
  circle: false,
  circlemarker: false,
  marker: false,
};
const polyBaseOptions = {
  allowIntersection: false,
  drawError: {
    color: "orange",
    timeout: 1000,
  },
  showArea: true,
  metric: false,
  repeatMode: false,
};