using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace soundBoardTorb {
	class CachedSound {
		public float[] AudioData { get; private set; }
		public WaveFormat WaveFormat { get; private set; }
		public CachedSound(string audioFileName) {
			using (var audioFileReader = new AudioFileReader(audioFileName)) {
				// TODO: could add resampling in here if required
				int outRate = 16000;
				var outFormat = new WaveFormat(outRate, audioFileReader.WaveFormat.Channels);

				using (var resampler = new MediaFoundationResampler(audioFileReader, outFormat)) {

					WaveFormat = audioFileReader.WaveFormat;
					var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
					var readBuffer = new float[WaveFormat.SampleRate * WaveFormat.Channels];
					int samplesRead;
					while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0) {
						wholeFile.AddRange(readBuffer.Take(samplesRead));
					}

					AudioData = wholeFile.ToArray();
				}
			}
		}
	}
}
