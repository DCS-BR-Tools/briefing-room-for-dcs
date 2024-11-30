using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.GUI.Utils
{
    internal class FlightGroupUtils
    {
        internal FlightGroupUtils() { }
        internal MissionTemplateFlightGroup Tab { get; set; }
        internal void SetFlightGroupTab(MissionTemplateFlightGroup var)
        {
            Tab = var;
        }

        internal void AddFlightGroup(IBaseTemplate Template)
        {
            MissionTemplateFlightGroup newflight = new();
            newflight.Alias = BriefingRoom.GetAlias(Template.PlayerFlightGroups.Count);
            Template.PlayerFlightGroups.Add(newflight);
            Tab = newflight;
        }

        internal void CloneFlightGroup(MissionTemplateFlightGroup flight, IBaseTemplate Template)
        {
            MissionTemplateFlightGroup newflight = new()
            {
                Aircraft = flight.Aircraft,
                AIWingmen = flight.AIWingmen,
                Carrier = flight.Carrier,
                Count = flight.Count,
                Country = flight.Country,
                Payload = flight.Payload,
                StartLocation = flight.StartLocation,
                Livery = flight.Livery
            };
            newflight.Alias = BriefingRoom.GetAlias(Template.PlayerFlightGroups.Count);
            Template.PlayerFlightGroups.Add(newflight);
            Tab = newflight;
        }

        internal void RemoveFlightGroup(MissionTemplateFlightGroup flight, IBaseTemplate Template)
        {
            Template.PlayerFlightGroups.Remove(flight);
            if (Template.PlayerFlightGroups.Count == 1)
            {
                Tab = Template.PlayerFlightGroups[0];
            }
        }

    }
}