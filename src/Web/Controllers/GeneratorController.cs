using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BriefingRoom4DCS.Template;
using System.Threading.Tasks;

namespace BriefingRoom4DCS.GUI.Web.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GeneratorController : ControllerBase
    {
        private readonly ILogger<GeneratorController> _logger;
        private readonly IBriefingRoom _briefingRoom;

        public GeneratorController(ILogger<GeneratorController> logger, IBriefingRoom briefingRoom)
        {
            _logger = logger;
            _briefingRoom = briefingRoom;
        }

        [HttpPost]
        public async Task<FileContentResult> Post(MissionTemplate template)
        {
            var mission =  _briefingRoom.GenerateMission(template);
            var mizBytes = await mission.SaveToMizBytes(_briefingRoom.Database);

            if (mizBytes == null) return null; // Something went wrong during the .miz export
            return File(mizBytes, "application/octet-stream", $"{mission.Briefing.Name}.miz");
        }
    }
}
