#version 330 core

layout(location = 0) in vec3 position; // Входные данные - позиции вершин
layout(location = 1) in vec3 normal;   // Входные данные - нормали

out vec3 fragNormal;      // Нормали, передаваемые фрагментному шейдеру
out vec3 fragPosition;    // Позиции, передаваемые фрагментному шейдеру

uniform mat4 model;       // Модельная матрица
uniform mat4 view;        // Видывая матрица
uniform mat4 projection;   // Проекционная матрица

void main()
{
    fragNormal = normal; // Передаем нормали
    fragPosition = vec3(model * vec4(position, 1.0)); // Рассчитываем позицию фрагмента
    gl_Position = projection * view * vec4(fragPosition, 1.0); // Устанавливаем финальную позицию вершины
}
