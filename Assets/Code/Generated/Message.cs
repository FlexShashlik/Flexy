// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 0.5.8
// 

using Colyseus.Schema;

public class Message : Schema {
	[Type(0, "uint32")]
	public uint stateNum = 0;

	[Type(1, "number")]
	public float x = 0;

	[Type(2, "number")]
	public float y = 0;

	[Type(3, "number")]
	public float z = 0;

	[Type(4, "string")]
	public string msg = "";
}

