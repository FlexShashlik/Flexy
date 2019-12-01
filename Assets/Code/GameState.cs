﻿using Colyseus;
using GameDevWare.Serialization;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public enum Scenes {MainMenu, Game};
    public static Client _Client;
    public static Room<State> _Room;

    public static string CurrentRoomName = "demo";

    public static IndexedDictionary<string, object> Entities = new IndexedDictionary<string, object>();

    public static Vector3 HeroVelocity;
    public static uint CurrentCommand = 0;
}
