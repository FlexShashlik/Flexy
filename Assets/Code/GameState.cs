using Colyseus;
using GameDevWare.Serialization;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static Client _Client;
    public static Room<State> _Room;

    public static string CurrentRoomName = "demo";

    public static IndexedDictionary<string, Player> Entities = new IndexedDictionary<string, Player>();

    public static Vector3 HeroVelocity;
}
