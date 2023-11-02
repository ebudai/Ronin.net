using System;
using System.Collections;
using System.Collections.Generic;

namespace Ronin.Utility;

internal class ArrayIndexPermutations : IEnumerable<Index[]>
{
    public IEnumerator<Index[]> GetEnumerator() => enumerator;

    IEnumerator IEnumerable.GetEnumerator() => enumerator;

    public void Add(Index[] indices) => enumerator.Add(indices);

    public sealed class Enumerator : IEnumerator<Index[]>
    {
        public void Add(Index[] array)
        {
            arrays.Add(array);
            currentindices = new int[arrays.Count];
        }

        public Index[] Current
        {
            get
            {
                var number = new Index[arrays.Count];
                for (var i = 0; i != arrays.Count; ++i)
                {
                    number[i] = arrays[i][currentindices[i]];
                }
                return number;
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext()
        {
            for (int i = arrays.Count - 1; i != 0; --i)
            {
                ++currentindices[i];
                if (currentindices[i] < arrays[i].Length) return true;
                currentindices[i] = 0;
            }
            return false;
        }

        public void Reset()
        {
            for (int i = 0; i != currentindices.Length - 1; ++i)
            {
                currentindices[i] = 0;
            }
        }

        private readonly List<Index[]> arrays = new();
        private int[] currentindices;
    }

    private readonly Enumerator enumerator = new();
}