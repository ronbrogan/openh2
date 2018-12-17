using OpenH2.Core.Factories;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OpenH2.Core.Meta;
using Xunit;
using Xunit.Abstractions;

namespace OpenH2.Core.Tests
{
    public class SceneFactoryTests
    {
        private readonly ITestOutputHelper output;

        public SceneFactoryTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Load_scene_from_file()
        {
            var mapStream = new FileStream(@"D:\Halo 2 Vista Original Maps\ascension.map", FileMode.Open, FileAccess.Read, FileShare.Read);

            var factory = new SceneFactory();

            var sw = new Stopwatch();
            sw.Restart();

            var scene = factory.FromFile(mapStream, out var coverage);
            mapStream.Dispose();
            sw.Stop();

            Assert.NotNull(scene);
            Assert.Equal("ascension", scene.Name);
            Assert.Equal(8, scene.Header.Version);
            Assert.Equal(16059904, scene.Header.TotalBytes);
            Assert.Equal(14503424, scene.Header.IndexOffset.Value);
            Assert.Equal(245760, scene.Header.MetaOffset.OriginalValue);
            Assert.Equal(14503424, scene.PrimaryMagic);
            Assert.Equal(6468096, scene.SecondaryMagic);

            Assert.Equal(scene.IndexHeader.ObjectCount, scene.ObjectIndex.Count);

            scene.TagTree.Print(s => Debug.WriteLine(s));

            var scnr = scene.ObjectMeta.Values.First(v => v.GetType() == typeof(ScenarioMeta)) as ScenarioMeta;

            File.WriteAllBytes("D:\\scnr.meta", scnr.RawMeta);

            output.WriteLine($"Scene parsing took: {sw.ElapsedMilliseconds}ms and covered: {coverage.PercentCovered.ToString("0.00")}%");
        }

        [Fact]
        public void Calculated_signature_matches_stored_signature()
        {
            var mapStream = new FileStream("ascension.map", FileMode.Open, FileAccess.Read, FileShare.Read);

            var factory = new SceneFactory();

            var scene = factory.FromFile(mapStream);

            mapStream.Dispose();

            var sig = scene.CalculateSignature();

            Assert.Equal(scene.Header.StoredSignature, sig);
        }

        
    }
}