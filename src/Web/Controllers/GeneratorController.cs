using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BriefingRoom4DCS;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System.Threading.Tasks;

namespace BriefingRoom4DCS.GUI.Web.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GeneratorController : ControllerBase
    {
        private readonly ILogger<GeneratorController> _logger;

        public GeneratorController(ILogger<GeneratorController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<FileContentResult> Post(MissionTemplate template)
        {
            var briefingRoom = new BriefingRoom();
            var mission =  briefingRoom.GenerateMission(template);
            var mizBytes = await mission.SaveToMizBytes();

            if (mizBytes == null) return null; // Something went wrong during the .miz export
            return File(mizBytes, "application/octet-stream", $"{mission.Briefing.Name}.miz");
        }
    }
}
