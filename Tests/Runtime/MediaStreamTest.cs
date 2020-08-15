using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Linq;
using System.Collections;

namespace Unity.WebRTC.RuntimeTest
{
    class MediaStreamTest
    {
        [SetUp]
        public void SetUp()
        {
            var value = NativeMethods.GetHardwareEncoderSupport();
            WebRTC.Initialize(value ? EncoderType.Hardware : EncoderType.Software);
        }

        [TearDown]
        public void TearDown()
        {
            WebRTC.Dispose();
        }

        [Test]
        public void CreateAndDeleteMediaStream()
        {
            var stream = new MediaStream();
            Assert.NotNull(stream);
            stream.Dispose();
        }

        [Test]
        public void RegisterDelegate()
        {
            var stream = new MediaStream();
            stream.OnAddTrack = e => {};
            stream.OnRemoveTrack = e => {};
            stream.Dispose();
        }

        [UnityTest]
        [Timeout(5000)]
        [Category("MediaStreamTrack")]
        public IEnumerator MediaStreamTrackEnabled()
        {
            var width = 256;
            var height = 256;
            var format = WebRTC.GetSupportedRenderTextureFormat(SystemInfo.graphicsDeviceType);
            var rt = new RenderTexture(width, height, 0, format);
            rt.Create();
            var track = new VideoStreamTrack("video", rt);
            Assert.NotNull(track);
            yield return new WaitForSeconds(0.1f);
            Assert.True(track.IsInitialized);

            // Enabled property
            Assert.True(track.Enabled);
            track.Enabled = false;
            Assert.False(track.Enabled);

            // ReadyState property
            Assert.AreEqual(track.ReadyState, TrackState.Live);
            track.Dispose();
            yield return new WaitForSeconds(0.1f);

            Object.DestroyImmediate(rt);
        }

        [UnityTest]
        [Timeout(5000)]
        [Category("MediaStream")]
        public IEnumerator MediaStreamAddTrack()
        {
            var width = 256;
            var height = 256;
            var format = WebRTC.GetSupportedRenderTextureFormat(UnityEngine.SystemInfo.graphicsDeviceType);
            var rt = new UnityEngine.RenderTexture(width, height, 0, format);
            rt.Create();
            var stream = new MediaStream();
            var track = new VideoStreamTrack("video", rt);
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(TrackKind.Video, track.Kind);
            Assert.AreEqual(0, stream.GetVideoTracks().Count());
            Assert.True(stream.AddTrack(track));
            Assert.AreEqual(1, stream.GetVideoTracks().Count());
            Assert.NotNull(stream.GetVideoTracks().First());
            Assert.True(stream.RemoveTrack(track));
            Assert.AreEqual(0, stream.GetVideoTracks().Count());
            track.Dispose();
            yield return new WaitForSeconds(0.1f);
            stream.Dispose();
            Object.DestroyImmediate(rt);
        }

        [Test]
        public void AddAndRemoveAudioStreamTrack()
        {
            var stream = new MediaStream();
            var track = new AudioStreamTrack("audio");
            Assert.AreEqual(TrackKind.Audio, track.Kind);
            Assert.AreEqual(0, stream.GetAudioTracks().Count());
            Assert.True(stream.AddTrack(track));
            Assert.AreEqual(1, stream.GetAudioTracks().Count());
            Assert.NotNull(stream.GetAudioTracks().First());
            Assert.True(stream.RemoveTrack(track));
            Assert.AreEqual(0, stream.GetAudioTracks().Count());
            track.Dispose();
            stream.Dispose();
        }

        /// <todo>
        /// This unittest failed standalone mono 2019.3 on linux
        /// </todo>
        [UnityTest]
        [Timeout(5000)]
        public IEnumerator CameraCaptureStream()
        {
            var camObj = new GameObject("Camera");
            var cam = camObj.AddComponent<Camera>();
            var videoStream = cam.CaptureStream(1280, 720, 1000000);
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(1, videoStream.GetVideoTracks().Count());
            Assert.AreEqual(0, videoStream.GetAudioTracks().Count());
            Assert.AreEqual(1, videoStream.GetTracks().Count());
            yield return new WaitForSeconds(0.1f);
            videoStream.Dispose();
            Object.DestroyImmediate(camObj);
        }

        [Test]
        public void AddAndRemoveAudioStream()
        {
            var audioStream = Audio.CaptureStream();
            Assert.AreEqual(1, audioStream.GetAudioTracks().Count());
            Assert.AreEqual(0, audioStream.GetVideoTracks().Count());
            Assert.AreEqual(1, audioStream.GetTracks().Count());
            audioStream.Dispose();
        }

        [UnityTest]
        [Timeout(5000)]
        public IEnumerator AddAndRemoveAudioMediaTrack()
        {
            RTCConfiguration config = default;
            config.iceServers = new[]
            {
                new RTCIceServer {urls = new[] {"stun:stun.l.google.com:19302"}}
            };
            var audioStream = Audio.CaptureStream();
            var test = new MonoBehaviourTest<SignalingPeers>();
            test.component.SetStream(audioStream);
            yield return test;
            test.component.Dispose();
            audioStream.Dispose();
        }

        /// <todo>
        /// This unittest failed standalone mono 2019.3 on linux
        /// </todo>
        [UnityTest]
        [Timeout(5000)]
        public IEnumerator CaptureStream()
        {
            var camObj = new GameObject("Camera");
            var cam = camObj.AddComponent<Camera>();
            var videoStream = cam.CaptureStream(1280, 720, 1000000);
            yield return new WaitForSeconds(0.1f);

            var test = new MonoBehaviourTest<SignalingPeers>();
            test.component.SetStream(videoStream);
            yield return test;
            test.component.CoroutineUpdate();
            yield return new WaitForSeconds(0.1f);
            test.component.Dispose();
            videoStream.Dispose();
            Object.DestroyImmediate(camObj);
        }

        [UnityTest]
        [Timeout(5000)]
        public IEnumerator SenderGetStats()
        {
            var camObj = new GameObject("Camera");
            var cam = camObj.AddComponent<Camera>();
            var videoStream = cam.CaptureStream(1280, 720, 1000000);
            yield return new WaitForSeconds(0.1f);

            var test = new MonoBehaviourTest<SignalingPeers>();
            test.component.SetStream(videoStream);
            yield return test;
            test.component.CoroutineUpdate();
            yield return new WaitForSeconds(0.1f);
            var op = test.component.GetSenderStats(0);
            yield return op;
            Assert.True(op.IsDone);
            Assert.IsNotEmpty(op.Value.Stats);
            Assert.Greater(op.Value.Stats.Count, 0);

            foreach (RTCStats stats in op.Value.Stats.Values)
            {
                Assert.NotNull(stats);
                Assert.Greater(stats.Timestamp, 0);
                Assert.IsNotEmpty(stats.Id);
                foreach (var pair in stats.Dict)
                {
                    Assert.IsNotEmpty(pair.Key);
                    Assert.NotNull(pair.Value);
                }
                StatsCheck.Test(stats);
            }

            op.Value.Dispose();
            test.component.Dispose();
            videoStream.Dispose();
            Object.DestroyImmediate(camObj);
        }

        [UnityTest]
        [Timeout(5000)]
        public IEnumerator ReceiverGetStats()
        {
            var camObj = new GameObject("Camera");
            var cam = camObj.AddComponent<Camera>();
            var videoStream = cam.CaptureStream(1280, 720, 1000000);
            yield return new WaitForSeconds(0.1f);

            var test = new MonoBehaviourTest<SignalingPeers>();
            test.component.SetStream(videoStream);
            yield return test;
            test.component.CoroutineUpdate();
            yield return new WaitForSeconds(0.1f);
            var op = test.component.GetReceiverStats(0);
            yield return op;
            Assert.True(op.IsDone);
            Assert.IsNotEmpty(op.Value.Stats);
            Assert.Greater(op.Value.Stats.Count, 0);

            foreach (RTCStats stats in op.Value.Stats.Values)
            {
                Assert.NotNull(stats);
                Assert.Greater(stats.Timestamp, 0);
                Assert.IsNotEmpty(stats.Id);
                foreach (var pair in stats.Dict)
                {
                    Assert.IsNotEmpty(pair.Key);
                    Assert.NotNull(pair.Value);
                }
                StatsCheck.Test(stats);
            }
            test.component.Dispose();
            videoStream.Dispose();
            Object.DestroyImmediate(camObj);
        }

        /// <todo>
        /// This unittest failed standalone mono 2019.3 on linux
        /// </todo>
        [UnityTest]
        [Timeout(5000)]
        public IEnumerator CaptureStreamTrack()
        {
            var camObj = new GameObject("Camera");
            var cam = camObj.AddComponent<Camera>();
            var track = cam.CaptureStreamTrack(1280, 720, 1000000);
            yield return new WaitForSeconds(0.1f);
            track.Dispose();
            yield return new WaitForSeconds(0.1f);
            Object.DestroyImmediate(camObj);
        }

        /// <todo>
        /// This unittest failed standalone mono 2019.3 on linux
        /// </todo>
        [UnityTest]
        [Timeout(5000)]
        public IEnumerator SetParametersReturnNoError()
        {
            var camObj = new GameObject("Camera");
            var cam = camObj.AddComponent<Camera>();
            var videoStream = cam.CaptureStream(1280, 720, 1000000);
            yield return new WaitForSeconds(0.1f);

            var test = new MonoBehaviourTest<SignalingPeers>();
            test.component.SetStream(videoStream);
            yield return test;
            test.component.CoroutineUpdate();
            yield return new WaitForSeconds(0.1f);

            var senders = test.component.GetPeer1Senders();
            Assert.IsNotEmpty(senders);

            foreach(var sender in senders)
            {
                var parameters = sender.GetParameters();
                Assert.IsNotEmpty(parameters.Encodings);
                const uint framerate = 20;
                parameters.Encodings[0].maxFramerate = framerate;
                RTCErrorType error = sender.SetParameters(parameters);
                Assert.AreEqual(RTCErrorType.None, error);
                var parameters2 = sender.GetParameters();
                Assert.AreEqual(framerate, parameters2.Encodings[0].maxFramerate);
            }

            test.component.Dispose();
            videoStream.Dispose();
            Object.DestroyImmediate(camObj);
        }
    }
}
