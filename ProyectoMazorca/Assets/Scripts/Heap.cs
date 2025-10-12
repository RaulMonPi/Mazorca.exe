using System;

public class Heap<T> where T : IComparable<T>
{
    T[] items;
    int currentItemCount;

    public Heap(int maxHeapSize)
    {
        items = new T[maxHeapSize];
    }

    public void Add(T item)
    {
        items[currentItemCount] = item;
        SortUp(currentItemCount);
        currentItemCount++;
    }

    public T RemoveFirst()
    {
        if (currentItemCount == 0) throw new InvalidOperationException("Heap empty");
        T firstItem = items[0];
        currentItemCount--;
        items[0] = items[currentItemCount];
        items[currentItemCount] = default(T);
        SortDown(0);
        return firstItem;
    }

    public void UpdateItem(T item)
    {
        // Asumimos que el item cambió su prioridad a menor (sube en el heap)
        int index = Array.IndexOf(items, item, 0, currentItemCount);
        if (index >= 0) SortUp(index);
    }

    public int Count => currentItemCount;

    bool Contains(T item)
    {
        return Array.IndexOf(items, item, 0, currentItemCount) >= 0;
    }

    void SortUp(int index)
    {
        int parentIndex = (index - 1) / 2;

        while (index > 0)
        {
            if (items[index].CompareTo(items[parentIndex]) > 0)
            {
                Swap(index, parentIndex);
                index = parentIndex;
                parentIndex = (index - 1) / 2;
            }
            else break;
        }
    }

    void SortDown(int index)
    {
        while (true)
        {
            int leftChild = index * 2 + 1;
            int rightChild = leftChild + 1;
            int swapIndex = -1;

            if (leftChild < currentItemCount)
            {
                swapIndex = leftChild;

                if (rightChild < currentItemCount)
                {
                    if (items[rightChild].CompareTo(items[leftChild]) > 0)
                        swapIndex = rightChild;
                }

                if (items[swapIndex].CompareTo(items[index]) > 0)
                    Swap(swapIndex, index);
                else
                    return;
                index = swapIndex;
            }
            else
            {
                return;
            }
        }
    }

    void Swap(int a, int b)
    {
        T tmp = items[a];
        items[a] = items[b];
        items[b] = tmp;
    }
}
