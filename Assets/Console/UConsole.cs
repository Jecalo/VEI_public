using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine.Experimental.AI;
using static UnityEngine.Rendering.DebugUI;
using Newtonsoft.Json.Linq;

namespace UConsole
{
    public class UConsole
    {
        private class InputString
        {
            public string str;
            public int index;
            public int lastEnd;
            public readonly int end;
            public List<Token> tokens;
            public bool hasError;
            public string error;

            public char Current { get { return str[index]; } }
            public char Next { get { return str[index + 1]; } }

            public bool IsAtEnd { get { return index == end; } }
            public bool IsLast { get { return index == (end - 1); } }

            public int LengthFromLast { get { return index - lastEnd; } }
            public int RemainingLength { get { return end - index; } }


            public void Advance()
            {
                index++;
            }

            public void SetEnd()
            {
                lastEnd = index;
            }

            public void DiscardWhitespace()
            {
                while (index != end)
                {
                    if (str[index] != ' ') { break; }
                    else { index++; }
                }
            }

            public void SetError(string msg)
            {
                if (hasError) { Debug.LogWarning("Overwriting error"); }
                if (msg == null) { error = ""; }
                else { error = msg; }
                hasError = true;
            }

            public InputString(string str)
            {
                if (str == null) { this.str = "\0"; }
                else { this.str = str + '\0'; }
                index = 0;
                lastEnd = 0;
                end = this.str.Length - 1;
                tokens = new List<Token>();
                hasError = false;
                error = "Unkown error";
            }
        }

        private class TokenChain
        {
            public List<Expression> exprs;
            public int index;
            public int lastEnd;
            public readonly int end;
            public List<Token> tokens;
            public bool hasError;
            public string error;

            public TokenType Current { get { return tokens[index].Type; } }
            public TokenType Next { get { return tokens[index + 1].Type; } }

            public object CurrentValue { get { return tokens[index].Value; } }
            public object NextValue { get { return tokens[index + 1].Value; } }

            public bool IsAtEnd { get { return index == end; } }
            public bool IsLast { get { return index == (end - 1); } }

            public int LengthFromLast { get { return index - lastEnd; } }
            public int RemainingLength { get { return end - index; } }


            public void Advance()
            {
                index++;
            }

            public void SetEnd()
            {
                lastEnd = index;
            }

            public void SetError(string msg)
            {
                if (hasError) { Debug.LogWarning("Overwriting error"); }
                if (msg == null) { error = ""; }
                else { error = msg; }
                hasError = true;
            }

            public TokenChain(List<Token> tokens)
            {
                if (tokens == null) { throw new NullReferenceException(); }
                this.tokens = tokens;

                if (tokens.Count == 0 || tokens[tokens.Count - 1].Type != TokenType.EOF)
                {
                    tokens.Add(new Token(TokenType.EOF));
                }

                exprs = new List<Expression>();
                index = 0;
                lastEnd = 0;
                end = this.tokens.Count - 1;
                hasError = false;
                error = "Unkown error";
            }

        }

        private struct EnumLiteral
        {
            public string Identifier;

            public EnumLiteral(string identifier) { Identifier = identifier; }
        }

        private enum TokenType
        {
            Unknown, Error, EOF, Void,
            ParenthesisOpen, ParenthesisClose, BraceOpen, BraceClose, BracketOpen, BracketClose,
            Dot, Comma, Semicolon, Greater, Lesser, Equal, Not,
            Plus, Minus, Asterisk, Slash,
            Dollar, Hash,
            Identifier, String, Integer, Float
        }

        private enum ExpressionType
        {
            Unknown, Literal, EnumLiteral, Variable, Word, Assignment, Command
        }

        private class Expression
        {
            public ExpressionType Type = ExpressionType.Unknown;
            public string Identifier = null;
            public object Value = null;
            public bool DefinedVar = false;
            public List<Expression> Children = null;
        }

        private struct Token
        {
            public TokenType Type;
            public int Start;
            public int End;
            public object Value;

            public Token(TokenType type, int start, int end)
            {
                Type = type;
                Start = start;
                End = end;
                Value = null;
            }

            public Token(TokenType type, int start, int end, object value)
            {
                Type = type;
                Start = start;
                End = end;
                Value = value;
            }

            public Token(TokenType type, object value)
            {
                Type = type;
                Start = -1;
                End = -1;
                Value = value;
            }

            public Token(TokenType type)
            {
                Type = type;
                Start = -1;
                End = -1;
                Value = null;
            }
        }

        private class Command
        {
            public string name;
            public string description;
            public MethodInfo method;
            public string[] parameterNames;
            public Type[] parameters;
            public bool targetConsole;

            public Type[] expandedParameters;

            public string GetFullDescription()
            {
                System.Text.StringBuilder sb = new();
                List<Type> enums = new List<Type>();

                sb.Append(name);
                sb.Append(" (");

                if (parameters.Length != 0)
                {
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        sb.Append(parameters[i].Name);
                        sb.Append(' ');
                        sb.Append(parameterNames[i]);
                        sb.Append(' ');

                        if (parameters[i].IsEnum) { enums.Add(parameters[i]); }
                    }
                    sb.Remove(sb.Length - 1, 1);
                }
                sb.Append(")");

                if (description != "")
                {
                    sb.Append('\n');
                    sb.Append(description);
                }

                foreach (Type t in enums)
                {
                    sb.Append('\n');
                    sb.Append(t.Name);
                    sb.Append(": ");

                    foreach (var n in t.GetEnumNames())
                    {
                        sb.Append(n);
                        sb.Append(", ");
                    }

                    sb.Remove(sb.Length - 2, 2);
                }

                return sb.ToString();
            }

            public string GetSignature()
            {
                System.Text.StringBuilder sb = new();

                foreach (Type type in parameters)
                {
                    sb.Append(type.Name).Append(' ');
                }
                if (parameters.Length != 0) { sb.Remove(sb.Length - 1, 1); }

                return sb.ToString();
            }
        }

        private Dictionary<string, List<Command>> commands;
        private CultureInfo culture;
        private string[] commandNames;

        public Dictionary<string, object> StoredVars;

        public bool EnableExpandedParameters = true;


        public UConsole(params Type[] extraMethodSources)
        {
            StoredVars = new Dictionary<string, object>();

            culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.NumberFormat.NumberGroupSeparator = ",";

            commands = new Dictionary<string, List<Command>>();
            List<MethodInfo> methods = new();

            Type[] methodSources = new Type[2 + extraMethodSources.Length];
            methodSources[0] = typeof(DefaultConsoleCommands);
            methodSources[1] = typeof(ConsoleCommands);
            extraMethodSources.CopyTo(methodSources, 2);


            foreach (MethodInfo m in typeof(UConsole).GetMethods())
            {
                if (m.GetCustomAttribute(typeof(ConsoleCommandAttribute), false) == null) { continue; }
                else { methods.Add(m); }
            }

            foreach (Type type in methodSources)
            {
                foreach (MethodInfo m in type.GetMethods())
                {
                    if (m.Name == "Equals") { continue; }
                    if (m.Name == "GetHashCode") { continue; }
                    if (m.Name == "GetType") { continue; }
                    if (m.Name == "ToString") { continue; }

                    methods.Add(m);
                }
            }

            foreach (var m in methods)
            {
                Command command = new Command();
                var parameters = m.GetParameters();
                command.name = m.Name.ToLower();
                command.method = m;
                command.parameters = new Type[parameters.Length];
                command.parameterNames = new string[parameters.Length];
                command.targetConsole = m.DeclaringType == typeof(UConsole);

                for (int i = 0; i < parameters.Length; i++)
                {
                    command.parameters[i] = parameters[i].ParameterType;
                    command.parameterNames[i] = parameters[i].Name;
                }

                var att = (ConsoleCommandAttribute)m.GetCustomAttribute(typeof(ConsoleCommandAttribute), false);
                if (att != null) { command.description = att.Description; }
                else { command.description = ""; }

                if (att == null || att.AllowParameterExpansion)
                {
                    List<Type> exp = new List<Type>();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].ParameterType == typeof(Vector2)) { exp.Add(typeof(float)); exp.Add(typeof(float)); }
                        else if (parameters[i].ParameterType == typeof(Vector3)) { exp.Add(typeof(float)); exp.Add(typeof(float)); exp.Add(typeof(float)); }
                        else if (parameters[i].ParameterType == typeof(Vector4)) { exp.Add(typeof(float)); exp.Add(typeof(float)); exp.Add(typeof(float)); exp.Add(typeof(float)); }
                        else if (parameters[i].ParameterType == typeof(Vector2Int)) { exp.Add(typeof(int)); exp.Add(typeof(int)); }
                        else if (parameters[i].ParameterType == typeof(Vector3Int)) { exp.Add(typeof(int)); exp.Add(typeof(int)); exp.Add(typeof(int)); }
                        else if (parameters[i].ParameterType == typeof(Color)) { exp.Add(typeof(float)); exp.Add(typeof(float)); exp.Add(typeof(float)); exp.Add(typeof(float)); }
                        else { exp.Add(parameters[i].ParameterType); }
                    }

                    if (parameters.Length != exp.Count) { command.expandedParameters = exp.ToArray(); }
                    else { command.expandedParameters = new Type[0]; }
                }
                else { command.expandedParameters = new Type[0]; }

                if (!commands.ContainsKey(command.name)) { commands[command.name] = new List<Command>(); }

                foreach (var c in commands[command.name])
                {
                    if (c.parameters == command.parameters)
                    {
                        Debug.LogWarningFormat("Identical overload: {0} {1}", command.name, command.GetSignature());
                        break;
                    }
                }

                commands[command.name].Add(command);
            }

            List<string> names = new List<string>();
            foreach (var c in commands.Values)
            {
                names.Add(c[0].name);
            }

            names.Sort();

            commandNames = names.ToArray();
        }

        public void Input(string cmd)
        {
            if (cmd == null || cmd.Length == 0) { return; }

            object r = null;
            Type t = null;
            Expression expression = null;

            if (!ParseCommand(cmd, ref expression)) { return; }
            InvokeExpression(expression.Children, ref r, ref t);
        }

        private bool TryInvokeMethod(string name, object[] parameters, ref object result, ref Type resultType)
        {
            object[] invokeParams = null;
            resultType = null;

            if (!commands.ContainsKey(name)) { Debug.LogFormat("Unknown command: \"{0}\"", name); return false; }

            List<Command> list = commands[name];

            foreach (var command in list)
            {
                if (CheckParameters(command, parameters, ref invokeParams))
                {
                    try
                    {
                        if (command.method.ReturnType == typeof(void))
                        {
                            if (command.targetConsole) { command.method.Invoke(this, invokeParams); }
                            else { command.method.Invoke(null, invokeParams); }
                        }
                        else
                        {
                            resultType = command.method.ReturnType;
                            if (command.targetConsole) { result = command.method.Invoke(this, invokeParams); }
                            else { result = command.method.Invoke(null, invokeParams); }
                        }
                        return true;
                    }
                    catch (Exception e)
                    { 
                        Debug.LogException(e);
                        return false;
                    }
                }
            }

            System.Text.StringBuilder sb = new();
            if (parameters.Length != 0)
            {
                foreach (object arg in parameters)
                {
                    sb.Append(arg.GetType().Name).Append(' ');
                }
                sb.Remove(sb.Length - 1, 1);
            }

            Debug.LogFormat("Argument mismatch: {0} {1}", name, sb.ToString());
            return false;
        }

        private bool InvokeExpression(List<Expression> exprs, ref object result, ref Type resultType)
        {
            result = null;
            resultType = null;

            if (exprs.Count == 0) { Debug.Log("Empty statement."); return false; }

            if (exprs[0].Type == ExpressionType.Variable)
            {
                string name = exprs[0].Identifier;
                if (exprs.Count != 3) { goto unknownStructure; }
                else if (exprs[1].Type != ExpressionType.Assignment) { goto unknownStructure; }
                else if (exprs[2].Type == ExpressionType.Literal)
                {
                    StoredVars[exprs[0].Identifier] = exprs[2].Value;
                    return true;
                }
                else if (exprs[2].Type == ExpressionType.Variable)
                {
                    if (exprs[2].DefinedVar) { StoredVars[exprs[0].Identifier] = exprs[2].Value; return true; }
                    else { Debug.LogFormat("Undefined variable: {0}", exprs[2].Identifier); return false; }
                }
                else if (exprs[2].Type == ExpressionType.Command)
                {
                    object subResult = null;
                    Type subType = null;
                    if (InvokeExpression(exprs[2].Children, ref subResult, ref subType))
                    {
                        if (subType == null) { Debug.Log("Subcommand has no return value."); return false; }
                        StoredVars[exprs[0].Identifier] = subResult;
                        return true;
                    }
                    else { return false; }
                }
                else if (exprs[2].Type == ExpressionType.Word && exprs[2].Identifier == "del")
                {
                    StoredVars.Remove(exprs[0].Identifier);
                    return true;
                }
                else { goto unknownStructure; }
            }
            else if (exprs[0].Type == ExpressionType.Word)
            {
                string command = exprs[0].Identifier;
                object[] parameters = new object[exprs.Count - 1];

                for (int i = 1; i < exprs.Count; i++)
                {
                    if (exprs[i].Type == ExpressionType.Literal) { parameters[i - 1] = exprs[i].Value; }
                    else if (exprs[i].Type == ExpressionType.Variable)
                    {
                        if (exprs[i].DefinedVar) { parameters[i - 1] = exprs[i].Value; }
                        else { Debug.LogFormat("Undefined variable: {0}", exprs[i].Identifier); return false; }
                    }
                    else if (exprs[i].Type == ExpressionType.Command)
                    {
                        object subResult = null;
                        Type subType = null;
                        if (InvokeExpression(exprs[i].Children, ref subResult, ref subType))
                        {
                            if (subType == null) { Debug.Log("Subcommand has no return value."); return false; }
                            parameters[i - 1] = subResult;
                        }
                        else { return false; }
                    }
                    else if (exprs[i].Type == ExpressionType.Word && exprs[0].Identifier == "help")
                    {
                        parameters[i - 1] = exprs[i].Identifier;
                    }
                    else if (exprs[i].Type == ExpressionType.EnumLiteral)
                    {
                        parameters[i - 1] = new EnumLiteral(exprs[i].Identifier);
                    }
                    else { goto unknownStructure; }
                }

                return TryInvokeMethod(command, parameters, ref result, ref resultType);
            }

            unknownStructure:
            Debug.Log("Unknown command structure.");
            result = null;
            resultType = null;
            return false;
        }

        //Check if the given parameters match those of the command.
        //If matching, perform casting or compacting if necessary and store the ready to be invoked parameters in invokeParams.
        private bool CheckParameters(Command command, object[] parameters, ref object[] invokeParams)
        {
            bool expanded = false;
            Type[] commandParams = command.parameters;

            if (parameters.Length != command.parameters.Length)
            {
                if (!EnableExpandedParameters) { return false; }
                else if (parameters.Length == 0) { return false; }
                else if (parameters.Length != command.expandedParameters.Length) { return false; }
                else { commandParams = command.expandedParameters; expanded = true; }
            }
            else
            {
                commandParams = command.parameters;
                expanded = false;
            }

            invokeParams = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                if (commandParams[i] == typeof(object)) { invokeParams[i] = parameters[i]; }
                else if (parameters[i].GetType() == commandParams[i]) { invokeParams[i] = parameters[i]; }
                else if (commandParams[i] == typeof(object[]) && parameters[i].GetType().IsArray)
                {
                    Type elementType = parameters[i].GetType().GetElementType();
                    if (elementType == typeof(object)) { invokeParams[i] = parameters[i]; continue; }

                    Array original = (Array)parameters[i];
                    Array a = Array.CreateInstance(typeof(object), original.Length);
                    for (int j = 0; j < original.Length; j++)
                    {
                        a.SetValue(original.GetValue(j), j);
                    }

                    invokeParams[i] = a;
                }
                else if (commandParams[i] == typeof(int[]) && parameters[i].GetType() == typeof(float[]))
                {
                    float[] original = (float[])parameters[i];
                    int[] a = new int[original.Length];

                    for (int j = 0; j < original.Length; j++)
                    {
                        a[j] = (int)original[j];
                    }

                    invokeParams[i] = a;
                }
                else if (commandParams[i].IsEnum && parameters[i].GetType() == typeof(EnumLiteral))
                {
                    if (Enum.TryParse(commandParams[i], ((EnumLiteral)parameters[i]).Identifier, true, out object e)) { invokeParams[i] = e; }
                    else { return false; }
                }
                else if (commandParams[i] == typeof(float) && parameters[i].GetType() == typeof(int)) { invokeParams[i] = (float)(int)parameters[i]; }
                else if (commandParams[i] == typeof(int) && parameters[i].GetType() == typeof(float)) { invokeParams[i] = (int)(float)parameters[i]; }
                else if (commandParams[i] == typeof(Color) && parameters[i].GetType() == typeof(Vector4)) { invokeParams[i] = (Color)(Vector4)parameters[i]; }
                else if (commandParams[i] == typeof(Vector3Int) && parameters[i].GetType() == typeof(Vector3))
                {
                    Vector3 v = (Vector3)parameters[i];
                    invokeParams[i] = new Vector3Int((int)v.x, (int)v.y, (int)v.z);
                }
                else if (commandParams[i] == typeof(Vector2Int) && parameters[i].GetType() == typeof(Vector2))
                {
                    Vector3 v = (Vector2)parameters[i];
                    invokeParams[i] = new Vector2Int((int)v.x, (int)v.y);
                }
                else if (commandParams[i] == typeof(bool) && parameters[i].GetType() == typeof(int)) { invokeParams[i] = (int)parameters[i] != 0; }
                else if (commandParams[i] == typeof(bool) && parameters[i].GetType() == typeof(float)) { invokeParams[i] = (float)parameters[i] != 0f; }
                else if (commandParams[i] == typeof(GameObject) && parameters[i] is Component) { invokeParams[i] = ((Component)parameters[i]).gameObject; }
                else { return false; }
            }

            if (expanded) { invokeParams = CombineParameters(command, invokeParams); }

            return true;
        }

        private object[] CombineParameters(Command command, object[] parameters)
        {
            object[] result = new object[command.parameters.Length];

            for (int i = 0, j = 0; i < command.parameters.Length; i++)
            {
                if (command.parameters[i] == typeof(Vector2))
                {
                    result[i] = (new Vector2((float)parameters[j], (float)parameters[j + 1]));
                    j += 2;
                }
                else if (command.parameters[i] == typeof(Vector3))
                {
                    result[i] = (new Vector3((float)parameters[j], (float)parameters[j + 1], (float)parameters[j + 2]));
                    j += 3;
                }
                else if (command.parameters[i] == typeof(Vector4))
                {
                    result[i] = (new Vector4((float)parameters[j], (float)parameters[j + 1], (float)parameters[j + 2], (float)parameters[j + 3]));
                    j += 4;
                }
                else if (command.parameters[i] == typeof(Vector2Int))
                {
                    result[i] = (new Vector2Int((int)parameters[j], (int)parameters[j + 1]));
                    j += 2;
                }
                else if (command.parameters[i] == typeof(Vector3Int))
                {
                    result[i] = (new Vector3Int((int)parameters[j], (int)parameters[j + 1], (int)parameters[j + 2]));
                    j += 3;
                }
                else if (command.parameters[i] == typeof(Color))
                {
                    result[i] = (new Color((float)parameters[j], (float)parameters[j + 1], (float)parameters[j + 2], (float)parameters[j + 3]));
                    j += 4;
                }
                else if (command.parameters[i] == typeof(Color32))
                {
                    result[i] = (new Color32((byte)(int)parameters[j], (byte)(int)parameters[j + 1], (byte)(int)parameters[j + 2], (byte)(int)parameters[j + 3]));
                    j += 4;
                }
                else
                {
                    result[i] = (parameters[j]);
                    j++;
                }
            }

            return result;
        }


        //Attempt to parse a full command. Output the resulting expression.
        private bool ParseCommand(string str, ref Expression command)
        {
            if (str == null || str.Length == 0) { return false; }

            command = new Expression() { Children = new List<Expression>() };

            InputString inputString = new InputString(str);
            inputString.DiscardWhitespace();

            while (!inputString.IsAtEnd)
            {
                ParseToken(inputString);

                if (inputString.hasError) { Debug.LogFormat("Could not parse the command: {0}", inputString.error); return false; }
            }

            inputString.tokens.Add(new Token(TokenType.EOF, inputString.index, inputString.index));

            TokenChain chain = new TokenChain(inputString.tokens);

            while (!chain.IsAtEnd)
            {
                TryParseExpression(chain, command.Children);
                if (chain.hasError) { Debug.LogFormat("Could not parse the command: {0}", chain.error); return false; }
            }

            return true;
        }

        private void TryParseExpression(TokenChain chain, List<Expression> target)
        {
            if (chain.Current == TokenType.Identifier)
            {
                string id = (string)chain.CurrentValue;
                chain.Advance();

                if (id == "true") { target.Add(new Expression() { Type = ExpressionType.Literal, Value = true }); }
                else if (id == "false") { target.Add(new Expression() { Type = ExpressionType.Literal, Value = false }); }
                else if (id == "this") { target.Add(new Expression() { Type = ExpressionType.Literal, Value = this }); }
                else if (id == "null") { target.Add(new Expression() { Type = ExpressionType.Literal, Value = null }); }
                else { target.Add(new Expression() { Type = ExpressionType.Word, Identifier = id }); }
            }
            else if (chain.Current == TokenType.Dollar)
            {
                chain.Advance();    
                if (chain.Current == TokenType.Identifier)
                {
                    string name = (string)chain.CurrentValue;
                    if (StoredVars.TryGetValue(name, out var value))
                    {
                        target.Add(new Expression() { Identifier = name, Type = ExpressionType.Variable, Value = value, DefinedVar = true });
                    }
                    else
                    {
                        target.Add(new Expression() { Identifier = name, Type = ExpressionType.Variable });
                    }
                    chain.Advance();
                }
                else { chain.SetError("Var symbol not followed by identifier."); return; }
            }
            else if (chain.Current == TokenType.Hash)
            {
                chain.Advance();
                if (chain.Current == TokenType.Identifier)
                {
                    target.Add(new Expression() { Identifier = (string)chain.CurrentValue, Type = ExpressionType.EnumLiteral });
                    chain.Advance();
                }
                else { chain.SetError("Enum symbol not followed by identifier."); return; }
            }
            else if (chain.Current == TokenType.Equal)
            {
                target.Add(new Expression() { Type = ExpressionType.Assignment });
                chain.Advance();

                if (chain.Current == TokenType.Identifier && (string)chain.CurrentValue != "del")
                {
                    Expression result = new Expression() { Type = ExpressionType.Command, Children = new List<Expression>() };

                    while (!chain.IsAtEnd)
                    {
                        TryParseExpression(chain, result.Children);
                        if (chain.hasError) { return; }
                    }

                    target.Add(result);
                }
            }
            else if (chain.Current == TokenType.BraceOpen)
            {
                chain.Advance();

                if (chain.Current == TokenType.BraceClose)
                {
                    target.Add(new Expression() { Type = ExpressionType.Literal, Value = new object[0] });
                    chain.Advance();
                    return;
                }

                bool closed = false;
                List<Expression> elements = new();

                while (!chain.IsAtEnd)
                {
                    TryParseExpression(chain, elements);
                    if (chain.hasError) { return; }

                    Expression expr = elements[elements.Count - 1];
                    if (expr.Type != ExpressionType.Literal && expr.Type != ExpressionType.Variable) { chain.SetError("Unexpected expression in array."); return; }

                    if (chain.Current == TokenType.Comma) { chain.Advance(); }
                    else if (chain.Current == TokenType.BraceClose) { chain.Advance(); closed = true; break; }
                    else { chain.SetError("Unexpected token in array."); return; }
                }

                if (!closed) { chain.SetError("Non terminated array"); return; }

                bool singleType = true;
                for (int i = 1; i < elements.Count; i++)
                {
                    if (elements[i].GetType() != elements[i - 1].GetType()) { singleType = false; break; }
                }

                if (singleType)
                {
                    Array array = Array.CreateInstance(elements[0].Value == null ? typeof(object) : elements[0].Value.GetType(), elements.Count);
                    for (int i = 0; i < elements.Count; i++)
                    {
                        array.SetValue(elements[i].Value, i);
                    }
                    target.Add(new Expression() { Type = ExpressionType.Literal, Value = array });
                }
                else
                {
                    object[] array = new object[elements.Count];
                    for(int i = 0; i < elements.Count; i++)
                    {
                        array[i] = elements[i].Value;
                    }
                    target.Add(new Expression() { Type = ExpressionType.Literal, Value = array });
                }
            }
            else if (chain.Current == TokenType.ParenthesisOpen && chain.Next == TokenType.Identifier)
            {
                chain.Advance();
                bool closed = false;
                Expression result = new Expression() { Type = ExpressionType.Command, Children = new List<Expression>() };

                while (!chain.IsAtEnd)
                {
                    TryParseExpression(chain, result.Children);
                    if (chain.hasError) { return; }
                    if (chain.Current == TokenType.ParenthesisClose) { chain.Advance(); closed = true; break; }
                }

                if (!closed) { chain.SetError("Non terminated subcommand."); return; }

                target.Add(result);
            }
            else if (chain.Current == TokenType.ParenthesisOpen)
            {
                chain.Advance();

                if (chain.Current == TokenType.ParenthesisClose) { chain.SetError("Empty vector."); return; }

                bool closed = false;
                List<Expression> elements = new();

                while (!chain.IsAtEnd)
                {
                    TryParseExpression(chain, elements);
                    if (chain.hasError) { return; }

                    Expression expr = elements[elements.Count - 1];
                    if (expr.Type != ExpressionType.Literal && expr.Type != ExpressionType.Variable) { chain.SetError("Unexpected expression in vector."); return; }

                    if (chain.Current == TokenType.Comma) { chain.Advance(); }
                    else if (chain.Current == TokenType.ParenthesisClose) { chain.Advance(); closed = true; break; }
                    else { chain.SetError("Unexpected token in vector."); return; }
                }

                if (!closed) { chain.SetError("Non terminated vector"); return; }

                for (int i = 1; i < elements.Count; i++)
                {
                    if (elements[i].GetType() != elements[i - 1].GetType()) { chain.SetError("Different types within a vector"); return; }
                }

                Type vectorType = elements[0].Value.GetType();
                if (elements.Count == 2)
                {
                    if (vectorType == typeof(float))
                    {
                        object value = new Vector2((float)elements[0].Value, (float)elements[1].Value);
                        target.Add(new Expression() { Type = ExpressionType.Literal, Value = value });
                        return;
                    }
                    else if (vectorType == typeof(int))
                    {
                        object value = new Vector2Int((int)elements[0].Value, (int)elements[1].Value);
                        target.Add(new Expression() { Type = ExpressionType.Literal, Value = value });
                        return;
                    }
                }
                else if (elements.Count == 3)
                {
                    if (vectorType == typeof(float))
                    {
                        object value = new Vector3((float)elements[0].Value, (float)elements[1].Value, (float)elements[2].Value);
                        target.Add(new Expression() { Type = ExpressionType.Literal, Value = value });
                        return;
                    }
                    else if (vectorType == typeof(int))
                    {
                        object value = new Vector3Int((int)elements[0].Value, (int)elements[1].Value, (int)elements[2].Value);
                        target.Add(new Expression() { Type = ExpressionType.Literal, Value = value });
                        return;
                    }
                }
                else if (elements.Count == 4)
                {
                    if (vectorType == typeof(float))
                    {
                        object value = new Vector4((float)elements[0].Value, (float)elements[1].Value, (float)elements[2].Value, (float)elements[3].Value);
                        target.Add(new Expression() { Type = ExpressionType.Literal, Value = value });
                        return;
                    }
                }
                chain.SetError("Vector type not recognized."); return;
            }
            else if (chain.Current == TokenType.Integer || chain.Current == TokenType.Float || chain.Current == TokenType.String)
            {
                Expression expr = new Expression() { Type = ExpressionType.Literal, Value = chain.CurrentValue };
                chain.Advance();
                target.Add(expr);
            }
            else
            {
                chain.SetError(string.Format("Invalid token found: {0}", chain.Current)); return;
            }
        }

        

        private void ParseToken(InputString str)
        {
            if (str.IsAtEnd || str.hasError) { return; }

            str.DiscardWhitespace();

            if (str.IsAtEnd) { return; }

            str.SetEnd();

            if (TryParseString(str)) { return; }
            if (TryParseIdentifier(str)) { return; }
            if (TryParseNumber(str)) { return; }
            if (TryParseSymbol(str)) { return; }

            str.SetError(string.Format("Unkown input or symbol ({0}).", str.Current));
        }

        private bool TryParseIdentifier(InputString str)
        {
            if (!char.IsLetter(str.Current) && str.Current != '_') { return false; }

            while (!str.IsAtEnd)
            {
                if (char.IsLetterOrDigit(str.Current) || str.Current == '_') { str.Advance(); }
                else { break; }
            }

            str.tokens.Add(new Token(TokenType.Identifier, str.lastEnd, str.index, str.str.Substring(str.lastEnd, str.LengthFromLast)));
            return true;
        }

        private bool TryParseString(InputString str)
        {
            if (str.Current != '"') { return false; }

            System.Text.StringBuilder sb = new();
            bool terminated = false;

            while (!str.IsAtEnd)
            {
                str.Advance();

                if (str.Current == '\\')
                {
                    if (str.IsLast) { break; }
                    str.Advance();

                    if (str.Current == '"') { sb.Append('"'); }
                    else if (str.Current == '\\') { sb.Append('\\'); }
                    else if (str.Current == 'n') { sb.Append('\n'); }
                    else if (str.Current == '0') { sb.Append('\0'); }
                    else { sb.Append('\u25a1'); }
                }
                else if (str.Current == '"')
                {
                    terminated = true;
                    str.Advance();
                    break;
                }
                else
                {
                    sb.Append(str.Current);
                }
            }

            if (!terminated) { str.hasError = true; str.error = "Non terminated string."; return true; }
            str.tokens.Add(new Token(TokenType.String, str.lastEnd, str.index, sb.ToString()));
            return true;
        }

        private bool TryParseSymbol(InputString str)
        {
            switch (str.Current)
            {
                case '$':
                    str.tokens.Add(new Token(TokenType.Dollar, str.index, str.index + 1));
                    str.Advance();
                    return true;
                case '(':
                    str.tokens.Add(new Token(TokenType.ParenthesisOpen, str.index, str.index + 1));
                    str.Advance();
                    return true;
                case ')':
                    str.tokens.Add(new Token(TokenType.ParenthesisClose, str.index, str.index + 1));
                    str.Advance();
                    return true;
                case '{':
                    str.tokens.Add(new Token(TokenType.BraceOpen, str.index, str.index + 1));
                    str.Advance();
                    return true;
                case '}':
                    str.tokens.Add(new Token(TokenType.BraceClose, str.index, str.index + 1));
                    str.Advance();
                    return true;
                case '[':
                    str.tokens.Add(new Token(TokenType.BracketOpen, str.index, str.index + 1));
                    str.Advance();
                    return true;
                case ']':
                    str.tokens.Add(new Token(TokenType.BracketClose, str.index, str.index + 1));
                    str.Advance();
                    return true;
                case ',':
                    str.tokens.Add(new Token(TokenType.Comma, str.index, str.index + 1));
                    str.Advance();
                    return true;
                case '.':
                    str.tokens.Add(new Token(TokenType.Dot, str.index, str.index + 1));
                    str.Advance();
                    return true;
                case '-':
                    str.tokens.Add(new Token(TokenType.Minus, str.index, str.index + 1));
                    str.Advance();
                    return true;
                case '=':
                    str.tokens.Add(new Token(TokenType.Equal, str.index, str.index + 1));
                    str.Advance();
                    return true;
                case '#':
                    str.tokens.Add(new Token(TokenType.Hash, str.index, str.index + 1));
                    str.Advance();
                    return true;
                default:
                    return false;
            }
        }

        private bool TryParseNumber(InputString str)
        {
            if (!char.IsDigit(str.Current) && str.Current != '-') { return false; }
            if (str.Current == '-' && !char.IsDigit(str.Next)) { return false; }

            bool dotted = false;
            bool isInt = false;

            if (str.Current == '-') { str.Advance(); }

            while (!str.IsAtEnd)
            {
                if (char.IsDigit(str.Current)) { str.Advance(); }
                else if (str.Current == 'i')
                {
                    if (dotted) { str.SetError("Float marked as integer"); return true; }
                    else { isInt = true; str.Advance(); break; }
                }
                else if(str.Current == '.')
                {
                    if (dotted) { break; }
                    else if (char.IsDigit(str.Next)) { str.Advance(); dotted = true; }
                    else { break; }
                }
                else { break; }
            }

            if (isInt)
            {
                if (int.TryParse(str.str.AsSpan().Slice(str.lastEnd, str.LengthFromLast - 1), out int integer))
                {
                    str.tokens.Add(new Token(TokenType.Integer, str.lastEnd, str.index, integer));
                }
                else
                {
                    str.SetError("Could not parse integer.");
                }
            }
            else
            {
                if (float.TryParse(str.str.AsSpan().Slice(str.lastEnd, str.LengthFromLast), NumberStyles.Number, culture, out float f))
                {
                    str.tokens.Add(new Token(TokenType.Float, str.lastEnd, str.index, f));
                }
                else
                {
                    str.SetError("Could not parse float.");
                }
            }
            
            return true;
        }

        private string GetWord(string source, int position, out bool isVar)
        {
            if (source == "") { isVar = false; return ""; }

            int start = position - 1;
            for (; start >= 0; start--)
            {
                if (!char.IsLetter(source[start])) { break; }
            }
            start++;

            if (start == 0) { isVar = false; }
            else { isVar = source[start - 1] == '$'; }

            return source.Substring(start, position - start);
        }

        public bool AutocompleteSuffix(string source, int cursor, out string newInput, out int newCursor, out string hint)
        {
            hint = null;
            newInput = null;
            newCursor = cursor;
            string word = GetWord(source, cursor, out bool isVar);
            if (word == "") { return false; }

            int s = -1, e = -1;
            for (int i = 0; i < commandNames.Length; i++)
            {
                if (commandNames[i].Length <= word.Length)
                {
                    if (s == -1) { continue; }
                    else { e = i - 1; break; }
                }
                bool match = true;
                for (int j = 0; j < word.Length; j++)
                {
                    if (word[j] != commandNames[i][j]) { match = false; break; }
                }
                if (match) { if (s == -1) { s = i; } }
                else if (s != -1) { e = i - 1; break; }
            }
            
            if (s == -1) { return false; }
            if (e == -1) { e = commandNames.Length - 1; }


            if (s == e)
            {
                newInput = source.Insert(cursor, commandNames[s].Substring(word.Length));
                newCursor = cursor + commandNames[s].Length - word.Length;
            }
            else
            {
                int k = commandNames[0].Length;
                string currentWord = commandNames[s];
                for(int i = s + 1; i <= e; i++)
                {
                    for (int j = word.Length; j < commandNames[i].Length && j < k; j++)
                    {
                        if (commandNames[i][j] != commandNames[i - 1][j]) { k = j; break; }
                    }
                }

                if (k != word.Length)
                {
                    newInput = source.Insert(cursor, commandNames[s].Substring(word.Length, k - word.Length));
                    newCursor = cursor + k - word.Length;
                }

                System.Text.StringBuilder sb = new();
                for (int i = s; i <= e; i++)
                {
                    sb.Append(commandNames[i]);
                    sb.Append(' ');
                }
                sb.Remove(sb.Length - 1, 1);
                hint = sb.ToString();
            }

            return true;
        }

        public bool AutocompleteInfix(string source, int cursor, out string newInput, out int newCursor, out string hint)
        {
            hint = null;
            newInput = null;
            newCursor = cursor;
            string word = GetWord(source, cursor, out bool isVar);
            if (word.Length <= 1) { return false; }

            System.Text.StringBuilder sb = new();
            int i = 0;
            string command = null;

            foreach (string c in commandNames)
            {
                if (c.Contains(word)) { sb.Append(c).Append(' '); i++; command = c; }
            }

            if (i == 0)
            {
                return false;
            }
            else if (i == 1)
            {
                sb.Clear();
                sb.Append(source);
                sb.Remove(cursor - word.Length, word.Length);
                sb.Insert(cursor - word.Length, command);
                newInput = sb.ToString();
                newCursor = cursor - word.Length + command.Length;
                return true;
            }
            else
            {
                sb.Remove(sb.Length - 1, 1);
                hint = sb.ToString();
                return true;
            }
        }

        [ConsoleCommand()]
        public void SetVar(string name, object obj)
        {
            StoredVars[name] = obj;
        }

        [ConsoleCommand()]
        public void DelVar(string name)
        {
            StoredVars.Remove(name);
        }

        [ConsoleCommand()]
        public void ClearVars()
        {
            StoredVars.Clear();
        }

        [ConsoleCommand()]
        public void ListVars()
        {
            System.Text.StringBuilder sb = new();
            foreach (var v in StoredVars)
            {
                sb.AppendFormat("{0} ({1}), ", v.Key, v.Value.GetType());
            }

            if (sb.Length == 0) { Debug.Log("No vars stored."); }
            else
            {
                sb.Remove(sb.Length - 2, 2);
                Debug.Log(sb.ToString());
            }
        }

        [ConsoleCommand()]
        public void Help()
        {
            Debug.Log("help \"commandname\"\ncommands");
        }

        [ConsoleCommand()]
        public void Commands()
        {
            System.Text.StringBuilder sb = new();
            foreach (var s in commandNames)
            {
                sb.Append(s).Append(", ");
            }
            sb.Remove(sb.Length - 2, 2);
            Debug.Log(sb.ToString());
        }

        [ConsoleCommand()]
        public void Help(string command)
        {
            if (!commands.ContainsKey(command)) { Debug.LogFormat("Unknown command: {0}", command); return; }
            foreach (var cmd in commands[command])
            {
                Debug.Log(cmd.GetFullDescription());
            }
        }
    }
}