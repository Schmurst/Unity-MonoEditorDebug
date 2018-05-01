using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Serialization;
using System.Reflection;
using Type = System.Type;
#endif

[System.AttributeUsage(System.AttributeTargets.Method)]
public class EditorDebugMethod : System.Attribute{}
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

		public EditorDebugMethodData(MethodInfo _method, ParameterInfo[] _params)
		{
			method = _method;
			parameters = new object[_params.Length];
			parameterInfos = _params;
			for (int i = 0; i < parameterInfos.Length; i++)
			{
				var type = parameterInfos[i].ParameterType;
				if(type.IsValueType)
					parameters[i] = System.Activator.CreateInstance(type);
				else if(CanSerialiseGenericList(type))
					parameters[i] = System.Activator.CreateInstance(typeof(List<>).MakeGenericType(new Type[]{type}));
				else if (type.IsArray)
					parameters[i] = System.Array.CreateInstance(type.GetElementType(), 0);
				else					
					parameters[i] = null;
			}
		}
	}

	//----------------------------------------------------------------------
	static bool CanSerialiseGenericList(System.Type _type)
	{
		if (!_type.IsGenericType || _type.GetGenericTypeDefinition () != typeof(List<>))
			return false;
		var parameters = _type.GetGenericArguments ();
		if (parameters.Length > 1)
			return false;
		var param = !parameters [0].IsEnum ? parameters [0] : typeof(System.Enum);
		return UnitySerialiseFieldByType.ContainsKey (param);
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
				var parameters = methods [i].GetParameters ();
				if (parameters != null && CanSerialiseAllParameters (parameters))
					m_debugMethods.Add (new EditorDebugMethodData (methods [i], parameters));
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
					EditorGUILayout.BeginVertical (EditorStyles.helpBox);
					EditorGUILayout.BeginHorizontal ();
					var dm = m_debugMethods [i];
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
			else if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof(List<>))
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
		}

		//----------------------------------------------------------------------
		bool CanSerialiseAllParameters(ParameterInfo[] _params)
		{ 
			for (int i = 0; i < _params.Length; i++)
			{
				var type = _params [i].ParameterType;
				if (type.IsEnum)
					continue;
				if (type.IsArray)
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