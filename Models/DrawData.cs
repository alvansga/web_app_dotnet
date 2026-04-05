using System.Text.Json.Serialization;

namespace WebAppSandbox.Models
{
    public class DrawData
    {
        [JsonPropertyName("lastX")]
        public double LastX { get; set; }

        [JsonPropertyName("lastY")]
        public double LastY { get; set; }

        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; } = "";

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("isEraser")]
        public bool IsEraser { get; set; }

        [JsonPropertyName("strokeId")]
        public string StrokeId { get; set; } = "";
    }
}