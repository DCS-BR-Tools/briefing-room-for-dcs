const fs = require('fs');

const mission = fs.readFileSync('./emptyMission.lua', 'utf8')
const unit = fs.readFileSync('./unit.lua', 'utf8')
const groupLua = fs.readFileSync('./group.lua', 'utf8')

const map = process.argv[2];
const templateLocations = JSON.parse(fs.readFileSync(`../../DatabaseJSON/TheaterTemplateLocations/${map}.json`, 'utf8'))



let modmission = mission.replaceAll("$THEATER$", map)


let groupIndex = 1;
let unitIndex = 1;

modmission = modmission.replaceAll(`$MAPX$`, templateLocations[0].coords[0])
modmission = modmission.replaceAll(`$MAPY$`, templateLocations[0].coords[1])

let tempGroups = []
templateLocations.forEach(templateLocation => {
    let tempGroup = groupLua;
    tempGroup = tempGroup.replaceAll("$X$", templateLocation.coords[0])
    tempGroup = tempGroup.replaceAll("$Y$", templateLocation.coords[1])
    tempGroup = tempGroup.replaceAll("$LOCTYPE$", templateLocation.locationType)
    tempGroup = tempGroup.replaceAll("$GLOBIDX$", groupIndex)
    tempGroup = tempGroup.replaceAll("$IDX$", groupIndex)

    let tempunits = []
    let idx = 1;
    templateLocation.locations.forEach((loc, i) => {
        let tempUnit = unit;
        tempUnit = tempUnit.replaceAll("$X$", templateLocation.coords[0] +loc.coords[0])
        tempUnit = tempUnit.replaceAll("$Y$", templateLocation.coords[1] + loc.coords[1])
        tempUnit = tempUnit.replaceAll("$HEADING$", loc.heading)
        tempUnit = tempUnit.replaceAll("$GLOBIDX$", groupIndex)
        tempUnit = tempUnit.replaceAll("$TYPE$", loc.originalType)
        tempUnit = tempUnit.replaceAll("$IDX$", idx)
        tempunits.push(tempUnit)
        idx++
        unitIndex++
    });
    tempGroup = tempGroup.replace(`$UNITS$`, tempunits.join("\n"));
    tempGroups.push(tempGroup)
    groupIndex++
})
modmission = modmission.replaceAll(`$GROUPS$`, tempGroups.join("\n"))


fs.writeFileSync('./out/mission.lua', modmission)
console.log("Mission lua file complete: './out/mission.lua'")
