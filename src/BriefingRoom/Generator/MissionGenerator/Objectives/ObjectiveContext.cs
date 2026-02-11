using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Data.JSON;
using BriefingRoom4DCS.Generator.UnitMaker;
using BriefingRoom4DCS.Template;
using System.Collections.Generic;
using System.Linq;

namespace BriefingRoom4DCS.Generator.Mission.Objectives
{
    /// <summary>
    /// Context data passed throughout objective creation.
    /// Encapsulates all parameters and derived state needed during objective generation.
    /// </summary>
    internal class ObjectiveContext
    {
        #region Core References
        
        public IBriefingRoom BriefingRoom;
        public DCSMission Mission;
        
        #endregion

        #region Task Configuration
        
        public MissionTemplateSubTaskRecord Task;
        public DBEntryObjectiveTask TaskDB;
        public DBEntryObjectiveTarget TargetDB;
        public DBEntryObjectiveTargetBehavior TargetBehaviorDB;
        public ObjectiveOption[] ObjectiveOptions;
        public string[] FeaturesID;
        public int ObjectiveIndex;
        
        #endregion

        #region Coordinates
        
        public Coordinates ObjectiveCoordinates;
        public Coordinates UnitCoordinates;
        
        #endregion

        #region Unit Data (Derived)
        
        public string LuaUnit;
        public int UnitCount;
        public MinMaxI UnitCountMinMax;
        public List<UnitFamily> ObjectiveTargetUnitFamilies;
        public GroupFlags GroupFlags;
        
        public UnitFamily ObjectiveTargetUnitFamily => ObjectiveTargetUnitFamilies.First();
        
        #endregion

        #region Working State
        
        public Dictionary<string, object> ExtraSettings = [];
        public Dictionary<string, object> LuaExtraSettings = [];
        public List<string> Units = [];
        public List<DBEntryJSONUnit> UnitDBs = [];
        public string ObjectiveName;
        public List<Waypoint> ObjectiveWaypoints = [];
        
        #endregion

        #region Methods
        
        /// <summary>
        /// Initializes unit data from task and target configuration.
        /// Must be called after setting Task, TargetDB, TargetBehaviorDB, and ObjectiveOptions.
        /// </summary>
        public void InitializeUnitData()
        {
            var (luaUnit, unitCount, unitCountMinMax, objectiveTargetUnitFamilies, groupFlags) = 
                ObjectiveUtils.GetUnitData(Task, TargetDB, TargetBehaviorDB, ObjectiveOptions);
            LuaUnit = luaUnit;
            UnitCount = unitCount;
            UnitCountMinMax = unitCountMinMax;
            ObjectiveTargetUnitFamilies = objectiveTargetUnitFamilies;
            GroupFlags = groupFlags;
            UnitCoordinates = ObjectiveCoordinates;
        }
        
        #endregion
    }
}
