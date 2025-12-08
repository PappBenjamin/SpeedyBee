using System;

namespace SpeedyBee.ViewModels
{
    public class ImuData
    {
        public string? timestamp { get; set; }
        public float accel_x { get; set; }
        public float accel_y { get; set; }
        public float accel_z { get; set; }
        public float gyro_x { get; set; }
        public float gyro_y { get; set; }
        public float gyro_z { get; set; }
        public float temperature { get; set; }
    }
}
