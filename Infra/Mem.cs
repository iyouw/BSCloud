using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace mem
{
  public class Mem<T> : IEnumerable<T>, IDisposable
    where T : unmanaged
  {

    private bool _disposed = false;
    private IntPtr _native;
    private long _length;

    public long Length => _length;

    public long Position { get; set; } 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Mem(long length)
      :this(length, false)
    {

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Mem(long length, bool init)
    {
      var size = sizeof(T) * length;
      _native = Marshal.AllocHGlobal(new IntPtr(size));
      _length = length;
      Position = 0;
      InitMemory();

      void InitMemory()
      {
        if(!init)
        {
          return;
        }
        byte* pointer = (byte*)_native.ToPointer();
        for(var i = 0; i < size; i++)
        {
          pointer[i] = 0;
        }
      }
    }

    public unsafe T this[long index]
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => ((T*)_native)[index];
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => ((T*)_native)[index] = value;
    }

    public void Read(byte[] buffer, long offset, long count)
    {

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void Dispose(bool disposing)
    {
      if(_disposed)
      {
        return;
      }

      if(disposing)
      {
        Marshal.FreeHGlobal(_native);
        _native = IntPtr.Zero;
        _length = 0;
      }

      _disposed = true;
    }

    public IEnumerator<T> GetEnumerator()
    {
      ThrowIfDisposed();
      return new EnumeratorClass(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      ThrowIfDisposed();
      return new EnumeratorClass(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
      if(_disposed) 
      { 
        throw new ObjectDisposedException(nameof(Mem<T>), "Memory of array is already free."); 
      }
    }

    public unsafe class EnumeratorClass : IEnumerator<T>, IEnumerator
    {
      private readonly T* _ptr;
      private readonly long _len;
      private long _index;

      internal EnumeratorClass(Mem<T> mem)
      {
        _ptr = (T*)mem._native;
        _len = mem._length;
        _index = 0;
        Current = default;
      }

      public T Current { get; private set; }

      object IEnumerator.Current => Current;

      public void Dispose()
      {
        
      }

      public bool MoveNext()
      {
        if((ulong)_index < (ulong)_len)
        {
          Current = _ptr[_index++];
          return true;
        }
        else
        {
          _index = _len + 1;
          Current = default;
          return false;
        }
      }

      public void Reset()
      {
        _index = 0;
        Current = default;
      }
    }
  }
}