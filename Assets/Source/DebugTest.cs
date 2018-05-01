using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DebugTestBase : MonoEditorDebug
{
	[EditorDebugMethod]
	protected void Test_Base(string _string)
	{

	}
}

public class DebugTest : DebugTestBase
{
	enum ETest
	{
		none, 
		pizza, 
		burger
	}
		
	void TestEnum (ETest _test)
	{
		
	}

	[EditorDebugMethod]
	void TestPrivate1Param(int _value)
	{
		Debug.LogFormat ("TestPrivate1Param: {0}", _value);
	}

	[EditorDebugMethod]
	public void TestPublic1Int(int _value)
	{
		Debug.LogFormat ("TestPublic1Int: {0}", _value);
	}

	[EditorDebugMethod]
	public void TestPublic1String(string _value)
	{
		Debug.LogFormat ("TestPublic1Param: {0}", _value);
	}

	[EditorDebugMethod]
	public void TestPublic1Vec(Vector2 _vec2, Vector3 _vec3)
	{

	}

	[EditorDebugMethod]
	public void TestPublicAllTheStuff(Color _col, Bounds _bounds, Rect _rect, long _long)
	{

	}

	[EditorDebugMethod]
	public void TestPublic1bool(bool _bool)
	{

	}

	[EditorDebugMethod]
	public void TestPublic1String1Int(string _string, int _int)
	{
		Debug.LogFormat ("TestPublic1Param: {0}|{1}", _string, _int);
	}
}