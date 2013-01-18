using System;
using BTDB.IL;

namespace BTDB.EventStoreLayer
{
    public static class TypeDescriptorExtensions
    {
        public static void GenerateSave(this ITypeDescriptor typeDescriptor, IILGen ilGenerator, Action<IILGen> pushWriter, Action<IILGen> pushCtx, Action<IILGen> pushSubValue)
        {
            if (typeDescriptor.StoredInline)
            {
                var generator = typeDescriptor.BuildBinarySerializerGenerator();
                generator.GenerateSave(ilGenerator, pushWriter, pushCtx, pushSubValue);
            }
            else
            {
                ilGenerator
                    .Do(pushCtx)
                    .Do(pushSubValue)
                    .Callvirt(() => default(ITypeBinarySerializerContext).StoreObject(null));
            }
        }

        public static void GenerateSkip(this ITypeDescriptor itemDescriptor, IILGen ilGenerator, Action<IILGen> pushReader, Action<IILGen> pushCtx)
        {
            if (itemDescriptor.StoredInline)
            {
                var skipper = itemDescriptor.BuildBinarySkipperGenerator();
                skipper.GenerateSkip(ilGenerator, pushReader, pushCtx);
            }
            else
            {
                ilGenerator
                    .Do(pushCtx)
                    .Callvirt(() => default(ITypeBinaryDeserializerContext).SkipObject());
            }
        }

        public static void GenerateLoad(this ITypeDescriptor dictionaryTypeDescriptor, IILGen ilGenerator, Action<IILGen> pushReader, Action<IILGen> pushCtx, Type asType)
        {
            if (dictionaryTypeDescriptor.StoredInline)
            {
                var des = dictionaryTypeDescriptor.BuildBinaryDeserializerGenerator(asType);
                des.GenerateLoad(ilGenerator, pushReader, pushCtx);
            }
            else
            {
                ilGenerator
                    .Do(pushCtx)
                    .Callvirt(() => default(ITypeBinaryDeserializerContext).LoadObject());
                if (asType != typeof(object))
                    ilGenerator.Castclass(asType);
            }
        }
    }
}