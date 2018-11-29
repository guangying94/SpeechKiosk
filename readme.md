# Speech Kiosk Example
This is a demo app that leverage on [Microsoft Cognitive Services](https://azure.microsoft.com/en-us/services/cognitive-services/) to build a speech-enabled FAQ Kiosk.

## Services Used
1. [Cognitive Services - Speech Service](https://azure.microsoft.com/en-us/services/cognitive-services/directory/speech/)
1. [Cognitive Services - Language Understanding Intelligent Service](https://azure.microsoft.com/en-us/services/cognitive-services/language-understanding-intelligent-service/)
1. [Cognitive Services - QnAMaker](https://azure.microsoft.com/en-us/services/cognitive-services/qna-maker/)

## Scenarios
#### Speech to text with Intent Recognition
The first scenario is straight forward, where it performs speech to text, and understand the intent. In Speech Services SDK, speech recognition and intent recognition are combined. Instead of **SpeechRecognizer**, we use **IntentRecognizer**.

```csharp
var config = SpeechConfig.FromSubscription(luisKey, luisRegion);
using (var recognizer = new IntentRecognizer(config))
{
	var model = LanguageUnderstandingModel.FromAppId(luisAppId);
    	recognizer.AddAllIntents(model);
        //Rest of the codes...
}
```

Then the result is recorded in this line:
```csharp
var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
```

We do a switch case on **result** and display results in console app.

#### Continuous Speech to text with Intent Recognition
Similar to the scenario above, but this is continuously recognize the speech when users start talking, which provides a better experience to users, so that users know the app is __listening__. 

In this case, the recognizer will has different modes as shown below:
```csharp
 recognizer.Recognizing += (s, e) =>
 {
 	Console.WriteLine($"Recognizing: Text = {e.Result.Text}");
 };
 
 recognizer.Recognized += (s, e) =>
 {
 	Console.WriteLine($"Recognized Text = {e.Result.Text}");
    	//do something else
 };
 
 recognizer.Canceled += (s, e) =>
 {
 	//Give error message
 };
 
 recognizer.SessionStarted += (s, e) =>
 {
 	//Display start session message
 };
 
 recognizer.SessionStopped += (s, e) =>
 {
 	//Display end session message
 };
 ```
 
 Take note that we need to start and stop the speech recognition.
 
 ```chsarp
 await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
 
//to stop
 await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
 ```
 
 #### Custom Speech (CRIS) with Intent Recognition
 Speech Services provides customizable speech recognition and speech transcription, to overcome background noise, or accents from users, more info [here](https://southeastasia.cris.ai/Home/CustomSpeech).
 
 In this example, I train the CRIS to understand acronym such as **ABT** and non-English words such as **Jalan Bahar** (a road name in Singapore).
 
 Sample files:
 1. [Audio files](https://1drv.ms/u/s!AsmpFVEoNfZ8odpUcZpSPdgd5Kbiyg)
 1. [Transcript](https://1drv.ms/t/s!AsmpFVEoNfZ8odpSeB-qjtRjW0kn-A)
 1. [Language File](https://1drv.ms/t/s!AsmpFVEoNfZ8odpTHQf1cqs8Ae1rDw)
 
 ##### Guide on CRIS
 1. Login to [portal](https://southeastasia.cris.ai/Home/CustomSpeech).
 2. Upload *Audio files* and *Transcript*.
 3. The portal will validate the files. Once done, create acoustic and langauge model respectively.
 4. Detailed guide - [Acoustic model](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/how-to-customize-acoustic-models)
 5. Detailed guide - [Language model](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/how-to-customize-language-model)
 6. Detailed guide - [Record your sample](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/record-custom-voice-samples)

Tips: There's a specific requirements for the audio file, hence you can use free tools like [Audacity](https://www.audacityteam.org/) to do recording.
 
    