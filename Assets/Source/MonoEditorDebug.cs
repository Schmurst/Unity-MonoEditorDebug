using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Serialization;
using System.Reflection;
#endif

public class EditorDebugMethod : System.Attribute{}
public abstract class MonoEditorDebug : MonoBehaviour
{
	protected abstract System.Type Type { get; }	

	#if UNITY_EDITOR
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
			var methods = m_this.Type.GetMethods (METHOD_FLAGS);
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
		bool CanUnitySerialise(System.Type _type)
		{
			if(_type.IsEnum || _type.IsPrimitive)
				return true;
			if (_type == typeof(string))
				return true;
			return false;
		}

		//----------------------------------------------------------------------
		bool CanSerialiseAllParameters(ParameterInfo[] _params)
		{ 
			for (int i = 0; i < _params.Length; i++)
			{
				var param = _params [i];
				if (!CanUnitySerialise (param.ParameterType))
					return false;
			}
			return true;
		}

		//----------------------------------------------------------------------
		public override void OnInspectorGUI ()
		{
			if (m_this == null)
				Initialise ();

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
					if (GUILayout.Button ("Invoke", GUILayout.MaxWidth (80)))
						dm.method.Invoke (m_this, dm.parameters);
					EditorGUILayout.EndHorizontal ();
					for (int k = 0; k < dm.parameterInfos.Length; k++)
						dm.parameters [k] = SerialiseParameter (
							dm.parameters [k], dm.parameterInfos [k]);
					EditorGUILayout.EndVertical ();
				}
			}

			EditorGUILayout.EndVertical ();
			base.OnInspectorGUI ();
		}

		//----------------------------------------------------------------------
		object SerialiseParameter(object _param, ParameterInfo _info)
		{
			var type = _info.ParameterType;
			if(type == typeof(int))
				return (object)EditorGUILayout.IntField (_info.Name, (int)_param);
			if( type == typeof(string))
				return (object)EditorGUILayout.TextField (_info.Name, (string)_param);

			return (object)default(int);
		}
	}

	#endif
}