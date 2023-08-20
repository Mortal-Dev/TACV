using System.Collections.Generic;
using Unity.Mathematics;
using System.Linq;

public static class AirDensity
{
    //density in meters - kilograms / meter cubed
    private static Dictionary<float, float> altitudeDensity = new Dictionary<float, float>()
    {
        { -1000, 1.347f },
        { 0, 1.225f },
        { 1000, 1.112f },
        { 2000, 1.007f },
        { 3000, 0.9093f },
        { 4000, 0.8194f },
        { 5000, 0.7364f },
        { 6000, 0.6601f },
        { 7000, 0.5900f },
        { 8000, 0.5258f },
        { 9000, 0.4671f },
        { 10000, 0.4135f },
        { 15000, 0.1948f },
        { 20000, 0.08891f },
        { 25000, 0.04008f },
        { 30000, 0.01841f },
        { 40000, 0.003996f },
        { 50000, 0.001027f },
        { 60000, 0.0003097f },
        { 70000, 0.00008283f },
        { 80000, 0.00001846f }
    };

    public static float GetAirDensityFromFeet(float altitudeFeet)
    {
        return GetAirDensityFromMeters(altitudeFeet / 3.2808399f);
    }

    public static float GetAirDensityFromMeters(float altitudeMeters)
    {
        KeyValuePair<float, float> upperAirAltitudeDensity = new(float.NegativeInfinity, float.NegativeInfinity);
        KeyValuePair<float, float> lowerAirAltitudeDensity = new(float.NegativeInfinity, float.NegativeInfinity);

        foreach (KeyValuePair<float, float> airAltitudeDensity in altitudeDensity.Reverse())
        {
            if (altitudeMeters >= airAltitudeDensity.Key)
            {
                lowerAirAltitudeDensity = airAltitudeDensity;
                break;
            }

            upperAirAltitudeDensity = airAltitudeDensity;
        }

        return math.lerp(upperAirAltitudeDensity.Value, lowerAirAltitudeDensity.Value, (upperAirAltitudeDensity.Key - altitudeMeters) / (upperAirAltitudeDensity.Key - lowerAirAltitudeDensity.Key));
    }
}