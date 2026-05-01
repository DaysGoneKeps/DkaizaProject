namespace DkaizaProject.Models
{
    public class MercadoPagoSettings
    {
        public string AccessToken { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public int PorcentajeSenal { get; set; } = 30;
        public string Currency { get; set; } = "PEN";
    }
}
