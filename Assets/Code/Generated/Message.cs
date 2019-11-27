// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 0.5.8
// 

using Colyseus.Schema;

public class Message : Schema {
	[Type(0, "number")]
	public float num = 0;

	[Type(1, "number")]
	public float dX = 0;

	[Type(2, "number")]
	public float dY = 0;

	[Type(3, "number")]
	public float dZ = 0;

	[Type(4, "string")]
	public string msg = "";
}

