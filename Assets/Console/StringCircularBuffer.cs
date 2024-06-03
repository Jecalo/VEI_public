using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace UConsole
{
    public class StringCircularBuffer
    {
        public int MaxStringLenth { get; private set; }
        public int Capacity { get; private set; }
        public ulong Counter { get; private set; }

        private StringBuilder[] buffer;
        private int index;
        private int lastIndex;

        public StringCircularBuffer(int maxStringLenth, int capacity)
        {
            Capacity = capacity;
            MaxStringLenth = maxStringLenth;
            Counter = 0;

            buffer = new StringBuilder[Capacity];

            for (int i = 0; i < Capacity; i++)
            {
                buffer[i] = new StringBuilder(maxStringLenth);
            }

            index = 0;
            lastIndex = Capacity - 1;
        }

        public void SetCapacity(int newCapacity)
        {
            if (newCapacity <= 0) { return; }
            if (newCapacity == Capacity) { return; }

            StringBuilder[] newBuffer = new StringBuilder[newCapacity];

            int i = index;
            int newIndex = Capacity;
            if (newCapacity < Capacity)
            {
                i += Capacity - newCapacity;
                newIndex -= Capacity - newCapacity;
                if (i >= Capacity) { i -= Capacity; }
            }

            System.Array.Copy(buffer, i, newBuffer, 0, Capacity - i);
            System.Array.Copy(buffer, 0, newBuffer, Capacity - i, i);

            index = newIndex;
            Capacity = newCapacity;
            buffer = newBuffer;
            lastIndex = index--;
            if (lastIndex < 0) { lastIndex = Capacity - 1; }
        }

        public void SetMaxLength(int newMax)
        {
            if (newMax <= 0) { return; }
            if (newMax == MaxStringLenth) { return; }

            for (int i = 0; i < Capacity; i++)
            {
                if (buffer[i].Length > newMax) { buffer[i].Length = newMax; }
                buffer[i].Capacity = Capacity;
            }

            MaxStringLenth = newMax;
        }

        public string Get()
        {
            return buffer[lastIndex].ToString();
        }

        public string Get(int offset)
        {
            int c = (int)Counter - 1;
            if (offset > c) { offset = c; }
            if (offset < 0) { offset = 0; }

            int i = lastIndex - offset;
            if (i < 0) { i += Capacity; }

            return buffer[i].ToString();
        }

        public string GetOldest()
        {
            return buffer[index].ToString();
        }

        public void Add(string str)
        {
            if (str == null) { return; }
            int l = str.Length;
            if (l > MaxStringLenth) { l = MaxStringLenth; }

            buffer[index].Clear();
            buffer[index].Append(str, 0, l);

            lastIndex = index;
            index++;
            if (index >= Capacity) { index = 0; }
            Counter++;
        }

        public void Clear()
        {
            for (int i = 0; i < Capacity; i++)
            {
                buffer[i].Clear();
            }

            index = 0;
            lastIndex = Capacity - 1;
            Counter = 0;
        }

        public void CombineStrings(StringBuilder sb, string separator)
        {
            if (sb == null) { return; }
            if (separator == null) { separator = ""; }

            sb.Clear();

            for (int i = index; i < Capacity; i++)
            {
                if (buffer[i].Length == 0) { continue; }
                sb.Append(buffer[i]);
                sb.Append(separator);
            }

            for (int i = 0; i < index; i++)
            {
                if (buffer[i].Length == 0) { continue; }
                sb.Append(buffer[i]);
                sb.Append(separator);
            }
        }
    }

}