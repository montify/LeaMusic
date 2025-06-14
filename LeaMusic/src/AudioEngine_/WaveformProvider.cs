﻿using NAudio.Wave;

namespace LeaMusic.src.AudioEngine_
{
    public class WaveformProvider
    {
        public float[] waveformBuffer;
        private WaveFormat WaveFormat;

        public WaveformProvider(ISampleProvider sampleProvider, int totalTimeInSec)
        {
            int totalSamples = sampleProvider.WaveFormat.SampleRate * sampleProvider.WaveFormat.Channels * totalTimeInSec;
            waveformBuffer = new float[totalSamples];

            sampleProvider.Read(waveformBuffer, 0, waveformBuffer.Length);
          
            WaveFormat = sampleProvider.WaveFormat;
        }

        public WaveformProvider(float[] waveform, WaveFormat waveFormat)
        {
            waveformBuffer = waveform;
            WaveFormat = waveFormat;
        }


        public Memory<float> RequestSamples(double startInSec, double endInSec, int widthInPixel)
        {
            double startSampleIndex = startInSec * WaveFormat.SampleRate * WaveFormat.Channels;
            double endSampleIndex = endInSec * WaveFormat.SampleRate * WaveFormat.Channels;


            double totalSamplesInRange = endSampleIndex - startSampleIndex;
            double samplesPerPixel = totalSamplesInRange / widthInPixel;


            var resultSamples = new float[widthInPixel];

            for (int i = 0; i < resultSamples.Length; i++)
            {

                double sliceStart = Math.Max(startSampleIndex + i * samplesPerPixel, 0);
                double sliceEnd = Math.Min(sliceStart + samplesPerPixel, endSampleIndex);

                int start = (int)sliceStart;
                int end = (int)sliceEnd;

                start = Math.Max(0, start);
                end = Math.Min(waveformBuffer.Length, end);

                //if (start < end)
                //{
                //    var samples = waveformBuffer[start..end];
                //    resultSamples[i] = samples.Max();
                //}
                //else
                //{
                //    resultSamples[i] = 0f;
                //}


                //Better Version no GC pressure 
                if (start < end)
                {
                    float max = float.MinValue;
                    for (int j = start; j < end; j++)
                    {
                        if (waveformBuffer[j] > max)
                            max = waveformBuffer[j];
                    }
                    resultSamples[i] = max;
                }
                else
                {
                    resultSamples[i] = 0f;
                }
            }
            return resultSamples.AsMemory();
        }
    }
}