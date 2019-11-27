using GameDevWare.Serialization;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    void Start()
    {
        JoinOrCreateRoom();
    }

    void Update()
    {
        Message msg = new Message();
        bool isMovementHandle = HandleMovement();
                
        if (isMovementHandle)
        {
            msg.dX = GameState.HeroVelocity.x;
            msg.dY = GameState.HeroVelocity.y;
            msg.dZ = GameState.HeroVelocity.z;

            GameState._Room.Send(msg);
        }
    }

    bool HandleMovement()
    {
        bool isMoved = false;
        float velocity = 0.1f;

        GameState.HeroVelocity.Set(0, 0, 0);

        if (Input.GetKey(KeyCode.D))
        {
            GameState.HeroVelocity.x = velocity;
            isMoved = true;
        }

        if (Input.GetKey(KeyCode.A))
        {
            GameState.HeroVelocity.x = -velocity;
            isMoved = true;
        }

        if (Input.GetKey(KeyCode.W))
        {
            GameState.HeroVelocity.z = velocity;
            isMoved = true;
        }

        if (Input.GetKey(KeyCode.S))
        {
            GameState.HeroVelocity.z = -velocity;
            isMoved = true;
        }

        GameState.HeroVelocity = Vector3.ClampMagnitude(GameState.HeroVelocity, velocity);

        return isMoved;
    }

    public async void JoinOrCreateRoom()
    {
        GameState._Room = await GameState._Client.JoinOrCreate<State>(GameState.CurrentRoomName, new Dictionary<string, object>() { });

        Debug.Log($"sessionId: {GameState._Room.SessionId}");

        GameState._Room.State.entities.OnAdd += OnEntityAdd;
        GameState._Room.State.entities.OnRemove += OnEntityRemove;
        GameState._Room.State.entities.OnChange += OnEntityMove;

        PlayerPrefs.SetString("roomId", GameState._Room.Id);
        PlayerPrefs.SetString("sessionId", GameState._Room.SessionId);
        PlayerPrefs.Save();

        GameState._Room.OnLeave += (code) => Debug.Log("ROOM: ON LEAVE");
        GameState._Room.OnError += (message) => Debug.LogError(message);
        //room.OnStateChange += OnStateChangeHandler;
        GameState._Room.OnMessage += OnMessage;
    }
    
    void OnMessage(object msg)
    {
        if (msg is Message)
        {
            var message = (Message)msg;
            Debug.Log("Received schema-encoded message:");
            Debug.Log("message.num => " + message.num + ", message.str => " + message.msg);
        }
        else
        {
            // msgpack-encoded message
            var message = (IndexedDictionary<string, object>)msg;
            Debug.Log("Received msgpack-encoded message:");
            Debug.Log(message["hello"]);
        }
    }

    void OnEntityAdd(Entity entity, string key)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        //Debug.Log("Player add! x => " + entity.x + ", y => " + entity.y);

        cube.transform.position = new Vector3(entity.x, entity.y, entity.z);

        // add "player" to map of players
        Player player = new Player();
        player._Entity = entity;
        player.Cube = cube;

        GameState.Entities.Add(key, player);
    }

    void OnEntityRemove(Entity entity, string key)
    {
        Player player;
        GameState.Entities.TryGetValue(key, out player);
        Destroy(player.Cube);

        GameState.Entities.Remove(key);
    }

    void OnEntityMove(Entity entity, string key)
    {
        Player player;
        GameState.Entities.TryGetValue(key, out player);

        float dX = entity.x - player.Cube.transform.position.x;
        float dY = entity.y - player.Cube.transform.position.y;
        float dZ = entity.z - player.Cube.transform.position.z;

        player.Cube.transform.Translate(new Vector3(dX, dY, dZ));
    }
}
