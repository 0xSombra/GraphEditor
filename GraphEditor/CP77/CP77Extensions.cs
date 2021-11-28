using System.Collections.Generic;
using System.Linq;
using WolvenKit.RED4.CR2W;
using WolvenKit.Common.Model.Cr2w;
using CP77Types = WolvenKit.RED4.CR2W.Types;

namespace GraphEditor.CP77
{
    public static class CR2WExtensions
    {
        public static void ClearVariable(this IEditableVariable editableVariable)
        {
            if (editableVariable is IArrayAccessor arrayAccessor)
            {
                foreach (var element in arrayAccessor)
                {
                    (element as IEditableVariable)?.ClearVariable();
                }

                if (!arrayAccessor.IsStatic())
                {
                    editableVariable.IsSerialized = false;
                    arrayAccessor.Clear();
                }
            }
            else if (editableVariable is IHandleAccessor handleAccessor)
            {
                editableVariable.IsSerialized = false;
                handleAccessor.ClearHandle();
            }
            else if (editableVariable is IREDPrimitive)
            {
                editableVariable.IsSerialized = false;
            }
            else
            {
                var members = editableVariable.accessor.GetMembers();
                foreach (var member in members)
                {
                    if (member.Ordinal == -1)
                        continue;

                    (editableVariable.accessor[editableVariable, member.Name] as IEditableVariable)?.ClearVariable();
                }
            }
        }

        public static void SetReference(this IHandleAccessor handleAccessor, ICR2WExport reference)
        {
            if (handleAccessor.ChunkHandle && handleAccessor.Reference != null)
            {
                if (handleAccessor.Reference.ChunkIndex == reference.ChunkIndex)
                    return;
            }

            handleAccessor.ClearHandle();

            handleAccessor.IsSerialized = true;
            handleAccessor.ChunkHandle = true;
            handleAccessor.Reference = reference;
        }

        // a chunk can be referened by multiple handles
        // this takes care of that.
        // clearing a handle will check if the chunk is referenced by any other handle
        // if not, the chunk is deleted. hopefully..
        public static void ClearHandle(this IHandleAccessor handleAccessor, bool deleteChunkIfNoRef = true)
        {
            if (!handleAccessor.ChunkHandle)
            {
                handleAccessor.DepotPath = string.Empty;
                handleAccessor.ClassName = string.Empty;
                handleAccessor.Flags = 0;
            }
            else
            {
                // this must be set to false before Reference.data.ClearVariable()
                handleAccessor.ChunkHandle = false;
                if (handleAccessor.Reference != null)
                {
                    // remove old reverse-lookups
                    handleAccessor.Reference.AdReferences.Remove(handleAccessor);
                    handleAccessor.Cr2wFile.Chunks[handleAccessor.LookUpChunkIndex()].AbReferences.Remove(handleAccessor);

                    if (deleteChunkIfNoRef && handleAccessor.Reference.AdReferences.Count == 0)
                    {
                        var cr2w = (CR2WFile)handleAccessor.Cr2wFile;
                        // this takes care of any children chunks
                        // letting RemoveChunks deal with them is a bad idea
                        // because it doesnt check if another handle is referencing the child chunk
                        handleAccessor.Reference.data.ClearVariable();
                        cr2w.RemoveChunkEx(handleAccessor.Reference, CR2WFile.EChunkDisplayMode.Linear, true);
                    }

                    handleAccessor.Reference = null;
                }
                // back to true! (prevents possible endless recursive ClearHandle)
                handleAccessor.ChunkHandle = true;
            }
        }

        public static ICR2WExport CreateChunkEx(this CR2WFile cr2wFile, string type, ICR2WExport parent = null, ICR2WExport virtualParent = null)
        {
            var chunk = cr2wFile.CreateChunk(type, cr2wFile.GetNextChunkID(), parent, virtualParent);
            chunk.data.VarChunkIndex = chunk.ChunkIndex;
            chunk.data.IsSerialized = true;
            return chunk;
        }

        public static int RemoveChunksEx(this CR2WFile cr2wFile, List<ICR2WExport> chunksToBeRemoved, CR2WFile.EChunkDisplayMode recursionMode, bool purgeReferrers = false)
        {
            var removedChunks = cr2wFile.RemoveChunks(chunksToBeRemoved, false, recursionMode, purgeReferrers);
            if (removedChunks != 0)
            {
                foreach (var chunk in cr2wFile.Chunks)
                {
                    // ??????
                    chunk.data.VarChunkIndex = chunk.ChunkIndex;
                }
            }

            return removedChunks;
        }

        public static int RemoveChunkEx(this CR2WFile cr2wFile, ICR2WExport chunk, CR2WFile.EChunkDisplayMode recursionMode, bool purgeReferrers = false)
        {
            return cr2wFile.RemoveChunksEx(new List<ICR2WExport>(1) { chunk }, recursionMode, purgeReferrers);
        }

        // I don't want to change previously-made-chunks' IDs
        // Is this ok?
        public static int GetNextChunkID(this CR2WFile cr2wFile)
        {
            // 'Chunks' should be ordered
            return cr2wFile.Chunks.Last().ChunkIndex + 1;
        }

        public static bool IsStatic(this IArrayAccessor arrayAccessor)
        {
            var arrayType = arrayAccessor.GetType().GetGenericTypeDefinition();
            return arrayType == typeof(CP77Types.CArrayFixedSize<>) || arrayType == typeof(CP77Types.CStatic<>);
        }

        public static T As<T>(this CR2WFile cr2wFile) where T : class, IEditableVariable
        {
            return cr2wFile.Chunks.Count == 0 ? null : cr2wFile.Chunks[0].data as T;
        }

        public static T GetInstance<T>(this IHandleAccessor handle) where T : class, IEditableVariable
        {
            if (!handle.ChunkHandle)
                return null;

            return handle.Reference?.data as T;
        }

        public static T GetInstance<T>(this CP77Types.wCHandle<T> handle) where T : class, IEditableVariable
        {
            if (!handle.ChunkHandle)
                return null;

            return handle.Reference?.data as T;
        }

        public static T GetInstance<T>(this CP77Types.CHandle<T> handle) where T : class, IEditableVariable
        {
            if (!handle.ChunkHandle)
                return null;

            return handle.Reference?.data as T;
        }
    }
}
