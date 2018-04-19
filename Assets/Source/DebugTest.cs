using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugTest : MonoEditorDebug
{
	protected override System.Type Type { get { return typeof(DebugTest); } }

	[EditorDebugMethod]
	void TestPrivate0Param ()
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
	public void TestPublic0Param()
	{

	}

	[EditorDebugMethod]
	public void TestPublic1String1Int(string _string, int _int)
	{
		Debug.LogFormat ("TestPublic1Param: {0}|{1}", _string, _int);
	}
}