using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Intent;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using System.Media;
using System.IO;

namespace SpeechKiosk
{
    class Program
    {
        //LUIS Key
        const string luisKey = "<LUIS API Key>";
        const string luisAppId = "<LUIS App ID>";
        const string luisRegion = "<LUIS Region>";

        //custom speech key
        const string customSpeechKey = "<Custom Speech API>";
        const string customSpeechId = "<Custom Speech App ID>";
        const string customSpeechRegion = "<Custom Speech Region>";

        //speech key
        const string speechKey = "<Speech Service API Key>";
        const string speechRegion = "<Speech Service Region>";

        static void Main(string[] args)
        {
            Console.WriteLine("This is a sample demo for speech recognition.");
            Console.WriteLine("Which mode? (Enter number)");
            Console.WriteLine("[1] Normal speech recognition");
            Console.WriteLine("[2] Continous speech recognition with intent");
            Console.WriteLine("[3] Continuous custom speech recognition with intent");
            Console.WriteLine("[4] Speech Translation");
            Console.Write("Your choice (0: Stop.): ");

            ConsoleKeyInfo x;
            do
            {
                x = Console.ReadKey();
                Console.WriteLine("");
                switch (x.Key)
                {
                    case ConsoleKey.D1:
                        RecognizeIntentAsync().Wait();
                        break;
                    case ConsoleKey.D2:
                        ContinuousRecognizeIntentAsync().Wait();
                        break;
                    case ConsoleKey.D3:
                        CustomRecognizeAsync().Wait();
                        break;
                    case ConsoleKey.D4:
                        SpeechTranslationAsync().Wait();
                        break;
                    case ConsoleKey.D0:
                        Console.WriteLine("Exiting...");
                        break;
                    default:
                        Console.WriteLine("Invalid input.");
                        break;
                }
                Console.WriteLine("\nRecognition done. Your Choice (0: Stop): ");
            } while (x.Key != ConsoleKey.D0);
        }

        static async Task ContinuousRecognizeIntentAsync()
        {
            //continuous speech recognition and detect intent
            var config = SpeechConfig.FromSubscription(luisKey, luisRegion);

            using (var recognizer = new IntentRecognizer(config))
            {

                var stopRecognition = new TaskCompletionSource<int>();

                var model = LanguageUnderstandingModel.FromAppId(luisAppId);
                recognizer.AddAllIntents(model);

                recognizer.Recognizing += (s, e) =>
                {
                    Console.WriteLine($"Recognizing: Text = {e.Result.Text}");
                };

                recognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedIntent)
                    {
                        Console.WriteLine($"Recognized: Text = {e.Result.Text}");
                        LUIS.GetLuisResult(e.Result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult));
                        string intent = LUIS.GetLuisIntent(e.Result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult));
                        LUIS.ResponseToLuisIntent(intent, e.Result.Text);
                    }

                    else if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        Console.WriteLine($"Recognized: Text = {e.Result.Text}");
                        Console.WriteLine("Intent not recognized.");
                    }
                };

                recognizer.Canceled += (s, e) =>
                {
                    Console.WriteLine($"Canceled: Reason={e.Reason}");
                    if (e.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }
                    stopRecognition.TrySetResult(0);
                };

                recognizer.SessionStarted += (s, e) =>
                {
                    Console.WriteLine("\n Session started event.");
                };

                recognizer.SessionStopped += (s, e) =>
                {
                    Console.WriteLine("\n Session stopped.");
                    stopRecognition.TrySetResult(0);
                };

                Console.WriteLine("[Continous Recognition] Say something...");
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                do
                {
                    Console.WriteLine("Press Enter to stop");
                } while (Console.ReadKey().Key != ConsoleKey.Enter);

                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
        }

        static async Task RecognizeIntentAsync()
        {
            //simple speech recognition with intent
            var config = SpeechConfig.FromSubscription(luisKey, luisRegion);

            using (var recognizer = new IntentRecognizer(config))
            {
                var model = LanguageUnderstandingModel.FromAppId(luisAppId);

                //add LUIS intents, you have the option to add only selected intents
                recognizer.AddAllIntents(model);

                Console.WriteLine("Say something...");

                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

                if (result.Reason == ResultReason.RecognizedIntent)
                {
                    Console.WriteLine($"Recognized: Text = {result.Text}");
                    Console.WriteLine($"Language Understanding JSON: {result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult)}");
                }
                else if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"Recognized: Text = {result.Text}");
                    Console.WriteLine("Intent not recognized.");
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine("Speech could not be recognized.");
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    Console.WriteLine($"Canceled. Reason = {cancellation.Reason}");
                }
            }
        }

        static async Task CustomRecognizeAsync()
        {
            //custom speech recognition with continuous recognition
            var config = SpeechConfig.FromSubscription(customSpeechKey, customSpeechRegion);
            config.EndpointId = customSpeechId;

            var stopRecognition = new TaskCompletionSource<int>();

            using (var recognizer = new SpeechRecognizer(config))
            {

                recognizer.Recognizing += (s, e) =>
                {
                    Console.WriteLine($"Recognizing: Text = {e.Result.Text}");
                };

                recognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        Console.WriteLine($"Recognized: Text = {e.Result.Text}");
                        //get the full text, send the request to luis to identify intent and get response
                        LUIS.SendToLuis(e.Result.Text, luisAppId, luisKey).Wait();
                    }
                };

                recognizer.Canceled += (s, e) =>
                {
                    Console.WriteLine($"Canceled: Reason={e.Reason}");
                    if (e.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }
                    stopRecognition.TrySetResult(0);
                };

                recognizer.SessionStarted += (s, e) =>
                {
                    Console.WriteLine("\n Session started event.");
                };

                recognizer.SessionStopped += (s, e) =>
                {
                    Console.WriteLine("\n Session stopped.");
                    stopRecognition.TrySetResult(0);
                };

                Console.WriteLine("[Custom Speech] Say something...");
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                do
                {
                    Console.WriteLine("Press Enter to stop");
                } while (Console.ReadKey().Key != ConsoleKey.Enter);

                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
        }

        static async Task SpeechTranslationAsync()
        {
            //real time speech translation from english to chinese
            string fromLanguage = "en-US";
            const string EngVoice = "Microsoft Server Speech Text to Speech Voice (zh-CN, HuihuiRUS)";
            var config = SpeechTranslationConfig.FromSubscription(speechKey, speechRegion);
            config.SpeechRecognitionLanguage = fromLanguage;
            config.VoiceName = EngVoice;
            config.AddTargetLanguage("zh");

            using (var recognizer = new TranslationRecognizer(config))
            {
                // Subscribes to events.
                recognizer.Recognizing += (s, e) =>
                {
                    Console.WriteLine($"RECOGNIZING in '{fromLanguage}': Text={e.Result.Text}");
                    foreach (var element in e.Result.Translations)
                    {
                        Console.WriteLine($"    TRANSLATING into '{element.Key}': {element.Value}");
                    }
                };

                recognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.TranslatedSpeech)
                    {
                        Console.WriteLine($"RECOGNIZED in '{fromLanguage}': Text={e.Result.Text}");
                        foreach (var element in e.Result.Translations)
                        {
                            Console.WriteLine($"    TRANSLATED into '{element.Key}': {element.Value}");
                        }
                    }
                    else if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                        Console.WriteLine($"    Speech not translated.");
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                };

                recognizer.Synthesizing += (s, e) =>
                {
                    var audio = e.Result.GetAudio();
                    Console.WriteLine(audio.Length != 0
                        ? $"AudioSize: {audio.Length}"
                        : $"AudioSize: {audio.Length} (end of synthesis data)");

                    if (audio.Length > 0)
                    {
                        using (var m = new MemoryStream(audio))
                        {
                            SoundPlayer simpleSound = new SoundPlayer(m);
                            simpleSound.PlaySync();
                        }
                    }
                };

                recognizer.Canceled += (s, e) =>
                {
                    Console.WriteLine($"CANCELED: Reason={e.Reason}");

                    if (e.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }
                };

                recognizer.SessionStarted += (s, e) =>
                {
                    Console.WriteLine("\nSession started event.");
                };

                recognizer.SessionStopped += (s, e) =>
                {
                    Console.WriteLine("\nSession stopped event.");
                };

                // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                Console.WriteLine("[Speech Translation] [English to Chinese] Say something...");
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                do
                {
                    Console.WriteLine("Press Enter to stop");
                } while (Console.ReadKey().Key != ConsoleKey.Enter);

                // Stops continuous recognition.
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }

        }
    }
}
