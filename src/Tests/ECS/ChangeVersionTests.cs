using System.Numerics;
using Friflo.Engine.ECS;
using NUnit.Framework;

namespace Tests.ECS
{
    public class ChangeVersionTests
    {
        [Test]
        public void TestChangeVersion_Basic()
        {
            var store = new EntityStore();
            var entity = store.CreateEntity(new Position(1, 2, 3));
            
            // Check initial version
            var query = store.Query<Position>();
            foreach (var chunks in query.Chunks) {
                Assert.AreEqual(0, chunks.Chunk1.ChangeVersion);
            }

            // Simulate a System run that modifies data
            store.GlobalSystemVersion = 1;
            foreach (var chunks in query.Chunks) {
                // "Mark Changed"
                chunks.Chunk1.SetChangeVersion(store.GlobalSystemVersion);
            }

            // Verify version is updated
            foreach (var chunks in query.Chunks) {
                Assert.AreEqual(1, chunks.Chunk1.ChangeVersion);
            }
        }

        [Test]
        public void TestChangeVersion_MultipleChunks()
        {
            var store = new EntityStore();
            store.GlobalSystemVersion = 100;

            // Create enough entities to potentially split chunks (though Friflo uses Archetype Chunks which are contiguous)
            // But if we have different components, we have different archetypes.
            
            var e1 = store.CreateEntity(new Position(1, 0, 0));
            var e2 = store.CreateEntity(new Position(2, 0, 0), new Rotation()); // Different Archetype

            var query = store.Query<Position>();
            
            // Iterating query should yield different chunks (different archetypes)
            int chunkCount = 0;
            foreach (var chunks in query.Chunks) {
                chunkCount++;
                // Mark only the first archetype/chunk as changed
                if (chunks.Length == 1 && chunks.Chunk1[0].value.X == 1) {
                    chunks.Chunk1.SetChangeVersion(store.GlobalSystemVersion);
                }
            }
            // Note: Depending on iteration order and batching, count might vary, but here distinct archetypes = 2 chunks?
            // Friflo aggregates compatible archetypes. QueryChunks iterates archetype by archetype.
            // So we should have 2 iterations.
            
            // Verify
            foreach (var chunks in query.Chunks) {
               if (chunks.Chunk1[0].value.X == 1) {
                   Assert.AreEqual(100u, chunks.Chunk1.ChangeVersion);
               } else {
                   Assert.AreEqual(0u, chunks.Chunk1.ChangeVersion);
               }
            }
        }
    }
}
