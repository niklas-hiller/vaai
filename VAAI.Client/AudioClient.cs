﻿using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using VAAI.Library;
using VAAI.Shared.Enums;

namespace VAAI.Client
{
    internal class AudioClient
    {
        private readonly HubClient Client;
        private readonly int SampleRate;
        private readonly int Channels;

        public AudioClient(int sampleRate, int channels) 
        {
            Client = new HubClient("Client");
            Client.registerInvoker();

            SampleRate = sampleRate;
            Channels = channels;
        }

        public async Task RecordMicrophoneAsync(Channel<byte[]> audioChannel, CancellationToken cancellationToken)
        {
            using var waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(SampleRate, Channels);

            waveIn.DataAvailable += (sender, args) =>
            {
                var buffer = new byte[args.BytesRecorded];
                Array.Copy(args.Buffer, buffer, args.BytesRecorded);
                audioChannel.Writer.TryWrite(buffer);
            };

            waveIn.StartRecording();

            await Task.Delay(Timeout.Infinite, cancellationToken);

            waveIn.StopRecording();
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await Client.StartAsync();

            var audioChannel = Channel.CreateUnbounded<byte[]>();
            _ = RecordMicrophoneAsync(audioChannel, cancellationToken);
            try
            {
                var recordSeconds = 5;
                var sampleSize = SampleRate * recordSeconds;
                var samples = new float[sampleSize];
                var currentIndex = 0;

                await foreach (var buffer in audioChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    for (int i = 0; i < buffer.Length; i += 2)
                    {
                        var sampleValue = BitConverter.ToInt16(buffer, i) / Channels / (float)short.MaxValue;
                        samples[currentIndex] = sampleValue;

                        currentIndex++;
                        if (currentIndex >= sampleSize)
                        {
                            var id = await Client.InvokeSTT(samples);

                            currentIndex = 0;
                        }
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
            finally
            {
                audioChannel.Writer.Complete();
            }
        }
    }
}