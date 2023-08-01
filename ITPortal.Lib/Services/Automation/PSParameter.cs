﻿using System.Management.Automation;
using System.Runtime.Serialization;

namespace ITPortal.Lib.Services.Automation;

public class PSParameter {
    public string Name { get; }
    public object? Value { get; set; }
    public bool Mandatory { get; }
    public Type DesiredType { get; private set; }

    public PSParameter(string name, Type desiredType, bool mandatory = false)
    {
        Name = name;
        DesiredType = desiredType;
        Mandatory = mandatory;
        Value = InitValue();
    }

    private object? InitValue()
    {
        // it's very important to check IsValueType before calling GetUninitializedObject
        // GetUninitializedObject is valid for reference types, but it will not return null
        if (DesiredType.IsValueType)
        {
            // The actual default value for DateTime given by C# sucks (1/1/1001)
            if (DesiredType == typeof(DateTime))
            {
                return DateTime.Today;
            }
            else if (DesiredType == typeof(SwitchParameter))
            {
                DesiredType = typeof(bool);
                return false;
            }
            // This is what Microsoft's codebase uses to get default values for types at runtime
            return FormatterServices.GetUninitializedObject(DesiredType);
        }
        // Prepare default values for reference types
        else
        {
            if (DesiredType == typeof(string))
            {
                return string.Empty;
            }
            else if (DesiredType.IsArray)
            {
                Type? elementType = DesiredType.GetElementType();

                if (elementType == null)
                {
                    return null;
                }
                // All PowerShell arrays will be treated internally as lists of strings
                return new List<string>();
            }
        }
        return null;
    }

    public Type? GetValueType()
    {
        return Value?.GetType();
    }

    public override string? ToString()
    {
        return $"[{Name}, {Value}]";
    }
}

