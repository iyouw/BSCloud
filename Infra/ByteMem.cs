using System.IO;

namespace mem
{
  public class ByteMem : Mem<byte>
  {

    public static ByteMem FromStream(Stream stream)
    {
      var res = new ByteMem(stream.Length);
      int i = 0, bt = 0;
      while((bt = stream.ReadByte()) != -1)
      {
        res[i++] = (byte)bt;
      }
      return res;
    }

    public ByteMem(long length) : this(length, false)
    {
    }

    public ByteMem(long length, bool init) : base(length, init)
    {
    }


    public void CopyToStream(Stream stream)
    {
      for(var i = 0; i< Length; i++)
      {
        stream.WriteByte(this[i]);
      }
    }
  }
}