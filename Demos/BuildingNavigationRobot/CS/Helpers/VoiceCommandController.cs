// Copyright (c) Microsoft. All rights reserved.

using AzureIoTHub;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.SpeechRecognition;

namespace BuildingNavigationRobot
{
    public class VoiceCommandControllerEventArgs : EventArgs
    {
        public object Data;
    }

    public enum VoiceControllerState
    {
        ListenCommand,
        ListenTrigger,
        ProcessCommand,
        ProcessTrigger,
        Idle
    }

    public enum VoiceCommandType
    {
        None,
        Navigate,
        Move,
        Stop
    }

    public struct VoiceCommand
    {
        public VoiceCommandType CommandType;
        public string CommandText;
        public object Data;
    }

    public class VoiceCommandController
    {
        public const string ROBOT_NAME = "DJ Roomba";

        /// <summary>
        /// The list of key phrases the robot is listening for to start interaction.
        /// </summary>
        private string[] activationPhrases = 
        {
            "ok " + ROBOT_NAME,
            "hey " + ROBOT_NAME,
            "hi " + ROBOT_NAME,
            "hello " + ROBOT_NAME,
            ROBOT_NAME
        };

        private string[] navCommands = 
        {
            "go to",
            "navigate to",
            "take me to",
            "navigate me to",
        };

        private string initText = "Say \"Hey " + ROBOT_NAME + "\" to activate me!";

        /// <summary>
        /// Local recognizer used for listening for the robot's name to start interacting.
        /// </summary>
        private SpeechRecognizer triggerRecognizer;

        /// <summary>
        /// Web speech recognizer used for command interpretation.
        /// </summary>
        private SpeechRecognizer speechRecognizer;

        public delegate void VoiceCommandControllerResponseEventHandler(object sender, VoiceCommandControllerEventArgs e);
        public delegate void VoiceCommandControllerCommandReceivedEventHandler(object sender, VoiceCommandControllerEventArgs e);
        public delegate void VoiceCommandControllerStateChangedEventHandler(object sender, VoiceCommandControllerEventArgs e);

        public event VoiceCommandControllerResponseEventHandler ResponseReceived;
        public event VoiceCommandControllerCommandReceivedEventHandler CommandReceived;
        public event VoiceCommandControllerStateChangedEventHandler StateChanged;

        public VoiceControllerState State = VoiceControllerState.Idle;
        

        /// <summary>
        /// Initializes the speech recognizer.
        /// </summary>
        public async void Initialize()
        {
            // Local recognizer
            triggerRecognizer = new SpeechRecognizer();

            var list = new SpeechRecognitionListConstraint(activationPhrases);
            triggerRecognizer.Constraints.Add(list);
            await triggerRecognizer.CompileConstraintsAsync();

            triggerRecognizer.ContinuousRecognitionSession.Completed += localSessionCompleted;

            triggerRecognizer.ContinuousRecognitionSession.ResultGenerated +=
                LocalSessionResult;

            //triggerRecognizer.HypothesisGenerated += CommandHypothesisGenerated;


            // Command recognizer (web)
            speechRecognizer = new SpeechRecognizer();
            var result = await speechRecognizer.CompileConstraintsAsync();

            speechRecognizer.ContinuousRecognitionSession.ResultGenerated +=
                CommandResultGenerated;

            speechRecognizer.HypothesisGenerated += CommandHypothesisGenerated;

            speechRecognizer.ContinuousRecognitionSession.Completed +=
                CommandSessionCompleted;

            await StartTriggerRecognizer();

            OnResponseReceived(initText);
        }

        private async Task StartTriggerRecognizer()
        {
            try
            {
                await triggerRecognizer.ContinuousRecognitionSession.StartAsync();
                Debug.WriteLine("Launched trigger recognizer");
                OnStateChanged(VoiceControllerState.ListenTrigger);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// This method will stop the trigger recognizer session and launch the command recognizer session.
        /// </summary>
        /// <returns></returns>
        private async Task StartCommandRecognizer()
        {
            try
            {
                await triggerRecognizer.ContinuousRecognitionSession.StopAsync();
                await speechRecognizer.ContinuousRecognitionSession.StartAsync();
                Debug.WriteLine("Trigger -> Command");
                OnStateChanged(VoiceControllerState.ListenCommand);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// This method will launch the trigger recognizer session.
        /// </summary>
        /// <returns></returns>
        private async Task StopCommandRecognizer()
        {
            try
            {
                await speechRecognizer.ContinuousRecognitionSession.StopAsync();
                Debug.WriteLine("Stopped command recognizer.");
                OnStateChanged(VoiceControllerState.Idle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Method that runs when the local recognizer generates a result.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void LocalSessionResult(
            SpeechContinuousRecognitionSession sender,
            SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            Debug.WriteLine("Received command");
            if (args.Result.Confidence != SpeechRecognitionConfidence.Rejected)
            {
                Debug.WriteLine("Received command: OK");
                await StartCommandRecognizer();
                OnResponseReceived("TriggerSuccess");
            }
        }

        /// <summary>
        /// This method is called whenever the local recognizer generates a completed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void localSessionCompleted(
            SpeechContinuousRecognitionSession sender,
            SpeechContinuousRecognitionCompletedEventArgs args)
        {
            string text = args.Status.ToString();
            Debug.WriteLine("EndLocal -> " + text);

            //If the session stopped for some reason not related to success relaunch it.
            if (text != "Success" && text != "MicrophoneUnavailable")
            {
                Debug.WriteLine("Relaunching");
                StartTriggerRecognizer();
            }
        }

        /// <summary>
        /// Method that runs when the recognition session is completed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void CommandSessionCompleted(
            SpeechContinuousRecognitionSession sender,
            SpeechContinuousRecognitionCompletedEventArgs args)
        {
            string text = args.Status.ToString();
            Debug.WriteLine("EndCommand -> " + text);
            if (text == "TimeoutExceeded")
            {
                OnResponseReceived(text);
                await StartTriggerRecognizer();
            }
        }

        /// <summary>
        /// Runs when a hypothesis is generated, displays the text on the screen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CommandHypothesisGenerated(
            SpeechRecognizer sender,
            SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            OnResponseReceived(args.Hypothesis.Text);
        }

        /// <summary>
        /// Runs when a final result is created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void CommandResultGenerated(
            SpeechContinuousRecognitionSession sender,
            SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            try
            {
                string text = args.Result.Text;
                Debug.WriteLine("-> " + text);

                await StopCommandRecognizer();

                string[] tokens = text.Split();
                var type = ValidateCommand(text);

                if (text.Contains("what can I say"))
                {
                    OnResponseReceived("Try: \"Go to room 2011\"");
                    //VoiceFeedback("Try take me to room 2011");
                    await Task.Delay(3000);
                }
                else
                {
                    int dest = -1;
                    try
                    {
                        dest = int.Parse(tokens.Last());
                    }
                    catch (Exception)
                    {
                        // Fail silently
                    }

                    OnCommandReceived(new VoiceCommand()
                    {
                        CommandType = type,
                        CommandText = text,
                        Data = dest
                    });
                }

                await StartTriggerRecognizer();
            }catch(Exception ex)
            {
                // Command not in expected format
                Debug.WriteLine("Something Broke!\n {0}", ex.Message);
                await FailedCommand();
            }
        }

        private async Task FailedCommand(string text = "Sorry, I didn't understand that.", string error = "Failed Command.")
        {
            Telemetry.SendReport(Telemetry.MessageType.VoiceCommand, error);
            //VoiceFeedback(text);
            OnResponseReceived(text);
            await Task.Delay(3000);
        }

        /// <summary>
        /// Validates a command string against pre-defined key phrases.
        /// </summary>
        /// <param name="spr"></param>
        /// <returns></returns>
        private VoiceCommandType ValidateCommand(string spr)
        {
            foreach (string s in navCommands)
            {
                if (spr.Contains(s))
                {
                    return VoiceCommandType.Navigate;
                }
                else if(spr.Contains("move"))
                {
                    return VoiceCommandType.Move;
                }
                else if(spr.Contains("stop"))
                {
                    return VoiceCommandType.Stop;
                }
            }

            return VoiceCommandType.None;
        }

        protected virtual void OnResponseReceived(string message)
        {
            ResponseReceived?.Invoke(this, new VoiceCommandControllerEventArgs()
            {
                Data = message
            });
        }

        protected virtual void OnCommandReceived(VoiceCommand command)
        {
            CommandReceived?.Invoke(this, new VoiceCommandControllerEventArgs()
            {
                Data = command
            });
        }

        protected virtual void OnStateChanged(VoiceControllerState state)
        {
            State = state;
            StateChanged?.Invoke(this, new VoiceCommandControllerEventArgs()
            {
                Data = state
            });
        }
    }
}
