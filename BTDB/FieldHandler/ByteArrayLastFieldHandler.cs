using System;
using BTDB.IL;
using BTDB.StreamLayer;

namespace BTDB.FieldHandler
{
    public class ByteArrayLastFieldHandler : SimpleFieldHandlerBase
    {
        public ByteArrayLastFieldHandler()
            : base(
                EmitHelpers.GetMethodInfo(() => ((AbstractBufferedReader)null).ReadByteArrayRawTillEof()),
                null,
                EmitHelpers.GetMethodInfo(() => ((AbstractBufferedWriter)null).WriteByteArrayRaw(null)))
        {
        }

        public override string Name
        {
            get { return "Byte[]Last"; }
        }

        public override bool IsCompatibleWith(Type type, FieldHandlerOptions options)
        {
            if (!options.HasFlag(FieldHandlerOptions.AtEndOfStream)) return false;
            return base.IsCompatibleWith(type, options);
        }

    }
}