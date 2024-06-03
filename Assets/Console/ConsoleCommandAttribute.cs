using System.Collections;
using System.Collections.Generic;
using System;


namespace UConsole
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class ConsoleCommandAttribute : Attribute
    {
        public string Description;
        public bool AllowParameterExpansion;

        public ConsoleCommandAttribute()
        {
            Description = "";
            AllowParameterExpansion = true;
        }

        public ConsoleCommandAttribute(string description)
        {
            Description = description;
            AllowParameterExpansion = true;
        }

        public ConsoleCommandAttribute(string description, bool allowParameterExpansion)
        {
            Description = description;
            AllowParameterExpansion = allowParameterExpansion;
        }
    }
}
