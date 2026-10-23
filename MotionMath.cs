using System;
using System.Collections.Generic;

namespace _2DProjectileMotionSimulator
{
    public class MotionMath
    {
        private readonly double initialSpeed;
        private readonly double projectionAngle;
        private readonly double g;
        private readonly double m;
        private readonly double k;

        public MotionMath(
            double speed,
            double projAngle = Math.PI / 4,
            double gravitationalFieldStrength = 9.81,
            double mass = 1,
            double dragCoefficient = 0)
        {
            if (speed < 0) throw new ArgumentOutOfRangeException(nameof(speed));
            if (mass <= 0) throw new ArgumentOutOfRangeException(nameof(mass));
            if (gravitationalFieldStrength < 0) throw new ArgumentOutOfRangeException(nameof(gravitationalFieldStrength));
            if (dragCoefficient < 0) throw new ArgumentOutOfRangeException(nameof(dragCoefficient));

            initialSpeed = speed;
            projectionAngle = projAngle;
            g = gravitationalFieldStrength;
            m = mass;
            k = dragCoefficient;
        }

        // Returns points as [x, y]. Drag follows F_drag = k*v^2 and acts opposite to velocity.
        public List<double[]> GetCoordinates(double dt = 0.001)
        {
            if (dt <= 0) throw new ArgumentOutOfRangeException(nameof(dt));

            var coordinates = new List<double[]>();
            double x = 0, y = 0;
            double vx = initialSpeed * Math.Cos(projectionAngle);
            double vy = initialSpeed * Math.Sin(projectionAngle);
            coordinates.Add(new[] { x, y });

            const int maxSteps = 2000000;
            for (int step = 0; step < maxSteps; step++)
            {
                double oldX = x, oldY = y;
                double oldVx = vx, oldVy = vy;

                double v = Math.Sqrt(vx * vx + vy * vy);
                double ax = -(k / m) * v * vx;
                double ay = -g - (k / m) * v * vy;

                // Semi-implicit Euler integration.
                vx += ax * dt;
                vy += ay * dt;
                x += vx * dt;
                y += vy * dt;

                // Stop once the descending projectile reaches the ground.
                if (y < 0 && step > 0)
                {
                    double denominator = oldY - y;
                    double fraction = denominator == 0 ? 0 : oldY / denominator;
                    fraction = Math.Max(0, Math.Min(1, fraction));
                    double landingX = oldX + (x - oldX) * fraction;
                    coordinates.Add(new[] { landingX, 0.0 });
                    break;
                }

                coordinates.Add(new[] { x, y });

                if (double.IsNaN(x) || double.IsNaN(y) ||
                    double.IsInfinity(x) || double.IsInfinity(y))
                    break;
            }

            return coordinates;
        }
    }
}
