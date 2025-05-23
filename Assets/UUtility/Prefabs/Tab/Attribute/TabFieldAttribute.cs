using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UTool.TabSystem
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class TabFieldAttribute : Attribute
    {
        public object parent;

        public bool vaild => parent != null;

        public TabName tab;
        public string variableName;
        public FieldUpdateMode fieldUpdateModel;
        public string callbackMethodName;

        public TabVariableType variableType;
        public Type vType;
        public object defaultValue;

        public TVariable tVariable;
        public FieldInfo fieldInfo;

        public TabFieldAttribute(FieldUpdateMode fieldUpdateModel, string callbackMethodName = null) : this(TabName.None, fieldUpdateModel: fieldUpdateModel, callbackMethodName: callbackMethodName)
        {
        }

        public TabFieldAttribute(string callbackMethodName) : this(TabName.None, callbackMethodName: callbackMethodName)
        {
        }

        public TabFieldAttribute(TabName tabName = TabName.None, string fieldName = "", FieldUpdateMode fieldUpdateModel = FieldUpdateMode.Applied, string callbackMethodName = null)
        {
            tab = tabName;
            variableName = fieldName;
            this.fieldUpdateModel = fieldUpdateModel;
            this.callbackMethodName = callbackMethodName;
        }

        public void Setup(FieldInfo fieldInfo, object parent)
        {
            this.fieldInfo = fieldInfo;
            this.parent = parent;

            if (variableName == "")
                variableName = fieldInfo.Name;

            defaultValue = fieldInfo.GetValue(parent);
            if (defaultValue == null)
                defaultValue = GetDefaultValueForType(fieldInfo.FieldType);

            variableType = TabBackend.GetVariableType(defaultValue);
            vType = TabBackend.GetType(variableType);

            if (variableType == TabVariableType.None)
                Debug.LogWarning($"Tab Field Attribute - Variable '{variableName}' Is Unsupported Type : {defaultValue.GetType()}");
        }

        public void UpdateValue(object value, VariableUpdateType updateType)
        {
            if (updateType == VariableUpdateType.Changed && fieldUpdateModel == FieldUpdateMode.Applied)
                return;

            fieldInfo.SetValue(parent, value);

            if (string.IsNullOrEmpty(callbackMethodName))
                return;

            MethodInfo methodInfo = parent.GetType().GetMethod(callbackMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo == null)
                return;

            switch (methodInfo.GetParameters().Length)
            {
                case 0:
                    methodInfo.Invoke(parent, null);
                    break;

                case 1:
                    methodInfo.Invoke(parent, new object[] { updateType });
                    break;

                case 2:
                    methodInfo.Invoke(parent, new object[] { value, updateType });
                    break;

                case 3:
                    methodInfo.Invoke(parent, new object[] { value.GetType(), value, updateType });
                    break;

                default:
                    break;

            }
        }
        
        private object GetDefaultValueForType(Type type)
        {
            if (type == typeof(string))
                return "";
            else if (type == typeof(int))
                return 0;
            else if (type == typeof(float))
                return 0f;
            else if (type == typeof(bool))
                return false;
            else if (type == typeof(Vector2))
                return Vector2.zero;
            else if (type == typeof(Vector3))
                return Vector3.zero;
            else if (type == typeof(Vector4))
                return Vector4.zero;
            else if (type == typeof(Color))
                return Color.white;
            else if (type.IsEnum)
                return Enum.GetValues(type).GetValue(0);
            else if (type.IsArray)
                return Array.CreateInstance(type.GetElementType(), 0);
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return Activator.CreateInstance(type);
            else
                return null; // For types we can't create a default value for
        }
    }
}