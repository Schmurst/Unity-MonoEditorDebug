using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DebugTestBase : MonoEditorDebug
{
	[EditorDebugMethod]
	protected void Test_Base(string _string)
	{
		Debug.LogFormat ("hello: {0}", _string);
	}
		
	[EditorDebugMethod]
	protected void TestArray(string[] _stringArray)
	{
		
	}

	[EditorDebugMethod]
	protected void TestArray(List<int> _intList)
	{

	}

	[EditorDebugMethod]
	protected void TestList(List<string> _stringList)
	{

	}

	[EditorDebugMethod]
	protected void HelloWorld(Rect _rect)
	{
	}

    [EditorDebugMethod(true)]
    protected void AllowInEditMode(Rect _rect)
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
}