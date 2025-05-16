using Microsoft.Extensions.VectorData;
using System;

namespace SemanticKernelPlayground.Plugins.Models
{
    public class TextChunk
    {
        [VectorStoreRecordKey]
        public string Key { get; set; } = "";

        public string Text { get; set; } = "";

        [VectorStoreRecordVector(1536)]
        public ReadOnlyMemory<float> TextEmbedding { get; set; }
    }
}
