using Microsoft.Extensions.VectorData;
#nullable disable

namespace RigAiGame;

public class DocumentChunk
{
    [VectorStoreKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [VectorStoreData]
    public string FileName { get; set; }

    [VectorStoreData]
    public int ChunkIndex { get; set; }

    [VectorStoreData]
    public string Text { get; set; }

    [VectorStoreVector(4096, DistanceFunction = DistanceFunction.CosineDistance, IndexKind = IndexKind.QuantizedFlat)]
    public ReadOnlyMemory<float> TextEmbedding { get; set; }
}
//qwen3-embedding:8b
