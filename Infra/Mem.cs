using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections;

namespace BSCloud.Infra
{
   public class Mem<T> : IEnumerable<T>, IDisposable
    where T : unmanaged
  {

    public static Mem<byte> FromStream(Stream stream)
    {
      var res = new Mem<byte>(stream.Length);
      int i = 0, bt = 0;
      while( (bt = stream.ReadByte()) != -1)
      {
        res[i++] = (byte)bt;
      }
      return res;
    }

    private bool _disposed = false;
    private IntPtr _native;
    private long _length;
    private long _size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Mem(long length)
      :this(length, false)
    {

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Mem(long length, bool init)
    {
      _size = sizeof(T) * length;
      _native = Marshal.AllocHGlobal(new IntPtr(_size));
      _length = length;
      InitMemory();

      void InitMemory()
      {
        if(!init)
        {
          return;
        }
        byte* pointer = (byte*)_native.ToPointer();
        for(var i = 0; i < _size; i++)
        {
          pointer[i] = 0;
        }
      }
    }

    public unsafe T this[int index]
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => ((T*)_native)[index];
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => ((T*)_native)[index] = value;
    }

    public long Length => _length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void CopyToStream(Stream stream)
    {
      for(var i = 0; i< _size; i++)
      {
        stream.WriteByte(((byte*)_native)[i]);
      }
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
      throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      throw new NotImplementedException();
    }
  }
}