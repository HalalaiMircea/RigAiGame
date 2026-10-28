using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using SemanticChunkerNET;

namespace RigAiGame;

public class DocumentIngestionService
{
    private readonly VectorStore _vectorStore;
    private readonly SemanticChunker _semanticChunker;

    public DocumentIngestionService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        VectorStore vectorStore)
    {
        _vectorStore = vectorStore;
        _semanticChunker = new SemanticChunker(embeddingGenerator, tokenLimit: 512, bufferSize: 2, thresholdAmount: 92);
    }

    public async Task ProcessDirectoryAsync(string folderPath)
    {
        // 1. Grab both .txt and .md files
        var allowedExtensions = new[] {".txt", ".md"};
        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLower()));

        var collection = _vectorStore.GetCollection<string, DocumentChunk>("documents");
        //TODO TEMP: just regenerating it every time...we will do proper updating later
        await collection.EnsureCollectionDeletedAsync();
        //
        await collection.EnsureCollectionExistsAsync();

        foreach (var filePath in files)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            string rawContent = await File.ReadAllTextAsync(filePath);

            // 2. Dynamic handling based on type if needed
            if (extension == ".md")
            {
                // Optional: You can do light Markdown formatting adjustments here,
                // but usually, letting the semantic chunker ingest the raw markdown text 
                // works best as it respects paragraph breaks (\n\n).
            }

            // 3. Let the Semantic Chunker dynamically slice it based on context
            //TODO: Improve the chunking??? In SQLite table i can see some anomalies like just a 5. text chunk... instead of the whole bullet point
            // URLs are chopped at the ? query symbol, the chunker thinks it's a question
            IList<Chunk> semanticChunks = await _semanticChunker.CreateChunksAsync(rawContent);

            Console.WriteLine($"Processed {Path.GetFileName(filePath)} into {semanticChunks.Count} semantic chunks.");

            // Map to your MEVD POCOs and save to SQLite here...
            var mappedChunks = semanticChunks.Select((chunk, index) => new DocumentChunk
            {
                Id = chunk.Id,
                FileName = Path.GetRelativePath(folderPath, filePath),
                ChunkIndex = index,
                Text = chunk.Text,
                TextEmbedding = chunk.Embedding.Vector
            });

            await collection.UpsertAsync(mappedChunks);
        }
    }
}
//TODO: On file updated, delete all the records for that fileName and rebuild them
// File is updated when the lastUpdateDate from the vector record is older than the file modified date info.
/*
 * graph TD
    A[File Updated] --> B[1. Delete all rows WHERE FileName == 'manual.txt']
    B --> C[2. Run Semantic Chunker on new text]
    C --> D[3. Insert new chunks fresh with Id = fileName_chunkIndex]
 */
