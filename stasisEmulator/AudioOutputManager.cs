using Microsoft.Xna.Framework.Audio;
using System;

namespace stasisEmulator
{
    public class AudioOutputManager
    {
        private float _playbackSpeed = 1;
        public float PlaybackSpeed 
        { 
            get => _playbackSpeed;
            set
            {
                _playbackSpeed = value;
                CreateBuffers();
            }
        }

        private const float MinPlaybackSpeed = 0.25f;
        private const float MaxPlaybackSpeed = 4;

        private readonly DynamicSoundEffectInstance _soundEffectInstance;

        private readonly int _sampleRate;
        private readonly AudioChannels _audioChannels;
        private readonly int _bufferSubmitRate;
        private readonly int _loadBufferSize;

        private byte[] _outputBuffer;
        private short[] _workingBuffer;
        private short[] _loadBuffer;
        private int _loadBufferIndex = 0;
        private float _samplesToSubmit = 0;

        public AudioOutputManager(int sampleRate, AudioChannels audioChannels, int bufferSubmitRate, int loadBufferSize)
        {
            _soundEffectInstance = new(sampleRate, audioChannels);
            _soundEffectInstance.Play();

            _sampleRate = sampleRate;
            _audioChannels = audioChannels;
            _bufferSubmitRate = bufferSubmitRate;
            _loadBufferSize = loadBufferSize;

            CreateBuffers();
        }

        private void CreateBuffers()
        {
            int bufferLength = (int)Math.Ceiling((float)_sampleRate / _bufferSubmitRate / Math.Min(_playbackSpeed, 1)) * (_audioChannels == AudioChannels.Stereo ? 2 : 1);

            _loadBuffer = new short[_loadBufferSize];
            _workingBuffer = new short[bufferLength];
            _outputBuffer = new byte[bufferLength * 2];
        }

        private static void Resample(short[] sourceBuffer, int sourceStartIndex, int sourceSampleCount, short[] outputBuffer, int outputStartIndex, int outputSampleCount)
        {
            sourceSampleCount = Math.Min(sourceSampleCount, sourceBuffer.Length - sourceStartIndex);
            outputSampleCount = Math.Min(outputSampleCount, outputBuffer.Length - outputStartIndex);

            float sourceSamplesPerOutputSample = sourceSampleCount / (float)outputSampleCount;
            float sourceSamplePosition = sourceStartIndex;

            for (int i = outputStartIndex; i < outputStartIndex + outputSampleCount; i++)
            {
                int sourceSampleIndex = (int)sourceSamplePosition;
                sourceSampleIndex = Math.Clamp(sourceSampleIndex, 0, sourceBuffer.Length - 1);

                outputBuffer[i] = sourceBuffer[sourceSampleIndex];
                sourceSamplePosition += sourceSamplesPerOutputSample;
            }
        }

        private static void ToByteBuffer(short[] shortBuffer, byte[] outputBuffer)
        {
            if (outputBuffer.Length != shortBuffer.Length * 2)
                throw new Exception("Incorrect buffer size!");

            for (int i = 0; i < shortBuffer.Length; i++)
            {
                short shortSample = shortBuffer[i];

                if (!BitConverter.IsLittleEndian)
                {
                    outputBuffer[i * 2] = (byte)(shortSample >> 8);
                    outputBuffer[i * 2 + 1] = (byte)shortSample;
                }
                else
                {
                    outputBuffer[i * 2] = (byte)shortSample;
                    outputBuffer[i * 2 + 1] = (byte)(shortSample >> 8);
                }
            }
        }

        public void SubmitSample(short sample)
        {
            _samplesToSubmit += 1 / Math.Min(_playbackSpeed, 1);
            while (_samplesToSubmit > 0)
            {
                if (_loadBufferIndex >= _loadBuffer.Length)
                {
                    _samplesToSubmit -= (float)Math.Ceiling(_samplesToSubmit);
                    break;
                }

                _loadBuffer[_loadBufferIndex++] = sample;
                _samplesToSubmit--;
            }
        }

        public void SubmitBuffer()
        {
            Resample(_loadBuffer, 0, _loadBufferIndex, _workingBuffer, 0, _workingBuffer.Length);
            ToByteBuffer(_workingBuffer, _outputBuffer);
            if (_soundEffectInstance.PendingBufferCount < 3 && _playbackSpeed >= MinPlaybackSpeed && _playbackSpeed <= MaxPlaybackSpeed)
                _soundEffectInstance.SubmitBuffer(_outputBuffer);
            _loadBufferIndex = 0;
        }
    }
}
