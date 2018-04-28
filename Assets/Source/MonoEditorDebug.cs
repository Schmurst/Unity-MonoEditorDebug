using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Serialization;
using System.Reflection;
using Type = System.Type;
#endif

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

	class EditorDebugMethod
	{
		public MethodInfo method;
		public object[] parameters;
		public ParameterInfo[] parameterInfos;

		public EditorDebugMethod(MethodInfo _method, ParameterInfo[] _params)
		{
			method = _method;
			parameters = new object[_params.Length];
			parameterInfos = _params;
			for (int i = 0; i < parameterInfos.Length; i++)
			{
				var type = parameterInfos[i].ParameterType;
				if(type.IsValueType)
					parameters[i] = System.Activator.CreateInstance(type);
				else					
					parameters[i] = null;
			}
		}
	}
	#endif //unity editor

	//----------------------------------------------------------------------
	// Editor
	//----------------------------------------------------------------------
	#if UNITY_EDITOR
	[CustomEditor(typeof(MonoEditorDebug), true)]
	public class MonoBehaviourEditor : Editor
	{
		const BindingFlags METHOD_FLAGS = BindingFlags.DeclaredOnly | BindingFlags.Instance | 
										  BindingFlags.NonPublic | BindingFlags.Public;
		bool m_isVisible = true;
		MonoEditorDebug m_this;
		List<EditorDebugMethod> m_debugMethods;

		//----------------------------------------------------------------------
		void Initialise()
		{
			m_this = (MonoEditorDebug)target;
			m_debugMethods = new List<EditorDebugMethod> ();
			var methods = m_this.GetType().GetMethods (METHOD_FLAGS);
			for (int i = 0; i < methods.Length; i++)
			{
				var attributes = methods [i].GetCustomAttributes (true);//(typeof(EditorDebugMethod), true);
				if (attributes.Length == 0)
					continue;
				var parameters = methods [i].GetParameters ();
				if (parameters != null && CanSerialiseAllParameters (parameters))
					m_debugMethods.Add (new EditorDebugMethod (methods [i], parameters));
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
					{
						Type type = dm.parameterInfos [k].ParameterType;
						if (dm.parameterInfos [k].ParameterType.IsEnum)
							type = typeof(System.Enum);
						dm.parameters [k] = UnitySerialiseFieldByType [type] (dm.parameters [k], dm.parameterInfos [k]);
					}
					EditorGUILayout.EndVertical ();
				}
			}

			EditorGUILayout.EndVertical ();
			base.OnInspectorGUI ();
		}
		
		//----------------------------------------------------------------------
		bool CanSerialiseAllParameters(ParameterInfo[] _params)
		{ 
			for (int i = 0; i < _params.Length; i++)
			{
				var type = _params [i].ParameterType;
				if (type.IsEnum)
					continue;
				if (type.IsArray || type is IList)
					return false;
				if (!UnitySerialiseFieldByType.ContainsKey (_params [i].ParameterType))
					return false;
			}
			return true;
		}
	}

	#endif
}