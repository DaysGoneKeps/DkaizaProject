using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DkaizaProject.Services.IA;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DkaizaProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatApiController : ControllerBase
    {
        private readonly IChatAiService _chatService;

        public ChatApiController(IChatAiService chatService)
        {
            _chatService = chatService;
        }

        public class ChatRequest
        {
            public string Message { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Mensaje vacío");

            var reply = await _chatService.GetReplyAsync(request.Message);
            return Ok(new { reply });
        }
    }
}