using System.IO;
using TMPro;
using UnityEngine;
using System;

namespace HuggingFace.API.Demos {
    public class VoiceControlledSpider : MonoBehaviour {
        [SerializeField] private TextMeshProUGUI statusText;
        public GameObject arachnid; // Referencia a la araña

        private AudioClip voiceClip;
        private byte[] audioData;
        private bool isRecording;

        private void Update() {
            if (Input.GetMouseButtonDown(0)) { // Click izquierdo para comenzar a grabar
                BeginAudioCapture();
            }

            if (Input.GetMouseButtonDown(1) && isRecording) { // Click derecho para detener
                EndAudioCapture();
            }
        }

        private void BeginAudioCapture() {
            statusText.color = Color.green;
            statusText.text = "Grabando...";
            voiceClip = Microphone.Start(null, false, 10, 44100);
            isRecording = true;
        }

        private void EndAudioCapture() {
            try {
                int sampleCount = Microphone.GetPosition(null);
                Microphone.End(null);
                float[] sampleArray = new float[sampleCount * voiceClip.channels];
                voiceClip.GetData(sampleArray, 0);
                audioData = ConvertToWAV(sampleArray, voiceClip.frequency, voiceClip.channels);
                isRecording = false;
                TransmitAudioData();
            } catch (Exception ex) {
                Debug.LogError($"Error al detener la grabación: {ex.Message}");
            }
        }

        private void TransmitAudioData() {
            statusText.color = Color.blue;
            statusText.text = "Enviando audio...";
            HuggingFaceAPI.AutomaticSpeechRecognition(audioData, response => {
                statusText.color = Color.white;
                statusText.text = response;
                InterpretVoiceCommand(response);
            }, error => {
                statusText.color = Color.red;
                statusText.text = error;
            });
        }

        private void InterpretVoiceCommand(string voiceOutput) {
            statusText.text = voiceOutput;

            if (voiceOutput.ToLower().Contains("back")) {
                statusText.text = "Moviendo hacia atrás...";
                MoveArachnidBackward();
            } else if (voiceOutput.ToLower().Contains("jump")) {
                statusText.text = "Saltando...";
                MakeArachnidJump();
            }
        }

        private void MoveArachnidBackward() {
            arachnid.transform.Translate(-Vector3.forward * 5.0f);
        }

        private void MakeArachnidJump() {
            Rigidbody rb = arachnid.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.AddForce(Vector3.up * 5.0f, ForceMode.Impulse);
            } else {
                Debug.LogWarning("Rigidbody no encontrado en la araña");
            }
        }

        private byte[] ConvertToWAV(float[] samples, int sampleRate, int channelCount) {
            using (var memoryStream = new MemoryStream()) {
                using (var writer = new BinaryWriter(memoryStream)) {
                    WriteWavHeader(writer, sampleRate, channelCount, samples.Length);
                    foreach (var sample in samples) {
                        writer.Write((short)(sample * short.MaxValue));
                    }
                }
                return memoryStream.ToArray();
            }
        }

        private void WriteWavHeader(BinaryWriter writer, int sampleRate, int channelCount, int sampleCount) {
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + sampleCount * 2);
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);
            writer.Write((ushort)1);
            writer.Write((ushort)channelCount);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channelCount * 2);
            writer.Write((ushort)(channelCount * 2));
            writer.Write((ushort)16);
            writer.Write("data".ToCharArray());
            writer.Write(sampleCount * 2);
        }
    }
}
