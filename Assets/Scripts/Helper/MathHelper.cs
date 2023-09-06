using Unity.Mathematics;

public static class MathHelper
{
    public static float Magnitude(float3 vector) { return (float)math.sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z); }

    public static float3 Normalize(float3 value)
    {
        float mag = Magnitude(value);
        if (mag > 0.00001F)
            return value / mag;
        else
            return float3.zero;
    }
}