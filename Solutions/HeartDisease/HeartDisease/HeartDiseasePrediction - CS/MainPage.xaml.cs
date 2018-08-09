using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HeartDiseasePrediction
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private CNTKGraphModel ModelGen = new CNTKGraphModel();
        private CNTKGraphModelInput ModelInput = new CNTKGraphModelInput();
        private CNTKGraphModelOutput ModelOutput = new CNTKGraphModelOutput();

        public class StringTable
        {
            public string[] ColumnNames { get; set; }
            public string[,] Values { get; set; }
        }

        public MainPage()
        {
            this.InitializeComponent();

            comboBoxThal.SelectedIndex = 1;
            comboBoxCp.SelectedIndex = 2;
            comboBoxCa.SelectedIndex = 2;
            textBoxOldpeak.Text = "1";
            comboBoxExang.SelectedIndex = 0;
            textBoxThalach.Text = "180";
            comboBoxSlope.SelectedIndex = 0;
            textBoxAge.Text = "60";

            LoadModel();
        }

        private async void LoadModel()
        {
            //Load a machine learning model
            StorageFile modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/Heart.onnx"));
            ModelGen = await CNTKGraphModel.CreateCNTKGraphModel(modelFile);
        }

        private async void InvokeRequestResponseService()
        {
            ring.IsActive = true;

            try
            {
                using (var client = new HttpClient())
                {
                    var scoreRequest = new
                    {

                        Inputs = new Dictionary<string, StringTable>() {
                        {
                            "input1",
                            new StringTable()
                            {
                                ColumnNames = new string[] { "heart_disease_diag", "thal", "chestpaintype", "number_of_major_vessel",
                                    "st_depression_induced_by_exercise", "exercise_induced_angina", "max_heart_rate",
                                    "slope_of_peak_exercise", "age"},
                                Values = new string[,] {  { "0",
                                                            comboBoxThal.SelectedValue.ToString().Substring(0, 1),
                                                            comboBoxCp.SelectedValue.ToString().Substring(0, 1),
                                                            comboBoxCa.SelectedValue.ToString().Substring(0, 1),
                                                            textBoxOldpeak.Text,
                                                            comboBoxExang.SelectedValue.ToString().Substring(0, 1),
                                                            textBoxThalach.Text,
                                                            comboBoxSlope.SelectedValue.ToString().Substring(0, 1),
                                                            textBoxAge.Text } }

                            }
                        },
                                        },
                        GlobalParameters = new Dictionary<string, string>()
                        {
                        }
                    };

                    const string apiKey = "BqLG47rjT/ox07z4UiSzQT6YNX1k0FUXIfQyqLGKTLyRStzdf9wrmxd5qGgmvlglcEIObmjR1w9rohj6vsJTzA=="; // Replace this with the API key for the web service
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                    client.BaseAddress = new Uri("https://ussouthcentral.services.azureml.net/workspaces/3deea62efa414e73b85abc9652f52010/services/18454b9e6a75450299ed56450d99a881/execute?api-version=2.0&details=true");

                    // WARNING: The 'await' statement below can result in a deadlock if you are calling this code from the UI thread of an ASP.Net application.
                    // One way to address this would be to call ConfigureAwait(false) so that the execution does not attempt to resume on the original context.
                    // For instance, replace code such as:
                    //      result = await DoSomeTask()
                    // with the following:
                    //      result = await DoSomeTask().ConfigureAwait(false)

                    HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest);

                    ring.IsActive = false;

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();

                        JsonObject jsonObject = JsonValue.Parse(result).GetObject().GetNamedObject("Results").GetNamedObject("output1").GetNamedObject("value");
                        var responseBody = JsonConvert.DeserializeObject<StringTable>(jsonObject.ToString());

                        var percentage = float.Parse(responseBody.Values[0, 10]) * 100;
                        textBlockResult.Text = String.Format("Probability of Heart Disease is {0:0.00}%", percentage);
                        icon.Text = (percentage > 50) ? "😧" : "😃";
                    }
                    else
                    {
                        textBlockResult.Text = String.Format("The request failed: {0}", response.StatusCode);
                    }
                }
            }
            catch (Exception)
            {
                ring.IsActive = false;
                textBlockResult.Text = "Parameter incorrect";
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Bind model input
                ModelInput.Input3 = new List<float>();
                foreach(var n in GetThalValue()){
                    ModelInput.Input3.Add(n);
                }

                foreach (var n in GetChestPainType())
                {
                    ModelInput.Input3.Add(n);
                }

                ModelInput.Input3.Add(GetMajorVesselsNumber());
                ModelInput.Input3.Add(GetSTDepByExercise());
                ModelInput.Input3.Add(GetAnginaByExercise());
                ModelInput.Input3.Add(GetMaxHeartRate());

                foreach (var n in GetExerciseSlope())
                {
                    ModelInput.Input3.Add(n);
                }

                ModelInput.Input3.Add(GetAge());

                //Evaluate the model
                ModelOutput = await ModelGen.EvaluateAsync(ModelInput);

                //Iterate through evaluation output to determine highest probability digit
                float maxProb = 0;
                int maxIndex = 0;
                for (int i = 0; i < ModelOutput.Softmax99_Output_0.Count; i++)
                {
                    if (ModelOutput.Softmax99_Output_0[i] > maxProb)
                    {
                        maxIndex = i;
                        maxProb = ModelOutput.Softmax99_Output_0[i];
                    }
                }

                Debug.WriteLine("Yes: {0:0.00}%, No: {1:0.00}%", ModelOutput.Softmax99_Output_0[0] * 100, ModelOutput.Softmax99_Output_0[1] * 100);

                var percentage = ModelOutput.Softmax99_Output_0[0] * 100;
                textBlockResult.Text = String.Format("Probability of Heart Disease is {0:0.00}%", percentage);
                icon.Text = (maxIndex == 0) ? "😧" : "😃";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private List<float> GetThalValue()
        {
            List<float> val = new List<float>();

            switch (comboBoxThal.SelectedIndex)
            {
                case 0:
                    val.Add(0);
                    val.Add(1);
                    break;
                case 1:
                    val.Add(1);
                    val.Add(0);
                    break;
                case 2:
                    val.Add(-1);
                    val.Add(-1);
                    break;
                default:
                    break;
            }

            return val;
        }

        private List<float> GetChestPainType()
        {
            List<float> val = new List<float>();

            switch (comboBoxCp.SelectedIndex)
            {
                case 0:
                    val.Add(1);
                    val.Add(0);
                    val.Add(0);
                    break;
                case 1:
                    val.Add(0);
                    val.Add(1);
                    val.Add(0);
                    break;
                case 2:
                    val.Add(0);
                    val.Add(0);
                    val.Add(1);
                    break;
                case 3:
                    val.Add(-1);
                    val.Add(-1);
                    val.Add(-1);
                    break;
                default:
                    break;
            }

            return val;
        }

        private float GetMajorVesselsNumber()
        {
            var num = comboBoxCa.SelectedIndex;
            return Convert.ToSingle(num);
        }

        private float GetSTDepByExercise()
        {
            var min = 0.8f;
            var max = 6.2f;
            var num = Convert.ToSingle(textBoxOldpeak.Text);
            if (num > max) num = max;
            if (num < min) num = min;
            var normalized = (num - min) / (max - min);
            return normalized;
        }

        private float GetAnginaByExercise()
        {
            return Convert.ToSingle(comboBoxExang.SelectedIndex);
        }

        private float GetMaxHeartRate()
        {
            var min = 71f;
            var max = 202f;
            var num = Convert.ToSingle(textBoxThalach.Text);
            if (num > max) num = max;
            if (num < min) num = min;
            var normalized = (num - min) / (max - min);
            return normalized;
        }

        private List<float> GetExerciseSlope()
        {
            List<float> val = new List<float>();

            switch (comboBoxSlope.SelectedIndex)
            {
                case 0:
                    val.Add(0);
                    val.Add(1);
                    break;
                case 1:
                    val.Add(1);
                    val.Add(0);
                    break;
                case 2:
                    val.Add(-1);
                    val.Add(-1);
                    break;
                default:
                    break;
            }

            return val;
        }

        private float GetAge()
        {
            var min = 29f;
            var max = 77f;
            var num = Convert.ToSingle(textBoxAge.Text);
            if (num > max) num = max;
            if (num < min) num = min;
            var normalized = (num - min) / (max - min);
            return normalized;
        }
    }
}
