using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DkaizaProject.Services.IA
{
    public interface IChatAiService
    {
        Task<string> GetReplyAsync(string userMessage);
    }
}