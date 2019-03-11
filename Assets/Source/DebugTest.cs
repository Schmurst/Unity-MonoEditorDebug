using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DebugTestBase : MonoEditorDebug
{
	protected void Test_Base(string _string)
	{
		Debug.LogFormat ("hello: {0}", _string);
	}
		
	[EditorDebugMethod(true)]
	protected void TestArray(string[] _stringArray)
	{
        string arr = string.Empty;
        for (int i = 0; i < _stringArray.Length; i++)
            arr += _stringArray[i];
        Debug.Log(arr);
	}

	[EditorDebugMethod(true)]
    protected void TestList(List<int> _intList)
	{

	}

	[EditorDebugMethod]
	protected void TestList(List<string> _stringList)
	{

	}

	protected void HelloWorld(Rect _rect)
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