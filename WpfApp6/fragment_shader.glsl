#version 330 core

in vec3 fragNormal;  // Принимаем нормали из вершинного шейдера
in vec3 fragPosition; // Принимаем позиции из вершинного шейдера

out vec4 color;  // Цвет, который будет выведен 

void main()
{
    // Основной цвет фрагмента
    color = vec4(fragNormal * 0.5 + 0.5, 1.0); // Простой цвет на основе нормалей
}
