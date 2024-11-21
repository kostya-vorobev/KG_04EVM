using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp6
{
    public class Camera
    {
        public float PositionZ { get; private set; } = -5.0f; // Начальное положение камеры
        public float RotationX { get; private set; } = 0.0f; // Вращение по оси X
        public float RotationY { get; private set; } = 0.0f; // Вращение по оси Y

        public void Move(float x, float y, float z)
        {
            PositionZ += z; // Движение сцены вперед или назад
        }

        public void Rotate(float deltaX, float deltaY)
        {
            RotationY += deltaX; // Вращение по оси Y
            RotationX += deltaY; // Вращение по оси X
        }
    }
}
