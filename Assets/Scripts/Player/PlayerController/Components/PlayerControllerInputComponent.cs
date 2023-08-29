using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;

public struct PlayerControllerInputComponent : IComponentData
{
    public float rightControllerTrigger;
    public float rightControllerGrip;

    public bool rightControllerButtonOne;
    public bool rightControllerButtonTwo;

    public float2 rightControllerThumbstick;
    public bool rightControllerThumbstickPress;

    public float leftControllerTrigger;
    public float leftControllerGrip;

    public bool leftControllerButtonOne;
    public bool leftControllerButtonTwo;

    public float2 leftControllerThumbstick;
    public bool leftControllerThumbstickPress;
}