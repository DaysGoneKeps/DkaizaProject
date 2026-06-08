using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DkaizaProject.Services.IA
{
    public class OllamaChatService : IChatAiService
    {
        private readonly HttpClient _http;

        private const string SystemPrompt = @"
Eres DKaizaBot, el asistente virtual del Salón de Belleza D'KAIZA ubicado en Lima, Perú.
Tu misión es atender a los clientes con amabilidad, responder dudas sobre servicios,
horarios, precios y ayudarles a entender cómo reservar una cita.

Responde SIEMPRE en español, de forma natural, breve y amable.

Información importante:
- Horarios: Lunes a Sábado de 10:00 AM a 10:00 PM. Domingos cerrado.
- Teléfono y WhatsApp: (+51) 944 245 892
- Dirección: Av. los Olivos, La Molina 15024, Lima, Perú
- Email: info@dkaiza.com
- Para reservar una cita, el cliente debe iniciar sesión y usar la sección 'Reservar' del menú.
- Aceptamos pagos en efectivo y con tarjeta (Visa, Mastercard, Amex, PayPal, Apple Pay).

Si el cliente pregunta por precios exactos o disponibilidad en tiempo real,
indícale que puede verlos directamente en la plataforma o llamar al WhatsApp.
";

        public OllamaChatService()
        {
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<string> GetReplyAsync(string userMessage)
        {
            var fullPrompt = $"{SystemPrompt}\nUsuario: {userMessage}\nAsistente:";

            var body = new
            {
                model = "llama3.2",   // cambia al modelo que tengas en tu servidor Ollama
                prompt = fullPrompt,
                stream = false
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage resp;
            try
            {
                // Cambia esta URL por la IP/dominio de tu servidor Ollama
                resp = await _http.PostAsync("http://127.0.0.1:11434/api/generate", content);
            }
            catch
            {
                return "No pude conectarme al asistente en este momento. Por favor intenta más tarde.";
            }

            if (!resp.IsSuccessStatusCode)
                return $"El asistente tuvo un problema (código {(int)resp.StatusCode}). Intenta nuevamente.";

            var respJson = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(respJson);
            var reply = doc.RootElement.GetProperty("response").GetString();

            return reply?.Trim() ?? "No pude generar una respuesta.";
        }
    }
}