﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using FFMediaToolkit;
using FFMediaToolkit.Audio;
using FFMediaToolkit.Decoding;
using MusicX.Helpers;
using MusicX.Shared.Player;

namespace MusicX.Services.Player.Sources;

public abstract class MediaSourceBase : ITrackMediaSource
{
    private static readonly Semaphore FFmpegSemaphore = new(1, 1, "MusicX_FFmpegSemaphore");
    
    protected readonly MediaOptions MediaOptions = new()
    {
        StreamsToLoad = MediaMode.Audio,
        AudioSampleFormat = SampleFormat.SignedWord,
        DemuxerOptions =
        {
            FlagDiscardCorrupt = true,
            FlagEnableFastSeek = true,
            SeekToAny = true,
            PrivateOptions =
            {
                ["http_persistent"] = "false",
                ["reconnect"] = "1",
                ["reconnect_streamed"] = "1",
                ["reconnect_delay_max"] = "5",
                ["stimeout"] = "10"
            }
        }
    };

    public abstract Task<MediaPlaybackItem?> CreateMediaSourceAsync(MediaPlaybackSession playbackSession,
        PlaylistTrack track,
        CancellationToken cancellationToken = default);

    protected static MediaPlaybackItem CreateMediaPlaybackItem(MediaFile file)
    {
        var properties =
            AudioEncodingProperties.CreatePcm((uint)file.Audio.Info.SampleRate, (uint)file.Audio.Info.NumChannels, 16);

        var streamingSource = new MediaStreamSource(new AudioStreamDescriptor(properties))
        {
            CanSeek = true,
            IsLive = true,
            Duration = file.Audio.Info.Duration,
            BufferTime = TimeSpan.Zero
        };
        
        var position = TimeSpan.Zero;

        streamingSource.Starting += (_, args) =>
        {
            position = args.Request.StartPosition == TimeSpan.Zero
                ? file.Info.StartTime
                : args.Request.StartPosition.GetValueOrDefault();
            
            args.Request.SetActualStartPosition(position);

            try
            {
                FFmpegSemaphore.WaitOne();
                file.Audio.GetFrame(position);
            }
            catch (FFmpegException)
            {
            }
            finally
            {
                FFmpegSemaphore.Release();
            }
        };

        streamingSource.Closed += async (_, _) =>
        {
            await FFmpegSemaphore.WaitOneAsync();
            try
            {
                file.Dispose();
            }
            finally
            {
                FFmpegSemaphore.Release();
            }
        };

        streamingSource.SampleRequested += (_, args) =>
        {
            //var deferral = args.Request.GetDeferral();
            
            //await FFmpegSemaphore.WaitOneAsync();
            FFmpegSemaphore.WaitOne();
            
            try
            {
                if (file.IsDisposed)
                    return;
                
                var array = ProcessSample();
                if (array != null)
                    args.Request.Sample = MediaStreamSample.CreateFromBuffer(array.AsBuffer(), position);
            }
            finally
            {
                FFmpegSemaphore.Release();
                //deferral.Complete();
            }
        };
        
        byte[]? ProcessSample()
        {
            AudioData frame;
            while (true)
            {
                try
                {
                    if (!file.Audio.TryGetNextFrame(out frame))
                        return null;

                    position = file.Audio.Position;
                    
                    break;
                }
                catch (FFmpegException)
                {
                }
            }

            var blockSize = frame.NumSamples * Unsafe.SizeOf<short>();
            var array = new byte[frame.NumChannels * blockSize];

            frame.GetChannelData<short>(0).CopyTo(MemoryMarshal.Cast<byte, short>(array));
            
            frame.Dispose();

            return array;
        }

        return new (MediaSource.CreateFromMediaStreamSource(streamingSource));
    }
}