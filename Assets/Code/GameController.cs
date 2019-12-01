using GameDevWare.Serialization;
using System.Collections.Generic;
using UnityEngine;
using CnControls;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public float dP = 0.1f;

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

        // Move all entities toward their server position
        foreach(KeyValuePair<string, object> entry in GameState.Entities)
        {
            object entity;
            GameState.Entities.TryGetValue(entry.Key, out entity);
            if(entry.Key != GameState._Room.SessionId && entity is Player)
            {
                Player player = (Player)entity;
                Vector3 serverPos = new Vector3(player._Entity.x, player._Entity.y, player._Entity.z);
                
                if(serverPos != player.Cube.transform.position)
                {
                    Vector3 interpolatedPos = Vector3.Lerp(player.Cube.transform.position, serverPos, 0.1f);
                    player.Cube.transform.position = interpolatedPos;
                }
            }
        }
    }

    Player GetPlayer(string key)
    {
        Player player = null;
        object entity;
        GameState.Entities.TryGetValue(key, out entity);

        if(entity is Player)
        {
            player = (Player)entity;
        }

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
            GameState.HeroVelocity.x = dP;
            isMoved = true;
        }

        if (Input.GetKey(KeyCode.A) || CnInputManager.GetAxis("Horizontal") < 0)
        {
            GameState.HeroVelocity.x = -dP;
            isMoved = true;
        }

        if (Input.GetKey(KeyCode.W) || CnInputManager.GetAxis("Vertical") > 0)
        {
            GameState.HeroVelocity.z = dP;
            isMoved = true;
        }

        if (Input.GetKey(KeyCode.S) || CnInputManager.GetAxis("Vertical") < 0)
        {
            GameState.HeroVelocity.z = -dP;
            isMoved = true;
        }

        GameState.HeroVelocity = Vector3.ClampMagnitude(GameState.HeroVelocity, dP);

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

        GameState._Room.OnLeave += (code) => 
        {
            SceneManager.LoadSceneAsync((int)GameState.Scenes.MainMenu);
            Debug.Log("ROOM: ON LEAVE");
        };

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
               message.z == playerPos.z &&
               message.stateNum > GameState.CurrentCommand)
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

        Debug.Log("Player add! x => " + entity.x + ", y => " + entity.y);

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
        Debug.Log($"Deleting {key}");
        Player player = GetPlayer(key);
        Destroy(player.Cube);

        GameState.Entities.Remove(key);
    }

    void OnEntityMove(Entity entity, string key)
    {
        Player player = GetPlayer(key);

        // If speculation for this client was valid then we do nothing :3
        if(key != GameState._Room.SessionId || !IsPreviousSpeculativeMovementValid(entity.stateNum))
        {
            player._Entity.stateNum = entity.stateNum;
            player._Entity.x = entity.x;
            player._Entity.y = entity.y;
            player._Entity.z = entity.z;
        }
    }

    bool IsPreviousSpeculativeMovementValid(uint commandNum)
    {
        return GameState.CurrentCommand >= commandNum;
    }
}
