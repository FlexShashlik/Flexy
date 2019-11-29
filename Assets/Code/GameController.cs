using GameDevWare.Serialization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CnControls;

public class GameController : MonoBehaviour
{
    public float Velocity = 0.1f;

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
            msg.stateNum = GameState.CurrentCommand++;
            Debug.Log(GameState._Room.SessionId);
            Vector3 playerPos = GetPlayer(GameState._Room.SessionId).Cube.transform.position;

            msg.x = playerPos.x + GameState.HeroVelocity.x;
            msg.y = playerPos.y + GameState.HeroVelocity.y;
            msg.z = playerPos.z + GameState.HeroVelocity.z;

            SpeculativeMovement();

            GameState._Room.Send(msg);
        }
    }

    Player GetPlayer(string key)
    {
        Player player;
        GameState.Entities.TryGetValue(key, out player);

        return player;
    }

    void SpeculativeMovement()
    {
        GetPlayer(GameState._Room.SessionId).Cube.transform.Translate(GameState.HeroVelocity);
    }

    bool HandleMovement()
    {
        bool isMoved = false;

        GameState.HeroVelocity.Set(0, 0, 0);

        if (Input.GetKey(KeyCode.D) || CnInputManager.GetAxis("Horizontal") > 0)
        {
            GameState.HeroVelocity.x = Velocity;
            isMoved = true;
        }

        if (Input.GetKey(KeyCode.A) || CnInputManager.GetAxis("Horizontal") < 0)
        {
            GameState.HeroVelocity.x = -Velocity;
            isMoved = true;
        }

        if (Input.GetKey(KeyCode.W) || CnInputManager.GetAxis("Vertical") > 0)
        {
            GameState.HeroVelocity.z = Velocity;
            isMoved = true;
        }

        if (Input.GetKey(KeyCode.S) || CnInputManager.GetAxis("Vertical") < 0)
        {
            GameState.HeroVelocity.z = -Velocity;
            isMoved = true;
        }

        GameState.HeroVelocity = Vector3.ClampMagnitude(GameState.HeroVelocity, Velocity);

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
            Debug.Log("message.num => " + message.stateNum + ", message.str => " + message.msg);

            Vector3 playerPos = GetPlayer(GameState._Room.SessionId).Cube.transform.position;
            if(message.x == playerPos.x &&
               message.y == playerPos.y &&
               message.z == playerPos.z)
            {
                GameState.CurrentCommand = message.stateNum;
            }
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

        Debug.Log($"Added player with ssID: {key}");
        GameState.Entities.Add(key, player);
    }

    void OnEntityRemove(Entity entity, string key)
    {
        Player player = GetPlayer(key);
        Destroy(player.Cube);

        GameState.Entities.Remove(key);
    }

    void OnEntityMove(Entity entity, string key)
    {
        Player player = GetPlayer(key);

        if(key != GameState._Room.SessionId || !IsPreviousSpeculativeMovementValid(entity.stateNum))
        {
            Vector3 realPos = new Vector3(entity.x, entity.y, entity.z);
            player.Cube.transform.position = realPos;
        }
    }

    bool IsPreviousSpeculativeMovementValid(uint commandNum)
    {
        return GameState.CurrentCommand >= commandNum;
    }
}
