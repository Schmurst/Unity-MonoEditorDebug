using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Serialization;
using System.Reflection;
using Type = System.Type;
using System.Linq;
using System;
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
[System.AttributeUsage(System.AttributeTargets.Property)]
public class ExposeInInspector : System.Attribute {
	public string m_prefix;
	public ExposeInInspector() { m_prefix = string.Empty; }
	public ExposeInInspector(string _prefix) {m_prefix = _prefix;}
}


//----------------------------------------------------------------------
public abstract class MonoEditorDebug : MonoBehaviour
{
    #if UNITY_EDITOR
    delegate object SerialiseParameter(object param, ParameterInfo info);
    static Dictionary<Type, SerialiseParameter> UnitySerialiseFieldByType = new Dictionary<Type, SerialiseParameter> {
        {typeof(int),             (p, i) => {return(object)EditorGUILayout.IntField (i.Name, (int)p);}},
        {typeof(bool),             (p, i) => {return(object)EditorGUILayout.Toggle (i.Name, (bool)p);}},
        {typeof(float),         (p, i) => {return(object)EditorGUILayout.FloatField (i.Name, (float)p);}},
        {typeof(string),         (p, i) => {return(object)EditorGUILayout.TextField (i.Name, (string)p);}},
        {typeof(long),             (p, i) => {return(object)EditorGUILayout.LongField (i.Name, (long)p);}},
        {typeof(System.Enum),    (p, i) => {return(object)EditorGUILayout.EnumPopup (i.Name, (System.Enum)p);}},
        {typeof(Vector2),         (p, i) => {return(object)EditorGUILayout.Vector2Field (i.Name, (Vector2)p);}},
        {typeof(Vector2Int),    (p, i) => {return(object)EditorGUILayout.Vector2IntField (i.Name, (Vector2Int)p);}},
        {typeof(Vector3),         (p, i) => {return(object)EditorGUILayout.Vector3Field (i.Name, (Vector3)p);}},
        {typeof(Vector3Int),    (p, i) => {return(object)EditorGUILayout.Vector3IntField (i.Name, (Vector3Int)p);}},
        {typeof(Vector4),         (p, i) => {return(object)EditorGUILayout.Vector4Field (i.Name, (Vector4)p);}},
        {typeof(Rect),             (p, i) => {return(object)EditorGUILayout.RectField (i.Name, (Rect)p);}},
        {typeof(RectInt),         (p, i) => {return(object)EditorGUILayout.RectIntField (i.Name, (RectInt)p);}},
        {typeof(Color),         (p, i) => {return(object)EditorGUILayout.ColorField (i.Name, (Color)p);}},
        {typeof(Color32),         (p, i) => {return(object)EditorGUILayout.ColorField (i.Name, (Color)p);}},
        {typeof(Bounds),         (p, i) => {return(object)EditorGUILayout.BoundsField (i.Name, (Bounds)p);}},
        {typeof(BoundsInt),     (p, i) => {return(object)EditorGUILayout.BoundsIntField (i.Name, (BoundsInt)p);}},
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
    class InspectorMessageData
	{
		PropertyInfo property;
		MethodInfo getMethod;
		string prefix;

		public InspectorMessageData(PropertyInfo _property, string _prefix)
		{
			prefix = _prefix;
			property = _property;
			getMethod = property.GetMethod;
		}

		public string GetMessage(object _object)
		{
			return $"{prefix}{getMethod.Invoke(_object, null)}";
		}
	}

    //----------------------------------------------------------------------
    static object CreateDefaultObject(System.Type type)
    {
        if (type.IsValueType)
            return Activator.CreateInstance (type);
        if (CanSerialiseEnumerable (type))
        {
            var elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
            return Activator.CreateInstance (typeof(List<>).MakeGenericType (new Type[]{ elementType }));
        }
        return null;
    }

    //----------------------------------------------------------------------
    static bool CanSerialiseEnumerable(System.Type _type)
    {
        //array
        if (_type.IsArray)
            return CanSerialiseType(_type.GetElementType());

        //list<T>
        if (!_type.IsGenericType || _type.GetGenericTypeDefinition () != typeof(List<>))
            return false;
        var parameters = _type.GetGenericArguments ();
        if (parameters.Length > 1)
            return false;
        return CanSerialiseType(parameters[0]);
    }

    //----------------------------------------------------------------------
    static bool CanSerialiseType(System.Type _type)
    {
        var param = !_type.IsEnum ? _type : typeof(System.Enum);
        if (!UnitySerialiseFieldByType.ContainsKey (_type))
        {
            if (typeof(UnityEngine.Object).IsAssignableFrom(_type))
            {
                var methodTemplate = typeof(MonoBehaviourEditor).GetMethod("DrawMonobehaviour", BindingFlags.Static | BindingFlags.NonPublic);
                var newMethod = methodTemplate.MakeGenericMethod(_type);
                var serialiseDelegate = (SerialiseParameter) Delegate.CreateDelegate(typeof(SerialiseParameter), newMethod);
                UnitySerialiseFieldByType.Add(_type, serialiseDelegate);
            }
            if (!UnitySerialiseFieldByType.ContainsKey(_type))
                return false;
        }
        return UnitySerialiseFieldByType.ContainsKey(param);
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
        bool m_areMethodsVisible = true;
        bool m_areMessagesVisible = true;
        MonoEditorDebug m_this;
        List<EditorDebugMethodData> m_debugMethods;
        List<InspectorMessageData> m_messages;

        //----------------------------------------------------------------------
        public override bool RequiresConstantRepaint()
        {
            return m_messages != null && m_messages.Count > 0;
        }
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

            m_messages = new List<InspectorMessageData>();
			var properties = m_this.GetType().GetProperties(METHOD_FLAGS);
			for (int i = 0; i < properties.Length; i++)
			{
				var attribute = properties[i].GetCustomAttribute<ExposeInInspector>(true);
				if (attribute == null)
					continue;
				m_messages.Add(new InspectorMessageData(properties[i], attribute.m_prefix));
			}
        }

        //----------------------------------------------------------------------
        public override void OnInspectorGUI ()
        {
            if (m_this == null)
                Initialise ();
            
            if (m_debugMethods.Count > 0)
                DrawAndUpdateMethods();

            if (EditorApplication.isPlaying && m_messages.Count > 0)
                DrawInspectorMessages();

            base.OnInspectorGUI ();
        }

        //----------------------------------------------------------------------
        void DrawAndUpdateMethods()
        {
            EditorGUILayout.BeginVertical (EditorStyles.helpBox);
            m_areMethodsVisible = GUILayout.Toggle (m_areMethodsVisible, "Debug Commands", EditorStyles.boldLabel);

            if (m_areMethodsVisible)
            {
                for (int i = 0; i < m_debugMethods.Count; i++)
                {
                    var dm = m_debugMethods [i];
                    if (!Application.isPlaying && !dm.showInEditMode)
                        continue;
                    EditorGUILayout.BeginVertical (EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal ();
                    GUILayout.Label (dm.method.Name);
                    if (GUILayout.Button("Invoke", GUILayout.MaxWidth(55)))
                        OnInvokePress(dm);
                    EditorGUILayout.EndHorizontal ();
                    for (int k = 0; k < dm.parameterInfos.Length; k++)
                    DrawParameterFieldAndUpdateValue (ref dm.parameters [k], dm.parameterInfos [k]);
                    EditorGUILayout.EndVertical ();
                }
            }
            EditorGUILayout.EndVertical ();
        }

        //----------------------------------------------------------------------
        void DrawInspectorMessages()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            m_areMessagesVisible = GUILayout.Toggle(m_areMessagesVisible, "Debug Messages", EditorStyles.boldLabel);

            if (m_areMessagesVisible)
            {
                for (int i = 0; i < m_messages.Count; i++)
                {
                    GUILayout.Label(m_messages[i].GetMessage(m_this));
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        //----------------------------------------------------------------------
        void OnInvokePress(EditorDebugMethodData _methodData)
        {
            var parametersToInvoke = GetParametersToInvoke(_methodData);
            _methodData.method.Invoke(m_this, parametersToInvoke);            
        }

        //----------------------------------------------------------------------
		static object DrawMonobehaviour<T>(object _param, ParameterInfo _info) where T: UnityEngine.Object
		{
			return EditorGUILayout.ObjectField(_info.Name, _param as T, typeof(T), true);
		}

        //----------------------------------------------------------------------
        object[] GetParametersToInvoke(EditorDebugMethodData _methodData)
        {
            var _params = new object[_methodData.parameters.Length];

            //convert any parameters that should be arrays
            for (int i = 0; i < _methodData.parameters.Length; i++)
            {
                if (_methodData.parameterInfos[i].ParameterType.IsArray)
                {
                    var list = _methodData.parameters[i];
                    var arrayType = _methodData.parameterInfos[i].ParameterType;
                    var toArrayMethod = list.GetType().GetMethod("ToArray");
                    var array = toArrayMethod.Invoke(list, null);

                    _params[i] = array;
                }
                else
                {
                    _params[i] = _methodData.parameters[i];
                }
            }

            return _params;
        }

        //----------------------------------------------------------------------
        void DrawParameterFieldAndUpdateValue(ref object _param, ParameterInfo _info)
        {
            Type type = _info.ParameterType;
            if(type.IsEnum)
                type = typeof(System.Enum);
            
            if (CanSerialiseEnumerable(type))
                DrawEnumerableFieldAndUpdateValues (ref _param, _info);
            else
                _param = UnitySerialiseFieldByType [type] (_param, _info);
        }

        //----------------------------------------------------------------------
        void DrawEnumerableFieldAndUpdateValues(ref object _param, ParameterInfo _info)
        {
            var list = _param as IList;
            int desiredLength = EditorGUILayout.IntField ("Count", list.Count);
            int diff = desiredLength - list.Count;
            var paramType = _info.ParameterType; 
            var baseType = paramType.IsArray ?  paramType.GetElementType() : paramType.GetGenericArguments () [0];

            if (diff != 0)
            {
                for (int i = 0; i < diff; i++)
                {
                    object value = CreateDefaultObject (baseType);
                    list.Add (value);    
                }

                int target = list.Count - 1 + diff;
                for (int i = list.Count-1; i > target; i--)
                  list.RemoveAt (i);
            }
            
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                list[i] = UnitySerialiseFieldByType [baseType] (item, _info);
            }
        }

        //----------------------------------------------------------------------
        bool CanSerialiseAllParameters(ParameterInfo[] _params)
        { 
            for (int i = 0; i < _params.Length; i++)
            {
                var type = _params [i].ParameterType;
                if (CanSerialiseEnumerable(type))
                    type = type.IsArray ? type.GetElementType() : type.GetGenericArguments ()[0];
                if (!CanSerialiseType(type))
                    return false;
            }
            return true;
        }

        //----------------------------------------------------------------------
        static T Cast<T> (object _object)
        {
            return (T)_object;
        }

        //----------------------------------------------------------------------
        static MethodInfo CreateCastMethod(Type _type)
        {
            return typeof(MonoEditorDebug).GetMethod("Cast", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(_type);
        }

        //----------------------------------------------------------------------
        static T Find<T>(T[] _array, Predicate<T> _predicate)
        {
            if (_array == null || _predicate == null)
                return default(T);
            for (int i = 0; i < _array.Length; i++)
                if (_predicate(_array[i]))
                    return _array[i];
            return default(T);
        }

        //----------------------------------------------------------------------
        static bool Contains<T>(T[] _array, Predicate<T> _predicate)
        {
            if (_array == null || _predicate == null)
                return false;
            for (int i = 0; i < _array.Length; i++)
                if (_predicate(_array[i]))
                    return true;
            return false;
        }
    }
    #endif
}