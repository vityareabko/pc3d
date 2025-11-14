using System.Collections.Generic;
using UnityEngine;

namespace H
{
    public static class Utils
    {
        public static bool UnorderedEqual<T>(IEnumerable<T> a, IEnumerable<T> b, IEqualityComparer<T> comparer = null)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;

            comparer ??= EqualityComparer<T>.Default;

            // Подсчёт количества каждого элемента в словаре
            var counts = new Dictionary<T, int>(comparer);

            foreach (var x in a)
            {
                counts.TryGetValue(x, out int c);
                counts[x] = c + 1;
            }

            foreach (var y in b)
            {
                if (!counts.TryGetValue(y, out int c)) return false; // элемента не было в 'a'
                if (c == 1) counts.Remove(y);
                else counts[y] = c - 1;
            }

            return counts.Count == 0;
        }
        
        // Нормализуем угол в 0..360 (так проще смотреть на значения)
        public static float NormalizeAngle(float a)
        {
            a %= 360f;
            if (a < 0f) a += 360f;
            return a;
        }

        // Возвращает true, если на этом кадре прошли через target (с учётом обёртки 360)
        public static bool CrossedTarget(float prev, float curr, float target, float eps)
        {
            // расстояния от угла до цели (–180..180)
            float dPrev = Mathf.DeltaAngle(prev, target);
            float dCurr = Mathf.DeltaAngle(curr, target);

            // попали в окрестность цели
            if (Mathf.Abs(dCurr) <= eps) return true;

            // пересекли цель, т.е. расстояние сменило знак
            // (это устойчиво к любым прыжкам кадра и wrap-around)
            return (dPrev > 0f && dCurr < 0f) || (dPrev < 0f && dCurr > 0f);
        }
    }

}