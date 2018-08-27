using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Serialization;
using System.Reflection;
using Type = System.Type;
#endif

//----------------------------------------------------------------------
[System.AttributeUsage(System.AttributeTargets.Method)]
public class EditorDebugMethod : System.Attribute
{
    public bool AllowUseInEditMode;   

    public EditorDebugMethod(bool _showInEditMode = false)
    {
        AllowUseInEditMode = _showInEditMode;
    }
}

//----------------------------------------------------------------------
public abstract class MonoEditorDebug : MonoBehaviour
{
	#if UNITY_EDITOR
	delegate object SerialiseParameter(object param, ParameterInfo info);
	static readonly Dictionary<Type, SerialiseParameter> UnitySerialiseFieldByType = 
		new Dictionary<Type, SerialiseParameter> {
		{typeof(int), 			(p, i) => {return(object)EditorGUILayout.IntField (i.Name, (int)p);}},
		{typeof(bool), 			(p, i) => {return(object)EditorGUILayout.Toggle (i.Name, (bool)p);}},
		{typeof(float), 		(p, i) => {return(object)EditorGUILayout.FloatField (i.Name, (float)p);}},
		{typeof(string), 		(p, i) => {return(object)EditorGUILayout.TextField (i.Name, (string)p);}},
		{typeof(long), 			(p, i) => {return(object)EditorGUILayout.LongField (i.Name, (long)p);}},
		{typeof(System.Enum),	(p, i) => {return(object)EditorGUILayout.EnumPopup (i.Name, (System.Enum)p);}},
		{typeof(Vector2), 		(p, i) => {return(object)EditorGUILayout.Vector2Field (i.Name, (Vector2)p);}},
		{typeof(Vector2Int),	(p, i) => {return(object)EditorGUILayout.Vector2IntField (i.Name, (Vector2Int)p);}},
		{typeof(Vector3), 		(p, i) => {return(object)EditorGUILayout.Vector3Field (i.Name, (Vector3)p);}},
		{typeof(Vector3Int),	(p, i) => {return(object)EditorGUILayout.Vector3IntField (i.Name, (Vector3Int)p);}},
		{typeof(Vector4), 		(p, i) => {return(object)EditorGUILayout.Vector4Field (i.Name, (Vector4)p);}},
		{typeof(Rect), 			(p, i) => {return(object)EditorGUILayout.RectField (i.Name, (Rect)p);}},
		{typeof(RectInt), 		(p, i) => {return(object)EditorGUILayout.RectIntField (i.Name, (RectInt)p);}},
		{typeof(Color), 		(p, i) => {return(object)EditorGUILayout.ColorField (i.Name, (Color)p);}},
		{typeof(Color32), 		(p, i) => {return(object)EditorGUILayout.ColorField (i.Name, (Color)p);}},
		{typeof(Bounds), 		(p, i) => {return(object)EditorGUILayout.BoundsField (i.Name, (Bounds)p);}},
		{typeof(BoundsInt), 	(p, i) => {return(object)EditorGUILayout.BoundsIntField (i.Name, (BoundsInt)p);}},
	};
		
	class EditorDebugMethodData
	{
		public MethodInfo method;
		public object[] parameters;
		public ParameterInfo[] parameterInfos;
        public bool showInEditMode;

		public EditorDebugMethodData(MethodInfo _method, ParameterInfo[] _params, bool _showInEditMode)
		{
			method = _method;
			parameters = new object[_params.Length];
			parameterInfos = _params;
            showInEditMode = _showInEditMode;
			for (int i = 0; i < parameterInfos.Length; i++)
				parameters[i] = CreateDefaultObject(parameterInfos[i].ParameterType);
		}
	}

    //----------------------------------------------------------------------
    const bool SUPPORT_LISTS = false; 
    const bool SUPPORT_ARRAYS = false; 
	//----------------------------------------------------------------------
	static object CreateDefaultObject(System.Type type)
	{
		if (type.IsValueType)
			return (object) System.Activator.CreateInstance (type);
        if (CanSerialiseGenericList (type) && SUPPORT_LISTS)
			return (object) System.Activator.CreateInstance (typeof(List<>).MakeGenericType (new Type[]{ type }));
        if (CanSerialiseArray(type))
			return (object) System.Array.CreateInstance (type.GetElementType (), 0);
        return null;
	}

    //----------------------------------------------------------------------
    static bool CanSerialiseArray(System.Type _type)
    {
        return
            _type.IsArray &&
            SUPPORT_ARRAYS;
    }

	//----------------------------------------------------------------------
	static bool CanSerialiseGenericList(System.Type _type)
	{
        if (!SUPPORT_LISTS)
            return false;

		if (!_type.IsGenericType || _type.GetGenericTypeDefinition () != typeof(List<>))
			return false;
		var parameters = _type.GetGenericArguments ();
		if (parameters.Length > 1)
			return false;
		var param = !parameters [0].IsEnum ? parameters [0] : typeof(System.Enum);
		return UnitySerialiseFieldByType.ContainsKey (param);
	}

    //----------------------------------------------------------------------
    static T Find<T>(T[] _array, System.Predicate<T> _predicate)
    {
        for (int i = 0; i < _array.Length; i++)
            if (_predicate(_array[i]))
                return _array[i];
        return default(T);
    }

	#endif //unity editor

	//----------------------------------------------------------------------
	// Editor
	//----------------------------------------------------------------------
	#if UNITY_EDITOR
	[CustomEditor(typeof(MonoEditorDebug), true)]
	public class MonoBehaviourEditor : Editor
	{
		const BindingFlags METHOD_FLAGS = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
		bool m_isVisible = true;
		MonoEditorDebug m_this;
		List<EditorDebugMethodData> m_debugMethods;

		//----------------------------------------------------------------------
		void Initialise()
		{
			m_this = (MonoEditorDebug)target;
			m_debugMethods = new List<EditorDebugMethodData> ();
			var methods = m_this.GetType().GetMethods (METHOD_FLAGS);
			for (int i = 0; i < methods.Length; i++)
			{
				var attributes = methods [i].GetCustomAttributes (typeof(EditorDebugMethod), true);
				if (attributes.Length == 0)
					continue;
                var debugAttrib = Find(attributes, (x) => { return x is EditorDebugMethod; }) as EditorDebugMethod;
                if (debugAttrib == null)
                    continue;
				var parameters = methods [i].GetParameters ();
				if (parameters != null && CanSerialiseAllParameters (parameters))
                    m_debugMethods.Add (new EditorDebugMethodData (methods [i], parameters, debugAttrib.AllowUseInEditMode));
			}
		}

		//----------------------------------------------------------------------
		public override void OnInspectorGUI ()
		{
			if (m_this == null)
				Initialise ();
			
			if (m_debugMethods.Count == 0)
			{
				base.OnInspectorGUI ();
				return;
			}
				
			EditorGUILayout.BeginVertical (EditorStyles.helpBox);
			m_isVisible = GUILayout.Toggle (m_isVisible, "Debug Commands", EditorStyles.boldLabel);

			if (m_isVisible)
			{
				for (int i = 0; i < m_debugMethods.Count; i++)
				{
                    var dm = m_debugMethods [i];
                    if (!Application.isPlaying && !dm.showInEditMode)
                        continue;
                    EditorGUILayout.BeginVertical (EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal ();
                    GUILayout.Label (dm.method.Name);
                    if (GUILayout.Button ("Invoke", GUILayout.MaxWidth (55)))
                        dm.method.Invoke (m_this, dm.parameters);
                    EditorGUILayout.EndHorizontal ();
                    for (int k = 0; k < dm.parameterInfos.Length; k++)
                       DrawParameterFieldAndUpdateValue (ref dm.parameters [k], dm.parameterInfos [k]);
					EditorGUILayout.EndVertical ();
				}
			}

			EditorGUILayout.EndVertical ();
			base.OnInspectorGUI ();
		}

		//----------------------------------------------------------------------
		void DrawParameterFieldAndUpdateValue(ref object _param, ParameterInfo _info)
		{
			Type type = _info.ParameterType;
			if(type.IsEnum)
				type = typeof(System.Enum);
			
			if (type.IsArray)
				DrawArrayFieldAndUpdateValues (ref _param, _info);
			else if (CanSerialiseGenericList(type))
				DrawListFieldAndUpdateValues (ref _param, _info);
			else
				_param = UnitySerialiseFieldByType [type] (_param, _info);
		}

		//----------------------------------------------------------------------
		void DrawArrayFieldAndUpdateValues(ref object _param, ParameterInfo _info)
		{
			var array = _param as System.Array;
			int desiredLength = EditorGUILayout.IntField ("Length", array.Length);
		}

		//----------------------------------------------------------------------
		void DrawListFieldAndUpdateValues(ref object _param, ParameterInfo _info)
		{
			var list = _param as IList;
			int desiredLength = EditorGUILayout.IntField ("Length", list.Count);
			int diff = desiredLength - list.Count;
			var baseType = _info.ParameterType.GetGenericArguments () [0];

			//add/remove contents
			for (int i = 0; i < diff; i++)
			{
				object value = CreateDefaultObject (baseType);
				list.Add (value);	
			}
			for (int i = list.Count-1; i > list.Count+diff; i--)
				list.RemoveAt (i);
			
			for (int i = 0; i < list.Count; i++)
			{
                var item = list[i];
                if(list[i] != null)
                    Debug.LogFormat ("{0}:{1}", i, item.ToString ());
                list[i] = UnitySerialiseFieldByType [baseType] (item, _info);
			}
		}

		//----------------------------------------------------------------------
		bool CanSerialiseAllParameters(ParameterInfo[] _params)
		{ 
			for (int i = 0; i < _params.Length; i++)
			{
				var type = _params [i].ParameterType;
				if (type.IsEnum)
					continue;
                if (CanSerialiseArray(type))
					type = type.GetElementType ();
				if (CanSerialiseGenericList(type))
					type = type.GetGenericArguments ()[0];
				if (!UnitySerialiseFieldByType.ContainsKey (type))
					return false;
			}
			return true;
		}
	}

	#endif
}