using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VideoGame.Editor
{
    public sealed class StreamingAudioImporter : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Contains("Resources/Audio/")) return;
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .75f;
            settings.preloadAudioData = false; importer.defaultSampleSettings = settings;
        }
    }
}
