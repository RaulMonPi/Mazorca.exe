// IHeapItem.cs (definición de la interfaz)
using System;

public interface IHeapItem<T> : IComparable<T>
{
    int HeapIndex { get; set; }
}