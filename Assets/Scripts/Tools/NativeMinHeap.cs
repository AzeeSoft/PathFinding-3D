using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;

public struct NativeMinHeap<T> where T : struct
{
    public struct HeapNode<TU> where TU : struct
    {
        public TU Value;
        public float Cost;
    }

    private NativeList<HeapNode<T>> heap;
    private int heapSize;

    #region Interface

    public NativeMinHeap(Allocator allocator)
    {
        heap = new NativeList<HeapNode<T>>(allocator);
        heapSize = 0;
    }

    public void Push(T t, float cost)
    {
        HeapNode<T> node;
        node.Value = t;
        node.Cost = cost;

        if (heapSize >= heap.Length)
        {
            heap.Add(node);
        }
        else
        {
            heap[heapSize] = node;
        }
        heapSize++;

        int i = heapSize - 1;
        while (i != 0 && heap[GetParentIndex(i)].Cost > heap[i].Cost)
        {
            Swap(i, GetParentIndex(i));
            i = GetParentIndex(i);
        }
    }

    public T Pop()
    {
        if (heapSize <= 0)
        {
            return default;
        }

        T val = heap[0].Value;

        if (heapSize == 1)
        {
            heapSize--;
            return val;
        }

        heap[0] = heap[heapSize - 1];
        heapSize--;
        MinHeapify(0);

        return val;
    }

    public int Size()
    {
        return heapSize;
    }

    public void Dispose()
    {
        heap.Dispose();
    }

    #endregion


    #region Implementation

    private int GetParentIndex(int index)
    {
        return (index - 1) / 2;
    }

    private int GetLeftChildIndex(int index)
    {
        return (2 * index) + 1;
    }

    private int GetRightChildIndex(int index)
    {
        return (2 * index) + 2;
    }

    private void Swap(int i, int j)
    {
        HeapNode<T> tmp = heap[i];
        heap[i] = heap[j];
        heap[j] = tmp;
    }

    private void MinHeapify(int index)
    {
        int l = GetLeftChildIndex(index);
        int r = GetRightChildIndex(index);
        int smallest = index;

        if (l < heapSize && heap[l].Cost < heap[index].Cost)
        {
            smallest = l;
        }

        if (r < heapSize && heap[r].Cost < heap[smallest].Cost)
        {
            smallest = r;
        }

        if (smallest != index)
        {
            Swap(index, smallest);
            MinHeapify(smallest);
        }
    }

    #endregion
}